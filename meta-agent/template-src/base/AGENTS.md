# AGENTS.md

## Mission

Use this repository's architecture and documentation model as the default contract for all AI/LLM and human-assisted changes.

## Canonical Architecture Rules

1. Structurizr DSL is the canonical architecture source:
   - `docs/architecture/site/workspace.dsl`
2. Published architecture docs are only:
   - `docs/architecture/site/_docs/`
   - `docs/architecture/site/model/**/_docs/`
3. Internal/non-published architecture notes are only:
   - `docs/architecture/internal/` (Johnny.Decimal)
4. Do not create parallel architecture documentation systems.
5. If code and model disagree, report mismatch explicitly and update intentionally.

## Mandatory Capability: Exploration & Knowledge Bootstrap Agent

This capability is required in all generated setups.

### Purpose

- Explore an existing codebase.
- Align code with the Structurizr C4 model.
- Generate temporary Project Knowledge Base (`PKB/`) staging artifacts.
- Reduce future context/token usage through structured retrieval artifacts.
- Uplift immature teams through enforced architectural discipline.

### Assumptions

- Structurizr and `structurizr-site-generatr` are mandatory.
- Bundled helper scripts in `scripts/` are Python-based by default.
- Python 3 is required only when using those bundled scripts.
- Teams may replace/remove bundled scripts if equivalent verification and workflow checks are preserved.
- Structurizr DSL is canonical (`docs/architecture/site/workspace.dsl`).
- Parallel architecture documentation systems are not permitted.

### Agent Definition

- Agent name: `exploration-agent`
- Primary responsibilities:
  1. Architecture alignment (C4-first)
  2. Operational documentation baseline
  3. Knowledge base construction
  4. Drift detection
  5. Team uplift guidance
- Operating mode:
  - Read-only by default.
  - No code or DSL modifications unless explicitly authorized.
  - Reports and improvement proposals first.

### Workflow: `/explore`

Phase 1: Structurizr Verification
- Locate DSL and site generation configuration.
- Identify site generation command.
- If execution is allowed, verify site builds.
- Extract systems, containers, components, relationships, and deployment model.
- Outputs:
  - `PKB/STRUCTURIZR_GUIDE.md`
  - `PKB/ARCHITECTURE_ALIGNMENT.md` (initial)

Phase 2: Code Alignment
- For each container, confirm code location, entrypoints, and responsibilities.
- Detect code not modeled, model not backed by code, and relationship mismatches.
- Outputs:
  - `PKB/ARCHITECTURE_ALIGNMENT.md` (updated)
  - `PKB/ENTRYPOINTS.md`

Phase 3: Operational Baseline
- Discover build, test, and run commands.
- If execution is allowed, validate minimal execution.
- Create runbooks for local development, testing, and debugging.
- Outputs:
  - `PKB/BUILD_TEST_RUN.md`
  - `PKB/RUNBOOKS/*`
  - `PKB/CHANGE_GUIDE.md`

Phase 4: Knowledge Structuring
- Build structured retrieval index at `PKB/INDEX/AGENT_INDEX.json`.
- Required top-level keys:
  - `containers`
  - `components`
  - `flows`
  - `commands`
  - `runbooks`
  - `invariants`

Phase 5: Uplift Report
- Produce top 5 architectural improvements.
- Produce top 5 documentation improvements.
- Produce top 5 workflow improvements.
- Prioritize actionable, small improvements.

### PKB Scaffold

`PKB/` must contain:
- `PROJECT_SNAPSHOT.md`
- `STRUCTURIZR_GUIDE.md`
- `ARCHITECTURE_ALIGNMENT.md`
- `ENTRYPOINTS.md`
- `BUILD_TEST_RUN.md`
- `CHANGE_GUIDE.md`
- `DOMAIN_GLOSSARY.md`
- `RUNBOOKS/`
- `INDEX/AGENT_INDEX.json`
- `KNOWN_UNKNOWNS.md`

`SYSTEM_MAP.md` must not exist.
Architecture summaries must reference C4 element names.

### PKB Lifecycle (Temporary Staging Only)

- `PKB/` is a temporary staging area, not a long-term documentation home.
- When PKB content is stable, promote it into `docs/` in the appropriate canonical location.
- Avoid duplicate truth across `PKB/` and `docs/`:
  - After promotion, keep only a short summary + canonical pointer in `PKB/`.
  - If a canonical `docs/` page already exists, update it directly and use PKB only for short-lived draft notes.
- During `/explore` and `/explore:drift`, every substantive PKB section should include a planned `docs/` target path.
- Every `PKB/*.md` artifact must include `Staging Metadata (Required)` with:
  - `staging_status`
  - `staged_at_utc`
  - `last_reviewed_at_utc`
  - `promotion_target_path`
  - `not_promoted_reason`
- While `staging_status` is `staging`, `last_reviewed_at_utc` must be refreshed at least every 30 days.
- `not_promoted_reason` is mandatory while staged so stale and blocked promotion items are auditable.
- `scripts/check-pkb-staging.py` enforces metadata presence/format and stale thresholds.

### Rules And Constraints

1. Structurizr is canonical.
2. No duplicate architecture systems.
3. Architectural mismatches are reported, not silently fixed.
4. Label all findings as `FACT`, `INFERENCE`, or `UNKNOWN`.
5. Include a source pointer for each claim (path + symbol where possible).
6. Avoid large code pastes.
7. Stop deep tracing once responsibility boundaries are clear.
8. Completion criteria:
   - Site generation verified or failure documented.
   - All containers mapped to code.
   - Top 3 flows understood.
   - Local dev loop documented.
   - Retrieval index built.

### Integration Contract

When `meta-agent` generates a project, it must include:
- `exploration-agent` definition.
- `/explore` workflow.
- `PKB/` scaffold.
- Structurizr alignment rules.

`exploration-agent` must be callable:
- Manually (interactive mode).
- Automatically (after initial setup).
- Periodically (drift-check mode).

Outputs must be structured and reproducible.

### Drift Mode: `/explore:drift`

- Re-scan DSL.
- Compare DSL to code.
- Update `PKB/ARCHITECTURE_ALIGNMENT.md`.
- Report new mismatches.
- Do not modify DSL automatically.

### Cultural Goal

- Enforce C4 thinking.
- Prevent architecture drift.
- Reduce token waste in future agent operations.
- Improve immature teams through structured artifacts.

### Pre-Existing Instruction Compliance

- The `Required Pre-Edit Reading` section in this file remains mandatory before running `/explore` or `/explore:drift`.
- `PKB/` artifacts are temporary retrieval/workflow staging aids, not a replacement architecture system.
- Stable PKB content must be promoted into canonical `docs/` pages to prevent long-lived duplication.
- Timestamp + reason metadata is required on staged PKB artifacts to make stale content discoverable.
- Canonical architecture rules still apply:
  - Structurizr DSL remains canonical (`docs/architecture/site/workspace.dsl`).
  - Published architecture docs remain constrained to `docs/architecture/site/_docs/` and `docs/architecture/site/model/**/_docs/`.
  - Internal architecture notes remain constrained to `docs/architecture/internal/` with Johnny.Decimal organization.
- Exploration outputs must report mismatches and proposed changes first; model/code/doc updates are intentional follow-up actions.
- Any architecture-affecting follow-up work still uses the existing Definition of Done and verification commands in this file.

## Johnny.Decimal Working Rules

- Start at `docs/architecture/internal/index.md`.
- Use active areas and categories; do not create placeholder-only trees.
- Keep area/category numbering stable once introduced.
- `.00` metadata folders are optional and only used when local category rules are needed.

## Required Pre-Edit Reading

- `docs/architecture/internal/4x/41/41.01/index.md` (Structurizr quickstart)
- `docs/architecture/internal/4x/41/41.02/index.md` (Structurizr playbook)
- `docs/architecture/internal/4x/41/41.03/index.md` (compatibility matrix)
- `docs/architecture/internal/4x/41/41.04/index.md` (agent bootstrap/config)
- `docs/architecture/site/AGENT_WRITING_GUIDELINES.md` (published-doc writing rules)

## Required Verification Before Completion

Default verification command (bundled helper; replaceable with equivalent local tooling):

```bash
python3 ./scripts/verify-architecture.py
```

This verification command also runs `scripts/check-pkb-staging.py` to enforce PKB staging metadata and stale-review limits.

## Definition Of Done (Architecture-Affecting Changes)

- DSL, docs, and implementation are coherent.
- Published docs remain within the Structurizr publication boundary.
- Internal docs are updated in the correct Johnny.Decimal bucket.
- Architecture verification script passes.
