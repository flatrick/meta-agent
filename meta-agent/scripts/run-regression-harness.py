#!/usr/bin/env python3
"""Execute canonical meta-agent tasks and track drift across runs."""

from __future__ import annotations

import argparse
import csv
import datetime as dt
import hashlib
import json
import os
import pathlib
import subprocess
from dataclasses import dataclass
from typing import Any


@dataclass
class TaskResult:
    name: str
    description: str
    command: list[str]
    expected_exit_code: int
    exit_code: int
    passed: bool
    duration_ms: int
    stdout_path: str
    stderr_path: str


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Run canonical meta-agent regression harness tasks")
    parser.add_argument(
        "--tasks",
        default=str(pathlib.Path(__file__).resolve().parent / "canonical-regression-tasks.json"),
        help="Path to task manifest JSON",
    )
    parser.add_argument(
        "--output-root",
        default=str(pathlib.Path(__file__).resolve().parents[2] / ".meta-agent-temp" / "regression-harness"),
        help="Root directory for harness outputs",
    )
    parser.add_argument(
        "--repo-root",
        default=str(pathlib.Path(__file__).resolve().parents[2]),
        help="Repository root used as process working directory",
    )
    parser.add_argument(
        "--run-id",
        default=dt.datetime.now(dt.timezone.utc).strftime("%Y%m%dT%H%M%SZ"),
        help="Explicit run id (defaults to current UTC timestamp)",
    )
    parser.add_argument(
        "--skip-execute",
        action="store_true",
        help="Skip command execution and only validate/load the manifest",
    )
    return parser.parse_args()


def load_tasks(path: pathlib.Path) -> list[dict[str, Any]]:
    payload = json.loads(path.read_text(encoding="utf-8"))
    tasks = payload.get("tasks")
    if not isinstance(tasks, list) or not tasks:
        raise ValueError(f"Task manifest at {path} must contain a non-empty 'tasks' array")
    return tasks


def write_default_policy(path: pathlib.Path) -> None:
    policy = {
        "name": "harness-policy",
        "autonomyDefault": "A2",
        "defaultMode": "hybrid",
        "commandGating": "mutating_only",
        "ambiguityThreshold": 0.6,
        "changeBoundaries": {
            "allowedPaths": ["**"],
            "disallowedPaths": [
                "infra/",
                ".github/workflows/",
                "deploy/",
                "terraform/",
                "k8s/"
            ]
        },
        "budgets": {
            "tokensPerDay": 50000,
            "ticketsPerDay": 100,
            "maxConcurrentPrs": 25,
        },
        "abortConditions": ["ci_flaky", "repeated_failure", "high_ambiguity"],
        "budgetAccounting": {
            "mode": "per_invocation",
            "stateFile": ".meta-agent-budget-state.json",
        },
    }
    path.write_text(json.dumps(policy, indent=2) + "\n", encoding="utf-8")


def execute_task(
    task: dict[str, Any],
    repo_root: pathlib.Path,
    cli_project: pathlib.Path,
    command_args: list[str],
    env: dict[str, str],
    logs_dir: pathlib.Path,
) -> TaskResult:
    expected_exit_code = int(task["expected_exit_code"])
    name = str(task["name"])
    description = str(task.get("description", ""))
    command = ["dotnet", "run", "--project", str(cli_project), "--", *command_args]

    start = dt.datetime.now(dt.timezone.utc)
    completed = subprocess.run(
        command,
        cwd=str(repo_root),
        env=env,
        text=True,
        capture_output=True,
        check=False,
    )
    finish = dt.datetime.now(dt.timezone.utc)
    duration_ms = int((finish - start).total_seconds() * 1000)

    stdout_path = logs_dir / f"{name}.stdout.log"
    stderr_path = logs_dir / f"{name}.stderr.log"
    stdout_path.write_text(completed.stdout or "", encoding="utf-8")
    stderr_path.write_text(completed.stderr or "", encoding="utf-8")

    return TaskResult(
        name=name,
        description=description,
        command=command,
        expected_exit_code=expected_exit_code,
        exit_code=completed.returncode,
        passed=completed.returncode == expected_exit_code,
        duration_ms=duration_ms,
        stdout_path=str(stdout_path),
        stderr_path=str(stderr_path),
    )


def ensure_templates_composed(repo_root: pathlib.Path) -> None:
    compose_script = repo_root / "meta-agent" / "scripts" / "compose-templates.py"
    command = ["python3", str(compose_script)]
    completed = subprocess.run(
        command,
        cwd=str(repo_root),
        text=True,
        capture_output=True,
        check=False,
    )
    if completed.returncode != 0:
        raise RuntimeError(
            "template composition failed before regression harness execution:\n"
            f"$ {' '.join(command)}\n"
            f"stdout:\n{completed.stdout}\n"
            f"stderr:\n{completed.stderr}"
        )


def load_metrics(metrics_path: pathlib.Path) -> dict[str, Any]:
    if not metrics_path.exists():
        return {}
    return json.loads(metrics_path.read_text(encoding="utf-8"))


def compute_task_signature(tasks: list[dict[str, Any]]) -> str:
    canonical = json.dumps(tasks, sort_keys=True, separators=(",", ":"))
    return hashlib.sha256(canonical.encode("utf-8")).hexdigest()


def append_history(history_csv: pathlib.Path, summary: dict[str, Any], task_signature: str) -> None:
    history_csv.parent.mkdir(parents=True, exist_ok=True)
    write_header = not history_csv.exists()

    row = [
        summary["run_id"],
        summary["timestamp_utc"],
        summary["tasks_total"],
        summary["tasks_passed"],
        summary["tasks_failed"],
        summary["expected_failures"],
        summary["unexpected_failures"],
        f"{summary['pass_rate']:.4f}",
        summary["metrics"].get("totalRuns", 0),
        summary["metrics"].get("successfulRuns", 0),
        summary["metrics"].get("failedRuns", 0),
        summary["metrics"].get("clarificationRuns", 0),
        summary["metrics"].get("defectLeakageIncidents", 0),
        f"{summary['metrics'].get('tokenCostPerSuccess', 0.0):.4f}",
        f"{summary['metrics'].get('timeToAcceptedSolution', 0.0):.4f}",
        task_signature,
    ]

    with history_csv.open("a", newline="", encoding="utf-8") as handle:
        writer = csv.writer(handle)
        if write_header:
            writer.writerow(
                [
                    "run_id",
                    "timestamp_utc",
                    "tasks_total",
                    "tasks_passed",
                    "tasks_failed",
                    "expected_failures",
                    "unexpected_failures",
                    "pass_rate",
                    "metrics_total_runs",
                    "metrics_successful_runs",
                    "metrics_failed_runs",
                    "metrics_clarification_runs",
                    "metrics_defect_leakage_incidents",
                    "metrics_token_cost_per_success",
                    "metrics_time_to_accepted_solution",
                    "task_signature_sha256",
                ]
            )
        writer.writerow(row)


def load_previous_row(history_csv: pathlib.Path) -> dict[str, Any] | None:
    if not history_csv.exists():
        return None
    rows = list(csv.DictReader(history_csv.read_text(encoding="utf-8").splitlines()))
    if len(rows) < 2:
        return None
    return rows[-2]


def build_drift(summary: dict[str, Any], previous: dict[str, Any] | None, task_signature: str) -> dict[str, Any]:
    drift: dict[str, Any] = {
        "task_signature_changed": False,
        "deltas": {},
    }
    if previous is None:
        drift["baseline"] = True
        return drift

    drift["task_signature_changed"] = previous.get("task_signature_sha256") != task_signature

    def delta(key: str, current_value: float) -> float:
        prev = float(previous.get(key, "0") or 0)
        return round(current_value - prev, 6)

    drift["deltas"] = {
        "pass_rate": delta("pass_rate", float(summary["pass_rate"])),
        "metrics_token_cost_per_success": delta(
            "metrics_token_cost_per_success",
            float(summary["metrics"].get("tokenCostPerSuccess", 0.0)),
        ),
        "metrics_time_to_accepted_solution": delta(
            "metrics_time_to_accepted_solution",
            float(summary["metrics"].get("timeToAcceptedSolution", 0.0)),
        ),
        "metrics_defect_leakage_incidents": delta(
            "metrics_defect_leakage_incidents",
            float(summary["metrics"].get("defectLeakageIncidents", 0)),
        ),
    }
    return drift


def main() -> int:
    args = parse_args()

    tasks_path = pathlib.Path(args.tasks).resolve()
    output_root = pathlib.Path(args.output_root).resolve()
    repo_root = pathlib.Path(args.repo_root).resolve()

    tasks = load_tasks(tasks_path)
    task_signature = compute_task_signature(tasks)

    if args.skip_execute:
        print(f"Loaded {len(tasks)} tasks from {tasks_path}")
        print(f"Task signature: {task_signature}")
        return 0

    run_dir = output_root / "runs" / args.run_id
    workspace_dir = run_dir / "workspace"
    artifacts_dir = run_dir / "artifacts"
    logs_dir = run_dir / "logs"
    workspace_dir.mkdir(parents=True, exist_ok=True)
    (workspace_dir / "existing-repo").mkdir(parents=True, exist_ok=True)
    artifacts_dir.mkdir(parents=True, exist_ok=True)
    logs_dir.mkdir(parents=True, exist_ok=True)

    policy_path = workspace_dir / ".meta-agent-policy.json"
    metrics_path = artifacts_dir / "metrics-scoreboard.json"
    write_default_policy(policy_path)
    ensure_templates_composed(repo_root)

    cli_project = repo_root / "meta-agent" / "dotnet" / "MetaAgent.Cli"
    env = dict(os.environ)
    env["META_AGENT_NONINTERACTIVE"] = "1"

    results: list[TaskResult] = []
    for task in tasks:
        ctx = {
            "workspace_dir": str(workspace_dir),
            "artifacts_dir": str(artifacts_dir),
            "policy_path": str(policy_path),
            "metrics_path": str(metrics_path),
            "run_id": args.run_id,
        }
        raw_args = task.get("args", [])
        if not isinstance(raw_args, list) or not raw_args:
            raise ValueError(f"Task '{task.get('name', '<unknown>')}' must include a non-empty args list")
        command_args = [str(arg).format(**ctx) for arg in raw_args]

        result = execute_task(task, repo_root, cli_project, command_args, env, logs_dir)
        results.append(result)
        marker = "PASS" if result.passed else "FAIL"
        print(f"[{marker}] {result.name}: expected {result.expected_exit_code}, got {result.exit_code}")

    total = len(results)
    passed = sum(1 for r in results if r.passed)
    failed = total - passed
    expected_failures = sum(1 for r in results if r.expected_exit_code != 0)
    unexpected_failures = sum(1 for r in results if not r.passed)
    pass_rate = (passed / total) if total else 0.0

    metrics = load_metrics(metrics_path)
    summary = {
        "run_id": args.run_id,
        "timestamp_utc": dt.datetime.now(dt.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
        "tasks_total": total,
        "tasks_passed": passed,
        "tasks_failed": failed,
        "expected_failures": expected_failures,
        "unexpected_failures": unexpected_failures,
        "pass_rate": pass_rate,
        "metrics": metrics,
        "task_results": [
            {
                "name": r.name,
                "description": r.description,
                "expected_exit_code": r.expected_exit_code,
                "exit_code": r.exit_code,
                "passed": r.passed,
                "duration_ms": r.duration_ms,
                "command": r.command,
                "stdout_path": r.stdout_path,
                "stderr_path": r.stderr_path,
            }
            for r in results
        ],
        "artifacts": {
            "run_dir": str(run_dir),
            "workspace_dir": str(workspace_dir),
            "artifacts_dir": str(artifacts_dir),
            "metrics_path": str(metrics_path),
            "tasks_manifest": str(tasks_path),
        },
    }

    history_csv = output_root / "history" / "harness-history.csv"
    append_history(history_csv, summary, task_signature)
    previous_row = load_previous_row(history_csv)
    summary["drift"] = build_drift(summary, previous_row, task_signature)

    summary_path = run_dir / "harness-summary.json"
    summary_path.write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")

    latest_path = output_root / "latest-summary.json"
    latest_path.write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")

    print(f"Harness summary: {summary_path}")
    print(f"History CSV: {history_csv}")

    return 0 if unexpected_failures == 0 else 1


if __name__ == "__main__":
    raise SystemExit(main())
