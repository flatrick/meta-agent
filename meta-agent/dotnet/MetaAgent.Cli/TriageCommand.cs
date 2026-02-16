using System;
using System.IO;
using System.Text.Json;
using MetaAgent.Core;

sealed class TriageCommand : ICliCommand
{
    public int Execute(string[] args)
    {
        var options = CommandOptionParser.ParseTriage(args);

        if (!options.HasTicketText && !options.HasTicketFile)
        {
            Console.Error.WriteLine("Usage: triage --ticket <text> | --ticket-file <path> [--output <path>]");
            return 2;
        }

        if (!string.IsNullOrWhiteSpace(options.TicketFile))
        {
            var path = Path.GetFullPath(options.TicketFile);
            if (!File.Exists(path))
            {
                Console.Error.WriteLine($"Ticket file not found: {path}");
                return 4;
            }
            options.TicketText = File.ReadAllText(path);
        }

        var triage = TriageEngine.Evaluate(options.TicketText ?? string.Empty);
        var outputPath = string.IsNullOrWhiteSpace(options.OutputPath)
            ? Path.Combine(Directory.GetCurrentDirectory(), ".meta-agent-triage.json")
            : Path.GetFullPath(options.OutputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? Directory.GetCurrentDirectory());
        File.WriteAllText(outputPath, JsonSerializer.Serialize(triage, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));
        Console.WriteLine($"Triage result written: {outputPath}");

        var artifactDirectory = Path.GetDirectoryName(outputPath) ?? Directory.GetCurrentDirectory();
        var decisionPath = CliPolicySupport.ResolveDecisionRecordPath(args, artifactDirectory);
        PolicyEnforcer.WriteDecisionRecord(decisionPath, CliPolicySupport.BuildCommandDecision("triage", true, "triage pipeline completed", ExecutionMode.Hybrid, "A1"));
        Console.WriteLine($"Policy decision record written: {decisionPath}");

        return triage.Eligible ? 0 : 8;
    }
}
