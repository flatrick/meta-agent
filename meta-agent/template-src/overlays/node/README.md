# Node template

Usage:

- Install: `npm install`
- Run: `node index.js`
- Test: `npm test`

## Architecture verification

- All platforms: `python3 ./scripts/verify-architecture.py`
- Requirement: Python 3 must be installed; template scripts are Python-only.

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
