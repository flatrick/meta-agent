using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace MetaAgent.Core
{
    public sealed class WorkflowStageResult
    {
        public string Stage { get; set; } = string.Empty;
        public string Status { get; set; } = "pending";
        public string Detail { get; set; } = string.Empty;
    }

    public sealed class WorkflowRecord
    {
        public string Version { get; set; } = "1";
        public string Command { get; set; } = string.Empty;
        public string Mode { get; set; } = ExecutionMode.Hybrid;
        public string RequestedAutonomy { get; set; } = "A1";
        public bool IsNonTrivial { get; set; } = true;
        public double AmbiguityScore { get; set; }
        public double AmbiguityThreshold { get; set; }
        public bool RequiresOperatorEscalation { get; set; }
        public bool OperatorApproved { get; set; }
        public bool RequiresPlanApproval { get; set; }
        public bool OperatorApprovedPlan { get; set; }
        public bool CanProceed { get; set; }
        public TriageResult? Triage { get; set; }
        public List<WorkflowStageResult> Stages { get; set; } = new List<WorkflowStageResult>();
    }

    public static class WorkflowEngine
    {
        private static readonly string[] MandatoryNonTrivialStages = new[]
        {
            "UnderstandAndClarify",
            "Plan",
            "DocumentPlan",
            "Execute",
            "Validate",
            "DocumentDocDelta",
            "EvaluateMetrics",
            "RefineStrategy"
        };

        private static readonly string[] InteractiveNonTrivialStages = new[]
        {
            "UnderstandAndClarify",
            "Plan",
            "ConfirmPlanWithOperator",
            "DocumentPlan",
            "Execute",
            "Validate",
            "DocumentDocDelta",
            "EvaluateMetrics",
            "RefineStrategy"
        };

        public static WorkflowRecord BuildForCommand(string command, string mode, string requestedAutonomy, double ambiguityScore, double ambiguityThreshold, bool operatorApproved, bool isNonTrivial = true, bool operatorApprovedPlan = true)
        {
            var requiresEscalation = ambiguityScore > ambiguityThreshold;
            var requiresPlanApproval = isNonTrivial
                && string.Equals(mode, ExecutionMode.InteractiveIde, StringComparison.OrdinalIgnoreCase);
            var planApproved = !requiresPlanApproval || operatorApprovedPlan;
            var canProceed = (!requiresEscalation || operatorApproved) && planApproved;

            var stages = new List<WorkflowStageResult>();
            var selectedStages = isNonTrivial
                ? (requiresPlanApproval ? InteractiveNonTrivialStages : MandatoryNonTrivialStages)
                : new[] { "Execute", "Validate" };

            foreach (var stage in selectedStages)
            {
                var status = "completed";
                var detail = "stage completed";
                if (stage == "ConfirmPlanWithOperator")
                {
                    status = planApproved ? "completed" : "blocked";
                    detail = planApproved
                        ? "operator approved execution plan"
                        : "operator plan approval required before execution";
                }
                if (!canProceed && (stage == "Execute" || stage == "Validate" || stage == "DocumentDocDelta" || stage == "EvaluateMetrics" || stage == "RefineStrategy"))
                {
                    status = "blocked";
                    detail = requiresEscalation && !operatorApproved
                        ? "blocked due to unresolved ambiguity"
                        : "blocked due to missing operator plan approval";
                }

                stages.Add(new WorkflowStageResult
                {
                    Stage = stage,
                    Status = status,
                    Detail = detail
                });
            }

            return new WorkflowRecord
            {
                Command = command,
                Mode = mode,
                RequestedAutonomy = requestedAutonomy,
                IsNonTrivial = isNonTrivial,
                AmbiguityScore = ambiguityScore,
                AmbiguityThreshold = ambiguityThreshold,
                RequiresOperatorEscalation = requiresEscalation,
                OperatorApproved = operatorApproved,
                RequiresPlanApproval = requiresPlanApproval,
                OperatorApprovedPlan = planApproved,
                CanProceed = canProceed,
                Stages = stages
            };
        }

        public static WorkflowRecord BuildForInit(string mode, string requestedAutonomy, double ambiguityScore, double ambiguityThreshold, bool operatorApproved, bool operatorApprovedPlan = true)
        {
            return BuildForCommand("init", mode, requestedAutonomy, ambiguityScore, ambiguityThreshold, operatorApproved, isNonTrivial: true, operatorApprovedPlan: operatorApprovedPlan);
        }

        public static void WriteWorkflowRecord(string path, WorkflowRecord record)
        {
            var fullPath = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? Directory.GetCurrentDirectory());
            var opts = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                WriteIndented = true
            };
            File.WriteAllText(fullPath, JsonSerializer.Serialize(record, opts));
        }
    }
}
