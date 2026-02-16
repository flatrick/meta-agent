using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MetaAgent.Core
{
    public sealed class PolicyCheckInput
    {
        public string Command { get; set; } = string.Empty;
        public string PolicyPath { get; set; } = string.Empty;
        public string RequestedAutonomy { get; set; } = "A1";
        public int TokensRequested { get; set; } = 0;
        public int TicketsRequested { get; set; } = 0;
        public int TokensUsedToday { get; set; } = 0;
        public int TicketsUsedToday { get; set; } = 0;
        public int OpenPullRequests { get; set; } = 0;
        public string[] WritePaths { get; set; } = Array.Empty<string>();
        public string[] AbortSignals { get; set; } = Array.Empty<string>();
    }

    public sealed class PolicyCheckResult
    {
        public string Check { get; set; } = string.Empty;
        public bool Passed { get; set; }
        public string Detail { get; set; } = string.Empty;
    }

    public sealed class PolicyDecisionRecord
    {
        public string Version { get; set; } = "1";
        public string Command { get; set; } = string.Empty;
        public string PolicyPath { get; set; } = string.Empty;
        public string Mode { get; set; } = ExecutionMode.Hybrid;
        public string RequestedAutonomy { get; set; } = "A1";
        public string BudgetProfile { get; set; } = "default";
        public string BudgetProfileReason { get; set; } = "daily budget policy";
        public bool Allowed { get; set; }
        public TriageResult? Triage { get; set; }
        public List<PolicyCheckResult> Checks { get; set; } = new List<PolicyCheckResult>();
    }

    public static class PolicyEnforcer
    {
        public static PolicyDecisionRecord Evaluate(Policy policy, PolicyCheckInput input)
        {
            var checks = new List<PolicyCheckResult>();
            checks.Add(CheckAutonomy(policy, input));
            checks.Add(CheckTokensBudget(policy, input));
            checks.Add(CheckTicketsBudget(policy, input));
            checks.Add(CheckConcurrentPrBudget(policy, input));
            checks.Add(CheckAbortConditions(policy, input));
            checks.Add(CheckChangeBoundaries(policy, input));

            var allPassed = true;
            foreach (var check in checks)
            {
                if (!check.Passed)
                {
                    allPassed = false;
                    break;
                }
            }

            return new PolicyDecisionRecord
            {
                Command = input.Command,
                PolicyPath = Path.GetFullPath(input.PolicyPath),
                Allowed = allPassed,
                Checks = checks
            };
        }

        public static void WriteDecisionRecord(string path, PolicyDecisionRecord record)
        {
            var fullPath = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? Directory.GetCurrentDirectory());
            var opts = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                WriteIndented = true
            };
            File.WriteAllText(fullPath, JsonSerializer.Serialize(record, opts));
        }

        private static PolicyCheckResult CheckAutonomy(Policy policy, PolicyCheckInput input)
        {
            var allowed = ParseAutonomy(policy.AutonomyDefault);
            var requested = ParseAutonomy(input.RequestedAutonomy);
            var passed = requested <= allowed;
            return new PolicyCheckResult
            {
                Check = "autonomy_gate",
                Passed = passed,
                Detail = $"requested={input.RequestedAutonomy}, allowed={policy.AutonomyDefault}"
            };
        }

        private static PolicyCheckResult CheckTokensBudget(Policy policy, PolicyCheckInput input)
        {
            var projected = input.TokensUsedToday + input.TokensRequested;
            var passed = projected <= policy.Budgets.TokensPerDay;
            return new PolicyCheckResult
            {
                Check = "budget_tokens",
                Passed = passed,
                Detail = $"used={input.TokensUsedToday}, requested={input.TokensRequested}, projected={projected}, limit={policy.Budgets.TokensPerDay}"
            };
        }

        private static PolicyCheckResult CheckTicketsBudget(Policy policy, PolicyCheckInput input)
        {
            var projected = input.TicketsUsedToday + input.TicketsRequested;
            var passed = projected <= policy.Budgets.TicketsPerDay;
            return new PolicyCheckResult
            {
                Check = "budget_tickets",
                Passed = passed,
                Detail = $"used={input.TicketsUsedToday}, requested={input.TicketsRequested}, projected={projected}, limit={policy.Budgets.TicketsPerDay}"
            };
        }

        private static PolicyCheckResult CheckConcurrentPrBudget(Policy policy, PolicyCheckInput input)
        {
            var passed = input.OpenPullRequests <= policy.Budgets.MaxConcurrentPrs;
            return new PolicyCheckResult
            {
                Check = "budget_max_concurrent_prs",
                Passed = passed,
                Detail = $"open={input.OpenPullRequests}, limit={policy.Budgets.MaxConcurrentPrs}"
            };
        }

        private static PolicyCheckResult CheckAbortConditions(Policy policy, PolicyCheckInput input)
        {
            var blocked = new List<string>();
            var configured = new HashSet<string>(policy.AbortConditions ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            foreach (var signal in input.AbortSignals ?? Array.Empty<string>())
            {
                if (configured.Contains(signal))
                {
                    blocked.Add(signal);
                }
            }

            var passed = blocked.Count == 0;
            return new PolicyCheckResult
            {
                Check = "abort_conditions",
                Passed = passed,
                Detail = passed ? "no abort conditions triggered" : $"triggered={string.Join(",", blocked)}"
            };
        }

        private static PolicyCheckResult CheckChangeBoundaries(Policy policy, PolicyCheckInput input)
        {
            var writePaths = input.WritePaths ?? Array.Empty<string>();
            if (writePaths.Length == 0)
            {
                return new PolicyCheckResult
                {
                    Check = "change_boundaries",
                    Passed = true,
                    Detail = "no write paths"
                };
            }

            var policyRoot = Path.GetDirectoryName(Path.GetFullPath(input.PolicyPath)) ?? Directory.GetCurrentDirectory();
            var allowedPatterns = policy.ChangeBoundaries?.AllowedPaths ?? Array.Empty<string>();
            var disallowedPatterns = policy.ChangeBoundaries?.DisallowedPaths ?? Array.Empty<string>();

            var denied = new List<string>();
            foreach (var candidate in writePaths)
            {
                var rel = NormalizePath(MakeRelative(policyRoot, candidate));

                var disallowed = MatchesAny(rel, disallowedPatterns);
                if (disallowed)
                {
                    denied.Add($"{rel} matched disallowed path");
                    continue;
                }

                if (allowedPatterns.Length > 0 && !MatchesAny(rel, allowedPatterns))
                {
                    denied.Add($"{rel} is outside allowed paths");
                }
            }

            var passed = denied.Count == 0;
            return new PolicyCheckResult
            {
                Check = "change_boundaries",
                Passed = passed,
                Detail = passed ? $"validated={writePaths.Length}" : string.Join("; ", denied)
            };
        }

        private static bool MatchesAny(string relativePath, string[] patterns)
        {
            foreach (var pattern in patterns)
            {
                if (GlobMatch(relativePath, pattern))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool GlobMatch(string value, string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                return false;
            }

            var normalizedPattern = NormalizePath(pattern.Trim());
            if (normalizedPattern == "**")
            {
                return true;
            }

            var regexPattern = "^" + Regex.Escape(normalizedPattern)
                .Replace(@"\*\*", ".*")
                .Replace(@"\*", "[^/]*")
                .Replace(@"\?", "[^/]") + "$";

            return Regex.IsMatch(NormalizePath(value), regexPattern, RegexOptions.IgnoreCase);
        }

        private static string MakeRelative(string root, string candidatePath)
        {
            var absolute = Path.IsPathRooted(candidatePath)
                ? candidatePath
                : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), candidatePath));
            return Path.GetRelativePath(root, absolute);
        }

        private static string NormalizePath(string p)
        {
            return p.Replace('\\', '/');
        }

        private static int ParseAutonomy(string autonomy)
        {
            if (string.IsNullOrWhiteSpace(autonomy) || autonomy.Length != 2 || autonomy[0] != 'A' || autonomy[1] < '0' || autonomy[1] > '3')
            {
                throw new InvalidOperationException($"Invalid autonomy level: {autonomy}");
            }
            return autonomy[1] - '0';
        }
    }
}
