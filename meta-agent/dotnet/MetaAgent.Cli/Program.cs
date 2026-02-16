using System;
using System.Collections.Generic;

class Program
{
    static readonly Dictionary<string, ICliCommand> Commands = new(StringComparer.OrdinalIgnoreCase)
    {
        ["init"] = new InitCommand(),
        ["configure"] = new ConfigureCommand(),
        ["validate"] = new ValidateCommand(),
        ["triage"] = new TriageCommand(),
        ["agent"] = new AgentCommand(),
        ["version"] = new VersionCommand()
    };

    static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: meta-agent <command> [options]\nCommands: init, configure, validate, triage, agent, version");
            return 1;
        }

        var command = args[0];
        var normalizedCommand = command.ToLowerInvariant();
        if (!Commands.TryGetValue(command, out var handler))
        {
            Console.Error.WriteLine($"Unknown command: {normalizedCommand}");
            return 2;
        }

        try
        {
            var exitCode = handler.Execute(args);
            RunResultOrchestrator.TryWrite(normalizedCommand, args, exitCode);
            return exitCode;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            RunResultOrchestrator.TryWrite(normalizedCommand, args, 3);
            return 3;
        }
    }
}
