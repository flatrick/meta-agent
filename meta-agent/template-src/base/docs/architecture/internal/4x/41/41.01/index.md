# Structurizr Quickstart

This project includes a Structurizr DSL workspace:
- `docs/architecture/site/workspace.dsl`

This workspace is pre-wired with:
- `!docs _docs` for workspace-level pages
- element-scoped `!docs` in `docs/architecture/site/model/**/_docs/`
- `!adrs adrs`
- published workspace docs under `docs/architecture/site/_docs/`
- element docs adjacent to model definitions under `docs/architecture/site/model/**/_docs/`
- human + agent Structurizr playbook in `docs/architecture/internal/4x/41/41.02/index.md`
- model-to-code alignment workflow in `docs/architecture/internal/1x/12/12.01/index.md`
- structurizr-site-generatr compatibility matrix in `docs/architecture/internal/4x/41/41.03/index.md`
- setup/configuration guide for humans + agents in `docs/architecture/internal/4x/41/41.04/index.md`
- agent publishing rules in `docs/architecture/site/AGENT_WRITING_GUIDELINES.md` (non-published)
- starter ADRs under `docs/architecture/site/adrs/`

## Documentation rendering semantics

- For workspace-level docs (`!docs _docs`), the first ordered section becomes the workspace home page.
- For element-scoped docs (`!docs _docs/`), the first ordered section becomes that element's `Info` page.
- Remaining sections in the same scope appear under `Documentation` for that element.
- Use markdown links only for targets expected to be reachable in the generated site.
- Use inline-code paths when referencing intentionally non-published/internal docs from published pages.

Recommended naming for deterministic output:

- Use `00-index.md` as the first page in every docs scope.
- Use `01-...`, `02-...` for additional pages.

Homepage behavior:

- The first workspace-level docs file in `docs/architecture/site/_docs/` (alphabetically) becomes the generated site homepage.
- Use `docs/architecture/site/_docs/00-00-index.md` as the explicit landing page.

ADR behavior:

- ADR files in `docs/architecture/site/adrs/` are rendered into site navigation.
- During scaffold (`init`), ADR IDs are configurable via `--adr-id-prefix`.
- Example: `--adr-id-prefix PLATFORM-1234` produces files like `PLATFORM-1234-ai-governance.md`.
- If `--adr-id-prefix` is omitted, `init` derives a Jira-style key from `--ticket`/`--ticket-file` when present, otherwise defaults to `0001`.

Deployment behavior:

- The workspace includes starter deployment environments/views for local development and production.
- Keep sensitive deployment detail out of the main repository when needed by maintaining private deployment overlays in a separate private repository.

Multi-workspace guidance:

- Keep the primary workspace/site architecture-first and concise.
- Use companion detailed workspaces/sites when system depth grows, even if one team owns the full system.
- Split by comprehension and change-surface, then link from primary to companion sites.

Generate static site (Docker):

```bash
docker run -it --rm \
  --user "$(id -u):$(id -g)" \
  -v "$(pwd)/docs/architecture/site:/var/model" \
  -w /var/model \
  ghcr.io/avisi-cloud/structurizr-site-generatr \
  generate-site --workspace-file workspace.dsl --assets-dir assets
```

Serve locally (Docker):

```bash
docker run -it --rm \
  --user "$(id -u):$(id -g)" \
  -v "$(pwd)/docs/architecture/site:/var/model" \
  -p 8080:8080 \
  -w /var/model \
  ghcr.io/avisi-cloud/structurizr-site-generatr \
  serve --workspace-file workspace.dsl --assets-dir assets
```

Important:

- `serve` requires `-p 8080:8080` (or another host-port mapping).
- `generate-site` does not expose HTTP and does not need `-p`.

Verify architecture compatibility (recommended; all platforms):

```bash
python3 ./scripts/verify-architecture.py
```

SCM CI examples included:

- GitHub Actions: `.github/workflows/architecture-docs.yml` (GitHub Pages deploy on SemVer tags)
- GitLab CI: `.gitlab-ci.yml` (GitLab Pages deploy on `main`)

Use whichever CI style matches your platform, or adapt both patterns to your SCM provider.

## Known limitations and changelog

- Track feature support in `docs/architecture/internal/4x/41/41.03/index.md` using `Supported`, `Limited`, and `Not validated`.
- When testing a new Structurizr DSL feature, add a dated probe note and workaround details if behavior is limited.
