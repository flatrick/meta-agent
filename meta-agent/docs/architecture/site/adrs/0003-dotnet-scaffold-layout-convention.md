# 0003: Dotnet scaffold layout convention

- Status: Accepted
- Date: 2026-02-14

## Context

Template consumers need predictable project structure that aligns with mature .NET repository conventions and CI usage.

## Decision

Dotnet scaffolds use a fixed repository layout:

- application project under `src/`
- test project under `tests/`
- root solution file `<project-name>.slnx`

The solution includes both projects and is the canonical entry point for `dotnet build` and `dotnet test`.

## Consequences

Positive:

- immediate test-project presence in every scaffolded service
- consistent paths for tooling, policies, and CI scripts
- straightforward adoption by teams using standard repo layouts

Trade-offs:

- templates that assume flat root-level `.csproj` layout must be adjusted
