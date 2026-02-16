#!/usr/bin/env python3
"""Synchronize release-facing version markers across key docs."""

from __future__ import annotations

import argparse
import json
import pathlib
import re
import sys
import xml.etree.ElementTree as ET

SEMVER_TAG_REGEX = re.compile(
    r"^v?(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(-[0-9A-Za-z.-]+)?(\+[0-9A-Za-z.-]+)?$"
)
SIMPLE_VERSION_REGEX = re.compile(r"^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Sync release-facing version markers")
    parser.add_argument(
        "--repo-root",
        type=pathlib.Path,
        default=pathlib.Path(__file__).resolve().parents[2],
        help="Repository root (default: this script's repository root).",
    )
    parser.add_argument(
        "--tag",
        type=str,
        default=None,
        help="Optional release tag to enforce (e.g. v1.0.2).",
    )
    parser.add_argument(
        "--version",
        type=str,
        default=None,
        help="Optional explicit version (X.Y.Z). Overrides --tag and csproj discovery.",
    )
    parser.add_argument(
        "--config",
        type=pathlib.Path,
        default=None,
        help="Path to release marker config JSON (default: meta-agent/config/release-version-markers.json).",
    )
    parser.add_argument(
        "--check",
        action="store_true",
        help="Check only. Exit non-zero if any markers are not synced.",
    )
    parser.add_argument(
        "--apply",
        action="store_true",
        help="Write updates in place.",
    )
    return parser.parse_args()


def normalize_tag(tag: str) -> str:
    if not SEMVER_TAG_REGEX.match(tag):
        raise ValueError(f"tag is not semver: {tag}")
    normalized = tag[1:] if tag.startswith("v") else tag
    if not SIMPLE_VERSION_REGEX.match(normalized):
        raise ValueError(f"tag must be a release version (X.Y.Z): {tag}")
    return normalized


def parse_csproj_version(csproj_path: pathlib.Path) -> str:
    tree = ET.parse(csproj_path)
    root = tree.getroot()
    for node in root.findall(".//Version"):
        if node.text and node.text.strip():
            version = node.text.strip()
            if not SIMPLE_VERSION_REGEX.match(version):
                raise ValueError(f"csproj Version is not simple semver (X.Y.Z): {version}")
            return version
    raise ValueError(f"No <Version> found in {csproj_path}")


def resolve_expected_version(args: argparse.Namespace, repo_root: pathlib.Path) -> str:
    if args.version is not None:
        if not SIMPLE_VERSION_REGEX.match(args.version.strip()):
            raise ValueError(f"--version must be X.Y.Z: {args.version}")
        return args.version.strip()
    if args.tag is not None:
        return normalize_tag(args.tag.strip())
    return parse_csproj_version(repo_root / "meta-agent" / "dotnet" / "MetaAgent.Cli" / "MetaAgent.Cli.csproj")


def load_markers(config_path: pathlib.Path) -> list[dict[str, str]]:
    data = json.loads(config_path.read_text(encoding="utf-8"))
    if not isinstance(data, dict):
        raise ValueError(f"release marker config must be an object: {config_path}")

    markers = data.get("markers")
    if not isinstance(markers, list) or not markers:
        raise ValueError(f"release marker config must contain non-empty 'markers': {config_path}")

    normalized: list[dict[str, str]] = []
    for idx, marker in enumerate(markers):
        if not isinstance(marker, dict):
            raise ValueError(f"marker entry at index {idx} must be an object")
        label = marker.get("label")
        rel_path = marker.get("path")
        pattern = marker.get("pattern")
        if not isinstance(label, str) or not label.strip():
            raise ValueError(f"marker entry at index {idx} has invalid 'label'")
        if not isinstance(rel_path, str) or not rel_path.strip():
            raise ValueError(f"marker entry '{label}' has invalid 'path'")
        if not isinstance(pattern, str) or not pattern.strip():
            raise ValueError(f"marker entry '{label}' has invalid 'pattern'")

        compiled = re.compile(pattern, re.MULTILINE)
        if "version" not in compiled.groupindex:
            raise ValueError(f"marker entry '{label}' pattern must include named group 'version'")

        normalized.append({"label": label, "path": rel_path, "pattern": pattern})
    return normalized


def update_marker(text: str, pattern: str, version: str, label: str) -> tuple[str, bool]:
    compiled = re.compile(pattern, re.MULTILINE)
    match = compiled.search(text)
    if match is None:
        raise ValueError(f"could not locate version marker for {label}")
    current = match.group("version")
    if current == version:
        return text, False
    updated = text[: match.start("version")] + version + text[match.end("version") :]
    return updated, True


def main() -> int:
    args = parse_args()
    if args.check and args.apply:
        print("Choose either --check or --apply, not both.", file=sys.stderr)
        return 2

    mode = "check" if args.check else "apply" if args.apply else "check"
    repo_root = args.repo_root.resolve()
    config_path = (args.config.resolve() if args.config else (repo_root / "meta-agent" / "config" / "release-version-markers.json"))
    expected = resolve_expected_version(args, repo_root)
    targets = load_markers(config_path)

    changed: list[str] = []
    for marker in targets:
        label = marker["label"]
        pattern = marker["pattern"]
        path = repo_root / marker["path"]
        original = path.read_text(encoding="utf-8")
        updated, did_change = update_marker(original, pattern, expected, label)
        if did_change:
            changed.append(f"{label} ({path})")
            if mode == "apply":
                path.write_text(updated, encoding="utf-8")

    if mode == "check":
        if changed:
            print("Version marker sync check failed. Outdated markers:")
            for item in changed:
                print(f"- {item}")
            print(f"Expected version: {expected}")
            return 1
        print(f"Version marker sync check passed (expected={expected}).")
        return 0

    if changed:
        print("Updated version markers:")
        for item in changed:
            print(f"- {item}")
    else:
        print("No version marker updates were needed.")
    print(f"Expected version: {expected}")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as exc:  # noqa: BLE001
        print(f"version marker sync failed: {exc}", file=sys.stderr)
        raise SystemExit(1)
