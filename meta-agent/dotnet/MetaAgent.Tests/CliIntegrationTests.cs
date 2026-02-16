using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using MetaAgent.Core;
using Xunit;

[Collection("CliProcessCollection")]
public class CliIntegrationTests
{
    [Fact]
    public void Init_Allowed_WritesAllowedDecisionRecord()
    {
        var repoRoot = FindRepoRoot();
        var targetDir = CreateTempDir();
        var decisionPath = Path.Combine(targetDir, "decision-init-allowed.json");

        var result = RunCli(repoRoot,
            "init",
            "--target", targetDir,
            "--requested-autonomy", "A1",
            "--tokens-requested", "50",
            "--tickets-requested", "1",
            "--open-prs", "0",
            "--decision-record", decisionPath);

        Assert.True(
            result.ExitCode == 0,
            $"Expected exit code 0 but got {result.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{result.StdOut}{Environment.NewLine}stderr:{Environment.NewLine}{result.StdErr}");
        Assert.True(File.Exists(decisionPath), "expected decision record to be written");
        AssertDecisionAllowed(decisionPath, expectedAllowed: true);
    }

    [Fact]
    public void Init_InvalidTokensRequested_ReturnsUsageError()
    {
        var repoRoot = FindRepoRoot();
        var targetDir = CreateTempDir();

        var result = RunCli(repoRoot,
            "init",
            "--target", targetDir,
            "--tokens-requested", "-1");

        Assert.Equal(2, result.ExitCode);
    }

    [Fact]
    public void Init_WithOutputDirectory_WritesArtifactsToOutputDirectory()
    {
        var repoRoot = FindRepoRoot();
        var targetDir = CreateTempDir();
        var outputDir = Path.Combine(targetDir, "artifacts");

        var result = RunCli(repoRoot,
            "init",
            "--target", targetDir,
            "--output", outputDir,
            "--requested-autonomy", "A1",
            "--tokens-requested", "10",
            "--tickets-requested", "1",
            "--open-prs", "0");

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(Path.Combine(outputDir, ".meta-agent-decision.json")));
        Assert.True(File.Exists(Path.Combine(outputDir, ".meta-agent-workflow.json")));
        Assert.True(File.Exists(Path.Combine(outputDir, ".meta-agent-run-result.json")));
        Assert.True(File.Exists(Path.Combine(outputDir, ".meta-agent-metrics.json")));
    }

    [Fact]
    public void Init_CanCustomizeAdrIdPrefix_ForStructurizrAdrFiles()
    {
        var repoRoot = FindRepoRoot();
        var layout = TemplateLayout.LoadFromRepositoryOrDefault(repoRoot);
        var targetDir = CreateTempDir();

        var result = RunCli(repoRoot,
            "init",
            "--template", "dotnet",
            "--target", targetDir,
            "--name", "demo-service",
            "--adr-id-prefix", "PLATFORM-1234",
            "--requested-autonomy", "A1",
            "--tokens-requested", "10",
            "--tickets-requested", "1",
            "--open-prs", "0");

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(CombineRelative(targetDir, $"{layout.AdrsRoot}/PLATFORM-1234-ai-governance.md")));
        Assert.False(File.Exists(CombineRelative(targetDir, $"{layout.AdrsRoot}/0001-ai-governance.md")));
    }

    [Fact]
    public void Init_UsesJiraLikeTicketKey_AsAdrPrefix_WhenExplicitPrefixIsOmitted()
    {
        var repoRoot = FindRepoRoot();
        var layout = TemplateLayout.LoadFromRepositoryOrDefault(repoRoot);
        var targetDir = CreateTempDir();

        var result = RunCli(repoRoot,
            "init",
            "--template", "dotnet",
            "--target", targetDir,
            "--name", "demo-service",
            "--ticket", "Implement acceptance criteria for PLATFORM-5678 and update docs",
            "--requested-autonomy", "A1",
            "--tokens-requested", "10",
            "--tickets-requested", "1",
            "--open-prs", "0");

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(CombineRelative(targetDir, $"{layout.AdrsRoot}/PLATFORM-5678-ai-governance.md")));
        Assert.False(File.Exists(CombineRelative(targetDir, $"{layout.AdrsRoot}/0001-ai-governance.md")));
    }

    [Fact]
    public void Init_ExplicitAdrPrefix_TakesPrecedence_OverTicketDerivedPrefix()
    {
        var repoRoot = FindRepoRoot();
        var layout = TemplateLayout.LoadFromRepositoryOrDefault(repoRoot);
        var targetDir = CreateTempDir();

        var result = RunCli(repoRoot,
            "init",
            "--template", "dotnet",
            "--target", targetDir,
            "--name", "demo-service",
            "--adr-id-prefix", "PLATFORM-9999",
            "--ticket", "Implement acceptance criteria for PLATFORM-5678 and update docs",
            "--requested-autonomy", "A1",
            "--tokens-requested", "10",
            "--tickets-requested", "1",
            "--open-prs", "0");

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(CombineRelative(targetDir, $"{layout.AdrsRoot}/PLATFORM-9999-ai-governance.md")));
        Assert.False(File.Exists(CombineRelative(targetDir, $"{layout.AdrsRoot}/PLATFORM-5678-ai-governance.md")));
    }

    [Fact]
    public void Configure_ExistingRepository_CreatesPolicyAndArtifacts_WithoutScaffolding()
    {
        var repoRoot = FindRepoRoot();
        var targetRepo = CreateTempDir();
        var existingFile = Path.Combine(targetRepo, "README.md");
        File.WriteAllText(existingFile, "existing-content");

        var result = RunCli(repoRoot,
            "configure",
            "--repo", targetRepo,
            "--requested-autonomy", "A1",
            "--tokens-requested", "5",
            "--tickets-requested", "1",
            "--open-prs", "0");

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(Path.Combine(targetRepo, ".meta-agent-policy.json")));
        Assert.True(File.Exists(Path.Combine(targetRepo, ".meta-agent-decision.json")));
        Assert.True(File.Exists(Path.Combine(targetRepo, ".meta-agent-workflow.json")));
        Assert.True(File.Exists(Path.Combine(targetRepo, ".meta-agent-run-result.json")));
        Assert.True(File.Exists(Path.Combine(targetRepo, ".meta-agent-metrics.json")));
        Assert.Equal("existing-content", File.ReadAllText(existingFile));
        Assert.False(File.Exists(Path.Combine(targetRepo, "Program.cs")));
    }

    [Fact]
    public void Configure_WithOutputDirectory_WritesArtifactsToOutputDirectory()
    {
        var repoRoot = FindRepoRoot();
        var targetRepo = CreateTempDir();
        var outputDir = Path.Combine(targetRepo, "artifacts");

        var result = RunCli(repoRoot,
            "configure",
            "--repo", targetRepo,
            "--output", outputDir,
            "--requested-autonomy", "A1",
            "--tokens-requested", "1",
            "--tickets-requested", "1",
            "--open-prs", "0");

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(Path.Combine(outputDir, ".meta-agent-decision.json")));
        Assert.True(File.Exists(Path.Combine(outputDir, ".meta-agent-workflow.json")));
        Assert.True(File.Exists(Path.Combine(outputDir, ".meta-agent-run-result.json")));
        Assert.True(File.Exists(Path.Combine(outputDir, ".meta-agent-metrics.json")));
    }

    [Fact]
    public void Configure_Blocks_WhenRequestedAutonomyIsA0()
    {
        var repoRoot = FindRepoRoot();
        var targetRepo = CreateTempDir();
        var decisionPath = Path.Combine(targetRepo, "decision-configure-a0-blocked.json");

        var result = RunCli(repoRoot,
            "configure",
            "--repo", targetRepo,
            "--requested-autonomy", "A0",
            "--decision-record", decisionPath);

        Assert.Equal(7, result.ExitCode);
        Assert.True(File.Exists(decisionPath), "expected decision record to be written");
        using var doc = JsonDocument.Parse(File.ReadAllText(decisionPath));
        Assert.False(doc.RootElement.GetProperty("allowed").GetBoolean());
        Assert.Equal("A0", doc.RootElement.GetProperty("requestedAutonomy").GetString());
    }

    [Fact]
    public void Configure_Blocks_InAutonomousTicketRunner_WhenTicketMissing()
    {
        var repoRoot = FindRepoRoot();
        var targetRepo = CreateTempDir();
        var decisionPath = Path.Combine(targetRepo, "decision-configure-ticket-missing.json");

        var result = RunCli(repoRoot,
            "configure",
            "--repo", targetRepo,
            "--mode", "autonomous_ticket_runner",
            "--requested-autonomy", "A2",
            "--decision-record", decisionPath);

        Assert.Equal(9, result.ExitCode);
        Assert.True(File.Exists(decisionPath), "expected decision record to be written");
        using var doc = JsonDocument.Parse(File.ReadAllText(decisionPath));
        Assert.False(doc.RootElement.GetProperty("allowed").GetBoolean());
    }

    [Fact]
    public void Configure_InteractiveHighCost_BlocksWithoutApproval_AndAllowsWithApproval()
    {
        var repoRoot = FindRepoRoot();
        var targetRepo = CreateTempDir();
        var policyPath = Path.Combine(targetRepo, ".meta-agent-policy.json");
        var blockedDecisionPath = Path.Combine(targetRepo, "decision-configure-high-cost-blocked.json");
        File.WriteAllText(policyPath, """
{
  "name": "policy",
  "defaultMode": "interactive_ide",
  "autonomyDefault": "A1",
  "commandGating": "all_commands",
  "budgets": {
    "tokensPerDay": 100000,
    "ticketsPerDay": 10,
    "maxConcurrentPrs": 3
  },
  "tokenGovernance": {
    "interactiveIde": {
      "warningTokensPerRun": 50,
      "requireOperatorApproval": true
    }
  },
  "changeBoundaries": {
    "allowedPaths": ["**"],
    "disallowedPaths": []
  },
  "abortConditions": []
}
""");

        var blocked = RunCli(repoRoot,
            "configure",
            "--repo", targetRepo,
            "--policy", policyPath,
            "--mode", "interactive_ide",
            "--requested-autonomy", "A1",
            "--tokens-requested", "100",
            "--decision-record", blockedDecisionPath);
        Assert.Equal(5, blocked.ExitCode);
        using (var blockedDoc = JsonDocument.Parse(File.ReadAllText(blockedDecisionPath)))
        {
            Assert.False(blockedDoc.RootElement.GetProperty("allowed").GetBoolean());
            Assert.Equal("interactive_soft_warning", blockedDoc.RootElement.GetProperty("budgetProfile").GetString());
        }

        var allowed = RunCli(repoRoot,
            "configure",
            "--repo", targetRepo,
            "--policy", policyPath,
            "--mode", "interactive_ide",
            "--requested-autonomy", "A1",
            "--tokens-requested", "100",
            "--operator-approved-high-cost");
        Assert.Equal(0, allowed.ExitCode);
    }

    [Fact]
    public void Init_BlockedByAutonomy_WritesBlockedDecisionRecord()
    {
        var repoRoot = FindRepoRoot();
        var targetDir = CreateTempDir();
        var decisionPath = Path.Combine(targetDir, "decision-init-blocked.json");

        var result = RunCli(repoRoot,
            "init",
            "--target", targetDir,
            "--requested-autonomy", "A3",
            "--decision-record", decisionPath);

        Assert.Equal(5, result.ExitCode);
        Assert.True(File.Exists(decisionPath), "expected decision record to be written");
        AssertDecisionAllowed(decisionPath, expectedAllowed: false);
    }

    [Fact]
    public void Init_AutonomousTokenHardCap_BlocksWithStrictProfile()
    {
        var repoRoot = FindRepoRoot();
        var targetDir = CreateTempDir();
        var policyPath = Path.Combine(targetDir, ".meta-agent-policy.json");
        var decisionPath = Path.Combine(targetDir, "decision-init-token-profile-blocked.json");
        File.WriteAllText(policyPath, """
{
  "name": "policy",
  "defaultMode": "interactive_ide",
  "autonomyDefault": "A2",
  "commandGating": "all_commands",
  "budgets": {
    "tokensPerDay": 100000,
    "ticketsPerDay": 10,
    "maxConcurrentPrs": 3
  },
  "tokenGovernance": {
    "autonomousTicketRunner": {
      "hardCapTokensPerRun": 100
    }
  },
  "changeBoundaries": {
    "allowedPaths": ["**"],
    "disallowedPaths": []
  },
  "abortConditions": []
}
""");

        var result = RunCli(repoRoot,
            "init",
            "--target", targetDir,
            "--policy", policyPath,
            "--mode", "autonomous_ticket_runner",
            "--requested-autonomy", "A2",
            "--ticket", "Update docs and triage metadata",
            "--tokens-requested", "101",
            "--decision-record", decisionPath);

        Assert.Equal(5, result.ExitCode);
        using var doc = JsonDocument.Parse(File.ReadAllText(decisionPath));
        Assert.False(doc.RootElement.GetProperty("allowed").GetBoolean());
        Assert.Equal("autonomous_strict_hard_cap", doc.RootElement.GetProperty("budgetProfile").GetString());
    }

    [Fact]
    public void Init_BlockedByAmbiguityWithoutOperatorApproval_WritesWorkflowRecord()
    {
        var repoRoot = FindRepoRoot();
        var targetDir = CreateTempDir();
        var workflowPath = Path.Combine(targetDir, "workflow-blocked.json");

        var result = RunCli(repoRoot,
            "init",
            "--target", targetDir,
            "--ambiguity-score", "0.95",
            "--workflow-record", workflowPath);

        Assert.Equal(6, result.ExitCode);
        Assert.True(File.Exists(workflowPath), "expected workflow record to be written");
        using var doc = JsonDocument.Parse(File.ReadAllText(workflowPath));
        Assert.False(doc.RootElement.GetProperty("canProceed").GetBoolean());
    }

    [Fact]
    public void Init_AllowsHighAmbiguity_WhenOperatorApprovalFlagProvided()
    {
        var repoRoot = FindRepoRoot();
        var targetDir = CreateTempDir();
        var workflowPath = Path.Combine(targetDir, "workflow-approved.json");

        var result = RunCli(repoRoot,
            "init",
            "--target", targetDir,
            "--ambiguity-score", "0.95",
            "--operator-approved-ambiguity",
            "--workflow-record", workflowPath);

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(workflowPath), "expected workflow record to be written");
        using var doc = JsonDocument.Parse(File.ReadAllText(workflowPath));
        Assert.True(doc.RootElement.GetProperty("canProceed").GetBoolean());
    }

    [Fact]
    public void Init_Blocks_WhenRequestedAutonomyIsA0_ForMutatingCommand()
    {
        var repoRoot = FindRepoRoot();
        var targetDir = CreateTempDir();
        var decisionPath = Path.Combine(targetDir, "decision-init-a0-blocked.json");

        var result = RunCli(repoRoot,
            "init",
            "--target", targetDir,
            "--requested-autonomy", "A0",
            "--decision-record", decisionPath);

        Assert.Equal(7, result.ExitCode);
        Assert.True(File.Exists(decisionPath), "expected decision record to be written");
        using var doc = JsonDocument.Parse(File.ReadAllText(decisionPath));
        Assert.False(doc.RootElement.GetProperty("allowed").GetBoolean());
        Assert.Equal("A0", doc.RootElement.GetProperty("requestedAutonomy").GetString());
    }

    [Fact]
    public void Init_InteractiveMode_ForcedNonInteractiveSession_DoesNotBlockOnPlanPrompt()
    {
        var repoRoot = FindRepoRoot();
        var targetDir = CreateTempDir();
        var workflowPath = Path.Combine(targetDir, "workflow-init-plan-blocked.json");

        // Force non-interactive execution via META_AGENT_NONINTERACTIVE=1 to keep this test deterministic.
        var result = RunCli(repoRoot,
            "init",
            "--target", targetDir,
            "--mode", "interactive_ide",
            "--workflow-record", workflowPath);

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(workflowPath), "expected workflow record to be written");
        using var doc = JsonDocument.Parse(File.ReadAllText(workflowPath));
        Assert.True(doc.RootElement.GetProperty("canProceed").GetBoolean());
        Assert.True(doc.RootElement.GetProperty("requiresPlanApproval").GetBoolean());
        Assert.True(doc.RootElement.GetProperty("operatorApprovedPlan").GetBoolean());
    }

    [Fact]
    public void Init_InteractiveMode_Allows_WhenPlanApproved()
    {
        var repoRoot = FindRepoRoot();
        var targetDir = CreateTempDir();
        var workflowPath = Path.Combine(targetDir, "workflow-init-plan-approved.json");

        var result = RunCliInteractive(repoRoot,
            "init",
            "--target", targetDir,
            "--mode", "interactive_ide",
            "--operator-approved-plan",
            "--workflow-record", workflowPath);

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(workflowPath), "expected workflow record to be written");
        using var doc = JsonDocument.Parse(File.ReadAllText(workflowPath));
        Assert.True(doc.RootElement.GetProperty("canProceed").GetBoolean());
        Assert.True(doc.RootElement.GetProperty("operatorApprovedPlan").GetBoolean());
    }

    [Fact]
    public void Init_Blocks_InAutonomousTicketRunner_WhenAutonomyBelowA2()
    {
        var repoRoot = FindRepoRoot();
        var targetDir = CreateTempDir();
        var decisionPath = Path.Combine(targetDir, "decision-init-mode-blocked.json");

        var result = RunCli(repoRoot,
            "init",
            "--target", targetDir,
            "--mode", "autonomous_ticket_runner",
            "--requested-autonomy", "A1",
            "--decision-record", decisionPath);

        Assert.Equal(7, result.ExitCode);
        Assert.True(File.Exists(decisionPath), "expected decision record to be written");
        using var doc = JsonDocument.Parse(File.ReadAllText(decisionPath));
        Assert.False(doc.RootElement.GetProperty("allowed").GetBoolean());
        Assert.Equal("autonomous_ticket_runner", doc.RootElement.GetProperty("mode").GetString());
    }

    [Fact]
    public void Init_Blocks_InAutonomousTicketRunner_WhenTicketMissing()
    {
        var repoRoot = FindRepoRoot();
        var targetDir = CreateTempDir();
        var decisionPath = Path.Combine(targetDir, "decision-init-ticket-missing.json");

        var result = RunCli(repoRoot,
            "init",
            "--target", targetDir,
            "--mode", "autonomous_ticket_runner",
            "--requested-autonomy", "A2",
            "--decision-record", decisionPath);

        Assert.Equal(9, result.ExitCode);
        Assert.True(File.Exists(decisionPath), "expected decision record to be written");
        using var doc = JsonDocument.Parse(File.ReadAllText(decisionPath));
        Assert.False(doc.RootElement.GetProperty("allowed").GetBoolean());
    }

    [Fact]
    public void Validate_Allowed_WritesAllowedDecisionRecord()
    {
        var repoRoot = FindRepoRoot();
        var tempDir = CreateTempDir();
        var policyPath = Path.Combine(tempDir, ".meta-agent-policy.json");
        var decisionPath = Path.Combine(tempDir, "decision-validate-allowed.json");
        var workflowPath = Path.Combine(tempDir, "workflow-validate-allowed.json");
        File.WriteAllText(policyPath, """
{
  "name": "policy",
  "defaultMode": "interactive_ide",
  "autonomyDefault": "A1",
  "commandGating": "all_commands",
  "budgets": {
    "tokensPerDay": 1000,
    "ticketsPerDay": 10,
    "maxConcurrentPrs": 3
  },
  "changeBoundaries": {
    "allowedPaths": ["**"],
    "disallowedPaths": []
  },
  "abortConditions": ["ci_failing_repeatedly"]
}
""");

        var result = RunCli(repoRoot,
            "validate",
            "--policy", policyPath,
            "--workflow-record", workflowPath,
            "--decision-record", decisionPath);

        Assert.True(
            result.ExitCode == 0,
            $"Expected exit code 0 but got {result.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{result.StdOut}{Environment.NewLine}stderr:{Environment.NewLine}{result.StdErr}");
        Assert.True(File.Exists(decisionPath), "expected decision record to be written");
        Assert.True(File.Exists(workflowPath), "expected workflow record to be written");
        AssertDecisionAllowed(decisionPath, expectedAllowed: true);
    }

    [Fact]
    public void Validate_InvalidAmbiguityScore_ReturnsUsageError()
    {
        var repoRoot = FindRepoRoot();
        var tempDir = CreateTempDir();
        var policyPath = Path.Combine(tempDir, ".meta-agent-policy.json");
        File.WriteAllText(policyPath, """
{
  "name": "policy",
  "defaultMode": "interactive_ide",
  "autonomyDefault": "A1",
  "commandGating": "all_commands",
  "budgets": {
    "tokensPerDay": 1000,
    "ticketsPerDay": 10,
    "maxConcurrentPrs": 3
  },
  "changeBoundaries": {
    "allowedPaths": ["**"],
    "disallowedPaths": []
  },
  "abortConditions": []
}
""");

        var result = RunCli(repoRoot,
            "validate",
            "--policy", policyPath,
            "--ambiguity-score", "1.5");

        Assert.Equal(2, result.ExitCode);
    }

    [Fact]
    public void Validate_WithOutputDirectory_WritesArtifactsToOutputDirectory()
    {
        var repoRoot = FindRepoRoot();
        var tempDir = CreateTempDir();
        var outputDir = Path.Combine(tempDir, "artifacts");
        var policyPath = Path.Combine(tempDir, ".meta-agent-policy.json");
        File.WriteAllText(policyPath, """
{
  "name": "policy",
  "defaultMode": "interactive_ide",
  "autonomyDefault": "A1",
  "commandGating": "all_commands",
  "budgets": {
    "tokensPerDay": 1000,
    "ticketsPerDay": 10,
    "maxConcurrentPrs": 3
  },
  "changeBoundaries": {
    "allowedPaths": ["**"],
    "disallowedPaths": []
  },
  "abortConditions": []
}
""");

        var result = RunCli(repoRoot,
            "validate",
            "--policy", policyPath,
            "--output", outputDir);

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(Path.Combine(outputDir, ".meta-agent-decision.json")));
        Assert.True(File.Exists(Path.Combine(outputDir, ".meta-agent-workflow.json")));
        Assert.True(File.Exists(Path.Combine(outputDir, ".meta-agent-run-result.json")));
        Assert.True(File.Exists(Path.Combine(outputDir, ".meta-agent-metrics.json")));
    }

    [Fact]
    public void Version_WithOutputDirectory_WritesDecisionRecordToOutputDirectory()
    {
        var repoRoot = FindRepoRoot();
        var tempDir = CreateTempDir();
        var outputDir = Path.Combine(tempDir, "artifacts");

        var result = RunCli(repoRoot,
            "version",
            "--output", outputDir);

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(Path.Combine(outputDir, ".meta-agent-decision.json")));
    }

    [Fact]
    public void Triage_WithOutputFile_WritesDecisionRecordBesideOutputFile()
    {
        var repoRoot = FindRepoRoot();
        var tempDir = CreateTempDir();
        var outputPath = Path.Combine(tempDir, "results", "triage.json");

        var result = RunCli(repoRoot,
            "triage",
            "--ticket", "Acceptance Criteria: Add tests and docs updates for artifact output consistency",
            "--output", outputPath);

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(outputPath));
        Assert.True(File.Exists(Path.Combine(Path.GetDirectoryName(outputPath)!, ".meta-agent-decision.json")));
        Assert.True(File.Exists(Path.Combine(Path.GetDirectoryName(outputPath)!, ".meta-agent-run-result.json")));
        Assert.True(File.Exists(Path.Combine(Path.GetDirectoryName(outputPath)!, ".meta-agent-metrics.json")));
    }

    [Fact]
    public void Validate_BlockedByAbortSignal_WritesBlockedDecisionRecord()
    {
        var repoRoot = FindRepoRoot();
        var tempDir = CreateTempDir();
        var policyPath = Path.Combine(tempDir, ".meta-agent-policy.json");
        var decisionPath = Path.Combine(tempDir, "decision-validate-blocked.json");
        var workflowPath = Path.Combine(tempDir, "workflow-validate-blocked-abort.json");
        File.WriteAllText(policyPath, """
{
  "name": "policy",
  "defaultMode": "interactive_ide",
  "autonomyDefault": "A1",
  "commandGating": "all_commands",
  "budgets": {
    "tokensPerDay": 1000,
    "ticketsPerDay": 10,
    "maxConcurrentPrs": 3
  },
  "changeBoundaries": {
    "allowedPaths": ["**"],
    "disallowedPaths": []
  },
  "abortConditions": ["tests_flaky"]
}
""");

        var result = RunCli(repoRoot,
            "validate",
            "--policy", policyPath,
            "--abort-signal", "tests_flaky",
            "--workflow-record", workflowPath,
            "--decision-record", decisionPath);

        Assert.Equal(5, result.ExitCode);
        Assert.True(File.Exists(decisionPath), "expected decision record to be written");
        Assert.True(File.Exists(workflowPath), "expected workflow record to be written");
        AssertDecisionAllowed(decisionPath, expectedAllowed: false);
    }

    [Fact]
    public void Validate_BlockedByAmbiguityWithoutOperatorApproval_WritesWorkflowRecord()
    {
        var repoRoot = FindRepoRoot();
        var tempDir = CreateTempDir();
        var policyPath = Path.Combine(tempDir, ".meta-agent-policy.json");
        var workflowPath = Path.Combine(tempDir, "workflow-validate-blocked.json");
        File.WriteAllText(policyPath, """
{
  "name": "policy",
  "defaultMode": "interactive_ide",
  "autonomyDefault": "A1",
  "ambiguityThreshold": 0.5,
  "commandGating": "all_commands",
  "budgets": {
    "tokensPerDay": 1000,
    "ticketsPerDay": 10,
    "maxConcurrentPrs": 3
  },
  "changeBoundaries": {
    "allowedPaths": ["**"],
    "disallowedPaths": []
  },
  "abortConditions": []
}
""");

        var result = RunCli(repoRoot,
            "validate",
            "--policy", policyPath,
            "--ambiguity-score", "0.95",
            "--workflow-record", workflowPath);

        Assert.Equal(6, result.ExitCode);
        Assert.True(File.Exists(workflowPath), "expected workflow record to be written");
        using var doc = JsonDocument.Parse(File.ReadAllText(workflowPath));
        Assert.False(doc.RootElement.GetProperty("canProceed").GetBoolean());
        Assert.Equal("validate", doc.RootElement.GetProperty("command").GetString());
    }

    [Fact]
    public void Validate_AllowsHighAmbiguity_WhenOperatorApprovalFlagProvided()
    {
        var repoRoot = FindRepoRoot();
        var tempDir = CreateTempDir();
        var policyPath = Path.Combine(tempDir, ".meta-agent-policy.json");
        var workflowPath = Path.Combine(tempDir, "workflow-validate-approved.json");
        File.WriteAllText(policyPath, """
{
  "name": "policy",
  "defaultMode": "interactive_ide",
  "autonomyDefault": "A1",
  "ambiguityThreshold": 0.5,
  "commandGating": "all_commands",
  "budgets": {
    "tokensPerDay": 1000,
    "ticketsPerDay": 10,
    "maxConcurrentPrs": 3
  },
  "changeBoundaries": {
    "allowedPaths": ["**"],
    "disallowedPaths": []
  },
  "abortConditions": []
}
""");

        var result = RunCli(repoRoot,
            "validate",
            "--policy", policyPath,
            "--ambiguity-score", "0.95",
            "--operator-approved-ambiguity",
            "--workflow-record", workflowPath);

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(workflowPath), "expected workflow record to be written");
        using var doc = JsonDocument.Parse(File.ReadAllText(workflowPath));
        Assert.True(doc.RootElement.GetProperty("canProceed").GetBoolean());
        Assert.Equal("validate", doc.RootElement.GetProperty("command").GetString());
    }

    [Fact]
    public void Validate_Bypassed_WhenCommandGatingMutatingOnly()
    {
        var repoRoot = FindRepoRoot();
        var tempDir = CreateTempDir();
        var policyPath = Path.Combine(tempDir, ".meta-agent-policy.json");
        var decisionPath = Path.Combine(tempDir, "decision-validate-bypass.json");
        var workflowPath = Path.Combine(tempDir, "workflow-validate-bypass.json");
        File.WriteAllText(policyPath, """
{
  "name": "policy",
  "defaultMode": "interactive_ide",
  "autonomyDefault": "A1",
  "commandGating": "mutating_only",
  "budgets": {
    "tokensPerDay": 1000,
    "ticketsPerDay": 10,
    "maxConcurrentPrs": 3
  },
  "changeBoundaries": {
    "allowedPaths": ["**"],
    "disallowedPaths": []
  },
  "abortConditions": ["tests_flaky"]
}
""");

        var result = RunCli(repoRoot,
            "validate",
            "--policy", policyPath,
            "--abort-signal", "tests_flaky",
            "--workflow-record", workflowPath,
            "--decision-record", decisionPath);

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(decisionPath), "expected decision record to be written");
        Assert.True(File.Exists(workflowPath), "expected workflow record to be written");
        using var doc = JsonDocument.Parse(File.ReadAllText(decisionPath));
        Assert.True(doc.RootElement.GetProperty("allowed").GetBoolean());
        var checks = doc.RootElement.GetProperty("checks");
        Assert.Equal("command_gating", checks[0].GetProperty("check").GetString());
    }

    [Fact]
    public void Validate_InAutonomousTicketRunner_IncludesTriageInDecisionRecord()
    {
        var repoRoot = FindRepoRoot();
        var tempDir = CreateTempDir();
        var policyPath = Path.Combine(tempDir, ".meta-agent-policy.json");
        var decisionPath = Path.Combine(tempDir, "decision-validate-triage.json");
        var workflowPath = Path.Combine(tempDir, "workflow-validate-triage.json");
        File.WriteAllText(policyPath, """
{
  "name": "policy",
  "defaultMode": "interactive_ide",
  "autonomyDefault": "A2",
  "commandGating": "all_commands",
  "budgets": {
    "tokensPerDay": 1000,
    "ticketsPerDay": 10,
    "maxConcurrentPrs": 3
  },
  "changeBoundaries": {
    "allowedPaths": ["**"],
    "disallowedPaths": []
  },
  "abortConditions": []
}
""");

        var result = RunCli(repoRoot,
            "validate",
            "--policy", policyPath,
            "--mode", "autonomous_ticket_runner",
            "--requested-autonomy", "A2",
            "--ticket", "Update docs only. - README refreshed",
            "--workflow-record", workflowPath,
            "--decision-record", decisionPath);

        Assert.Equal(0, result.ExitCode);
        using var doc = JsonDocument.Parse(File.ReadAllText(decisionPath));
        Assert.True(doc.RootElement.GetProperty("allowed").GetBoolean());
        Assert.True(doc.RootElement.TryGetProperty("triage", out var triage));
        Assert.True(triage.GetProperty("eligible").GetBoolean());
    }

    [Fact]
    public void Validate_UsesConfiguredTicketContextEnvVars_ForModeClassification()
    {
        var repoRoot = FindRepoRoot();
        var tempDir = CreateTempDir();
        var policyPath = Path.Combine(tempDir, ".meta-agent-policy.json");
        var decisionPath = Path.Combine(tempDir, "decision-validate-custom-ticket-context.json");
        File.WriteAllText(policyPath, """
{
  "name": "policy",
  "defaultMode": "interactive_ide",
  "autonomyDefault": "A2",
  "commandGating": "all_commands",
  "budgets": {
    "tokensPerDay": 1000,
    "ticketsPerDay": 10,
    "maxConcurrentPrs": 3
  },
  "integrations": {
    "ticketContextEnvVars": ["WORK_ITEM_ID"]
  },
  "changeBoundaries": {
    "allowedPaths": ["**"],
    "disallowedPaths": []
  },
  "abortConditions": []
}
""");

        var result = RunCliWithEnv(
            repoRoot,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["WORK_ITEM_ID"] = "DEVOPS-42"
            },
            "validate",
            "--policy", policyPath,
            "--ticket", "Add tests and docs for mode classification",
            "--decision-record", decisionPath);

        Assert.Equal(0, result.ExitCode);
        using var doc = JsonDocument.Parse(File.ReadAllText(decisionPath));
        Assert.Equal("autonomous_ticket_runner", doc.RootElement.GetProperty("mode").GetString());
    }

    [Fact]
    public void Validate_UsesDefaultGenericTicketContextEnvVars_WhenIntegrationsAreOmitted()
    {
        var repoRoot = FindRepoRoot();
        var tempDir = CreateTempDir();
        var policyPath = Path.Combine(tempDir, ".meta-agent-policy.json");
        var decisionPath = Path.Combine(tempDir, "decision-validate-default-ticket-context.json");
        File.WriteAllText(policyPath, """
{
  "name": "policy",
  "defaultMode": "hybrid",
  "autonomyDefault": "A2",
  "commandGating": "all_commands",
  "budgets": {
    "tokensPerDay": 1000,
    "ticketsPerDay": 10,
    "maxConcurrentPrs": 3
  },
  "changeBoundaries": {
    "allowedPaths": ["**"],
    "disallowedPaths": []
  },
  "abortConditions": []
}
""");

        var result = RunCliWithEnv(
            repoRoot,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["WORK_ITEM_ID"] = "PLAT-9001"
            },
            "validate",
            "--policy", policyPath,
            "--ticket", "Review CI provider adapter boundaries",
            "--decision-record", decisionPath);

        Assert.Equal(0, result.ExitCode);
        using var doc = JsonDocument.Parse(File.ReadAllText(decisionPath));
        Assert.Equal("autonomous_ticket_runner", doc.RootElement.GetProperty("mode").GetString());
    }

    [Fact]
    public void Init_WritesRunResult_WithStructuredSections()
    {
        var repoRoot = FindRepoRoot();
        var targetDir = CreateTempDir();
        var runResultPath = Path.Combine(targetDir, "run-result-init.json");

        var result = RunCli(repoRoot,
            "init",
            "--target", targetDir,
            "--requested-autonomy", "A1",
            "--run-result", runResultPath);

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(runResultPath), "expected run result to be written");
        using var doc = JsonDocument.Parse(File.ReadAllText(runResultPath));
        Assert.Equal("init", doc.RootElement.GetProperty("command").GetString());
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
        Assert.True(doc.RootElement.TryGetProperty("summary", out _));
        Assert.True(doc.RootElement.TryGetProperty("assumptions", out _));
        Assert.True(doc.RootElement.TryGetProperty("extractedRequirements", out _));
        Assert.True(doc.RootElement.TryGetProperty("riskLevel", out _));
        Assert.True(doc.RootElement.TryGetProperty("plan", out _));
        Assert.True(doc.RootElement.TryGetProperty("implementation", out _));
        Assert.True(doc.RootElement.TryGetProperty("validationEvidence", out _));
        Assert.True(doc.RootElement.TryGetProperty("documentationUpdates", out _));
        Assert.True(doc.RootElement.TryGetProperty("metricsImpact", out _));
        Assert.True(doc.RootElement.TryGetProperty("nextActions", out _));
        Assert.True(doc.RootElement.TryGetProperty("artifacts", out _));
    }

    [Fact]
    public void Validate_Blocked_WritesRunResult_WithFailureContext()
    {
        var repoRoot = FindRepoRoot();
        var tempDir = CreateTempDir();
        var policyPath = Path.Combine(tempDir, ".meta-agent-policy.json");
        var runResultPath = Path.Combine(tempDir, "run-result-validate-blocked.json");
        File.WriteAllText(policyPath, """
{
  "name": "policy",
  "defaultMode": "interactive_ide",
  "autonomyDefault": "A1",
  "commandGating": "all_commands",
  "budgets": {
    "tokensPerDay": 1000,
    "ticketsPerDay": 10,
    "maxConcurrentPrs": 3
  },
  "changeBoundaries": {
    "allowedPaths": ["**"],
    "disallowedPaths": []
  },
  "abortConditions": ["tests_flaky"]
}
""");

        var result = RunCli(repoRoot,
            "validate",
            "--policy", policyPath,
            "--abort-signal", "tests_flaky",
            "--run-result", runResultPath);

        Assert.Equal(5, result.ExitCode);
        Assert.True(File.Exists(runResultPath), "expected run result to be written");
        using var doc = JsonDocument.Parse(File.ReadAllText(runResultPath));
        Assert.Equal("validate", doc.RootElement.GetProperty("command").GetString());
        Assert.False(doc.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal(5, doc.RootElement.GetProperty("exitCode").GetInt32());
        var next = doc.RootElement.GetProperty("nextActions");
        Assert.True(next.GetArrayLength() > 0);
    }

    [Fact]
    public void Init_WritesMetricsScoreboard_WithSuccessCounters()
    {
        var repoRoot = FindRepoRoot();
        var targetDir = CreateTempDir();
        var metricsPath = Path.Combine(targetDir, "metrics-init.json");

        var result = RunCli(repoRoot,
            "init",
            "--target", targetDir,
            "--requested-autonomy", "A1",
            "--tokens-requested", "42",
            "--metrics-scoreboard", metricsPath);

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(metricsPath), "expected metrics scoreboard to be written");
        using var doc = JsonDocument.Parse(File.ReadAllText(metricsPath));
        Assert.Equal(1, doc.RootElement.GetProperty("totalRuns").GetInt32());
        Assert.Equal(1, doc.RootElement.GetProperty("successfulRuns").GetInt32());
        Assert.Equal(0, doc.RootElement.GetProperty("failedRuns").GetInt32());
        Assert.Equal(42, doc.RootElement.GetProperty("totalTokensRequested").GetInt32());
    }

    [Fact]
    public void Validate_AmbiguityBlocked_UpdatesClarificationAndReworkRates()
    {
        var repoRoot = FindRepoRoot();
        var tempDir = CreateTempDir();
        var policyPath = Path.Combine(tempDir, ".meta-agent-policy.json");
        var metricsPath = Path.Combine(tempDir, "metrics-validate.json");
        File.WriteAllText(policyPath, """
{
  "name": "policy",
  "defaultMode": "interactive_ide",
  "autonomyDefault": "A1",
  "ambiguityThreshold": 0.5,
  "commandGating": "all_commands",
  "budgets": {
    "tokensPerDay": 1000,
    "ticketsPerDay": 10,
    "maxConcurrentPrs": 3
  },
  "changeBoundaries": {
    "allowedPaths": ["**"],
    "disallowedPaths": []
  },
  "abortConditions": []
}
""");

        var blocked = RunCli(repoRoot,
            "validate",
            "--policy", policyPath,
            "--ambiguity-score", "0.95",
            "--metrics-scoreboard", metricsPath);

        Assert.Equal(6, blocked.ExitCode);

        var allowed = RunCli(repoRoot,
            "validate",
            "--policy", policyPath,
            "--ambiguity-score", "0.95",
            "--operator-approved-ambiguity",
            "--metrics-scoreboard", metricsPath);

        Assert.Equal(0, allowed.ExitCode);
        Assert.True(File.Exists(metricsPath), "expected metrics scoreboard to be written");
        using var doc = JsonDocument.Parse(File.ReadAllText(metricsPath));
        Assert.Equal(2, doc.RootElement.GetProperty("totalRuns").GetInt32());
        Assert.Equal(1, doc.RootElement.GetProperty("successfulRuns").GetInt32());
        Assert.Equal(1, doc.RootElement.GetProperty("failedRuns").GetInt32());
        Assert.Equal(1, doc.RootElement.GetProperty("reworkRuns").GetInt32());
        Assert.Equal(1, doc.RootElement.GetProperty("clarificationRuns").GetInt32());
    }

    [Fact]
    public void Init_Blocks_WhenSafetyLevel2MissingApprovalOrIntegrationEvidence()
    {
        var repoRoot = FindRepoRoot();
        var targetDir = CreateTempDir();
        var policyPath = WriteAutonomyA2Policy(targetDir);
        var decisionPath = Path.Combine(targetDir, "decision-init-safety-blocked.json");

        var result = RunCli(repoRoot,
            "init",
            "--target", targetDir,
            "--policy", policyPath,
            "--requested-autonomy", "A2",
            "--ticket", "API change across modules with acceptance criteria: endpoint updated",
            "--decision-record", decisionPath);

        Assert.Equal(10, result.ExitCode);
        Assert.True(File.Exists(decisionPath), "expected decision record to be written");
        using var doc = JsonDocument.Parse(File.ReadAllText(decisionPath));
        Assert.False(doc.RootElement.GetProperty("allowed").GetBoolean());
        Assert.Equal("A2", doc.RootElement.GetProperty("requestedAutonomy").GetString());
        var checks = doc.RootElement.GetProperty("checks");
        Assert.Equal("command_execution", checks[0].GetProperty("check").GetString());
        Assert.Equal("change safety level 2 requires operator approval", checks[0].GetProperty("detail").GetString());
        Assert.Equal(2, doc.RootElement.GetProperty("triage").GetProperty("changeSafetyLevel").GetInt32());
    }

    [Fact]
    public void Init_Blocks_WhenSafetyLevel2MissingIntegrationEvidenceEvenWithApproval()
    {
        var repoRoot = FindRepoRoot();
        var targetDir = CreateTempDir();
        var policyPath = WriteAutonomyA2Policy(targetDir);
        var decisionPath = Path.Combine(targetDir, "decision-init-safety-missing-integration.json");

        var result = RunCli(repoRoot,
            "init",
            "--target", targetDir,
            "--policy", policyPath,
            "--requested-autonomy", "A2",
            "--ticket", "API change across modules with acceptance criteria: endpoint updated",
            "--operator-approved-safety",
            "--decision-record", decisionPath);

        Assert.Equal(10, result.ExitCode);
        Assert.True(File.Exists(decisionPath), "expected decision record to be written");
        using var doc = JsonDocument.Parse(File.ReadAllText(decisionPath));
        Assert.False(doc.RootElement.GetProperty("allowed").GetBoolean());
        var checks = doc.RootElement.GetProperty("checks");
        Assert.Equal("change safety level 2 requires --validated-method integration_tests", checks[0].GetProperty("detail").GetString());
        Assert.Equal(2, doc.RootElement.GetProperty("triage").GetProperty("changeSafetyLevel").GetInt32());
    }

    [Fact]
    public void Init_Allows_WhenSafetyLevel2ApprovalAndIntegrationEvidenceProvided()
    {
        var repoRoot = FindRepoRoot();
        var targetDir = CreateTempDir();
        var policyPath = WriteAutonomyA2Policy(targetDir);
        var decisionPath = Path.Combine(targetDir, "decision-init-safety-allowed.json");
        var workflowPath = Path.Combine(targetDir, "workflow-init-safety-allowed.json");

        var result = RunCli(repoRoot,
            "init",
            "--target", targetDir,
            "--policy", policyPath,
            "--requested-autonomy", "A2",
            "--ticket", "API change across modules with acceptance criteria: endpoint updated",
            "--operator-approved-safety",
            "--validated-method", "integration_tests",
            "--workflow-record", workflowPath,
            "--decision-record", decisionPath);

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(decisionPath), "expected decision record to be written");
        using var doc = JsonDocument.Parse(File.ReadAllText(decisionPath));
        Assert.True(doc.RootElement.GetProperty("allowed").GetBoolean());
        Assert.Equal("A2", doc.RootElement.GetProperty("requestedAutonomy").GetString());
        Assert.Equal(2, doc.RootElement.GetProperty("triage").GetProperty("changeSafetyLevel").GetInt32());
    }

    [Fact]
    public void Agent_List_WritesDecisionRecord()
    {
        var repoRoot = FindRepoRoot();
        var tempDir = CreateTempDir();
        var decisionPath = Path.Combine(tempDir, "decision-agent-list.json");

        var result = RunCli(repoRoot, "agent", "list", "--decision-record", decisionPath);

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(decisionPath), "expected decision record to be written");
        using var doc = JsonDocument.Parse(File.ReadAllText(decisionPath));
        Assert.True(doc.RootElement.GetProperty("allowed").GetBoolean());
        Assert.Equal("agent:list", doc.RootElement.GetProperty("command").GetString());
    }

    [Fact]
    public void Version_WritesDecisionRecord()
    {
        var repoRoot = FindRepoRoot();
        var tempDir = CreateTempDir();
        var decisionPath = Path.Combine(tempDir, "decision-version.json");

        var result = RunCli(repoRoot, "version", "--decision-record", decisionPath);

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(decisionPath), "expected decision record to be written");
        using var doc = JsonDocument.Parse(File.ReadAllText(decisionPath));
        Assert.True(doc.RootElement.GetProperty("allowed").GetBoolean());
        Assert.Equal("version", doc.RootElement.GetProperty("command").GetString());
    }

    [Fact]
    public void Triage_WritesOutputAndDecisionRecord()
    {
        var repoRoot = FindRepoRoot();
        var tempDir = CreateTempDir();
        var triagePath = Path.Combine(tempDir, "triage.json");
        var decisionPath = Path.Combine(tempDir, "decision-triage.json");

        var result = RunCli(repoRoot,
            "triage",
            "--ticket", "Update docs only. - README refreshed",
            "--output", triagePath,
            "--decision-record", decisionPath);

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(triagePath), "expected triage output");
        Assert.True(File.Exists(decisionPath), "expected decision record");
        using var triage = JsonDocument.Parse(File.ReadAllText(triagePath));
        Assert.True(triage.RootElement.GetProperty("eligible").GetBoolean());
    }

    [Fact]
    public void Triage_ReturnsIneligibleExitCode_ForEmptyTicket()
    {
        var repoRoot = FindRepoRoot();
        var tempDir = CreateTempDir();
        var triagePath = Path.Combine(tempDir, "triage-ineligible.json");
        var decisionPath = Path.Combine(tempDir, "decision-triage-ineligible.json");

        var result = RunCli(repoRoot,
            "triage",
            "--ticket", "   ",
            "--output", triagePath,
            "--decision-record", decisionPath);

        Assert.Equal(8, result.ExitCode);
        Assert.True(File.Exists(triagePath), "expected triage output");
        Assert.True(File.Exists(decisionPath), "expected decision record");
        using var triage = JsonDocument.Parse(File.ReadAllText(triagePath));
        Assert.False(triage.RootElement.GetProperty("eligible").GetBoolean());
    }

    [Fact]
    public void Validate_MigratesLegacyPolicyFile_ByAddingPolicyVersion()
    {
        var repoRoot = FindRepoRoot();
        var tempDir = CreateTempDir();
        var policyPath = Path.Combine(tempDir, ".meta-agent-policy.json");
        File.WriteAllText(policyPath, """
{
  "name": "legacy-policy",
  "defaultMode": "interactive_ide",
  "autonomyDefault": "A1",
  "budgets": {
    "tokensPerDay": 5000,
    "ticketsPerDay": 3,
    "maxConcurrentPrs": 1
  }
}
""");

        var result = RunCli(repoRoot, "validate", "--policy", policyPath);

        Assert.Equal(0, result.ExitCode);
        var content = File.ReadAllText(policyPath);
        Assert.Contains("\"policyVersion\": 1", content, StringComparison.Ordinal);
        Assert.Contains("Policy migrated from version 0 to 1", result.StdOut, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 10; i++)
        {
            var candidate = Path.Combine(dir.FullName, "meta-agent", "dotnet", "MetaAgent.Cli", "MetaAgent.Cli.csproj");
            if (File.Exists(candidate))
            {
                return dir.FullName;
            }

            if (dir.Parent == null)
            {
                break;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Repository root not found");
    }

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static string CombineRelative(string basePath, string relativePath)
    {
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(basePath, normalized);
    }

    private static string WriteAutonomyA2Policy(string dir)
    {
        var policyPath = Path.Combine(dir, ".meta-agent-policy.json");
        File.WriteAllText(policyPath, """
{
  "name": "policy",
  "defaultMode": "interactive_ide",
  "autonomyDefault": "A2",
  "commandGating": "all_commands",
  "budgets": {
    "tokensPerDay": 1000,
    "ticketsPerDay": 10,
    "maxConcurrentPrs": 3
  },
  "changeBoundaries": {
    "allowedPaths": ["**"],
    "disallowedPaths": []
  },
  "abortConditions": []
}
""");
        return policyPath;
    }

    private static void AssertDecisionAllowed(string decisionPath, bool expectedAllowed)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(decisionPath));
        var allowed = doc.RootElement.GetProperty("allowed").GetBoolean();
        Assert.Equal(expectedAllowed, allowed);
    }

    private static CliRunResult RunCli(string repoRoot, params string[] cliArgs)
    {
        return RunCliInternal(repoRoot, forceNonInteractive: true, envOverrides: null, cliArgs);
    }

    private static CliRunResult RunCliInteractive(string repoRoot, params string[] cliArgs)
    {
        return RunCliInternal(repoRoot, forceNonInteractive: false, envOverrides: null, cliArgs);
    }

    private static CliRunResult RunCliWithEnv(string repoRoot, System.Collections.Generic.IDictionary<string, string> envOverrides, params string[] cliArgs)
    {
        return RunCliInternal(repoRoot, forceNonInteractive: true, envOverrides, cliArgs);
    }

    private static CliRunResult RunCliInternal(string repoRoot, bool forceNonInteractive, System.Collections.Generic.IDictionary<string, string>? envOverrides, params string[] cliArgs)
    {
        var psi = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        psi.ArgumentList.Add("run");
        psi.ArgumentList.Add("--project");
        psi.ArgumentList.Add("./meta-agent/dotnet/MetaAgent.Cli");
        psi.ArgumentList.Add("--");
        if (forceNonInteractive)
        {
            psi.Environment["META_AGENT_NONINTERACTIVE"] = "1";
        }
        if (envOverrides != null)
        {
            foreach (var kvp in envOverrides)
            {
                psi.Environment[kvp.Key] = kvp.Value;
            }
        }
        foreach (var arg in cliArgs)
        {
            psi.ArgumentList.Add(arg);
        }

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start CLI process");
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit(120000);

        return new CliRunResult(process.ExitCode, stdout, stderr);
    }

    private sealed record CliRunResult(int ExitCode, string StdOut, string StdErr);
}
