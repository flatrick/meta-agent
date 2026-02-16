# structurizr-site-generatr Compatibility Matrix

This document tracks Structurizr DSL compatibility findings for the `meta-agent` repository and its scaffold templates.

Primary probe source: compatibility probe runs performed in this repository.

## Purpose

- Keep a durable map of what works, what is limited, and what still needs validation.
- Avoid repeatedly rediscovering parser/rendering constraints.
- Give humans and AI agents a single reference when choosing DSL patterns.

## Status labels

- `Supported`: validated in generated output.
- `Limited`: works only under constraints, or fails in some forms.
- `Not validated`: not tested yet.

## Current matrix

### Views and navigation

- System context view: `Supported`
- Container view: `Supported`
- Component view: `Supported`
- Deployment views: `Supported`
- Filtered views: `Supported`
- Dynamic views: `Limited`
  - `dynamic <softwareSystem>` with container relationships: supported.
  - `dynamic *` with container relationships: not supported.

### Modeling features

- Element tags/styles: `Supported`
- Relationship tags/styles: `Supported`
- Element perspectives: `Supported`
- Relationship perspectives: `Supported`
- Relationship properties: `Supported`
- Archetypes: `Supported`
- `workspace extends`: `Supported`
- `!impliedRelationships`: `Supported`

### Animation and operations metadata

- System context animation: `Supported`
- Deployment animation: `Limited`
  - Supported with container identifiers (`ss.wa`, `ss.db`).
  - Not supported with deployment instance identifiers (`localWebApp`, `prodWebApp`).
- Deployment `healthCheck`: `Supported`

### Documentation integration

- `!docs` workspace docs: `Supported`
- element-scoped `!docs`: `Supported`
- docs section ordering: `Supported`
  - first section in a docs scope renders as `Info`
  - remaining sections render under `Documentation`
- Markdown rendering: `Supported`
- Markdown cross-page links via `.md` targets: `Limited`
  - Prefer generated slug paths such as `10-00-architecture-index/` and `50-01-multi-workspace-strategy/`.
- `!adrs` integration: `Supported`

### Branding

- `branding` with scalar `font` value: `Supported`
- nested `font { name ... url ... }` block: `Limited` (parser rejection observed)

## Operational guidance

- Keep agent-operation docs outside published `!docs` trees.
- Prefer patterns already marked `Supported`.
- If using a `Limited` feature, document the exact workaround in project docs.

## Re-test process

1. Add probe change in an isolated test workspace.
2. Generate with `structurizr-site-generatr`.
3. Verify expected structures in `workspace.json` and static HTML output.
4. Update this matrix with concrete outcomes.

## Static HTML smoke test baseline

1. Generate site.
2. Serve site locally.
3. Validate root redirect/home page renders.
4. Validate expected views/pages in served `master/workspace.json`.


## Compatibility changelog

- 2026-02-15: Initial compatibility baseline documented from probe runs (views, dynamic/filtered/deployment behavior, branding, extends/archetypes/implied relationships).
- 2026-02-15: Documented markdown cross-page link limitation; slug-style links are the reliable default for published docs.
