using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MetaAgent.Core
{
    public class Budgets
    {
        public int TokensPerDay { get; set; } = 50000;
        public int TicketsPerDay { get; set; } = 10;
        public int MaxConcurrentPrs { get; set; } = 3;
    }

    public class ChangeBoundaries
    {
        public string[] AllowedPaths { get; set; } = new[] { "**" };
        public string[] DisallowedPaths { get; set; } = Array.Empty<string>();
    }

    public class BudgetAccounting
    {
        public string Mode { get; set; } = "per_invocation";
        public string StateFile { get; set; } = ".meta-agent-budget-state.json";
    }

    public class AutonomousTokenGovernance
    {
        public int HardCapTokensPerRun { get; set; } = 5000;
    }

    public class InteractiveTokenGovernance
    {
        public int WarningTokensPerRun { get; set; } = 2000;
        public bool RequireOperatorApproval { get; set; } = true;
    }

    public class TokenGovernance
    {
        public AutonomousTokenGovernance AutonomousTicketRunner { get; set; } = new AutonomousTokenGovernance();
        public InteractiveTokenGovernance InteractiveIde { get; set; } = new InteractiveTokenGovernance();
    }

    public class IntegrationSettings
    {
        public string[] TicketContextEnvVars { get; set; } = new[] { "WORK_ITEM_ID", "META_AGENT_TICKET_ID" };
    }

    public class Policy
    {
        public const int CurrentPolicyVersion = 1;

        internal static readonly JsonSerializerOptions ReadJsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        internal static readonly JsonSerializerOptions WriteJsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };

        public int PolicyVersion { get; set; } = CurrentPolicyVersion;
        public string? Name { get; set; } = "default-policy";
        public string DefaultMode { get; set; } = ExecutionMode.InteractiveIde;
        public string AutonomyDefault { get; set; } = "A1";
        public double AmbiguityThreshold { get; set; } = 0.6;
        public string CommandGating { get; set; } = "mutating_only";
        public Budgets Budgets { get; set; } = new Budgets();
        public BudgetAccounting BudgetAccounting { get; set; } = new BudgetAccounting();
        public TokenGovernance TokenGovernance { get; set; } = new TokenGovernance();
        public IntegrationSettings Integrations { get; set; } = new IntegrationSettings();
        public ChangeBoundaries ChangeBoundaries { get; set; } = new ChangeBoundaries();
        public string[] AbortConditions { get; set; } = Array.Empty<string>();

        public static Policy LoadFromFile(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException(path);
            var txt = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Policy>(txt, ReadJsonOptions) ?? new Policy();
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this, WriteJsonOptions);
        }
    }
}
