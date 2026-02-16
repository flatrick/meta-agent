# meta-agent

Top-level repository for the `meta-agent` tool.

## Primary docs

- Agent entry instructions: `AGENTS.md`
- Main project README: `meta-agent/README.md`
- Project map: `meta-agent/PROJECT_MAP.md`
- Operations usage guide: `meta-agent/docs/operations/USAGE_GUIDE.md`
- Architecture docs: `meta-agent/docs/architecture/internal/40-governance/40-03-structurizr-tooling.md`
- Published GitHub Pages site: <https://flatrick.github.io/meta-agent/master/>

## Quickstart

```bash
dotnet msbuild ./meta-agent/dotnet/MetaAgent.slnx -restore -m:1 -nr:false -v:minimal
dotnet test ./meta-agent/dotnet/MetaAgent.slnx
```

## License

This repository is licensed under the MIT License. See `LICENSE`.
