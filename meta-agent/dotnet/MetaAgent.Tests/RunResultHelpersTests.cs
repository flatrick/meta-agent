using System;
using System.IO;
using MetaAgent.Core;
using Xunit;

public class RunResultHelpersTests
{
    [Fact]
    public void ArtifactPaths_Resolve_UsesInitTargetAndOverrides()
    {
        var tempDir = CreateTempDir();
        var targetDir = Path.Combine(tempDir, "target");
        var args = new[]
        {
            "init",
            "--target", targetDir,
            "--decision-record", Path.Combine(tempDir, "d.json"),
            "--workflow-record", Path.Combine(tempDir, "w.json"),
            "--triage-output", Path.Combine(tempDir, "t.json"),
            "--run-result", Path.Combine(tempDir, "r.json"),
            "--metrics-scoreboard", Path.Combine(tempDir, "m.json")
        };

        var resolved = RunResultArtifactPaths.Resolve("init", args);

        Assert.Equal(Path.GetFullPath(targetDir), resolved.ArtifactsDirectory);
        Assert.Equal(Path.GetFullPath(Path.Combine(tempDir, "d.json")), resolved.DecisionPath);
        Assert.Equal(Path.GetFullPath(Path.Combine(tempDir, "w.json")), resolved.WorkflowPath);
        Assert.Equal(Path.GetFullPath(Path.Combine(tempDir, "t.json")), resolved.TriagePath);
        Assert.Equal(Path.GetFullPath(Path.Combine(tempDir, "r.json")), resolved.RunResultPath);
        Assert.Equal(Path.GetFullPath(Path.Combine(tempDir, "m.json")), resolved.MetricsPath);
    }

    [Fact]
    public void RunResultJson_ReadString_ReturnsNull_ForMissingAndInvalidJson()
    {
        var tempDir = CreateTempDir();
        var missingPath = Path.Combine(tempDir, "missing.json");
        Assert.Null(RunResultJson.ReadString(missingPath, "mode"));

        var invalidPath = Path.Combine(tempDir, "invalid.json");
        File.WriteAllText(invalidPath, "{ invalid json");
        Assert.Null(RunResultJson.ReadString(invalidPath, "mode"));
    }

    [Fact]
    public void ExtractTokensRequested_ReadsBudgetTokensCheck()
    {
        var tempDir = CreateTempDir();
        var decisionPath = Path.Combine(tempDir, "decision.json");
        File.WriteAllText(decisionPath, """
{
  "checks": [
    { "check": "budget_tokens", "passed": true, "detail": "requested=42, used=0, limit=1000" }
  ]
}
""");

        var requested = RunResultSectionsBuilder.ExtractTokensRequested(decisionPath);
        Assert.Equal(42, requested);
    }

    [Fact]
    public void BuildPlan_ParsesWorkflowStages()
    {
        var tempDir = CreateTempDir();
        var workflowPath = Path.Combine(tempDir, "workflow.json");
        File.WriteAllText(workflowPath, """
{
  "stages": [
    { "stage": "understand", "status": "completed" },
    { "stage": "validate", "status": "completed" }
  ]
}
""");

        var plan = RunResultSectionsBuilder.BuildPlan(workflowPath);
        Assert.Contains("understand: completed", plan);
        Assert.Contains("validate: completed", plan);
    }

    private static string CreateTempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), "meta-agent-tests", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(path);
        return path;
    }
}
