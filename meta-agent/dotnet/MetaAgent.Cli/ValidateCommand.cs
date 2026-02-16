using System;
using System.IO;
using MetaAgent.Core;

sealed class ValidateCommand : ICliCommand
{
    public int Execute(string[] args)
    {
        if (!CommandOptionParser.TryParseValidate(args, out var options))
        {
            return 2;
        }

        var policyPath = Path.GetFullPath(options.PolicyPath);
        var policyDirectory = Path.GetDirectoryName(policyPath) ?? Directory.GetCurrentDirectory();
        var artifactDirectory = CliPolicySupport.ResolveArtifactDirectory(options.OutputDirectory, policyDirectory);
        var loadedPolicy = CliPolicySupport.LoadPolicyWithMigration(policyPath);
        var requested = string.IsNullOrWhiteSpace(options.RequestedAutonomy) ? loadedPolicy.AutonomyDefault : options.RequestedAutonomy;
        var mode = CliPolicySupport.ResolveExecutionMode(options.Mode, loadedPolicy);
        var tokenProfile = CliPolicySupport.EvaluateTokenProfile(loadedPolicy, mode, tokensRequested: 0, operatorApprovedHighCost: false);
        if (!CliPolicySupport.TryEnforceModeAutonomy(isMutatingCommand: false, mode, requested, out var modeAutonomyFailure))
        {
            var blockedPath = string.IsNullOrWhiteSpace(options.DecisionRecordPath)
                ? Path.Combine(artifactDirectory, ".meta-agent-decision.json")
                : Path.GetFullPath(options.DecisionRecordPath);
            PolicyEnforcer.WriteDecisionRecord(blockedPath, CliPolicySupport.BuildCommandDecision("validate", false, modeAutonomyFailure, mode, requested));
            Console.WriteLine($"Policy decision record written: {blockedPath}");
            Console.Error.WriteLine($"Mode/autonomy enforcement failed: {modeAutonomyFailure}");
            return 7;
        }

        var triage = CliPolicySupport.ResolveTriage(options.TicketText, options.TicketFile, options.TriageOutputPath, artifactDirectory);
        if (string.Equals(mode, ExecutionMode.AutonomousTicketRunner, StringComparison.OrdinalIgnoreCase))
        {
            if (triage == null)
            {
                var blockedPath = string.IsNullOrWhiteSpace(options.DecisionRecordPath)
                    ? Path.Combine(artifactDirectory, ".meta-agent-decision.json")
                    : Path.GetFullPath(options.DecisionRecordPath);
                PolicyEnforcer.WriteDecisionRecord(blockedPath, CliPolicySupport.BuildCommandDecision("validate", false, "autonomous_ticket_runner requires ticket input for triage", mode, requested));
                Console.WriteLine($"Policy decision record written: {blockedPath}");
                Console.Error.WriteLine("Triage required in autonomous_ticket_runner mode. Provide --ticket or --ticket-file.");
                return 9;
            }
            if (!triage.Eligible)
            {
                var blockedPath = string.IsNullOrWhiteSpace(options.DecisionRecordPath)
                    ? Path.Combine(artifactDirectory, ".meta-agent-decision.json")
                    : Path.GetFullPath(options.DecisionRecordPath);
                var blocked = CliPolicySupport.BuildCommandDecision("validate", false, $"triage ineligible: {triage.EligibilityReason}", mode, requested);
                blocked.Triage = triage;
                PolicyEnforcer.WriteDecisionRecord(blockedPath, blocked);
                Console.WriteLine($"Policy decision record written: {blockedPath}");
                Console.Error.WriteLine($"Triage ineligible for autonomous mode: {triage.EligibilityReason}");
                return 9;
            }
        }

        if (!CliPolicySupport.TryEnforceSafetyGates(triage, options.OperatorApprovedSafety, options.ValidatedMethods, out var safetyReason))
        {
            var blockedPath = string.IsNullOrWhiteSpace(options.DecisionRecordPath)
                ? Path.Combine(artifactDirectory, ".meta-agent-decision.json")
                : Path.GetFullPath(options.DecisionRecordPath);
            var blocked = CliPolicySupport.BuildCommandDecision("validate", false, safetyReason, mode, requested);
            blocked.Triage = triage;
            PolicyEnforcer.WriteDecisionRecord(blockedPath, blocked);
            Console.WriteLine($"Policy decision record written: {blockedPath}");
            Console.Error.WriteLine($"Safety gate enforcement failed: {safetyReason}");
            return 10;
        }
        Console.WriteLine("Policy loaded:");
        Console.WriteLine(loadedPolicy.ToJson());

        var ambiguityThreshold = loadedPolicy.AmbiguityThreshold;
        if (options.AmbiguityScore > ambiguityThreshold && !options.OperatorApprovedAmbiguity && CliPolicySupport.ShouldPromptOperator())
        {
            Console.Write($"Ambiguity score {options.AmbiguityScore:0.00} exceeds threshold {ambiguityThreshold:0.00}. Continue? [y/N]: ");
            var answer = Console.ReadLine();
            options.OperatorApprovedAmbiguity = string.Equals(answer?.Trim(), "y", StringComparison.OrdinalIgnoreCase)
                || string.Equals(answer?.Trim(), "yes", StringComparison.OrdinalIgnoreCase);
        }
        var requireInteractivePlanApproval = string.Equals(mode, ExecutionMode.InteractiveIde, StringComparison.OrdinalIgnoreCase)
            && CliPolicySupport.ShouldPromptOperator();
        options.OperatorApprovedPlan = requireInteractivePlanApproval
            ? CliPolicySupport.ResolveOperatorPlanApproval(mode, options.OperatorApprovedPlan)
            : true;

        var workflow = WorkflowEngine.BuildForCommand("validate", mode, requested, options.AmbiguityScore, ambiguityThreshold, options.OperatorApprovedAmbiguity, isNonTrivial: true, operatorApprovedPlan: options.OperatorApprovedPlan);
        workflow.Triage = triage;
        var workflowPath = string.IsNullOrWhiteSpace(options.WorkflowRecordPath)
            ? Path.Combine(artifactDirectory, ".meta-agent-workflow.json")
            : Path.GetFullPath(options.WorkflowRecordPath);
        WorkflowEngine.WriteWorkflowRecord(workflowPath, workflow);
        Console.WriteLine($"Workflow record written: {workflowPath}");

        if (!workflow.CanProceed)
        {
            if (workflow.RequiresPlanApproval && !workflow.OperatorApprovedPlan)
            {
                Console.Error.WriteLine("Workflow blocked due to missing operator plan approval.");
                return 11;
            }
            Console.Error.WriteLine("Workflow blocked due to unresolved ambiguity. Operator approval required.");
            return 6;
        }

        var enforceThisCommand = CliPolicySupport.ShouldEnforceCommand(loadedPolicy, isMutatingCommand: false);
        var usageState = BudgetUsageStore.Load(policyPath, loadedPolicy);
        var decision = enforceThisCommand
            ? PolicyEnforcer.Evaluate(loadedPolicy, new PolicyCheckInput
            {
                Command = "validate",
                PolicyPath = policyPath,
                RequestedAutonomy = requested,
                TokensRequested = 0,
                TicketsRequested = 0,
                TokensUsedToday = usageState?.TokensUsed ?? 0,
                TicketsUsedToday = usageState?.TicketsUsed ?? 0,
                OpenPullRequests = 0,
                WritePaths = Array.Empty<string>(),
                AbortSignals = options.AbortSignals.ToArray()
            })
            : CliPolicySupport.BuildBypassDecision("validate", policyPath, "command_gating=mutating_only");
        decision.Mode = mode;
        decision.RequestedAutonomy = requested;
        decision.BudgetProfile = tokenProfile.Profile;
        decision.BudgetProfileReason = tokenProfile.Reason;
        decision.Checks.Insert(1, tokenProfile.Check);
        decision.Triage = triage;
        var decisionPath = string.IsNullOrWhiteSpace(options.DecisionRecordPath)
            ? Path.Combine(artifactDirectory, ".meta-agent-decision.json")
            : Path.GetFullPath(options.DecisionRecordPath);
        PolicyEnforcer.WriteDecisionRecord(decisionPath, decision);
        Console.WriteLine($"Policy decision record written: {decisionPath}");

        if (!decision.Allowed)
        {
            Console.WriteLine("Policy enforcement FAILED:");
            foreach (var check in decision.Checks)
            {
                if (!check.Passed) Console.WriteLine($" - {check.Check}: {check.Detail}");
            }
            return 5;
        }

        try
        {
            var (isValid, errors) = PolicySchemaValidator.ValidateFile(policyPath);
            if (isValid)
            {
                Console.WriteLine("Policy conforms to schema.");
            }
            else
            {
                Console.WriteLine("Policy schema validation FAILED:");
                foreach (var error in errors) Console.WriteLine($" - {error}");
                return 4;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Schema validation error: {ex.Message}");
        }

        return 0;
    }
}
