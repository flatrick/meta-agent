#!/usr/bin/env python3
"""Ensure release-facing version markers stay in sync."""

from __future__ import annotations

import argparse
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
        "--tag",
        type=str,
        default=None,
        help="Optional release tag to enforce (e.g. v1.0.0).",
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

    csproj_path = repo_root / "meta-agent" / "dotnet" / "MetaAgent.Cli" / "MetaAgent.Cli.csproj"
    readme_path = repo_root / "meta-agent" / "README.md"
    playbook_path = repo_root / "meta-agent" / "PLAYBOOK.md"
    runbook_path = repo_root / "meta-agent" / "docs" / "operations" / "runbook.md"

    csproj_version = parse_csproj_version(csproj_path)
    expected_version = csproj_version

    if args.tag is not None:
        tag_version = normalize_tag(args.tag.strip())
        expected_version = tag_version

    readme_text = readme_path.read_text(encoding="utf-8")
    playbook_text = playbook_path.read_text(encoding="utf-8")
    runbook_text = runbook_path.read_text(encoding="utf-8")

    values = {
        "README title": extract(r"^# meta-agent — v(?P<version>\d+\.\d+\.\d+)$", readme_text, "README title"),
        "PLAYBOOK intro": extract(
            r"This playbook describes how to use `meta-agent` v(?P<version>\d+\.\d+\.\d+) in a \.NET-centric organisation\.",
            playbook_text,
            "PLAYBOOK intro",
        ),
        "runbook title": extract(
            r"^# meta-agent runbook \(v(?P<version>\d+\.\d+\.\d+) baseline\)$",
            runbook_text,
            "runbook title",
        ),
        "runbook baseline line": extract(
            r"- The v(?P<version>\d+\.\d+\.\d+) baseline defaults to `dotnet` template scaffolding for out-of-box usage\.",
            runbook_text,
            "runbook baseline line",
        ),
    }

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
