# Runbook: Interactive IDE / Developer-Assist

Purpose: day-2 operating flow when a developer is in the loop (Cursor/IDE style).

## Operating Model

- Mode: `interactive_ide`
- Typical autonomy: `A1`
- Governance: soft token warning profile + explicit approvals for high-cost/risky actions.

## Daily Task Flow

1. Triage non-trivial task:
```bash
dotnet run --project ./meta-agent/dotnet/MetaAgent.Cli -- triage --ticket "Implement feature X with API and data changes"
```
2. Validate with task context:
```bash
dotnet run --project ./meta-agent/dotnet/MetaAgent.Cli -- validate --policy .meta-agent-policy.json --mode interactive_ide --requested-autonomy A1 --ticket "Implement feature X with API and data changes"
```
3. Execute in IDE agent workflow.
4. Re-run `validate` if task scope/risk changes.

## Interactive-Specific Gates

- Non-trivial commands require plan approval (unless pre-approved).
- Ambiguity above threshold requires approval.
- High token-cost execution requires approval when policy says so.

## Failure Handling

If blocked (`5/6/7/9/10/11`):

1. Read `.meta-agent-decision.json`.
2. Read `.meta-agent-workflow.json`.
3. Fix input flags/policy/task framing.
4. Re-run `validate`.
