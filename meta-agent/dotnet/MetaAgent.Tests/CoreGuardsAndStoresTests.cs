using System;
using System.Collections.Generic;
using System.IO;
using MetaAgent.Core;
using Xunit;

public class CoreGuardsAndStoresTests
{
    [Fact]
    public void BudgetUsageStore_Load_ReturnsNull_WhenAccountingIsNotPersistentDaily()
    {
        var policy = new Policy();
        var state = BudgetUsageStore.Load(Path.Combine(CreateTempDir(), ".meta-agent-policy.json"), policy);
        Assert.Null(state);
    }

    [Fact]
    public void BudgetUsageStore_SaveAndLoad_PersistsDailyState()
    {
        var root = CreateTempDir();
        var policyPath = Path.Combine(root, ".meta-agent-policy.json");
        var policy = new Policy
        {
            BudgetAccounting = new BudgetAccounting
            {
                Mode = "persistent_daily",
                StateFile = ".meta-agent-budget-state.json"
            }
        };

        var toSave = new BudgetUsageState
        {
            DateUtc = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            TokensUsed = 120,
            TicketsUsed = 3
        };

        BudgetUsageStore.Save(policyPath, policy, toSave);
        var loaded = BudgetUsageStore.Load(policyPath, policy);

        Assert.NotNull(loaded);
        Assert.Equal(toSave.DateUtc, loaded!.DateUtc);
        Assert.Equal(120, loaded.TokensUsed);
        Assert.Equal(3, loaded.TicketsUsed);
    }

    [Fact]
    public void BudgetUsageStore_Load_Resets_WhenStateDateIsStale()
    {
        var root = CreateTempDir();
        var policyPath = Path.Combine(root, ".meta-agent-policy.json");
        var statePath = Path.Combine(root, ".meta-agent-budget-state.json");
        var policy = new Policy
        {
            BudgetAccounting = new BudgetAccounting
            {
                Mode = "persistent_daily",
                StateFile = ".meta-agent-budget-state.json"
            }
        };

        File.WriteAllText(statePath, """
{
  "dateUtc": "2000-01-01",
  "tokensUsed": 999,
  "ticketsUsed": 99
}
""");

        var loaded = BudgetUsageStore.Load(policyPath, policy);
        Assert.NotNull(loaded);
        Assert.Equal(DateTime.UtcNow.ToString("yyyy-MM-dd"), loaded!.DateUtc);
        Assert.Equal(0, loaded.TokensUsed);
        Assert.Equal(0, loaded.TicketsUsed);
    }

    [Fact]
    public void PolicyRuntimeGuards_ShouldEnforceCommand_RespectsCommandGating()
    {
        var policy = new Policy { CommandGating = "all_commands" };
        Assert.True(PolicyRuntimeGuards.ShouldEnforceCommand(policy, isMutatingCommand: false));

        policy.CommandGating = "mutating_only";
        Assert.False(PolicyRuntimeGuards.ShouldEnforceCommand(policy, isMutatingCommand: false));
        Assert.True(PolicyRuntimeGuards.ShouldEnforceCommand(policy, isMutatingCommand: true));
    }

    [Fact]
    public void PolicyRuntimeGuards_BuildDecisions_PopulateExpectedFields()
    {
        var bypass = PolicyRuntimeGuards.BuildBypassDecision("validate", "policy.json", "mutating_only");
        Assert.True(bypass.Allowed);
        Assert.Equal("command_gating", bypass.Checks[0].Check);
        Assert.Equal(ExecutionMode.Hybrid, bypass.Mode);

        var command = PolicyRuntimeGuards.BuildCommandDecision(
            "init",
            allowed: false,
            detail: "blocked",
            mode: ExecutionMode.AutonomousTicketRunner,
            requestedAutonomy: "A2",
            policyPath: "policy.json");
        Assert.False(command.Allowed);
        Assert.Equal("command_execution", command.Checks[0].Check);
        Assert.Equal(ExecutionMode.AutonomousTicketRunner, command.Mode);
        Assert.Equal("A2", command.RequestedAutonomy);
    }

    [Fact]
    public void PolicyRuntimeGuards_TryEnforceModeAutonomy_EnforcesA0AndAutonomousRequirements()
    {
        Assert.False(
            PolicyRuntimeGuards.TryEnforceModeAutonomy(
                isMutatingCommand: true,
                mode: ExecutionMode.Hybrid,
                requestedAutonomy: "A0",
                out var reasonA0));
        Assert.Contains("suggest-only", reasonA0, StringComparison.OrdinalIgnoreCase);

        Assert.False(
            PolicyRuntimeGuards.TryEnforceModeAutonomy(
                isMutatingCommand: false,
                mode: ExecutionMode.AutonomousTicketRunner,
                requestedAutonomy: "A1",
                out var reasonMode));
        Assert.Contains("requires autonomy A2 or A3", reasonMode, StringComparison.OrdinalIgnoreCase);

        Assert.True(
            PolicyRuntimeGuards.TryEnforceModeAutonomy(
                isMutatingCommand: false,
                mode: ExecutionMode.AutonomousTicketRunner,
                requestedAutonomy: "A2",
                out _));
    }

    [Fact]
    public void PolicyRuntimeGuards_TryEnforceSafetyGates_HandlesLevels2And3()
    {
        Assert.True(PolicyRuntimeGuards.TryEnforceSafetyGates(null, false, new List<string>(), out _));

        var level2 = new TriageResult { ChangeSafetyLevel = 2 };
        Assert.False(PolicyRuntimeGuards.TryEnforceSafetyGates(level2, false, new List<string>(), out var noApproval));
        Assert.Contains("operator approval", noApproval, StringComparison.OrdinalIgnoreCase);

        Assert.False(PolicyRuntimeGuards.TryEnforceSafetyGates(level2, true, new List<string>(), out var noIntegration));
        Assert.Contains("integration_tests", noIntegration, StringComparison.OrdinalIgnoreCase);

        Assert.True(PolicyRuntimeGuards.TryEnforceSafetyGates(level2, true, new List<string> { "integration_tests" }, out _));

        var level3 = new TriageResult { ChangeSafetyLevel = 3 };
        Assert.False(
            PolicyRuntimeGuards.TryEnforceSafetyGates(
                level3,
                true,
                new List<string> { "integration_tests", "manual_validation_steps" },
                out var missingRuntimeAssert));
        Assert.Contains("runtime_assertions", missingRuntimeAssert, StringComparison.OrdinalIgnoreCase);

        Assert.True(
            PolicyRuntimeGuards.TryEnforceSafetyGates(
                level3,
                true,
                new List<string> { "integration_tests", "manual_validation_steps", "runtime_assertions" },
                out _));
    }

    private static string CreateTempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), "meta-agent-tests", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(path);
        return path;
    }
}
