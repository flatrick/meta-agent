# PowerShell template

Usage:

- Run: `pwsh -NoProfile -File ./src/main.ps1`
- Test (Pester): `pwsh -NoProfile -Command "Invoke-Pester -Configuration (Import-PowerShellDataFile ./tests/PesterConfiguration.psd1)"`
- If Pester is missing: `pwsh -NoProfile -Command "Install-Module Pester -Scope CurrentUser -Force -SkipPublisherCheck"`

## Architecture verification

- All platforms: `python3 ./scripts/verify-architecture.py`
- Default helper scripts are Python-based.
- Python 3 is only required if you use the bundled scripts.
- Note: this template intentionally ships operational/verification scripts only in Python to reduce duplicated maintenance across runtimes. Teams can replace/remove them and maintain PowerShell equivalents locally if preferred.

## Documentation and agent setup

- Agent operating contract: `AGENTS.md`
- Mandatory exploration capability: `AGENTS.md` (`exploration-agent`, `/explore`, `/explore:drift`)
- Project Knowledge Base scaffold: `PKB/` (temporary staging before promotion into canonical `docs/` paths)
- PKB staging gate: `python3 ./scripts/check-pkb-staging.py --pkb-root ./PKB --max-age-days 30 --fail-on-issues`
- Internal docs root: `docs/architecture/internal/index.md`
- Structurizr quickstart: `docs/architecture/internal/4x/41/41.01/index.md`
- Structurizr playbook: `docs/architecture/internal/4x/41/41.02/index.md`
- Compatibility matrix: `docs/architecture/internal/4x/41/41.03/index.md`
- Agent bootstrap/config guide: `docs/architecture/internal/4x/41/41.04/index.md`
- Published-doc writing rules: `docs/architecture/site/AGENT_WRITING_GUIDELINES.md`
