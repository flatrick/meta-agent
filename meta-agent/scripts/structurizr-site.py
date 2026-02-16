#!/usr/bin/env python3
"""Run structurizr-site-generatr commands for this repository with safe defaults."""

from __future__ import annotations

import argparse
import os
import pathlib
import subprocess
import sys


DEFAULT_IMAGE = "ghcr.io/avisi-cloud/structurizr-site-generatr"
DEFAULT_MODEL_DIR = "meta-agent/docs/architecture/site"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Structurizr site wrapper for meta-agent")
    sub = parser.add_subparsers(dest="command", required=True)

    def add_common(cmd: argparse.ArgumentParser) -> None:
        cmd.add_argument("--repo-root", default=str(find_repo_root()), help="Repository root path")
        cmd.add_argument("--model-dir", default=DEFAULT_MODEL_DIR, help="Structurizr model directory")
        cmd.add_argument("--image", default=DEFAULT_IMAGE, help="Container image to use")
        cmd.add_argument("--use-local-binary", action="store_true", help="Use local structurizr-site-generatr binary")
        cmd.add_argument("--dry-run", action="store_true", help="Print command and exit without running")

    generate = sub.add_parser("generate", help="Generate static site output into build/site")
    add_common(generate)

    serve = sub.add_parser("serve", help="Serve docs locally (Docker publishes port)")
    add_common(serve)
    serve.add_argument("--port", type=int, default=8080, help="Host port for serve mode")

    return parser.parse_args()


def find_repo_root() -> pathlib.Path:
    return pathlib.Path(__file__).resolve().parents[2]


def run_command(command: list[str], cwd: pathlib.Path, dry_run: bool) -> int:
    print("$ " + " ".join(command))
    if dry_run:
        return 0
    completed = subprocess.run(command, cwd=str(cwd), check=False)
    return completed.returncode


def run_local_binary(model_dir: pathlib.Path, command: str, dry_run: bool) -> int:
    tool_command = "generate-site" if command == "generate" else command
    cmd = [
        "structurizr-site-generatr",
        tool_command,
        "--workspace-file",
        "workspace.dsl",
        "--assets-dir",
        "assets",
    ]
    return run_command(cmd, model_dir, dry_run)


def run_docker(repo_root: pathlib.Path, model_dir: pathlib.Path, command: str, image: str, port: int | None, dry_run: bool) -> int:
    tool_command = "generate-site" if command == "generate" else command
    cmd: list[str] = ["docker", "run", "--rm"]

    if hasattr(os, "getuid") and hasattr(os, "getgid"):
        cmd.extend(["--user", f"{os.getuid()}:{os.getgid()}"])

    cmd.extend(["-v", f"{model_dir}:/var/model"])
    if tool_command == "serve" and port is not None:
        cmd.extend(["-p", f"{port}:8080"])

    cmd.extend(
        [
            "-w",
            "/var/model",
            image,
            tool_command,
            "--workspace-file",
            "workspace.dsl",
            "--assets-dir",
            "assets",
        ]
    )
    return run_command(cmd, repo_root, dry_run)


def main() -> int:
    args = parse_args()
    repo_root = pathlib.Path(args.repo_root).resolve()
    model_dir = (repo_root / args.model_dir).resolve()
    if not model_dir.exists():
        print(f"Model directory not found: {model_dir}", file=sys.stderr)
        return 2

    if args.use_local_binary:
        return run_local_binary(model_dir, args.command, args.dry_run)

    port = args.port if args.command == "serve" else None
    return run_docker(repo_root, model_dir, args.command, args.image, port, args.dry_run)


if __name__ == "__main__":
    raise SystemExit(main())
