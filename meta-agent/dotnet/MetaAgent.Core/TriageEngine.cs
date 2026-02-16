using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MetaAgent.Core
{
    public sealed class ValidationMethodDecision
    {
        public string Method { get; set; } = string.Empty;
        public string Decision { get; set; } = string.Empty; // chosen | skipped
        public string Rationale { get; set; } = string.Empty;
    }

    public sealed class TriageResult
    {
        public string Version { get; set; } = "1";
        public bool Eligible { get; set; }
        public string EligibilityReason { get; set; } = string.Empty;
        public string RiskLevel { get; set; } = "low";
        public int RiskScore { get; set; }
        public string Size { get; set; } = "small";
        public int ChangeSafetyLevel { get; set; } // 0..3
        public string StrategyTier { get; set; } = "1";
        public List<string> DefinitionOfDone { get; set; } = new List<string>();
        public List<string> RequiredGates { get; set; } = new List<string>();
        public List<ValidationMethodDecision> ValidationPlan { get; set; } = new List<ValidationMethodDecision>();
    }

    public static class TriageEngine
    {
        private static readonly string[] HighRiskKeywords = new[]
        {
            "security", "auth", "authentication", "authorization", "payment", "billing", "data", "database", "infrastructure", "infra", "deployment", "secrets", "ci", "cd"
        };

        private static readonly string[] Level3Keywords = new[]
        {
            "infrastructure", "infra", "auth", "authentication", "authorization", "payment", "billing", "database", "data migration", "schema"
        };

        private static readonly string[] Level0Keywords = new[]
        {
            "docs", "documentation", "readme", "comment", "tests only", "test only"
        };

        public static TriageResult Evaluate(string ticketText)
        {
            var text = ticketText ?? string.Empty;
            var normalized = text.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return new TriageResult
                {
                    Eligible = false,
                    EligibilityReason = "ticket is empty or underspecified",
                    RiskLevel = "unknown",
                    RiskScore = 0,
                    Size = "unknown",
                    ChangeSafetyLevel = 3,
                    StrategyTier = "3",
                    RequiredGates = RequiredGatesForSafetyLevel(3),
                    ValidationPlan = BuildValidationPlan(3, "high", "ineligible ticket")
                };
            }

            var dod = ExtractDefinitionOfDone(normalized);
            var riskScore = ScoreRisk(normalized);
            var riskLevel = riskScore >= 2 ? "high" : riskScore >= 1 ? "medium" : "low";
            var size = EstimateSize(normalized);
            var level = ClassifyChangeSafetyLevel(normalized);
            var tier = SelectStrategyTier(riskLevel, size);

            var eligible = dod.Count > 0 || normalized.Length > 30;
            var reason = eligible ? "eligible" : "missing explicit acceptance criteria/definition of done";

            return new TriageResult
            {
                Eligible = eligible,
                EligibilityReason = reason,
                RiskLevel = riskLevel,
                RiskScore = riskScore,
                Size = size,
                ChangeSafetyLevel = level,
                StrategyTier = tier,
                DefinitionOfDone = dod,
                RequiredGates = RequiredGatesForSafetyLevel(level),
                ValidationPlan = BuildValidationPlan(level, riskLevel, "triage-derived plan")
            };
        }

        private static List<string> RequiredGatesForSafetyLevel(int safetyLevel)
        {
            if (safetyLevel <= 0) return new List<string> { "basic_validation" };
            if (safetyLevel == 1) return new List<string> { "unit_tests" };
            if (safetyLevel == 2) return new List<string> { "integration_tests", "operator_approval" };
            return new List<string> { "integration_tests", "manual_validation_steps", "runtime_assertions", "operator_approval" };
        }

        private static List<string> ExtractDefinitionOfDone(string text)
        {
            var lines = text.Split('\n');
            var dod = new List<string>();
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (line.StartsWith("- ") || line.StartsWith("* "))
                {
                    dod.Add(line.Substring(2).Trim());
                    continue;
                }

                if (line.StartsWith("ac:", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("acceptance criteria:", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("definition of done:", StringComparison.OrdinalIgnoreCase))
                {
                    var idx = line.IndexOf(':');
                    var value = idx >= 0 ? line.Substring(idx + 1).Trim() : string.Empty;
                    if (!string.IsNullOrWhiteSpace(value)) dod.Add(value);
                }
            }
            return dod.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static int ScoreRisk(string text)
        {
            var score = 0;
            var lower = text.ToLowerInvariant();
            foreach (var keyword in HighRiskKeywords)
            {
                if (lower.Contains(keyword))
                {
                    score++;
                }
            }
            return score;
        }

        private static string EstimateSize(string text)
        {
            var tokenEstimate = Regex.Split(text.Trim(), @"\s+").Length;
            if (tokenEstimate < 60) return "small";
            if (tokenEstimate < 180) return "medium";
            return "large";
        }

        private static int ClassifyChangeSafetyLevel(string text)
        {
            var lower = text.ToLowerInvariant();
            if (Level0Keywords.Any(k => lower.Contains(k))) return 0;
            if (Level3Keywords.Any(k => lower.Contains(k))) return 3;
            if (lower.Contains("api") || lower.Contains("cross-module") || lower.Contains("cross module")) return 2;
            return 1;
        }

        private static string SelectStrategyTier(string riskLevel, string size)
        {
            if (riskLevel == "high" || size == "large") return "3";
            if (riskLevel == "medium" || size == "medium") return "2";
            return "1";
        }

        private static List<ValidationMethodDecision> BuildValidationPlan(int safetyLevel, string riskLevel, string context)
        {
            var all = new[] { "static_analysis", "unit_tests", "integration_tests", "property_tests", "manual_validation_steps", "runtime_assertions" };
            var selected = new HashSet<string>(StringComparer.Ordinal);

            selected.Add("static_analysis");
            if (safetyLevel >= 1) selected.Add("unit_tests");
            if (safetyLevel >= 2 || riskLevel == "high") selected.Add("integration_tests");
            if (safetyLevel >= 3) selected.Add("manual_validation_steps");
            if (safetyLevel >= 3) selected.Add("runtime_assertions");
            if (safetyLevel >= 2 && riskLevel == "high") selected.Add("property_tests");

            var result = new List<ValidationMethodDecision>();
            foreach (var method in all)
            {
                var chosen = selected.Contains(method);
                result.Add(new ValidationMethodDecision
                {
                    Method = method,
                    Decision = chosen ? "chosen" : "skipped",
                    Rationale = chosen
                        ? $"chosen due to safety_level={safetyLevel}, risk_level={riskLevel}, context={context}"
                        : $"skipped for current safety_level={safetyLevel}, risk_level={riskLevel}"
                });
            }

            return result;
        }
    }
}
