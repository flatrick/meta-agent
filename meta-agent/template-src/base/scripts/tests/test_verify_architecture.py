#!/usr/bin/env python3
"""Tests for verify-architecture.py."""

from __future__ import annotations

import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPTS = REPO_ROOT / "scripts"
VERIFY = SCRIPTS / "verify-architecture.py"


class TestVerifyArchitecture(unittest.TestCase):
    def test_missing_model_dir_fails(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            result = subprocess.run(
                [sys.executable, str(VERIFY), "--repo-root", tmp],
                cwd=str(REPO_ROOT),
                capture_output=True,
                text=True,
            )
            self.assertNotEqual(result.returncode, 0)
            self.assertIn("not found", result.stderr or result.stdout)


if __name__ == "__main__":
    unittest.main()
