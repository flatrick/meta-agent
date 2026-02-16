# {{ project_name }} Documentation Index

This published site is intentionally concise and architecture-first.

## Areas

- [10-00 Architecture Index](10-00-architecture-index/)
- [40-00 Operations Index](40-00-operations-index/)
- [50-00 Planning Index](50-00-planning-index/)
- [99-00 Archive Index](99-00-archive-index/)

## Publication boundary

Published workspace-level docs come from `docs/architecture/site/_docs/` (`!docs _docs`).
Published element-level docs come from `docs/architecture/site/model/**/_docs/` when an element defines local `!docs`.
Keep internal-only notes outside both published paths.

## Depth control

If this site becomes too deep, keep this workspace high-level and link to companion detailed workspaces/sites.
This split is valid even when one team owns the full system.
