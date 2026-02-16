#!/usr/bin/env python3
"""Manage meta-agent/DOC_DELTA.md with deterministic add/check/fix workflows."""

from __future__ import annotations

import argparse
import contextlib
from dataclasses import dataclass
from datetime import datetime, timezone
import getpass
import json
import os
import pathlib
import re
import socket
import sys
import time

if os.name == "nt":
    import msvcrt
else:
    import fcntl

TIMESTAMP_FORMAT = "%Y-%m-%d %H:%M:%SZ"
DEFAULT_DOC_DELTA_RELATIVE_PATH = "meta-agent/DOC_DELTA.md"
DEFAULT_LOCK_TIMEOUT_SECONDS = 30.0
DEFAULT_LOCK_POLL_INTERVAL_MS = 200
ENTRY_HEADER_LINE_PATTERN = re.compile(
    r"^## (?P<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}Z) - (?P<title>.+)$"
)
ENTRY_HEADER_PATTERN = re.compile(ENTRY_HEADER_LINE_PATTERN.pattern, re.MULTILINE)


@dataclass(frozen=True)
class DocDeltaEntry:
    index: int
    timestamp_text: str
    timestamp_value: datetime
    title: str
    block: str


@dataclass(frozen=True)
class ParsedDocDelta:
    preamble: str
    entries: list[DocDeltaEntry]


class LockTimeoutError(RuntimeError):
    """Raised when DOC_DELTA lock cannot be acquired before timeout."""


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Manage meta-agent/DOC_DELTA.md entries.")
    parser.add_argument(
        "--doc-delta",
        type=pathlib.Path,
        default=pathlib.Path(DEFAULT_DOC_DELTA_RELATIVE_PATH),
        help=f"Path to DOC_DELTA file (default: {DEFAULT_DOC_DELTA_RELATIVE_PATH}).",
    )
    parser.add_argument(
        "--lock-timeout-seconds",
        type=float,
        default=DEFAULT_LOCK_TIMEOUT_SECONDS,
        help=f"Max wait to acquire lock before failing (default: {DEFAULT_LOCK_TIMEOUT_SECONDS}).",
    )
    parser.add_argument(
        "--lock-poll-interval-ms",
        type=int,
        default=DEFAULT_LOCK_POLL_INTERVAL_MS,
        help=f"Polling interval while waiting for lock (default: {DEFAULT_LOCK_POLL_INTERVAL_MS} ms).",
    )
    subparsers = parser.add_subparsers(dest="command", required=True)

    add_parser = subparsers.add_parser("add", help="Add a timestamped DOC_DELTA entry.")
    add_parser.add_argument("--title", required=True, help="Entry title text.")
    add_parser.add_argument(
        "--timestamp",
        default=None,
        help="Optional UTC timestamp in format YYYY-MM-DD HH:MM:SSZ (default: now in UTC).",
    )
    add_parser.add_argument(
        "--change",
        action="append",
        default=[],
        help="Repeatable bullet content. Rendered as '- <value>'.",
    )
    add_parser.add_argument(
        "--verification",
        action="append",
        default=[],
        help="Repeatable verification bullet content. Rendered under '- Verification:'.",
    )
    add_parser.add_argument(
        "--body-file",
        type=pathlib.Path,
        default=None,
        help="Optional markdown body file for the entry (mutually exclusive with --change/--verification).",
    )

    subparsers.add_parser("check", help="Validate header format and chronological order.")
    subparsers.add_parser("fix", help="Sort entries by timestamp and rewrite canonically.")

    return parser.parse_args()


def utc_now_timestamp() -> str:
    return datetime.now(timezone.utc).strftime(TIMESTAMP_FORMAT)


def doc_delta_lock_path(doc_delta_path: pathlib.Path) -> pathlib.Path:
    return doc_delta_path.parent / f"{doc_delta_path.name}.lock"


def lock_holder_metadata(operation: str) -> dict[str, object]:
    return {
        "heldByPid": os.getpid(),
        "heldByUser": getpass.getuser(),
        "heldByHost": socket.gethostname(),
        "heldForOperation": operation,
        "heldSinceUtc": utc_now_timestamp(),
    }


def ensure_lock_file_seeded(lock_file: object) -> None:
    file_obj = lock_file
    file_obj.seek(0, os.SEEK_END)
    if file_obj.tell() == 0:
        file_obj.write("\n")
        file_obj.flush()


def try_lock_file(lock_file: object) -> None:
    file_obj = lock_file
    if os.name == "nt":
        file_obj.seek(0)
        msvcrt.locking(file_obj.fileno(), msvcrt.LK_NBLCK, 1)
        return
    fcntl.flock(file_obj.fileno(), fcntl.LOCK_EX | fcntl.LOCK_NB)


def unlock_file(lock_file: object) -> None:
    file_obj = lock_file
    if os.name == "nt":
        file_obj.seek(0)
        msvcrt.locking(file_obj.fileno(), msvcrt.LK_UNLCK, 1)
        return
    fcntl.flock(file_obj.fileno(), fcntl.LOCK_UN)


def write_lock_metadata(lock_file: object, metadata: dict[str, object]) -> None:
    file_obj = lock_file
    file_obj.seek(0)
    file_obj.truncate(0)
    file_obj.write(json.dumps(metadata, sort_keys=True))
    file_obj.write("\n")
    file_obj.flush()


def clear_lock_metadata(lock_file: object) -> None:
    file_obj = lock_file
    file_obj.seek(0)
    file_obj.truncate(0)
    file_obj.flush()


def read_lock_metadata(lock_path: pathlib.Path) -> str:
    try:
        raw = lock_path.read_text(encoding="utf-8").strip()
    except FileNotFoundError:
        return ""
    except OSError:
        return ""
    if not raw:
        return ""
    return raw


@contextlib.contextmanager
def acquire_doc_delta_lock(
    doc_delta_path: pathlib.Path,
    timeout_seconds: float,
    poll_interval_seconds: float,
    operation: str,
):
    lock_path = doc_delta_lock_path(doc_delta_path)
    lock_path.parent.mkdir(parents=True, exist_ok=True)

    with lock_path.open("a+", encoding="utf-8") as lock_file:
        ensure_lock_file_seeded(lock_file)

        started = time.monotonic()
        while True:
            try:
                try_lock_file(lock_file)
                break
            except OSError as exc:
                elapsed = time.monotonic() - started
                if elapsed >= timeout_seconds:
                    holder = read_lock_metadata(lock_path)
                    holder_suffix = f" Current holder metadata: {holder}" if holder else ""
                    raise LockTimeoutError(
                        f"Timed out waiting for DOC_DELTA lock after {timeout_seconds:.3f}s "
                        f"for command '{operation}'. Lock file: {lock_path}.{holder_suffix}"
                    ) from exc
                time.sleep(poll_interval_seconds)

        try:
            write_lock_metadata(lock_file, lock_holder_metadata(operation))
            yield
        finally:
            clear_lock_metadata(lock_file)
            unlock_file(lock_file)


def parse_timestamp(timestamp_text: str) -> datetime:
    return datetime.strptime(timestamp_text, TIMESTAMP_FORMAT)


def parse_doc_delta(text: str) -> ParsedDocDelta:
    invalid_headers: list[tuple[int, str]] = []
    for line_number, line in enumerate(text.splitlines(), start=1):
        if line.startswith("## ") and ENTRY_HEADER_LINE_PATTERN.fullmatch(line) is None:
            invalid_headers.append((line_number, line))

    if invalid_headers:
        first_line, first_text = invalid_headers[0]
        raise ValueError(
            "Invalid level-2 heading format. "
            f"Expected `## YYYY-MM-DD HH:MM:SSZ - Title`, found at line {first_line}: {first_text}"
        )

    matches = list(ENTRY_HEADER_PATTERN.finditer(text))
    if not matches:
        raise ValueError("No DOC_DELTA entries found.")

    preamble = text[: matches[0].start()].rstrip("\n")
    entries: list[DocDeltaEntry] = []
    for idx, match in enumerate(matches):
        start = match.start()
        end = matches[idx + 1].start() if idx + 1 < len(matches) else len(text)
        block = text[start:end].strip("\n")

        timestamp_text = match.group("timestamp")
        try:
            timestamp_value = parse_timestamp(timestamp_text)
        except ValueError as exc:
            raise ValueError(
                f"Invalid timestamp value at entry '{match.group('title')}': {timestamp_text}"
            ) from exc

        entries.append(
            DocDeltaEntry(
                index=idx,
                timestamp_text=timestamp_text,
                timestamp_value=timestamp_value,
                title=match.group("title"),
                block=block,
            )
        )

    return ParsedDocDelta(preamble=preamble, entries=entries)


def sort_entries(entries: list[DocDeltaEntry]) -> list[DocDeltaEntry]:
    # Stable sort preserves relative order for equal timestamps.
    return sorted(entries, key=lambda entry: (entry.timestamp_value, entry.index))


def render_doc_delta(preamble: str, entries: list[DocDeltaEntry]) -> str:
    sections: list[str] = []
    preamble_text = preamble.rstrip()
    if preamble_text:
        sections.append(preamble_text)

    for entry in entries:
        sections.append(entry.block.rstrip())

    return "\n\n".join(sections).rstrip() + "\n"


def read_doc_delta(path: pathlib.Path) -> tuple[str, ParsedDocDelta]:
    if not path.exists():
        raise ValueError(f"DOC_DELTA file not found: {path}")
    text = path.read_text(encoding="utf-8")
    parsed = parse_doc_delta(text)
    return text, parsed


def write_doc_delta(path: pathlib.Path, preamble: str, entries: list[DocDeltaEntry]) -> None:
    path.write_text(render_doc_delta(preamble, entries), encoding="utf-8")


def create_entry_block(title: str, timestamp_text: str, body_text: str) -> str:
    header = f"## {timestamp_text} - {title}"
    normalized_body = body_text.strip("\n")
    if not normalized_body:
        return header
    return f"{header}\n\n{normalized_body}"


def resolve_body_text(args: argparse.Namespace) -> str:
    if args.body_file is not None and (args.change or args.verification):
        raise ValueError("Use either --body-file or --change/--verification values, not both.")

    if args.body_file is not None:
        if not args.body_file.exists():
            raise ValueError(f"Body file not found: {args.body_file}")
        body_text = args.body_file.read_text(encoding="utf-8")
        if not body_text.strip():
            raise ValueError("Body file must contain non-empty content.")
        return body_text

    bullets: list[str] = []
    for value in args.change:
        bullets.append(f"- {value}")

    if args.verification:
        if bullets:
            bullets.append("")
        bullets.append("- Verification:")
        for value in args.verification:
            bullets.append(f"- {value}")

    if not bullets:
        raise ValueError("Add mode requires --body-file or at least one --change/--verification value.")

    return "\n".join(bullets)


def run_add(doc_delta_path: pathlib.Path, args: argparse.Namespace) -> int:
    text, parsed = read_doc_delta(doc_delta_path)
    _ = text  # keep symmetry with other modes

    title = args.title.strip()
    if not title:
        raise ValueError("Title must not be empty.")

    timestamp_text = args.timestamp.strip() if args.timestamp is not None else utc_now_timestamp()
    try:
        timestamp_value = parse_timestamp(timestamp_text)
    except ValueError as exc:
        raise ValueError(
            f"Invalid --timestamp '{timestamp_text}'. Expected format: YYYY-MM-DD HH:MM:SSZ."
        ) from exc

    body_text = resolve_body_text(args)
    entry_block = create_entry_block(title=title, timestamp_text=timestamp_text, body_text=body_text)
    new_entry = DocDeltaEntry(
        index=len(parsed.entries),
        timestamp_text=timestamp_text,
        timestamp_value=timestamp_value,
        title=title,
        block=entry_block,
    )

    sorted_entries = sort_entries(parsed.entries + [new_entry])
    write_doc_delta(doc_delta_path, parsed.preamble, sorted_entries)
    print(f"Added DOC_DELTA entry: {timestamp_text} - {title}")
    return 0


def run_check(doc_delta_path: pathlib.Path) -> int:
    _, parsed = read_doc_delta(doc_delta_path)
    expected_order = sort_entries(parsed.entries)
    actual_order = [entry.index for entry in parsed.entries]
    normalized_order = [entry.index for entry in expected_order]

    if actual_order != normalized_order:
        first_mismatch = next(
            i for i, (actual, expected) in enumerate(zip(actual_order, normalized_order), start=1) if actual != expected
        )
        actual_entry = parsed.entries[first_mismatch - 1]
        expected_entry = expected_order[first_mismatch - 1]
        print("DOC_DELTA order check failed.", file=sys.stderr)
        print(
            f"First mismatch at position {first_mismatch}: "
            f"found '{actual_entry.timestamp_text} - {actual_entry.title}', "
            f"expected '{expected_entry.timestamp_text} - {expected_entry.title}'.",
            file=sys.stderr,
        )
        print(
            f"Run `python3 ./meta-agent/scripts/manage-doc-delta.py --doc-delta {doc_delta_path} fix` to normalize.",
            file=sys.stderr,
        )
        return 1

    print(f"DOC_DELTA check passed ({len(parsed.entries)} entries).")
    return 0


def run_fix(doc_delta_path: pathlib.Path) -> int:
    current_text, parsed = read_doc_delta(doc_delta_path)
    sorted_entries = sort_entries(parsed.entries)
    normalized_text = render_doc_delta(parsed.preamble, sorted_entries)
    if normalized_text == current_text:
        print("DOC_DELTA is already normalized.")
        return 0

    write_doc_delta(doc_delta_path, parsed.preamble, sorted_entries)
    print("DOC_DELTA normalized and written.")
    return 0


def main() -> int:
    args = parse_args()
    repo_root = pathlib.Path(__file__).resolve().parents[2]
    doc_delta_path = args.doc_delta
    if not doc_delta_path.is_absolute():
        doc_delta_path = (repo_root / doc_delta_path).resolve()

    lock_timeout_seconds = args.lock_timeout_seconds
    if lock_timeout_seconds < 0:
        print("--lock-timeout-seconds must be >= 0.", file=sys.stderr)
        return 2

    lock_poll_interval_ms = args.lock_poll_interval_ms
    if lock_poll_interval_ms <= 0:
        print("--lock-poll-interval-ms must be > 0.", file=sys.stderr)
        return 2

    try:
        with acquire_doc_delta_lock(
            doc_delta_path=doc_delta_path,
            timeout_seconds=lock_timeout_seconds,
            poll_interval_seconds=lock_poll_interval_ms / 1000.0,
            operation=args.command,
        ):
            if args.command == "add":
                return run_add(doc_delta_path, args)
            if args.command == "check":
                return run_check(doc_delta_path)
            if args.command == "fix":
                return run_fix(doc_delta_path)
    except ValueError as exc:
        print(str(exc), file=sys.stderr)
        return 1
    except LockTimeoutError as exc:
        print(str(exc), file=sys.stderr)
        return 1

    print(f"Unsupported command: {args.command}", file=sys.stderr)
    return 2


if __name__ == "__main__":
    raise SystemExit(main())
