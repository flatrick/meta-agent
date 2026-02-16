# meta-agent Project Map

Use this as the fast navigation index for humans and agents.

## Product Code

- CLI entrypoint and commands: `meta-agent/dotnet/MetaAgent.Cli/`
- Core domain/runtime logic: `meta-agent/dotnet/MetaAgent.Core/`
- Automated tests: `meta-agent/dotnet/MetaAgent.Tests/`
- Solution file: `meta-agent/dotnet/MetaAgent.slnx`

## Templates (Scaffold Output)

- Template root: `meta-agent/templates/`
- `meta-agent/templates/` is generated output (composed from `meta-agent/template-src/`), not source-of-truth content.
- Template composition source root (base + overlays): `meta-agent/template-src/`
- Template composition manifest: `meta-agent/template-src/manifest.json`
- .NET template: `meta-agent/templates/dotnet/`
- Node template: `meta-agent/templates/node/`
- Generic template: `meta-agent/templates/generic/`
- PowerShell template: `meta-agent/templates/powershell/`

Each template contains:
- scaffold files (`README`, runtime files, CI samples)
- Structurizr model and ADRs under `docs/architecture/site/`
- Structurizr-published docs under `docs/architecture/site/_docs/`

## Documentation

- Canonical docs root: `meta-agent/docs/`
- Published docs scopes (Structurizr):
  - workspace-level: `meta-agent/docs/architecture/site/_docs/`
  - element-level: `meta-agent/docs/architecture/site/model/**/_docs/`
- Internal/non-published architecture notes (Johnny.Decimal subfolders): `meta-agent/docs/architecture/internal/`
- Internal architecture index root: `meta-agent/docs/architecture/internal/index.md`
- 1x metadata index: `meta-agent/docs/architecture/internal/1x/10/index.md`
- Internal JD bucket map (1x-9x): `meta-agent/docs/architecture/internal/index.md#jd-bucket-map-1x-9x`
- Internal architecture modeling detail: `meta-agent/docs/architecture/internal/1x/11/`
- Internal architecture governance/tooling detail: `meta-agent/docs/architecture/internal/1x/12/`
- Internal architecture roadmap detail: `meta-agent/docs/architecture/internal/3x/31/`
- Internal Structurizr documentation/publishing guidance: `meta-agent/docs/architecture/internal/4x/41/`
- Structurizr usage + generation guide: `meta-agent/docs/architecture/internal/4x/41/41.01/index.md`
- Operational usage guide: `meta-agent/docs/operations/USAGE_GUIDE.md`
- Operator runbook: `meta-agent/docs/operations/runbook.md`
- Roadmap/status: `meta-agent/docs/planning/IMPLEMENTATION_ROADMAP.md`

## Architecture Model

- Workspace DSL: `meta-agent/docs/architecture/site/workspace.dsl`
- ADR source for model: `meta-agent/docs/architecture/site/adrs/`
- Generated site output (ephemeral): `meta-agent/docs/architecture/site/build/`

## Tooling and Automation

- Scripts: `meta-agent/scripts/`
- Template composition script: `meta-agent/scripts/compose-templates.py`
- Template composition tests (Python): `meta-agent/scripts/test-compose-templates.py`
- Template layout config (transitional source of truth): `meta-agent/config/template-layout.json`
- Cleanup helper: `meta-agent/scripts/clean-worktree.py`
- Structurizr wrapper: `meta-agent/scripts/structurizr-site.py`
- Architecture verification script: `meta-agent/scripts/verify-architecture.py`
- Structurizr wrapper tests (Python): `meta-agent/scripts/test-structurizr-site-wrappers.py`
- Markdown link/backlink scanner (Python): `meta-agent/scripts/scan-markdown-links.py`
- Markdown link scanner tests (Python): `meta-agent/scripts/test-scan-markdown-links.py`
- DOC_DELTA manager (Python): `meta-agent/scripts/manage-doc-delta.py`
- DOC_DELTA manager tests (Python): `meta-agent/scripts/test-manage-doc-delta.py`
- Release packaging (Python): `meta-agent/scripts/package-release.py`
- Release packaging test (Python): `meta-agent/scripts/test-package-release.py`
- Root GitHub workflow for this repository: `.github/workflows/ci.yml`
- GitLab CI for this repository: `meta-agent/ci/gitlab-ci.yml`

## Governance and History

- Playbook: `meta-agent/PLAYBOOK.md`
- Doc delta log: `meta-agent/DOC_DELTA.md`
- Policy schema: `meta-agent/schema/meta-agent-policy.schema.json`
