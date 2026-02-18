# Task: docs baseline clarity + deletion guardrails for scaffolded `docs/`

## Why
Scaffolded repositories currently include a `docs/` folder that can be misread as static/final. We want it to be explicitly presented as a **starting baseline** that teams and AI agents evolve over time.

We also want a clear operational rule: AI agents must **not remove existing content under `docs/`** unless a human operator explicitly authorizes it.

## Proposed implementation (for approved follow-up)

### 1) Add an explicit baseline contract in scaffolded docs landing page
- Edit template source docs entrypoint(s) under `meta-agent/template-src/` so generated repos include a short “Docs Baseline Contract” section:
  - `docs/` is intentionally mutable.
  - teams may add/replace/restructure content.
  - generated content is a reference starting point, not a frozen standard.

### 2) Add an AI-agent safety policy file in scaffolded repos
- Add a policy file in template source (for example `docs/AGENT_DOCS_POLICY.md` or root `AGENTS.md` section in scaffold output scope) that states:
  - default behavior in `docs/`: additive/modify allowed.
  - destructive behavior (delete files/sections) requires explicit operator instruction in the active task/request.
  - when deletion is requested, agents should document rationale and impacted paths in commit message/PR body.

### 3) Add a lightweight CI guardrail for unintended docs deletions
- Add a script (template source) that fails if tracked files under `docs/` are deleted without an override flag/marker.
- Example control plane:
  - default CI mode: block deletions under `docs/`.
  - explicit override: environment variable (e.g., `ALLOW_DOCS_DELETE=1`) plus required annotation in PR/body.
- Wire this into generated CI workflow templates for early signal.

### 4) Add runbook guidance for operators
- Update scaffolded operational docs to include:
  - how to intentionally approve docs deletions.
  - examples of acceptable explicit approval phrasing.
  - expectation for traceability in commits/PR descriptions.

### 5) Compose + verify templates
- Run:
  - `python3 ./meta-agent/scripts/compose-templates.py`
  - `python3 ./meta-agent/scripts/compose-templates.py --check`
- Run markdown link checks:
  - `python3 ./meta-agent/scripts/scan-markdown-links.py`
  - `python3 ./meta-agent/scripts/scan-markdown-links.py --fail-on-dead`

## Suggested acceptance criteria
- Newly scaffolded projects contain explicit language that `docs/` is a mutable baseline.
- Newly scaffolded projects include documented AI-agent non-deletion default behavior for `docs/`.
- CI in scaffolded projects flags docs deletions unless explicitly overridden.
- Operator docs include an explicit deletion approval workflow.

## Notes
- Per repository rules: edit `meta-agent/template-src/` (source of truth), then compose templates.
- Keep generated `meta-agent/templates/` changes machine-composed only.
