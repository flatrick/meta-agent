#!/usr/bin/env python3
"""Tests for check-pkb-staging.py."""

from __future__ import annotations

import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPTS = REPO_ROOT / "scripts"
CHECK_PKB = SCRIPTS / "check-pkb-staging.py"


class TestCheckPkbStaging(unittest.TestCase):
    def test_missing_pkb_root_reports_error(self) -> None:
        result = subprocess.run(
            [
                sys.executable,
                str(CHECK_PKB),
                "--pkb-root",
                "/nonexistent/pkb/path",
            ],
            cwd=str(REPO_ROOT),
            capture_output=True,
            text=True,
        )
        self.assertIn("not found", result.stderr)

    def test_empty_pkb_reports_missing_index(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            result = subprocess.run(
                [
                    sys.executable,
                    str(CHECK_PKB),
                    "--pkb-root",
                    tmp,
                ],
                cwd=str(REPO_ROOT),
                capture_output=True,
                text=True,
            )
            self.assertIn("AGENT_INDEX.json", result.stdout + result.stderr)

    def test_fail_on_issues_returns_nonzero_when_issues(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            result = subprocess.run(
                [
                    sys.executable,
                    str(CHECK_PKB),
                    "--pkb-root",
                    tmp,
                    "--fail-on-issues",
                ],
                cwd=str(REPO_ROOT),
                capture_output=True,
                text=True,
            )
            self.assertNotEqual(result.returncode, 0)


if __name__ == "__main__":
    unittest.main()
