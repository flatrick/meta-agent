using MetaAgent.Core;
using Xunit;

public class WorkflowEngineTests
{
    [Fact]
    public void BuildForInit_CanProceed_WhenAmbiguityBelowThreshold()
    {
        var record = WorkflowEngine.BuildForInit(ExecutionMode.InteractiveIde, "A1", ambiguityScore: 0.2, ambiguityThreshold: 0.6, operatorApproved: false);

        Assert.True(record.CanProceed);
        Assert.False(record.RequiresOperatorEscalation);
        Assert.All(record.Stages, s => Assert.Equal("completed", s.Status));
    }

    [Fact]
    public void BuildForInit_Blocks_WhenAmbiguityHighAndNotApproved()
    {
        var record = WorkflowEngine.BuildForInit(ExecutionMode.InteractiveIde, "A1", ambiguityScore: 0.9, ambiguityThreshold: 0.6, operatorApproved: false);

        Assert.False(record.CanProceed);
        Assert.True(record.RequiresOperatorEscalation);
        Assert.Contains(record.Stages, s => s.Stage == "Execute" && s.Status == "blocked");
    }

    [Fact]
    public void BuildForCommand_UsesProvidedCommandName()
    {
        var record = WorkflowEngine.BuildForCommand("validate", ExecutionMode.Hybrid, "A2", ambiguityScore: 0.2, ambiguityThreshold: 0.6, operatorApproved: false);

        Assert.Equal("validate", record.Command);
        Assert.Equal(ExecutionMode.Hybrid, record.Mode);
        Assert.Equal("A2", record.RequestedAutonomy);
        Assert.True(record.CanProceed);
    }

    [Fact]
    public void BuildForCommand_InteractiveMode_RequiresPlanApproval()
    {
        var blocked = WorkflowEngine.BuildForCommand(
            "configure",
            ExecutionMode.InteractiveIde,
            "A1",
            ambiguityScore: 0.2,
            ambiguityThreshold: 0.6,
            operatorApproved: false,
            isNonTrivial: true,
            operatorApprovedPlan: false);

        Assert.True(blocked.RequiresPlanApproval);
        Assert.False(blocked.OperatorApprovedPlan);
        Assert.False(blocked.CanProceed);
        Assert.Contains(blocked.Stages, s => s.Stage == "ConfirmPlanWithOperator" && s.Status == "blocked");
        Assert.Contains(blocked.Stages, s => s.Stage == "DocumentPlan" && s.Status == "completed");
        Assert.Contains(blocked.Stages, s => s.Stage == "Execute" && s.Status == "blocked");

        var approved = WorkflowEngine.BuildForCommand(
            "configure",
            ExecutionMode.InteractiveIde,
            "A1",
            ambiguityScore: 0.2,
            ambiguityThreshold: 0.6,
            operatorApproved: false,
            isNonTrivial: true,
            operatorApprovedPlan: true);

        Assert.True(approved.CanProceed);
        Assert.Contains(approved.Stages, s => s.Stage == "ConfirmPlanWithOperator" && s.Status == "completed");
    }
}
