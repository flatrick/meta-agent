# meta-agent — v1.0.0

Meta‑agent proof‑of‑concept implementing governance, scaffolding and validation harness based on the provided META‑AGENT specification.

Usage docs

- Fast repository navigation map: `meta-agent/PROJECT_MAP.md`.
- For full day-to-day operating guidance (developer-assist and autonomous flows), see `meta-agent/docs/operations/USAGE_GUIDE.md`.
- For explicit operator runbooks by scenario, see:
  - `meta-agent/docs/operations/RUNBOOK_NEW_PROJECT.md`
  - `meta-agent/docs/operations/RUNBOOK_EXISTING_PROJECT.md`
  - `meta-agent/docs/operations/RUNBOOK_INTERACTIVE_IDE.md`
  - `meta-agent/docs/operations/RUNBOOK_AUTONOMOUS_RUNNER.md`
  - `meta-agent/docs/operations/POLICY_UPGRADE_GUIDE.md`
- For command/mode verification requirements and phase-gate checklist, see `meta-agent/docs/operations/VERIFICATION_MATRIX.md`.
- For architecture modeling/site generation with Structurizr, see `meta-agent/docs/architecture/internal/4x/41/41.01/index.md`.
- Repository Structurizr model location: `meta-agent/docs/architecture/site/workspace.dsl`.
- Repository published-docs scopes:
  - workspace-level docs: `meta-agent/docs/architecture/site/_docs/`
  - element-level docs: `meta-agent/docs/architecture/site/model/**/_docs/`

Quickstart

- Build (reliable for `.slnx` in this environment): `dotnet msbuild ./meta-agent/dotnet/MetaAgent.slnx -restore -m:1 -nr:false -v:minimal`
- Test: `dotnet test ./meta-agent/dotnet/MetaAgent.slnx`
- Run CLI (example):
  - `dotnet run --project ./meta-agent/dotnet/MetaAgent.Cli -- init --template dotnet --target ../my-service --name my-service`
  - `dotnet run --project ./meta-agent/dotnet/MetaAgent.Cli -- configure --repo ../existing-service --requested-autonomy A1 --tokens-requested 100 --tickets-requested 1 --open-prs 0`
  - `dotnet run --project ./meta-agent/dotnet/MetaAgent.Cli -- validate --policy .meta-agent-policy.json --output ./artifacts`
  - `dotnet run --project ./meta-agent/dotnet/MetaAgent.Cli -- triage --ticket "Update docs only" --output ./.meta-agent-triage.json`
  - `dotnet run --project ./meta-agent/dotnet/MetaAgent.Cli -- init --target ../my-service --requested-autonomy A1 --tokens-requested 200 --tickets-requested 1 --open-prs 0`
  - `dotnet run --project ./meta-agent/dotnet/MetaAgent.Cli -- init --template dotnet --target ../my-service --name my-service --adr-id-prefix PLATFORM-1234`
  - `dotnet run --project ./meta-agent/dotnet/MetaAgent.Cli -- init --target ../my-service --ambiguity-score 0.75 --operator-approved-ambiguity`
  - `dotnet run --project ./meta-agent/dotnet/MetaAgent.Cli -- validate --policy .meta-agent-policy.json --ambiguity-score 0.75 --operator-approved-ambiguity`
  - `dotnet run --project ./meta-agent/dotnet/MetaAgent.Cli -- init --target ../my-service --mode autonomous_ticket_runner --requested-autonomy A2`

Verification protocol for gate/behavior changes

- For changes affecting policy gates, mode/autonomy enforcement, triage, or safety-level validation:
  - run targeted tests first (for the changed behavior), then
  - run the full suite: `dotnet test ./meta-agent/dotnet/MetaAgent.slnx -v minimal`
- Execution-collision safety rule:
  - do not run commands in parallel when they share project output paths (`bin/`, `obj/`, `coverage/`, shared artifact files)
  - if parallel runs are required, execute each run in its own isolated temporary copy/worktree and unique output paths
- Record architecture decisions in ADRs: `meta-agent/docs/architecture/site/adrs/`.
- Manage `DOC_DELTA` only for release-operational notes that are not architecture decisions:
  - add entry: `python3 ./meta-agent/scripts/manage-doc-delta.py add --title "..." --change "..." --verification "..."`
  - verify ordering/format: `python3 ./meta-agent/scripts/manage-doc-delta.py check`
  - auto-normalize ordering: `python3 ./meta-agent/scripts/manage-doc-delta.py fix`
  - lock behavior: script uses `meta-agent/DOC_DELTA.md.lock` and waits up to `30s` by default when another writer/agent holds the lock
  - lock tuning: `--lock-timeout-seconds <seconds>` and `--lock-poll-interval-ms <ms>`

Testing with coverage

Run tests with enforced code-coverage locally (all platforms with Python 3):

```text
# from the repository root
python3 ./meta-agent/scripts/test-with-coverage.py
```

After running the script, a human-readable HTML coverage report will be available at `meta-agent/dotnet/coverage/report/index.htm` and a historical CSV at `meta-agent/dotnet/coverage/history/coverage_history.csv`.

Canonical regression harness (Phase 5)

Run canonical behavior checks and drift comparison (all platforms with Python 3):

```text
# from the repository root
python3 ./meta-agent/scripts/run-regression-harness.py
```

Outputs are written to `.meta-agent-temp/regression-harness/`:
- per-run summary: `runs/<run-id>/harness-summary.json`
- rolling summary: `latest-summary.json`
- comparable history: `history/harness-history.csv`

Worktree cleanup and generated-artifact guard

- Remove generated artifacts from worktree (recommended before/after larger runs):
  - `python3 ./meta-agent/scripts/clean-worktree.py --apply --include-coverage`
- Verify worktree has no generated artifacts:
  - `python3 ./meta-agent/scripts/clean-worktree.py --check`
- Verify repository does not track generated artifacts:
  - `python3 ./meta-agent/scripts/clean-worktree.py --check-tracked`

Structurizr generation and serving (scripted)

- Generate site:
  - `python3 ./meta-agent/scripts/structurizr-site.py generate`
- Serve locally:
  - `python3 ./meta-agent/scripts/structurizr-site.py serve --port 8080`
- Validate wrapper behavior:
  - `python3 ./meta-agent/scripts/test-structurizr-site-wrappers.py`
- Compose scaffold templates from shared base/overlays:
  - `python3 ./meta-agent/scripts/compose-templates.py`
- Check scaffold template outputs against composed source (CI gate):
  - `python3 ./meta-agent/scripts/compose-templates.py --check`
- Template composition source and manifest:
  - `meta-agent/template-src/`
  - `meta-agent/template-src/manifest.json`
- `meta-agent/templates/` is a generated folder and is intentionally ignored in git.
- End-user scaffold runtime contract:
  - `init` scaffolding requires only the .NET CLI executable and runtime assets (`templates/`, `agents/`, `schema/`).
  - Runtime template composition fallback is implemented in C# from `template-src/manifest.json`; CLI scaffold flow does not invoke Python.
  - Python scripts are optional helper tooling for maintenance/verification and can be replaced or removed by downstream teams.
- Scan Markdown links/backlinks (repo-wide):
  - `python3 ./meta-agent/scripts/scan-markdown-links.py`
  - `python3 ./meta-agent/scripts/scan-markdown-links.py --fail-on-dead`
- Check docs against command-capability expectations:
  - `python3 ./meta-agent/scripts/check-doc-command-alignment.py`

Pre-release verification checklist

- Run full release-readiness checks in sequence:
  - `python3 ./meta-agent/scripts/pre-release-verify.py`
- Validate release-facing version sync (csproj + key docs):
  - `python3 ./meta-agent/scripts/check-version-sync.py --tag v1.2.3`
- Validate a release tag against the same SemVer gate used by CI:
  - `python3 ./meta-agent/scripts/pre-release-verify.py --tag v1.2.3`
- Write machine-readable verification summary JSON (for CI/audit):
  - `python3 ./meta-agent/scripts/pre-release-verify.py --summary-out ./.meta-agent-temp/pre-release-verification/latest-summary.json`
- Tag source resolution order when `--tag` is omitted:
  - `GITHUB_REF` tag value
  - `CI_COMMIT_TAG`

Release packaging (downloadable zip)

- Build release packages with executable + editable runtime assets:
  - `python3 ./meta-agent/scripts/package-release.py`
- Default runtimes:
  - `win-x64`
  - `linux-x64`
  - `osx-arm64`
  - `osx-x64`
- Output zip locations (default):
  - `.meta-agent-temp/release-packages/meta-agent-<version>-win-x64.zip`
  - `.meta-agent-temp/release-packages/meta-agent-<version>-linux-x64.zip`
  - `.meta-agent-temp/release-packages/meta-agent-<version>-osx-arm64.zip`
  - `.meta-agent-temp/release-packages/meta-agent-<version>-osx-x64.zip`
- SHA256 checksums file (default):
  - `.meta-agent-temp/release-packages/SHA256SUMS.txt`
- Build a subset of runtimes:
  - `python3 ./meta-agent/scripts/package-release.py --runtime linux-x64 --runtime osx-arm64`
- Contents include:
  - runtime-specific executable (`meta-agent.exe` on Windows, `meta-agent` on Linux/macOS)
  - `templates/`
  - `agents/`
  - `schema/`
  - `examples/`

Defaults

- First run `init` creates `.meta-agent-policy.json` when missing and prompts the operator for:
  - default execution mode (`interactive_ide`, `autonomous_ticket_runner`, or `hybrid`)
  - command gating scope (`mutating_only` or `all_commands`)
  - budget accounting mode (`per_invocation` or `persistent_daily`)
  (non-interactive runs use defaults)
- Default autonomy is `A1` (human review).
- Policy files are versioned (`policyVersion`), current version is `1`.
- `init`, `configure`, and `validate` automatically migrate legacy policy files missing `policyVersion` and persist the upgraded file.
- Out-of-box scaffold baseline: `dotnet`
- Includes templates: `node`, `generic`, `dotnet`, `powershell`
- Template source of truth for shared scaffold content: `meta-agent/template-src/` (`base/` + `overlays/`), composed into generated output `meta-agent/templates/` by `python3 ./meta-agent/scripts/compose-templates.py` (development/release-time helper).
- Repository docs publication scopes (this repo):
  - workspace-level docs: `meta-agent/docs/architecture/site/_docs/`
  - element-level docs: `meta-agent/docs/architecture/site/model/**/_docs/`
- Template/repo layout config (transitional): `meta-agent/config/template-layout.json` is now used in tests and migration wiring; current runtime behavior still keeps built-in defaults for compatibility.
- Scaffold templates (generated projects) include starter Structurizr docs assets:
  - Structurizr workspace/model root: `docs/architecture/site/`
  - published workspace docs (`!docs _docs`) at `docs/architecture/site/_docs/`
  - `docs/architecture/internal/4x/41/41.02/index.md` for shared human + agent Structurizr workflow
  - `docs/architecture/internal/4x/41/41.03/index.md` for tracked Structurizr DSL compatibility with `structurizr-site-generatr`
  - `AGENT_WRITING_GUIDELINES.md` adjacent to each template's Structurizr root (kept outside published site docs)
  - `adrs/` adjacent to each template's Structurizr root (including starter ADRs rendered in the site)
  - ADR filename prefix is configurable during `init` with `--adr-id-prefix` (e.g. Jira/Epic IDs)
  - If `--adr-id-prefix` is omitted and a Jira-style key appears in `--ticket`/`--ticket-file`, that key is used automatically (for example `PLATFORM-1234`)
  - deployment environments/views in the workspace (local development + production baseline) with recommendation to keep sensitive deployment overlays in a separate private repository
  - `.github/workflows/architecture-docs.yml` (GitHub example, SemVer-tag gated GitHub Pages publish)
  - `.gitlab-ci.yml` (GitLab example, GitLab Pages publish on `main`)
  - `docs/architecture/internal/4x/41/41.02/index.md`
- CI examples for GitHub Actions & GitLab CI (updated to use `dotnet test`)
- Artifact output defaults are command-scoped and consistent:
  - `init`: defaults to `--target`
  - `configure`: defaults to `--repo`
  - `validate`: defaults to the directory containing `--policy`
  - `triage`: defaults to the directory of triage output (`--output <path>`) or current directory if omitted
  - `version`/`agent`: default to current directory
- `--output <dir>` overrides the default artifact directory for `init`, `configure`, `validate`, `version`, and `agent`.
- `configure` is the safe onboarding path for pre-existing repositories: it writes governance/config artifacts without rendering scaffold template files.
- `.NET Framework` repositories are treated as legacy maintenance targets: use `configure` + `validate` for agent onboarding/governance, not `init` scaffolding.
- `init`, `configure`, and `validate` emit a deterministic machine-readable policy decision record (`.meta-agent-decision.json` by default).
- `init`, `configure`, and `validate` emit workflow records (`.meta-agent-workflow.json` by default) with mandatory non-trivial stages.
- In interactive developer sessions (`interactive_ide` with operator present), non-trivial commands (`init`, `configure`, `validate`) require explicit plan approval before execution (`--operator-approved-plan` can pre-approve).
- Mode-specific token governance profiles are enforced and logged in decision records:
  - `autonomous_ticket_runner` and `hybrid`: strict per-run hard cap (`tokenGovernance.autonomousTicketRunner.hardCapTokensPerRun`)
  - `interactive_ide`: soft warning threshold with explicit approval for high-cost runs (`tokenGovernance.interactiveIde.warningTokensPerRun`)
  - use `--operator-approved-high-cost` to pre-approve interactive high-cost execution in non-interactive runs
- `init`, `configure`, `validate`, and `triage` emit a structured run result (`.meta-agent-run-result.json` by default, override via `--run-result <path>`).
- `init`, `configure`, `validate`, and `triage` update a metrics scoreboard (`.meta-agent-metrics.json` by default, override via `--metrics-scoreboard <path>`).
- `init`, `configure`, and `validate` can attach triage metadata with `--ticket <text>` or `--ticket-file <path>` (writes `.meta-agent-triage.json` by default or `--triage-output <path>`).
- In `autonomous_ticket_runner` mode, ticket triage input is required and must be eligible.
- Safety-level gate enforcement:
  - level 2 requires `--operator-approved-safety` and `--validated-method integration_tests`
  - level 3 requires `--operator-approved-safety` and validated methods: `integration_tests`, `manual_validation_steps`, `runtime_assertions`
- `init`, `configure`, and `validate` support explicit mode selection (`--mode interactive_ide|autonomous_ticket_runner|hybrid`), otherwise mode is classified from context.
- ticket context env vars used for classification are configurable via `integrations.ticketContextEnvVars` (defaults: `WORK_ITEM_ID`, `META_AGENT_TICKET_ID`).
- Autonomy ladder enforcement:
  - `A0` is suggest-only and cannot run mutating commands.
  - `autonomous_ticket_runner` mode requires `A2` or `A3`.

Schema validation: a JSON Schema is provided at `schema/meta-agent-policy.schema.json` and CI enforces policy conformance via unit tests. The CLI `validate` command also validates policies and reports schema errors.

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
- `11`: workflow blocked due to missing operator plan approval (interactive mode requirement)

Allowed runtimes: C#/.NET 10, Node/TypeScript, and PowerShell for product code templates; Python 3 for the provided optional helper scripts on Windows, Linux, and macOS.
Users can replace/remove the provided Python helpers if they maintain equivalent project checks and workflows.

See `docs/` for ADRs and runbooks.
