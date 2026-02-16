using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MetaAgent.Core
{
    public static class PolicySchemaValidator
    {
        private static readonly Regex AutonomyPattern = new Regex("^A[0-3]$", RegexOptions.Compiled);

        private static readonly HashSet<string> AllowedRootProps = new HashSet<string>(StringComparer.Ordinal)
        {
            "policyVersion", "name", "defaultMode", "autonomyDefault", "ambiguityThreshold", "commandGating", "budgets", "budgetAccounting", "tokenGovernance", "integrations", "changeBoundaries", "abortConditions"
        };

        private static readonly HashSet<string> AllowedBudgetProps = new HashSet<string>(StringComparer.Ordinal)
        {
            "tokensPerDay", "ticketsPerDay", "maxConcurrentPrs"
        };

        private static readonly HashSet<string> AllowedBudgetAccountingProps = new HashSet<string>(StringComparer.Ordinal)
        {
            "mode", "stateFile"
        };

        private static readonly HashSet<string> AllowedBoundaryProps = new HashSet<string>(StringComparer.Ordinal)
        {
            "allowedPaths", "disallowedPaths"
        };

        private static readonly HashSet<string> AllowedTokenGovernanceProps = new HashSet<string>(StringComparer.Ordinal)
        {
            "autonomousTicketRunner", "interactiveIde"
        };

        private static readonly HashSet<string> AllowedAutonomousTokenGovernanceProps = new HashSet<string>(StringComparer.Ordinal)
        {
            "hardCapTokensPerRun"
        };

        private static readonly HashSet<string> AllowedInteractiveTokenGovernanceProps = new HashSet<string>(StringComparer.Ordinal)
        {
            "warningTokensPerRun", "requireOperatorApproval"
        };

        private static readonly HashSet<string> AllowedIntegrationProps = new HashSet<string>(StringComparer.Ordinal)
        {
            "ticketContextEnvVars"
        };

        public static (bool IsValid, List<string> Errors) ValidateFile(string path)
        {
            if (!File.Exists(path))
            {
                return (false, new List<string> { $"Policy file not found: {path}" });
            }

            return ValidateJson(File.ReadAllText(path));
        }

        public static (bool IsValid, List<string> Errors) ValidateJson(string json)
        {
            var errors = new List<string>();
            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(json);
            }
            catch (JsonException ex)
            {
                errors.Add($"Invalid JSON: {ex.Message}");
                return (false, errors);
            }

            using (doc)
            {
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                {
                    errors.Add("Policy must be a JSON object");
                    return (false, errors);
                }

                var root = doc.RootElement;
                ValidateRootProperties(root, errors);
                ValidateRootFields(root, errors);
                ValidateBudgets(root, errors);
                ValidateBudgetAccounting(root, errors);
                ValidateTokenGovernance(root, errors);
                ValidateIntegrations(root, errors);
                ValidateChangeBoundaries(root, errors);
                ValidateAbortConditions(root, errors);

                return (errors.Count == 0, errors);
            }
        }

        private static void ValidateIntegrations(JsonElement root, List<string> errors)
        {
            if (!root.TryGetProperty("integrations", out var integrations))
            {
                return;
            }

            if (integrations.ValueKind != JsonValueKind.Object)
            {
                errors.Add("'integrations' must be an object");
                return;
            }

            foreach (var prop in integrations.EnumerateObject())
            {
                if (!AllowedIntegrationProps.Contains(prop.Name))
                {
                    errors.Add($"Unknown property is not allowed: 'integrations.{prop.Name}'");
                }
            }

            ValidateStringArray(integrations, "ticketContextEnvVars", "'integrations.ticketContextEnvVars' must be an array of strings", "'integrations.ticketContextEnvVars' must only contain strings", errors);
        }

        private static void ValidateTokenGovernance(JsonElement root, List<string> errors)
        {
            if (!root.TryGetProperty("tokenGovernance", out var tokenGovernance))
            {
                return;
            }

            if (tokenGovernance.ValueKind != JsonValueKind.Object)
            {
                errors.Add("'tokenGovernance' must be an object");
                return;
            }

            foreach (var prop in tokenGovernance.EnumerateObject())
            {
                if (!AllowedTokenGovernanceProps.Contains(prop.Name))
                {
                    errors.Add($"Unknown property is not allowed: 'tokenGovernance.{prop.Name}'");
                }
            }

            if (tokenGovernance.TryGetProperty("autonomousTicketRunner", out var autonomous))
            {
                if (autonomous.ValueKind != JsonValueKind.Object)
                {
                    errors.Add("'tokenGovernance.autonomousTicketRunner' must be an object");
                }
                else
                {
                    foreach (var prop in autonomous.EnumerateObject())
                    {
                        if (!AllowedAutonomousTokenGovernanceProps.Contains(prop.Name))
                        {
                            errors.Add($"Unknown property is not allowed: 'tokenGovernance.autonomousTicketRunner.{prop.Name}'");
                        }
                    }

                    if (autonomous.TryGetProperty("hardCapTokensPerRun", out var hardCap)
                        && (hardCap.ValueKind != JsonValueKind.Number || !hardCap.TryGetInt32(out var parsed) || parsed < 0))
                    {
                        errors.Add("'tokenGovernance.autonomousTicketRunner.hardCapTokensPerRun' must be a non-negative integer");
                    }
                }
            }

            if (tokenGovernance.TryGetProperty("interactiveIde", out var interactive))
            {
                if (interactive.ValueKind != JsonValueKind.Object)
                {
                    errors.Add("'tokenGovernance.interactiveIde' must be an object");
                }
                else
                {
                    foreach (var prop in interactive.EnumerateObject())
                    {
                        if (!AllowedInteractiveTokenGovernanceProps.Contains(prop.Name))
                        {
                            errors.Add($"Unknown property is not allowed: 'tokenGovernance.interactiveIde.{prop.Name}'");
                        }
                    }

                    if (interactive.TryGetProperty("warningTokensPerRun", out var warning)
                        && (warning.ValueKind != JsonValueKind.Number || !warning.TryGetInt32(out var parsed) || parsed < 0))
                    {
                        errors.Add("'tokenGovernance.interactiveIde.warningTokensPerRun' must be a non-negative integer");
                    }

                    if (interactive.TryGetProperty("requireOperatorApproval", out var requireApproval)
                        && requireApproval.ValueKind != JsonValueKind.True
                        && requireApproval.ValueKind != JsonValueKind.False)
                    {
                        errors.Add("'tokenGovernance.interactiveIde.requireOperatorApproval' must be a boolean");
                    }
                }
            }
        }

        private static void ValidateRootProperties(JsonElement root, List<string> errors)
        {
            foreach (var prop in root.EnumerateObject())
            {
                if (!AllowedRootProps.Contains(prop.Name))
                {
                    errors.Add($"Unknown property is not allowed: '{prop.Name}'");
                }
            }
        }

        private static void ValidateRootFields(JsonElement root, List<string> errors)
        {
            if (root.TryGetProperty("policyVersion", out var policyVersion))
            {
                if (policyVersion.ValueKind != JsonValueKind.Number
                    || !policyVersion.TryGetInt32(out var version)
                    || version < 1)
                {
                    errors.Add("'policyVersion' must be a positive integer when present");
                }
                else if (version > Policy.CurrentPolicyVersion)
                {
                    errors.Add($"'policyVersion' '{version}' is not supported by this CLI (max supported: {Policy.CurrentPolicyVersion})");
                }
            }

            if (!root.TryGetProperty("name", out var nameProp) || nameProp.ValueKind != JsonValueKind.String)
            {
                errors.Add("'name' is required and must be a string");
            }

            if (!root.TryGetProperty("defaultMode", out var defaultMode) || defaultMode.ValueKind != JsonValueKind.String)
            {
                errors.Add("'defaultMode' is required and must be a string");
            }
            else
            {
                var value = defaultMode.GetString() ?? string.Empty;
                if (value != ExecutionMode.InteractiveIde && value != ExecutionMode.AutonomousTicketRunner && value != ExecutionMode.Hybrid)
                {
                    errors.Add("'defaultMode' must be one of: interactive_ide, autonomous_ticket_runner, hybrid");
                }
            }

            if (!root.TryGetProperty("autonomyDefault", out var autonomyDefault) || autonomyDefault.ValueKind != JsonValueKind.String)
            {
                errors.Add("'autonomyDefault' is required and must be a string (e.g. 'A1')");
            }
            else if (!AutonomyPattern.IsMatch(autonomyDefault.GetString() ?? string.Empty))
            {
                errors.Add("'autonomyDefault' must match '^A[0-3]$' (e.g. 'A1')");
            }

            if (root.TryGetProperty("commandGating", out var commandGating))
            {
                if (commandGating.ValueKind != JsonValueKind.String)
                {
                    errors.Add("'commandGating' must be a string (mutating_only|all_commands)");
                }
                else
                {
                    var value = commandGating.GetString() ?? string.Empty;
                    if (value != "mutating_only" && value != "all_commands")
                    {
                        errors.Add("'commandGating' must be one of: mutating_only, all_commands");
                    }
                }
            }

            if (root.TryGetProperty("ambiguityThreshold", out var ambiguityThreshold))
            {
                if (ambiguityThreshold.ValueKind != JsonValueKind.Number
                    || !ambiguityThreshold.TryGetDouble(out var value)
                    || value < 0
                    || value > 1)
                {
                    errors.Add("'ambiguityThreshold' must be a number between 0 and 1");
                }
            }
        }

        private static void ValidateBudgets(JsonElement root, List<string> errors)
        {
            if (!root.TryGetProperty("budgets", out var budgets) || budgets.ValueKind != JsonValueKind.Object)
            {
                errors.Add("'budgets' is required and must be an object");
                return;
            }

            foreach (var prop in budgets.EnumerateObject())
            {
                if (!AllowedBudgetProps.Contains(prop.Name))
                {
                    errors.Add($"Unknown property is not allowed: 'budgets.{prop.Name}'");
                }
            }

            ValidateNonNegativeInt(budgets, "tokensPerDay", "'budgets.tokensPerDay' is required and must be a non-negative integer", errors);
            ValidateNonNegativeInt(budgets, "ticketsPerDay", "'budgets.ticketsPerDay' is required and must be a non-negative integer", errors);
            ValidateNonNegativeInt(budgets, "maxConcurrentPrs", "'budgets.maxConcurrentPrs' is required and must be a non-negative integer", errors);
        }

        private static void ValidateBudgetAccounting(JsonElement root, List<string> errors)
        {
            if (!root.TryGetProperty("budgetAccounting", out var budgetAccounting))
            {
                return;
            }

            if (budgetAccounting.ValueKind != JsonValueKind.Object)
            {
                errors.Add("'budgetAccounting' must be an object");
                return;
            }

            foreach (var prop in budgetAccounting.EnumerateObject())
            {
                if (!AllowedBudgetAccountingProps.Contains(prop.Name))
                {
                    errors.Add($"Unknown property is not allowed: 'budgetAccounting.{prop.Name}'");
                }
            }

            if (budgetAccounting.TryGetProperty("mode", out var mode))
            {
                if (mode.ValueKind != JsonValueKind.String)
                {
                    errors.Add("'budgetAccounting.mode' must be a string (per_invocation|persistent_daily)");
                }
                else
                {
                    var value = mode.GetString() ?? string.Empty;
                    if (value != "per_invocation" && value != "persistent_daily")
                    {
                        errors.Add("'budgetAccounting.mode' must be one of: per_invocation, persistent_daily");
                    }
                }
            }

            if (budgetAccounting.TryGetProperty("stateFile", out var stateFile) && stateFile.ValueKind != JsonValueKind.String)
            {
                errors.Add("'budgetAccounting.stateFile' must be a string");
            }
        }

        private static void ValidateChangeBoundaries(JsonElement root, List<string> errors)
        {
            if (!root.TryGetProperty("changeBoundaries", out var changeBoundaries))
            {
                return;
            }

            if (changeBoundaries.ValueKind != JsonValueKind.Object)
            {
                errors.Add("'changeBoundaries' must be an object");
                return;
            }

            foreach (var prop in changeBoundaries.EnumerateObject())
            {
                if (!AllowedBoundaryProps.Contains(prop.Name))
                {
                    errors.Add($"Unknown property is not allowed: 'changeBoundaries.{prop.Name}'");
                }
            }

            ValidateStringArray(changeBoundaries, "allowedPaths", "'changeBoundaries.allowedPaths' must be an array of strings", "'changeBoundaries.allowedPaths' must only contain strings", errors);
            ValidateStringArray(changeBoundaries, "disallowedPaths", "'changeBoundaries.disallowedPaths' must be an array of strings", "'changeBoundaries.disallowedPaths' must only contain strings", errors);
        }

        private static void ValidateAbortConditions(JsonElement root, List<string> errors)
        {
            if (!root.TryGetProperty("abortConditions", out var abortConditions))
            {
                return;
            }

            ValidateArrayOfStrings(abortConditions, "'abortConditions' must be an array of strings", "'abortConditions' must only contain strings", errors);
        }

        private static void ValidateNonNegativeInt(JsonElement parent, string name, string errorMessage, List<string> errors)
        {
            if (!parent.TryGetProperty(name, out var value)
                || value.ValueKind != JsonValueKind.Number
                || !value.TryGetInt32(out var parsed)
                || parsed < 0)
            {
                errors.Add(errorMessage);
            }
        }

        private static void ValidateStringArray(JsonElement parent, string propertyName, string notArrayMessage, string itemTypeMessage, List<string> errors)
        {
            if (!parent.TryGetProperty(propertyName, out var property))
            {
                return;
            }

            ValidateArrayOfStrings(property, notArrayMessage, itemTypeMessage, errors);
        }

        private static void ValidateArrayOfStrings(JsonElement arrayElement, string notArrayMessage, string itemTypeMessage, List<string> errors)
        {
            if (arrayElement.ValueKind != JsonValueKind.Array)
            {
                errors.Add(notArrayMessage);
                return;
            }

            foreach (var item in arrayElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String)
                {
                    errors.Add(itemTypeMessage);
                }
            }
        }
    }
}
