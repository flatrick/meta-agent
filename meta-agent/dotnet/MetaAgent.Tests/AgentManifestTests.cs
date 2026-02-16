using System;
using System.IO;
using System.Text.Json;
using Xunit;

public class AgentManifestTests
{
    [Fact]
    public void DotnetAgentManifest_ExistsAndHasExpectedFields()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        // walk up until we find agents/dotnet-agent.json
        string? manifest = null;
        for (int i = 0; i < 8; i++)
        {
            var candidateJson = Path.Combine(dir.FullName, "agents", "dotnet-agent.json");
            if (File.Exists(candidateJson))
            {
                manifest = candidateJson;
                break;
            }
            if (dir.Parent == null) break;
            dir = dir.Parent;
        }

        Assert.False(string.IsNullOrEmpty(manifest), "agents/dotnet-agent.json not found in repository tree");
        var manifestPath = Assert.IsType<string>(manifest);
        var txt = File.ReadAllText(manifestPath);
        using var doc = JsonDocument.Parse(txt);
        var root = doc.RootElement;

        Assert.Equal("dotnet-meta-agent", root.GetProperty("id").GetString());
        Assert.Equal("dotnet", root.GetProperty("preferred_runtime").GetString());
        Assert.Equal("A1", root.GetProperty("default_autonomy").GetString());
    }
}
