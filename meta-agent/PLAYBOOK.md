# meta-agent Playbook — .NET-first usage

This playbook describes how to use `meta-agent` v1.0.1 in a .NET-centric organisation.

Allowed runtimes: C#/.NET 10, Node/TypeScript, and PowerShell for product-code templates; Python 3 for the provided optional helper scripts on Windows, Linux, and macOS.
End-user scaffolding runs through the .NET CLI and does not require Python unless the provided helper scripts are used.

Quick manual flows

- Scaffold a .NET service:
  - `dotnet run --project ./meta-agent/dotnet/MetaAgent.Cli -- init --template dotnet --target ../my-dotnet-service --name my-service`
  - `cd ../my-dotnet-service && dotnet build && dotnet run`

- Adopt an existing repository without scaffolding:
  - `dotnet run --project ./meta-agent/dotnet/MetaAgent.Cli -- configure --repo ../existing-service --requested-autonomy A1 --tokens-requested 100 --tickets-requested 1 --open-prs 0`
  - `dotnet run --project ./meta-agent/dotnet/MetaAgent.Cli -- validate --policy ../existing-service/.meta-agent-policy.json`
  - For `.NET Framework` repositories specifically, this is the primary path (legacy maintenance onboarding). Do not use `init` to scaffold new Framework projects.

- Validate repository policy:
  - `dotnet run --project ./meta-agent/dotnet/MetaAgent.Cli -- validate --policy .meta-agent-policy.json`

Policy tips for .NET shops

- Use `--template dotnet` on `init` when you want explicit .NET scaffold selection.
- Add `changeBoundaries.allowedPaths` to restrict what automated agents can touch (e.g., `src/**`, `tests/**`).

Developer UX

- The CLI defaults to dotnet templates but templates are pluggable — add/edit template sources under `meta-agent/template-src/`, then compose output into `meta-agent/templates/`.
- The .NET CLI scaffolder can render from template assets without invoking Python at runtime; Python compose/check scripts are for meta-agent development and release hygiene.
- In `interactive_ide` mode, the workflow is plan-first: create plan, get developer approval, document plan, then execute.
- Record architecture-affecting intent in `meta-agent/docs/architecture/site/adrs/`.
- Use `DOC_DELTA.md` only for release-operational notes that are not architecture decisions; when used, add via `python3 ./meta-agent/scripts/manage-doc-delta.py add ...` and validate with `python3 ./meta-agent/scripts/manage-doc-delta.py check`.

Published documentation boundaries

- Structurizr-published docs for this repository must be written under `meta-agent/docs/architecture/site/_docs/`.
- Element-level docs intended for publication must be written under `meta-agent/docs/architecture/site/model/**/_docs/`.
- Use `meta-agent/docs/architecture/internal/4x/41/41.04/index.md` as the authoritative placement/content rules for agents.
- Keep non-published internal notes outside `meta-agent/docs/architecture/site/` to avoid accidental site exposure.

Repository hygiene

- Use `meta-agent/PROJECT_MAP.md` for deterministic path selection before editing.
- For repository-wide Markdown link/backlink checks, agents must run `python3 ./meta-agent/scripts/scan-markdown-links.py` instead of manually scanning `.md` files.
- For gating and CI-style checks, use `python3 ./meta-agent/scripts/scan-markdown-links.py --fail-on-dead`.
- Compose scaffold templates from source (`meta-agent/template-src/`): `python3 ./meta-agent/scripts/compose-templates.py`.
- Enforce template-source parity before completion/release: `python3 ./meta-agent/scripts/compose-templates.py --check`.
- Treat `meta-agent/templates/` as generated output only (ignored in git); do not treat it as source of truth.
- Use scanner outputs (`.meta-agent-temp/markdown-link-report.json` and `.meta-agent-temp/markdown-link-report.md`) to inspect dead links, backlinks, and possible link/search forms.
- Run `python3 ./meta-agent/scripts/clean-worktree.py --check-tracked` before release/tagging to ensure generated artifacts are not tracked.
- Run `python3 ./meta-agent/scripts/clean-worktree.py --apply --include-coverage` after local runs when worktree noise accumulates.
- Never run multiple test/coverage/script invocations in parallel when they share output paths (`bin/`, `obj/`, `coverage/`, shared artifact files).
- If parallel runs are needed, clone/copy into separate temporary worktrees and use distinct output paths per run.
