using System;
using System.Diagnostics;
using System.IO;
using MetaAgent.Core;
using Xunit;

[Collection("CliProcessCollection")]
public class TemplateScaffoldPathSafetyTests
{
    [Theory]
    [InlineData("dotnet")]
    [InlineData("node")]
    [InlineData("generic")]
    [InlineData("powershell")]
    public void Init_TemplateScaffold_KeepsStructurizrOutputScoped_AndAvoidsRootBuildLeak(string template)
    {
        var repoRoot = FindRepoRoot();
        var layout = TemplateLayout.LoadFromRepositoryOrDefault(repoRoot).ResolveForTemplate(template);
        var targetDir = CreateTempDir();

        var result = RunCli(
            repoRoot,
            "init",
            "--template", template,
            "--target", targetDir,
            "--name", $"demo-{template}",
            "--requested-autonomy", "A1",
            "--tokens-requested", "10",
            "--tickets-requested", "1",
            "--open-prs", "0");

        Assert.Equal(0, result.ExitCode);

        var workflowPath = Path.Combine(targetDir, ".github", "workflows", "architecture-docs.yml");
        var gitlabPath = Path.Combine(targetDir, ".gitlab-ci.yml");
        var workspacePath = CombineRelative(targetDir, layout.WorkspaceDsl);

        Assert.True(File.Exists(workflowPath), "architecture-docs workflow should be scaffolded");
        Assert.True(File.Exists(gitlabPath), ".gitlab-ci should be scaffolded");
        Assert.True(File.Exists(workspacePath), "Structurizr workspace should be scaffolded");

        var workflow = File.ReadAllText(workflowPath);
        Assert.Contains($"{layout.ArchitectureRoot}/build/site", workflow, StringComparison.Ordinal);
        Assert.Contains("run: python3 ./scripts/verify-architecture.py", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain($"${{PWD}}/{layout.ArchitectureRoot}:/var/model", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("path: build/site", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("/workspace", workflow, StringComparison.Ordinal);

        var gitlab = File.ReadAllText(gitlabPath);
        Assert.Contains($"{layout.ArchitectureRoot}/build/site", gitlab, StringComparison.Ordinal);
        Assert.Contains("python3 ./scripts/verify-architecture.py", gitlab, StringComparison.Ordinal);
        Assert.DoesNotContain($"cd {layout.ArchitectureRoot}", gitlab, StringComparison.Ordinal);
        Assert.DoesNotContain("\n      - build/site", gitlab, StringComparison.Ordinal);

        Assert.False(Directory.Exists(Path.Combine(targetDir, "build")), "scaffold should not create a root-level build directory");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 10; i++)
        {
            var candidate = Path.Combine(dir.FullName, "meta-agent", "dotnet", "MetaAgent.Cli", "MetaAgent.Cli.csproj");
            if (File.Exists(candidate))
            {
                return dir.FullName;
            }

            if (dir.Parent == null)
            {
                break;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Repository root not found");
    }

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static string CombineRelative(string basePath, string relativePath)
    {
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(basePath, normalized);
    }

    private static CliRunResult RunCli(string repoRoot, params string[] cliArgs)
    {
        var psi = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        psi.ArgumentList.Add("run");
        psi.ArgumentList.Add("--project");
        psi.ArgumentList.Add("./meta-agent/dotnet/MetaAgent.Cli");
        psi.ArgumentList.Add("--");
        psi.Environment["META_AGENT_NONINTERACTIVE"] = "1";

        foreach (var arg in cliArgs)
        {
            psi.ArgumentList.Add(arg);
        }

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start CLI process");
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit(120000);

        return new CliRunResult(process.ExitCode, stdout, stderr);
    }

    private sealed record CliRunResult(int ExitCode, string StdOut, string StdErr);
}
