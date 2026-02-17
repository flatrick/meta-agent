# Scripts

Python-based helpers for this repo: Johnny.Decimal validation and add-entry, Markdown link scanning, PKB staging checks, and architecture verification.

## Layout

- **CLI entry points** (run from repo root, e.g. `python scripts/validate-johnny-decimal.py`):
  - `validate-johnny-decimal.py` — validate J.D structure (areas, categories, IDs)
  - `add-johnny-decimal-entry.py` — add area/category/id or store a document in an ID folder
  - `scan-markdown-links.py` — report Markdown links (alive/dead) and link opportunities
  - `check-pkb-staging.py` — validate PKB staging metadata and staleness
  - `verify-architecture.py` — Structurizr site generation + PKB checks

- **`johnny_decimal/`** — shared package used by the J.D validator and add-entry script (patterns, config, validation, add-entry logic).

- **`tests/`** — tests for the above scripts. Run from this directory: `pytest`.

- **Config** — `johnny-decimal-config.json` (J.D roots), `pyproject.toml` (pytest/coverage), `requirements-dev.txt` (pytest, pytest-cov).

## Usage (from repo root)

```bash
# Validate Johnny.Decimal tree
python scripts/validate-johnny-decimal.py --fail-on-issues

# Add a new area (title + description; tool picks next 1x..9x)
python scripts/add-johnny-decimal-entry.py add-area --title "My area" --description "What lives here" [--dry-run]

# Scan Markdown links
python scripts/scan-markdown-links.py [--fail-on-dead]
```

Full J.D and link-scanner usage: see `docs/architecture/internal/index.md` in the repo root.

## Tests and dependencies

Install dev dependencies (from anywhere):

```bash
pip install -r scripts/requirements-dev.txt
```

Run tests **from this directory** (with coverage, per `pyproject.toml`):

```bash
cd scripts
pytest
```

## Standalone / submodule

This folder is self-contained (all Python tooling and config live here). It **may** be split out into its own git repository and added to the parent repo as a **submodule**. If so, clone or update the parent repo with `git submodule update --init scripts` (or equivalent) so that `scripts/` is populated from the separate repo.
