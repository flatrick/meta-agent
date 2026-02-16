# 0004: ADR-first documentation governance

- Status: Accepted
- Date: 2026-02-14

## Context

Large chronological change logs create noise for operators and AI agents.
Architecture intent is easier to consume when decisions are captured as stable records instead of incremental notes.

## Decision

Architecture-affecting decisions are documented in ADRs under `meta-agent/docs/architecture/site/adrs/`.
`meta-agent/DOC_DELTA.md` is kept minimal and reserved for operational/release events that are not architecture decisions.

## Consequences

Positive:

- cleaner docs surface for first-time users
- reduced ambiguity for AI agents looking for canonical intent
- lower maintenance overhead for historical narrative documents

Trade-offs:

- less granular chronology in repository docs
- forensic timeline reconstruction relies more on git history and release artifacts
