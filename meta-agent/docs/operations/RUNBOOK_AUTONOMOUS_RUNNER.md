# Runbook: Autonomous Ticket Runner

Purpose: day-2 flow for governed autonomous execution.

## Operating Model

- Mode: `autonomous_ticket_runner`
- Required autonomy: `A2` or `A3`
- Governance: strict hard-cap token profile + mandatory triage/safety evidence.

## Daily Task Flow

1. Ensure ticket input is explicit (`--ticket` or `--ticket-file`).
2. Run validate with autonomous mode:
```bash
dotnet run --project ./meta-agent/dotnet/MetaAgent.Cli -- validate --policy .meta-agent-policy.json --mode autonomous_ticket_runner --requested-autonomy A2 --ticket-file ./ticket.txt
```
3. If eligible/allowed, execute task automation.
4. Persist and review outputs for audit.

## Autonomous-Specific Gates

- Missing ticket context blocks execution.
- Ineligible triage blocks execution.
- Safety-level gates require explicit validation evidence.
- Strict token hard cap is enforced.

## Required Audit Artifacts

- `.meta-agent-triage.json`
- `.meta-agent-decision.json`
- `.meta-agent-workflow.json`
- `.meta-agent-run-result.json`
- `.meta-agent-metrics.json`

## Failure Handling

If blocked, resolve root cause directly from:

1. decision check failure detail
2. workflow blocked stage detail
3. missing safety/validation evidence
