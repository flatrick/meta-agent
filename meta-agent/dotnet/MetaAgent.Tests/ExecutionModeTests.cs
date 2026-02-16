using MetaAgent.Core;
using Xunit;

public class ExecutionModeTests
{
    [Fact]
    public void Classify_UsesTicketRunner_WhenTicketContextExists()
    {
        var mode = ExecutionMode.Classify(null, isInteractiveShell: true, hasTicketContext: true);
        Assert.Equal(ExecutionMode.AutonomousTicketRunner, mode);
    }

    [Fact]
    public void Classify_DefaultsToInteractiveIde_WhenInteractiveAndNoTicket()
    {
        var mode = ExecutionMode.Classify(null, isInteractiveShell: true, hasTicketContext: false);
        Assert.Equal(ExecutionMode.InteractiveIde, mode);
    }

    [Fact]
    public void NormalizeOrFallback_FallsBackToHybrid_OnUnknownMode()
    {
        var mode = ExecutionMode.NormalizeOrFallback("unknown-mode");
        Assert.Equal(ExecutionMode.Hybrid, mode);
    }
}
