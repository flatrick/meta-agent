using System;
using System.IO;
using MetaAgent.Core;
using Xunit;

public class RunResultSectionsBuilderTests
{
    [Fact]
    public void BuildAssumptions_CoversMissingAndPresentArtifacts()
    {
        var tempDir = CreateTempDir();
        var missingDecision = Path.Combine(tempDir, "missing-decision.json");
        var missingTriage = Path.Combine(tempDir, "missing-triage.json");

        var assumptionsMissing = RunResultSectionsBuilder.BuildAssumptions("init", missingDecision, missingTriage);
        Assert.Contains(assumptionsMissing, x => x.Contains("No decision record", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(assumptionsMissing, x => x.Contains("No triage input", StringComparison.OrdinalIgnoreCase));

        var decisionPath = Path.Combine(tempDir, "decision.json");
        var triagePath = Path.Combine(tempDir, "triage.json");
        File.WriteAllText(decisionPath, """{ "checks": [] }""");
        File.WriteAllText(triagePath, """{ "riskLevel": "low" }""");

        var assumptionsPresent = RunResultSectionsBuilder.BuildAssumptions("init", decisionPath, triagePath);
        Assert.Single(assumptionsPresent);
        Assert.Contains("available and used", assumptionsPresent[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildExtractedRequirements_AndPlan_AndImplementation_Work()
    {
        var tempDir = CreateTempDir();
        var triagePath = Path.Combine(tempDir, "triage.json");
        var workflowPath = Path.Combine(tempDir, "workflow.json");

        File.WriteAllText(triagePath, """
{
  "definitionOfDone": [ "add tests", "update docs", "" ]
}
""");
        File.WriteAllText(workflowPath, """
{
  "stages": [
    { "stage": "Understand", "status": "completed" },
    { "stage": "Plan", "status": "completed" }
  ]
}
""");

        var requirements = RunResultSectionsBuilder.BuildExtractedRequirements(triagePath);
        Assert.Equal(2, requirements.Count);

        var plan = RunResultSectionsBuilder.BuildPlan(workflowPath);
        Assert.Contains("Understand: completed", plan);
        Assert.Contains("Plan: completed", plan);

        var implementation = RunResultSectionsBuilder.BuildImplementation("validate", new[] { "validate", "--policy", "x.json" });
        Assert.Contains("Executed command: validate", implementation);
        Assert.Contains("Arguments: validate --policy x.json", implementation);
    }

    [Fact]
    public void BuildValidationEvidence_HandlesChecksAndValidationPlan()
    {
        var tempDir = CreateTempDir();
        var decisionPath = Path.Combine(tempDir, "decision.json");
        var triagePath = Path.Combine(tempDir, "triage.json");

        File.WriteAllText(decisionPath, """
{
  "checks": [
    { "check": "autonomy_gate", "passed": true, "detail": "ok" },
    { "check": "budget_tokens", "passed": false, "detail": "requested=10, used=0, limit=5" }
  ]
}
""");
        File.WriteAllText(triagePath, """
{
  "validationPlan": [
    { "method": "integration_tests", "decision": "chosen" },
    { "method": "manual_validation_steps", "decision": "skipped" }
  ]
}
""");

        var evidence = RunResultSectionsBuilder.BuildValidationEvidence(decisionPath, triagePath);
        Assert.Contains(evidence, x => x.Contains("autonomy_gate", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(evidence, x => x.Contains("budget_tokens", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(evidence, x => x.Contains("validation method chosen: integration_tests", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BuildMetricsImpact_NextActions_DecisionLog_RiskLog_WorkForCommonCases()
    {
        var tempDir = CreateTempDir();
        var decisionPath = Path.Combine(tempDir, "decision.json");
        var triagePath = Path.Combine(tempDir, "triage.json");
        var workflowPath = Path.Combine(tempDir, "workflow.json");
        var metricsPath = Path.Combine(tempDir, "metrics.json");

        File.WriteAllText(decisionPath, """
{
  "checks": [
    { "check": "budget_tokens", "passed": true, "detail": "requested=42, used=0, limit=1000" },
    { "check": "budget_tickets", "passed": true, "detail": "requested=1, used=0, limit=10" }
  ]
}
""");
        File.WriteAllText(triagePath, """
{
  "riskLevel": "medium",
  "riskScore": 2,
  "changeSafetyLevel": 2
}
""");
        File.WriteAllText(workflowPath, """{ "stages": [] }""");

        var metrics = new MetricsScoreboardRecord
        {
            SuccessRate = 0.5,
            ReworkRate = 0.3,
            ClarificationRate = 0.2,
            TokenCostPerSuccess = 12.34,
            DefectLeakageIncidents = 1,
            TimeToAcceptedSolution = 2.0
        };

        var impact = RunResultSectionsBuilder.BuildMetricsImpact(decisionPath, metricsPath, metrics);
        Assert.Contains(impact, x => x.Contains("budget_tokens", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(impact, x => x.Contains("metrics_scoreboard:", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(impact, x => x.Contains("token_cost_per_success=12.34", StringComparison.OrdinalIgnoreCase));

        var successActions = RunResultSectionsBuilder.BuildNextActions(0, decisionPath, workflowPath, triagePath);
        Assert.Contains(successActions, x => x.Contains("Proceed with implementation", StringComparison.OrdinalIgnoreCase));

        var failActions = RunResultSectionsBuilder.BuildNextActions(5, decisionPath, workflowPath, triagePath);
        Assert.Contains(failActions, x => x.Contains("Inspect decision record", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(failActions, x => x.Contains("Inspect workflow record", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(failActions, x => x.Contains("Inspect triage record", StringComparison.OrdinalIgnoreCase));

        var decisionLog = RunResultSectionsBuilder.BuildDecisionLog(decisionPath);
        Assert.Contains(decisionLog, x => x.Contains("budget_tokens", StringComparison.OrdinalIgnoreCase));

        var riskLog = RunResultSectionsBuilder.BuildRiskLog(triagePath);
        Assert.Contains(riskLog, x => x.Contains("riskLevel=medium", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RunResultSectionsBuilder_Fallbacks_HandleMissingArtifacts()
    {
        var tempDir = CreateTempDir();
        var missingDecision = Path.Combine(tempDir, "missing-decision.json");
        var missingTriage = Path.Combine(tempDir, "missing-triage.json");
        var missingWorkflow = Path.Combine(tempDir, "missing-workflow.json");

        Assert.Empty(RunResultSectionsBuilder.BuildExtractedRequirements(missingTriage));
        Assert.Empty(RunResultSectionsBuilder.BuildPlan(missingWorkflow));
        Assert.Equal(0, RunResultSectionsBuilder.ExtractTokensRequested(missingDecision));

        var validationEvidence = RunResultSectionsBuilder.BuildValidationEvidence(missingDecision, missingTriage);
        Assert.Single(validationEvidence);
        Assert.Contains("No explicit validation evidence", validationEvidence[0], StringComparison.OrdinalIgnoreCase);

        var decisionLog = RunResultSectionsBuilder.BuildDecisionLog(missingDecision);
        Assert.Single(decisionLog);
        Assert.Contains("unavailable", decisionLog[0], StringComparison.OrdinalIgnoreCase);

        var riskLog = RunResultSectionsBuilder.BuildRiskLog(missingTriage);
        Assert.Single(riskLog);
        Assert.Contains("No triage artifact", riskLog[0], StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateTempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), "meta-agent-tests", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(path);
        return path;
    }
}
