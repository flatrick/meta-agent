# Agent Writing Guidelines For Published Docs

The workspace-level published documentation subtree for Structurizr is `meta-agent/docs/architecture/site/_docs/`.
Element-scoped published docs are kept adjacent to model elements under `meta-agent/docs/architecture/site/model/**/_docs/`.
This guidance file is intentionally stored outside that subtree so it does not appear in the generated public site.

## Scope

- Write workspace-level publishable docs for Structurizr site under `meta-agent/docs/architecture/site/_docs/`.
- Write object-scoped publishable docs under the owning model element path in `meta-agent/docs/architecture/site/model/**/_docs/`.
- Keep internal-only or operator-only working notes outside both published paths (`_docs/` and `model/**/_docs/`).
- Detailed non-published architecture notes should live in `meta-agent/docs/architecture/internal/` using Johnny.Decimal area subfolders.

## Placement Rules

- Put site home/landing content in `meta-agent/docs/architecture/site/_docs/00-00-index.md`.
- Put architecture summary pages in `meta-agent/docs/architecture/site/_docs/`.
- Put operations summary pages in `meta-agent/docs/architecture/site/_docs/`.
- Put planning summary pages in `meta-agent/docs/architecture/site/_docs/`.
- Put system/container/component/deployment-specific docs in the corresponding model-local `_docs/` folder next to that DSL element definition.
- In each docs scope, use `00-index.md` as the first section so it renders as `Info`; use `01-...`, `02-...` for pages that should render under `Documentation`.
- Use numeric prefixes in filenames to keep ordering deterministic and scalable (for example: 10-01, 10-02, 10-03, 40-01, 40-02, 50-01).
- For non-published architecture docs, use Johnny.Decimal area subfolders under `meta-agent/docs/architecture/internal/` (start at `1x/10/index.md`).

## Content Rules

- Prefer links to canonical source docs in `meta-agent/docs/` when content is shared.
- In published Structurizr site docs (`site/_docs/` and `site/model/**/_docs/`), do not create markdown links to non-published docs under `meta-agent/docs/architecture/internal/`; use inline-code file paths for those references.
- Use markdown links for targets that are intended to be reachable in the same rendered surface.
- For repository-wide Markdown link/backlink checks, use `python3 ./meta-agent/scripts/scan-markdown-links.py` instead of manual full-tree scanning.
- For validation gates, use `python3 ./meta-agent/scripts/scan-markdown-links.py --fail-on-dead`.
- Use `.meta-agent-temp/markdown-link-report.json` for machine-readable backlinks and possible link/search forms per document.
- Do not copy sensitive deployment details into site docs.
- For deployment documentation, publish sanitized topology only.
- If sensitive deployment detail is needed, point to a private documentation repository.

## Why This Exists

- Limits accidental publication scope.
- Keeps a clean separation between published and non-published documentation.
- Makes agent behavior deterministic when choosing where docs should be written.
