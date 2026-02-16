# Structurizr Architecture Documentation

This project can use Structurizr DSL as the architecture source of truth and generate a static architecture website.
The model is intentionally kept under `meta-agent/docs/architecture/site/` so everything Structurizr reads for this repository is in one subtree.

## Repository Layout

- Structurizr workspace DSL: `meta-agent/docs/architecture/site/workspace.dsl`
- Structurizr assets folder: `meta-agent/docs/architecture/site/assets/`
- Structurizr-published docs folder (wired via `!docs _docs`): `meta-agent/docs/architecture/site/_docs/`
- Element-scoped Structurizr docs folders (wired via element-level `!docs`): `meta-agent/docs/architecture/site/model/**/_docs/`
- Canonical docs root (not auto-published): `meta-agent/docs/`
- Human + agent Structurizr playbook: `meta-agent/docs/architecture/internal/4x/41/41.02/index.md`
- structurizr-site-generatr compatibility matrix: `meta-agent/docs/architecture/internal/4x/41/41.03/index.md`
- Agent writing rules (non-published): `meta-agent/docs/architecture/internal/4x/41/41.04/index.md`
- Structurizr ADR folder (wired via `!adrs adrs`): `meta-agent/docs/architecture/site/adrs/`
- Generated site output (recommended): `meta-agent/docs/architecture/site/build/`

Homepage behavior:
- The first workspace-level docs file in `meta-agent/docs/architecture/site/_docs/` (alphabetical sort) becomes the site homepage.
- This repository uses `meta-agent/docs/architecture/site/_docs/00-00-index.md` as the generated homepage.

Documentation rendering semantics:
- Within each `!docs` scope, the first section is rendered as `Info`.
- For workspace-level docs (`!docs _docs`), that first section is the workspace homepage.
- For element-scoped docs (`!docs _docs/`), that first section is the element `Info` page.
- Remaining sections in the same docs scope appear under `Documentation`.
- Use `00-index.md` as the first section and `01-...`, `02-...` for additional pages to keep behavior deterministic.
- Link semantics are publication-aware:
  - use markdown links for targets expected to be reachable in the rendered site
  - use inline-code paths when referencing non-published internal docs from published pages

ADR behavior:
- ADRs in `meta-agent/docs/architecture/site/adrs/` are included in this repository's generated site navigation.
- Scaffolded projects keep ADRs under `docs/architecture/site/adrs/`.
- ADR filename IDs are template-driven (`{{ adr_id_prefix }}`), so you can use numeric or work-item-linked IDs.
- In scaffolded repositories, ADR IDs can be set during `init` via `--adr-id-prefix` (for example `--adr-id-prefix PLATFORM-1234`).
- If `--adr-id-prefix` is omitted, `init` derives a Jira-style key from `--ticket`/`--ticket-file` when available; otherwise it falls back to `0001`.

Deployment modeling behavior:
- Deployment environments and deployment views are part of the workspace (local development, CI, production baseline).
- Keep deployment topology useful but sanitized in the main repository.

Agent guidance:
- Agents should write workspace-level publishable docs under `docs/architecture/site/_docs/`.
- Agents should write object-scoped publishable docs under the owning model element path (`docs/architecture/site/model/**/_docs/`) and keep them adjacent to that element definition.
- Agents should write canonical/internal working docs under `docs/architecture/internal/` when publication is not intended (Johnny.Decimal area subfolders under `internal/`).

Multi-workspace guidance:
- The primary repository workspace/site should stay concise and architecture-first.
- Companion detailed workspaces/sites are allowed when depth grows, including for systems owned by a single team.
- Split by comprehension and change-surface, not only by team boundaries.

## Tooling

This guide uses:
- Structurizr DSL workspace file
- `structurizr-site-generatr` for static website generation

Upstream tool:
- `https://github.com/avisi-cloud/structurizr-site-generatr`

## Canonical Commands (Low-Friction)

Use repository scripts first. Raw `docker run` commands are reference/fallback only.

All platforms (Python):

```bash
python3 ./meta-agent/scripts/structurizr-site.py generate
python3 ./meta-agent/scripts/structurizr-site.py serve --port 8080
python3 ./meta-agent/scripts/structurizr-site.py generate --dry-run
```

Wrapper behavior checks:

```bash
python3 ./meta-agent/scripts/test-structurizr-site-wrappers.py
```

Documentation link/backlink checks:

```bash
python3 ./meta-agent/scripts/scan-markdown-links.py
python3 ./meta-agent/scripts/scan-markdown-links.py --fail-on-dead
```

Compatibility smoke check (recommended before merge):

```bash
python3 ./meta-agent/scripts/verify-architecture.py
```

## CLI Reference Discovery

Show top-level help (local install):

```bash
structurizr-site-generatr --help
```

Show top-level help (Docker):

```bash
docker run -it --rm ghcr.io/avisi-cloud/structurizr-site-generatr --help
```

Expected subcommands:
- `serve` — start a development server
- `generate-site` — generate a static site for the selected workspace
- `version` — print version information

Show subcommand-specific help:

```bash
structurizr-site-generatr serve --help
structurizr-site-generatr generate-site --help
```

```bash
docker run -it --rm ghcr.io/avisi-cloud/structurizr-site-generatr serve --help
docker run -it --rm ghcr.io/avisi-cloud/structurizr-site-generatr generate-site --help
```

## Local Binary (Safe Usage)

Avoid calling `structurizr-site-generatr` directly from repository root with workspace-file paths.
Direct invocation can emit output under root-level `build/`.

Use wrapper scripts with local binary mode instead:

```bash
python3 ./meta-agent/scripts/structurizr-site.py generate --use-local-binary
python3 ./meta-agent/scripts/structurizr-site.py serve --use-local-binary --port 8080
```

## Generate Site (Docker)

Run from repository root:

```bash
docker run -it --rm \
  --user "$(id -u):$(id -g)" \
  -v "$(pwd)/meta-agent/docs/architecture/site:/var/model" \
  -w /var/model \
  ghcr.io/avisi-cloud/structurizr-site-generatr \
  generate-site --workspace-file workspace.dsl --assets-dir assets
```

## Serve During Editing (Docker)

Run from repository root:

```bash
docker run -it --rm \
  --user "$(id -u):$(id -g)" \
  -v "$(pwd)/meta-agent/docs/architecture/site:/var/model" \
  -p 8080:8080 \
  -w /var/model \
  ghcr.io/avisi-cloud/structurizr-site-generatr \
  serve --workspace-file workspace.dsl --assets-dir assets
```

Then open:
- `http://localhost:8080`

Important:
- `serve` requires `-p 8080:8080` (or another host-port mapping) so your browser can reach the container.
- `generate-site` does not expose HTTP and therefore does not need `-p`.

Custom host port example:

```bash
docker run -it --rm \
  --user "$(id -u):$(id -g)" \
  -v "$(pwd)/meta-agent/docs/architecture/site:/var/model" \
  -p 9090:8080 \
  -w /var/model \
  ghcr.io/avisi-cloud/structurizr-site-generatr \
  serve --workspace-file workspace.dsl --assets-dir assets
```

Then open:
- `http://localhost:9090`

Serve notes:
- Keep this command running while you browse.
- Stop serving with `Ctrl+C`.
- Re-run after DSL changes if hot-reload is not reflected.

## Suggested Team Workflow

1. Update the model in `workspace.dsl`.
2. Generate site locally.
3. Review diagrams and navigation.
4. Commit DSL changes (do not commit generated site unless you explicitly choose a checked-in static-docs strategy).
5. Publish generated site via CI/CD to your static hosting target (GitHub Pages, GitLab Pages, internal artifact hosting, etc.).

## Deployment and Sensitive Data Strategy

- Recommended default: keep high-level deployment architecture in the main repository and publish it with the rest of docs.
- For potentially sensitive deployment details (hostnames, network segments, tenant identifiers, internal endpoints), keep details in a private repository and publish privately.
- Keep the public/main workspace sanitized and reference private detail docs from controlled internal channels.
- This is a policy choice, not a hard restriction: teams can keep deployment docs local if their risk model allows it.
- For generated projects, the same pattern applies: use Structurizr deployment views for baseline architecture, and optionally maintain sensitive overlays in a separate private repo.

## CI/CD Hint

In CI, run `generate-site` and publish the `build/` folder as a static website artifact/deployment.

## CI Jobs Included In This Repository

GitHub Actions:
- Workflow: `.github/workflows/ci.yml`
- Job `structurizr-site` generates and uploads a static site artifact.
- Job `deploy-github-pages` deploys the generated site to GitHub Pages only on SemVer release tags.
- SemVer gate accepts tags like `1.2.3`, `v1.2.3`, and optional pre-release/build metadata.
- Scaffolded projects also receive a starter workflow template at `.github/workflows/architecture-docs.yml`.

GitLab CI:
- Root include: `.gitlab-ci.yml`
- Pipeline definition: `meta-agent/ci/gitlab-ci.yml`
- Job `structurizr-site` generates and stores site artifacts.
- Job `pages` publishes the generated site to GitLab Pages on SemVer tags.

## Notes

- `structurizr-site-generatr` supports including documentation/ADRs in generated site when referenced in your Structurizr workspace.
- Keep architecture model updates small and tied to real system changes to avoid diagram drift.
- If Docker generation fails, verify the container mount and working directory point to the Structurizr model folder.


## Known limitations

See `meta-agent/docs/architecture/internal/4x/41/41.03/index.md` for currently observed `Supported` vs `Limited` behavior and workarounds.

## Compatibility changelog

Record new probe outcomes in `meta-agent/docs/architecture/internal/4x/41/41.03/index.md` under a dated section whenever you validate a previously untested feature.
