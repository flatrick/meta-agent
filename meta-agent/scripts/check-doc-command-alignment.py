#!/usr/bin/env python3
"""Check docs for known CLI command capability drift patterns.

This guard targets high-value claims that frequently drift:
- command artifact emission scope (`configure` parity with init/validate)
- mode fallback semantics (policy default vs hardcoded hybrid)
"""

from __future__ import annotations

import argparse
import pathlib
import re
import sys


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Check docs for command-capability drift")
    parser.add_argument(
        "--repo-root",
        type=pathlib.Path,
        default=pathlib.Path(__file__).resolve().parents[2],
        help="Repository root (defaults to this script's repository).",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    repo_root = args.repo_root.resolve()

    targets = {
        "README": repo_root / "meta-agent" / "README.md",
        "runbook": repo_root / "meta-agent" / "docs" / "operations" / "runbook.md",
    }

    missing: list[str] = []
    violations: list[str] = []

    required_literals = {
        "README": [
            "`init`, `configure`, and `validate` emit a deterministic machine-readable policy decision record",
            "`init`, `configure`, and `validate` emit workflow records",
            "`init`, `configure`, `validate`, and `triage` emit a structured run result",
            "`init`, `configure`, `validate`, and `triage` update a metrics scoreboard",
        ],
        "runbook": [
            "`init`, `configure`, and `validate` evaluate autonomy, budgets, change boundaries, and abort conditions before execution.",
            "`init`, `configure`, and `validate` write a workflow record to `.meta-agent-workflow.json`",
            "`init`, `configure`, `validate`, and `triage` emit `.meta-agent-run-result.json` by default",
            "`init`, `configure`, `validate`, and `triage` update `.meta-agent-metrics.json` by default",
            "otherwise => policy `defaultMode`",
            "| `configure` | yes | yes | yes | yes |",
        ],
    }

    forbidden_patterns = {
        "README": [
            r"`init` and `validate` emit a deterministic machine-readable policy decision record",
            r"`init` and `validate` emit workflow records",
            r"`init`, `validate`, and `triage` emit a structured run result",
            r"`init`, `validate`, and `triage` update a metrics scoreboard",
        ],
        "runbook": [
            r"`init` and `validate` evaluate autonomy, budgets, change boundaries, and abort conditions before execution\.",
            r"`init` and `validate` write a workflow record to `\.meta-agent-workflow\.json`",
            r"`init`, `validate`, and `triage` emit `\.meta-agent-run-result\.json` by default",
            r"`init`, `validate`, and `triage` update `\.meta-agent-metrics\.json` by default",
            r"otherwise => `hybrid`",
        ],
    }

    for label, path in targets.items():
        if not path.exists():
            missing.append(f"{label}: missing file {path}")
            continue

        text = path.read_text(encoding="utf-8")
        for literal in required_literals[label]:
            if literal not in text:
                missing.append(f"{label}: missing expected text: {literal}")
        for pattern in forbidden_patterns[label]:
            if re.search(pattern, text):
                violations.append(f"{label}: contains stale text matching: {pattern}")

    if missing or violations:
        print("Documentation command-alignment check failed.")
        if missing:
            print("Missing expectations:")
            for item in missing:
                print(f"- {item}")
        if violations:
            print("Stale patterns found:")
            for item in violations:
                print(f"- {item}")
        return 1

    print("Documentation command-alignment check passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
