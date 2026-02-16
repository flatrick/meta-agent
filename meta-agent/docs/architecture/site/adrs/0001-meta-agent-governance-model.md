# 0001: Meta-agent governance model

- Status: Accepted
- Date: 2026-02-14

## Context

We need a reusable system that helps teams run AI-assisted software delivery with clear safety, observability, and operational controls.

## Decision

Use `meta-agent` as a governance + operating layer with:

- repository bootstrap (`init`) and existing-repo onboarding (`configure`)
- policy-gated execution (`validate`)
- structured risk intake (`triage`)
- persistent operational artifacts (`decision`, `workflow`, `run-result`, `metrics`)

## Consequences

Positive:

- reproducible control points for human-assisted and autonomous modes
- audit-friendly machine-readable records
- easier policy evolution and drift detection

Trade-offs:

- more process overhead than ad-hoc agent usage
- requires teams to maintain policy/docs discipline
