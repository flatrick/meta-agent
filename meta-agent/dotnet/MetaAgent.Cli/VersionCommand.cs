using System;
using System.IO;
using System.Reflection;
using MetaAgent.Core;

sealed class VersionCommand : ICliCommand
{
    public int Execute(string[] args)
    {
        var outputDirectory = ResolveOutputDirectory(args);
        var decisionPath = CliPolicySupport.ResolveDecisionRecordPath(args, outputDirectory);
        PolicyEnforcer.WriteDecisionRecord(decisionPath, CliPolicySupport.BuildCommandDecision("version", true, "version command", ExecutionMode.Hybrid, "A1"));
        Console.WriteLine($"Policy decision record written: {decisionPath}");
        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
            ?? "1.0.2";
        Console.WriteLine($"meta-agent (dotnet) {version}");
        return 0;
    }

    private static string ResolveOutputDirectory(string[] args)
    {
        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "--output" && i + 1 < args.Length)
            {
                return CliPolicySupport.ResolveArtifactDirectory(args[i + 1], Directory.GetCurrentDirectory());
            }
        }

        return Directory.GetCurrentDirectory();
    }
}
