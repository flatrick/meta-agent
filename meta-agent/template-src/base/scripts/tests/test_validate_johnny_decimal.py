#!/usr/bin/env python3
"""Tests for validate-johnny-decimal.py."""

from __future__ import annotations

import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPTS = REPO_ROOT / "scripts"
VALIDATOR = SCRIPTS / "validate-johnny-decimal.py"


def run_validator(repo_root: Path, extra_args: list[str] | None = None) -> subprocess.CompletedProcess[str]:
    cmd = [sys.executable, str(VALIDATOR), "--root", str(repo_root)]
    if extra_args:
        cmd.extend(extra_args)
    return subprocess.run(cmd, cwd=str(REPO_ROOT), capture_output=True, text=True)


class TestValidateJohnnyDecimal(unittest.TestCase):
    def test_valid_structure_passes(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root / "1x").mkdir()
            (root / "1x" / "10").mkdir()
            (root / "1x" / "10" / "10.00").mkdir()
            (root / "1x" / "11").mkdir()
            (root / "1x" / "11" / "11.01").mkdir()
            result = run_validator(root)
            self.assertEqual(result.returncode, 0, msg=result.stderr)

    def test_valid_structure_with_0x_root_metadata(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root / "0x").mkdir()
            (root / "0x" / "00").mkdir()
            (root / "0x" / "00" / "00.00").mkdir()
            (root / "1x").mkdir()
            (root / "1x" / "10").mkdir()
            (root / "1x" / "10" / "10.00").mkdir()
            result = run_validator(root)
            self.assertEqual(result.returncode, 0, msg=result.stderr)

    def test_invalid_area_reported(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root / "10").mkdir()
            result = run_validator(root)
            out = result.stdout + result.stderr
            self.assertIn("Invalid area", out)

    def test_fail_on_issues_returns_nonzero_when_invalid(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root / "bad").mkdir()
            result = run_validator(root, extra_args=["--fail-on-issues"])
            self.assertNotEqual(result.returncode, 0)

    def test_config_multiple_roots(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root / "tree1" / "1x" / "10").mkdir(parents=True)
            (root / "tree1" / "1x" / "10" / "10.00").mkdir()
            (root / "tree2" / "2x" / "20").mkdir(parents=True)
            (root / "tree2" / "2x" / "20" / "20.00").mkdir()
            config = root / "jconfig.json"
            config.write_text(json.dumps({"roots": ["tree1", "tree2"]}), encoding="utf-8")
            result = subprocess.run(
                [
                    sys.executable,
                    str(VALIDATOR),
                    "--repo-root",
                    str(root),
                    "--config",
                    str(config),
                ],
                cwd=str(REPO_ROOT),
                capture_output=True,
                text=True,
            )
            self.assertEqual(result.returncode, 0, msg=result.stderr)


if __name__ == "__main__":
    unittest.main()
