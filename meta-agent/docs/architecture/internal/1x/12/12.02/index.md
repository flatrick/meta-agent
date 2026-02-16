# Structurizr Alignment Loop

Use this loop to keep architecture and code aligned without introducing parallel architecture documentation.

## Canonical inputs

- Architecture source of truth: `meta-agent/docs/architecture/site/workspace.dsl`.
- Published architecture docs: `meta-agent/docs/architecture/site/_docs/` and `meta-agent/docs/architecture/site/model/**/_docs/`.
- CI verification entrypoint: `meta-agent/scripts/verify-architecture.py`.

## Loop steps

1. Start from model intent:
identify which system/container/deployment relationships should exist.
2. Verify code reality:
confirm container ownership, entrypoints, and key responsibilities in code.
3. Record mismatches:
capture model gaps, drift, and relationship mismatches in existing docs pages.
4. Apply minimal updates:
edit DSL/docs with traceable, bounded changes.
5. Validate generation:
run local generation and architecture verification commands.
6. Confirm CI alignment:
ensure architecture verification jobs still gate publication.

## Minimum evidence for each alignment cycle

- Container-to-code mapping with concrete file paths.
- At least three representative runtime flows.
- Explicit statement of model gaps and planned remediation.
- Successful site generation output.

## Working rules

- Structurizr DSL remains canonical for architecture.
- Code does not silently override model; disagreements are documented first.
- Keep architecture notes in Structurizr-published docs, not ad hoc side files.
- Keep non-architecture operational notes in non-Structurizr docs.
