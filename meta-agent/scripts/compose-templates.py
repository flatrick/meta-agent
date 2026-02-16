#!/usr/bin/env python3
"""Compose scaffold templates from shared base + template overlays."""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import pathlib
import shutil
import sys
import tempfile
from dataclasses import dataclass


@dataclass
class TemplateConfig:
    name: str
    overlays: list[str]
    remove: list[str]
    required: list[str]


@dataclass
class CompareResult:
    missing: list[str]
    extra: list[str]
    changed: list[str]

    @property
    def is_match(self) -> bool:
        return not self.missing and not self.extra and not self.changed


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Compose scaffold templates from template-src")
    parser.add_argument(
        "--repo-root",
        type=pathlib.Path,
        default=pathlib.Path(__file__).resolve().parents[2],
        help="Repository root (default: this script's repository root).",
    )
    parser.add_argument(
        "--source-root",
        type=pathlib.Path,
        default=None,
        help="Template source root (default: <repo-root>/meta-agent/template-src).",
    )
    parser.add_argument(
        "--output-root",
        type=pathlib.Path,
        default=None,
        help="Composed template output root (default: <repo-root>/meta-agent/templates).",
    )
    parser.add_argument(
        "--manifest",
        type=pathlib.Path,
        default=None,
        help="Manifest path (default: <source-root>/manifest.json).",
    )
    parser.add_argument(
        "--template",
        action="append",
        default=None,
        help="Template name to compose/check (repeatable). Default: all templates in manifest.",
    )
    parser.add_argument(
        "--check",
        action="store_true",
        help="Check composed output against existing templates without writing.",
    )
    return parser.parse_args()


def parse_safe_relative_path(raw: str, field_name: str) -> pathlib.Path:
    rel = pathlib.Path(raw)
    if rel.is_absolute():
        raise ValueError(f"{field_name} must be relative: {raw}")
    if any(part == ".." for part in rel.parts):
        raise ValueError(f"{field_name} must not traverse parents: {raw}")
    if rel == pathlib.Path("."):
        raise ValueError(f"{field_name} must not be '.': {raw}")
    return rel


def load_manifest(path: pathlib.Path) -> tuple[pathlib.Path, pathlib.Path, dict[str, TemplateConfig]]:
    payload = json.loads(path.read_text(encoding="utf-8"))
    base_value = payload.get("base")
    overlay_root_value = payload.get("overlayRoot")
    templates_payload = payload.get("templates")

    if not isinstance(base_value, str) or not base_value.strip():
        raise ValueError("manifest field 'base' must be a non-empty string")
    if not isinstance(overlay_root_value, str) or not overlay_root_value.strip():
        raise ValueError("manifest field 'overlayRoot' must be a non-empty string")
    if not isinstance(templates_payload, dict) or not templates_payload:
        raise ValueError("manifest field 'templates' must be a non-empty object")

    base_rel = parse_safe_relative_path(base_value.strip(), "base")
    overlay_root_rel = parse_safe_relative_path(overlay_root_value.strip(), "overlayRoot")

    templates: dict[str, TemplateConfig] = {}
    for template_name, raw_config in templates_payload.items():
        if not isinstance(template_name, str) or not template_name.strip():
            raise ValueError("manifest template keys must be non-empty strings")
        if not isinstance(raw_config, dict):
            raise ValueError(f"template '{template_name}': config must be an object")

        overlays_raw = raw_config.get("overlays", [])
        remove_raw = raw_config.get("remove", [])
        required_raw = raw_config.get("required", [])

        if not isinstance(overlays_raw, list) or not all(isinstance(v, str) and v.strip() for v in overlays_raw):
            raise ValueError(f"template '{template_name}': overlays must be a list of non-empty strings")
        if not isinstance(remove_raw, list) or not all(isinstance(v, str) and v.strip() for v in remove_raw):
            raise ValueError(f"template '{template_name}': remove must be a list of non-empty strings")
        if not isinstance(required_raw, list) or not all(isinstance(v, str) and v.strip() for v in required_raw):
            raise ValueError(f"template '{template_name}': required must be a list of non-empty strings")

        overlays = [parse_safe_relative_path(v.strip(), f"{template_name}.overlays").as_posix() for v in overlays_raw]
        remove = [parse_safe_relative_path(v.strip(), f"{template_name}.remove").as_posix() for v in remove_raw]
        required = [parse_safe_relative_path(v.strip(), f"{template_name}.required").as_posix() for v in required_raw]

        templates[template_name.strip()] = TemplateConfig(
            name=template_name.strip(),
            overlays=overlays,
            remove=remove,
            required=required,
        )

    return base_rel, overlay_root_rel, templates


def ensure_directory(path: pathlib.Path, label: str) -> None:
    if not path.exists() or not path.is_dir():
        raise FileNotFoundError(f"{label} not found: {path}")


def copy_tree(src: pathlib.Path, dst: pathlib.Path) -> None:
    if not src.exists():
        raise FileNotFoundError(f"source path not found: {src}")
    shutil.copytree(src, dst, dirs_exist_ok=True)


def remove_path(path: pathlib.Path) -> None:
    if not path.exists():
        return
    if path.is_dir():
        shutil.rmtree(path)
    else:
        path.unlink()


def hash_file(path: pathlib.Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def collect_entries(root: pathlib.Path) -> dict[str, tuple[str, str | None]]:
    entries: dict[str, tuple[str, str | None]] = {}
    if not root.exists():
        return entries

    for dirpath, dirnames, filenames in os.walk(root):
        dirnames.sort()
        filenames.sort()
        current = pathlib.Path(dirpath)
        rel_dir = current.relative_to(root).as_posix()
        if rel_dir != ".":
            entries[rel_dir] = ("dir", None)
        for filename in filenames:
            path = current / filename
            rel = path.relative_to(root).as_posix()
            entries[rel] = ("file", hash_file(path))
    return entries


def compare_directories(expected: pathlib.Path, actual: pathlib.Path) -> CompareResult:
    expected_entries = collect_entries(expected)
    actual_entries = collect_entries(actual)

    expected_keys = set(expected_entries.keys())
    actual_keys = set(actual_entries.keys())

    missing = sorted(expected_keys - actual_keys)
    extra = sorted(actual_keys - expected_keys)
    changed = sorted(
        key for key in (expected_keys & actual_keys) if expected_entries[key] != actual_entries[key]
    )
    return CompareResult(missing=missing, extra=extra, changed=changed)


def compose_template(
    template: TemplateConfig,
    base_dir: pathlib.Path,
    overlay_root: pathlib.Path,
    composed_dir: pathlib.Path,
) -> None:
    copy_tree(base_dir, composed_dir)

    for overlay_name in template.overlays:
        overlay_path = overlay_root / overlay_name
        ensure_directory(overlay_path, f"overlay directory for template '{template.name}'")
        copy_tree(overlay_path, composed_dir)

    for rel in template.remove:
        target = composed_dir / rel
        remove_path(target)

    missing_required: list[str] = []
    for rel in template.required:
        target = composed_dir / rel
        if not target.exists():
            missing_required.append(rel)
    if missing_required:
        joined = ", ".join(missing_required)
        raise RuntimeError(f"template '{template.name}' missing required paths after compose: {joined}")


def main() -> int:
    args = parse_args()
    repo_root = args.repo_root.resolve()

    source_root = (args.source_root or (repo_root / "meta-agent" / "template-src")).resolve()
    manifest_path = (args.manifest or (source_root / "manifest.json")).resolve()
    output_root = (args.output_root or (repo_root / "meta-agent" / "templates")).resolve()

    ensure_directory(source_root, "template source root")
    if not manifest_path.exists():
        raise FileNotFoundError(f"manifest not found: {manifest_path}")

    base_rel, overlay_root_rel, templates = load_manifest(manifest_path)
    base_dir = source_root / base_rel
    overlay_root = source_root / overlay_root_rel

    ensure_directory(base_dir, "base template directory")
    ensure_directory(overlay_root, "overlay root directory")

    selected_names = args.template if args.template else sorted(templates.keys())
    unknown = [name for name in selected_names if name not in templates]
    if unknown:
        raise ValueError(f"unknown template(s): {', '.join(sorted(unknown))}")

    had_mismatch = False
    with tempfile.TemporaryDirectory(prefix="meta-agent-template-compose-", dir="/tmp") as temp_dir:
        temp_root = pathlib.Path(temp_dir)
        for name in selected_names:
            template = templates[name]
            staged_dir = temp_root / name
            staged_dir.mkdir(parents=True, exist_ok=True)
            compose_template(template, base_dir, overlay_root, staged_dir)

            target_dir = output_root / name
            if args.check:
                diff = compare_directories(staged_dir, target_dir)
                if diff.is_match:
                    print(f"[OK] template '{name}' matches composed output")
                    continue

                had_mismatch = True
                print(f"[DRIFT] template '{name}' differs from composed output")
                for label, values in (
                    ("missing", diff.missing),
                    ("extra", diff.extra),
                    ("changed", diff.changed),
                ):
                    if values:
                        preview = values[:20]
                        print(f"  {label} ({len(values)}):")
                        for item in preview:
                            print(f"    - {item}")
                        if len(values) > len(preview):
                            print(f"    - ... ({len(values) - len(preview)} more)")
                continue

            target_dir.parent.mkdir(parents=True, exist_ok=True)
            if target_dir.exists():
                shutil.rmtree(target_dir)
            shutil.copytree(staged_dir, target_dir)
            print(f"[WRITE] composed template '{name}' -> {target_dir}")

    if args.check and had_mismatch:
        return 1
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as exc:  # noqa: BLE001
        print(f"template composition failed: {exc}", file=sys.stderr)
        raise SystemExit(1)
