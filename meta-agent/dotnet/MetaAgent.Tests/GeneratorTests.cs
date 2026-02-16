using System;
using System.IO;
using MetaAgent.Core;
using Xunit;

public class GeneratorTests
{
    [Fact]
    public void RenderTemplate_CopiesFilesAndRendersPlaceholder()
    {
        var tmp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmp);
        // use 'generic' template which exists in repo
        Generator.RenderTemplate("generic", tmp, "demo-project");
        var readme = Path.Combine(tmp, "README.md");
        Assert.True(File.Exists(readme));
        var txt = File.ReadAllText(readme);
        Assert.Contains("demo-project", txt);
    }

    [Fact]
    public void RenderTemplate_UsesDirectoryNameWhenProjectNameIsNullOrEmpty()
    {
        var tmp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmp);

        // projectName == null
        Generator.RenderTemplate("generic", tmp, null);
        var txt = File.ReadAllText(Path.Combine(tmp, "README.md"));
        Assert.Contains(Path.GetFileName(tmp), txt);

        // projectName == empty string
        var tmp2 = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmp2);
        Generator.RenderTemplate("generic", tmp2, "");
        var txt2 = File.ReadAllText(Path.Combine(tmp2, "README.md"));
        Assert.Contains(Path.GetFileName(tmp2), txt2);
    }

    [Fact]
    public void RenderTemplate_HonorsJinjaOrFallback_DoubleQuotes()
    {
        var originalCwd = Directory.GetCurrentDirectory();
        var repo = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            Directory.CreateDirectory(repo);
            var templates = Path.Combine(repo, "templates", "jt");
            Directory.CreateDirectory(templates);
            var src = Path.Combine(templates, "file.txt");
            File.WriteAllText(src, "Name: {{ project_name or \"FALLBACK-DQ\" }}");

            Directory.SetCurrentDirectory(repo);
            var dest = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(dest);

            // null -> uses fallback from template
            Generator.RenderTemplate("jt", dest, null);
            var outTxt = File.ReadAllText(Path.Combine(dest, "file.txt"));
            Assert.Contains("FALLBACK-DQ", outTxt);

            // provided project name overrides fallback
            var dest2 = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(dest2);
            Generator.RenderTemplate("jt", dest2, "Provided");
            var out2 = File.ReadAllText(Path.Combine(dest2, "file.txt"));
            Assert.Contains("Provided", out2);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
        }
    }

    [Fact]
    public void RenderTemplate_HonorsJinjaOrFallback_SingleQuotes()
    {
        var originalCwd = Directory.GetCurrentDirectory();
        var repo = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            Directory.CreateDirectory(repo);
            var templates = Path.Combine(repo, "templates", "jt2");
            Directory.CreateDirectory(templates);
            var src = Path.Combine(templates, "file.txt");
            File.WriteAllText(src, "Value: {{ project_name or 'FALLBACK-SQ' }}");

            Directory.SetCurrentDirectory(repo);
            var dest = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(dest);

            // null -> uses fallback from template
            Generator.RenderTemplate("jt2", dest, null);
            var outTxt = File.ReadAllText(Path.Combine(dest, "file.txt"));
            Assert.Contains("FALLBACK-SQ", outTxt);

            // provided project name overrides fallback
            var dest2 = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(dest2);
            Generator.RenderTemplate("jt2", dest2, "Provided2");
            var out2 = File.ReadAllText(Path.Combine(dest2, "file.txt"));
            Assert.Contains("Provided2", out2);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
        }
    }

    [Fact]
    public void RenderTemplate_FindsMetaAgentTemplates_FromRepositoryRoot()
    {
        var originalCwd = Directory.GetCurrentDirectory();
        var repo = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            var templates = Path.Combine(repo, "meta-agent", "templates", "g");
            Directory.CreateDirectory(templates);
            File.WriteAllText(Path.Combine(templates, "README.md"), "# {{ project_name }}");

            Directory.SetCurrentDirectory(repo);
            var dest = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(dest);

            Generator.RenderTemplate("g", dest, "root-run");
            var outTxt = File.ReadAllText(Path.Combine(dest, "README.md"));
            Assert.Contains("root-run", outTxt);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
        }
    }

    [Fact]
    public void RenderTemplate_RendersTemplateVariablesInFilePaths_IncludingAdrPrefix()
    {
        var originalCwd = Directory.GetCurrentDirectory();
        var repo = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            var templateDir = Path.Combine(repo, "templates", "pathvars", "adrs");
            Directory.CreateDirectory(templateDir);
            var src = Path.Combine(templateDir, "{{ adr_id_prefix }}-decision.md");
            File.WriteAllText(src, "# {{ adr_id_prefix }} for {{ project_name }}");

            Directory.SetCurrentDirectory(repo);
            var dest = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(dest);

            Generator.RenderTemplate("pathvars", dest, "demo", "PLATFORM-1234");
            var renderedPath = Path.Combine(dest, "adrs", "PLATFORM-1234-decision.md");
            Assert.True(File.Exists(renderedPath));
            Assert.Contains("PLATFORM-1234 for demo", File.ReadAllText(renderedPath));
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
        }
    }

    [Fact]
    public void RenderTemplate_ExposesGeneratedAtUtcTemplateVariable()
    {
        var originalCwd = Directory.GetCurrentDirectory();
        var repo = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            var templateDir = Path.Combine(repo, "templates", "timevar");
            Directory.CreateDirectory(templateDir);
            File.WriteAllText(
                Path.Combine(templateDir, "stamp.txt"),
                "Generated: {{ generated_at_utc }}");

            Directory.SetCurrentDirectory(repo);
            var dest = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(dest);

            Generator.RenderTemplate("timevar", dest, "demo");
            var rendered = File.ReadAllText(Path.Combine(dest, "stamp.txt"));
            Assert.Matches(@"Generated: \d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z", rendered);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
        }
    }

    [Fact]
    public void RenderTemplate_ComposesTemplateFromDirectTemplateSource()
    {
        var originalCwd = Directory.GetCurrentDirectory();
        var repo = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            var sourceRoot = Path.Combine(repo, "template-src");
            var baseDir = Path.Combine(sourceRoot, "base");
            var overlayDir = Path.Combine(sourceRoot, "overlays", "demo");
            Directory.CreateDirectory(baseDir);
            Directory.CreateDirectory(overlayDir);

            File.WriteAllText(Path.Combine(baseDir, "README.md"), "# Base {{ project_name }}");
            File.WriteAllText(Path.Combine(baseDir, "base.txt"), "base");
            File.WriteAllText(Path.Combine(overlayDir, "README.md"), "# Overlay {{ project_name }}");
            File.WriteAllText(Path.Combine(overlayDir, "overlay.txt"), "overlay");
            File.WriteAllText(
                Path.Combine(sourceRoot, "manifest.json"),
                """
                {
                  "base": "base",
                  "overlayRoot": "overlays",
                  "templates": {
                    "demo": {
                      "overlays": ["demo"],
                      "remove": ["base.txt"],
                      "required": ["README.md", "overlay.txt"]
                    }
                  }
                }
                """);

            Directory.SetCurrentDirectory(repo);
            var dest = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(dest);

            Generator.RenderTemplate("demo", dest, "from-direct");

            var rendered = File.ReadAllText(Path.Combine(dest, "README.md"));
            Assert.Contains("Overlay from-direct", rendered);
            Assert.True(File.Exists(Path.Combine(dest, "overlay.txt")));
            Assert.False(File.Exists(Path.Combine(dest, "base.txt")));

            var composed = Path.Combine(repo, "templates", "demo", "README.md");
            Assert.True(File.Exists(composed));
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
        }
    }

    [Fact]
    public void RenderTemplate_ComposesTemplateFromNestedMetaAgentTemplateSource()
    {
        var originalCwd = Directory.GetCurrentDirectory();
        var repo = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            var sourceRoot = Path.Combine(repo, "meta-agent", "template-src");
            var baseDir = Path.Combine(sourceRoot, "base");
            var overlayDir = Path.Combine(sourceRoot, "overlays", "nested");
            Directory.CreateDirectory(baseDir);
            Directory.CreateDirectory(overlayDir);

            File.WriteAllText(Path.Combine(baseDir, "README.md"), "# Base {{ project_name }}");
            File.WriteAllText(Path.Combine(overlayDir, "README.md"), "# Nested {{ project_name }}");
            File.WriteAllText(
                Path.Combine(sourceRoot, "manifest.json"),
                """
                {
                  "base": "base",
                  "overlayRoot": "overlays",
                  "templates": {
                    "nested": {
                      "overlays": ["nested"],
                      "remove": [],
                      "required": ["README.md"]
                    }
                  }
                }
                """);

            Directory.SetCurrentDirectory(repo);
            var dest = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(dest);

            Generator.RenderTemplate("nested", dest, "from-nested");

            var rendered = File.ReadAllText(Path.Combine(dest, "README.md"));
            Assert.Contains("Nested from-nested", rendered);
            var composed = Path.Combine(repo, "meta-agent", "templates", "nested", "README.md");
            Assert.True(File.Exists(composed));
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
        }
    }
}
