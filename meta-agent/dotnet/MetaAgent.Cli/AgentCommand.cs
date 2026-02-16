using System;
using System.IO;
using MetaAgent.Core;

sealed class AgentCommand : ICliCommand
{
    public int Execute(string[] args)
    {
        var options = CommandOptionParser.ParseAgent(args);
        var outputDirectory = CliPolicySupport.ResolveArtifactDirectory(options.OutputDirectory, Directory.GetCurrentDirectory());
        var decisionPath = CliPolicySupport.ResolveDecisionRecordPath(args, outputDirectory);

        var agentsDir = CliPathDiscovery.FindAgentsDir();
        if (options.Subcommand == "list")
        {
            foreach (var file in Directory.EnumerateFiles(agentsDir, "*.json"))
            {
                Console.WriteLine(Path.GetFileNameWithoutExtension(file));
            }
            PolicyEnforcer.WriteDecisionRecord(decisionPath, CliPolicySupport.BuildCommandDecision("agent:list", true, "listed agent manifests", ExecutionMode.Hybrid, "A1"));
            Console.WriteLine($"Policy decision record written: {decisionPath}");
            return 0;
        }

        if (options.Subcommand == "describe")
        {
            if (string.IsNullOrWhiteSpace(options.AgentId))
            {
                PolicyEnforcer.WriteDecisionRecord(decisionPath, CliPolicySupport.BuildCommandDecision("agent:describe", false, "missing agent id", ExecutionMode.Hybrid, "A1"));
                Console.WriteLine($"Policy decision record written: {decisionPath}");
                Console.Error.WriteLine("Usage: agent describe <id>");
                return 2;
            }

            var manifestPath = Path.Combine(agentsDir, options.AgentId + ".json");
            if (!File.Exists(manifestPath))
            {
                PolicyEnforcer.WriteDecisionRecord(decisionPath, CliPolicySupport.BuildCommandDecision("agent:describe", false, $"agent not found: {options.AgentId}", ExecutionMode.Hybrid, "A1"));
                Console.WriteLine($"Policy decision record written: {decisionPath}");
                Console.Error.WriteLine($"Agent not found: {options.AgentId}");
                return 2;
            }

            Console.WriteLine(File.ReadAllText(manifestPath));
            PolicyEnforcer.WriteDecisionRecord(decisionPath, CliPolicySupport.BuildCommandDecision("agent:describe", true, $"described agent: {options.AgentId}", ExecutionMode.Hybrid, "A1"));
            Console.WriteLine($"Policy decision record written: {decisionPath}");
            return 0;
        }

        PolicyEnforcer.WriteDecisionRecord(decisionPath, CliPolicySupport.BuildCommandDecision("agent", false, $"unknown subcommand: {options.Subcommand}", ExecutionMode.Hybrid, "A1"));
        Console.WriteLine($"Policy decision record written: {decisionPath}");
        Console.Error.WriteLine($"Unknown agent subcommand: {options.Subcommand}");
        return 2;
    }
}
