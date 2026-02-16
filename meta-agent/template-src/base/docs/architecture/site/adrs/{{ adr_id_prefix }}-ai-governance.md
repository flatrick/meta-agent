# {{ adr_id_prefix }}: AI-assisted delivery governance for {{ project_name }}

- Status: Proposed
- Date: 2026-02-15

## Context

This repository adopts AI-assisted development and needs explicit guardrails for quality, safety, and team trust.

## Decision

Adopt a governance-first operating model:

- use policy-gated validation before risky execution
- use task triage for risk/safety classification
- keep machine-readable decision/workflow/run artifacts
- require explicit human approvals where required by policy

## Consequences

Positive:

- clearer operational boundaries for AI usage
- improved auditability and repeatability

Trade-offs:

- additional upfront process and documentation overhead
