using System.Collections.Generic;

sealed class InitCommandOptions
{
    public string Template { get; set; } = "dotnet";
    public string Target { get; set; } = ".";
    public string Name { get; set; } = string.Empty;
    public string? AdrIdPrefix { get; set; }
    public string? PolicyPath { get; set; }
    public string? DecisionRecordPath { get; set; }
    public string? RequestedAutonomy { get; set; }
    public string? Mode { get; set; }
    public string? TicketText { get; set; }
    public string? TicketFile { get; set; }
    public string? TriageOutputPath { get; set; }
    public bool OperatorApprovedSafety { get; set; }
    public List<string> ValidatedMethods { get; } = new List<string>();
    public int TokensRequested { get; set; }
    public int TicketsRequested { get; set; } = 1;
    public int OpenPrs { get; set; }
    public double AmbiguityScore { get; set; } = 0.1;
    public bool OperatorApprovedAmbiguity { get; set; }
    public bool OperatorApprovedPlan { get; set; }
    public bool OperatorApprovedHighCost { get; set; }
    public string? WorkflowRecordPath { get; set; }
    public string? OutputDirectory { get; set; }
    public List<string> AbortSignals { get; } = new List<string>();
}

sealed class ValidateCommandOptions
{
    public string PolicyPath { get; set; } = ".meta-agent-policy.json";
    public string? DecisionRecordPath { get; set; }
    public string? WorkflowRecordPath { get; set; }
    public string? RequestedAutonomy { get; set; }
    public string? Mode { get; set; }
    public string? TicketText { get; set; }
    public string? TicketFile { get; set; }
    public string? TriageOutputPath { get; set; }
    public bool OperatorApprovedSafety { get; set; }
    public List<string> ValidatedMethods { get; } = new List<string>();
    public double AmbiguityScore { get; set; } = 0.1;
    public bool OperatorApprovedAmbiguity { get; set; }
    public bool OperatorApprovedPlan { get; set; }
    public bool OperatorApprovedHighCost { get; set; }
    public string? OutputDirectory { get; set; }
    public List<string> AbortSignals { get; } = new List<string>();
}

sealed class ConfigureCommandOptions
{
    public string RepoPath { get; set; } = ".";
    public string? PolicyPath { get; set; }
    public string? DecisionRecordPath { get; set; }
    public string? WorkflowRecordPath { get; set; }
    public string? RequestedAutonomy { get; set; }
    public string? Mode { get; set; }
    public string? TicketText { get; set; }
    public string? TicketFile { get; set; }
    public string? TriageOutputPath { get; set; }
    public bool OperatorApprovedSafety { get; set; }
    public List<string> ValidatedMethods { get; } = new List<string>();
    public int TokensRequested { get; set; }
    public int TicketsRequested { get; set; }
    public int OpenPrs { get; set; }
    public double AmbiguityScore { get; set; } = 0.1;
    public bool OperatorApprovedAmbiguity { get; set; }
    public bool OperatorApprovedPlan { get; set; }
    public bool OperatorApprovedHighCost { get; set; }
    public string? OutputDirectory { get; set; }
    public List<string> AbortSignals { get; } = new List<string>();
}

sealed class TriageCommandOptions
{
    public string? TicketText { get; set; }
    public string? TicketFile { get; set; }
    public string? OutputPath { get; set; }
    public bool HasTicketText { get; set; }
    public bool HasTicketFile { get; set; }
}

sealed class AgentCommandOptions
{
    public string Subcommand { get; set; } = "list";
    public string? AgentId { get; set; }
    public string? OutputDirectory { get; set; }
}

static class CommandOptionParser
{
    public static bool TryParseInit(string[] args, out InitCommandOptions options)
    {
        options = new InitCommandOptions();
        for (int i = 1; i < args.Length; i++)
        {
            var a = args[i];
            if (a == "--template" && i + 1 < args.Length) options.Template = args[++i];
            else if (a == "--target" && i + 1 < args.Length) options.Target = args[++i];
            else if (a == "--name" && i + 1 < args.Length) options.Name = args[++i];
            else if (a == "--adr-id-prefix" && i + 1 < args.Length) options.AdrIdPrefix = args[++i];
            else if (a == "--policy" && i + 1 < args.Length) options.PolicyPath = args[++i];
            else if (a == "--decision-record" && i + 1 < args.Length) options.DecisionRecordPath = args[++i];
            else if (a == "--requested-autonomy" && i + 1 < args.Length) options.RequestedAutonomy = args[++i];
            else if (a == "--mode" && i + 1 < args.Length) options.Mode = args[++i];
            else if (a == "--ticket" && i + 1 < args.Length) options.TicketText = args[++i];
            else if (a == "--ticket-file" && i + 1 < args.Length) options.TicketFile = args[++i];
            else if (a == "--triage-output" && i + 1 < args.Length) options.TriageOutputPath = args[++i];
            else if (a == "--operator-approved-safety") options.OperatorApprovedSafety = true;
            else if (a == "--validated-method" && i + 1 < args.Length) options.ValidatedMethods.Add(args[++i]);
            else if (a == "--tokens-requested" && i + 1 < args.Length)
            {
                if (!CliParsing.TryParseNonNegativeInt(args[++i], "--tokens-requested", out var parsed))
                {
                    return false;
                }
                options.TokensRequested = parsed;
            }
            else if (a == "--tickets-requested" && i + 1 < args.Length)
            {
                if (!CliParsing.TryParseNonNegativeInt(args[++i], "--tickets-requested", out var parsed))
                {
                    return false;
                }
                options.TicketsRequested = parsed;
            }
            else if (a == "--open-prs" && i + 1 < args.Length)
            {
                if (!CliParsing.TryParseNonNegativeInt(args[++i], "--open-prs", out var parsed))
                {
                    return false;
                }
                options.OpenPrs = parsed;
            }
            else if (a == "--ambiguity-score" && i + 1 < args.Length)
            {
                if (!CliParsing.TryParseUnitDouble(args[++i], "--ambiguity-score", out var parsed))
                {
                    return false;
                }
                options.AmbiguityScore = parsed;
            }
            else if (a == "--operator-approved-ambiguity") options.OperatorApprovedAmbiguity = true;
            else if (a == "--operator-approved-plan") options.OperatorApprovedPlan = true;
            else if (a == "--operator-approved-high-cost") options.OperatorApprovedHighCost = true;
            else if (a == "--workflow-record" && i + 1 < args.Length) options.WorkflowRecordPath = args[++i];
            else if (a == "--output" && i + 1 < args.Length) options.OutputDirectory = args[++i];
            else if (a == "--abort-signal" && i + 1 < args.Length) options.AbortSignals.Add(args[++i]);
        }

        return true;
    }

    public static bool TryParseValidate(string[] args, out ValidateCommandOptions options)
    {
        options = new ValidateCommandOptions();
        for (int i = 1; i < args.Length; i++)
        {
            var a = args[i];
            if (a == "--policy" && i + 1 < args.Length) options.PolicyPath = args[++i];
            else if (a == "--decision-record" && i + 1 < args.Length) options.DecisionRecordPath = args[++i];
            else if (a == "--workflow-record" && i + 1 < args.Length) options.WorkflowRecordPath = args[++i];
            else if (a == "--requested-autonomy" && i + 1 < args.Length) options.RequestedAutonomy = args[++i];
            else if (a == "--mode" && i + 1 < args.Length) options.Mode = args[++i];
            else if (a == "--ticket" && i + 1 < args.Length) options.TicketText = args[++i];
            else if (a == "--ticket-file" && i + 1 < args.Length) options.TicketFile = args[++i];
            else if (a == "--triage-output" && i + 1 < args.Length) options.TriageOutputPath = args[++i];
            else if (a == "--operator-approved-safety") options.OperatorApprovedSafety = true;
            else if (a == "--validated-method" && i + 1 < args.Length) options.ValidatedMethods.Add(args[++i]);
            else if (a == "--ambiguity-score" && i + 1 < args.Length)
            {
                if (!CliParsing.TryParseUnitDouble(args[++i], "--ambiguity-score", out var parsed))
                {
                    return false;
                }
                options.AmbiguityScore = parsed;
            }
            else if (a == "--operator-approved-ambiguity") options.OperatorApprovedAmbiguity = true;
            else if (a == "--operator-approved-plan") options.OperatorApprovedPlan = true;
            else if (a == "--operator-approved-high-cost") options.OperatorApprovedHighCost = true;
            else if (a == "--output" && i + 1 < args.Length) options.OutputDirectory = args[++i];
            else if (a == "--abort-signal" && i + 1 < args.Length) options.AbortSignals.Add(args[++i]);
        }

        return true;
    }

    public static bool TryParseConfigure(string[] args, out ConfigureCommandOptions options)
    {
        options = new ConfigureCommandOptions();
        for (int i = 1; i < args.Length; i++)
        {
            var a = args[i];
            if (a == "--repo" && i + 1 < args.Length) options.RepoPath = args[++i];
            else if (a == "--policy" && i + 1 < args.Length) options.PolicyPath = args[++i];
            else if (a == "--decision-record" && i + 1 < args.Length) options.DecisionRecordPath = args[++i];
            else if (a == "--workflow-record" && i + 1 < args.Length) options.WorkflowRecordPath = args[++i];
            else if (a == "--requested-autonomy" && i + 1 < args.Length) options.RequestedAutonomy = args[++i];
            else if (a == "--mode" && i + 1 < args.Length) options.Mode = args[++i];
            else if (a == "--ticket" && i + 1 < args.Length) options.TicketText = args[++i];
            else if (a == "--ticket-file" && i + 1 < args.Length) options.TicketFile = args[++i];
            else if (a == "--triage-output" && i + 1 < args.Length) options.TriageOutputPath = args[++i];
            else if (a == "--operator-approved-safety") options.OperatorApprovedSafety = true;
            else if (a == "--validated-method" && i + 1 < args.Length) options.ValidatedMethods.Add(args[++i]);
            else if (a == "--tokens-requested" && i + 1 < args.Length)
            {
                if (!CliParsing.TryParseNonNegativeInt(args[++i], "--tokens-requested", out var parsed))
                {
                    return false;
                }
                options.TokensRequested = parsed;
            }
            else if (a == "--tickets-requested" && i + 1 < args.Length)
            {
                if (!CliParsing.TryParseNonNegativeInt(args[++i], "--tickets-requested", out var parsed))
                {
                    return false;
                }
                options.TicketsRequested = parsed;
            }
            else if (a == "--open-prs" && i + 1 < args.Length)
            {
                if (!CliParsing.TryParseNonNegativeInt(args[++i], "--open-prs", out var parsed))
                {
                    return false;
                }
                options.OpenPrs = parsed;
            }
            else if (a == "--ambiguity-score" && i + 1 < args.Length)
            {
                if (!CliParsing.TryParseUnitDouble(args[++i], "--ambiguity-score", out var parsed))
                {
                    return false;
                }
                options.AmbiguityScore = parsed;
            }
            else if (a == "--operator-approved-ambiguity") options.OperatorApprovedAmbiguity = true;
            else if (a == "--operator-approved-plan") options.OperatorApprovedPlan = true;
            else if (a == "--operator-approved-high-cost") options.OperatorApprovedHighCost = true;
            else if (a == "--output" && i + 1 < args.Length) options.OutputDirectory = args[++i];
            else if (a == "--abort-signal" && i + 1 < args.Length) options.AbortSignals.Add(args[++i]);
        }

        return true;
    }

    public static TriageCommandOptions ParseTriage(string[] args)
    {
        var options = new TriageCommandOptions();
        for (int i = 1; i < args.Length; i++)
        {
            var a = args[i];
            if (a == "--ticket" && i + 1 < args.Length) { options.HasTicketText = true; options.TicketText = args[++i]; }
            else if (a == "--ticket-file" && i + 1 < args.Length) { options.HasTicketFile = true; options.TicketFile = args[++i]; }
            else if (a == "--output" && i + 1 < args.Length) options.OutputPath = args[++i];
        }

        return options;
    }

    public static AgentCommandOptions ParseAgent(string[] args)
    {
        var options = new AgentCommandOptions();
        var positionals = new List<string>();
        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "--decision-record" && i + 1 < args.Length)
            {
                i++;
                continue;
            }
            if (args[i] == "--output" && i + 1 < args.Length)
            {
                options.OutputDirectory = args[++i];
                continue;
            }
            positionals.Add(args[i]);
        }

        if (positionals.Count > 0)
        {
            options.Subcommand = positionals[0].ToLowerInvariant();
        }
        if (positionals.Count > 1)
        {
            options.AgentId = positionals[1];
        }

        return options;
    }
}
