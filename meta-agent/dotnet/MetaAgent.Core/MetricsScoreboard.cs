using System;
using System.IO;
using System.Text.Json;

namespace MetaAgent.Core
{
    public sealed class MetricsScoreboardRecord
    {
        public string Version { get; set; } = "1";
        public string LastUpdatedUtc { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        public int TotalRuns { get; set; }
        public int SuccessfulRuns { get; set; }
        public int FailedRuns { get; set; }
        public int ReworkRuns { get; set; }
        public int ClarificationRuns { get; set; }
        public int TotalTokensRequested { get; set; }
        public int DefectLeakageIncidents { get; set; }
        public int TotalAcceptedSolutions { get; set; }
        public int TotalRunsAtAcceptance { get; set; }

        // Internal state used to derive "time to accepted solution" and leakage proxy.
        public int RunsSinceLastAccepted { get; set; }
        public bool PendingPostChangeValidation { get; set; }

        public double SuccessRate { get; set; }
        public double ReworkRate { get; set; }
        public double ClarificationRate { get; set; }
        public double TokenCostPerSuccess { get; set; }
        public double TimeToAcceptedSolution { get; set; }
    }

    public sealed class MetricsUpdate
    {
        public string Command { get; set; } = string.Empty;
        public int ExitCode { get; set; }
        public int TokensRequested { get; set; }
    }

    public static class MetricsScoreboard
    {
        private static readonly JsonSerializerOptions JsonReadOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        private static readonly JsonSerializerOptions JsonWriteOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };

        public static MetricsScoreboardRecord Update(string path, MetricsUpdate update)
        {
            var scoreboard = Load(path);
            Apply(scoreboard, update);
            Save(path, scoreboard);
            return scoreboard;
        }

        public static MetricsScoreboardRecord Load(string path)
        {
            var fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath))
            {
                return new MetricsScoreboardRecord();
            }

            var txt = File.ReadAllText(fullPath);
            return JsonSerializer.Deserialize<MetricsScoreboardRecord>(txt, JsonReadOptions)
                ?? new MetricsScoreboardRecord();
        }

        public static void Save(string path, MetricsScoreboardRecord record)
        {
            var fullPath = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? Directory.GetCurrentDirectory());
            File.WriteAllText(fullPath, JsonSerializer.Serialize(record, JsonWriteOptions));
        }

        private static void Apply(MetricsScoreboardRecord board, MetricsUpdate update)
        {
            board.TotalRuns++;
            board.RunsSinceLastAccepted++;
            board.TotalTokensRequested += Math.Max(0, update.TokensRequested);

            var success = update.ExitCode == 0;
            if (success)
            {
                board.SuccessfulRuns++;
                board.TotalAcceptedSolutions++;
                board.TotalRunsAtAcceptance += board.RunsSinceLastAccepted;
                board.RunsSinceLastAccepted = 0;
            }
            else
            {
                board.FailedRuns++;
                board.ReworkRuns++;
            }

            if (update.ExitCode == 6 || update.ExitCode == 9)
            {
                board.ClarificationRuns++;
            }

            // Leakage proxy: successful mutating change followed by failed validate.
            if (string.Equals(update.Command, "init", StringComparison.OrdinalIgnoreCase) && success)
            {
                board.PendingPostChangeValidation = true;
            }
            else if (string.Equals(update.Command, "validate", StringComparison.OrdinalIgnoreCase) && board.PendingPostChangeValidation)
            {
                if (!success)
                {
                    board.DefectLeakageIncidents++;
                }
                board.PendingPostChangeValidation = false;
            }

            board.LastUpdatedUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            board.SuccessRate = board.TotalRuns == 0 ? 0 : (double)board.SuccessfulRuns / board.TotalRuns;
            board.ReworkRate = board.TotalRuns == 0 ? 0 : (double)board.ReworkRuns / board.TotalRuns;
            board.ClarificationRate = board.TotalRuns == 0 ? 0 : (double)board.ClarificationRuns / board.TotalRuns;
            board.TokenCostPerSuccess = board.SuccessfulRuns == 0 ? 0 : (double)board.TotalTokensRequested / board.SuccessfulRuns;
            board.TimeToAcceptedSolution = board.TotalAcceptedSolutions == 0 ? 0 : (double)board.TotalRunsAtAcceptance / board.TotalAcceptedSolutions;
        }
    }
}
