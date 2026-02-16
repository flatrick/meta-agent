using System.IO;
using MetaAgent.Core;
using Xunit;

public class PolicyTests
{
    [Fact]
    public void LoadFromFile_ParsesJson()
    {
        var json = """
{
  "name": "enterprise-default",
  "defaultMode": "interactive_ide",
  "autonomyDefault": "A1",
  "budgets": { "tokensPerDay": 50000, "ticketsPerDay": 10, "maxConcurrentPrs": 3 }
}
""";
        var tmp = Path.GetTempFileName();
        File.WriteAllText(tmp, json);
        var p = Policy.LoadFromFile(tmp);
        Assert.Equal("A1", p.AutonomyDefault);
        Assert.Equal(50000, p.Budgets.TokensPerDay);
    }
}
