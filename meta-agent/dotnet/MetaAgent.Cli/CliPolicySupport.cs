using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Text.Json;
using MetaAgent.Core;

sealed class TokenProfileEvaluation
{
    public bool Allowed { get; set; }
    public string Profile { get; set; } = "default";
    public string Reason { get; set; } = string.Empty;
    public PolicyCheckResult Check { get; set; } = new PolicyCheckResult
    {
        Check = "mode_token_governance",
        Passed = true,
        Detail = "token governance not evaluated"
    };
}

static class CliPolicySupport
{
    static readonly Regex JiraLikeTicketKeyRegex = new(@"\b[A-Z][A-Z0-9]+-\d+\b", RegexOptions.Compiled);

    public static string ResolveArtifactDirectory(string? outputDirectory, string fallbackDirectory)
    {
        var baseDir = string.IsNullOrWhiteSpace(outputDirectory)
            ? fallbackDirectory
            : outputDirectory;
        var full = Path.GetFullPath(baseDir);
        Directory.CreateDirectory(full);
        return full;
    }

    public static Policy BuildDefaultPolicyFromOperatorInput()
    {
        var policy = new Policy();
        if (!ShouldPromptOperator())
        {
            return policy;
        }

        Console.WriteLine("Select default execution mode:");
        Console.WriteLine("  1) interactive_ide (recommended)");
        Console.WriteLine("  2) autonomous_ticket_runner");
        Console.WriteLine("  3) hybrid");
        var modeChoice = ReadChoice("Enter choice [1-3] (default 1): ", "1", "2", "3");
        policy.DefaultMode = modeChoice switch
        {
            "2" => ExecutionMode.AutonomousTicketRunner,
            "3" => ExecutionMode.Hybrid,
            _ => ExecutionMode.InteractiveIde
        };

        Console.WriteLine("Select command gating scope:");
        Console.WriteLine("  1) mutating_only (recommended)");
        Console.WriteLine("  2) all_commands");
        var gating = ReadChoice("Enter choice [1-2] (default 1): ", "1", "2");
        policy.CommandGating = gating == "2" ? "all_commands" : "mutating_only";

        Console.WriteLine("Select budget accounting mode:");
        Console.WriteLine("  1) per_invocation (recommended)");
        Console.WriteLine("  2) persistent_daily");
        var accounting = ReadChoice("Enter choice [1-2] (default 1): ", "1", "2");
        policy.BudgetAccounting.Mode = accounting == "2" ? "persistent_daily" : "per_invocation";
        if (policy.BudgetAccounting.Mode == "persistent_daily")
        {
            Console.Write("State file path (default .meta-agent-budget-state.json): ");
            var input = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(input))
            {
                policy.BudgetAccounting.StateFile = input.Trim();
            }
        }

        return policy;
    }

    public static Policy LoadPolicyWithMigration(string policyPath)
    {
        var result = PolicyMigration.LoadWithMigration(policyPath, persistMigrated: true);
        if (result.Migrated && result.Persisted)
        {
            Console.WriteLine($"Policy migrated from version {result.SourcePolicyVersion} to {result.EffectivePolicyVersion}: {policyPath}");
        }

        return result.Policy;
    }

    public static bool ShouldPromptOperator()
    {
        var forceNonInteractive = string.Equals(Environment.GetEnvironmentVariable("META_AGENT_NONINTERACTIVE"), "1", StringComparison.Ordinal);
        return !forceNonInteractive && !Console.IsInputRedirected;
    }

    public static bool ShouldEnforceCommand(Policy policy, bool isMutatingCommand)
    {
        return PolicyRuntimeGuards.ShouldEnforceCommand(policy, isMutatingCommand);
    }

    public static PolicyDecisionRecord BuildBypassDecision(string command, string policyPath, string detail)
    {
        return PolicyRuntimeGuards.BuildBypassDecision(command, policyPath, detail);
    }

    public static string ResolveDecisionRecordPath(string[] args, string defaultDir)
    {
        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "--decision-record" && i + 1 < args.Length)
            {
                return Path.GetFullPath(args[i + 1]);
            }
        }
        return Path.Combine(defaultDir, ".meta-agent-decision.json");
    }

    public static PolicyDecisionRecord BuildCommandDecision(string command, bool allowed, string detail, string mode, string requestedAutonomy)
    {
        var policyPath = Path.Combine(Directory.GetCurrentDirectory(), ".meta-agent-policy.json");
        return PolicyRuntimeGuards.BuildCommandDecision(command, allowed, detail, mode, requestedAutonomy, policyPath);
    }

    public static TriageResult? ResolveTriage(string? ticketTextArg, string? ticketFileArg, string? triageOutputArg, string defaultDir)
    {
        var hasTicketText = !string.IsNullOrWhiteSpace(ticketTextArg);
        var hasTicketFile = !string.IsNullOrWhiteSpace(ticketFileArg);
        if (!hasTicketText && !hasTicketFile)
        {
            return null;
        }

        var ticketText = ticketTextArg;
        if (hasTicketFile)
        {
            var filePath = Path.GetFullPath(ticketFileArg!);
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Ticket file not found: {filePath}");
            }
            ticketText = File.ReadAllText(filePath);
        }

        var triage = TriageEngine.Evaluate(ticketText ?? string.Empty);
        var triagePath = string.IsNullOrWhiteSpace(triageOutputArg)
            ? Path.Combine(defaultDir, ".meta-agent-triage.json")
            : Path.GetFullPath(triageOutputArg);
        Directory.CreateDirectory(Path.GetDirectoryName(triagePath) ?? Directory.GetCurrentDirectory());
        File.WriteAllText(triagePath, JsonSerializer.Serialize(triage, new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        }));
        Console.WriteLine($"Triage result written: {triagePath}");
        return triage;
    }

    public static string ResolveExecutionMode(string? modeArg, Policy policy)
    {
        var hasTicketContext = false;
        var envVars = policy.Integrations?.TicketContextEnvVars ?? Array.Empty<string>();
        foreach (var envVar in envVars)
        {
            if (string.IsNullOrWhiteSpace(envVar))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(envVar)))
            {
                hasTicketContext = true;
                break;
            }
        }
        var interactiveShell = !Console.IsInputRedirected && !string.Equals(Environment.GetEnvironmentVariable("META_AGENT_NONINTERACTIVE"), "1", StringComparison.Ordinal);
        if (!string.IsNullOrWhiteSpace(modeArg))
        {
            return ExecutionMode.Classify(modeArg, interactiveShell, hasTicketContext);
        }

        if (hasTicketContext)
        {
            return ExecutionMode.AutonomousTicketRunner;
        }

        if (interactiveShell)
        {
            return ExecutionMode.InteractiveIde;
        }

        return ExecutionMode.NormalizeOrFallback(policy.DefaultMode);
    }

    public static bool TryEnforceModeAutonomy(bool isMutatingCommand, string mode, string requestedAutonomy, out string reason)
    {
        return PolicyRuntimeGuards.TryEnforceModeAutonomy(isMutatingCommand, mode, requestedAutonomy, out reason);
    }

    public static bool TryEnforceSafetyGates(TriageResult? triage, bool operatorApprovedSafety, List<string> validatedMethods, out string reason)
    {
        return PolicyRuntimeGuards.TryEnforceSafetyGates(triage, operatorApprovedSafety, validatedMethods, out reason);
    }

    public static bool ResolveOperatorPlanApproval(string mode, bool alreadyApproved)
    {
        if (alreadyApproved || !string.Equals(mode, ExecutionMode.InteractiveIde, StringComparison.OrdinalIgnoreCase))
        {
            return alreadyApproved;
        }

        if (!ShouldPromptOperator())
        {
            return false;
        }

        Console.Write("Plan prepared for this task. Approve plan before execution? [y/N]: ");
        var answer = Console.ReadLine();
        return string.Equals(answer?.Trim(), "y", StringComparison.OrdinalIgnoreCase)
            || string.Equals(answer?.Trim(), "yes", StringComparison.OrdinalIgnoreCase);
    }

    public static bool ResolveHighCostApproval(string mode, bool alreadyApproved, int tokensRequested, Policy policy)
    {
        if (alreadyApproved || !string.Equals(mode, ExecutionMode.InteractiveIde, StringComparison.OrdinalIgnoreCase))
        {
            return alreadyApproved;
        }

        var threshold = policy.TokenGovernance?.InteractiveIde?.WarningTokensPerRun ?? 2000;
        var requiresApproval = policy.TokenGovernance?.InteractiveIde?.RequireOperatorApproval ?? true;
        if (!requiresApproval || tokensRequested <= threshold)
        {
            return alreadyApproved;
        }

        if (!ShouldPromptOperator())
        {
            return false;
        }

        Console.Write($"Requested tokens ({tokensRequested}) exceed interactive warning threshold ({threshold}). Continue? [y/N]: ");
        var answer = Console.ReadLine();
        return string.Equals(answer?.Trim(), "y", StringComparison.OrdinalIgnoreCase)
            || string.Equals(answer?.Trim(), "yes", StringComparison.OrdinalIgnoreCase);
    }

    public static TokenProfileEvaluation EvaluateTokenProfile(Policy policy, string mode, int tokensRequested, bool operatorApprovedHighCost)
    {
        if (string.Equals(mode, ExecutionMode.AutonomousTicketRunner, StringComparison.OrdinalIgnoreCase)
            || string.Equals(mode, ExecutionMode.Hybrid, StringComparison.OrdinalIgnoreCase))
        {
            var hardCap = policy.TokenGovernance?.AutonomousTicketRunner?.HardCapTokensPerRun ?? 5000;
            var passed = tokensRequested <= hardCap;
            return new TokenProfileEvaluation
            {
                Allowed = passed,
                Profile = string.Equals(mode, ExecutionMode.AutonomousTicketRunner, StringComparison.OrdinalIgnoreCase)
                    ? "autonomous_strict_hard_cap"
                    : "hybrid_strict_hard_cap",
                Reason = passed
                    ? $"strict token profile applied; requested={tokensRequested} within hardCap={hardCap}"
                    : $"strict token profile blocked execution; requested={tokensRequested} exceeds hardCap={hardCap}",
                Check = new PolicyCheckResult
                {
                    Check = "mode_token_governance",
                    Passed = passed,
                    Detail = $"mode={mode}, profile=strict_hard_cap, requested={tokensRequested}, hardCap={hardCap}"
                }
            };
        }

        var warning = policy.TokenGovernance?.InteractiveIde?.WarningTokensPerRun ?? 2000;
        var requiresApproval = policy.TokenGovernance?.InteractiveIde?.RequireOperatorApproval ?? true;
        var highCost = tokensRequested > warning;
        var passedInteractive = !requiresApproval || !highCost || operatorApprovedHighCost;

        return new TokenProfileEvaluation
        {
            Allowed = passedInteractive,
            Profile = "interactive_soft_warning",
            Reason = passedInteractive
                ? $"interactive token profile applied; requested={tokensRequested}, warningThreshold={warning}, highCostApproved={operatorApprovedHighCost}"
                : $"interactive token profile blocked execution; requested={tokensRequested} exceeds warningThreshold={warning} and operator approval was not provided",
            Check = new PolicyCheckResult
            {
                Check = "mode_token_governance",
                Passed = passedInteractive,
                Detail = $"mode={mode}, profile=soft_warning, requested={tokensRequested}, warningThreshold={warning}, requiresApproval={requiresApproval}, operatorApprovedHighCost={operatorApprovedHighCost}"
            }
        };
    }

    public static string ResolveAdrIdPrefix(string? explicitPrefix, string? ticketTextArg, string? ticketFileArg)
    {
        if (!string.IsNullOrWhiteSpace(explicitPrefix))
        {
            return explicitPrefix.Trim();
        }

        var ticketText = ticketTextArg;
        if (string.IsNullOrWhiteSpace(ticketText) && !string.IsNullOrWhiteSpace(ticketFileArg))
        {
            var filePath = Path.GetFullPath(ticketFileArg);
            if (File.Exists(filePath))
            {
                ticketText = File.ReadAllText(filePath);
            }
        }

        if (!string.IsNullOrWhiteSpace(ticketText))
        {
            var match = JiraLikeTicketKeyRegex.Match(ticketText);
            if (match.Success)
            {
                return match.Value;
            }
        }

        return "0001";
    }

    static string ReadChoice(string prompt, string defaultValue, params string[] allowedValues)
    {
        Console.Write(prompt);
        var raw = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return defaultValue;
        }

        var trimmed = raw.Trim();
        if (allowedValues.Length == 0)
        {
            return trimmed;
        }

        foreach (var allowed in allowedValues)
        {
            if (trimmed == allowed)
            {
                return trimmed;
            }
        }

        return defaultValue;
    }
}
