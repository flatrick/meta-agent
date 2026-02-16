# Structurizr Guide

`PKB/` is temporary staging only.
Once this guidance is stable, promote it to canonical `docs/` paths and keep this file as a pointer.

## Staging Metadata (Required)

- `staging_status`: `staging`
- `staged_at_utc`: `{{ generated_at_utc }}`
- `last_reviewed_at_utc`: `{{ generated_at_utc }}`
- `promotion_target_path`: `docs/architecture/internal/4x/41/41.01/index.md`
- `not_promoted_reason`: `Initial scaffold guide has not yet been validated against the actual project model and tooling.`

## Canonical Source

- `docs/architecture/site/workspace.dsl`

## Verification command

- All platforms: `python3 ./scripts/verify-architecture.py`

## Exploration notes

- FACT: Document generated systems, containers, components, relationships, and deployment nodes.
- INFERENCE: Note likely model extensions.
- UNKNOWN: Track unresolved model gaps.
