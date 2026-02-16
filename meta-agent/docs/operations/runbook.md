# meta-agent runbook (v1.0.1 baseline)

Quick tasks

- Project navigation map: `meta-agent/PROJECT_MAP.md`
- Verification matrix (commands, modes, gate checklist): `meta-agent/docs/operations/VERIFICATION_MATRIX.md`
- New project onboarding runbook: `meta-agent/docs/operations/RUNBOOK_NEW_PROJECT.md`
- Existing project onboarding runbook: `meta-agent/docs/operations/RUNBOOK_EXISTING_PROJECT.md`
- Interactive developer-assist runbook: `meta-agent/docs/operations/RUNBOOK_INTERACTIVE_IDE.md`
- Autonomous ticket-runner runbook: `meta-agent/docs/operations/RUNBOOK_AUTONOMOUS_RUNNER.md`
- Release runbook (GitHub-automated): `meta-agent/docs/operations/RUNBOOK_RELEASE.md`
- Policy upgrade/migration guide: `meta-agent/docs/operations/POLICY_UPGRADE_GUIDE.md`
- Build solution (recommended for `.slnx`): `dotnet msbuild ./meta-agent/dotnet/MetaAgent.slnx -restore -m:1 -nr:false -v:minimal`
- Template/repo layout config (transitional, defaults still supported): `meta-agent/config/template-layout.json`
- Scaffold a new repo: `dotnet run --project ./meta-agent/dotnet/MetaAgent.Cli -- init --template dotnet --target ../my-service --name my-service`
- Configure an existing repo (no scaffolding): `dotnet run --project ./meta-agent/dotnet/MetaAgent.Cli -- configure --repo ../existing-service --requested-autonomy A1 --tokens-requested 100 --tickets-requested 1 --open-prs 0`
- Validate a policy: `dotnet run --project ./meta-agent/dotnet/MetaAgent.Cli -- validate --policy .meta-agent-policy.json --output ./artifacts`
- Triage a ticket: `dotnet run --project ./meta-agent/dotnet/MetaAgent.Cli -- triage --ticket "Update docs only" --output ./.meta-agent-triage.json`
- Enforced scaffold with explicit gate inputs: `dotnet run --project ./meta-agent/dotnet/MetaAgent.Cli -- init --target ../my-service --requested-autonomy A1 --tokens-requested 200 --tickets-requested 1 --open-prs 0`
- Run canonical regression harness: `python3 ./meta-agent/scripts/run-regression-harness.py`
- Generate Structurizr site (scripted): `python3 ./meta-agent/scripts/structurizr-site.py generate`
- Serve Structurizr site locally (scripted): `python3 ./meta-agent/scripts/structurizr-site.py serve --port 8080`
- Verify Structurizr wrappers (argument handling): `python3 ./meta-agent/scripts/test-structurizr-site-wrappers.py`
- Compose scaffold templates from shared base/overlays: `python3 ./meta-agent/scripts/compose-templates.py`
- Check scaffold template outputs against composition source: `python3 ./meta-agent/scripts/compose-templates.py --check`
- `meta-agent/templates/` is generated output and should not be used as source-of-truth authoring location.
- End-user scaffolding is .NET CLI-only and does not require Python unless the provided helper scripts are used.
- Scan Markdown links/backlinks across the repository: `python3 ./meta-agent/scripts/scan-markdown-links.py`
- Fail-fast on dead local Markdown links: `python3 ./meta-agent/scripts/scan-markdown-links.py --fail-on-dead`
- Check docs against command-capability expectations: `python3 ./meta-agent/scripts/check-doc-command-alignment.py`
- Architecture decisions belong in ADRs: `meta-agent/docs/architecture/site/adrs/`
- Add DOC_DELTA entry only for release-operational notes: `python3 ./meta-agent/scripts/manage-doc-delta.py add --title "..." --change "..." --verification "..."`
- Check DOC_DELTA ordering/format when DOC_DELTA is changed: `python3 ./meta-agent/scripts/manage-doc-delta.py check`
- Normalize DOC_DELTA ordering when needed: `python3 ./meta-agent/scripts/manage-doc-delta.py fix`
- DOC_DELTA lock file path: `meta-agent/DOC_DELTA.md.lock` (used automatically to serialize concurrent agent/writer access)
- Tune lock wait behavior when needed: `--lock-timeout-seconds <seconds>` and `--lock-poll-interval-ms <ms>`
- Scanner outputs:
  - JSON: `.meta-agent-temp/markdown-link-report.json`
  - Markdown summary: `.meta-agent-temp/markdown-link-report.md`
- Run full pre-release verification checklist (single command): `python3 ./meta-agent/scripts/pre-release-verify.py`
- Validate release-facing version sync (csproj + key docs): `python3 ./meta-agent/scripts/check-version-sync.py --tag v1.2.3`
- Validate release tag SemVer gate locally: `python3 ./meta-agent/scripts/pre-release-verify.py --tag v1.2.3`
- Emit machine-readable verification summary JSON: `python3 ./meta-agent/scripts/pre-release-verify.py --summary-out ./.meta-agent-temp/pre-release-verification/latest-summary.json`
- Trigger GitHub-automated release flow by pushing a SemVer tag:
  - `git tag -a v1.2.3 -m "v1.2.3" && git push origin refs/tags/v1.2.3`
  - release flow details: `meta-agent/docs/operations/RUNBOOK_RELEASE.md`
- Manual fallback packaging (only when CI release automation is unavailable): `python3 ./meta-agent/scripts/package-release.py`
- Manual fallback checksums path: `.meta-agent-temp/release-packages/SHA256SUMS.txt`
- If `--tag` is omitted, the script resolves tag context from CI vars in this order: `GITHUB_REF`, then `CI_COMMIT_TAG`.
- Clean generated artifacts: `python3 ./meta-agent/scripts/clean-worktree.py --apply --include-coverage`
- Check for generated-artifact drift in worktree: `python3 ./meta-agent/scripts/clean-worktree.py --check`
- Check repository does not track generated-artifact paths: `python3 ./meta-agent/scripts/clean-worktree.py --check-tracked`

Verification protocol (required for gate changes)

- For changes that affect policy enforcement, triage, mode/autonomy rules, or safety-level gates:
- Run targeted tests for the modified behavior first.
- Run full suite second: `dotnet test ./meta-agent/dotnet/MetaAgent.slnx -v minimal`
- Hard rule: never execute parallel runs that write to shared output paths (`bin/`, `obj/`, `coverage/`, shared artifact files).
- If parallel execution is needed, run each invocation against a separate temporary copy/worktree and separate output locations.
- Treat the change as incomplete unless both pass.

Allowed runtimes: C#/.NET 10, Node/TypeScript, and PowerShell for product-code templates; Python 3 for the provided optional helper scripts across Windows, Linux, and macOS.
The scaffold runtime path is C#/.NET; Python is only needed if you keep using the bundled helper scripts.

Agent manifests

- List available pre-defined agents: `dotnet run --project ./meta-agent/dotnet/MetaAgent.Cli -- agent list`
- Describe the pre-defined .NET agent: `dotnet run --project ./meta-agent/dotnet/MetaAgent.Cli -- agent describe dotnet-agent`

Reviewer notes: The template renderer uses `Fluid.Core` (Liquid-compatible) and preserves compatibility for legacy `{{ project_name or "Default" }}` fallback forms. Prefer explicit fallback intent in templates and use plain `{{ project_name }}` when directory-name fallback behavior is desired.

First-run behavior

- On first `init`, the CLI creates a default policy file if missing and asks the operator to choose:
- `defaultMode`: `interactive_ide`, `autonomous_ticket_runner`, or `hybrid`
- `commandGating`: `mutating_only` or `all_commands`
- `budgetAccounting.mode`: `per_invocation` or `persistent_daily`
- In non-interactive runs, the defaults are used (`mutating_only`, `per_invocation`).
- Default autonomy in the generated policy is A1.
- Policy files are versioned with `policyVersion` (current: `1`).
- `init`, `configure`, and `validate` auto-migrate legacy policy files that do not have `policyVersion`, then persist the migrated file.
- The v1.0.1 baseline defaults to `dotnet` template scaffolding for out-of-box usage.
- Scaffolded templates (generated projects) include starter architecture-doc tooling for Structurizr:
- Structurizr workspace/model root: `docs/architecture/site/`
- published workspace docs via `!docs _docs`: `docs/architecture/site/_docs/`
- `AGENT_WRITING_GUIDELINES.md` adjacent to each template's Structurizr root for deterministic agent placement/rules
- deployment environments/views (local development + production baseline)
- recommendation to keep sensitive deployment overlays in a separate private repository when needed
- `.github/workflows/architecture-docs.yml` (GitHub example, GitHub Pages deploy on SemVer tags)
- `.gitlab-ci.yml` (GitLab example, GitLab Pages deploy on `main`)
- [`docs/architecture/internal/4x/41/41.02/index.md`](../architecture/internal/4x/41/41.02/index.md)
- Repository model location for this project: `docs/architecture/site/workspace.dsl`
- Repository published-docs scopes for this project:
- workspace-level docs: `docs/architecture/site/_docs/` (wired via `!docs _docs`)
- element-level docs: `docs/architecture/site/model/**/_docs/` (wired via element-level `!docs`)

Enabling higher autonomy

- Edit `.meta-agent-policy.json` and change `autonomyDefault` (allowed: A0..A3) and document approval gates.
- Add feature flags or CI gating before allowing auto‑merge.

Policy enforcement and decision records

- `init`, `configure`, and `validate` evaluate autonomy, budgets, change boundaries, and abort conditions before execution.
- Artifact directory defaults:
- `init`: `--target`
- `configure`: `--repo`
- `validate`: directory containing `--policy`
- `triage`: directory of `--output <path>` (or current directory when omitted)
- `version` and `agent`: current directory
- `--output <dir>` overrides artifact directory defaults for `init`, `configure`, `validate`, `version`, and `agent`.
- `configure` is intended for pre-existing repositories and does not render template scaffold files.
- `.NET Framework` codebases are legacy-maintenance targets; onboard them with `configure` and `validate` (not `init` scaffolding).
- Each run writes a machine-readable decision record to `.meta-agent-decision.json` unless overridden by `--decision-record <path>`.
- Gate inputs (command-dependent):
- `--policy <path>`
- `--requested-autonomy <A0..A3>`
- `--adr-id-prefix <id>` (`init` only; sets ADR filename prefix in scaffolded Structurizr ADRs, e.g. `PLATFORM-1234`)
- If `--adr-id-prefix` is omitted, `init` attempts to derive a Jira-style key from `--ticket`/`--ticket-file` and uses it as ADR prefix.
- `--run-result <path>`
- `--metrics-scoreboard <path>`
- `--ticket <text>` or `--ticket-file <path>`
- `--triage-output <path>`
- `--tokens-requested <int>`
- `--tickets-requested <int>`
- `--open-prs <int>`
- `--operator-approved-high-cost` (interactive-mode high token-cost approval for non-interactive execution)
- `--abort-signal <id>` (repeatable)

Workflow and ambiguity controls

- `init`, `configure`, and `validate` write a workflow record to `.meta-agent-workflow.json` (override via `--workflow-record <path>`).
- `init`, `configure`, and `validate` support ambiguity scoring with `--ambiguity-score <0..1>`.
- In interactive developer sessions (`interactive_ide` with operator present), non-trivial commands (`init`, `configure`, `validate`) require operator plan approval before execution.
- `--operator-approved-plan` can be supplied to pre-approve the plan.
- If ambiguity exceeds policy `ambiguityThreshold`, execution requires operator approval:
- interactive mode: prompt confirmation at runtime
- non-interactive mode: require `--operator-approved-ambiguity` or command exits with code `6`

Mode and autonomy controls

- `init`, `configure`, and `validate` accept `--mode <interactive_ide|autonomous_ticket_runner|hybrid>`.
- If `--mode` is omitted, mode is classified from runtime context:
- configured ticket context env vars (`integrations.ticketContextEnvVars`) => `autonomous_ticket_runner`
- interactive shell => `interactive_ide`
- otherwise => policy `defaultMode`
- `--requested-autonomy <A0..A3>` is supported on `init`, `configure`, and `validate`.
- Enforcement rules:
- `A0` cannot execute mutating commands (`init` exits with code `7`).
- `autonomous_ticket_runner` mode requires autonomy `A2` or `A3`.
- `autonomous_ticket_runner` mode requires triage input (`--ticket` or `--ticket-file`) and eligible triage outcome.
- mode-specific token governance:
- `autonomous_ticket_runner` and `hybrid` enforce strict per-run token hard caps (`tokenGovernance.autonomousTicketRunner.hardCapTokensPerRun`)
- `interactive_ide` uses soft warning threshold (`tokenGovernance.interactiveIde.warningTokensPerRun`) and requires explicit approval for high-cost runs (`--operator-approved-high-cost` in non-interactive sessions)
- safety level gate enforcement (when triage metadata is present):
- level 2: requires `--operator-approved-safety` and `--validated-method integration_tests`
- level 3: requires `--operator-approved-safety` and validated methods:
- `integration_tests`
- `manual_validation_steps`
- `runtime_assertions`

Rollback

- All automated changes must include rollback notes in the generated PR description. If a PR is merged that causes issues, revert using a standard `git revert` and create a follow-up ticket.

Triage output

- `triage` emits `.meta-agent-triage.json` (override via `--output <path>`).
- Output includes:
- eligibility + reason
- risk level + score
- size estimate
- change safety level (0-3)
- strategy tier (1-3)
- definition of done extraction
- validation plan with per-method `chosen|skipped` rationale

Run result output

- `init`, `configure`, `validate`, and `triage` emit `.meta-agent-run-result.json` by default (override via `--run-result <path>`).
- Structured sections include:
- summary
- assumptions
- extracted requirements
- risk level
- plan
- implementation
- validation evidence
- documentation updates
- metrics impact
- next actions
- Autonomous mode includes additional:
- decision log
- risk log
- rollback notes

Metrics scoreboard

- `init`, `configure`, `validate`, and `triage` update `.meta-agent-metrics.json` by default (override via `--metrics-scoreboard <path>`).
- Tracked metrics include:
- success rate
- rework rate
- clarification rate
- token cost per success
- defect leakage incidents (proxy)
- time to accepted solution (run-count proxy)

Command capability matrix

| Command | Decision record | Workflow record | Run result | Metrics | Triage inputs | Exit codes (primary) |
| --- | --- | --- | --- | --- | --- | --- |
| `init` | yes | yes | yes | yes | `--ticket`, `--ticket-file` | `0`, `2`, `4`, `5`, `6`, `7`, `9`, `10`, `11` |
| `configure` | yes | yes | yes | yes | `--ticket`, `--ticket-file` | `0`, `2`, `4`, `5`, `6`, `7`, `9`, `10`, `11` |
| `validate` | yes | yes | yes | yes | `--ticket`, `--ticket-file` | `0`, `2`, `4`, `5`, `6`, `7`, `9`, `10`, `11` |
| `triage` | yes | no | yes | yes | required (`--ticket` or `--ticket-file`) | `0`, `2`, `4`, `8` |
| `agent` | yes | no | no | no | no | `0`, `2` |
| `version` | yes | no | no | no | no | `0` |

Regression harness

- Canonical task manifest: `meta-agent/scripts/canonical-regression-tasks.json`
- Runner: `meta-agent/scripts/run-regression-harness.py`
- Default output root: `.meta-agent-temp/regression-harness/`
- Queryable/comparable outputs:
- per-run structured report: `runs/<run-id>/harness-summary.json`
- latest snapshot: `latest-summary.json`
- historical comparison table: `history/harness-history.csv`

CLI exit codes

- `0`: success
- `1`: missing top-level command
- `2`: invalid CLI usage/arguments (including unknown command)
- `3`: unhandled runtime error
- `4`: missing required file or schema validation failure
- `5`: policy enforcement blocked execution
- `6`: workflow blocked due to unresolved ambiguity
- `7`: mode/autonomy gate enforcement failed
- `8`: triage command completed with ineligible ticket
- `9`: autonomous mode triage requirement not met (missing or ineligible triage)
- `10`: safety-level gate enforcement failed
- `11`: workflow blocked due to missing operator plan approval
