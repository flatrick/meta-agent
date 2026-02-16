# Change Guide

`PKB/` is temporary staging only.
Promote stable contributor guidance into canonical `docs/` and keep this file as a short pointer.

## Staging Metadata (Required)

- `staging_status`: `staging`
- `staged_at_utc`: `{{ generated_at_utc }}`
- `last_reviewed_at_utc`: `{{ generated_at_utc }}`
- `promotion_target_path`: `docs/architecture/internal/1x/12/12.01/index.md`
- `not_promoted_reason`: `Initial change guidance is generic and must be aligned with project-specific governance before promotion.`

Use this as a concise impact guide for contributors.

## Before changing architecture-related code

1. Review `docs/architecture/site/workspace.dsl` and relevant C4 element docs.
2. Confirm current alignment in `PKB/ARCHITECTURE_ALIGNMENT.md`.
3. Plan updates to model, docs, and code intentionally.

## After changes

1. Update affected PKB entries.
2. Promote stable PKB content to canonical `docs/` paths.
3. Run architecture verification.
4. Record remaining `UNKNOWN` items.
