#!/usr/bin/env python3
"""Tests for DOC_DELTA management helper script."""

from __future__ import annotations

import importlib.util
import pathlib
import subprocess
import sys
import tempfile
import unittest

SCRIPT_PATH = pathlib.Path(__file__).resolve().parent / "manage-doc-delta.py"


def run_script(*args: str) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        ["python3", str(SCRIPT_PATH), *args],
        check=False,
        capture_output=True,
        text=True,
    )


def load_module():
    spec = importlib.util.spec_from_file_location("manage_doc_delta", SCRIPT_PATH)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"Unable to load module from {SCRIPT_PATH}")
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


def build_doc_delta(entries: list[str]) -> str:
    preamble = """# DOC_DELTA Update Contract

Use this file as a strict chronological log of substantive repository changes.
"""
    sections = [preamble.strip()]
    sections.extend(entry.strip() for entry in entries)
    return "\n\n".join(sections).rstrip() + "\n"


class ManageDocDeltaTests(unittest.TestCase):
    def test_check_passes_when_entries_are_sorted(self):
        with tempfile.TemporaryDirectory() as tmp:
            doc_path = pathlib.Path(tmp) / "DOC_DELTA.md"
            doc_path.write_text(
                build_doc_delta(
                    [
                        "## 2026-02-14 20:40:00Z - First\n\n- A.",
                        "## 2026-02-14 20:50:00Z - Second\n\n- B.",
                    ]
                ),
                encoding="utf-8",
            )
            result = run_script("--doc-delta", str(doc_path), "check")
            self.assertEqual(0, result.returncode)
            self.assertIn("check passed", result.stdout)

    def test_check_fails_when_entries_are_out_of_order(self):
        with tempfile.TemporaryDirectory() as tmp:
            doc_path = pathlib.Path(tmp) / "DOC_DELTA.md"
            doc_path.write_text(
                build_doc_delta(
                    [
                        "## 2026-02-14 20:50:00Z - Second\n\n- B.",
                        "## 2026-02-14 20:40:00Z - First\n\n- A.",
                    ]
                ),
                encoding="utf-8",
            )
            result = run_script("--doc-delta", str(doc_path), "check")
            self.assertEqual(1, result.returncode)
            self.assertIn("order check failed", result.stderr)

    def test_fix_sorts_out_of_order_entries(self):
        with tempfile.TemporaryDirectory() as tmp:
            doc_path = pathlib.Path(tmp) / "DOC_DELTA.md"
            doc_path.write_text(
                build_doc_delta(
                    [
                        "## 2026-02-14 20:50:00Z - Second\n\n- B.",
                        "## 2026-02-14 20:40:00Z - First\n\n- A.",
                    ]
                ),
                encoding="utf-8",
            )
            result = run_script("--doc-delta", str(doc_path), "fix")
            self.assertEqual(0, result.returncode)
            self.assertIn("normalized", result.stdout)

            normalized = doc_path.read_text(encoding="utf-8")
            self.assertLess(
                normalized.index("## 2026-02-14 20:40:00Z - First"),
                normalized.index("## 2026-02-14 20:50:00Z - Second"),
            )

    def test_add_supports_change_and_verification_bullets(self):
        with tempfile.TemporaryDirectory() as tmp:
            doc_path = pathlib.Path(tmp) / "DOC_DELTA.md"
            doc_path.write_text(
                build_doc_delta(["## 2026-02-14 20:40:00Z - First\n\n- A."]),
                encoding="utf-8",
            )
            result = run_script(
                "--doc-delta",
                str(doc_path),
                "add",
                "--title",
                "New Entry",
                "--timestamp",
                "2026-02-14 20:45:00Z",
                "--change",
                "Added helper.",
                "--verification",
                "`python3 ... check` passed.",
            )
            self.assertEqual(0, result.returncode)

            updated = doc_path.read_text(encoding="utf-8")
            self.assertIn("## 2026-02-14 20:45:00Z - New Entry", updated)
            self.assertIn("- Added helper.", updated)
            self.assertIn("- Verification:", updated)
            self.assertIn("- `python3 ... check` passed.", updated)

            check_result = run_script("--doc-delta", str(doc_path), "check")
            self.assertEqual(0, check_result.returncode)

    def test_add_supports_body_file(self):
        with tempfile.TemporaryDirectory() as tmp:
            doc_path = pathlib.Path(tmp) / "DOC_DELTA.md"
            body_path = pathlib.Path(tmp) / "body.md"
            doc_path.write_text(
                build_doc_delta(["## 2026-02-14 20:40:00Z - First\n\n- A."]),
                encoding="utf-8",
            )
            body_path.write_text("- line one\n- line two\n", encoding="utf-8")

            result = run_script(
                "--doc-delta",
                str(doc_path),
                "add",
                "--title",
                "Body File Entry",
                "--timestamp",
                "2026-02-14 20:41:00Z",
                "--body-file",
                str(body_path),
            )
            self.assertEqual(0, result.returncode)

            updated = doc_path.read_text(encoding="utf-8")
            self.assertIn("## 2026-02-14 20:41:00Z - Body File Entry", updated)
            self.assertIn("- line one", updated)
            self.assertIn("- line two", updated)

    def test_lock_timeout_when_locked_by_another_agent(self):
        mod = load_module()
        with tempfile.TemporaryDirectory() as tmp:
            doc_path = pathlib.Path(tmp) / "DOC_DELTA.md"
            doc_path.write_text(
                build_doc_delta(["## 2026-02-14 20:40:00Z - First\n\n- A."]),
                encoding="utf-8",
            )
            with mod.acquire_doc_delta_lock(
                doc_delta_path=doc_path,
                timeout_seconds=5.0,
                poll_interval_seconds=0.05,
                operation="test-holder",
            ):
                result = run_script(
                    "--doc-delta",
                    str(doc_path),
                    "--lock-timeout-seconds",
                    "0.20",
                    "--lock-poll-interval-ms",
                    "50",
                    "check",
                )

            self.assertEqual(1, result.returncode)
            self.assertIn("Timed out waiting for DOC_DELTA lock", result.stderr)
            self.assertIn("Current holder metadata", result.stderr)

    def test_three_concurrent_writers_all_succeed_under_locking(self):
        mod = load_module()
        with tempfile.TemporaryDirectory() as tmp:
            doc_path = pathlib.Path(tmp) / "DOC_DELTA.md"
            doc_path.write_text(
                build_doc_delta(["## 2026-02-14 20:40:00Z - First\n\n- A."]),
                encoding="utf-8",
            )

            writers = [
                ("Writer One", "2026-02-14 20:41:00Z"),
                ("Writer Two", "2026-02-14 20:42:00Z"),
                ("Writer Three", "2026-02-14 20:43:00Z"),
            ]

            with mod.acquire_doc_delta_lock(
                doc_delta_path=doc_path,
                timeout_seconds=5.0,
                poll_interval_seconds=0.05,
                operation="test-launch-gate",
            ):
                processes: list[subprocess.Popen[str]] = []
                try:
                    for title, timestamp in writers:
                        processes.append(
                            subprocess.Popen(
                                [
                                    "python3",
                                    str(SCRIPT_PATH),
                                    "--doc-delta",
                                    str(doc_path),
                                    "--lock-timeout-seconds",
                                    "5.0",
                                    "--lock-poll-interval-ms",
                                    "50",
                                    "add",
                                    "--title",
                                    title,
                                    "--timestamp",
                                    timestamp,
                                    "--change",
                                    f"{title} change.",
                                ],
                                stdout=subprocess.PIPE,
                                stderr=subprocess.PIPE,
                                text=True,
                            )
                        )
                finally:
                    # lock is released when context exits, allowing queued writers to proceed
                    pass

            for proc in processes:
                stdout, stderr = proc.communicate(timeout=10.0)
                self.assertEqual(0, proc.returncode, msg=f"stdout={stdout}\nstderr={stderr}")

            updated = doc_path.read_text(encoding="utf-8")
            for title, _ in writers:
                self.assertIn(title, updated)

            check_result = run_script("--doc-delta", str(doc_path), "check")
            self.assertEqual(0, check_result.returncode, msg=check_result.stderr)


if __name__ == "__main__":
    unittest.main(verbosity=2)
