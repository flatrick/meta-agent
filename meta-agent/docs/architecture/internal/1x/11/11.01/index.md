# System Overview

`meta-agent` is a governance and operating layer for AI-assisted software delivery.

## Mission

- Standardize how teams initialize and govern AI-assisted engineering workflows.
- Enforce policy gates for autonomy, safety, budget, and workflow quality.
- Produce deterministic, auditable runtime artifacts for each significant run.

## Core actors

- Developer: Runs and reviews interactive workflows.
- Platform Operator: Defines and maintains governance policy.
- Autonomous Agent Runner: Executes approved autonomous ticket flows.

## Architectural shape

`meta-agent` is modeled as one software system with four key containers:

- `MetaAgent.Cli`: command entrypoint and user/operator interface.
- `MetaAgent.Core`: policy engine and workflow enforcement logic.
- `Templates`: scaffold assets used for generated projects.
- `Runtime Artifacts`: generated records used for auditability and traceability.

## Primary workflow

1. A command is invoked (`init`, `configure`, `validate`, `triage`, etc.).
2. Policy and mode gates are evaluated in core logic.
3. Execution is allowed or blocked with explicit rationale.
4. Structured artifacts are written to the workspace.

## Verified implementation anchors

- CLI entrypoint and command dispatch: `meta-agent/dotnet/MetaAgent.Cli/Program.cs`.
- Policy gate evaluation and decision records: `meta-agent/dotnet/MetaAgent.Core/PolicyEnforcer.cs`.
- Workflow stage generation and persistence: `meta-agent/dotnet/MetaAgent.Core/WorkflowEngine.cs`.
- Triage evaluation: `meta-agent/dotnet/MetaAgent.Core/TriageEngine.cs`.
- Template rendering for scaffold output: `meta-agent/dotnet/MetaAgent.Core/Generator.cs`.

## Representative runtime flows

1. `init` flow:
`Program.Main -> InitCommand.Execute -> WorkflowEngine.BuildForInit -> PolicyEnforcer.Evaluate -> Generator.RenderTemplate`.
2. `validate` flow:
`Program.Main -> ValidateCommand.Execute -> PolicyEnforcer.Evaluate -> PolicySchemaValidator.ValidateFile`.
3. `triage` flow:
`Program.Main -> TriageCommand.Execute -> TriageEngine.Evaluate -> RunResultOrchestrator.TryWrite`.

## Non-goals

- Runtime app hosting/orchestration for generated applications.
- Replacing team-specific software development lifecycle practices.
- Centralized secret or production environment management.
