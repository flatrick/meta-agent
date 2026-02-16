using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MetaAgent.Core
{
    public sealed class PolicyLoadResult
    {
        public Policy Policy { get; init; } = new Policy();
        public int SourcePolicyVersion { get; init; }
        public int EffectivePolicyVersion { get; init; }
        public bool Migrated { get; init; }
        public bool Persisted { get; init; }
    }

    public static class PolicyMigration
    {
        public static PolicyLoadResult LoadWithMigration(string path, bool persistMigrated)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(path);
            }

            var json = File.ReadAllText(path);
            var root = JsonNode.Parse(json) as JsonObject
                ?? throw new InvalidDataException("Policy must be a JSON object.");

            var sourceVersion = ResolvePolicyVersion(root);
            if (sourceVersion > Policy.CurrentPolicyVersion)
            {
                throw new InvalidDataException(
                    $"Unsupported policyVersion '{sourceVersion}'. This CLI supports up to '{Policy.CurrentPolicyVersion}'.");
            }

            var migrated = false;
            if (sourceVersion == 0)
            {
                root["policyVersion"] = Policy.CurrentPolicyVersion;
                migrated = true;
            }

            if (root.Remove("preferredRuntime"))
            {
                migrated = true;
            }

            var normalizedJson = root.ToJsonString(Policy.WriteJsonOptions);
            var policy = JsonSerializer.Deserialize<Policy>(normalizedJson, Policy.ReadJsonOptions) ?? new Policy();
            policy.PolicyVersion = Policy.CurrentPolicyVersion;

            var persisted = false;
            if (migrated && persistMigrated)
            {
                File.WriteAllText(path, policy.ToJson());
                persisted = true;
            }

            return new PolicyLoadResult
            {
                Policy = policy,
                SourcePolicyVersion = sourceVersion,
                EffectivePolicyVersion = policy.PolicyVersion,
                Migrated = migrated,
                Persisted = persisted
            };
        }

        private static int ResolvePolicyVersion(JsonObject root)
        {
            if (!root.TryGetPropertyValue("policyVersion", out var policyVersionNode) || policyVersionNode == null)
            {
                return 0;
            }

            if (policyVersionNode is JsonValue policyVersionValue
                && policyVersionValue.TryGetValue<int>(out var parsed)
                && parsed >= 1)
            {
                return parsed;
            }

            throw new InvalidDataException("'policyVersion' must be a positive integer when present.");
        }
    }
}
