# Meta-Agent Implementation Roadmap

Purpose: capture the post-v1.0.2 upgrade plan from the released scaffolder/validator baseline to a full meta-agent system aligned to the original specification.

## Inter-Phase Stabilization Gate (Mandatory)

Between any completed phase and the start of the next phase, run a stabilization gate to protect quality and avoid regressions.

Required gate checks:
- full automated test suite passes
- smoke tests for critical scripts/commands pass
- coverage threshold remains satisfied
- generated-artifact hygiene checks pass (`clean-worktree` check + check-tracked)
- collision-safe execution is verified (no parallel runs on shared artifact/output paths; parallel runs must use isolated temporary copies/worktrees and distinct output paths)
- docs/roadmap/DOC_DELTA are updated to match implemented behavior
- no unresolved known regressions in current phase scope

Gate rule:
- next phase does not start until this stabilization gate is explicitly marked passed for the current phase.

## Phase 0: Baseline Integrity

- Fix malformed manifest JSON in `agents/dotnet-agent.json`.
- Align policy/docs naming and autonomy ladder semantics.
- Ensure docs reflect actual CLI behavior (no drift).
- Strengthen tests so malformed config/manifest files fail fast.

Exit criteria:
- `dotnet test` passes.
- Manifest and policy are validated structurally, not string-matched.

## Phase 1: Policy Enforcement Core

- Add runtime policy enforcement for:
- autonomy gates
- budget limits (tokens/day, tickets/day, max concurrent PRs)
- change boundaries (allowed/disallowed paths)
- abort conditions
- Implement deterministic decision records for each execution.

Exit criteria:
- Execution is blocked when policy constraints are violated.
- Each run emits machine-readable policy enforcement outcomes.

Status: completed (2026-02-14 22:01:38Z UTC).

## Phase 2: Execution Workflow Engine

- Implement mandatory workflow:
- Understand & Clarify
- Plan
- Execute
- Validate
- Document (Doc Delta)
- Evaluate metrics
- Refine strategy (bounded loops)
- Add ambiguity scoring and operator escalation threshold.

Exit criteria:
- Non-trivial tasks must pass through all workflow stages.
- High ambiguity paths require explicit operator input.

Status: completed (2026-02-14 22:12:33Z UTC).

## Phase 3: Modes + Autonomy Ladder

- Implement deployment mode classifier:
- Interactive IDE
- Autonomous Ticket Runner
- Hybrid
- Implement autonomy ladder behavior:
- A0 suggest only
- A1 draft artifacts
- A2 execute with gates
- A3 autonomous merge/deploy (policy-gated)

Exit criteria:
- Mode and autonomy are explicit, logged, and enforced at runtime.

Status: completed (2026-02-14 22:27:23Z UTC).

## Phase 4: Triage + Risk + Validation Strategy

- Add ticket triage pipeline:
- eligibility
- risk scoring
- sizing
- Definition of Done extraction
- strategy tier selection
- Add change safety levels (0-3) with required validation gates.
- Validation planner must explain:
- why methods were chosen
- why others were skipped

Exit criteria:
- Every autonomous task has triage metadata and explicit validation strategy.

Status: completed (2026-02-14 22:56:54Z UTC).

## Phase 5: Observability + Metrics + Regression Harness

- Emit structured outputs:
- Summary
- Assumptions
- Extracted Requirements
- Risk Level
- Plan
- Implementation
- Validation Evidence
- Documentation Updates
- Metrics Impact
- Next Actions
- Add autonomous-only logs:
- decision log
- risk log
- rollback notes
- Track performance scoreboard:
- success/rework/clarification rates
- token cost per success
- defect leakage
- time to accepted solution
- Build canonical regression harness tasks and drift comparisons.

Exit criteria:
- Historical metrics and harness runs are queryable and comparable.

Status: completed (2026-02-15 00:34:33Z UTC).

## Phase 6: Tooling and Platform Alignment

- Keep .NET 10/C# as orchestration core.
- Require Python as the cross-platform scripting runtime.
- Replace shell-specific scripting logic with Python for consistency.
- Keep CI provider adapters as optional integrations, not core coupling.
- Add explicit onboarding for pre-existing repositories without scaffolding side-effects.
- Separate token-governance profiles by operating mode:
- `autonomous_ticket_runner`: strict hard caps and fast-stop enforcement to prevent runaway spend.
- `interactive_ide` / developer-assist: softer token policy with operator warning + explicit confirmation for high-cost tasks.
- Keep policy/decision records explicit about which budget profile was applied and why.
- Enforce plan-first execution in `interactive_ide` for non-trivial commands:
- create plan
- require developer approval
- document plan
- then execute

Exit criteria:
- Core behavior is platform-neutral with optional GitHub/GitLab/Jira adapters.

Status: completed (2026-02-15 14:10:56Z UTC, platform alignment goals implemented: Python-first script strategy, pre-existing repository onboarding path, mode-specific token governance, explicit budget-profile decision logging, and provider-agnostic ticket-context integrations).

## Phase 7: Release Readiness and Operational Hardening

- Add scaffold output end-to-end verification for template CI/doc workflows:
- generated `dotnet`, `node`, `generic`, and `powershell` projects must keep Structurizr output scoped to `docs/architecture/site/build/`
- generated projects must not leak root-level `build/` output
- Introduce policy versioning and migration path:
- add explicit `policyVersion` field
- add deterministic migration behavior for older policy files
- add migration validation tests and operator-facing upgrade notes
- Add a verification matrix for commands and modes:
- required checks per command (`init`, `configure`, `validate`, `triage`, `agent`, `version`)
- required checks per mode (`interactive_ide`, `autonomous_ticket_runner`, `hybrid`)
- expected artifacts and exit codes
- Add release checklist automation:
- pre-release verification script that runs smoke + full test + coverage + clean-worktree checks
- semVer-tag readiness gate for repository release flow
- Tighten documentation for day-2 usage:
- explicit “new project” and “pre-existing project” runbooks
- explicit “interactive developer assist” vs “autonomous runner” operating guidance

Exit criteria:
- Release gate is reproducible by script and CI.
- Scaffolded output behavior is validated end-to-end for all templates.
- Policy upgrades are explicit, tested, and documented.
- Operator docs are task-oriented and unambiguous for daily use.

Status: completed (2026-02-15 15:25:17Z UTC, exit criteria validated: release gate reproducible in script/CI with SemVer and summary artifacts, scaffold path safety covered for all templates, policy migration/versioning implemented and documented, and day-2 operator runbooks finalized).

Phase 7 kickoff checklist:

1. Confirm stabilization gate is still green (`pre-release-verify.py` passes).
2. Lock first Phase 7 PR scope to one deliverable (avoid mixed concerns).
3. Add/adjust tests before implementation where behavior is not already covered.
4. Update runbook/README/verification matrix in the same PR when behavior changes.
5. Record each Phase 7 increment in `DOC_DELTA.md` with UTC timestamp.

## Phase 8: Pilot Validation and Battle-Test

- Run controlled pilots across representative repositories:
- at least 3 repositories, including legacy-heavy and greenfield cases
- include both `interactive_ide` and `autonomous_ticket_runner` operating modes
- Capture and classify real-world friction:
- setup time and onboarding friction
- false positives/false blocks from governance gates
- required policy exceptions and recurring operator overrides
- regression and stability behavior under repeated usage
- Build a pilot evidence pack:
- pilot scorecard per repository
- top issues by severity/frequency
- remediation backlog with clear ownership/priorities
- Apply stabilization pass based on pilot findings before enterprise expansion.

Exit criteria:
- Pilot evidence demonstrates stable, repeatable onboarding and day-2 operation across representative repos.
- Top critical/high-severity pilot issues are resolved or explicitly accepted with mitigation.
- Updated defaults/runbooks reflect pilot-proven workflows and edge cases.

Status: planned (2026-02-15 15:45:34Z UTC).

## Phase X: Post-Pilot Strategy

Purpose: choose the next strategic track based on measured Phase 8 pilot outcomes.

Decision inputs:
- pilot scorecards and remediation closure status
- adoption metrics (activation, retention, override rate)
- safety/reliability metrics (block accuracy, defect leakage, rollback incidents)
- integration demand signals (Jira/Azure DevOps/GitHub/GitLab priorities)

Possible tracks (choose based on evidence):
- Track A: enterprise integration scale-up (identity, approvals, ticket/PR platform depth)
- Track B: autonomous-mode hardening (governance strictness, audit guarantees, cost controls)
- Track C: legacy-first acceleration (deep onboarding for complex pre-existing repositories)
- Track D: platform/productization (distribution, supportability, release operations)

Exit criteria:
- One strategic track is selected with explicit rationale from Phase 8 data.
- 2-3 phased deliverables are defined with measurable success metrics.
- Non-selected tracks are captured as deferred backlog with trigger conditions.

Status: planned (2026-02-15 15:46:37Z UTC; decision gate after Phase 8 completion, with optional additional hardening phases before strategy execution).

## Immediate Backlog (Next PR Candidates)

1. Completed: template scaffold e2e checks for Structurizr path safety (`dotnet`, `node`, `generic`, `powershell`).
2. Completed: `policyVersion` + migration support in core/CLI + tests.
3. Completed: verification matrix doc wired into runbook/README.
4. Completed: pre-release verification script + CI gate wrapper.
