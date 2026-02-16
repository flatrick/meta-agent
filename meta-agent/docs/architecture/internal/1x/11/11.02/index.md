# Container Responsibilities

This page maps each modeled container to concrete responsibility boundaries.

## MetaAgent.Cli

Responsibility:
- Provide a deterministic command interface for operators, developers, and automation.

Key concerns:
- Command parsing and invocation flow.
- Mode/autonomy context handling.
- Artifact output path orchestration.

## MetaAgent.Core

Responsibility:
- Implement policy interpretation and workflow gate enforcement.

Key concerns:
- Policy validation and migration.
- Triage gating and safety-level checks.
- Run-result and metrics generation.

## Templates

Responsibility:
- Provide scaffold assets for new project initialization.

Key concerns:
- Template quality and consistency.
- Architecture docs, workflows, and policy starter assets.
- Portability across runtime targets.

## Runtime Artifacts

Responsibility:
- Persist machine-readable records from command execution.

Key concerns:
- Deterministic shape and stable schema contracts.
- Easy auditability and CI consumption.
- Low-friction local inspection.

## Container interaction summary

- `MetaAgent.Cli -> MetaAgent.Core`: policy/workflow enforcement and command logic.
- `MetaAgent.Cli -> Templates`: read scaffold inputs for generation.
- `MetaAgent.Cli -> Runtime Artifacts`: write and read structured outputs.

## Alignment notes (model vs code)

Current model coverage:

- All modeled containers are verified in code or repository structure.
- Container-level relationships above are implemented and observable.

Model gaps currently tracked:

- C4 component-level modeling is not yet present in `workspace.dsl`.
- The following stable code components are candidates for explicit component modeling:
- CLI: `InitCommand`, `ConfigureCommand`, `ValidateCommand`, `TriageCommand`, `AgentCommand`, `VersionCommand` (`meta-agent/dotnet/MetaAgent.Cli/`).
- Core: `PolicyEnforcer`, `WorkflowEngine`, `TriageEngine`, `PolicySchemaValidator`, `RunResultSectionsBuilder`, `RunResultArtifactPaths`, `MetricsScoreboard`, `BudgetUsageStore`, `Generator` (`meta-agent/dotnet/MetaAgent.Core/`).

Relationship refinements to consider:

- `MetaAgent.Core -> Runtime Artifacts` is implicit in code and should be modeled explicitly (examples: `WriteDecisionRecord`, `WriteWorkflowRecord`, metrics/budget persistence).
- `MetaAgent.Core -> Templates` is implicit in code via `Generator` and should be modeled explicitly.
