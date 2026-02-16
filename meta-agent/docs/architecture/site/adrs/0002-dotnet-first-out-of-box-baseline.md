# 0002: Dotnet-first out-of-box baseline

- Status: Accepted
- Date: 2026-02-14

## Context

The project goal is a strong out-of-box experience with minimal operator tuning.
Runtime-selection complexity in the policy contract adds surface area without helping the default path.

## Decision

Use a dotnet-first baseline for scaffold and runtime expectations:

- `init` defaults to the dotnet template for first-run usage.
- policy contract does not include a runtime-selection key.
- downstream teams that want additional runtimes can extend templates independently.

## Consequences

Positive:

- simpler policy surface and onboarding guidance
- fewer branches in validation/migration behavior
- clearer default for operators and AI agents

Trade-offs:

- less built-in flexibility for mixed-runtime organizations
- runtime expansion requires explicit downstream customization
