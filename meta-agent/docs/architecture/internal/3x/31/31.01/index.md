# Architecture Roadmap

This page tracks architecture-facing documentation and modeling improvements.

## Near-term goals

1. Increase published architecture depth for each modeled view.
2. Maintain the compatibility map as Structurizr DSL usage expands.
3. Keep scaffold templates aligned with repository best practices.
4. Add C4 component modeling for `MetaAgent.Cli` and `MetaAgent.Core`.
5. Make currently implicit code relationships explicit in DSL (`MetaAgent.Core -> Runtime Artifacts`, `MetaAgent.Core -> Templates`).
6. Ensure scaffolded templates include alignment-loop documentation and update guidance.

## Mid-term goals

1. Add richer deployment overlays with clear sensitivity boundaries.
2. Expand dynamic/filtered view usage for critical workflows.
3. Add explicit ownership perspectives to key model elements.

## Guardrails

- Prefer small, traceable model updates over broad rewrites.
- Keep view keys stable unless deliberate versioning changes are required.
- Couple model updates with documentation updates in the same change.

## Success criteria

- Generated site is understandable by new contributors without private context.
- CI reliably catches architecture generation regressions.
- Scaffolded projects inherit maintainable architecture workflows by default.

## Current alignment-driven backlog

1. Add a component view for `MetaAgent.Cli` with command handlers as first-class components.
2. Add a component view for `MetaAgent.Core` covering policy, workflow, triage, run-result, metrics, and template rendering responsibilities.
3. Model `MetaAgent.Core -> Runtime Artifacts` based on existing persistence behavior in code.
4. Model `MetaAgent.Core -> Templates` based on `Generator` template discovery/rendering behavior.
5. Document architecture verification toolchain dependencies (verification scripts and CI jobs) as explicit architecture support relationships where appropriate.

## Workspace DSL modularization status

Implemented (repository workspace):
1. `meta-agent/docs/architecture/site/workspace.dsl` is now orchestration-focused and uses includes.
2. Model slices are split into:
- `model/10-elements.dsl`
- `model/meta-agent/software-system.dsl`
- `model/20-relationships.dsl`
- `model/deployment/30-local.dsl`
- `model/deployment/31-ci.dsl`
3. View slices are split into:
- `views/10-system-context.dsl`
- `views/20-containers.dsl`
- `views/30-deployment-local.dsl`
- `views/31-deployment-ci.dsl`
4. Model entity subfolders are in place (`model/meta-agent/...`) with element-scoped docs co-located under `_docs/`.
5. `!docs _docs` and `!adrs adrs` remain in orchestration.
6. View keys are unchanged (`SystemContext`, `Containers`, `LocalDevelopmentDeployment`, `CiDeployment`).

Validation performed:
1. `python3 ./meta-agent/scripts/structurizr-site.py generate`
2. `python3 ./meta-agent/scripts/verify-architecture.py`

Template alignment status:
1. `meta-agent/templates/dotnet/docs/architecture/site/workspace.dsl` now uses modular includes (`model/*`, `views/*`).
2. `meta-agent/templates/node/docs/architecture/site/workspace.dsl` now uses modular includes (`model/*`, `views/*`).
3. `meta-agent/templates/generic/docs/architecture/site/workspace.dsl` now uses modular includes (`model/*`, `views/*`).
4. All three templates now include system/container-local `_docs/` with `00-index.md` + `01-system-documentation.md` pattern.
5. Template guidance docs now document the deterministic `Info` vs `Documentation` routing semantics.
