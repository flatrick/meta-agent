using System;
using System.IO;
using System.Text.Json;
using MetaAgent.Core;
using Xunit;

public class MetricsAndRunResultTests
{
    [Fact]
    public void MetricsScoreboard_Load_ReturnsDefault_WhenFileMissing()
    {
        var path = Path.Combine(CreateTempDir(), "missing-metrics.json");
        var record = MetricsScoreboard.Load(path);
        Assert.Equal(0, record.TotalRuns);
        Assert.Equal(0, record.SuccessfulRuns);
    }

    [Fact]
    public void MetricsScoreboard_Update_TracksSuccessAndRates()
    {
        var path = Path.Combine(CreateTempDir(), "metrics.json");

        var record = MetricsScoreboard.Update(path, new MetricsUpdate
        {
            Command = "init",
            ExitCode = 0,
            TokensRequested = 50
        });

        Assert.Equal(1, record.TotalRuns);
        Assert.Equal(1, record.SuccessfulRuns);
        Assert.Equal(0, record.FailedRuns);
        Assert.Equal(50, record.TotalTokensRequested);
        Assert.Equal(1.0, record.SuccessRate);
        Assert.Equal(50.0, record.TokenCostPerSuccess);
    }

    [Fact]
    public void MetricsScoreboard_Update_TracksClarificationAndLeakageProxy()
    {
        var path = Path.Combine(CreateTempDir(), "metrics.json");

        MetricsScoreboard.Update(path, new MetricsUpdate
        {
            Command = "init",
            ExitCode = 0,
            TokensRequested = 10
        });

        var clarify = MetricsScoreboard.Update(path, new MetricsUpdate
        {
            Command = "validate",
            ExitCode = 6,
            TokensRequested = 5
        });
        Assert.Equal(1, clarify.ClarificationRuns);
        Assert.Equal(1, clarify.DefectLeakageIncidents);

        var blocked = MetricsScoreboard.Update(path, new MetricsUpdate
        {
            Command = "validate",
            ExitCode = 9,
            TokensRequested = -100
        });
        Assert.Equal(2, blocked.ClarificationRuns);
        Assert.True(blocked.TotalTokensRequested >= 0);
        Assert.True(blocked.ReworkRate > 0);
        Assert.True(blocked.TimeToAcceptedSolution >= 0);
    }

    [Fact]
    public void RunResultWriter_Write_PersistsStructuredJson()
    {
        var output = Path.Combine(CreateTempDir(), "artifacts", "run-result.json");
        var record = new RunResultRecord
        {
            Command = "init",
            ExitCode = 0,
            Success = true,
            Summary = "ok",
            Plan = { "stage: completed" }
        };
        record.Artifacts["decision"] = "/tmp/decision.json";
        record.Autonomous = new AutonomousRunDetails
        {
            DecisionLog = { "check: passed" },
            RiskLog = { "riskLevel=low" },
            RollbackNotes = { "git revert <sha>" }
        };

        RunResultWriter.Write(output, record);

        Assert.True(File.Exists(output));
        using var doc = JsonDocument.Parse(File.ReadAllText(output));
        Assert.Equal("init", doc.RootElement.GetProperty("command").GetString());
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("ok", doc.RootElement.GetProperty("summary").GetString());
        Assert.True(doc.RootElement.GetProperty("autonomous").TryGetProperty("decisionLog", out _));
    }

    private static string CreateTempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), "meta-agent-tests", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(path);
        return path;
    }
}
