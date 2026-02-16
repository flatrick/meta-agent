# DOC_DELTA Update Contract

Use this file as a minimal operational log for release-critical events that are not architecture decisions.

Rules (required):
- Entry header format: `## YYYY-MM-DD HH:MM:SSZ - Title`.
- Timestamps must be UTC (`Z`) and exact to seconds.
- Global order is ascending by timestamp (oldest first, newest last).
- Add new entries at the end only.
- Architecture decisions belong in `meta-agent/docs/architecture/site/adrs/`.

## 2026-02-14 00:00:00Z - Initial baseline

- Canonical architecture decisions are recorded in `meta-agent/docs/architecture/site/adrs/`.
- Dotnet is the out-of-box scaffold baseline for this repository.
- Dotnet scaffold layout uses `src/`, `tests/`, and a root `<project-name>.slnx` solution.
- Verification:
  - `python3 ./meta-agent/scripts/pre-release-verify.py --skip-clean-apply`
