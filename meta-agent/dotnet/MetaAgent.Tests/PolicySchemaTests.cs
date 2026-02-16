using System;
using System.IO;
using MetaAgent.Core;
using Xunit;

public class PolicySchemaTests
{
    [Fact]
    public void SampleConfigJson_ValidatesAgainstSchema()
    {
        var repo = AppContext.BaseDirectory;
        // walk up to repository root to find examples/sample_config.json
        var dir = new DirectoryInfo(repo);
        string? sample = null;
        for (int i = 0; i < 8; i++)
        {
            var candidate = Path.Combine(dir.FullName, "examples", "sample_config.json");
            if (File.Exists(candidate)) { sample = candidate; break; }
            if (dir.Parent == null) break;
            dir = dir.Parent;
        }

        Assert.False(string.IsNullOrEmpty(sample), "examples/sample_config.json not found");
        var samplePath = Assert.IsType<string>(sample);
        var (isValid, errors) = PolicySchemaValidator.ValidateFile(samplePath);
        Assert.True(isValid, string.Join("; ", errors));
    }

    [Fact]
    public void DefaultPolicy_CreatedByGenerator_ValidatesAgainstSchema()
    {
        var tmp = Path.GetTempFileName();
        Generator.CreateDefaultPolicy(tmp);
        var (isValid, errors) = PolicySchemaValidator.ValidateFile(tmp);
        Assert.True(isValid, string.Join("; ", errors));
    }

    [Fact]
    public void InvalidPolicy_FailsValidation()
    {
        var tmp = Path.GetTempFileName();
        File.WriteAllText(tmp, "{ \"name\": 123 }");
        var (isValid, errors) = PolicySchemaValidator.ValidateFile(tmp);
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("'name' is required" ) || e.Contains("must be a string"));
    }

    [Fact]
    public void AutonomyA0_IsAccepted()
    {
        var tmp = Path.GetTempFileName();
        File.WriteAllText(tmp, """
{
  "name": "enterprise-default",
  "defaultMode": "interactive_ide",
  "autonomyDefault": "A0",
  "budgets": {
    "tokensPerDay": 50000,
    "ticketsPerDay": 10,
    "maxConcurrentPrs": 3
  }
}
""");
        var (isValid, errors) = PolicySchemaValidator.ValidateFile(tmp);
        Assert.True(isValid, string.Join("; ", errors));
    }

    [Fact]
    public void UnknownTopLevelProperty_FailsValidation()
    {
        var tmp = Path.GetTempFileName();
        File.WriteAllText(tmp, """
{
  "name": "enterprise-default",
  "defaultMode": "interactive_ide",
  "autonomyDefault": "A1",
  "budgets": {
    "tokensPerDay": 50000,
    "ticketsPerDay": 10,
    "maxConcurrentPrs": 3
  },
  "unexpected": true
}
""");
        var (isValid, errors) = PolicySchemaValidator.ValidateFile(tmp);
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("Unknown property is not allowed"));
    }

    [Fact]
    public void UnknownNestedBudgetsProperty_FailsValidation()
    {
        var tmp = Path.GetTempFileName();
        File.WriteAllText(tmp, """
{
  "name": "enterprise-default",
  "defaultMode": "interactive_ide",
  "autonomyDefault": "A1",
  "budgets": {
    "tokensPerDay": 50000,
    "ticketsPerDay": 10,
    "maxConcurrentPrs": 3,
    "burstTokens": 9000
  }
}
""");
        var (isValid, errors) = PolicySchemaValidator.ValidateFile(tmp);
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("Unknown property is not allowed: 'budgets."));
    }

    [Fact]
    public void CommandGatingAndBudgetAccounting_AreAccepted()
    {
        var tmp = Path.GetTempFileName();
        File.WriteAllText(tmp, """
{
  "name": "enterprise-default",
  "defaultMode": "interactive_ide",
  "autonomyDefault": "A1",
  "ambiguityThreshold": 0.65,
  "commandGating": "all_commands",
  "budgets": {
    "tokensPerDay": 50000,
    "ticketsPerDay": 10,
    "maxConcurrentPrs": 3
  },
  "budgetAccounting": {
    "mode": "persistent_daily",
    "stateFile": ".meta-agent-budget-state.json"
  },
  "tokenGovernance": {
    "autonomousTicketRunner": {
      "hardCapTokensPerRun": 6000
    },
    "interactiveIde": {
      "warningTokensPerRun": 2500,
      "requireOperatorApproval": true
    }
  },
  "integrations": {
    "ticketContextEnvVars": ["JIRA_TICKET_KEY", "GITLAB_TICKET_ID"]
  }
}
""");
        var (isValid, errors) = PolicySchemaValidator.ValidateFile(tmp);
        Assert.True(isValid, string.Join("; ", errors));
    }

    [Fact]
    public void PolicyVersion_WithInvalidValue_FailsValidation()
    {
        var tmp = Path.GetTempFileName();
        File.WriteAllText(tmp, """
{
  "policyVersion": 0,
  "name": "enterprise-default",
  "defaultMode": "interactive_ide",
  "autonomyDefault": "A1",
  "budgets": {
    "tokensPerDay": 50000,
    "ticketsPerDay": 10,
    "maxConcurrentPrs": 3
  }
}
""");

        var (isValid, errors) = PolicySchemaValidator.ValidateFile(tmp);
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("'policyVersion' must be a positive integer when present"));
    }

    [Fact]
    public void AmbiguityThreshold_OutOfRange_FailsValidation()
    {
        var tmp = Path.GetTempFileName();
        File.WriteAllText(tmp, """
{
  "name": "enterprise-default",
  "defaultMode": "interactive_ide",
  "autonomyDefault": "A1",
  "ambiguityThreshold": 1.5,
  "budgets": {
    "tokensPerDay": 50000,
    "ticketsPerDay": 10,
    "maxConcurrentPrs": 3
  }
}
""");
        var (isValid, errors) = PolicySchemaValidator.ValidateFile(tmp);
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("'ambiguityThreshold' must be a number between 0 and 1"));
    }

    [Fact]
    public void TokenGovernance_WithInvalidValueType_FailsValidation()
    {
        var tmp = Path.GetTempFileName();
        File.WriteAllText(tmp, """
{
  "name": "enterprise-default",
  "defaultMode": "interactive_ide",
  "autonomyDefault": "A1",
  "budgets": {
    "tokensPerDay": 50000,
    "ticketsPerDay": 10,
    "maxConcurrentPrs": 3
  },
  "tokenGovernance": {
    "interactiveIde": {
      "warningTokensPerRun": "high"
    }
  }
}
""");
        var (isValid, errors) = PolicySchemaValidator.ValidateFile(tmp);
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("'tokenGovernance.interactiveIde.warningTokensPerRun' must be a non-negative integer"));
    }

    [Fact]
    public void Integrations_WithNonStringEnvVar_FailsValidation()
    {
        var tmp = Path.GetTempFileName();
        File.WriteAllText(tmp, """
{
  "name": "enterprise-default",
  "defaultMode": "interactive_ide",
  "autonomyDefault": "A1",
  "budgets": {
    "tokensPerDay": 50000,
    "ticketsPerDay": 10,
    "maxConcurrentPrs": 3
  },
  "integrations": {
    "ticketContextEnvVars": ["JIRA_TICKET_KEY", 123]
  }
}
""");
        var (isValid, errors) = PolicySchemaValidator.ValidateFile(tmp);
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("'integrations.ticketContextEnvVars' must only contain strings"));
    }

    [Fact]
    public void PreferredRuntime_Property_IsRejected_AsUnknown()
    {
        var tmp = Path.GetTempFileName();
        File.WriteAllText(tmp, """
{
  "name": "enterprise-default",
  "defaultMode": "interactive_ide",
  "autonomyDefault": "A1",
  "preferredRuntime": "dotnet",
  "budgets": {
    "tokensPerDay": 50000,
    "ticketsPerDay": 10,
    "maxConcurrentPrs": 3
  }
}
""");
        var (isValid, errors) = PolicySchemaValidator.ValidateFile(tmp);
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("Unknown property is not allowed: 'preferredRuntime'"));
    }
}
