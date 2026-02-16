#!/usr/bin/env python3
"""Unit tests for sync-version-markers script."""

from __future__ import annotations

import pathlib
import subprocess
import tempfile
import textwrap
import unittest


SCRIPT_PATH = pathlib.Path(__file__).resolve().parent / "sync-version-markers.py"


def create_repo_fixture(root: pathlib.Path, version: str, drift: bool) -> None:
    (root / "meta-agent" / "dotnet" / "MetaAgent.Cli").mkdir(parents=True, exist_ok=True)
    (root / "meta-agent" / "docs" / "operations").mkdir(parents=True, exist_ok=True)
    (root / "meta-agent" / "config").mkdir(parents=True, exist_ok=True)

    csproj = textwrap.dedent(
        f"""\
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
            <Version>{version}</Version>
          </PropertyGroup>
        </Project>
        """
    )
    (root / "meta-agent" / "dotnet" / "MetaAgent.Cli" / "MetaAgent.Cli.csproj").write_text(csproj, encoding="utf-8")

    readme_version = "0.0.0" if drift else version
    playbook_version = "0.0.0" if drift else version
    runbook_title_version = "0.0.0" if drift else version
    runbook_baseline_version = "0.0.0" if drift else version

    (root / "meta-agent" / "README.md").write_text(
        f"# meta-agent — v{readme_version}\n",
        encoding="utf-8",
    )
    (root / "meta-agent" / "PLAYBOOK.md").write_text(
        f"This playbook describes how to use `meta-agent` v{playbook_version} in a .NET-centric organisation.\n",
        encoding="utf-8",
    )
    (root / "meta-agent" / "docs" / "operations" / "runbook.md").write_text(
        textwrap.dedent(
            f"""\
            # meta-agent runbook (v{runbook_title_version} baseline)

            - The v{runbook_baseline_version} baseline defaults to `dotnet` template scaffolding for out-of-box usage.
            """
        ),
        encoding="utf-8",
    )

    (root / "meta-agent" / "config" / "release-version-markers.json").write_text(
        textwrap.dedent(
            """\
            {
              "version": 1,
              "markers": [
                {
                  "label": "README title",
                  "path": "meta-agent/README.md",
                  "pattern": "^# meta-agent \\u2014 v(?P<version>\\\\d+\\\\.\\\\d+\\\\.\\\\d+)$"
                },
                {
                  "label": "PLAYBOOK intro",
                  "path": "meta-agent/PLAYBOOK.md",
                  "pattern": "This playbook describes how to use `meta-agent` v(?P<version>\\\\d+\\\\.\\\\d+\\\\.\\\\d+) in a \\\\.NET-centric organisation\\\\."
                },
                {
                  "label": "runbook title",
                  "path": "meta-agent/docs/operations/runbook.md",
                  "pattern": "^# meta-agent runbook \\\\(v(?P<version>\\\\d+\\\\.\\\\d+\\\\.\\\\d+) baseline\\\\)$"
                },
                {
                  "label": "runbook baseline line",
                  "path": "meta-agent/docs/operations/runbook.md",
                  "pattern": "- The v(?P<version>\\\\d+\\\\.\\\\d+\\\\.\\\\d+) baseline defaults to `dotnet` template scaffolding for out-of-box usage\\\\."
                }
              ]
            }
            """
        ),
        encoding="utf-8",
    )


class SyncVersionMarkersTests(unittest.TestCase):
    def test_check_fails_when_markers_are_outdated(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = pathlib.Path(tmp)
            create_repo_fixture(root, version="1.2.3", drift=True)
            result = subprocess.run(
                ["python3", str(SCRIPT_PATH), "--repo-root", str(root), "--check"],
                check=False,
                capture_output=True,
                text=True,
            )
            self.assertEqual(1, result.returncode)
            self.assertIn("Outdated markers", result.stdout)

    def test_apply_updates_markers_and_check_passes(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = pathlib.Path(tmp)
            create_repo_fixture(root, version="1.2.3", drift=True)

            apply_result = subprocess.run(
                ["python3", str(SCRIPT_PATH), "--repo-root", str(root), "--apply"],
                check=False,
                capture_output=True,
                text=True,
            )
            self.assertEqual(0, apply_result.returncode)

            check_result = subprocess.run(
                ["python3", str(SCRIPT_PATH), "--repo-root", str(root), "--check"],
                check=False,
                capture_output=True,
                text=True,
            )
            self.assertEqual(0, check_result.returncode)
            self.assertIn("passed", check_result.stdout)

    def test_tag_sets_expected_version(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = pathlib.Path(tmp)
            create_repo_fixture(root, version="1.2.3", drift=False)
            result = subprocess.run(
                ["python3", str(SCRIPT_PATH), "--repo-root", str(root), "--tag", "v9.8.7", "--check"],
                check=False,
                capture_output=True,
                text=True,
            )
            self.assertEqual(1, result.returncode)
            self.assertIn("Expected version: 9.8.7", result.stdout)


if __name__ == "__main__":
    unittest.main(verbosity=2)
