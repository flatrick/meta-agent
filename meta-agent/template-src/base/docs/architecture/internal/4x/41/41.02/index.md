# Structurizr DSL + structurizr-site-generatr Playbook

Use this playbook for both human and AI/LLM agent updates.

Compatibility matrix: `docs/architecture/internal/4x/41/41.03/index.md`.
Alignment workflow: `docs/architecture/internal/1x/12/12.01/index.md`.

## Workflow

1. Define intended architecture change and affected views/docs.
2. Update DSL with minimal, scoped edits.
3. Update docs/ADRs if behavior or decisions changed.
4. Generate site output.
5. Serve and inspect static HTML output.
6. Record any compatibility limitations discovered.

## Authoring rules

- Keep `docs/architecture/site/workspace.dsl` orchestration-focused.
- Keep model slices modular as complexity grows.
- Keep object-scoped docs adjacent to object definitions (`model/**/_docs/`).
- Keep view keys stable unless intentionally changed.
- Keep non-published agent instructions outside published `!docs` paths.
- If this workspace grows too deep, split into layered companion workspaces/sites.

## Documentation routing rules

- For each docs scope (`_docs` or element `_docs/`), the first section is rendered as `Info`.
- Use `00-index.md` as the first page in each docs scope for deterministic rendering.
- Put deeper detail into `01-...`, `02-...` files so it appears under `Documentation`.
- Use markdown links only for targets expected to be reachable in the same rendered documentation surface.
- If a referenced document is intentionally non-published, keep it as an inline-code path to avoid dead site links.

## Validation checklist

- generation succeeds
- expected view keys exist
- docs navigation includes changed pages
- `Info` vs `Documentation` routing is correct for changed docs scopes
- dependencies/deployment views render as expected

## Agent rules

Before edits, read:

- `docs/architecture/site/AGENT_WRITING_GUIDELINES.md`
- `docs/architecture/internal/4x/41/41.01/index.md`
- `docs/architecture/internal/4x/41/41.04/index.md`

After edits, report:

- changed files
- render outcome
- compatibility notes (Supported/Limited/Not validated)
