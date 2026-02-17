#!/usr/bin/env python3
"""Tests for scan-markdown-links.py."""

from __future__ import annotations

import json
import subprocess
import tempfile
import textwrap
import unittest
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
SCANNER = REPO_ROOT / "scripts" / "scan-markdown-links.py"


def run_scanner(
    repo_root: Path,
    fail_on_dead: bool = False,
) -> subprocess.CompletedProcess[str]:
    json_out = repo_root / ".meta-agent-temp" / "report.json"
    md_out = repo_root / ".meta-agent-temp" / "report.md"
    cmd = [
        "python",
        str(SCANNER),
        "--repo-root",
        str(repo_root),
        "--json-out",
        str(json_out),
        "--markdown-out",
        str(md_out),
    ]
    if fail_on_dead:
        cmd.append("--fail-on-dead")
    return subprocess.run(cmd, cwd=str(REPO_ROOT), check=False, capture_output=True, text=True)


class TestScanMarkdownLinks(unittest.TestCase):
    def test_detects_dead_and_alive_links(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            repo = Path(temp_dir)
            docs = repo / "docs"
            docs.mkdir(parents=True, exist_ok=True)

            (docs / "a.md").write_text(
                textwrap.dedent(
                    """\
                    # A Doc

                    - [B File](b.md)
                    - [B Slug](b-page/)
                    - [Missing](missing.md)
                    - [Anchor](#local)
                    - [External](https://example.com)
                    """
                ),
                encoding="utf-8",
            )
            (docs / "b.md").write_text("# B Page\n", encoding="utf-8")

            result = run_scanner(repo)
            self.assertEqual(result.returncode, 0, msg=result.stderr)

            payload = json.loads((repo / ".meta-agent-temp" / "report.json").read_text(encoding="utf-8"))
            summary = payload["summary"]
            self.assertEqual(summary["markdown_files"], 2)
            self.assertEqual(summary["links_total"], 5)
            self.assertEqual(summary["links_local_dead"], 1)
            self.assertEqual(summary["links_local_alive"], 2)
            self.assertEqual(summary["links_anchor"], 1)
            self.assertEqual(summary["links_external"], 1)
            self.assertEqual(summary["link_opportunities_total"], 0)

            dead = payload["dead_links"]
            self.assertEqual(len(dead), 1)
            self.assertEqual(dead[0]["target"], "missing.md")

    def test_fail_on_dead_returns_nonzero(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            repo = Path(temp_dir)
            docs = repo / "docs"
            docs.mkdir(parents=True, exist_ok=True)
            (docs / "a.md").write_text("# A\n\n[Missing](missing.md)\n", encoding="utf-8")

            result = run_scanner(repo, fail_on_dead=True)
            self.assertNotEqual(result.returncode, 0)

    def test_detects_inline_code_link_opportunities(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            repo = Path(temp_dir)
            docs = repo / "docs"
            docs.mkdir(parents=True, exist_ok=True)

            (docs / "a.md").write_text(
                textwrap.dedent(
                    """\
                    # A Doc

                    - `b.md`
                    - `b-page/`
                    """
                ),
                encoding="utf-8",
            )
            (docs / "b.md").write_text("# B Page\n", encoding="utf-8")

            result = run_scanner(repo)
            self.assertEqual(result.returncode, 0, msg=result.stderr)

            payload = json.loads((repo / ".meta-agent-temp" / "report.json").read_text(encoding="utf-8"))
            summary = payload["summary"]
            self.assertEqual(summary["link_opportunities_total"], 2)
            self.assertEqual(summary["link_opportunities_standard_path"], 1)
            self.assertEqual(summary["link_opportunities_structurizr_slug"], 1)

            by_candidate = {item["candidate"]: item for item in payload["link_opportunities"]}
            self.assertIn("b.md", by_candidate)
            self.assertEqual(by_candidate["b.md"]["link_style"], "standard_path")
            self.assertIn("b-page/", by_candidate)
            self.assertEqual(by_candidate["b-page/"]["link_style"], "structurizr_slug")


if __name__ == "__main__":
    unittest.main()
