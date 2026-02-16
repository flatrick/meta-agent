# 10-00 Architecture Index

## System summary

- System: `meta-agent`
- Canonical model: `meta-agent/docs/architecture/site/workspace.dsl`
- Primary C4 coverage: system context, containers, deployment (local + CI)

## Containers

- `MetaAgent.Cli`: command entrypoint and orchestration
- `MetaAgent.Core`: policy/workflow/triage/run-result logic
- `Templates`: scaffold source assets
- `Runtime Artifacts`: machine-readable execution records

## Detailed docs (non-published)

- `meta-agent/docs/architecture/internal/1x/11/11.01/index.md`
- `meta-agent/docs/architecture/internal/1x/11/11.02/index.md`
- `meta-agent/docs/architecture/internal/1x/11/11.03/index.md`
