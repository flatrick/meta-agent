using MetaAgent.Core;
using Xunit;

public class TriageEngineTests
{
    [Fact]
    public void Evaluate_AssignsHighRiskAndLevel3_ForSensitiveKeywords()
    {
        var ticket = """
Implement authentication and payment changes.
Acceptance Criteria: API access requires OAuth.
""";

        var result = TriageEngine.Evaluate(ticket);

        Assert.True(result.Eligible);
        Assert.Equal("high", result.RiskLevel);
        Assert.Equal(3, result.ChangeSafetyLevel);
        Assert.Contains(result.ValidationPlan, v => v.Method == "manual_validation_steps" && v.Decision == "chosen");
    }

    [Fact]
    public void Evaluate_AssignsLevel0_ForDocsOnlyTicket()
    {
        var ticket = """
Update docs only.
- README includes setup steps
""";

        var result = TriageEngine.Evaluate(ticket);

        Assert.True(result.Eligible);
        Assert.Equal(0, result.ChangeSafetyLevel);
        Assert.Equal("1", result.StrategyTier);
    }

    [Fact]
    public void Evaluate_MarksIneligible_WhenTicketIsEmpty()
    {
        var result = TriageEngine.Evaluate("   ");
        Assert.False(result.Eligible);
        Assert.Equal("ticket is empty or underspecified", result.EligibilityReason);
    }
}
