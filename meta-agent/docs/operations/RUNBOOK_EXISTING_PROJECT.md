# Runbook: Existing Project Onboarding

Purpose: onboard an existing repository without scaffold side effects.

## Preconditions

- Existing target repository path is available.
- `.NET 10` SDK installed.
- Use this flow for legacy repositories (including `.NET Framework` maintenance targets).

## Workflow

1. Configure governance artifacts (no template scaffold writes):
```bash
dotnet run --project ./meta-agent/dotnet/MetaAgent.Cli -- configure --repo ../existing-service --requested-autonomy A1 --tokens-requested 100 --tickets-requested 1 --open-prs 0
```
2. Validate policy and gates:
```bash
dotnet run --project ./meta-agent/dotnet/MetaAgent.Cli -- validate --policy ../existing-service/.meta-agent-policy.json
```
3. For autonomous flows, include ticket context and mode:
```bash
dotnet run --project ./meta-agent/dotnet/MetaAgent.Cli -- validate --policy ../existing-service/.meta-agent-policy.json --mode autonomous_ticket_runner --requested-autonomy A2 --ticket-file ./ticket.txt
```

## Expected Artifacts

- `../existing-service/.meta-agent-policy.json`
- `../existing-service/.meta-agent-decision.json`
- `../existing-service/.meta-agent-workflow.json`
- `../existing-service/.meta-agent-run-result.json`
- `../existing-service/.meta-agent-metrics.json`

## Notes

- `configure` is the primary path for pre-existing repositories.
- `.NET Framework` repositories should use `configure` + `validate` only (no new-project scaffolding).
- If `validate` blocks, inspect `.meta-agent-decision.json` and `.meta-agent-workflow.json` first.
