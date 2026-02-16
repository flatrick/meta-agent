using System;
using System.IO;
using MetaAgent.Core;
using Xunit;

public class TemplateLayoutTests
{
    [Fact]
    public void LoadFromFile_AppliesOverrides_AndKeepsDefaultsForMissingFields()
    {
        var tmp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmp);
        var cfg = Path.Combine(tmp, "template-layout.json");
        File.WriteAllText(cfg, """
{
  "architectureRoot": "architecture/custom-site",
  "adrsRoot": "architecture/custom-site/adrs"
}
""");

        var layout = TemplateLayout.LoadFromFile(cfg);

        Assert.Equal("architecture/custom-site", layout.ArchitectureRoot);
        Assert.Equal("architecture/custom-site/adrs", layout.AdrsRoot);
        Assert.Equal(TemplateLayout.Default.WorkspaceDsl, layout.WorkspaceDsl);
        Assert.Equal(TemplateLayout.Default.PublishedDocsRoot, layout.PublishedDocsRoot);
    }

    [Fact]
    public void LoadFromRepositoryOrDefault_FindsRepositoryConfig()
    {
        var repoRoot = FindRepoRoot();
        var layout = TemplateLayout.LoadFromRepositoryOrDefault(repoRoot);
        Assert.Equal("docs/architecture/site", layout.ArchitectureRoot);
        Assert.Equal("docs/architecture/site/workspace.dsl", layout.WorkspaceDsl);
        Assert.Equal("docs/architecture/site/adrs", layout.AdrsRoot);
        var generic = layout.ResolveForTemplate("generic");
        Assert.Equal("docs/architecture/site", generic.ArchitectureRoot);
        Assert.Equal("docs/architecture/site/workspace.dsl", generic.WorkspaceDsl);
        Assert.Equal("docs/architecture/site/adrs", generic.AdrsRoot);
    }

    [Fact]
    public void LoadFromFileOrDefault_ReturnsDefault_WhenMissing()
    {
        var missing = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), "missing.json");
        var layout = TemplateLayout.LoadFromFileOrDefault(missing);
        Assert.Equal(TemplateLayout.Default.ArchitectureRoot, layout.ArchitectureRoot);
        Assert.Equal(TemplateLayout.Default.WorkspaceDsl, layout.WorkspaceDsl);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 10; i++)
        {
            var candidate = Path.Combine(dir.FullName, "meta-agent", "config", "template-layout.json");
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
}
