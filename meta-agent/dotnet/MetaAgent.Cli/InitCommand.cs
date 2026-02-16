using System;
using System.IO;
using MetaAgent.Core;

sealed class InitCommand : ICliCommand
{
    public int Execute(string[] args)
    {
        if (!CommandOptionParser.TryParseInit(args, out var options))
        {
            return 2;
        }

        var targetPath = Path.GetFullPath(options.Target);
        Directory.CreateDirectory(targetPath);
        var artifactDirectory = CliPolicySupport.ResolveArtifactDirectory(options.OutputDirectory, targetPath);

        var policyPath = string.IsNullOrWhiteSpace(options.PolicyPath)
            ? Path.Combine(targetPath, ".meta-agent-policy.json")
            : Path.GetFullPath(options.PolicyPath);
        if (!File.Exists(policyPath))
        {
            if (string.IsNullOrWhiteSpace(options.PolicyPath))
            {
                Console.WriteLine($"Creating default policy at {policyPath}");
                var defaultPolicy = CliPolicySupport.BuildDefaultPolicyFromOperatorInput();
                Generator.CreateDefaultPolicy(policyPath, defaultPolicy);
            }
            else
            {
                Console.Error.WriteLine($"Policy file not found: {policyPath}");
                return 4;
            }
        }

        var policy = CliPolicySupport.LoadPolicyWithMigration(policyPath);
        var requested = string.IsNullOrWhiteSpace(options.RequestedAutonomy) ? policy.AutonomyDefault : options.RequestedAutonomy;
        var mode = CliPolicySupport.ResolveExecutionMode(options.Mode, policy);
        if (!CliPolicySupport.TryEnforceModeAutonomy(isMutatingCommand: true, mode, requested, out var modeAutonomyFailure))
        {
            var blockedPath = string.IsNullOrWhiteSpace(options.DecisionRecordPath)
                ? Path.Combine(artifactDirectory, ".meta-agent-decision.json")
                : Path.GetFullPath(options.DecisionRecordPath);
            PolicyEnforcer.WriteDecisionRecord(blockedPath, CliPolicySupport.BuildCommandDecision("init", false, modeAutonomyFailure, mode, requested));
            Console.WriteLine($"Policy decision record written: {blockedPath}");
            Console.Error.WriteLine($"Mode/autonomy enforcement failed: {modeAutonomyFailure}");
            return 7;
        }

        options.OperatorApprovedHighCost = CliPolicySupport.ResolveHighCostApproval(mode, options.OperatorApprovedHighCost, options.TokensRequested, policy);
        var tokenProfile = CliPolicySupport.EvaluateTokenProfile(policy, mode, options.TokensRequested, options.OperatorApprovedHighCost);
        if (!tokenProfile.Allowed)
        {
            var blockedPath = string.IsNullOrWhiteSpace(options.DecisionRecordPath)
                ? Path.Combine(artifactDirectory, ".meta-agent-decision.json")
                : Path.GetFullPath(options.DecisionRecordPath);
            var blocked = CliPolicySupport.BuildCommandDecision("init", false, tokenProfile.Reason, mode, requested);
            blocked.BudgetProfile = tokenProfile.Profile;
            blocked.BudgetProfileReason = tokenProfile.Reason;
            blocked.Checks.Add(tokenProfile.Check);
            PolicyEnforcer.WriteDecisionRecord(blockedPath, blocked);
            Console.WriteLine($"Policy decision record written: {blockedPath}");
            Console.Error.WriteLine($"Token governance enforcement failed: {tokenProfile.Reason}");
            return 5;
        }

        var triage = CliPolicySupport.ResolveTriage(options.TicketText, options.TicketFile, options.TriageOutputPath, artifactDirectory);
        if (string.Equals(mode, ExecutionMode.AutonomousTicketRunner, StringComparison.OrdinalIgnoreCase))
        {
            if (triage == null)
            {
                var blockedPath = string.IsNullOrWhiteSpace(options.DecisionRecordPath)
                    ? Path.Combine(artifactDirectory, ".meta-agent-decision.json")
                    : Path.GetFullPath(options.DecisionRecordPath);
                PolicyEnforcer.WriteDecisionRecord(blockedPath, CliPolicySupport.BuildCommandDecision("init", false, "autonomous_ticket_runner requires ticket input for triage", mode, requested));
                Console.WriteLine($"Policy decision record written: {blockedPath}");
                Console.Error.WriteLine("Triage required in autonomous_ticket_runner mode. Provide --ticket or --ticket-file.");
                return 9;
            }
            if (!triage.Eligible)
            {
                var blockedPath = string.IsNullOrWhiteSpace(options.DecisionRecordPath)
                    ? Path.Combine(artifactDirectory, ".meta-agent-decision.json")
                    : Path.GetFullPath(options.DecisionRecordPath);
                var blocked = CliPolicySupport.BuildCommandDecision("init", false, $"triage ineligible: {triage.EligibilityReason}", mode, requested);
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
            var blocked = CliPolicySupport.BuildCommandDecision("init", false, safetyReason, mode, requested);
            blocked.Triage = triage;
            PolicyEnforcer.WriteDecisionRecord(blockedPath, blocked);
            Console.WriteLine($"Policy decision record written: {blockedPath}");
            Console.Error.WriteLine($"Safety gate enforcement failed: {safetyReason}");
            return 10;
        }
        var enforceThisCommand = CliPolicySupport.ShouldEnforceCommand(policy, isMutatingCommand: true);
        var ambiguityThreshold = policy.AmbiguityThreshold;
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

        var workflow = WorkflowEngine.BuildForInit(mode, requested, options.AmbiguityScore, ambiguityThreshold, options.OperatorApprovedAmbiguity, options.OperatorApprovedPlan);
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

        var usageState = BudgetUsageStore.Load(policyPath, policy);
        var usedTokens = usageState?.TokensUsed ?? 0;
        var usedTickets = usageState?.TicketsUsed ?? 0;

        var decision = enforceThisCommand
            ? PolicyEnforcer.Evaluate(policy, new PolicyCheckInput
            {
                Command = "init",
                PolicyPath = policyPath,
                RequestedAutonomy = requested,
                TokensRequested = options.TokensRequested,
                TicketsRequested = options.TicketsRequested,
                TokensUsedToday = usedTokens,
                TicketsUsedToday = usedTickets,
                OpenPullRequests = options.OpenPrs,
                WritePaths = new[] { targetPath },
                AbortSignals = options.AbortSignals.ToArray()
            })
            : CliPolicySupport.BuildBypassDecision("init", policyPath, "command_gating=mutating_only");
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
            Console.Error.WriteLine("Policy enforcement failed. Review decision record for failing checks.");
            return 5;
        }

        var resolvedAdrIdPrefix = CliPolicySupport.ResolveAdrIdPrefix(options.AdrIdPrefix, options.TicketText, options.TicketFile);

        Generator.RenderTemplate(
            options.Template,
            targetPath,
            string.IsNullOrWhiteSpace(options.Name) ? Path.GetFileName(targetPath) : options.Name,
            resolvedAdrIdPrefix);
        Console.WriteLine($"Scaffolded template '{options.Template}' at {targetPath}");

        if (usageState != null)
        {
            usageState.TokensUsed += options.TokensRequested;
            usageState.TicketsUsed += options.TicketsRequested;
            BudgetUsageStore.Save(policyPath, policy, usageState);
        }

        return 0;
    }
}
