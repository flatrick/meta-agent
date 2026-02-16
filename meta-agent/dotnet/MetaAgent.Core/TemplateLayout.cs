using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace MetaAgent.Core
{
    public sealed class TemplateLayout
    {
        private readonly Dictionary<string, TemplateLayout> _templateOverrides;

        public string ArchitectureRoot { get; }
        public string WorkspaceDsl { get; }
        public string PublishedDocsRoot { get; }
        public string ModelDocsGlob { get; }
        public string AdrsRoot { get; }
        public string InternalArchitectureRoot { get; }
        public string InternalGovernanceRoot { get; }

        private TemplateLayout(
            string architectureRoot,
            string workspaceDsl,
            string publishedDocsRoot,
            string modelDocsGlob,
            string adrsRoot,
            string internalArchitectureRoot,
            string internalGovernanceRoot,
            Dictionary<string, TemplateLayout>? templateOverrides = null)
        {
            ArchitectureRoot = Normalize(architectureRoot);
            WorkspaceDsl = Normalize(workspaceDsl);
            PublishedDocsRoot = Normalize(publishedDocsRoot);
            ModelDocsGlob = Normalize(modelDocsGlob);
            AdrsRoot = Normalize(adrsRoot);
            InternalArchitectureRoot = Normalize(internalArchitectureRoot);
            InternalGovernanceRoot = Normalize(internalGovernanceRoot);
            _templateOverrides = templateOverrides ?? new Dictionary<string, TemplateLayout>(StringComparer.OrdinalIgnoreCase);
        }

        public static TemplateLayout Default { get; } = new TemplateLayout(
            architectureRoot: "architecture/site",
            workspaceDsl: "architecture/site/workspace.dsl",
            publishedDocsRoot: "architecture/site/_docs",
            modelDocsGlob: "architecture/site/model/**/_docs",
            adrsRoot: "architecture/site/adrs",
            internalArchitectureRoot: "docs/architecture/internal",
            internalGovernanceRoot: "docs/architecture/internal/1x/12",
            templateOverrides: null);

        public static TemplateLayout LoadFromFile(string path)
        {
            var json = File.ReadAllText(path);
            var dto = JsonSerializer.Deserialize<TemplateLayoutDto>(json, Policy.ReadJsonOptions) ?? new TemplateLayoutDto();
            var rootLayout = FromDto(dto, Default);
            var overrides = new Dictionary<string, TemplateLayout>(StringComparer.OrdinalIgnoreCase);
            if (dto.TemplateOverrides != null)
            {
                foreach (var kvp in dto.TemplateOverrides)
                {
                    if (string.IsNullOrWhiteSpace(kvp.Key))
                    {
                        continue;
                    }

                    overrides[kvp.Key.Trim()] = FromDto(kvp.Value ?? new TemplateLayoutDto(), rootLayout);
                }
            }

            return new TemplateLayout(
                architectureRoot: rootLayout.ArchitectureRoot,
                workspaceDsl: rootLayout.WorkspaceDsl,
                publishedDocsRoot: rootLayout.PublishedDocsRoot,
                modelDocsGlob: rootLayout.ModelDocsGlob,
                adrsRoot: rootLayout.AdrsRoot,
                internalArchitectureRoot: rootLayout.InternalArchitectureRoot,
                internalGovernanceRoot: rootLayout.InternalGovernanceRoot,
                templateOverrides: overrides);
        }

        public static TemplateLayout LoadFromFileOrDefault(string path)
        {
            return File.Exists(path) ? LoadFromFile(path) : Default;
        }

        public static TemplateLayout LoadFromRepositoryOrDefault(string startDirectory)
        {
            var dir = new DirectoryInfo(Path.GetFullPath(startDirectory));
            for (var i = 0; i < 12; i++)
            {
                var candidate = Path.Combine(dir.FullName, "meta-agent", "config", "template-layout.json");
                if (File.Exists(candidate))
                {
                    return LoadFromFile(candidate);
                }

                if (dir.Parent == null)
                {
                    break;
                }

                dir = dir.Parent;
            }

            return Default;
        }

        public TemplateLayout ResolveForTemplate(string? templateName)
        {
            if (string.IsNullOrWhiteSpace(templateName))
            {
                return this;
            }

            var normalized = templateName.Trim();
            if (_templateOverrides.TryGetValue(normalized, out var resolved))
            {
                return resolved;
            }

            return this;
        }

        private static TemplateLayout FromDto(TemplateLayoutDto dto, TemplateLayout baseLayout)
        {
            return new TemplateLayout(
                architectureRoot: string.IsNullOrWhiteSpace(dto.ArchitectureRoot) ? baseLayout.ArchitectureRoot : dto.ArchitectureRoot,
                workspaceDsl: string.IsNullOrWhiteSpace(dto.WorkspaceDsl) ? baseLayout.WorkspaceDsl : dto.WorkspaceDsl,
                publishedDocsRoot: string.IsNullOrWhiteSpace(dto.PublishedDocsRoot) ? baseLayout.PublishedDocsRoot : dto.PublishedDocsRoot,
                modelDocsGlob: string.IsNullOrWhiteSpace(dto.ModelDocsGlob) ? baseLayout.ModelDocsGlob : dto.ModelDocsGlob,
                adrsRoot: string.IsNullOrWhiteSpace(dto.AdrsRoot) ? baseLayout.AdrsRoot : dto.AdrsRoot,
                internalArchitectureRoot: string.IsNullOrWhiteSpace(dto.InternalArchitectureRoot) ? baseLayout.InternalArchitectureRoot : dto.InternalArchitectureRoot,
                internalGovernanceRoot: string.IsNullOrWhiteSpace(dto.InternalGovernanceRoot) ? baseLayout.InternalGovernanceRoot : dto.InternalGovernanceRoot);
        }

        private static string Normalize(string value)
        {
            var trimmed = value.Trim().Replace('\\', '/');
            return trimmed.Trim('/');
        }

        private sealed class TemplateLayoutDto
        {
            public string? ArchitectureRoot { get; set; }
            public string? WorkspaceDsl { get; set; }
            public string? PublishedDocsRoot { get; set; }
            public string? ModelDocsGlob { get; set; }
            public string? AdrsRoot { get; set; }
            public string? InternalArchitectureRoot { get; set; }
            public string? InternalGovernanceRoot { get; set; }
            public Dictionary<string, TemplateLayoutDto>? TemplateOverrides { get; set; }
        }
    }
}
