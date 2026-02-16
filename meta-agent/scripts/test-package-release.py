#!/usr/bin/env python3
"""Basic coverage for package-release.py skip-publish mode."""

from __future__ import annotations

import hashlib
import pathlib
import subprocess
import sys
import tempfile
import zipfile


def sha256_file(path: pathlib.Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def main() -> int:
    repo_root = pathlib.Path(__file__).resolve().parents[2]
    script = repo_root / "meta-agent" / "scripts" / "package-release.py"

    scenarios = [
        ("win-x64", "MetaAgent.Cli.exe", "meta-agent.exe"),
        ("linux-x64", "MetaAgent.Cli", "meta-agent"),
    ]

    for runtime, published_name, packaged_name in scenarios:
        with tempfile.TemporaryDirectory(prefix="meta-agent-package-test-publish-", dir="/tmp") as publish_dir_raw:
            publish_dir = pathlib.Path(publish_dir_raw)
            (publish_dir / published_name).write_bytes(b"fake-exe")

            with tempfile.TemporaryDirectory(prefix="meta-agent-package-test-out-", dir="/tmp") as output_dir_raw:
                output_dir = pathlib.Path(output_dir_raw)
                artifact_name = f"meta-agent-test-artifact-{runtime}"
                result = subprocess.run(
                    [
                        "python3",
                        str(script),
                        "--runtime",
                        runtime,
                        "--skip-publish",
                        "--publish-dir",
                        str(publish_dir),
                        "--output-dir",
                        str(output_dir),
                        "--artifact-name",
                        artifact_name,
                        "--version",
                        "1.0.2-test",
                    ],
                    cwd=str(repo_root),
                    check=False,
                )
                if result.returncode != 0:
                    print(f"package-release.py failed in --skip-publish mode for {runtime}", file=sys.stderr)
                    return 1

                zip_path = output_dir / f"{artifact_name}.zip"
                if not zip_path.is_file():
                    print(f"expected zip not found: {zip_path}", file=sys.stderr)
                    return 1

                checksums_path = output_dir / "SHA256SUMS.txt"
                if not checksums_path.is_file():
                    print(f"expected checksums file not found: {checksums_path}", file=sys.stderr)
                    return 1

                lines = [line.strip() for line in checksums_path.read_text(encoding="utf-8").splitlines() if line.strip()]
                entries = {}
                for line in lines:
                    parts = line.split("  ", 1)
                    if len(parts) != 2:
                        print(f"invalid checksums format line: {line}", file=sys.stderr)
                        return 1
                    entries[parts[1]] = parts[0]

                zip_name = f"{artifact_name}.zip"
                if zip_name not in entries:
                    print(f"checksums file missing entry for {zip_name}", file=sys.stderr)
                    return 1
                if entries[zip_name] != sha256_file(zip_path):
                    print(f"checksum mismatch for {zip_name}", file=sys.stderr)
                    return 1

                with zipfile.ZipFile(zip_path) as archive:
                    names = set(archive.namelist())

                expected = {
                    f"{artifact_name}/{packaged_name}",
                    f"{artifact_name}/templates/dotnet/{{{{ project_name }}}}.slnx",
                    f"{artifact_name}/templates/dotnet/src/Program.cs",
                    f"{artifact_name}/templates/dotnet/tests/Project.Tests.csproj",
                    f"{artifact_name}/templates/powershell/src/main.ps1",
                    f"{artifact_name}/agents/dotnet-agent.json",
                    f"{artifact_name}/schema/meta-agent-policy.schema.json",
                    f"{artifact_name}/examples/sample_config.json",
                    f"{artifact_name}/README.txt",
                }
                missing = [entry for entry in expected if entry not in names]
                if missing:
                    print(f"archive missing expected files for {runtime}:", file=sys.stderr)
                    for item in missing:
                        print(f"- {item}", file=sys.stderr)
                    return 1

    print("package-release.py test passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
