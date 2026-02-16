# System Documentation

This page captures the high-level architecture intent for the `meta-agent` system.

## Architectural intent

- Keep governance and policy enforcement explicit at command boundaries.
- Keep workflow outcomes observable through structured runtime artifacts.
- Keep architecture documentation concise at workspace level and detailed at element level.

## Primary structural boundaries

- `MetaAgent.Cli`: command entrypoint and interaction boundary.
- `MetaAgent.Core`: policy/workflow/triage domain logic.
- `Templates`: scaffold source for generated repositories.
- `Runtime Artifacts`: persisted decision/workflow/result/metrics records.

## C4 coverage in this workspace

- System context (`SystemContext`)
- Containers (`Containers`)
- Deployment (`LocalDevelopmentDeployment`, `CiDeployment`)

## Documentation model

- Workspace-level overview docs are published from `_docs/`.
- System and container details are published from model-local `_docs/` folders via element-scoped `!docs`.
