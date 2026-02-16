# 00-00 Index

This published site is intentionally concise: it explains system shape and design choices first.

## Areas

- [10-00 Architecture Index](10-00-architecture-index/) - architecture summary (system, containers, deployment)
- [40-00 Governance Index](40-00-governance-index/) - architecture governance and verification summary
- [50-00 Architecture Roadmap Index](50-00-architecture-roadmap-index/) - architecture roadmap summary
- [99-00 Archive Index](99-00-archive-index/) - archive index

## Publication boundary

Published workspace-level docs come from `meta-agent/docs/architecture/site/_docs/` (`!docs _docs`).
Published element-level docs come from `meta-agent/docs/architecture/site/model/**/_docs/` where elements define local `!docs`.

Detailed working docs live in `meta-agent/docs/architecture/internal/` (Johnny.Decimal area subfolders; see `1x/10/index.md` there).
When referencing non-published internal docs from published site pages, keep those references as inline-code paths (not markdown links) to avoid dead links in generated output.

## Depth control

If system documentation outgrows this entrypoint site, use multiple workspaces/sites:

- keep this site high-level
- publish deeper domain/workflow details in companion sites
- link outward from this primary site
- allow layered companion workspaces even when one team owns the whole system
