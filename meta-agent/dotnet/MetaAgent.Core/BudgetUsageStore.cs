using System;
using System.IO;
using System.Text.Json;

namespace MetaAgent.Core
{
    public sealed class BudgetUsageState
    {
        public string DateUtc { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");
        public int TokensUsed { get; set; }
        public int TicketsUsed { get; set; }
    }

    public static class BudgetUsageStore
    {
        private static readonly JsonSerializerOptions JsonReadOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        private static readonly JsonSerializerOptions JsonWriteOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };

        public static BudgetUsageState? Load(string policyPath, Policy policy)
        {
            if (!string.Equals(policy.BudgetAccounting.Mode, "persistent_daily", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var statePath = ResolvePath(policyPath, policy);
            if (!File.Exists(statePath))
            {
                return new BudgetUsageState();
            }

            var txt = File.ReadAllText(statePath);
            var state = JsonSerializer.Deserialize<BudgetUsageState>(txt, JsonReadOptions) ?? new BudgetUsageState();
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            if (!string.Equals(state.DateUtc, today, StringComparison.Ordinal))
            {
                return new BudgetUsageState { DateUtc = today };
            }

            return state;
        }

        public static void Save(string policyPath, Policy policy, BudgetUsageState state)
        {
            if (!string.Equals(policy.BudgetAccounting.Mode, "persistent_daily", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var statePath = ResolvePath(policyPath, policy);
            Directory.CreateDirectory(Path.GetDirectoryName(statePath) ?? Directory.GetCurrentDirectory());
            File.WriteAllText(statePath, JsonSerializer.Serialize(state, JsonWriteOptions));
        }

        static string ResolvePath(string policyPath, Policy policy)
        {
            var configured = string.IsNullOrWhiteSpace(policy.BudgetAccounting.StateFile)
                ? ".meta-agent-budget-state.json"
                : policy.BudgetAccounting.StateFile;

            if (Path.IsPathRooted(configured))
            {
                return configured;
            }

            var root = Path.GetDirectoryName(Path.GetFullPath(policyPath)) ?? Directory.GetCurrentDirectory();
            return Path.GetFullPath(Path.Combine(root, configured));
        }
    }
}
