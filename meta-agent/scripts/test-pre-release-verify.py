#!/usr/bin/env python3
"""Unit tests for SemVer gate behavior in pre-release verification script."""

from __future__ import annotations

import importlib.util
import json
import os
import pathlib
import subprocess
import tempfile
import unittest
from unittest import mock


def load_module():
    root = pathlib.Path(__file__).resolve().parent
    script = root / "pre-release-verify.py"
    spec = importlib.util.spec_from_file_location("pre_release_verify", script)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"Unable to load module from {script}")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class SemverTagTests(unittest.TestCase):
    def test_accepts_valid_semver_tags(self):
        mod = load_module()
        self.assertTrue(mod.is_semver_tag("1.2.3"))
        self.assertTrue(mod.is_semver_tag("v1.2.3"))
        self.assertTrue(mod.is_semver_tag("1.2.3-rc.1"))
        self.assertTrue(mod.is_semver_tag("v1.2.3+build.9"))

    def test_rejects_invalid_semver_tags(self):
        mod = load_module()
        self.assertFalse(mod.is_semver_tag("1.2"))
        self.assertFalse(mod.is_semver_tag("v1"))
        self.assertFalse(mod.is_semver_tag("release-1.2.3"))
        self.assertFalse(mod.is_semver_tag("1.2.3.4"))

    def test_resolve_tag_value_prefers_explicit_argument(self):
        mod = load_module()
        with mock.patch.dict(os.environ, {"GITHUB_REF": "refs/tags/v2.0.0", "CI_COMMIT_TAG": "v3.0.0"}, clear=True):
            tag, source = mod.resolve_tag_value("v1.2.3")
        self.assertEqual("v1.2.3", tag)
        self.assertEqual("argument", source)

    def test_resolve_tag_value_uses_github_ref_then_gitlab_tag(self):
        mod = load_module()
        with mock.patch.dict(os.environ, {"GITHUB_REF": "refs/tags/v2.0.0"}, clear=True):
            tag, source = mod.resolve_tag_value(None)
            self.assertEqual("v2.0.0", tag)
            self.assertEqual("github_ref", source)

        with mock.patch.dict(os.environ, {"CI_COMMIT_TAG": "v3.0.0"}, clear=True):
            tag, source = mod.resolve_tag_value(None)
            self.assertEqual("v3.0.0", tag)
            self.assertEqual("ci_commit_tag", source)

    def test_invalid_tag_writes_summary_and_returns_exit_code_2(self):
        script = pathlib.Path(__file__).resolve().parent / "pre-release-verify.py"
        with tempfile.TemporaryDirectory() as tmp:
            summary_path = pathlib.Path(tmp) / "summary.json"
            result = subprocess.run(
                [
                    "python3",
                    str(script),
                    "--tag",
                    "not-semver",
                    "--skip-clean-apply",
                    "--summary-out",
                    str(summary_path),
                ],
                check=False,
                capture_output=True,
                text=True,
            )
            self.assertEqual(2, result.returncode)
            self.assertTrue(summary_path.exists())
            summary = json.loads(summary_path.read_text(encoding="utf-8"))
            self.assertFalse(summary["success"])
            self.assertEqual(2, summary["exitCode"])
            self.assertEqual("not-semver", summary["tagValidation"]["tag"])
            self.assertFalse(summary["tagValidation"]["isSemVer"])

    def test_build_steps_includes_template_composition_checks(self):
        mod = load_module()
        steps = mod.build_steps(skip_clean_apply=True, tag=None)
        self.assertIn(["python3", "meta-agent/scripts/test-sync-version-markers.py"], steps)
        self.assertIn(["python3", "meta-agent/scripts/sync-version-markers.py", "--check"], steps)
        self.assertIn(["python3", "meta-agent/scripts/check-version-sync.py"], steps)
        self.assertIn(["python3", "meta-agent/scripts/test-compose-templates.py"], steps)
        self.assertIn(["python3", "meta-agent/scripts/compose-templates.py"], steps)
        self.assertIn(["python3", "meta-agent/scripts/compose-templates.py", "--check"], steps)
        self.assertIn(["python3", "meta-agent/scripts/test-manage-doc-delta.py"], steps)
        self.assertIn(["python3", "meta-agent/scripts/manage-doc-delta.py", "check"], steps)

    def test_build_steps_includes_version_sync_tag_when_present(self):
        mod = load_module()
        steps = mod.build_steps(skip_clean_apply=True, tag="v1.2.3")
        self.assertIn(["python3", "meta-agent/scripts/sync-version-markers.py", "--check", "--tag", "v1.2.3"], steps)
        self.assertIn(["python3", "meta-agent/scripts/check-version-sync.py", "--tag", "v1.2.3"], steps)

    def test_build_steps_respects_skip_clean_apply(self):
        mod = load_module()
        with_clean_apply = mod.build_steps(skip_clean_apply=False, tag=None)
        self.assertIn(
            ["python3", "meta-agent/scripts/clean-worktree.py", "--apply", "--include-coverage"],
            with_clean_apply,
        )
        self.assertIn(["python3", "meta-agent/scripts/clean-worktree.py", "--check"], with_clean_apply)

        without_clean_apply = mod.build_steps(skip_clean_apply=True, tag=None)
        self.assertNotIn(
            ["python3", "meta-agent/scripts/clean-worktree.py", "--apply", "--include-coverage"],
            without_clean_apply,
        )
        self.assertNotIn(["python3", "meta-agent/scripts/clean-worktree.py", "--check"], without_clean_apply)


if __name__ == "__main__":
    unittest.main(verbosity=2)
