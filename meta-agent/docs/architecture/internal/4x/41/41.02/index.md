# Structurizr DSL + structurizr-site-generatr Playbook

Audience: humans and AI/LLM agents maintaining Structurizr assets in this repository and generated projects.

## Purpose

Provide one repeatable operating model for authoring Structurizr DSL and validating static site output.

## Recommended architecture workflow

1. Model change intent first (scope, dependencies, deployment, docs impact).
2. Apply minimal DSL edits in modular files.
3. Update published docs and ADRs where applicable.
4. Generate and validate output with repository wrappers.
5. Perform static HTML smoke checks before merging.

## Authoring rules

- Keep `workspace.dsl` orchestration-focused and include modular slices.
- Keep model slices modular (relations/views/deployment separated).
- Prefer model subfolders per bounded entity (for example `model/<system>/containers/<container>/...`) to keep element DSL and docs co-located.
- Keep view keys stable unless intentionally versioning/breaking.
- Keep agent instruction files outside published `!docs` paths.
- Keep object-scoped documentation with the object definition (for example, system/container/component/deployment docs stay adjacent to their DSL definitions, not in unrelated slices).
- If the primary site becomes too deep, split into layered companion workspaces/sites; this is valid even for one owning team.

## Documentation routing rules

- For every docs scope (`_docs` or model-local `_docs/`), the first section renders as `Info`.
- Use `00-index.md` as the first section in each docs scope.
- Put additional detail pages in `01-...`, `02-...` so they render under `Documentation`.
- Keep summary intent in `Info` and avoid duplicating full detail pages there.
- Use markdown links only for targets expected to be reachable in the generated site surface.
- When referencing non-published/internal docs from published pages, use inline-code paths instead of markdown links to avoid site 404s.

## Validation rules

Use local wrappers in this repository:

- Generate: `python3 ./meta-agent/scripts/structurizr-site.py generate`
- Serve: `python3 ./meta-agent/scripts/structurizr-site.py serve --port 8080`

For scaffolded projects, use their local wrappers or equivalent Docker commands.

Minimum checks:

- generation succeeds
- expected view keys exist
- expected docs pages render
- markdown link scan report is clean (`python3 ./meta-agent/scripts/scan-markdown-links.py --fail-on-dead`)
- dependencies/deployment pages behave as intended

## Agent operational contract

When an agent edits Structurizr files, it must:

1. read project-specific Structurizr guidance + agent writing rules;
2. make targeted edits only;
3. run validation commands;
4. run `python3 ./meta-agent/scripts/scan-markdown-links.py` for documentation link/backlink integrity instead of manual repository-wide markdown scanning;
5. report compatibility or rendering constraints discovered;
6. update compatibility notes when new limitations are found.

## Compatibility discipline

Canonical matrix: `meta-agent/docs/architecture/internal/4x/41/41.03/index.md`.


Because `structurizr-site-generatr` may lag Structurizr DSL features:

- maintain a compatibility tracker in each project;
- label feature behavior as `Supported`, `Limited`, or `Not validated`;
- document concrete workarounds for limited features.

## Definition of done

A Structurizr change is complete when:

- model + docs are coherent,
- static output is verified,
- compatibility notes are current,
- reviewers can reproduce validation locally.
