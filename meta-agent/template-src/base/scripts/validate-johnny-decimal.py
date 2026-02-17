#!/usr/bin/env python3
"""Validate Johnny.Decimal structure under one or more configured roots.

Configuration file (default: scripts/johnny-decimal-config.json) lists paths to scan:
  {"roots": ["docs/architecture/internal", "other/jd-tree"]}

Rules enforced:
- Area: folders must be named Nx (N = 0..9). 0x is root metadata for the whole tree; 1x–9x are content areas.
- Category: under Nx, folders must be NN with N as first digit (0x -> 00-09, 1x -> 10-19, ...).
  Categories ending in 0 (00, 10, 20, ...) are metadata/rules for that area; content belongs in 01-09, 11-19, etc.
- ID: under Nx/NN, folders must be NN.MM (e.g. 21.01). NN.00 is category metadata; content IDs are .01–.99.

Use this before committing changes so agents and humans don't place content incorrectly.
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path
from typing import List, Tuple

from johnny_decimal.shared import (
    DEFAULT_CONFIG_PATH,
    ID_PATTERN,
    load_roots_from_config,
    validate_area_name,
    validate_category_name,
    validate_id_folder,
)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Validate Johnny.Decimal structure under configured root(s)."
    )
    parser.add_argument(
        "--repo-root",
        default=None,
        help="Repository root for resolving config and relative roots (default: script's parent directory)",
    )
    parser.add_argument(
        "--config",
        default=DEFAULT_CONFIG_PATH,
        help="Path to JSON config with 'roots' array (default: scripts/johnny-decimal-config.json)",
    )
    parser.add_argument(
        "--root",
        action="append",
        dest="roots",
        metavar="PATH",
        help="Override config: validate this path (repeatable). If given, config roots are ignored.",
    )
    parser.add_argument(
        "--fail-on-issues",
        action="store_true",
        help="Exit with non-zero when any validation issue is found",
    )
    parser.add_argument(
        "--warn-metadata-category-content",
        action="store_true",
        default=True,
        help="Warn when a category ending in 0 (metadata) contains ID subfolders other than N0.xx (default: on)",
    )
    return parser.parse_args()


def check_metadata_category_has_only_metadata_ids(cat_num: int, path: Path) -> List[str]:
    warnings: List[str] = []
    for child in path.iterdir():
        if not child.is_dir():
            continue
        m = ID_PATTERN.match(child.name)
        if not m:
            warnings.append(
                f"Category {cat_num} (metadata) contains non-ID folder '{child.name}'; "
                "metadata categories should only contain ID folders (e.g. 10.01, 10.03)."
            )
        elif m.group(1) != str(cat_num).zfill(2):
            warnings.append(
                f"Category {cat_num} (metadata) contains ID folder '{child.name}'; "
                f"IDs under metadata category should be {cat_num}.xx (e.g. {cat_num}.00, {cat_num}.01)."
            )
    return warnings


def validate_tree(
    internal_root: Path, warn_metadata: bool, root_label: str = ""
) -> Tuple[List[str], List[str]]:
    errors: List[str] = []
    warnings: List[str] = []
    prefix = f"[{root_label}] " if root_label else ""

    if not internal_root.is_dir():
        errors.append(f"{prefix}Root does not exist or is not a directory: {internal_root}")
        return errors, warnings

    for item in sorted(internal_root.iterdir()):
        if item.name == "index.md" or not item.is_dir():
            continue
        if not validate_area_name(item.name):
            errors.append(
                f"{prefix}Invalid area name '{item.name}'; must be Nx (N=1..9), e.g. 1x, 2x."
            )
            continue
        area_num = int(item.name[0])
        for cat_item in sorted(item.iterdir()):
            if not cat_item.is_dir():
                continue
            if not validate_category_name(area_num, cat_item.name):
                errors.append(
                    f"{prefix}Invalid category '{cat_item.name}' under {item.name}; "
                    f"must be {area_num}0..{area_num}9 (e.g. {area_num}0, {area_num}1)."
                )
                continue
            cat_num = int(cat_item.name)
            for id_item in sorted(cat_item.iterdir()):
                if not id_item.is_dir():
                    continue
                if not validate_id_folder(cat_item.name, id_item.name):
                    errors.append(
                        f"{prefix}Invalid ID folder '{id_item.name}' under {item.name}/{cat_item.name}; "
                        f"must be {cat_item.name}.MM (e.g. {cat_item.name}.00, {cat_item.name}.01)."
                    )
            if warn_metadata and (cat_num % 10 == 0):
                warnings.extend(
                    check_metadata_category_has_only_metadata_ids(cat_num, cat_item)
                )
    return errors, warnings


def main() -> int:
    args = parse_args()
    repo = (
        Path(args.repo_root).resolve()
        if args.repo_root
        else Path(__file__).resolve().parent.parent
    )
    roots: List[Path] = []
    if args.roots:
        for r in args.roots:
            p = (repo / r).resolve() if not Path(r).is_absolute() else Path(r).resolve()
            roots.append(p)
    else:
        roots = load_roots_from_config(repo, args.config)
        if not roots:
            # Fallback: single default root
            roots = [(repo / "docs/architecture/internal").resolve()]
    all_errors: List[str] = []
    all_warnings: List[str] = []
    for i, root_path in enumerate(roots):
        label = root_path.name or str(root_path)
        if len(roots) > 1:
            label = f"{root_path.relative_to(repo)}".replace("\\", "/")
        errs, warns = validate_tree(
            root_path, args.warn_metadata_category_content, label
        )
        all_errors.extend(errs)
        all_warnings.extend(warns)
    for e in all_errors:
        print(f"[ERROR] {e}")
    for w in all_warnings:
        print(f"[WARN] {w}")
    if all_errors and args.fail_on_issues:
        return 1
    if args.fail_on_issues and (all_errors or all_warnings):
        return 1
    if not all_errors and not all_warnings:
        print("Johnny.Decimal structure is valid.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
