# Agent Bootstrap And Configuration

Use this page to bootstrap humans and AI/LLM agents in scaffolded projects.

## Required references

- `docs/architecture/site/workspace.dsl` (canonical architecture source)
- `docs/architecture/site/AGENT_WRITING_GUIDELINES.md` (published-doc writing rules)
- `docs/architecture/internal/index.md` (internal Johnny.Decimal navigation)
- `docs/architecture/internal/4x/41/41.02/index.md` (Structurizr playbook)
- `docs/architecture/internal/4x/41/41.03/index.md` (compatibility matrix)

## Project configuration

- `AGENTS.md` is scaffolded at repository root; keep it aligned with this guide.
- Confirm `.meta-agent-policy.json` exists and reflects your team mode/autonomy/budget defaults.
- Require Python 3 for all repository scripts on Windows, Linux, and macOS.
- Keep architecture publication boundary in `docs/architecture/site/`.
- Keep internal/non-published notes in `docs/architecture/internal/` using Johnny.Decimal buckets.

## Agent prompt contract (recommended)

When setting up an agent instruction/prompt file (for example `AGENTS.md`), include:

1. Structurizr DSL is canonical (`docs/architecture/site/workspace.dsl`).
2. Published docs only under `docs/architecture/site/_docs/` and `docs/architecture/site/model/**/_docs/`.
3. Internal working docs under `docs/architecture/internal/` with Johnny.Decimal numbering.
4. Model/code mismatches must be reported explicitly (no silent architecture drift).
5. Run architecture verification before completing architecture-related changes.

## Mandatory capability: exploration-agent

All generated setups must include an `exploration-agent` capability with:

- workflow commands: `/explore` and `/explore:drift`
- read-first mode: report/propose before changing code or DSL
- output location: `PKB/` temporary staging scaffold at repository root
- required index: `PKB/INDEX/AGENT_INDEX.json` with `containers`, `components`, `flows`, `commands`, `runbooks`, and `invariants`

Required `PKB/` scaffold:

- `PKB/PROJECT_SNAPSHOT.md`
- `PKB/STRUCTURIZR_GUIDE.md`
- `PKB/ARCHITECTURE_ALIGNMENT.md`
- `PKB/ENTRYPOINTS.md`
- `PKB/BUILD_TEST_RUN.md`
- `PKB/CHANGE_GUIDE.md`
- `PKB/DOMAIN_GLOSSARY.md`
- `PKB/RUNBOOKS/`
- `PKB/INDEX/AGENT_INDEX.json`
- `PKB/KNOWN_UNKNOWNS.md`

`SYSTEM_MAP.md` must not be created.

## PKB promotion rule (required)

- `PKB/` is temporary and must not become a second long-term documentation system.
- Promote stable PKB artifacts into canonical paths under `docs/`.
- Keep one canonical source of truth:
  - if canonical docs exist, update them directly
  - after promotion, keep only concise pointers in PKB stubs
- Require a target `docs/` path for each substantive PKB artifact.
- Require timestamped staging metadata on all PKB markdown artifacts:
  - `staging_status`
  - `staged_at_utc`
  - `last_reviewed_at_utc`
  - `promotion_target_path`
  - `not_promoted_reason`
- While staged, refresh `last_reviewed_at_utc` at least every 30 days and keep `not_promoted_reason` current.
- Use `python3 ./scripts/check-pkb-staging.py --pkb-root ./PKB --max-age-days 30 --fail-on-issues` to gate stale/incomplete PKB staging data.

## Exploration output compliance

`exploration-agent` outputs must follow existing project instructions:

- Structurizr DSL remains canonical (`docs/architecture/site/workspace.dsl`).
- Published architecture docs remain under:
  - `docs/architecture/site/_docs/`
  - `docs/architecture/site/model/**/_docs/`
- Internal architecture notes remain under `docs/architecture/internal/` using Johnny.Decimal.
- `PKB/` is temporary staging and does not replace canonical architecture documentation.
- Findings must be tagged as `FACT`, `INFERENCE`, or `UNKNOWN` with source pointers.
- Architecture mismatches are reported first, then addressed intentionally.


## Script entry points for humans and agents

Use the bundled script set to keep common architecture/documentation tasks consistent:

- Full script catalog + examples: `scripts/README.md`
- Architecture verification wrapper: `python3 ./scripts/verify-architecture.py`
- PKB staging metadata gate: `python3 ./scripts/check-pkb-staging.py --pkb-root ./PKB --max-age-days 30 --fail-on-issues`
- Markdown link/backlink reporting: `python3 ./scripts/scan-markdown-links.py [--fail-on-dead]`
- Johnny.Decimal validation: `python3 ./scripts/validate-johnny-decimal.py --fail-on-issues`
- Johnny.Decimal entry helper: `python3 ./scripts/add-johnny-decimal-entry.py --help`

## Quick verification commands

All platforms (Python required):

```bash
python3 ./scripts/verify-architecture.py
```
