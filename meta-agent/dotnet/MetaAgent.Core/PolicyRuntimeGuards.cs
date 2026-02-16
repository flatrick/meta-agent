using System;
using System.Collections.Generic;
using System.IO;

namespace MetaAgent.Core
{
    public static class PolicyRuntimeGuards
    {
        public static bool ShouldEnforceCommand(Policy policy, bool isMutatingCommand)
        {
            if (string.Equals(policy.CommandGating, "all_commands", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return isMutatingCommand;
        }

        public static PolicyDecisionRecord BuildBypassDecision(string command, string policyPath, string detail)
        {
            return new PolicyDecisionRecord
            {
                Command = command,
                PolicyPath = Path.GetFullPath(policyPath),
                Mode = ExecutionMode.Hybrid,
                RequestedAutonomy = "A1",
                BudgetProfile = "gating_bypass",
                BudgetProfileReason = "command gating bypassed policy checks",
                Allowed = true,
                Checks = new List<PolicyCheckResult>
                {
                    new PolicyCheckResult
                    {
                        Check = "command_gating",
                        Passed = true,
                        Detail = detail
                    }
                }
            };
        }

        public static PolicyDecisionRecord BuildCommandDecision(string command, bool allowed, string detail, string mode, string requestedAutonomy, string policyPath)
        {
            return new PolicyDecisionRecord
            {
                Command = command,
                PolicyPath = Path.GetFullPath(policyPath),
                Mode = mode,
                RequestedAutonomy = requestedAutonomy,
                BudgetProfile = "command_decision_only",
                BudgetProfileReason = "decision emitted before full policy evaluation",
                Allowed = allowed,
                Checks = new List<PolicyCheckResult>
                {
                    new PolicyCheckResult
                    {
                        Check = "command_execution",
                        Passed = allowed,
                        Detail = detail
                    }
                }
            };
        }

        public static bool TryEnforceModeAutonomy(bool isMutatingCommand, string mode, string requestedAutonomy, out string reason)
        {
            reason = string.Empty;
            if (string.Equals(requestedAutonomy, "A0", StringComparison.OrdinalIgnoreCase) && isMutatingCommand)
            {
                reason = "A0 is suggest-only and cannot execute mutating commands";
                return false;
            }

            if (string.Equals(mode, ExecutionMode.AutonomousTicketRunner, StringComparison.OrdinalIgnoreCase)
                && (string.Equals(requestedAutonomy, "A0", StringComparison.OrdinalIgnoreCase) || string.Equals(requestedAutonomy, "A1", StringComparison.OrdinalIgnoreCase)))
            {
                reason = "autonomous_ticket_runner mode requires autonomy A2 or A3";
                return false;
            }

            return true;
        }

        public static bool TryEnforceSafetyGates(TriageResult? triage, bool operatorApprovedSafety, List<string> validatedMethods, out string reason)
        {
            reason = string.Empty;
            if (triage == null)
            {
                return true;
            }

            var level = triage.ChangeSafetyLevel;
            var provided = new HashSet<string>(validatedMethods ?? new List<string>(), StringComparer.OrdinalIgnoreCase);

            if (level >= 2 && !operatorApprovedSafety)
            {
                reason = $"change safety level {level} requires operator approval";
                return false;
            }

            if (level == 2)
            {
                if (!provided.Contains("integration_tests"))
                {
                    reason = "change safety level 2 requires --validated-method integration_tests";
                    return false;
                }
            }
            else if (level >= 3)
            {
                var required = new[] { "integration_tests", "manual_validation_steps", "runtime_assertions" };
                foreach (var method in required)
                {
                    if (!provided.Contains(method))
                    {
                        reason = $"change safety level 3 requires --validated-method {method}";
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
