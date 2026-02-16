#!/usr/bin/env python3
"""Run tests with coverage, generate report, and append coverage history."""

from __future__ import annotations

import argparse
import csv
import datetime as dt
import os
import pathlib
import subprocess
import xml.etree.ElementTree as ET


def run(cmd: list[str], cwd: pathlib.Path, env_overrides: dict[str, str] | None = None) -> None:
    print(f"$ {' '.join(cmd)}")
    env = os.environ.copy()
    if env_overrides:
        env.update(env_overrides)
    completed = subprocess.run(cmd, cwd=str(cwd), env=env, check=False)
    if completed.returncode != 0:
        raise SystemExit(completed.returncode)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Run coverage workflow for MetaAgent tests")
    parser.add_argument("--threshold", type=int, default=75)
    parser.add_argument("--threshold-type", default="line")
    parser.add_argument("--threshold-stat", default="total")
    parser.add_argument("--skip-report", action="store_true", help="Skip reportgenerator execution")
    return parser.parse_args()


def compute_line_coverage(opencover_xml: pathlib.Path) -> tuple[float, int, int]:
    if not opencover_xml.exists():
        raise FileNotFoundError(f"Opencover report not found: {opencover_xml}")

    root = ET.parse(opencover_xml).getroot()
    total = 0
    visited = 0
    for element in root.iter():
        num = element.attrib.get("numSequencePoints")
        vis = element.attrib.get("visitedSequencePoints")
        if num is not None:
            total += int(num)
        if vis is not None:
            visited += int(vis)

    pct = round((visited / total) * 100.0, 2) if total > 0 else 0.0
    return pct, visited, total


def append_history(history_csv: pathlib.Path, pct: float, visited: int, total: int) -> None:
    history_csv.parent.mkdir(parents=True, exist_ok=True)
    write_header = not history_csv.exists()
    with history_csv.open("a", newline="", encoding="utf-8") as handle:
        writer = csv.writer(handle)
        if write_header:
            writer.writerow(["timestamp", "line_coverage", "visited", "total"])
        writer.writerow([
            dt.datetime.now(dt.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
            f"{pct:.2f}",
            visited,
            total,
        ])


def main() -> int:
    args = parse_args()

    script_dir = pathlib.Path(__file__).resolve().parent
    root = script_dir.parent
    project = root / "dotnet" / "MetaAgent.Tests" / "MetaAgent.Tests.csproj"
    coverage_dir = root / "dotnet" / "coverage"
    coverage_dir.mkdir(parents=True, exist_ok=True)
    compose_script = root / "scripts" / "compose-templates.py"

    print("Composing scaffold templates from source...")
    run(["python3", str(compose_script)], cwd=root)

    print(f"Running tests with coverage for project: {project}")
    run(
        [
            "dotnet",
            "test",
            str(project),
            "/p:CollectCoverage=true",
            f"/p:CoverletOutput={coverage_dir}{os.sep}",
            "/p:CoverletOutputFormat=opencover",
            f"/p:Threshold={args.threshold}",
            f"/p:ThresholdType={args.threshold_type}",
            f"/p:ThresholdStat={args.threshold_stat}",
        ],
        cwd=root,
        env_overrides={"META_AGENT_NONINTERACTIVE": "1"},
    )

    if not args.skip_report:
        print("Restoring dotnet tools (reportgenerator)...")
        run(["dotnet", "tool", "restore"], cwd=root)

        report_dir = coverage_dir / "report"
        history_dir = coverage_dir / "history"
        report_dir.mkdir(parents=True, exist_ok=True)
        history_dir.mkdir(parents=True, exist_ok=True)

        print("Generating HTML report with ReportGenerator...")
        run(
            [
                "dotnet",
                "tool",
                "run",
                "reportgenerator",
                f"-reports:{coverage_dir / 'coverage.opencover.xml'}",
                f"-targetdir:{report_dir}",
                "-reporttypes:Html",
                f"-historydir:{history_dir}",
                "-verbosity:Info",
            ],
            cwd=root,
        )

    pct, visited, total = compute_line_coverage(coverage_dir / "coverage.opencover.xml")
    print(f"Line coverage: {pct:.2f}% ({visited}/{total})")

    history_csv = coverage_dir / "history" / "coverage_history.csv"
    append_history(history_csv, pct, visited, total)

    print(f"Coverage HTML report available at: {coverage_dir / 'report' / 'index.htm'}")
    print(f"Historic coverage appended to: {history_csv}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
