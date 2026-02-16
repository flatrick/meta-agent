# Agent Entry Instructions

Read this file before doing work in this repository.

## Read First

1. `meta-agent/PROJECT_MAP.md`
2. `meta-agent/PLAYBOOK.md`
3. `meta-agent/docs/README.md`

## Repository Intent

- This repository hosts the `meta-agent` project under `meta-agent/`.
- Architecture publication scope is `meta-agent/docs/architecture/site/`.
- Internal/non-published architecture working docs are under `meta-agent/docs/architecture/internal/`.

## Required Agent Workflow

1. Use `meta-agent/PROJECT_MAP.md` to locate files before editing.
2. When checking documentation links/backlinks, do not manually scan all markdown files.
3. Run `python3 ./meta-agent/scripts/scan-markdown-links.py` for discovery/reporting.
4. Run `python3 ./meta-agent/scripts/scan-markdown-links.py --fail-on-dead` for verification gates.
5. Use report output `.meta-agent-temp/markdown-link-report.json` for machine-readable data.
6. Use report output `.meta-agent-temp/markdown-link-report.md` for quick human review.
7. For scaffold template maintenance, edit `meta-agent/template-src/` first (base + overlays), not `meta-agent/templates/` directly.
8. After template-source edits, run `python3 ./meta-agent/scripts/compose-templates.py` then `python3 ./meta-agent/scripts/compose-templates.py --check`.
9. For release-operational notes in `meta-agent/DOC_DELTA.md`, use `python3 ./meta-agent/scripts/manage-doc-delta.py add ...` instead of manual insertion.
10. If `meta-agent/DOC_DELTA.md` changed, run `python3 ./meta-agent/scripts/manage-doc-delta.py check` before completion.

## Architecture Workflow

- Use `python3 ./meta-agent/scripts/structurizr-site.py generate` to generate the site.
- Use `python3 ./meta-agent/scripts/verify-architecture.py` for architecture verification checks.
- Structurizr guidance lives at `meta-agent/docs/architecture/internal/40-governance/40-03-structurizr-tooling.md`.

## Safety

- Keep changes scoped and traceable.
- Do not introduce parallel architecture systems outside Structurizr DSL + configured docs scopes.
