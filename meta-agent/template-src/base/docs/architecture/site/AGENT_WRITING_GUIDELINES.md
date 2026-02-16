# Agent Writing Guidelines For Published Docs

The workspace-level published documentation subtree for Structurizr is `docs/architecture/site/_docs/`.
Element-scoped published docs are kept adjacent to model elements under `docs/architecture/site/model/**/_docs/`.
This guidance file is intentionally outside published `!docs` paths.

## Scope

- Write workspace-level publishable docs under `docs/architecture/site/_docs/`.
- Write object-scoped publishable docs under the owning model element path in `docs/architecture/site/model/**/_docs/`.
- Keep internal-only working notes outside both published paths.

## Placement Rules

- Workspace home: `docs/architecture/site/_docs/00-00-index.md`
- Area indexes: `AA-00-index.md` in `docs/architecture/site/_docs/`
- Object docs: keep system/container/component/deployment docs next to each DSL definition in local `_docs/`
- Use numeric prefixes for stable ordering (`00-`, `01-`, `10-`, `40-`, `50-`, etc.)

## Structurizr Rendering Rules

- Within each `!docs` scope, the first section renders as that scope's `Info` page.
- For deterministic behavior, use `00-index.md` as the first section in every `_docs/` folder.
- Additional files (`01-...`, `02-...`) render under `Documentation` for that element.
- For workspace-level docs (`!docs _docs`), the first page in that scope is the workspace home page.

## Content Rules

- Keep published pages concise and architecture-first.
- Prefer links to canonical non-published docs when deeper operational detail is needed.
- In published docs scopes (`_docs` and `model/**/_docs/`), use markdown links only for targets expected to be reachable in the generated site.
- When referencing non-published/internal docs from published pages, use inline-code paths instead of markdown links.
- Keep sensitive deployment details out of published docs.
