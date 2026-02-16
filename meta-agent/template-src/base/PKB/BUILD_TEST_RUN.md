# Build, Test, Run

`PKB/` is temporary staging only.
Promote stable command/runbook guidance into canonical `docs/` pages to avoid duplicated operational docs.

## Staging Metadata (Required)

- `staging_status`: `staging`
- `staged_at_utc`: `{{ generated_at_utc }}`
- `last_reviewed_at_utc`: `{{ generated_at_utc }}`
- `promotion_target_path`: `docs/architecture/internal/1x/12/12.01/index.md`
- `not_promoted_reason`: `Initial command set is a bootstrap baseline and must be validated against real build/test/run workflows.`

Document minimal local developer loop commands.

## Build

- TODO

## Test

- TODO

## Run

- TODO

## Architecture verification

- All platforms: `python3 ./scripts/verify-architecture.py`
