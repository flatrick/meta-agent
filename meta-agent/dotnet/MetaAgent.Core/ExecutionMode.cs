using System;

namespace MetaAgent.Core
{
    public static class ExecutionMode
    {
        public const string InteractiveIde = "interactive_ide";
        public const string AutonomousTicketRunner = "autonomous_ticket_runner";
        public const string Hybrid = "hybrid";

        public static string Classify(string? requestedMode, bool isInteractiveShell, bool hasTicketContext)
        {
            if (!string.IsNullOrWhiteSpace(requestedMode))
            {
                return NormalizeOrFallback(requestedMode);
            }

            if (hasTicketContext)
            {
                return AutonomousTicketRunner;
            }

            if (isInteractiveShell)
            {
                return InteractiveIde;
            }

            return Hybrid;
        }

        public static string NormalizeOrFallback(string mode)
        {
            var normalized = mode.Trim().ToLowerInvariant();
            if (normalized == InteractiveIde || normalized == AutonomousTicketRunner || normalized == Hybrid)
            {
                return normalized;
            }

            return Hybrid;
        }
    }
}
