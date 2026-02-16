# Template Composition Source

`meta-agent/template-src/` is the source of truth for scaffold template composition.

Composition model:

- `base/`: shared files copied into every template.
- `overlays/<template>/`: template-specific add/replace files.
- `manifest.json`: per-template compose rules (`overlays`, `remove`, `required`).

Compose commands:

- Write composed output to `meta-agent/templates/`:
  - `python3 ./meta-agent/scripts/compose-templates.py`
- Check composed output parity without writing:
  - `python3 ./meta-agent/scripts/compose-templates.py --check`

Rules:

- Edit `template-src/` first.
- Regenerate `meta-agent/templates/` after changes.
- Use `--check` in CI/review to detect drift.
