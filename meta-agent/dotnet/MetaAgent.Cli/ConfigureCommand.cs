using System;
using System.IO;
using MetaAgent.Core;

sealed class ConfigureCommand : ICliCommand
{
    public int Execute(string[] args)
    {
        if (!CommandOptionParser.TryParseConfigure(args, out var options))
        {
            return 2;
        }

        var repoPath = Path.GetFullPath(options.RepoPath);
        if (!Directory.Exists(repoPath))
        {
            Console.Error.WriteLine($"Repository path not found: {repoPath}");
            return 4;
        }

        var artifactDirectory = CliPolicySupport.ResolveArtifactDirectory(options.OutputDirectory, repoPath);
        var policyPath = string.IsNullOrWhiteSpace(options.PolicyPath)
            ? Path.Combine(repoPath, ".meta-agent-policy.json")
            : Path.GetFullPath(options.PolicyPath);

        if (!File.Exists(policyPath))
        {
            Console.WriteLine($"Creating default policy at {policyPath}");
            var defaultPolicy = CliPolicySupport.BuildDefaultPolicyFromOperatorInput();
            Generator.CreateDefaultPolicy(policyPath, defaultPolicy);
        }

        var policy = CliPolicySupport.LoadPolicyWithMigration(policyPath);
        var requested = string.IsNullOrWhiteSpace(options.RequestedAutonomy) ? policy.AutonomyDefault : options.RequestedAutonomy;
        var mode = CliPolicySupport.ResolveExecutionMode(options.Mode, policy);

        if (!CliPolicySupport.TryEnforceModeAutonomy(isMutatingCommand: true, mode, requested, out var modeAutonomyFailure))
        {
            var blockedPath = string.IsNullOrWhiteSpace(options.DecisionRecordPath)
                ? Path.Combine(artifactDirectory, ".meta-agent-decision.json")
                : Path.GetFullPath(options.DecisionRecordPath);
            PolicyEnforcer.WriteDecisionRecord(blockedPath, CliPolicySupport.BuildCommandDecision("configure", false, modeAutonomyFailure, mode, requested));
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
            var blocked = CliPolicySupport.BuildCommandDecision("configure", false, tokenProfile.Reason, mode, requested);
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
                PolicyEnforcer.WriteDecisionRecord(blockedPath, CliPolicySupport.BuildCommandDecision("configure", false, "autonomous_ticket_runner requires ticket input for triage", mode, requested));
                Console.WriteLine($"Policy decision record written: {blockedPath}");
                Console.Error.WriteLine("Triage required in autonomous_ticket_runner mode. Provide --ticket or --ticket-file.");
                return 9;
            }
            if (!triage.Eligible)
            {
                var blockedPath = string.IsNullOrWhiteSpace(options.DecisionRecordPath)
                    ? Path.Combine(artifactDirectory, ".meta-agent-decision.json")
                    : Path.GetFullPath(options.DecisionRecordPath);
                var blocked = CliPolicySupport.BuildCommandDecision("configure", false, $"triage ineligible: {triage.EligibilityReason}", mode, requested);
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
            var blocked = CliPolicySupport.BuildCommandDecision("configure", false, safetyReason, mode, requested);
            blocked.Triage = triage;
            PolicyEnforcer.WriteDecisionRecord(blockedPath, blocked);
            Console.WriteLine($"Policy decision record written: {blockedPath}");
            Console.Error.WriteLine($"Safety gate enforcement failed: {safetyReason}");
            return 10;
        }

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

        var workflow = WorkflowEngine.BuildForCommand("configure", mode, requested, options.AmbiguityScore, ambiguityThreshold, options.OperatorApprovedAmbiguity, isNonTrivial: true, operatorApprovedPlan: options.OperatorApprovedPlan);
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

        var enforceThisCommand = CliPolicySupport.ShouldEnforceCommand(policy, isMutatingCommand: true);
        var usageState = BudgetUsageStore.Load(policyPath, policy);
        var decision = enforceThisCommand
            ? PolicyEnforcer.Evaluate(policy, new PolicyCheckInput
            {
                Command = "configure",
                PolicyPath = policyPath,
                RequestedAutonomy = requested,
                TokensRequested = options.TokensRequested,
                TicketsRequested = options.TicketsRequested,
                TokensUsedToday = usageState?.TokensUsed ?? 0,
                TicketsUsedToday = usageState?.TicketsUsed ?? 0,
                OpenPullRequests = options.OpenPrs,
                WritePaths = new[] { repoPath },
                AbortSignals = options.AbortSignals.ToArray()
            })
            : CliPolicySupport.BuildBypassDecision("configure", policyPath, "command_gating=mutating_only");
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

        if (usageState != null)
        {
            usageState.TokensUsed += options.TokensRequested;
            usageState.TicketsUsed += options.TicketsRequested;
            BudgetUsageStore.Save(policyPath, policy, usageState);
        }

        Console.WriteLine($"Configured meta-agent governance for existing repository: {repoPath}");
        return 0;
    }
}
