# Structurizr Alignment Workflow

This workflow keeps architecture and code aligned while preserving Structurizr as the canonical source of truth.

## Principles

- `docs/architecture/site/workspace.dsl` is the architecture source of truth.
- Architecture updates and architecture docs updates should be delivered together.
- Mismatches are reported explicitly; they are not silently resolved.

## Repeatable loop

1. Identify impacted C4 elements (system, containers, components, deployment).
2. Verify code anchors for each impacted container/component.
3. Record model gaps, drift, and relationship mismatches in architecture docs.
4. Update DSL and docs with minimal, traceable edits.
5. Generate/verify Structurizr site output before merge.

## Minimum checks

- Site generation succeeds.
- Expected views render.
- Updated docs appear in navigation.
- CI architecture verification remains green.
