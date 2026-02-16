# structurizr-site-generatr Compatibility Matrix

Use this file to track which Structurizr DSL features are confirmed to work in this project when rendered by `structurizr-site-generatr`.

## Why maintain this

- Feature support can differ from full Structurizr DSL documentation.
- Teams and AI agents need a stable compatibility reference.

## Status labels

- `Supported`
- `Limited`
- `Not validated`

## Starter matrix

### Views and navigation

- System context: `Supported`
- Container: `Supported`
- Component: `Supported`
- Deployment: `Supported`
- Filtered views: `Supported`
- Dynamic views: `Limited`
  - `dynamic <softwareSystem>` with container relationships is generally supported.
  - `dynamic *` with container relationships is generally limited.

### Modeling features

- tags/styles: `Supported`
- perspectives/properties: `Supported`
- archetypes: `Supported`
- `workspace extends`: `Supported`
- `!impliedRelationships`: `Supported`

### Deployment specifics

- deployment animation with container identifiers: `Supported`
- deployment animation with deployment-instance identifiers: `Limited`
- deployment `healthCheck`: `Supported`

### Docs/ADR integration

- `!docs` workspace docs: `Supported`
- element-scoped `!docs`: `Supported`
- docs section ordering: `Supported`
  - first section in a docs scope renders as `Info`
  - remaining sections render under `Documentation`
- markdown cross-page links via `.md` targets: `Limited` (prefer slug paths such as `10-00-architecture-index/`)
- `!adrs`: `Supported`

### Branding

- scalar `font` in `branding`: `Supported`
- nested `font { name ... url ... }`: `Limited`

## Project-specific updates

When you test a new DSL capability:

1. add/update model feature;
2. generate + serve site;
3. verify static HTML and `workspace.json` output;
4. update this file with concrete pass/fail notes.

## Compatibility changelog

- YYYY-MM-DD: Add tested feature outcomes and workarounds here.
- YYYY-MM-DD: Confirm markdown cross-page links and prefer slug targets instead of `.md` targets.
