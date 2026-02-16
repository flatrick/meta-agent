# Structurizr Model Folder

This folder contains the Structurizr model inputs for the `meta-agent` repository:

- `workspace.dsl` - system context/container/deployment views and docs/ADR wiring.
- `_docs/` - published workspace-level Structurizr documentation pages (`!docs _docs`).
- `model/**/_docs/` - published element-level docs co-located with system/container/component/deployment definitions (element-level `!docs`).
- `adrs/` - ADRs included in generated site navigation (`!adrs adrs`).
- `assets/` - optional site/model assets for Structurizr generation.
- [Structurizr Playbook](../../../4x/41/41.02/index.md) - shared human + agent operating playbook.
- [Compatibility Matrix](../../../4x/41/41.03/index.md) - compatibility matrix for Structurizr DSL features under `structurizr-site-generatr`.
- `../internal/` - non-published detailed working architecture notes.

Only content inside this folder is consumed by Structurizr for this repository.
`workspace.dsl` references workspace-level published pages via `!docs _docs`, and model elements can reference co-located docs via local `!docs _docs/`.
