#!/usr/bin/env python3
"""Clean and guard generated artifacts in the meta-agent repository."""

from __future__ import annotations

import argparse
import pathlib
import shutil
import subprocess
import sys


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Clean/check generated worktree artifacts")
    parser.add_argument("--apply", action="store_true", help="Delete detected generated artifacts")
    parser.add_argument("--check", action="store_true", help="Fail if generated artifacts exist in the worktree")
    parser.add_argument(
        "--check-tracked",
        action="store_true",
        help="Fail if generated artifact paths are tracked in git",
    )
    parser.add_argument(
        "--include-coverage",
        action="store_true",
        help="Include coverage output directory in clean/check targets",
    )
    return parser.parse_args()


def find_repo_root() -> pathlib.Path:
    return pathlib.Path(__file__).resolve().parents[2]


def generated_dirs(root: pathlib.Path, include_coverage: bool) -> list[pathlib.Path]:
    candidates: set[pathlib.Path] = set()
    meta_agent_root = root / "meta-agent"

    for path in meta_agent_root.rglob("*"):
        if not path.is_dir():
            continue
        if path.name in {"bin", "obj"}:
            candidates.add(path)

    candidates.add(meta_agent_root / "docs" / "architecture" / "structurizr" / "build")
    candidates.add(meta_agent_root / "templates")
    candidates.add(root / "build")

    if include_coverage:
        candidates.add(meta_agent_root / "dotnet" / "coverage")

    return sorted(path for path in candidates if path.exists())


def run_git_ls_files(root: pathlib.Path, pattern: str) -> list[pathlib.Path]:
    result = subprocess.run(
        ["git", "ls-files", pattern],
        cwd=str(root),
        text=True,
        capture_output=True,
        check=False,
    )
    if result.returncode != 0:
        raise RuntimeError(result.stderr.strip() or "git ls-files failed")
    files = [line.strip() for line in result.stdout.splitlines() if line.strip()]
    return [root / file for file in files]


def tracked_generated_files(root: pathlib.Path, include_coverage: bool) -> list[pathlib.Path]:
    patterns = [
        "meta-agent/**/bin/**",
        "meta-agent/**/obj/**",
        "meta-agent/templates/**",
        "meta-agent/docs/architecture/site/build/**",
        "build/**",
    ]
    if include_coverage:
        patterns.append("meta-agent/dotnet/coverage/**")

    files: set[pathlib.Path] = set()
    for pattern in patterns:
        for file_path in run_git_ls_files(root, pattern):
            files.add(file_path)
    return sorted(files)


def remove_paths(paths: list[pathlib.Path]) -> None:
    for path in paths:
        if path.is_dir():
            shutil.rmtree(path)
        elif path.exists():
            path.unlink()


def print_list(title: str, paths: list[pathlib.Path], root: pathlib.Path) -> None:
    if not paths:
        print(f"{title}: none")
        return
    print(f"{title}:")
    for path in paths:
        try:
            relative = path.relative_to(root)
            print(f"  - {relative}")
        except ValueError:
            print(f"  - {path}")


def main() -> int:
    args = parse_args()
    root = find_repo_root()

    if not (args.apply or args.check or args.check_tracked):
        print("No action selected. Use one or more of: --apply, --check, --check-tracked")
        return 2

    should_handle_worktree = args.apply or args.check
    existing_generated = generated_dirs(root, include_coverage=args.include_coverage) if should_handle_worktree else []
    tracked_generated = tracked_generated_files(root, include_coverage=args.include_coverage) if args.check_tracked else []

    if args.apply:
        remove_paths(existing_generated)
        # Recompute after deletion for accurate check output.
        existing_generated = generated_dirs(root, include_coverage=args.include_coverage)

    if should_handle_worktree:
        print_list("Generated artifact directories in worktree", existing_generated, root)
    if args.check_tracked:
        print_list("Tracked generated artifact files", tracked_generated, root)

    if args.check and existing_generated:
        print("Worktree contains generated artifacts.")
        return 1
    if args.check_tracked and tracked_generated:
        print("Repository tracks generated artifact paths. Remove them from git.")
        return 1

    return 0


if __name__ == "__main__":
    sys.exit(main())
