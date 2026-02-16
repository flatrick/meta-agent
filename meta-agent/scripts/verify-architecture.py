#!/usr/bin/env python3
"""Verify Structurizr site generation and required architecture elements."""

from __future__ import annotations

import argparse
import os
import pathlib
import shutil
import subprocess
import sys

DEFAULT_DOCKER_IMAGE = "ghcr.io/avisi-cloud/structurizr-site-generatr"
REQUIRED_VIEW_KEYS = (
    '"key" : "SystemContext"',
    '"key" : "Containers"',
    '"key" : "LocalDevelopmentDeployment"',
    '"key" : "CiDeployment"',
)
REQUIRED_ELEMENT_NAMES = (
    '"name" : "meta-agent"',
    '"name" : "MetaAgent.Cli"',
    '"name" : "MetaAgent.Core"',
)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Verify Structurizr architecture artifacts")
    parser.add_argument(
        "--repo-root",
        default=str(pathlib.Path(__file__).resolve().parents[2]),
        help="Repository root path",
    )
    parser.add_argument(
        "--model-dir",
        default="meta-agent/docs/architecture/site",
        help="Structurizr model directory relative to --repo-root",
    )
    parser.add_argument(
        "--docker-image",
        default=DEFAULT_DOCKER_IMAGE,
        help="Docker image used when local structurizr-site-generatr is unavailable",
    )
    parser.add_argument(
        "--use-local-binary",
        action="store_true",
        help="Require local structurizr-site-generatr instead of docker fallback",
    )
    return parser.parse_args()


def run(command: list[str], cwd: pathlib.Path) -> int:
    completed = subprocess.run(command, cwd=str(cwd), check=False)
    return completed.returncode


def generate_site(model_dir: pathlib.Path, repo_root: pathlib.Path, docker_image: str, use_local_binary: bool) -> int:
    has_local_binary = shutil.which("structurizr-site-generatr") is not None

    if use_local_binary or has_local_binary:
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

    command = ["docker", "run", "--rm"]
    if hasattr(os, "getuid") and hasattr(os, "getgid"):
        command.extend(["--user", f"{os.getuid()}:{os.getgid()}"])
    command.extend(
        [
            "-v",
            f"{model_dir}:/var/model",
            "-w",
            "/var/model",
            docker_image,
            "generate-site",
            "--workspace-file",
            "workspace.dsl",
            "--assets-dir",
            "assets",
        ]
    )
    return run(command, repo_root)


def require_literals(content: str, required_values: tuple[str, ...], label: str) -> list[str]:
    missing: list[str] = []
    for value in required_values:
        if value not in content:
            missing.append(f"expected {label} not found: {value}")
    return missing


def main() -> int:
    args = parse_args()
    repo_root = pathlib.Path(args.repo_root).resolve()
    model_dir = (repo_root / args.model_dir).resolve()

    if not model_dir.exists():
        print(f"Verification failed: model directory not found: {model_dir}", file=sys.stderr)
        return 1

    exit_code = generate_site(model_dir, repo_root, args.docker_image, args.use_local_binary)
    if exit_code != 0:
        return exit_code

    workspace_json = model_dir / "build" / "site" / "master" / "workspace.json"
    if not workspace_json.exists():
        print(f"Verification failed: missing {workspace_json}", file=sys.stderr)
        return 1

    content = workspace_json.read_text(encoding="utf-8")
    problems = []
    problems.extend(require_literals(content, REQUIRED_VIEW_KEYS, "view key"))
    problems.extend(require_literals(content, REQUIRED_ELEMENT_NAMES, "element"))

    if problems:
        print("Verification failed:", file=sys.stderr)
        for problem in problems:
            print(f"- {problem}", file=sys.stderr)
        return 1

    print("Architecture verification passed.")
    print(f"- Model dir: {model_dir}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
