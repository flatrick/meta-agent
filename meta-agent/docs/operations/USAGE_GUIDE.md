# Meta-Agent Usage Guide

Purpose: explain how teams should use `meta-agent` over time, not just how to run single commands.

## What This Tool Is

`meta-agent` is a governance and operating layer for AI-assisted software delivery.

It has two lifecycle roles:
- Bootstrap role: set up a repository for agent-aided development.
- Runtime role: continuously enforce safety, policy, workflow, and observability while work is being done.

## Who Uses It

There are two operator profiles.

### Developer-Assist Profile (Interactive IDE / Cursor-style)

A developer is present and uses an agent as a copilot.

Main objective:
- keep flow high
- keep guardrails visible
- require explicit confirmation for high-impact decisions

Typical mode:
- `interactive_ide`

### Autonomous Profile (Ticket Runner)

Agent executes tasks with minimal human intervention.

Main objective:
- strict risk control
- strict budget control
- deterministic audit trail

Typical mode:
- `autonomous_ticket_runner`

## Command Roles

`init`
- Use for new project scaffolding.
- Creates base structure and default policy when missing.

`configure`
- Use for existing repositories.
- Writes governance/config artifacts without scaffold template writes.
- This is the primary onboarding path for legacy repositories, including `.NET Framework`.

`triage`
- Converts ticket/task text into structured risk and validation strategy.
- Produces eligibility, risk score, safety level, and validation plan.

`validate`
- Enforces runtime policy gates before/around execution.
- Validates budgets, autonomy, safety gates, change boundaries, abort conditions, and workflow constraints.

## Day 0 Onboarding

### New Project

1. Scaffold repository:
```bash
dotnet run --project ./meta-agent/dotnet/MetaAgent.Cli -- init --template dotnet --target ../my-service --name my-service
```
2. Inspect generated policy:
- `../my-service/.meta-agent-policy.json`
3. Run validation:
```bash
dotnet run --project ./meta-agent/dotnet/MetaAgent.Cli -- validate --policy ../my-service/.meta-agent-policy.json
```

### Existing Project

1. Configure governance without scaffolding:
```bash
dotnet run --project ./meta-agent/dotnet/MetaAgent.Cli -- configure --repo ../existing-service --requested-autonomy A1 --tokens-requested 100 --tickets-requested 1 --open-prs 0
```
2. Validate policy and gates:
```bash
dotnet run --project ./meta-agent/dotnet/MetaAgent.Cli -- validate --policy ../existing-service/.meta-agent-policy.json
```

## Daily Operating Patterns

## Pattern A: Developer + Cursor (Recommended Default)

Use when a human is actively driving implementation.

1. Triage non-trivial task:
```bash
dotnet run --project ./meta-agent/dotnet/MetaAgent.Cli -- triage --ticket "Implement feature X with API and data changes"
```
2. Validate before execution:
```bash
dotnet run --project ./meta-agent/dotnet/MetaAgent.Cli -- validate --policy .meta-agent-policy.json --ticket "Implement feature X with API and data changes"
```
3. Execute in Cursor/IDE.
4. Re-run `validate` when task scope/risk changes.
5. Review generated artifacts for blocked decisions and required fixes.

Interactive plan-first behavior:
- In interactive developer sessions, non-trivial commands require plan approval before execution.
- Workflow records include plan confirmation/documentation stages.

## Pattern B: Autonomous Ticket Processing

Use for governed automation.

1. Provide explicit mode and autonomy:
```bash
dotnet run --project ./meta-agent/dotnet/MetaAgent.Cli -- validate --policy .meta-agent-policy.json --mode autonomous_ticket_runner --requested-autonomy A2 --ticket-file ./ticket.txt
```
2. Ensure triage eligibility and safety-gate evidence.
3. Block execution on gate failures, do not bypass with ad hoc edits.
4. Consume decision/workflow/run-result artifacts for audit.

## Triage + Validate Relationship

`triage` decides:
- is task eligible?
- how risky is it?
- what validation methods should be chosen/skipped?
- what safety level applies?

`validate` decides:
- may execution proceed under current policy and mode?
- are autonomy, budget, safety, and boundary gates satisfied?
- is ambiguity/approval state acceptable?

Use both for non-trivial work:
- `triage` first
- `validate` second

## Artifacts You Should Monitor

`.meta-agent-decision.json`
- gate-by-gate allow/block outcomes
- first file to inspect on blocked runs

`.meta-agent-workflow.json`
- stage progression and blocked stage reasons
- includes plan-first workflow fields for interactive sessions

`.meta-agent-triage.json`
- risk/eligibility/safety/validation strategy

`.meta-agent-run-result.json`
- structured execution summary for observability

`.meta-agent-metrics.json`
- longitudinal quality/cost metrics

## Governance Strategy by Mode

Developer-assist (`interactive_ide`):
- favor guidance + explicit confirmation
- require operator approval for high-impact plan execution
- keep developer-in-the-loop decisions explicit

Autonomous (`autonomous_ticket_runner`):
- enforce strict hard stops
- require stronger gates and evidence
- optimize for safe bounded throughput

## Legacy .NET Framework Guidance

Scope:
- maintenance/onboarding only
- not a target for new scaffold generation

Recommended flow:
1. `configure` existing repository
2. `validate` with policy + ticket context
3. use artifacts to enforce safe maintenance changes

## Regression and Drift Control

Use canonical harness regularly:
```bash
python3 ./meta-agent/scripts/run-regression-harness.py
```

Outputs:
- per-run: `.meta-agent-temp/regression-harness/runs/<run-id>/harness-summary.json`
- history: `.meta-agent-temp/regression-harness/history/harness-history.csv`

Use this to detect behavior drift after:
- model changes
- prompt/policy changes
- command/runtime refactors

## Practical Team Cadence

Per task:
1. triage (non-trivial tasks)
2. validate
3. execute
4. inspect artifacts

Per PR:
1. ensure policy/workflow/decision artifacts are consistent
2. ensure required tests pass
3. run markdown link/backlink scanner instead of manual markdown file scanning:
```bash
python3 ./meta-agent/scripts/scan-markdown-links.py --fail-on-dead
```
4. keep the latest pre-release verification summary for audit:
- `.meta-agent-temp/pre-release-verification/latest-summary.json`

Per release:
1. run pre-release verification with tag gate:
```bash
python3 ./meta-agent/scripts/pre-release-verify.py --tag v1.0.1
```
2. build a downloadable package:
```bash
python3 ./meta-agent/scripts/package-release.py
```
3. attach generated zip from:
- `.meta-agent-temp/release-packages/`
4. if needed, limit runtime set explicitly:
```bash
python3 ./meta-agent/scripts/package-release.py --runtime win-x64 --runtime linux-x64 --runtime osx-arm64
```

Per week:
1. review metrics scoreboard trends
2. review harness drift reports
3. tune policy thresholds and autonomy defaults

## When Runs Block

If blocked by exit code `5/6/7/9/10/11`:
1. Open `.meta-agent-decision.json` and `.meta-agent-workflow.json`
2. Identify failing check/stage
3. Fix input flags/policy/task shape
4. Re-run `validate` before continuing

## Related Docs

- Quick command overview: `meta-agent/README.md`
- Operational runbook: `meta-agent/docs/operations/runbook.md`
- Policy upgrade/migration guide: `meta-agent/docs/operations/POLICY_UPGRADE_GUIDE.md`
- Architecture decision record: `meta-agent/docs/architecture/site/adrs/0001-meta-agent-governance-model.md`
- Phase tracking: `meta-agent/docs/planning/IMPLEMENTATION_ROADMAP.md`
