#!/usr/bin/env python3
"""Run the pre-release verification checklist sequentially."""

from __future__ import annotations

import argparse
import json
import os
import pathlib
import re
import subprocess
import sys
import time
from datetime import datetime, timezone

SEMVER_TAG_REGEX = re.compile(
    r"^v?(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(-[0-9A-Za-z.-]+)?(\+[0-9A-Za-z.-]+)?$"
)
DEFAULT_SUMMARY_RELATIVE_PATH = ".meta-agent-temp/pre-release-verification/latest-summary.json"


def utc_now_iso() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def run(
    command: list[str],
    cwd: pathlib.Path,
    env_overrides: dict[str, str] | None = None,
) -> dict[str, object]:
    printable = " ".join(command)
    print(f"$ {printable}")
    started_at = utc_now_iso()
    started_perf = time.perf_counter()
    environment = os.environ.copy()
    if env_overrides:
        environment.update(env_overrides)

    result = subprocess.run(command, cwd=str(cwd), env=environment, check=False)
    finished_at = utc_now_iso()
    duration_seconds = round(time.perf_counter() - started_perf, 3)
    return {
        "command": command,
        "display": printable,
        "envOverrides": env_overrides or {},
        "exitCode": result.returncode,
        "startedAtUtc": started_at,
        "finishedAtUtc": finished_at,
        "durationSeconds": duration_seconds,
    }


def write_summary(path: pathlib.Path, summary: dict[str, object]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Run pre-release verification checks")
    parser.add_argument(
        "--skip-clean-apply",
        action="store_true",
        help="Skip cleanup apply step (useful if you need to keep generated coverage artifacts in CI).",
    )
    parser.add_argument(
        "--tag",
        type=str,
        default=None,
        help="Optional release tag to validate against SemVer gate (supports optional v prefix).",
    )
    parser.add_argument(
        "--summary-out",
        type=str,
        default=DEFAULT_SUMMARY_RELATIVE_PATH,
        help=f"Write machine-readable run summary JSON to this path (default: {DEFAULT_SUMMARY_RELATIVE_PATH}).",
    )
    return parser.parse_args()


def is_semver_tag(tag: str) -> bool:
    return bool(SEMVER_TAG_REGEX.match(tag))


def resolve_tag_value(explicit_tag: str | None) -> tuple[str | None, str]:
    if explicit_tag is not None:
        return explicit_tag, "argument"

    github_ref = os.environ.get("GITHUB_REF", "").strip()
    if github_ref.startswith("refs/tags/"):
        return github_ref.removeprefix("refs/tags/"), "github_ref"

    ci_commit_tag = os.environ.get("CI_COMMIT_TAG", "").strip()
    if ci_commit_tag:
        return ci_commit_tag, "ci_commit_tag"

    return None, "none"


def build_steps(skip_clean_apply: bool, tag: str | None) -> list[list[str]]:
    version_sync_step = ["python3", "meta-agent/scripts/check-version-sync.py"]
    if tag is not None:
        version_sync_step.extend(["--tag", tag])

    steps: list[list[str]] = [
        ["python3", "meta-agent/scripts/test-pre-release-verify.py"],
        version_sync_step,
        ["python3", "meta-agent/scripts/test-package-release.py"],
        ["python3", "meta-agent/scripts/test-compose-templates.py"],
        ["python3", "meta-agent/scripts/compose-templates.py"],
        ["python3", "meta-agent/scripts/compose-templates.py", "--check"],
        ["python3", "meta-agent/scripts/test-structurizr-site-wrappers.py"],
        ["python3", "meta-agent/scripts/test-manage-doc-delta.py"],
        ["python3", "meta-agent/scripts/manage-doc-delta.py", "check"],
        ["python3", "meta-agent/scripts/check-doc-command-alignment.py"],
        ["python3", "meta-agent/scripts/structurizr-site.py", "generate", "--dry-run"],
        ["python3", "meta-agent/scripts/structurizr-site.py", "serve", "--port", "8080", "--dry-run"],
        ["dotnet", "test", "./meta-agent/dotnet/MetaAgent.slnx", "-v", "minimal"],
        ["python3", "meta-agent/scripts/test-with-coverage.py"],
        ["python3", "meta-agent/scripts/clean-worktree.py", "--check-tracked"],
    ]

    if not skip_clean_apply:
        steps.append(["python3", "meta-agent/scripts/clean-worktree.py", "--apply", "--include-coverage"])
        steps.append(["python3", "meta-agent/scripts/clean-worktree.py", "--check"])

    steps.append(["python3", "meta-agent/scripts/clean-worktree.py", "--check-tracked"])
    return steps


def main() -> int:
    args = parse_args()
    repo_root = pathlib.Path(__file__).resolve().parents[2]
    resolved_tag, tag_source = resolve_tag_value(args.tag)
    summary_path = pathlib.Path(args.summary_out)
    if not summary_path.is_absolute():
        summary_path = (repo_root / summary_path).resolve()

    summary: dict[str, object] = {
        "startedAtUtc": utc_now_iso(),
        "finishedAtUtc": None,
        "success": False,
        "exitCode": None,
        "repoRoot": str(repo_root),
        "tagValidation": {
            "provided": resolved_tag is not None,
            "tag": resolved_tag,
            "source": tag_source,
            "isSemVer": None,
        },
        "skipCleanApply": args.skip_clean_apply,
        "steps": [],
        "failedStep": None,
    }

    if resolved_tag is not None:
        if not is_semver_tag(resolved_tag):
            print(
                f"Tag '{resolved_tag}' is not SemVer (expected: 1.2.3, v1.2.3, optional pre-release/build metadata).",
                file=sys.stderr,
            )
            summary["finishedAtUtc"] = utc_now_iso()
            summary["success"] = False
            summary["exitCode"] = 2
            tag_validation = dict(summary["tagValidation"])
            tag_validation["isSemVer"] = False
            summary["tagValidation"] = tag_validation
            write_summary(summary_path, summary)
            return 2
        tag_validation = dict(summary["tagValidation"])
        tag_validation["isSemVer"] = True
        summary["tagValidation"] = tag_validation
        print(f"SemVer tag validated: {resolved_tag}")

    steps = build_steps(args.skip_clean_apply, resolved_tag)

    for step in steps:
        env_overrides: dict[str, str] | None = None
        if len(step) >= 2 and step[0] == "dotnet" and step[1] == "test":
            env_overrides = {"META_AGENT_NONINTERACTIVE": "1"}

        step_result = run(step, repo_root, env_overrides=env_overrides)
        summary_steps = list(summary["steps"])  # type: ignore[arg-type]
        summary_steps.append(step_result)
        summary["steps"] = summary_steps
        if int(step_result["exitCode"]) != 0:
            print(f"Command failed ({step_result['exitCode']}): {step_result['display']}", file=sys.stderr)
            summary["failedStep"] = step_result
            summary["finishedAtUtc"] = utc_now_iso()
            summary["success"] = False
            summary["exitCode"] = 1
            write_summary(summary_path, summary)
            return 1

    print("Pre-release verification completed successfully.")
    summary["finishedAtUtc"] = utc_now_iso()
    summary["success"] = True
    summary["exitCode"] = 0
    write_summary(summary_path, summary)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
