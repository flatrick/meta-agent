# Internal Knowledge Index (Johnny.Decimal)

## Active Areas

- [1x Development](1x/index.md)
- [3x Projects And Planning](3x/index.md)
- [4x Documentation And Communication](4x/index.md)

No other area is materialized yet.

## Capacity Model

Johnny.Decimal allows areas `0x` (root metadata) and `1x`–`9x` (content). `0x` is optional and holds metadata for the entire tree; content lives in `1x`–`9x`.

Rules:

- Only materialize an area when it has real content.
- Do not create placeholder-only trees.
- Keep area numbers stable once introduced.
- Keep area purpose explicit in its `Nx/index.md`.

## JD Bucket Map (0x + 1x–9x)

## Maturity Baseline

For scaffolded projects, the baseline for high team-maturity docs is:

- `1x`: architecture and engineering execution.
- `3x`: planning and roadmap evolution.
- `4x`: documentation standards and publishing communication.

## Notes (metadata hierarchy)

- **Root:** `0x` is metadata for the entire Johnny.Decimal tree (optional).
  - Create with **add-area --area 0x**; it gets category `00` as its metadata bucket.
- **Area:** Under each area (`0x`, `1x`–`9x`), `N0` (e.g. 00, 10, 20) is the metadata bucket for that area; `N1`–`N9` are content categories.
  - **add-area** creates the `N0` category automatically.
- **Category:** Within a category, `NN.00` is the metadata bucket for that category; `NN.01`–`NN.99` are content IDs.
  - **add-category** creates `NN.00` and its index automatically; **add-id** assigns the next content ID (`.01`, `.02`, …).
- Keep Structurizr published docs outside this internal tree.

## When Adding A New Area

1. Add `Nx/index.md` only when there is actual content to add in that area.
2. Add at least one **content** category (`Nx/N1`–`N9/index.md`) with substantive content. (If you use **add-area**, it creates the `N0` metadata category for you; use **add-category** to create N1–N9.)
3. Define the area purpose in `Nx/index.md` and keep it narrow.
4. Update this root index to link the new area (the add-area script does this automatically).
5. Keep placeholder-only trees out of the repository.

## Global Rules

- Use `Ax/NN/NN.MM/index.md` grouping for non-published docs (`1x/11/11.01/index.md`, etc.).
- Keep publishable Structurizr docs under `docs/architecture/site/_docs/` and `docs/architecture/site/model/**/_docs/`.

## Johnny.Decimal scripts (for agents and humans)

To avoid placing content in the wrong folder (e.g. using a metadata category for content, or wrong ID format), use the repo scripts.

**Interface:** The entry points are the two CLI scripts in `scripts/`: `validate-johnny-decimal.py` and `add-johnny-decimal-entry.py`. 
Shared logic (patterns, config loading, validation, add-entry operations) lives in the **`scripts/johnny_decimal/`** package and is used by both; you only need to run the CLI scripts.

**Config:** `scripts/johnny-decimal-config.json` lists one or more J.D roots to validate (`{"roots": ["docs/architecture/internal"]}`). 
The validator and add-entry script both use this; if there are multiple roots, add-entry requires `--internal-root` (one of the listed paths).

1. **Validate** the internal tree before committing:
   ```bash
   python scripts/validate-johnny-decimal.py [--config scripts/johnny-decimal-config.json] [--root PATH ...] --fail-on-issues
   ```
   Checks: area names `Nx` (N=0..9; 0x = root metadata, 1x–9x = content); category names `N0`–`N9` under each area (N0 = metadata, N1–N9 = content); ID folders `NN.MM` (NN.00 = category metadata, .01–.99 = content). Use `--root` to validate specific paths (overrides config); use `--repo-root` when running from a different repo root (e.g. tests).

2. **Add entries** in a format-compliant way (title + description only; tool picks next area/category/ID and updates indexes):
   ```bash
   python scripts/add-johnny-decimal-entry.py add-area --title "Short title" --description "What broad group of data will reside in this tree" [--dry-run]
   python scripts/add-johnny-decimal-entry.py add-area --area 0x --title "Root metadata" --description "Metadata for the entire J.D tree" [--dry-run]
   python scripts/add-johnny-decimal-entry.py add-category --area 2x --title "Short title" --description "What the category shall contain" [--dry-run]
   python scripts/add-johnny-decimal-entry.py add-id --category 22 --title "Short title" --description "What this ID folder holds" [--dry-run]
   python scripts/add-johnny-decimal-entry.py store-document --id 33.12 --file path/to/document.md [--dry-run]
   ```
   Output is JSON by default: `success`, `relative_path`, `id`, `name`, `description`, `message` (and `failure_reason` on failure). Use `--no-json` for a short human-readable message instead. **add-area** (no `--area`) creates the next content area (1x–9x) and its **N0** metadata category. **add-area --area 0x** creates the root metadata area and its **00** category. **add-category** creates the next content category (N1–N9) and its **NN.00** metadata folder. **add-id** creates the next content ID (**.01**, .02, …; .00 is reserved for category metadata). The tool **adds the new entry to the root/area/category index** as appropriate. `store-document` copies the file into the ID folder (copy, verify, then remove original). If the config has multiple J.D roots, pass `--internal-root` (one of the config roots).

3. **Markdown link scanner** (links and slugs):
   ```bash
   python scripts/scan-markdown-links.py [--fail-on-dead]
   ```
   Produces a JSON and Markdown report of all Markdown links (alive/dead), backlinks, and “link opportunities” (e.g. inline paths that could be turned into links). Slug-style links (e.g. `some-page/`) are resolved from the first H1 of docs; this matches Structurizr usage under `docs/architecture/site`. Dead links under the site tree may be site-generated URLs—interpret with care.

**Tests:** All scripts under `scripts/` have tests in `scripts/tests/`. Run with **code coverage** from the scripts directory: `cd scripts && pytest` (coverage is enabled in `scripts/pyproject.toml`). Requires `pytest` and `pytest-cov` (see `scripts/requirements-dev.txt`; install with `pip install -r scripts/requirements-dev.txt`). Fallback without coverage: `python -m unittest discover -s scripts/tests -p "test_*.py" -v` (from repo root).
