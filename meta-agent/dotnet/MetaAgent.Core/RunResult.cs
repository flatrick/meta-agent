using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace MetaAgent.Core
{
    public sealed class AutonomousRunDetails
    {
        public List<string> DecisionLog { get; set; } = new List<string>();
        public List<string> RiskLog { get; set; } = new List<string>();
        public List<string> RollbackNotes { get; set; } = new List<string>();
    }

    public sealed class RunResultRecord
    {
        public string Version { get; set; } = "1";
        public string Command { get; set; } = string.Empty;
        public string TimestampUtc { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        public int ExitCode { get; set; }
        public bool Success { get; set; }
        public string Mode { get; set; } = ExecutionMode.Hybrid;
        public string RequestedAutonomy { get; set; } = "A1";

        public string Summary { get; set; } = string.Empty;
        public List<string> Assumptions { get; set; } = new List<string>();
        public List<string> ExtractedRequirements { get; set; } = new List<string>();
        public string RiskLevel { get; set; } = "unknown";
        public List<string> Plan { get; set; } = new List<string>();
        public List<string> Implementation { get; set; } = new List<string>();
        public List<string> ValidationEvidence { get; set; } = new List<string>();
        public List<string> DocumentationUpdates { get; set; } = new List<string>();
        public List<string> MetricsImpact { get; set; } = new List<string>();
        public List<string> NextActions { get; set; } = new List<string>();

        public Dictionary<string, string> Artifacts { get; set; } = new Dictionary<string, string>();
        public AutonomousRunDetails? Autonomous { get; set; }
    }

    public static class RunResultWriter
    {
        public static void Write(string path, RunResultRecord record)
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
