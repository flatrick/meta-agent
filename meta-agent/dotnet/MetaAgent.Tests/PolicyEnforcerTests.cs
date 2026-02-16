using System.IO;
using MetaAgent.Core;
using Xunit;

public class PolicyEnforcerTests
{
    [Fact]
    public void Evaluate_AllChecksPass_WhenInputsWithinPolicy()
    {
        var policy = new Policy
        {
            AutonomyDefault = "A2",
            Budgets = new Budgets
            {
                TokensPerDay = 1000,
                TicketsPerDay = 5,
                MaxConcurrentPrs = 2
            },
            ChangeBoundaries = new ChangeBoundaries
            {
                AllowedPaths = new[] { "src/**", "tests/**" },
                DisallowedPaths = new[] { "src/secrets/**" }
            },
            AbortConditions = new[] { "ci_failing_repeatedly" }
        };

        var policyPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), ".meta-agent-policy.json");
        var srcPath = Path.Combine(Path.GetDirectoryName(policyPath)!, "src", "feature");
        var input = new PolicyCheckInput
        {
            Command = "init",
            PolicyPath = policyPath,
            RequestedAutonomy = "A1",
            TokensRequested = 200,
            TicketsRequested = 1,
            OpenPullRequests = 1,
            WritePaths = new[] { srcPath },
            AbortSignals = new[] { "nothing" }
        };

        var record = PolicyEnforcer.Evaluate(policy, input);

        Assert.True(record.Allowed);
        Assert.Equal(6, record.Checks.Count);
    }

    [Fact]
    public void Evaluate_Blocks_WhenAutonomyExceedsPolicy()
    {
        var policy = new Policy { AutonomyDefault = "A1" };
        var input = new PolicyCheckInput
        {
            Command = "init",
            PolicyPath = Path.Combine(Path.GetTempPath(), "p.json"),
            RequestedAutonomy = "A2"
        };

        var record = PolicyEnforcer.Evaluate(policy, input);

        Assert.False(record.Allowed);
        Assert.Contains(record.Checks, c => c.Check == "autonomy_gate" && !c.Passed);
    }

    [Fact]
    public void Evaluate_Blocks_WhenAbortSignalMatchesConfiguredCondition()
    {
        var policy = new Policy
        {
            AbortConditions = new[] { "tests_flaky" }
        };
        var input = new PolicyCheckInput
        {
            Command = "init",
            PolicyPath = Path.Combine(Path.GetTempPath(), "p.json"),
            AbortSignals = new[] { "tests_flaky" }
        };

        var record = PolicyEnforcer.Evaluate(policy, input);

        Assert.False(record.Allowed);
        Assert.Contains(record.Checks, c => c.Check == "abort_conditions" && !c.Passed);
    }

    [Fact]
    public void Evaluate_Blocks_WhenPathOutsideAllowedBoundaries()
    {
        var policy = new Policy
        {
            ChangeBoundaries = new ChangeBoundaries
            {
                AllowedPaths = new[] { "src/**" },
                DisallowedPaths = new[] { "src/blocked/**" }
            }
        };

        var root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var policyPath = Path.Combine(root, ".meta-agent-policy.json");
        var outside = Path.Combine(root, "docs", "readme.md");

        var input = new PolicyCheckInput
        {
            Command = "init",
            PolicyPath = policyPath,
            WritePaths = new[] { outside }
        };

        var record = PolicyEnforcer.Evaluate(policy, input);

        Assert.False(record.Allowed);
        Assert.Contains(record.Checks, c => c.Check == "change_boundaries" && !c.Passed);
    }
}
