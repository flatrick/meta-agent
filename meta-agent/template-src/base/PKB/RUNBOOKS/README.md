# Runbooks

`PKB/RUNBOOKS/` is temporary staging only.
Promote stable runbooks into canonical `docs/` paths and keep only summary pointers in PKB.

## Staging Metadata (Required)

- `staging_status`: `staging`
- `staged_at_utc`: `{{ generated_at_utc }}`
- `last_reviewed_at_utc`: `{{ generated_at_utc }}`
- `promotion_target_path`: `docs/architecture/internal/1x/12/12.01/index.md`
- `not_promoted_reason`: `Runbooks are scaffold placeholders pending project-specific operational procedures.`

Create focused runbooks for:

- local development
- testing
- debugging

Keep each runbook short, reproducible, and linked to evidence.
