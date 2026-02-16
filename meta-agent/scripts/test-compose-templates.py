#!/usr/bin/env python3
"""Tests for compose-templates.py behavior."""

from __future__ import annotations

import json
import pathlib
import subprocess
import tempfile
import unittest


class ComposeTemplatesTests(unittest.TestCase):
    def test_write_and_check_flow(self) -> None:
        script = pathlib.Path(__file__).resolve().parent / "compose-templates.py"
        with tempfile.TemporaryDirectory(prefix="meta-agent-compose-test-", dir="/tmp") as tmp:
            root = pathlib.Path(tmp)
            source = root / "source"
            output = root / "output"
            source.mkdir(parents=True, exist_ok=True)
            output.mkdir(parents=True, exist_ok=True)

            (source / "base").mkdir(parents=True, exist_ok=True)
            (source / "base" / "common.txt").write_text("base\n", encoding="utf-8")
            (source / "base" / "delete-me.txt").write_text("remove\n", encoding="utf-8")

            overlay_root = source / "overlays" / "sample"
            overlay_root.mkdir(parents=True, exist_ok=True)
            (overlay_root / "common.txt").write_text("overlay\n", encoding="utf-8")
            (overlay_root / "added.txt").write_text("added\n", encoding="utf-8")

            manifest = {
                "version": 1,
                "base": "base",
                "overlayRoot": "overlays",
                "templates": {
                    "sample": {
                        "overlays": ["sample"],
                        "remove": ["delete-me.txt"],
                        "required": ["common.txt", "added.txt"],
                    }
                },
            }
            manifest_path = source / "manifest.json"
            manifest_path.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")

            write_result = subprocess.run(
                [
                    "python3",
                    str(script),
                    "--source-root",
                    str(source),
                    "--output-root",
                    str(output),
                    "--manifest",
                    str(manifest_path),
                ],
                check=False,
                capture_output=True,
                text=True,
            )
            self.assertEqual(0, write_result.returncode, write_result.stderr)

            target = output / "sample"
            self.assertTrue((target / "common.txt").exists())
            self.assertEqual("overlay\n", (target / "common.txt").read_text(encoding="utf-8"))
            self.assertTrue((target / "added.txt").exists())
            self.assertFalse((target / "delete-me.txt").exists())

            check_result = subprocess.run(
                [
                    "python3",
                    str(script),
                    "--source-root",
                    str(source),
                    "--output-root",
                    str(output),
                    "--manifest",
                    str(manifest_path),
                    "--check",
                ],
                check=False,
                capture_output=True,
                text=True,
            )
            self.assertEqual(0, check_result.returncode, check_result.stderr)

            (target / "common.txt").write_text("drift\n", encoding="utf-8")
            drift_result = subprocess.run(
                [
                    "python3",
                    str(script),
                    "--source-root",
                    str(source),
                    "--output-root",
                    str(output),
                    "--manifest",
                    str(manifest_path),
                    "--check",
                ],
                check=False,
                capture_output=True,
                text=True,
            )
            self.assertNotEqual(0, drift_result.returncode)
            self.assertIn("[DRIFT]", drift_result.stdout)

    def test_missing_required_path_fails(self) -> None:
        script = pathlib.Path(__file__).resolve().parent / "compose-templates.py"
        with tempfile.TemporaryDirectory(prefix="meta-agent-compose-test-", dir="/tmp") as tmp:
            root = pathlib.Path(tmp)
            source = root / "source"
            output = root / "output"
            source.mkdir(parents=True, exist_ok=True)
            output.mkdir(parents=True, exist_ok=True)

            (source / "base").mkdir(parents=True, exist_ok=True)
            (source / "base" / "common.txt").write_text("base\n", encoding="utf-8")
            (source / "overlays" / "sample").mkdir(parents=True, exist_ok=True)

            manifest = {
                "version": 1,
                "base": "base",
                "overlayRoot": "overlays",
                "templates": {
                    "sample": {
                        "overlays": ["sample"],
                        "remove": [],
                        "required": ["missing.txt"],
                    }
                },
            }
            manifest_path = source / "manifest.json"
            manifest_path.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")

            result = subprocess.run(
                [
                    "python3",
                    str(script),
                    "--source-root",
                    str(source),
                    "--output-root",
                    str(output),
                    "--manifest",
                    str(manifest_path),
                ],
                check=False,
                capture_output=True,
                text=True,
            )
            self.assertNotEqual(0, result.returncode)
            self.assertIn("missing required paths", result.stderr)


if __name__ == "__main__":
    unittest.main(verbosity=2)
