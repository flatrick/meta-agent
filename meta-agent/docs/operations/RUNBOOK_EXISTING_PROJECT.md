# Runbook: Existing Project Onboarding

Purpose: onboard an existing repository as a new meta-agent user (agent-asset scaffold + governance).

## Preconditions

- Existing target repository path is available.
- `.NET 10` SDK installed.
- Use this flow for legacy repositories (including `.NET Framework` maintenance targets).

## Workflow

1. Initialize onboarding for existing repository (agent assets + policy):
```bash
dotnet run --project ./meta-agent/dotnet/MetaAgent.Cli -- init --target ../existing-service --existing-project --mode interactive_ide --requested-autonomy A1 --tokens-requested 100 --tickets-requested 1 --open-prs 0 --on-conflict merge
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
- `../existing-service/AGENTS.md`
- `../existing-service/PKB/`
- `../existing-service/docs/`
- `../existing-service/scripts/`

## Notes

- `init --existing-project` is the primary onboarding path for pre-existing repositories.
- `init --existing-project` scaffolds agent assets (`docs/`, `PKB/`, `scripts/`, `AGENTS.md`) but does not scaffold product code (`src/`, app entrypoints, solution files).
- Conflict handling strategies for onboarding existing repos: `--on-conflict stop|merge|replace|rename`.
- `configure` should be used after onboarding for governance-only reconfiguration.
- `.NET Framework` repositories should use `init --existing-project` + `validate` for onboarding/governance.
- If `validate` blocks, inspect `.meta-agent-decision.json` and `.meta-agent-workflow.json` first.
