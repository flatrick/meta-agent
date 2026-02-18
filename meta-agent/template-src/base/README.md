# {{ project_name }}

This is a generic template scaffolded by `meta-agent`.

- Purpose: quickstart skeleton (README, LICENSE)
- Governance: add `.meta-agent-policy.json` to customize behavior

## Architecture verification

- All platforms: `python3 ./scripts/verify-architecture.py`
- Default helper scripts are Python-based.
- Python 3 is only required if you use the bundled scripts; teams may replace/remove them if equivalent checks are maintained.
- Script catalog and usage examples: `scripts/README.md`
- Markdown link/backlink scan: `python3 ./scripts/scan-markdown-links.py [--fail-on-dead]`
- Johnny.Decimal validation gate: `python3 ./scripts/validate-johnny-decimal.py --fail-on-issues`
- Johnny.Decimal structure helper: `python3 ./scripts/add-johnny-decimal-entry.py --help`

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
