using System;
using System.IO;

namespace MetaAgent.Core
{
    public sealed class RunResultArtifactPaths
    {
        public string ArtifactsDirectory { get; }
        public string DecisionPath { get; }
        public string WorkflowPath { get; }
        public string TriagePath { get; }
        public string RunResultPath { get; }
        public string MetricsPath { get; }

        private RunResultArtifactPaths(
            string artifactsDirectory,
            string decisionPath,
            string workflowPath,
            string triagePath,
            string runResultPath,
            string metricsPath)
        {
            ArtifactsDirectory = artifactsDirectory;
            DecisionPath = decisionPath;
            WorkflowPath = workflowPath;
            TriagePath = triagePath;
            RunResultPath = runResultPath;
            MetricsPath = metricsPath;
        }

        public static RunResultArtifactPaths Resolve(string command, string[] args)
        {
            var artifactsDir = ResolveArtifactDirectory(command, args);
            var decisionPath = ResolveOptionPath(args, "--decision-record", Path.Combine(artifactsDir, ".meta-agent-decision.json"));
            var workflowPath = ResolveOptionPath(args, "--workflow-record", Path.Combine(artifactsDir, ".meta-agent-workflow.json"));
            var triagePath = ResolveTriagePath(command, args, artifactsDir);
            var runResultPath = ResolveOptionPath(args, "--run-result", Path.Combine(artifactsDir, ".meta-agent-run-result.json"));
            var metricsPath = ResolveOptionPath(args, "--metrics-scoreboard", Path.Combine(artifactsDir, ".meta-agent-metrics.json"));

            return new RunResultArtifactPaths(artifactsDir, decisionPath, workflowPath, triagePath, runResultPath, metricsPath);
        }

        private static string ResolveArtifactDirectory(string command, string[] args)
        {
            var explicitOutput = TryResolveOptionPath(args, "--output");
            if (!string.IsNullOrWhiteSpace(explicitOutput))
            {
                if (string.Equals(command, "triage", StringComparison.OrdinalIgnoreCase))
                {
                    var triageOutputPath = Path.GetFullPath(explicitOutput);
                    return Path.GetDirectoryName(triageOutputPath) ?? Directory.GetCurrentDirectory();
                }

                return Path.GetFullPath(explicitOutput);
            }

            if (string.Equals(command, "init", StringComparison.OrdinalIgnoreCase))
            {
                var target = TryResolveOptionPath(args, "--target");
                if (!string.IsNullOrWhiteSpace(target))
                {
                    return Path.GetFullPath(target);
                }
            }

            if (string.Equals(command, "validate", StringComparison.OrdinalIgnoreCase))
            {
                var policy = TryResolveOptionPath(args, "--policy");
                if (!string.IsNullOrWhiteSpace(policy))
                {
                    var fullPolicy = Path.GetFullPath(policy);
                    return Path.GetDirectoryName(fullPolicy) ?? Directory.GetCurrentDirectory();
                }
            }

            if (string.Equals(command, "configure", StringComparison.OrdinalIgnoreCase))
            {
                var repo = TryResolveOptionPath(args, "--repo");
                if (!string.IsNullOrWhiteSpace(repo))
                {
                    return Path.GetFullPath(repo);
                }

                var policy = TryResolveOptionPath(args, "--policy");
                if (!string.IsNullOrWhiteSpace(policy))
                {
                    var fullPolicy = Path.GetFullPath(policy);
                    return Path.GetDirectoryName(fullPolicy) ?? Directory.GetCurrentDirectory();
                }
            }

            return Directory.GetCurrentDirectory();
        }

        private static string? TryResolveOptionPath(string[] args, string optionName)
        {
            for (int i = 1; i < args.Length; i++)
            {
                if (args[i] == optionName && i + 1 < args.Length)
                {
                    return args[i + 1];
                }
            }

            return null;
        }

        private static string ResolveOptionPath(string[] args, string optionName, string fallbackPath)
        {
            for (int i = 1; i < args.Length; i++)
            {
                if (args[i] == optionName && i + 1 < args.Length)
                {
                    return Path.GetFullPath(args[i + 1]);
                }
            }

            return Path.GetFullPath(fallbackPath);
        }

        private static string ResolveTriagePath(string command, string[] args, string artifactsDir)
        {
            if (string.Equals(command, "triage", StringComparison.OrdinalIgnoreCase))
            {
                return ResolveOptionPath(args, "--output", Path.Combine(artifactsDir, ".meta-agent-triage.json"));
            }

            return ResolveOptionPath(args, "--triage-output", Path.Combine(artifactsDir, ".meta-agent-triage.json"));
        }
    }
}
