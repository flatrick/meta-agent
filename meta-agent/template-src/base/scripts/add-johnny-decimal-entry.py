#!/usr/bin/env python3
"""Add Johnny.Decimal entries: area, category, or ID (title + description only). Store a document in an ID folder.

Subcommands:
  add-area        Add a new area (next 1x..9x, or --area 0x for root metadata); requires --title and --description.
  add-category    Add a new category in an area (creates NN.00 metadata folder); requires --area, --title, --description.
  add-id          Add a new content ID in a category (.01, .02, ...; .00 is category metadata); requires --category, --title, --description.
  store-document  Copy a file into an ID folder (e.g. 33.12); requires --id and --file. Safe: copy then remove original.

Output: JSON with success, relative_path, id, name, description, message (and failure_reason on failure).
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

from johnny_decimal.add_entry import (
    add_area,
    add_category,
    add_id,
    store_document,
)
from johnny_decimal.shared import load_roots_from_config

DEFAULT_INTERNAL = "docs/architecture/internal"
DEFAULT_CONFIG_PATH = "scripts/johnny-decimal-config.json"


def repo_root() -> Path:
    return Path(__file__).resolve().parent.parent


def resolve_internal(root: Path, internal_root: str) -> Path:
    p = (root / internal_root).resolve()
    if p.exists():
        return p
    return Path(internal_root).resolve()


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Add Johnny.Decimal entries (area/category/id) or store a document in an ID folder."
    )
    parser.add_argument(
        "--internal-root",
        default=None,
        help="J.D root (required if config has multiple roots)",
    )
    parser.add_argument(
        "--config",
        default=DEFAULT_CONFIG_PATH,
        help="Path to JSON config with 'roots' array",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Do not create files or folders; report what would be done",
    )
    parser.add_argument(
        "--no-json",
        action="store_true",
        help="Print human-readable message instead of JSON",
    )
    sub = parser.add_subparsers(dest="command", required=True)

    p_area = sub.add_parser("add-area", help="Add a new area (next available 1x..9x, or 0x for root metadata)")
    p_area.add_argument("--title", required=True, help="Short title for the area")
    p_area.add_argument("--description", required=True, help="What broad group of data will reside in this tree")
    p_area.add_argument("--area", dest="area_id", default=None, help="Use 0x to create root metadata area (otherwise next 1x..9x)")
    p_area.add_argument("--dry-run", action="store_true", help="Do not create; report what would be done")

    p_cat = sub.add_parser("add-category", help="Add a new category under an area")
    p_cat.add_argument("--area", required=True, help="Area, e.g. 2x")
    p_cat.add_argument("--title", required=True, help="Short title for the category")
    p_cat.add_argument("--description", required=True, help="What the category shall contain")
    p_cat.add_argument("--dry-run", action="store_true", help="Do not create; report what would be done")

    p_id = sub.add_parser("add-id", help="Add a new ID under a category")
    p_id.add_argument("--category", required=True, help="Category, e.g. 22 (area is inferred)")
    p_id.add_argument("--title", required=True, help="Short title for the ID")
    p_id.add_argument("--description", required=True, help="What this ID folder holds")
    p_id.add_argument("--dry-run", action="store_true", help="Do not create; report what would be done")

    p_store = sub.add_parser("store-document", help="Copy a document into an ID folder (safe: copy then remove original)")
    p_store.add_argument("--id", required=True, dest="id_val", metavar="ID", help="ID folder, e.g. 33.12")
    p_store.add_argument("--file", required=True, dest="file_path", help="Path to the document to store")
    p_store.add_argument("--dry-run", action="store_true", help="Do not move; report what would be done")

    args = parser.parse_args()
    root = repo_root()
    internal_root_arg = args.internal_root
    if internal_root_arg is not None:
        internal = (root / internal_root_arg).resolve() if not Path(internal_root_arg).is_absolute() else Path(internal_root_arg).resolve()
    else:
        config_roots = load_roots_from_config(root, args.config)
        if len(config_roots) > 1:
            candidates = [str(p.relative_to(root)).replace("\\", "/") for p in config_roots]
            out = {"success": False, "failure_reason": "multiple_roots", "message": f"Specify --internal-root (one of: {', '.join(candidates)})"}
            print(json.dumps(out))
            return 1
        if len(config_roots) == 1:
            internal = config_roots[0]
        else:
            internal = resolve_internal(root, DEFAULT_INTERNAL)
    if not internal.is_dir():
        out = {"success": False, "failure_reason": "internal_root_not_found", "message": str(internal)}
        print(json.dumps(out))
        return 1

    repo_base = root
    dry_run = getattr(args, "dry_run", False)

    if args.command == "add-area":
        r = add_area(
            internal,
            args.title,
            args.description,
            repo_base=repo_base,
            dry_run=dry_run,
            area_id=getattr(args, "area_id", None),
        )
    elif args.command == "add-category":
        r = add_category(internal, args.area, args.title, args.description, repo_base=repo_base, dry_run=dry_run)
    elif args.command == "add-id":
        r = add_id(internal, args.category, args.title, args.description, repo_base=repo_base, dry_run=dry_run)
    elif args.command == "store-document":
        r = store_document(internal, args.id_val, Path(args.file_path), repo_base=repo_base, dry_run=dry_run)
    else:
        r = type("R", (), {"success": False, "to_dict": lambda: {"success": False, "message": "Unknown command"}})()

    d = r.to_dict() if hasattr(r, "to_dict") else r
    if getattr(args, "no_json", False):
        if d.get("success"):
            print(d.get("message", ""))
            print(f"relative_path={d.get('relative_path')} id={d.get('id')} name={d.get('name')}")
        else:
            msg = d.get("message") or d.get("failure_reason") or "Failed"
            print(msg, file=sys.stderr)
        return 0 if d.get("success") else 1
    print(json.dumps(d))
    return 0 if d.get("success") else 1


if __name__ == "__main__":
    sys.exit(main())
