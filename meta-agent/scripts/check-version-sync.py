#!/usr/bin/env python3
"""Ensure release-facing version markers stay in sync."""

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
    parser = argparse.ArgumentParser(description="Check release version sync across project metadata and docs")
    parser.add_argument(
        "--repo-root",
        type=pathlib.Path,
        default=pathlib.Path(__file__).resolve().parents[2],
        help="Repository root (default: this script's repository root).",
    )
    parser.add_argument(
        "--config",
        type=pathlib.Path,
        default=None,
        help="Path to release marker config JSON (default: meta-agent/config/release-version-markers.json).",
    )
    parser.add_argument(
        "--tag",
        type=str,
        default=None,
        help="Optional release tag to enforce (e.g. v1.0.2).",
    )
    return parser.parse_args()


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


def extract(pattern: str, text: str, label: str) -> str:
    match = re.search(pattern, text, re.MULTILINE)
    if match is None:
        raise ValueError(f"could not locate version marker for {label}")
    return match.group("version")


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


def normalize_tag(tag: str) -> str:
    if not SEMVER_TAG_REGEX.match(tag):
        raise ValueError(f"tag is not semver: {tag}")
    normalized = tag[1:] if tag.startswith("v") else tag
    # Keep this sync check simple and stable: enforce only normal release tags X.Y.Z.
    if not SIMPLE_VERSION_REGEX.match(normalized):
        raise ValueError(f"tag must be a release version (X.Y.Z) for version sync checks: {tag}")
    return normalized


def main() -> int:
    args = parse_args()
    repo_root = args.repo_root.resolve()
    config_path = (args.config.resolve() if args.config else (repo_root / "meta-agent" / "config" / "release-version-markers.json"))

    csproj_path = repo_root / "meta-agent" / "dotnet" / "MetaAgent.Cli" / "MetaAgent.Cli.csproj"

    csproj_version = parse_csproj_version(csproj_path)
    expected_version = csproj_version

    if args.tag is not None:
        tag_version = normalize_tag(args.tag.strip())
        expected_version = tag_version

    values: dict[str, str] = {}
    for marker in load_markers(config_path):
        path = repo_root / marker["path"]
        text = path.read_text(encoding="utf-8")
        values[marker["label"]] = extract(marker["pattern"], text, marker["label"])

    failures: list[str] = []
    if csproj_version != expected_version:
        failures.append(
            f"MetaAgent.Cli.csproj <Version> is '{csproj_version}' but expected '{expected_version}'"
        )

    for label, value in values.items():
        if value != expected_version:
            failures.append(f"{label} is '{value}' but expected '{expected_version}'")

    if failures:
        print("Version sync check failed.")
        for failure in failures:
            print(f"- {failure}")
        return 1

    source = f"tag {args.tag}" if args.tag is not None else "csproj Version"
    print(f"Version sync check passed (expected={expected_version}, source={source}).")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as exc:  # noqa: BLE001
        print(f"version sync check failed: {exc}", file=sys.stderr)
        raise SystemExit(1)
