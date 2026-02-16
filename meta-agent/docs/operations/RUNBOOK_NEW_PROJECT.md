# Runbook: New Project Onboarding

Purpose: bootstrap a brand-new repository with meta-agent scaffolding and governance.

## Preconditions

- Repository root available and writable.
- `.NET 10` SDK installed.
- You are running from this repository root for the command examples below.

## Workflow

1. Scaffold the new repository:
```bash
dotnet run --project ./meta-agent/dotnet/MetaAgent.Cli -- init --template dotnet --target ../my-service --name my-service
```
2. Validate generated policy:
```bash
dotnet run --project ./meta-agent/dotnet/MetaAgent.Cli -- validate --policy ../my-service/.meta-agent-policy.json
```
3. If needed, set mode/autonomy explicitly and re-validate:
```bash
dotnet run --project ./meta-agent/dotnet/MetaAgent.Cli -- validate --policy ../my-service/.meta-agent-policy.json --mode interactive_ide --requested-autonomy A1
```

## Expected Artifacts

- `../my-service/.meta-agent-policy.json`
- `../my-service/.meta-agent-decision.json`
- `../my-service/.meta-agent-workflow.json`
- `../my-service/.meta-agent-run-result.json`
- `../my-service/.meta-agent-metrics.json`

## Notes

- Policy files are versioned (`policyVersion`) and auto-migrated when needed.
- For interactive sessions, non-trivial commands require plan approval unless pre-approved.
- For release readiness in this repository, use:
  - `python3 ./meta-agent/scripts/pre-release-verify.py --tag v1.2.3`
