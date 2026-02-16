using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace MetaAgent.Core
{
    public static class RunResultSectionsBuilder
    {
        public static List<string> BuildAssumptions(string command, string decisionPath, string triagePath)
        {
            var assumptions = new List<string>();
            if (!File.Exists(decisionPath))
            {
                assumptions.Add("No decision record was available at run-result emission time.");
            }
            if (!string.Equals(command, "triage", StringComparison.OrdinalIgnoreCase) && !File.Exists(triagePath))
            {
                assumptions.Add("No triage input was provided; risk/validation assumptions are reduced.");
            }
            if (assumptions.Count == 0)
            {
                assumptions.Add("Policy/triage artifacts were available and used to construct this run result.");
            }
            return assumptions;
        }

        public static List<string> BuildExtractedRequirements(string triagePath)
        {
            var requirements = new List<string>();
            using var doc = RunResultJson.TryParse(triagePath);
            if (doc == null)
            {
                return requirements;
            }

            if (!doc.RootElement.TryGetProperty("definitionOfDone", out var dod) || dod.ValueKind != JsonValueKind.Array)
            {
                return requirements;
            }

            foreach (var item in dod.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    var value = item.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        requirements.Add(value!);
                    }
                }
            }

            return requirements;
        }

        public static List<string> BuildPlan(string workflowPath)
        {
            var plan = new List<string>();
            using var doc = RunResultJson.TryParse(workflowPath);
            if (doc == null)
            {
                return plan;
            }

            if (!doc.RootElement.TryGetProperty("stages", out var stages) || stages.ValueKind != JsonValueKind.Array)
            {
                return plan;
            }

            foreach (var stage in stages.EnumerateArray())
            {
                var name = stage.TryGetProperty("stage", out var s) && s.ValueKind == JsonValueKind.String ? s.GetString() : null;
                var status = stage.TryGetProperty("status", out var st) && st.ValueKind == JsonValueKind.String ? st.GetString() : null;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    plan.Add($"{name}: {status ?? "unknown"}");
                }
            }

            return plan;
        }

        public static List<string> BuildImplementation(string command, string[] args)
        {
            return new List<string>
            {
                $"Executed command: {command}",
                $"Arguments: {string.Join(" ", args)}"
            };
        }

        public static int ExtractTokensRequested(string decisionPath)
        {
            using var doc = RunResultJson.TryParse(decisionPath);
            if (doc == null)
            {
                return 0;
            }

            if (!doc.RootElement.TryGetProperty("checks", out var checks) || checks.ValueKind != JsonValueKind.Array)
            {
                return 0;
            }

            foreach (var check in checks.EnumerateArray())
            {
                var checkName = check.TryGetProperty("check", out var c) && c.ValueKind == JsonValueKind.String ? c.GetString() : null;
                if (!string.Equals(checkName, "budget_tokens", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var detail = check.TryGetProperty("detail", out var d) && d.ValueKind == JsonValueKind.String ? d.GetString() : null;
                if (string.IsNullOrWhiteSpace(detail))
                {
                    continue;
                }

                var marker = "requested=";
                var idx = detail.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (idx < 0)
                {
                    continue;
                }

                idx += marker.Length;
                var end = idx;
                while (end < detail.Length && char.IsDigit(detail[end]))
                {
                    end++;
                }

                if (end > idx && int.TryParse(detail.Substring(idx, end - idx), out var requested))
                {
                    return requested;
                }
            }

            return 0;
        }

        public static List<string> BuildValidationEvidence(string decisionPath, string triagePath)
        {
            var evidence = new List<string>();

            using (var decisionDoc = RunResultJson.TryParse(decisionPath))
            {
                if (decisionDoc != null
                    && decisionDoc.RootElement.TryGetProperty("checks", out var checks)
                    && checks.ValueKind == JsonValueKind.Array)
                {
                    foreach (var check in checks.EnumerateArray())
                    {
                        var name = check.TryGetProperty("check", out var c) && c.ValueKind == JsonValueKind.String ? c.GetString() : "unknown_check";
                        var passed = check.TryGetProperty("passed", out var p) && p.ValueKind == JsonValueKind.True;
                        var detail = check.TryGetProperty("detail", out var d) && d.ValueKind == JsonValueKind.String ? d.GetString() : string.Empty;
                        evidence.Add($"{name}: {(passed ? "passed" : "failed")} ({detail})");
                    }
                }
            }

            using (var triageDoc = RunResultJson.TryParse(triagePath))
            {
                if (triageDoc != null
                    && triageDoc.RootElement.TryGetProperty("validationPlan", out var plan)
                    && plan.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in plan.EnumerateArray())
                    {
                        var method = item.TryGetProperty("method", out var m) && m.ValueKind == JsonValueKind.String ? m.GetString() : null;
                        var decision = item.TryGetProperty("decision", out var dec) && dec.ValueKind == JsonValueKind.String ? dec.GetString() : null;
                        if (!string.IsNullOrWhiteSpace(method) && string.Equals(decision, "chosen", StringComparison.OrdinalIgnoreCase))
                        {
                            evidence.Add($"validation method chosen: {method}");
                        }
                    }
                }
            }

            if (evidence.Count == 0)
            {
                evidence.Add("No explicit validation evidence artifacts were found.");
            }

            return evidence;
        }

        public static List<string> BuildMetricsImpact(string decisionPath, string metricsPath, MetricsScoreboardRecord metrics)
        {
            var impact = new List<string>();
            using var doc = RunResultJson.TryParse(decisionPath);
            if (doc != null
                && doc.RootElement.TryGetProperty("checks", out var checks)
                && checks.ValueKind == JsonValueKind.Array)
            {
                foreach (var check in checks.EnumerateArray())
                {
                    var checkName = check.TryGetProperty("check", out var c) && c.ValueKind == JsonValueKind.String ? c.GetString() : null;
                    var detail = check.TryGetProperty("detail", out var d) && d.ValueKind == JsonValueKind.String ? d.GetString() : null;
                    if (!string.IsNullOrWhiteSpace(checkName)
                        && !string.IsNullOrWhiteSpace(detail)
                        && checkName!.StartsWith("budget_", StringComparison.OrdinalIgnoreCase))
                    {
                        impact.Add($"{checkName}: {detail}");
                    }
                }
            }

            impact.Add($"metrics_scoreboard: {Path.GetFullPath(metricsPath)}");
            impact.Add($"success_rate={metrics.SuccessRate:0.000}");
            impact.Add($"rework_rate={metrics.ReworkRate:0.000}");
            impact.Add($"clarification_rate={metrics.ClarificationRate:0.000}");
            impact.Add($"token_cost_per_success={metrics.TokenCostPerSuccess:0.00}");
            impact.Add($"defect_leakage_incidents={metrics.DefectLeakageIncidents}");
            impact.Add($"time_to_accepted_solution={metrics.TimeToAcceptedSolution:0.00}");
            return impact;
        }

        public static List<string> BuildNextActions(int exitCode, string decisionPath, string workflowPath, string triagePath)
        {
            if (exitCode == 0)
            {
                return new List<string>
                {
                    "Proceed with implementation/review using generated artifacts.",
                    "Update docs if behavior changed."
                };
            }

            var actions = new List<string>
            {
                "Review the failing gate/check detail and rerun with corrected inputs."
            };
            if (File.Exists(decisionPath)) actions.Add($"Inspect decision record: {Path.GetFullPath(decisionPath)}");
            if (File.Exists(workflowPath)) actions.Add($"Inspect workflow record: {Path.GetFullPath(workflowPath)}");
            if (File.Exists(triagePath)) actions.Add($"Inspect triage record: {Path.GetFullPath(triagePath)}");
            return actions;
        }

        public static List<string> BuildDecisionLog(string decisionPath)
        {
            var log = new List<string>();
            using var doc = RunResultJson.TryParse(decisionPath);
            if (doc == null)
            {
                log.Add("Decision record unavailable.");
                return log;
            }

            if (doc.RootElement.TryGetProperty("checks", out var checks) && checks.ValueKind == JsonValueKind.Array)
            {
                foreach (var check in checks.EnumerateArray())
                {
                    var name = check.TryGetProperty("check", out var c) && c.ValueKind == JsonValueKind.String ? c.GetString() : null;
                    var detail = check.TryGetProperty("detail", out var d) && d.ValueKind == JsonValueKind.String ? d.GetString() : null;
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        log.Add($"{name}: {detail}");
                    }
                }
            }
            return log;
        }

        public static List<string> BuildRiskLog(string triagePath)
        {
            var log = new List<string>();
            using var doc = RunResultJson.TryParse(triagePath);
            if (doc == null)
            {
                log.Add("No triage artifact available for risk logging.");
                return log;
            }

            var riskLevel = doc.RootElement.TryGetProperty("riskLevel", out var r) && r.ValueKind == JsonValueKind.String ? r.GetString() : "unknown";
            var score = doc.RootElement.TryGetProperty("riskScore", out var s) && s.ValueKind == JsonValueKind.Number ? s.GetInt32().ToString() : "n/a";
            var safetyLevel = doc.RootElement.TryGetProperty("changeSafetyLevel", out var c) && c.ValueKind == JsonValueKind.Number ? c.GetInt32().ToString() : "n/a";
            log.Add($"riskLevel={riskLevel}, riskScore={score}, changeSafetyLevel={safetyLevel}");
            return log;
        }
    }
}
