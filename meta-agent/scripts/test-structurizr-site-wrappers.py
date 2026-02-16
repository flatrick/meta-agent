#!/usr/bin/env python3
"""Lightweight tests for Structurizr wrapper scripts.

Run from repository root:
  python3 ./meta-agent/scripts/test-structurizr-site-wrappers.py
"""

from __future__ import annotations

import pathlib
import subprocess
import sys
import unittest


REPO_ROOT = pathlib.Path(__file__).resolve().parents[2]
PY_WRAPPER = REPO_ROOT / "meta-agent" / "scripts" / "structurizr-site.py"


def run(args: list[str]) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        args,
        cwd=str(REPO_ROOT),
        check=False,
        capture_output=True,
        text=True,
    )


class StructurizrWrapperTests(unittest.TestCase):
    def test_python_invalid_model_dir_fails(self) -> None:
        result = run(
            [
                "python3",
                str(PY_WRAPPER),
                "generate",
                "--model-dir",
                "meta-agent/docs/architecture/site-not-found",
            ]
        )
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("Model directory not found", result.stderr)

    def test_python_serve_port_override_in_dry_run(self) -> None:
        result = run(
            [
                "python3",
                str(PY_WRAPPER),
                "serve",
                "--port",
                "9091",
                "--dry-run",
            ]
        )
        self.assertEqual(result.returncode, 0, msg=result.stderr)
        self.assertIn("-p 9091:8080", result.stdout)

    def test_python_generate_maps_to_generate_site_in_local_mode(self) -> None:
        result = run(
            [
                "python3",
                str(PY_WRAPPER),
                "generate",
                "--use-local-binary",
                "--dry-run",
            ]
        )
        self.assertEqual(result.returncode, 0, msg=result.stderr)
        self.assertIn("structurizr-site-generatr generate-site", result.stdout)


if __name__ == "__main__":
    suite = unittest.defaultTestLoader.loadTestsFromTestCase(StructurizrWrapperTests)
    result = unittest.TextTestRunner(verbosity=2).run(suite)
    sys.exit(0 if result.wasSuccessful() else 1)
