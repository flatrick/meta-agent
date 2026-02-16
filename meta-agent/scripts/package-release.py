#!/usr/bin/env python3
"""Build a Windows release zip with CLI executable and editable assets."""

from __future__ import annotations

import argparse
import hashlib
import pathlib
import shutil
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET

DEFAULT_RUNTIMES = ["win-x64", "linux-x64", "osx-arm64", "osx-x64"]
DEFAULT_CONFIGURATION = "Release"
DEFAULT_OUTPUT_DIR = ".meta-agent-temp/release-packages"
PROJECT_RELATIVE_PATH = "meta-agent/dotnet/MetaAgent.Cli/MetaAgent.Cli.csproj"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Build downloadable meta-agent release package")
    parser.add_argument(
        "--runtime",
        action="append",
        default=None,
        help="dotnet publish runtime (repeatable). Defaults: win-x64, linux-x64, osx-arm64, osx-x64",
    )
    parser.add_argument(
        "--configuration",
        default=DEFAULT_CONFIGURATION,
        help=f"dotnet publish configuration (default: {DEFAULT_CONFIGURATION})",
    )
    parser.add_argument(
        "--output-dir",
        default=DEFAULT_OUTPUT_DIR,
        help=f"Directory for final zip output (default: {DEFAULT_OUTPUT_DIR})",
    )
    parser.add_argument("--version", default=None, help="Override package version (default: read from MetaAgent.Cli.csproj)")
    parser.add_argument("--artifact-name", default=None, help="Override zip filename")
    parser.add_argument("--skip-publish", action="store_true", help="Skip dotnet publish and package existing output")
    parser.add_argument("--publish-dir", default=None, help="Use existing publish directory (required with --skip-publish)")
    return parser.parse_args()


def run(command: list[str], cwd: pathlib.Path) -> None:
    print("$ " + " ".join(command))
    result = subprocess.run(command, cwd=str(cwd), check=False)
    if result.returncode != 0:
        raise RuntimeError(f"Command failed ({result.returncode}): {' '.join(command)}")


def detect_version(csproj_path: pathlib.Path) -> str:
    tree = ET.parse(csproj_path)
    root = tree.getroot()
    version_nodes = root.findall(".//Version")
    for node in version_nodes:
        if node.text and node.text.strip():
            return node.text.strip()
    return "0.0.0-dev"


def copy_tree(src: pathlib.Path, dst: pathlib.Path) -> None:
    if not src.is_dir():
        raise FileNotFoundError(f"Missing required directory: {src}")
    shutil.copytree(src, dst)


def sha256_file(path: pathlib.Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def write_sha256_sums(output_dir: pathlib.Path, zip_paths: list[pathlib.Path]) -> pathlib.Path:
    sums_path = output_dir / "SHA256SUMS.txt"
    lines: list[str] = []
    for zip_path in sorted(zip_paths):
        rel = zip_path.relative_to(output_dir).as_posix()
        lines.append(f"{sha256_file(zip_path)}  {rel}")

    sums_path.write_text("\n".join(lines) + "\n", encoding="utf-8")
    return sums_path


def create_readme(destination: pathlib.Path, version: str, runtime: str) -> None:
    executable_name = "meta-agent.exe" if runtime.startswith("win") else "meta-agent"
    text = "\n".join(
        [
            "meta-agent release package",
            "",
            f"Version: {version}",
            f"Runtime: {runtime}",
            "",
            "Contents:",
            f"- {executable_name}: CLI executable",
            "- templates/: scaffold templates you can customize",
            "- agents/: built-in agent manifests",
            "- schema/: policy JSON schema",
            "- examples/: sample files",
            "",
            "Quick start:",
            f"  {executable_name} version",
            f"  {executable_name} init --template dotnet --target ..\\my-service --name my-service",
            "",
            "Notes:",
            "- Run commands from this extracted folder, so the CLI can resolve templates/agents/schema.",
            "- You can edit files in templates/ and agents/ before running commands.",
            "- Scaffolding itself runs in the .NET CLI runtime; bundled Python scripts are optional helpers and can be replaced/removed.",
            "",
        ]
    )
    destination.write_text(text, encoding="utf-8")


def compose_templates_to_output(repo_root: pathlib.Path, output_root: pathlib.Path) -> None:
    compose_script = repo_root / "meta-agent" / "scripts" / "compose-templates.py"
    if not compose_script.exists():
        raise FileNotFoundError(f"Template composition script not found: {compose_script}")

    run(
        [
            "python3",
            str(compose_script),
            "--output-root",
            str(output_root),
        ],
        repo_root,
    )


def resolve_publish_dir(args: argparse.Namespace, repo_root: pathlib.Path) -> pathlib.Path:
    if args.skip_publish:
        if not args.publish_dir:
            raise ValueError("--publish-dir is required when --skip-publish is used")
        publish_dir = pathlib.Path(args.publish_dir)
        if not publish_dir.is_absolute():
            publish_dir = (repo_root / publish_dir).resolve()
        return publish_dir

    publish_dir = pathlib.Path(tempfile.mkdtemp(prefix="meta-agent-publish-", dir="/tmp"))
    project_path = repo_root / PROJECT_RELATIVE_PATH
    run(
        [
            "dotnet",
            "publish",
            str(project_path),
            "-c",
            args.configuration,
            "-r",
            args.runtime,
            "--self-contained",
            "true",
            "/p:PublishSingleFile=true",
            "/p:PublishTrimmed=false",
            "-o",
            str(publish_dir),
        ],
        repo_root,
    )
    return publish_dir


def main() -> int:
    args = parse_args()
    repo_root = pathlib.Path(__file__).resolve().parents[2]
    csproj_path = repo_root / PROJECT_RELATIVE_PATH
    runtimes = args.runtime or list(DEFAULT_RUNTIMES)
    version = args.version or detect_version(csproj_path)
    output_dir = pathlib.Path(args.output_dir)
    if not output_dir.is_absolute():
        output_dir = (repo_root / output_dir).resolve()
    output_dir.mkdir(parents=True, exist_ok=True)

    try:
        if args.skip_publish and len(runtimes) != 1:
            raise ValueError("--skip-publish currently supports exactly one --runtime value")

        generated_zip_paths: list[pathlib.Path] = []
        for runtime in runtimes:
            artifact_name = args.artifact_name or f"meta-agent-{version}-{runtime}"
            runtime_args = argparse.Namespace(**vars(args))
            runtime_args.runtime = runtime
            publish_dir = resolve_publish_dir(runtime_args, repo_root)
            if not publish_dir.is_dir():
                raise FileNotFoundError(f"Publish directory not found: {publish_dir}")

            with tempfile.TemporaryDirectory(prefix="meta-agent-package-", dir="/tmp") as temp_dir:
                temp_root = pathlib.Path(temp_dir)
                package_root = temp_root / artifact_name
                package_root.mkdir(parents=True, exist_ok=True)
                composed_templates_dir = temp_root / "composed-templates"
                compose_templates_to_output(repo_root, composed_templates_dir)

                candidates = [
                    publish_dir / "MetaAgent.Cli.exe",
                    publish_dir / "meta-agent.exe",
                    publish_dir / "MetaAgent.Cli",
                    publish_dir / "meta-agent",
                ]
                exe_source = next((path for path in candidates if path.exists()), None)
                if exe_source is None:
                    raise FileNotFoundError(f"Published executable not found in {publish_dir}")

                packaged_exe_name = "meta-agent.exe" if runtime.startswith("win") else "meta-agent"
                shutil.copy2(exe_source, package_root / packaged_exe_name)
                copy_tree(composed_templates_dir, package_root / "templates")
                copy_tree(repo_root / "meta-agent" / "agents", package_root / "agents")
                copy_tree(repo_root / "meta-agent" / "schema", package_root / "schema")
                copy_tree(repo_root / "meta-agent" / "examples", package_root / "examples")
                create_readme(package_root / "README.txt", version, runtime)

                zip_base = output_dir / artifact_name
                zip_path = pathlib.Path(shutil.make_archive(str(zip_base), "zip", root_dir=temp_root, base_dir=artifact_name))
                print(f"Release package created: {zip_path}")
                generated_zip_paths.append(zip_path)

        checksums_path = write_sha256_sums(output_dir, generated_zip_paths)
        print(f"SHA256 checksums written: {checksums_path}")

    except Exception as exc:  # noqa: BLE001
        print(f"Packaging failed: {exc}", file=sys.stderr)
        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
