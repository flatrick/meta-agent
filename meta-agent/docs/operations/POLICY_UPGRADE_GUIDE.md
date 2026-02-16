# Policy Upgrade Guide (`policyVersion`)

Purpose: operator-facing guidance for policy migration and troubleshooting.

## Current Version

- Supported policy version: `1`
- Policy field: `policyVersion`

## How Migration Works

- Legacy policies without `policyVersion` are treated as version `0`.
- `init`, `configure`, and `validate` auto-migrate version `0` policies to `1`.
- Migrated policy files are written back to disk deterministically.

## Recommended Upgrade Procedure

1. Keep a copy of current policy file:
```bash
cp .meta-agent-policy.json .meta-agent-policy.backup.json
```
2. Trigger migration using `validate`:
```bash
dotnet run --project ./meta-agent/dotnet/MetaAgent.Cli -- validate --policy .meta-agent-policy.json
```
3. Confirm `policyVersion` exists and equals `1`.
4. Re-run normal validation with task context as needed.

## Expected Migration Signal

- CLI logs a migration notice when it upgrades a legacy policy.
- Example outcome:
  - file now includes `"policyVersion": 1`
  - `validate` succeeds with exit code `0` when all other gates pass

## Failure Cases and Fixes

- Unsupported future policy version:
  - symptom: error indicating policy version is unsupported
  - fix: downgrade policy file to supported schema for this CLI build or update the CLI

- Invalid policyVersion type/value:
  - symptom: schema/validation failure (`policyVersion` must be positive integer)
  - fix: set `policyVersion` to a valid integer (`1` for current version)

- Legacy `preferredRuntime` key still present in older policy files:
  - symptom: no manual action needed; migration removes the key automatically on `init`, `configure`, or `validate`
  - fix: run `validate` once and confirm the persisted policy no longer contains `preferredRuntime`

- Migration appears not to persist:
  - verify file path passed to `--policy`
  - verify write permissions on target file/directory

## CI/Release Alignment

- Use local release readiness check including SemVer tag gate:
```bash
python3 ./meta-agent/scripts/pre-release-verify.py --tag v1.2.3
```
- CI pipelines use compatible SemVer tag gate behavior for release flows.
