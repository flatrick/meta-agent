using System;
using System.Collections.Generic;
using System.IO;
using MetaAgent.Core;

internal static class RunResultOrchestrator
{
    public static void TryWrite(string command, string[] args, int exitCode)
    {
        if (!ShouldEmitFor(command))
        {
            return;
        }

        try
        {
            var artifacts = RunResultArtifactPaths.Resolve(command, args);
            var mode = RunResultJson.ReadString(artifacts.DecisionPath, "mode")
                ?? RunResultJson.ReadString(artifacts.WorkflowPath, "mode")
                ?? ExecutionMode.Hybrid;
            var requestedAutonomy = RunResultJson.ReadString(artifacts.DecisionPath, "requestedAutonomy")
                ?? RunResultJson.ReadString(artifacts.WorkflowPath, "requestedAutonomy")
                ?? "A1";
            var riskLevel = RunResultJson.ReadString(artifacts.TriagePath, "riskLevel")
                ?? (exitCode == 0 ? "low" : "unknown");
            var tokensRequested = RunResultSectionsBuilder.ExtractTokensRequested(artifacts.DecisionPath);
            var metrics = MetricsScoreboard.Update(artifacts.MetricsPath, new MetricsUpdate
            {
                Command = command,
                ExitCode = exitCode,
                TokensRequested = tokensRequested
            });

            var record = new RunResultRecord
            {
                Command = command,
                ExitCode = exitCode,
                Success = exitCode == 0,
                Mode = mode,
                RequestedAutonomy = requestedAutonomy,
                Summary = exitCode == 0
                    ? $"{command} completed successfully"
                    : $"{command} exited with code {exitCode}",
                RiskLevel = riskLevel,
                Assumptions = RunResultSectionsBuilder.BuildAssumptions(command, artifacts.DecisionPath, artifacts.TriagePath),
                ExtractedRequirements = RunResultSectionsBuilder.BuildExtractedRequirements(artifacts.TriagePath),
                Plan = RunResultSectionsBuilder.BuildPlan(artifacts.WorkflowPath),
                Implementation = RunResultSectionsBuilder.BuildImplementation(command, args),
                ValidationEvidence = RunResultSectionsBuilder.BuildValidationEvidence(artifacts.DecisionPath, artifacts.TriagePath),
                DocumentationUpdates = new List<string>
                {
                    "Update DOC_DELTA.md for non-trivial behavior changes."
                },
                MetricsImpact = RunResultSectionsBuilder.BuildMetricsImpact(artifacts.DecisionPath, artifacts.MetricsPath, metrics),
                NextActions = RunResultSectionsBuilder.BuildNextActions(exitCode, artifacts.DecisionPath, artifacts.WorkflowPath, artifacts.TriagePath)
            };

            AddArtifactReferences(record, artifacts);
            AddAutonomousDetails(record, mode, artifacts);

            RunResultWriter.Write(artifacts.RunResultPath, record);
            Console.WriteLine($"Run result written: {artifacts.RunResultPath}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Run result write warning: {ex.Message}");
        }
    }

    private static bool ShouldEmitFor(string command)
    {
        return string.Equals(command, "init", StringComparison.OrdinalIgnoreCase)
            || string.Equals(command, "configure", StringComparison.OrdinalIgnoreCase)
            || string.Equals(command, "validate", StringComparison.OrdinalIgnoreCase)
            || string.Equals(command, "triage", StringComparison.OrdinalIgnoreCase);
    }

    private static void AddArtifactReferences(RunResultRecord record, RunResultArtifactPaths artifacts)
    {
        if (File.Exists(artifacts.DecisionPath)) record.Artifacts["decisionRecord"] = Path.GetFullPath(artifacts.DecisionPath);
        if (File.Exists(artifacts.WorkflowPath)) record.Artifacts["workflowRecord"] = Path.GetFullPath(artifacts.WorkflowPath);
        if (File.Exists(artifacts.TriagePath)) record.Artifacts["triageRecord"] = Path.GetFullPath(artifacts.TriagePath);
        record.Artifacts["metricsScoreboard"] = Path.GetFullPath(artifacts.MetricsPath);
    }

    private static void AddAutonomousDetails(RunResultRecord record, string mode, RunResultArtifactPaths artifacts)
    {
        if (!string.Equals(mode, ExecutionMode.AutonomousTicketRunner, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        record.Autonomous = new AutonomousRunDetails
        {
            DecisionLog = RunResultSectionsBuilder.BuildDecisionLog(artifacts.DecisionPath),
            RiskLog = RunResultSectionsBuilder.BuildRiskLog(artifacts.TriagePath),
            RollbackNotes = new List<string>
            {
                "If changes were merged and regressions appear, revert with git revert and open a follow-up ticket.",
                "Use decision/workflow/triage artifacts to identify failed gates before retry."
            }
        };
    }
}
