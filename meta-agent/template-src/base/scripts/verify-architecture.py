#!/usr/bin/env python3
"""Verify Structurizr site generation and PKB staging metadata.

Supports both Podman and Docker as container runtimes.
"""

from __future__ import annotations

import argparse
import os
import pathlib
import shutil
import subprocess
import sys

DOCKER_IMAGE = "ghcr.io/avisi-cloud/structurizr-site-generatr"
REQUIRED_VIEW_KEYS = (
    '"key" : "SystemContext"',
    '"key" : "Containers"',
    '"key" : "LocalDevelopmentDeployment"',
    '"key" : "ProductionDeployment"',
)


def run(command: list[str], cwd: pathlib.Path) -> int:
    completed = subprocess.run(command, cwd=str(cwd), check=False)
    return completed.returncode


def generate_site(model_dir: pathlib.Path, repo_root: pathlib.Path) -> int:
    has_local_binary = shutil.which("structurizr-site-generatr") is not None

    if has_local_binary:
        return run(
            [
                "structurizr-site-generatr",
                "generate-site",
                "--workspace-file",
                "workspace.dsl",
                "--assets-dir",
                "assets",
            ],
            model_dir,
        )

    # Try container runtimes: prefer podman, fall back to docker
    container_runtime = None
    for candidate in ("podman", "docker"):
        if shutil.which(candidate) is not None:
            container_runtime = candidate
            break

    if container_runtime is None:
        print(
            "No container runtime found. Install podman or docker, "
            "or install structurizr-site-generatr locally.",
            file=sys.stderr,
        )
        return 1

    command = [container_runtime, "run", "--rm"]
    if hasattr(os, "getuid") and hasattr(os, "getgid"):
        command.extend(["--user", f"{os.getuid()}:{os.getgid()}"])
    command.extend(
        [
            "-v",
            f"{model_dir}:/var/model",
            "-w",
            "/var/model",
            DOCKER_IMAGE,
            "generate-site",
            "--workspace-file",
            "workspace.dsl",
            "--assets-dir",
            "assets",
        ]
    )
    return run(command, repo_root)


def validate_workspace_json(workspace_json: pathlib.Path) -> list[str]:
    if not workspace_json.exists():
        return [f"missing {workspace_json}"]

    content = workspace_json.read_text(encoding="utf-8")
    missing: list[str] = []
    for key in REQUIRED_VIEW_KEYS:
        if key not in content:
            missing.append(f"expected view key not found: {key}")
    return missing


def run_pkb_staging_check(repo_root: pathlib.Path) -> int:
    pkb_check_script = repo_root / "scripts" / "check-pkb-staging.py"
    if not pkb_check_script.exists():
        return 0

    command = [
        sys.executable,
        str(pkb_check_script),
        "--pkb-root",
        str(repo_root / "PKB"),
        "--max-age-days",
        "30",
        "--fail-on-issues",
    ]
    return run(command, repo_root)


def main() -> int:
    parser = argparse.ArgumentParser(description="Verify Structurizr site and PKB staging.")
    parser.add_argument(
        "--repo-root",
        default=None,
        help="Repository root (default: parent of script directory)",
    )
    args = parser.parse_args()
    repo_root = (
        pathlib.Path(args.repo_root).resolve()
        if args.repo_root
        else pathlib.Path(__file__).resolve().parent.parent
    )
    model_dir = repo_root / "docs" / "architecture" / "site"

    if not model_dir.exists():
        print(f"Verification failed: model directory not found: {model_dir}", file=sys.stderr)
        return 1

    generation_exit = generate_site(model_dir, repo_root)
    if generation_exit != 0:
        return generation_exit

    problems = validate_workspace_json(model_dir / "build" / "site" / "master" / "workspace.json")
    if problems:
        print("Verification failed:", file=sys.stderr)
        for problem in problems:
            print(f"- {problem}", file=sys.stderr)
        return 1

    pkb_exit = run_pkb_staging_check(repo_root)
    if pkb_exit != 0:
        return pkb_exit

    print("Architecture verification passed.")
    print(f"- Model dir: {model_dir}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
