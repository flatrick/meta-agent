#!/usr/bin/env python3
"""Tests for add-johnny-decimal-entry (CLI and library)."""

from __future__ import annotations

import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPTS = REPO_ROOT / "scripts"
ADD_ENTRY = SCRIPTS / "add-johnny-decimal-entry.py"

# Import from johnny_decimal package (scripts/ on path so johnny_decimal is found)
sys.path.insert(0, str(SCRIPTS))
from johnny_decimal.add_entry import (
    add_area,
    add_category,
    add_id,
    store_document,
)
from johnny_decimal.shared import load_roots_from_config


class TestAddJohnnyDecimalEntryLib(unittest.TestCase):
    """Tests that call the library directly (for coverage)."""

    def test_add_area_dry_run(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            internal = Path(tmp) / "jd"
            internal.mkdir()
            (internal / "index.md").write_text("## Active Areas\n\n- [1x Development](1x/index.md)\n\n## Other\n")
            (internal / "1x").mkdir()
            r = add_area(internal, "Test Area", "Description of data", repo_base=Path(tmp), dry_run=True)
            self.assertTrue(r.success)
            self.assertEqual(r.id, "2x")
            self.assertIn("dry-run", r.message)

    def test_add_area_creates_and_updates_index(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            internal = Path(tmp) / "jd"
            internal.mkdir()
            (internal / "index.md").write_text(
                "# Root\n\n## Active Areas\n\n- [1x First](1x/index.md)\n\n## Capacity\n"
            )
            (internal / "1x").mkdir()
            (internal / "1x" / "10").mkdir()
            (internal / "1x" / "10" / "index.md").write_text("# 10\n")
            (internal / "1x" / "index.md").write_text("# 1x\n\n## Categories\n\n- [10 Meta](10/index.md)\n")
            r = add_area(internal, "Second", "Second area data", repo_base=Path(tmp), dry_run=False)
            self.assertTrue(r.success)
            self.assertEqual(r.id, "2x")
            self.assertTrue((internal / "2x").is_dir())
            self.assertTrue((internal / "2x" / "index.md").exists())
            self.assertTrue((internal / "2x" / "20").is_dir())
            root_index = (internal / "index.md").read_text()
            self.assertIn("2x Second", root_index)
            self.assertIn("](2x/index.md)", root_index)

    def test_add_category_requires_area(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            internal = Path(tmp) / "jd"
            internal.mkdir()
            (internal / "2x").mkdir()
            (internal / "2x" / "index.md").write_text("# 2x\n\n## Categories\n\n- [20 Meta](20/index.md)\n")
            (internal / "2x" / "20").mkdir()
            (internal / "2x" / "20" / "index.md").write_text("# 20\n")
            r = add_category(internal, "2x", "New Cat", "Content here", repo_base=Path(tmp), dry_run=False)
            self.assertTrue(r.success)
            self.assertEqual(r.id, "21")
            self.assertTrue((internal / "2x" / "21").is_dir())
            self.assertTrue((internal / "2x" / "21" / "21.00").is_dir(), "add_category creates NN.00 metadata folder")
            self.assertTrue((internal / "2x" / "21" / "21.00" / "index.md").exists())
            cat_index = (internal / "2x" / "21" / "index.md").read_text()
            self.assertIn("21.00", cat_index)
            area_index = (internal / "2x" / "index.md").read_text()
            self.assertIn("21 New Cat", area_index)

    def test_add_id_infers_area(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            internal = Path(tmp) / "jd"
            internal.mkdir()
            (internal / "2x").mkdir()
            (internal / "2x" / "21").mkdir()
            (internal / "2x" / "21" / "index.md").write_text("# 21\n\n## IDs in this category\n\n")
            r = add_id(internal, "21", "My ID", "Docs here", repo_base=Path(tmp), dry_run=False)
            self.assertTrue(r.success)
            self.assertEqual(r.id, "21.01", "NN.00 is reserved for category metadata; first content ID is .01")
            self.assertTrue((internal / "2x" / "21" / "21.01").is_dir())
            cat_index = (internal / "2x" / "21" / "index.md").read_text()
            self.assertIn("21.01", cat_index)

    def test_store_document_copy_then_remove(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            internal = Path(tmp) / "jd"
            internal.mkdir()
            (internal / "2x").mkdir()
            (internal / "2x" / "21").mkdir()
            (internal / "2x" / "21" / "21.00").mkdir()
            doc = Path(tmp) / "note.md"
            doc.write_text("# Note\n")
            r = store_document(internal, "21.00", doc, repo_base=Path(tmp), dry_run=False)
            self.assertTrue(r.success)
            self.assertTrue((internal / "2x" / "21" / "21.00" / "note.md").exists())
            self.assertFalse(doc.exists())

    def test_store_document_fails_when_id_missing(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            internal = Path(tmp) / "jd"
            internal.mkdir()
            (internal / "2x").mkdir()
            (internal / "2x" / "21").mkdir()
            doc = Path(tmp) / "note.md"
            doc.write_text("# Note\n")
            r = store_document(internal, "21.99", doc, repo_base=Path(tmp), dry_run=False)
            self.assertFalse(r.success)
            self.assertIn("id_missing", r.failure_reason)

    def test_add_area_root_metadata_0x(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            internal = Path(tmp) / "jd"
            internal.mkdir()
            (internal / "index.md").write_text("# Root\n\n## Active Areas\n\n## Other\n")
            r = add_area(
                internal,
                "Root metadata",
                "Metadata and rules for the entire Johnny.Decimal tree",
                repo_base=Path(tmp),
                dry_run=False,
                area_id="0x",
            )
            self.assertTrue(r.success)
            self.assertEqual(r.id, "0x")
            self.assertTrue((internal / "0x").is_dir())
            self.assertTrue((internal / "0x" / "00").is_dir(), "0x gets 00 as metadata category")
            self.assertTrue((internal / "0x" / "00" / "index.md").exists())
            root_index = (internal / "index.md").read_text()
            self.assertIn("0x Root metadata", root_index)

    def test_load_roots_from_config(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root / "config.json").write_text('{"roots": ["a", "b"]}')
            roots = load_roots_from_config(root, "config.json")
            self.assertEqual(len(roots), 2)
            self.assertEqual(roots[0], (root / "a").resolve())
            self.assertEqual(roots[1], (root / "b").resolve())


class TestAddJohnnyDecimalEntryCLI(unittest.TestCase):
    """Tests that run the CLI (subprocess)."""

    def test_add_area_returns_json(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            internal = Path(tmp) / "jd"
            internal.mkdir()
            (internal / "index.md").write_text("## Active Areas\n\n- [1x A](1x/index.md)\n\n## X\n")
            (internal / "1x").mkdir()
            result = subprocess.run(
                [
                    sys.executable,
                    str(ADD_ENTRY),
                    "--internal-root",
                    str(internal),
                    "add-area",
                    "--title",
                    "New",
                    "--description",
                    "Data here",
                    "--dry-run",
                ],
                cwd=str(REPO_ROOT),
                capture_output=True,
                text=True,
            )
            self.assertEqual(result.returncode, 0, msg=result.stderr)
            out = json.loads(result.stdout)
            self.assertTrue(out["success"])
            self.assertEqual(out["id"], "2x")

    def test_add_id_requires_title_and_description(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            internal = Path(tmp) / "jd"
            internal.mkdir()
            (internal / "2x").mkdir()
            (internal / "2x" / "21").mkdir()
            (internal / "2x" / "21" / "index.md").write_text("# 21\n\n## IDs in this category\n\n")
            result = subprocess.run(
                [
                    sys.executable,
                    str(ADD_ENTRY),
                    "--internal-root",
                    str(internal),
                    "add-id",
                    "--category",
                    "21",
                    "--title",
                    "T",
                    "--description",
                    "D",
                ],
                cwd=str(REPO_ROOT),
                capture_output=True,
                text=True,
            )
            self.assertEqual(result.returncode, 0)
            out = json.loads(result.stdout)
            self.assertTrue(out["success"])
            self.assertEqual(out["id"], "21.01", "NN.00 reserved for category metadata")

    def test_multiple_roots_requires_internal_root(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root / "jconfig.json").write_text(json.dumps({"roots": ["docs/a", "docs/b"]}))
            (root / "docs" / "a").mkdir(parents=True)
            (root / "docs" / "b").mkdir(parents=True)
            result = subprocess.run(
                [
                    sys.executable,
                    str(ADD_ENTRY),
                    "--config",
                    str(root / "jconfig.json"),
                    "add-area",
                    "--title",
                    "X",
                    "--description",
                    "Y",
                ],
                cwd=str(root),
                capture_output=True,
                text=True,
            )
            self.assertNotEqual(result.returncode, 0)
            out = json.loads(result.stdout)
            self.assertFalse(out["success"])
            self.assertIn("internal-root", out.get("message", "").lower())


if __name__ == "__main__":
    unittest.main()
