# Verification Matrix

Purpose: define the required verification scope per command and per execution mode.

## Command Matrix

| Command | Required Checks | Expected Artifacts | Primary Exit Codes |
| --- | --- | --- | --- |
| `init` | policy enforcement, workflow gate, ambiguity gate, mode/autonomy gate, safety gate (when triage present), template scaffold checks | `.meta-agent-decision.json`, `.meta-agent-workflow.json`, `.meta-agent-run-result.json`, `.meta-agent-metrics.json`, scaffolded files under target | `0`, `4`, `5`, `6`, `7`, `9`, `10`, `11` |
| `configure` | policy enforcement, workflow gate, ambiguity gate, mode/autonomy gate, safety gate (when triage present) | `.meta-agent-policy.json` (if missing), `.meta-agent-decision.json`, `.meta-agent-workflow.json`, `.meta-agent-run-result.json`, `.meta-agent-metrics.json` | `0`, `4`, `5`, `6`, `7`, `9`, `10`, `11` |
| `validate` | policy schema validation, policy enforcement, workflow gate, ambiguity gate, mode/autonomy gate, safety gate (when triage present), migration handling for older policy versions | `.meta-agent-decision.json`, `.meta-agent-workflow.json`, `.meta-agent-run-result.json`, `.meta-agent-metrics.json` | `0`, `4`, `5`, `6`, `7`, `9`, `10`, `11` |
| `triage` | ticket eligibility + risk/sizing + validation strategy extraction | triage output JSON, `.meta-agent-decision.json`, `.meta-agent-run-result.json`, `.meta-agent-metrics.json` | `0`, `2`, `4`, `8` |
| `agent` | manifest load and decision-record emission | `.meta-agent-decision.json` | `0`, `2`, `3` |
| `version` | decision-record emission | `.meta-agent-decision.json` | `0` |

## Mode Matrix

| Mode | Required Checks | Notes |
| --- | --- | --- |
| `interactive_ide` | plan approval for non-trivial commands, ambiguity approval when score exceeds threshold, soft token warning profile | high-cost runs require explicit operator approval when policy requires it |
| `autonomous_ticket_runner` | autonomy `A2`/`A3`, required triage input + eligible triage result, strict token hard cap profile | fastest failure path for budget/safety/risk gates |
| `hybrid` | same strict token profile as autonomous, with operator checkpoints for risky work | used when context is unclear or mixed |

## Inter-Phase Stabilization Gate Checklist

Run all items before starting the next roadmap phase:

1. `python3 ./meta-agent/scripts/test-structurizr-site-wrappers.py`
2. `python3 ./meta-agent/scripts/test-compose-templates.py`
3. `python3 ./meta-agent/scripts/compose-templates.py`
4. `python3 ./meta-agent/scripts/compose-templates.py --check`
5. `python3 ./meta-agent/scripts/test-sync-version-markers.py`
6. `python3 ./meta-agent/scripts/sync-version-markers.py --check --tag v1.2.3`
7. `python3 ./meta-agent/scripts/check-version-sync.py --tag v1.2.3`
8. `python3 ./meta-agent/scripts/structurizr-site.py generate --dry-run`
9. `python3 ./meta-agent/scripts/structurizr-site.py serve --port 8080 --dry-run`
10. `dotnet test ./meta-agent/dotnet/MetaAgent.slnx -v minimal`
11. `python3 ./meta-agent/scripts/test-with-coverage.py`
12. `python3 ./meta-agent/scripts/scan-markdown-links.py --fail-on-dead`
13. `python3 ./meta-agent/scripts/check-doc-command-alignment.py`
14. `python3 ./meta-agent/scripts/test-manage-doc-delta.py`
15. `python3 ./meta-agent/scripts/manage-doc-delta.py check`
16. `python3 ./meta-agent/scripts/clean-worktree.py --check-tracked`
17. `python3 ./meta-agent/scripts/clean-worktree.py --apply --include-coverage`
18. `python3 ./meta-agent/scripts/clean-worktree.py --check`
19. `python3 ./meta-agent/scripts/clean-worktree.py --check-tracked`

For one-command execution, use:
- `python3 ./meta-agent/scripts/pre-release-verify.py`
- For tag-gated release readiness, run:
- `python3 ./meta-agent/scripts/pre-release-verify.py --tag v1.2.3`
- For machine-readable audit output, run:
- `python3 ./meta-agent/scripts/pre-release-verify.py --summary-out ./.meta-agent-temp/pre-release-verification/latest-summary.json`
- Release marker target coverage is configured in:
- `meta-agent/config/release-version-markers.json`
- CI tag source fallback (when `--tag` is omitted): `GITHUB_REF` then `CI_COMMIT_TAG`.
- For GitHub-automated release execution after verification, use `meta-agent/docs/operations/RUNBOOK_RELEASE.md`.
