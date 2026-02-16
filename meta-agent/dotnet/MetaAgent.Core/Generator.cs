using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using Fluid;

namespace MetaAgent.Core
{
    public static class Generator
    {
        private static readonly Regex LegacyFallbackSingleQuoted = new Regex(@"\{\{\s*project_name\s+or\s+'([^']*)'\s*\}\}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex LegacyFallbackDoubleQuoted = new Regex(@"\{\{\s*project_name\s+or\s+""([^""]*)""\s*\}\}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex BareProjectName = new Regex(@"\{\{\s*project_name\s*\}\}", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Search upward from current directory for either:
        // - templates/<templateName>
        // - meta-agent/templates/<templateName>
        private static string FindTemplatesDir(string templateName)
        {
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            for (int i = 0; i < 12; i++)
            {
                var direct = Path.Combine(dir.FullName, "templates");
                if (Directory.Exists(Path.Combine(direct, templateName))) return direct;

                var nested = Path.Combine(dir.FullName, "meta-agent", "templates");
                if (Directory.Exists(Path.Combine(nested, templateName))) return nested;

                if (dir.Parent == null) break;
                dir = dir.Parent;
            }

            var composed = TryComposeTemplates(Directory.GetCurrentDirectory(), templateName);
            if (!string.IsNullOrWhiteSpace(composed) && Directory.Exists(composed))
            {
                return composed;
            }

            throw new DirectoryNotFoundException($"template '{templateName}' not found and no compose source could produce it");
        }

        private static string? TryComposeTemplates(string startDirectory, string templateName)
        {
            var dir = new DirectoryInfo(startDirectory);
            for (int i = 0; i < 12; i++)
            {
                var directSourceRoot = Path.Combine(dir.FullName, "template-src");
                var directOutput = Path.Combine(dir.FullName, "templates");
                if (TryComposeTemplatesFromSource(directSourceRoot, directOutput, templateName))
                {
                    return directOutput;
                }

                var nestedSourceRoot = Path.Combine(dir.FullName, "meta-agent", "template-src");
                var nestedOutput = Path.Combine(dir.FullName, "meta-agent", "templates");
                if (TryComposeTemplatesFromSource(nestedSourceRoot, nestedOutput, templateName))
                {
                    return nestedOutput;
                }

                if (dir.Parent == null)
                {
                    break;
                }
                dir = dir.Parent;
            }
            return null;
        }

        private static bool TryComposeTemplatesFromSource(string sourceRoot, string outputRoot, string templateName)
        {
            var manifestPath = Path.Combine(sourceRoot, "manifest.json");
            if (!File.Exists(manifestPath))
            {
                return false;
            }

            var manifest = TemplateComposeManifest.Load(manifestPath);
            manifest.ComposeTemplate(sourceRoot, outputRoot, templateName);
            return Directory.Exists(Path.Combine(outputRoot, templateName));
        }

        public static void RenderTemplate(string templateName, string targetPath, string? projectName = null, string? adrIdPrefix = null)
        {
            var parser = new FluidParser();
            var templatesRoot = FindTemplatesDir(templateName);
            var srcDir = Path.Combine(templatesRoot, templateName);
            if (!Directory.Exists(srcDir)) throw new DirectoryNotFoundException(srcDir);

            var destRoot = Path.GetFullPath(targetPath);
            Directory.CreateDirectory(destRoot);
            var fallbackProjectName = Path.GetFileName(destRoot);
            var providedProjectName = string.IsNullOrWhiteSpace(projectName) ? null : projectName;
            var resolvedAdrIdPrefix = string.IsNullOrWhiteSpace(adrIdPrefix) ? "0001" : adrIdPrefix.Trim();

            foreach (var srcPath in Directory.EnumerateFiles(srcDir, "*", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(srcDir, srcPath).Replace('\\', '/');
                var normalizedRelTemplate = NormalizeTemplateSyntax(rel, fallbackProjectName);
                if (!parser.TryParse(normalizedRelTemplate, out var relTemplate, out var relError))
                {
                    throw new InvalidOperationException($"Path template parse error in '{srcPath}': {relError}");
                }

                var context = new TemplateContext();
                if (!string.IsNullOrWhiteSpace(providedProjectName))
                {
                    context.SetValue("project_name", providedProjectName);
                }
                context.SetValue("adr_id_prefix", resolvedAdrIdPrefix);
                context.SetValue("generated_at_utc", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));

                var renderedRelativePath = relTemplate.Render(context);
                var normalizedRelativePath = renderedRelativePath.Replace('/', Path.DirectorySeparatorChar);
                var destPath = Path.Combine(destRoot, normalizedRelativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destPath) ?? destRoot);
                var text = File.ReadAllText(srcPath);

                var normalizedTemplate = NormalizeTemplateSyntax(text, fallbackProjectName);
                if (!parser.TryParse(normalizedTemplate, out var template, out var error))
                {
                    throw new InvalidOperationException($"Template parse error in '{srcPath}': {error}");
                }

                var rendered = template.Render(context);
                File.WriteAllText(destPath, rendered);
            }
        }

        public static void ScaffoldAgentAssets(
            string templateName,
            string targetPath,
            string? projectName = null,
            string? adrIdPrefix = null,
            string conflictStrategy = "merge")
        {
            var destinationRoot = Path.GetFullPath(targetPath);
            Directory.CreateDirectory(destinationRoot);

            var stagingRoot = Path.Combine(Path.GetTempPath(), $"meta-agent-agent-assets-{Guid.NewGuid():N}");
            try
            {
                RenderTemplate(templateName, stagingRoot, projectName, adrIdPrefix);
                var normalizedStrategy = (conflictStrategy ?? "merge").Trim().ToLowerInvariant();
                if (normalizedStrategy != "stop"
                    && normalizedStrategy != "merge"
                    && normalizedStrategy != "replace"
                    && normalizedStrategy != "rename")
                {
                    throw new InvalidOperationException($"Unsupported conflict strategy: {conflictStrategy}");
                }

                var assetPairs = new[]
                {
                    (Source: Path.Combine(stagingRoot, "AGENTS.md"), Destination: Path.Combine(destinationRoot, "AGENTS.md")),
                    (Source: Path.Combine(stagingRoot, "PKB"), Destination: Path.Combine(destinationRoot, "PKB")),
                    (Source: Path.Combine(stagingRoot, "docs"), Destination: Path.Combine(destinationRoot, "docs")),
                    (Source: Path.Combine(stagingRoot, "scripts"), Destination: Path.Combine(destinationRoot, "scripts"))
                };

                if (normalizedStrategy == "stop")
                {
                    var conflicts = new List<string>();
                    foreach (var pair in assetPairs)
                    {
                        if (Directory.Exists(pair.Destination) || File.Exists(pair.Destination))
                        {
                            conflicts.Add(pair.Destination);
                        }
                    }

                    if (conflicts.Count > 0)
                    {
                        throw new InvalidOperationException($"Agent asset scaffold conflicts detected: {string.Join(", ", conflicts)}");
                    }
                }

                foreach (var pair in assetPairs)
                {
                    var destinationPath = pair.Destination;
                    if (normalizedStrategy == "rename" && (Directory.Exists(destinationPath) || File.Exists(destinationPath)))
                    {
                        destinationPath = BuildUniqueRenamePath(destinationPath);
                    }

                    var overwriteExisting = normalizedStrategy == "replace";
                    CopyPathIfPresent(pair.Source, destinationPath, overwriteExisting);
                }
            }
            finally
            {
                if (Directory.Exists(stagingRoot))
                {
                    Directory.Delete(stagingRoot, recursive: true);
                }
            }
        }

        public static List<string> GetAgentAssetConflicts(string targetPath)
        {
            var destinationRoot = Path.GetFullPath(targetPath);
            var conflicts = new List<string>();
            var candidatePaths = new[]
            {
                Path.Combine(destinationRoot, "AGENTS.md"),
                Path.Combine(destinationRoot, "PKB"),
                Path.Combine(destinationRoot, "docs"),
                Path.Combine(destinationRoot, "scripts")
            };

            foreach (var candidate in candidatePaths)
            {
                if (File.Exists(candidate) || Directory.Exists(candidate))
                {
                    conflicts.Add(candidate);
                }
            }

            return conflicts;
        }

        private static string NormalizeTemplateSyntax(string template, string defaultProjectName)
        {
            // Keep backward compatibility for previous Jinja-like fallback syntax:
            // {{ project_name or "Fallback" }} / {{ project_name or 'Fallback' }}
            var normalized = LegacyFallbackSingleQuoted.Replace(template, m =>
                $"{{{{ project_name | default: \"{EscapeLiquidString(m.Groups[1].Value)}\" }}}}");
            normalized = LegacyFallbackDoubleQuoted.Replace(normalized, m =>
                $"{{{{ project_name | default: \"{EscapeLiquidString(m.Groups[1].Value)}\" }}}}");

            // Preserve previous behavior for plain {{ project_name }} by defaulting to destination directory name.
            normalized = BareProjectName.Replace(normalized,
                $"{{{{ project_name | default: \"{EscapeLiquidString(defaultProjectName)}\" }}}}");

            return normalized;
        }

        private static string EscapeLiquidString(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static void CopyDirectory(string sourceDirectory, string destinationDirectory)
        {
            if (!Directory.Exists(sourceDirectory))
            {
                throw new DirectoryNotFoundException(sourceDirectory);
            }

            Directory.CreateDirectory(destinationDirectory);
            foreach (var sourceFile in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(sourceDirectory, sourceFile);
                var destinationFile = Path.Combine(destinationDirectory, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destinationFile) ?? destinationDirectory);
                File.Copy(sourceFile, destinationFile, overwrite: true);
            }
        }

        private static void CopyPathIfPresent(string sourcePath, string destinationPath, bool overwriteExisting)
        {
            if (Directory.Exists(sourcePath))
            {
                CopyDirectoryWithPolicy(sourcePath, destinationPath, overwriteExisting);
                return;
            }

            if (File.Exists(sourcePath))
            {
                var parent = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrWhiteSpace(parent))
                {
                    Directory.CreateDirectory(parent);
                }

                if (overwriteExisting || !File.Exists(destinationPath))
                {
                    File.Copy(sourcePath, destinationPath, overwrite: overwriteExisting);
                }
            }
        }

        private static void CopyDirectoryWithPolicy(string sourceDirectory, string destinationDirectory, bool overwriteExisting)
        {
            if (!Directory.Exists(sourceDirectory))
            {
                throw new DirectoryNotFoundException(sourceDirectory);
            }

            Directory.CreateDirectory(destinationDirectory);
            foreach (var sourceFile in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(sourceDirectory, sourceFile);
                var destinationFile = Path.Combine(destinationDirectory, relative);
                var parent = Path.GetDirectoryName(destinationFile);
                if (!string.IsNullOrWhiteSpace(parent))
                {
                    Directory.CreateDirectory(parent);
                }

                if (overwriteExisting || !File.Exists(destinationFile))
                {
                    File.Copy(sourceFile, destinationFile, overwrite: overwriteExisting);
                }
            }
        }

        private static string BuildUniqueRenamePath(string destinationPath)
        {
            if (Directory.Exists(destinationPath))
            {
                for (var i = 1; i <= 1000; i++)
                {
                    var candidate = $"{destinationPath}.meta-agent-{i}";
                    if (!Directory.Exists(candidate) && !File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
                throw new InvalidOperationException($"Unable to find unique renamed path for directory: {destinationPath}");
            }

            var dir = Path.GetDirectoryName(destinationPath) ?? Directory.GetCurrentDirectory();
            var name = Path.GetFileNameWithoutExtension(destinationPath);
            var ext = Path.GetExtension(destinationPath);
            for (var i = 1; i <= 1000; i++)
            {
                var candidate = Path.Combine(dir, $"{name}.meta-agent-{i}{ext}");
                if (!File.Exists(candidate) && !Directory.Exists(candidate))
                {
                    return candidate;
                }
            }
            throw new InvalidOperationException($"Unable to find unique renamed path for file: {destinationPath}");
        }

        private static void RemovePath(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
                return;
            }

            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static string NormalizeSafeRelativePath(string raw, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                throw new InvalidOperationException($"{fieldName} must be a non-empty relative path");
            }

            var normalized = raw.Trim().Replace('\\', '/');
            if (Path.IsPathRooted(normalized))
            {
                throw new InvalidOperationException($"{fieldName} must be relative: {raw}");
            }

            var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
            {
                throw new InvalidOperationException($"{fieldName} must be a non-empty relative path");
            }

            foreach (var segment in segments)
            {
                if (segment == "..")
                {
                    throw new InvalidOperationException($"{fieldName} must not traverse parent directories: {raw}");
                }
            }

            return string.Join('/', segments);
        }

        private sealed class TemplateComposeManifest
        {
            public string BasePath { get; }
            public string OverlayRootPath { get; }
            public Dictionary<string, TemplateComposeConfig> Templates { get; }

            private TemplateComposeManifest(string basePath, string overlayRootPath, Dictionary<string, TemplateComposeConfig> templates)
            {
                BasePath = basePath;
                OverlayRootPath = overlayRootPath;
                Templates = templates;
            }

            public static TemplateComposeManifest Load(string manifestPath)
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(manifestPath));
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                {
                    throw new InvalidOperationException($"manifest must be a JSON object: {manifestPath}");
                }

                var basePath = NormalizeSafeRelativePath(ReadRequiredString(root, "base"), "base");
                var overlayRootPath = NormalizeSafeRelativePath(ReadRequiredString(root, "overlayRoot"), "overlayRoot");

                if (!root.TryGetProperty("templates", out var templatesElement) || templatesElement.ValueKind != JsonValueKind.Object)
                {
                    throw new InvalidOperationException("manifest field 'templates' must be a non-empty object");
                }

                var templates = new Dictionary<string, TemplateComposeConfig>(StringComparer.OrdinalIgnoreCase);
                foreach (var templateProperty in templatesElement.EnumerateObject())
                {
                    var templateName = templateProperty.Name?.Trim() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(templateName))
                    {
                        throw new InvalidOperationException("manifest template keys must be non-empty strings");
                    }

                    if (templateProperty.Value.ValueKind != JsonValueKind.Object)
                    {
                        throw new InvalidOperationException($"template '{templateName}': config must be an object");
                    }

                    templates[templateName] = TemplateComposeConfig.Parse(templateName, templateProperty.Value);
                }

                if (templates.Count == 0)
                {
                    throw new InvalidOperationException("manifest field 'templates' must be a non-empty object");
                }

                return new TemplateComposeManifest(basePath, overlayRootPath, templates);
            }

            public void ComposeTemplate(string sourceRoot, string outputRoot, string templateName)
            {
                if (!Templates.TryGetValue(templateName, out var template))
                {
                    throw new InvalidOperationException($"template '{templateName}' is not defined in template composition manifest");
                }

                var baseDirectory = Path.Combine(sourceRoot, BasePath);
                if (!Directory.Exists(baseDirectory))
                {
                    throw new DirectoryNotFoundException($"base template directory not found: {baseDirectory}");
                }

                var overlayRootDirectory = Path.Combine(sourceRoot, OverlayRootPath);
                if (!Directory.Exists(overlayRootDirectory))
                {
                    throw new DirectoryNotFoundException($"overlay root directory not found: {overlayRootDirectory}");
                }

                var stagingRoot = Path.Combine(Path.GetTempPath(), $"meta-agent-template-compose-{Guid.NewGuid():N}");
                var stagingTemplateDirectory = Path.Combine(stagingRoot, templateName);
                Directory.CreateDirectory(stagingTemplateDirectory);

                try
                {
                    CopyDirectory(baseDirectory, stagingTemplateDirectory);

                    foreach (var overlay in template.Overlays)
                    {
                        var overlayPath = Path.Combine(overlayRootDirectory, overlay.Replace('/', Path.DirectorySeparatorChar));
                        if (!Directory.Exists(overlayPath))
                        {
                            throw new DirectoryNotFoundException($"overlay directory for template '{templateName}' not found: {overlayPath}");
                        }

                        CopyDirectory(overlayPath, stagingTemplateDirectory);
                    }

                    foreach (var rel in template.Remove)
                    {
                        var targetPath = Path.Combine(stagingTemplateDirectory, rel.Replace('/', Path.DirectorySeparatorChar));
                        RemovePath(targetPath);
                    }

                    var missingRequired = new List<string>();
                    foreach (var rel in template.Required)
                    {
                        var targetPath = Path.Combine(stagingTemplateDirectory, rel.Replace('/', Path.DirectorySeparatorChar));
                        if (!File.Exists(targetPath) && !Directory.Exists(targetPath))
                        {
                            missingRequired.Add(rel);
                        }
                    }

                    if (missingRequired.Count > 0)
                    {
                        throw new InvalidOperationException(
                            $"template '{templateName}' missing required paths after compose: {string.Join(", ", missingRequired)}");
                    }

                    Directory.CreateDirectory(outputRoot);
                    var outputTemplateDirectory = Path.Combine(outputRoot, templateName);
                    if (Directory.Exists(outputTemplateDirectory))
                    {
                        Directory.Delete(outputTemplateDirectory, recursive: true);
                    }

                    CopyDirectory(stagingTemplateDirectory, outputTemplateDirectory);
                }
                finally
                {
                    if (Directory.Exists(stagingRoot))
                    {
                        Directory.Delete(stagingRoot, recursive: true);
                    }
                }
            }

            private static string ReadRequiredString(JsonElement root, string propertyName)
            {
                if (!root.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.String)
                {
                    throw new InvalidOperationException($"manifest field '{propertyName}' must be a non-empty string");
                }

                var value = element.GetString()?.Trim() ?? string.Empty;
                if (value.Length == 0)
                {
                    throw new InvalidOperationException($"manifest field '{propertyName}' must be a non-empty string");
                }

                return value;
            }
        }

        private sealed class TemplateComposeConfig
        {
            public List<string> Overlays { get; }
            public List<string> Remove { get; }
            public List<string> Required { get; }

            private TemplateComposeConfig(List<string> overlays, List<string> remove, List<string> required)
            {
                Overlays = overlays;
                Remove = remove;
                Required = required;
            }

            public static TemplateComposeConfig Parse(string templateName, JsonElement config)
            {
                var overlays = ParseRelativeStringList(templateName, "overlays", config);
                var remove = ParseRelativeStringList(templateName, "remove", config);
                var required = ParseRelativeStringList(templateName, "required", config);
                return new TemplateComposeConfig(overlays, remove, required);
            }

            private static List<string> ParseRelativeStringList(string templateName, string propertyName, JsonElement config)
            {
                if (!config.TryGetProperty(propertyName, out var element))
                {
                    return new List<string>();
                }

                if (element.ValueKind != JsonValueKind.Array)
                {
                    throw new InvalidOperationException($"template '{templateName}': {propertyName} must be a list of non-empty strings");
                }

                var values = new List<string>();
                foreach (var item in element.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.String)
                    {
                        throw new InvalidOperationException($"template '{templateName}': {propertyName} must be a list of non-empty strings");
                    }

                    var raw = item.GetString()?.Trim() ?? string.Empty;
                    if (raw.Length == 0)
                    {
                        throw new InvalidOperationException($"template '{templateName}': {propertyName} must be a list of non-empty strings");
                    }

                    values.Add(NormalizeSafeRelativePath(raw, $"{templateName}.{propertyName}"));
                }

                return values;
            }
        }

        public static void CreateDefaultPolicy(string path)
        {
            CreateDefaultPolicy(path, new Policy());
        }

        public static void CreateDefaultPolicy(string path, Policy policy)
        {
            var json = policy.ToJson();
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? Directory.GetCurrentDirectory());
            File.WriteAllText(path, json);
        }
    }
}
