using System;
using System.IO;
using MetaAgent.Core;
using Xunit;

public class PolicyMigrationTests
{
    [Fact]
    public void LoadWithMigration_AddsPolicyVersion_WhenMissing_AndPersists()
    {
        var tempDir = CreateTempDir();
        var policyPath = Path.Combine(tempDir, ".meta-agent-policy.json");
        File.WriteAllText(policyPath, """
{
  "name": "legacy-policy",
  "defaultMode": "interactive_ide",
  "autonomyDefault": "A1",
  "budgets": {
    "tokensPerDay": 1000,
    "ticketsPerDay": 1,
    "maxConcurrentPrs": 1
  }
}
""");

        var result = PolicyMigration.LoadWithMigration(policyPath, persistMigrated: true);

        Assert.True(result.Migrated);
        Assert.True(result.Persisted);
        Assert.Equal(0, result.SourcePolicyVersion);
        Assert.Equal(Policy.CurrentPolicyVersion, result.EffectivePolicyVersion);
        var persisted = File.ReadAllText(policyPath);
        Assert.Contains("\"policyVersion\": 1", persisted, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadWithMigration_RejectsUnsupportedFuturePolicyVersion()
    {
        var tempDir = CreateTempDir();
        var policyPath = Path.Combine(tempDir, ".meta-agent-policy.json");
        File.WriteAllText(policyPath, """
{
  "policyVersion": 999,
  "name": "future-policy",
  "defaultMode": "interactive_ide",
  "autonomyDefault": "A1",
  "budgets": {
    "tokensPerDay": 1000,
    "ticketsPerDay": 1,
    "maxConcurrentPrs": 1
  }
}
""");

        var ex = Assert.Throws<InvalidDataException>(() => PolicyMigration.LoadWithMigration(policyPath, persistMigrated: true));
        Assert.Contains("Unsupported policyVersion", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadWithMigration_RejectsInvalidPolicyVersionType()
    {
        var tempDir = CreateTempDir();
        var policyPath = Path.Combine(tempDir, ".meta-agent-policy.json");
        File.WriteAllText(policyPath, """
{
  "policyVersion": "v1",
  "name": "bad-policy-version",
  "defaultMode": "interactive_ide",
  "autonomyDefault": "A1",
  "budgets": {
    "tokensPerDay": 1000,
    "ticketsPerDay": 1,
    "maxConcurrentPrs": 1
  }
}
""");

        var ex = Assert.Throws<InvalidDataException>(() => PolicyMigration.LoadWithMigration(policyPath, persistMigrated: true));
        Assert.Contains("'policyVersion' must be a positive integer", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadWithMigration_RemovesLegacyPreferredRuntime_WhenPresent()
    {
        var tempDir = CreateTempDir();
        var policyPath = Path.Combine(tempDir, ".meta-agent-policy.json");
        File.WriteAllText(policyPath, """
{
  "policyVersion": 1,
  "name": "legacy-runtime-key",
  "defaultMode": "interactive_ide",
  "autonomyDefault": "A1",
  "preferredRuntime": "dotnet",
  "budgets": {
    "tokensPerDay": 1000,
    "ticketsPerDay": 1,
    "maxConcurrentPrs": 1
  }
}
""");

        var result = PolicyMigration.LoadWithMigration(policyPath, persistMigrated: true);
        Assert.True(result.Migrated);
        Assert.True(result.Persisted);

        var persisted = File.ReadAllText(policyPath);
        Assert.DoesNotContain("\"preferredRuntime\"", persisted, StringComparison.Ordinal);
    }

    private static string CreateTempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), "meta-agent-tests", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(path);
        return path;
    }
}
