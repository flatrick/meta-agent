#!/usr/bin/env python3
"""Validate PKB staging metadata and flag stale artifacts."""

import argparse
import json
import re
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Dict, List

REQUIRED_METADATA_KEYS = (
    "staging_status",
    "staged_at_utc",
    "last_reviewed_at_utc",
    "promotion_target_path",
    "not_promoted_reason",
)

ALLOWED_STATUS = {"staging", "promoted", "archived"}
ISO_UTC_FORMAT = "%Y-%m-%dT%H:%M:%SZ"
METADATA_LINE = re.compile(r"^\s*-\s*`([^`]+)`:\s*`([^`]*)`\s*$", re.MULTILINE)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Validate PKB staging metadata.")
    parser.add_argument("--pkb-root", default="PKB", help="Path to PKB directory")
    parser.add_argument(
        "--max-age-days",
        type=int,
        default=30,
        help="Maximum allowed age for staged artifacts based on last_reviewed_at_utc",
    )
    parser.add_argument(
        "--fail-on-issues",
        action="store_true",
        help="Exit non-zero when issues are found",
    )
    return parser.parse_args()


def parse_iso_utc(value: str) -> datetime:
    parsed = datetime.strptime(value, ISO_UTC_FORMAT)
    return parsed.replace(tzinfo=timezone.utc)


def is_blank_or_placeholder(value: str) -> bool:
    trimmed = value.strip()
    if not trimmed:
        return True

    lowered = trimmed.lower()
    placeholder_literals = {
        "todo",
        "tbd",
        "unknown",
        "replace-me",
        "n/a",
        "na",
        "none",
    }
    if lowered in placeholder_literals:
        return True

    return "todo" in lowered


def normalize_rel(path: Path, root: Path) -> str:
    return path.relative_to(root).as_posix()


def validate_metadata(
    rel_path: str,
    metadata: Dict[str, str],
    max_age_days: int,
    now_utc: datetime,
    issues: List[str],
) -> None:
    for key in REQUIRED_METADATA_KEYS:
        if key not in metadata:
            issues.append(f"{rel_path}: missing metadata key `{key}`")

    if any(key not in metadata for key in REQUIRED_METADATA_KEYS):
        return

    status = metadata["staging_status"].strip().lower()
    staged_at_raw = metadata["staged_at_utc"].strip()
    reviewed_at_raw = metadata["last_reviewed_at_utc"].strip()
    promotion_target = metadata["promotion_target_path"].strip()
    reason = metadata["not_promoted_reason"].strip()

    if status not in ALLOWED_STATUS:
        issues.append(
            f"{rel_path}: invalid staging_status `{metadata['staging_status']}`; expected one of {sorted(ALLOWED_STATUS)}"
        )

    try:
        staged_at = parse_iso_utc(staged_at_raw)
    except ValueError:
        staged_at = None
        issues.append(f"{rel_path}: invalid staged_at_utc `{staged_at_raw}` (expected {ISO_UTC_FORMAT})")

    try:
        reviewed_at = parse_iso_utc(reviewed_at_raw)
    except ValueError:
        reviewed_at = None
        issues.append(
            f"{rel_path}: invalid last_reviewed_at_utc `{reviewed_at_raw}` (expected {ISO_UTC_FORMAT})"
        )

    if is_blank_or_placeholder(promotion_target):
        issues.append(f"{rel_path}: promotion_target_path must be non-empty and non-placeholder")
    elif status != "archived" and not promotion_target.startswith("docs/"):
        issues.append(f"{rel_path}: promotion_target_path must start with `docs/` while not archived")

    if status == "staging":
        if is_blank_or_placeholder(reason):
            issues.append(f"{rel_path}: not_promoted_reason is required while staging")
        if reviewed_at is not None:
            age_days = (now_utc - reviewed_at).total_seconds() / 86400.0
            if age_days > max_age_days:
                issues.append(
                    f"{rel_path}: stale staged artifact (last reviewed {age_days:.1f} days ago; max {max_age_days})"
                )

    if status in {"promoted", "archived"} and is_blank_or_placeholder(reason):
        issues.append(f"{rel_path}: not_promoted_reason must still explain status history")

    if staged_at is not None and reviewed_at is not None and reviewed_at < staged_at:
        issues.append(f"{rel_path}: last_reviewed_at_utc cannot be older than staged_at_utc")


def check_markdown_files(pkb_root: Path, max_age_days: int, now_utc: datetime, issues: List[str]) -> int:
    markdown_files = sorted(pkb_root.rglob("*.md"))
    for file_path in markdown_files:
        content = file_path.read_text(encoding="utf-8")
        metadata = {match.group(1).strip(): match.group(2).strip() for match in METADATA_LINE.finditer(content)}
        validate_metadata(normalize_rel(file_path, pkb_root), metadata, max_age_days, now_utc, issues)
    return len(markdown_files)


def check_index_json(pkb_root: Path, max_age_days: int, now_utc: datetime, issues: List[str]) -> bool:
    index_path = pkb_root / "INDEX" / "AGENT_INDEX.json"
    if not index_path.exists():
        issues.append("INDEX/AGENT_INDEX.json: missing file")
        return False

    try:
        payload = json.loads(index_path.read_text(encoding="utf-8"))
    except json.JSONDecodeError as exc:
        issues.append(f"INDEX/AGENT_INDEX.json: invalid JSON ({exc})")
        return True

    if not isinstance(payload, dict):
        issues.append("INDEX/AGENT_INDEX.json: root must be an object")
        return True

    required_index_keys = set(REQUIRED_METADATA_KEYS).union(
        {"containers", "components", "flows", "commands", "runbooks", "invariants"}
    )
    for key in sorted(required_index_keys):
        if key not in payload:
            issues.append(f"INDEX/AGENT_INDEX.json: missing key `{key}`")

    metadata = {
        key: str(payload.get(key, "")).strip() for key in REQUIRED_METADATA_KEYS
    }
    validate_metadata("INDEX/AGENT_INDEX.json", metadata, max_age_days, now_utc, issues)
    return True


def main() -> int:
    args = parse_args()
    pkb_root = Path(args.pkb_root).resolve()

    if not pkb_root.exists() or not pkb_root.is_dir():
        print(f"PKB staging check failed: PKB root not found: {pkb_root}", file=sys.stderr)
        return 1 if args.fail_on_issues else 0

    issues: List[str] = []
    now_utc = datetime.now(timezone.utc)

    markdown_count = check_markdown_files(pkb_root, args.max_age_days, now_utc, issues)
    index_checked = check_index_json(pkb_root, args.max_age_days, now_utc, issues)

    if issues:
        print(f"PKB staging check found {len(issues)} issue(s):")
        for issue in issues:
            print(f"- {issue}")
        if args.fail_on_issues:
            return 1
        print("PKB staging check did not fail the run (use --fail-on-issues to gate).")
        return 0

    print("PKB staging check passed.")
    print(f"- PKB root: {pkb_root}")
    print(f"- Markdown artifacts checked: {markdown_count}")
    print(f"- Index checked: {index_checked}")
    print(f"- Max staging age (days): {args.max_age_days}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
