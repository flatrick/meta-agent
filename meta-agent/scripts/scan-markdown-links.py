#!/usr/bin/env python3
"""Repository-wide Markdown link scanner.

Produces:
1. A machine-readable JSON report (files, links, resolution status, backlinks).
2. A human-readable Markdown summary.
3. Link opportunities for inline-code path snippets that resolve to real targets
   but are not markdown links yet (including Structurizr slug-style links).

Run from repository root:
  python3 ./meta-agent/scripts/scan-markdown-links.py
"""

from __future__ import annotations

import argparse
import datetime as dt
import json
import os
import pathlib
import re
import unicodedata
from dataclasses import dataclass
from typing import Any


LINK_RE = re.compile(r"!\[[^\]]*\]\(([^)]+)\)|\[[^\]]+\]\(([^)]+)\)")
H1_RE = re.compile(r"^\s*#\s+(.+?)\s*$", re.MULTILINE)
INLINE_CODE_RE = re.compile(r"`([^`\n]+)`")
FENCE_RE = re.compile(r"^\s*```")
MAX_OPPORTUNITIES_IN_MARKDOWN_REPORT = 200

EXTERNAL_PREFIXES = (
    "http://",
    "https://",
    "mailto:",
    "tel:",
    "data:",
    "javascript:",
)

DEFAULT_EXCLUDE_SEGMENTS = [".git", ".meta-agent-temp"]


@dataclass(frozen=True)
class LinkResult:
    source: pathlib.Path
    line: int
    raw_target: str
    target: str
    classification: str
    status: str
    resolution: str
    fragment: str | None
    resolved_path: pathlib.Path | None
    target_document: pathlib.Path | None


@dataclass(frozen=True)
class LinkOpportunity:
    source: pathlib.Path
    line: int
    kind: str
    link_style: str
    raw_text: str
    candidate: str
    resolution: str
    resolved_path: pathlib.Path
    target_document: pathlib.Path | None


def find_repo_root() -> pathlib.Path:
    return pathlib.Path(__file__).resolve().parents[2]


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Scan Markdown links across repository")
    parser.add_argument("--repo-root", default=str(find_repo_root()), help="Repository root")
    parser.add_argument(
        "--json-out",
        default=".meta-agent-temp/markdown-link-report.json",
        help="JSON report output path (relative to repo root unless absolute)",
    )
    parser.add_argument(
        "--markdown-out",
        default=".meta-agent-temp/markdown-link-report.md",
        help="Markdown summary output path (relative to repo root unless absolute)",
    )
    parser.add_argument(
        "--exclude-segment",
        action="append",
        default=[],
        help="Path segment to exclude during scan (repeatable)",
    )
    parser.add_argument(
        "--fail-on-dead",
        action="store_true",
        help="Return non-zero exit code when dead local links are detected",
    )
    return parser.parse_args()


def resolve_output_path(repo_root: pathlib.Path, value: str) -> pathlib.Path:
    candidate = pathlib.Path(value)
    if candidate.is_absolute():
        return candidate
    return repo_root / candidate


def is_excluded(path: pathlib.Path, exclude_segments: list[str]) -> bool:
    parts = set(path.parts)
    return any(segment in parts for segment in exclude_segments)


def list_markdown_files(repo_root: pathlib.Path, exclude_segments: list[str]) -> list[pathlib.Path]:
    files: list[pathlib.Path] = []
    for path in sorted(repo_root.rglob("*.md")):
        if is_excluded(path, exclude_segments):
            continue
        files.append(path)
    return files


def read_text(path: pathlib.Path) -> str:
    return path.read_text(encoding="utf-8", errors="ignore")


def slugify(value: str) -> str:
    normalized = unicodedata.normalize("NFKD", value.strip().lower())
    without_marks = "".join(ch for ch in normalized if not unicodedata.combining(ch))
    alnum_or_space = "".join(ch if (ch.isalnum() or ch in " -_") else " " for ch in without_marks)
    return "-".join(part for part in alnum_or_space.replace("_", " ").split() if part)


def first_h1(markdown_text: str) -> str | None:
    match = H1_RE.search(markdown_text)
    if not match:
        return None
    return match.group(1).strip()


def normalize_target(raw_target: str) -> str:
    value = raw_target.strip()
    if value.startswith("<"):
        end = value.find(">")
        if end > 0:
            return value[1:end].strip()
    # Markdown optional link title: (path "title")
    # Keep first token as link path in non-angle-bracket form.
    if " " in value:
        return value.split()[0].strip()
    return value


def classify_target(target: str) -> str:
    lowered = target.lower()
    if lowered.startswith(EXTERNAL_PREFIXES):
        return "external"
    if target.startswith("#"):
        return "anchor"
    return "local"


def split_fragment(target: str) -> tuple[str, str | None]:
    base = target
    fragment: str | None = None
    if "#" in base:
        base, fragment = base.split("#", 1)
    if "?" in base:
        base = base.split("?", 1)[0]
    return base, fragment


def normalize_path(path: pathlib.Path) -> pathlib.Path:
    return path.resolve(strict=False)


def repo_relative_or_absolute(path: pathlib.Path, repo_root: pathlib.Path) -> str:
    normalized = normalize_path(path)
    try:
        return str(normalized.relative_to(normalize_path(repo_root))).replace("\\", "/")
    except ValueError:
        return str(normalized).replace("\\", "/")


def build_slug_map(markdown_files: list[pathlib.Path]) -> dict[pathlib.Path, dict[str, list[pathlib.Path]]]:
    slug_map: dict[pathlib.Path, dict[str, list[pathlib.Path]]] = {}
    for md_file in markdown_files:
        text = read_text(md_file)
        h1 = first_h1(text)
        if not h1:
            continue
        slug = slugify(h1)
        parent = normalize_path(md_file.parent)
        slug_map.setdefault(parent, {}).setdefault(slug, []).append(md_file)
    return slug_map


def resolve_slug_link(
    repo_root: pathlib.Path,
    source: pathlib.Path,
    base_target: str,
    slug_map: dict[pathlib.Path, dict[str, list[pathlib.Path]]],
) -> tuple[pathlib.Path | None, str]:
    candidate = base_target.rstrip("/")
    if not candidate:
        return None, "not_resolved"

    parts = pathlib.PurePosixPath(candidate).parts
    slug = parts[-1]
    parent_parts = parts[:-1]

    if base_target.startswith("/"):
        parent_dir = normalize_path(repo_root.joinpath(*parent_parts))
    else:
        parent_dir = normalize_path(source.parent.joinpath(*parent_parts))

    matches = slug_map.get(parent_dir, {}).get(slug, [])
    if not matches:
        return None, "not_resolved"
    if len(matches) == 1:
        return matches[0], "slug"
    return matches[0], "slug_ambiguous"


def resolve_local_link(
    repo_root: pathlib.Path,
    source: pathlib.Path,
    base_target: str,
    slug_map: dict[pathlib.Path, dict[str, list[pathlib.Path]]],
) -> tuple[pathlib.Path | None, str]:
    if not base_target:
        return None, "anchor_only"

    if base_target.startswith("/"):
        candidate = normalize_path(repo_root / base_target.lstrip("/"))
    else:
        candidate = normalize_path(source.parent / base_target)

    if candidate.exists():
        if candidate.is_dir():
            index_md = candidate / "index.md"
            if index_md.exists():
                return index_md, "directory_index"
            return candidate, "directory"
        return candidate, "exact"

    if candidate.suffix == "":
        md_candidate = candidate.with_suffix(".md")
        if md_candidate.exists():
            return md_candidate, "md_extension"

    if base_target.endswith("/"):
        return resolve_slug_link(repo_root, source, base_target, slug_map)

    return None, "not_resolved"


def spans_overlap(start_a: int, end_a: int, start_b: int, end_b: int) -> bool:
    return start_a < end_b and start_b < end_a


def line_link_spans(line: str) -> list[tuple[int, int]]:
    return [(match.start(), match.end()) for match in LINK_RE.finditer(line)]


def overlaps_any(spans: list[tuple[int, int]], start: int, end: int) -> bool:
    return any(spans_overlap(start, end, span_start, span_end) for span_start, span_end in spans)


def normalize_inline_candidate(raw_text: str) -> str:
    candidate = raw_text.strip()
    candidate = candidate.strip("`")
    candidate = candidate.strip("()")
    candidate = candidate.rstrip(".,:;")
    return candidate.strip()


def looks_like_path_candidate(candidate: str) -> bool:
    if not candidate:
        return False
    if " " in candidate:
        return False
    lowered = candidate.lower()
    if lowered.startswith(EXTERNAL_PREFIXES):
        return False
    if candidate.startswith("#"):
        return False
    return "/" in candidate or candidate.endswith(".md")


def collect_link_opportunities(
    repo_root: pathlib.Path,
    markdown_files: list[pathlib.Path],
    slug_map: dict[pathlib.Path, dict[str, list[pathlib.Path]]],
) -> list[LinkOpportunity]:
    opportunities: list[LinkOpportunity] = []
    markdown_set = {normalize_path(path) for path in markdown_files}
    seen: set[tuple[pathlib.Path, int, str, pathlib.Path]] = set()

    for source in markdown_files:
        text = read_text(source)
        in_fence = False

        for line_number, line in enumerate(text.splitlines(), start=1):
            if FENCE_RE.match(line):
                in_fence = not in_fence
                continue
            if in_fence:
                continue

            link_spans = line_link_spans(line)
            for match in INLINE_CODE_RE.finditer(line):
                if overlaps_any(link_spans, match.start(), match.end()):
                    continue

                raw_text = match.group(1)
                candidate = normalize_inline_candidate(raw_text)
                if not looks_like_path_candidate(candidate):
                    continue

                base_target, _ = split_fragment(candidate)
                resolved_path, resolution = resolve_local_link(repo_root, source, base_target, slug_map)
                if resolved_path is None:
                    continue

                resolved_normalized = normalize_path(resolved_path)
                key = (source, line_number, candidate, resolved_normalized)
                if key in seen:
                    continue
                seen.add(key)

                target_document = resolved_path if resolved_normalized in markdown_set else None
                link_style = "structurizr_slug" if resolution in {"slug", "slug_ambiguous"} else "standard_path"
                opportunities.append(
                    LinkOpportunity(
                        source=source,
                        line=line_number,
                        kind=f"inline_code_{link_style}",
                        link_style=link_style,
                        raw_text=raw_text,
                        candidate=candidate,
                        resolution=resolution,
                        resolved_path=resolved_path,
                        target_document=target_document,
                    )
                )

    return opportunities


def suggested_markdown_link(
    source: pathlib.Path,
    resolved_path: pathlib.Path,
    target_document: pathlib.Path | None,
    target_h1: str | None,
) -> str:
    try:
        relative_target = os.path.relpath(str(resolved_path), start=str(source.parent))
    except ValueError:
        relative_target = str(resolved_path)
    relative_target = relative_target.replace("\\", "/")

    if target_document is not None and target_h1:
        label = target_h1
    elif target_document is not None:
        label = target_document.stem
    else:
        label = resolved_path.name

    return f"[{label}]({relative_target})"


def collect_links(
    repo_root: pathlib.Path,
    markdown_files: list[pathlib.Path],
    slug_map: dict[pathlib.Path, dict[str, list[pathlib.Path]]],
) -> list[LinkResult]:
    results: list[LinkResult] = []
    markdown_set = {normalize_path(path) for path in markdown_files}

    for source in markdown_files:
        text = read_text(source)
        for line_number, line in enumerate(text.splitlines(), start=1):
            for match in LINK_RE.finditer(line):
                raw_target = match.group(1) if match.group(1) is not None else match.group(2)
                if raw_target is None:
                    continue

                target = normalize_target(raw_target)
                classification = classify_target(target)

                if classification == "external":
                    results.append(
                        LinkResult(
                            source=source,
                            line=line_number,
                            raw_target=raw_target,
                            target=target,
                            classification=classification,
                            status="alive",
                            resolution="external",
                            fragment=None,
                            resolved_path=None,
                            target_document=None,
                        )
                    )
                    continue

                if classification == "anchor":
                    results.append(
                        LinkResult(
                            source=source,
                            line=line_number,
                            raw_target=raw_target,
                            target=target,
                            classification=classification,
                            status="alive",
                            resolution="anchor",
                            fragment=target[1:] if len(target) > 1 else None,
                            resolved_path=source,
                            target_document=source,
                        )
                    )
                    continue

                base_target, fragment = split_fragment(target)
                resolved, resolution = resolve_local_link(repo_root, source, base_target, slug_map)
                status = "alive" if resolved is not None else "dead"
                target_document = None
                if resolved is not None and normalize_path(resolved) in markdown_set:
                    target_document = resolved

                results.append(
                    LinkResult(
                        source=source,
                        line=line_number,
                        raw_target=raw_target,
                        target=target,
                        classification=classification,
                        status=status,
                        resolution=resolution,
                        fragment=fragment,
                        resolved_path=resolved,
                        target_document=target_document,
                    )
                )

    return results


def possible_link_forms(
    path: pathlib.Path,
    repo_root: pathlib.Path,
    h1: str | None,
) -> list[str]:
    rel = repo_relative_or_absolute(path, repo_root)
    stem_rel = rel[:-3] if rel.endswith(".md") else rel
    name = path.name
    stem_name = path.stem
    forms = {
        rel,
        stem_rel,
        f"/{rel}",
        f"/{stem_rel}",
        name,
        stem_name,
        f"./{name}",
        f"./{stem_name}",
    }
    if h1:
        forms.add(f"{slugify(h1)}/")
    return sorted(item for item in forms if item)


def build_report(
    repo_root: pathlib.Path,
    markdown_files: list[pathlib.Path],
    links: list[LinkResult],
    opportunities: list[LinkOpportunity],
) -> dict[str, Any]:
    doc_h1: dict[pathlib.Path, str | None] = {}
    for path in markdown_files:
        doc_h1[path] = first_h1(read_text(path))

    outgoing: dict[pathlib.Path, list[LinkResult]] = {path: [] for path in markdown_files}
    incoming: dict[pathlib.Path, list[LinkResult]] = {path: [] for path in markdown_files}
    outgoing_opportunities: dict[pathlib.Path, list[LinkOpportunity]] = {
        path: [] for path in markdown_files
    }
    incoming_opportunities: dict[pathlib.Path, list[LinkOpportunity]] = {
        path: [] for path in markdown_files
    }
    dead_links: list[LinkResult] = []

    for link in links:
        outgoing[link.source].append(link)
        if link.status == "dead" and link.classification == "local":
            dead_links.append(link)
        if link.target_document is not None:
            incoming.setdefault(link.target_document, []).append(link)

    for opportunity in opportunities:
        outgoing_opportunities.setdefault(opportunity.source, []).append(opportunity)
        if opportunity.target_document is not None:
            incoming_opportunities.setdefault(opportunity.target_document, []).append(opportunity)

    documents: list[dict[str, Any]] = []
    for path in markdown_files:
        outgoing_links = outgoing.get(path, [])
        incoming_links = incoming.get(path, [])
        document_outgoing_opportunities = outgoing_opportunities.get(path, [])
        document_incoming_opportunities = incoming_opportunities.get(path, [])
        h1 = doc_h1.get(path)
        documents.append(
            {
                "path": repo_relative_or_absolute(path, repo_root),
                "h1": h1,
                "slug": slugify(h1) if h1 else None,
                "possible_link_forms": possible_link_forms(path, repo_root, h1),
                "possible_search_tokens": possible_link_forms(path, repo_root, h1),
                "outgoing_links": [
                    {
                        "line": item.line,
                        "raw_target": item.raw_target,
                        "target": item.target,
                        "classification": item.classification,
                        "status": item.status,
                        "resolution": item.resolution,
                        "fragment": item.fragment,
                        "resolved_path": (
                            repo_relative_or_absolute(item.resolved_path, repo_root)
                            if item.resolved_path is not None
                            else None
                        ),
                        "target_document": (
                            repo_relative_or_absolute(item.target_document, repo_root)
                            if item.target_document is not None
                            else None
                        ),
                    }
                    for item in outgoing_links
                ],
                "incoming_links": [
                    {
                        "source": repo_relative_or_absolute(item.source, repo_root),
                        "line": item.line,
                        "raw_target": item.raw_target,
                        "target": item.target,
                        "classification": item.classification,
                        "status": item.status,
                        "resolution": item.resolution,
                    }
                    for item in incoming_links
                ],
                "outgoing_link_opportunities": [
                    {
                        "line": item.line,
                        "kind": item.kind,
                        "link_style": item.link_style,
                        "raw_text": item.raw_text,
                        "candidate": item.candidate,
                        "resolution": item.resolution,
                        "resolved_path": repo_relative_or_absolute(item.resolved_path, repo_root),
                        "target_document": (
                            repo_relative_or_absolute(item.target_document, repo_root)
                            if item.target_document is not None
                            else None
                        ),
                        "suggested_markdown_link": suggested_markdown_link(
                            source=item.source,
                            resolved_path=item.resolved_path,
                            target_document=item.target_document,
                            target_h1=doc_h1.get(item.target_document) if item.target_document else None,
                        ),
                    }
                    for item in document_outgoing_opportunities
                ],
                "incoming_link_opportunities": [
                    {
                        "source": repo_relative_or_absolute(item.source, repo_root),
                        "line": item.line,
                        "kind": item.kind,
                        "link_style": item.link_style,
                        "raw_text": item.raw_text,
                        "candidate": item.candidate,
                        "resolution": item.resolution,
                        "resolved_path": repo_relative_or_absolute(item.resolved_path, repo_root),
                    }
                    for item in document_incoming_opportunities
                ],
                "stats": {
                    "outgoing_total": len(outgoing_links),
                    "outgoing_dead_local": sum(
                        1
                        for item in outgoing_links
                        if item.classification == "local" and item.status == "dead"
                    ),
                    "incoming_total": len(incoming_links),
                    "outgoing_link_opportunities_total": len(document_outgoing_opportunities),
                    "incoming_link_opportunities_total": len(document_incoming_opportunities),
                },
            }
        )

    total_local = sum(1 for link in links if link.classification == "local")
    dead_local = sum(
        1 for link in links if link.classification == "local" and link.status == "dead"
    )

    report = {
        "generated_at_utc": (
            dt.datetime.now(dt.UTC).replace(microsecond=0).isoformat().replace("+00:00", "Z")
        ),
        "repo_root": str(normalize_path(repo_root)),
        "summary": {
            "markdown_files": len(markdown_files),
            "links_total": len(links),
            "links_local": total_local,
            "links_external": sum(1 for link in links if link.classification == "external"),
            "links_anchor": sum(1 for link in links if link.classification == "anchor"),
            "links_local_alive": total_local - dead_local,
            "links_local_dead": dead_local,
            "link_opportunities_total": len(opportunities),
            "link_opportunities_to_markdown_docs": sum(
                1 for item in opportunities if item.target_document is not None
            ),
            "link_opportunities_structurizr_slug": sum(
                1 for item in opportunities if item.link_style == "structurizr_slug"
            ),
            "link_opportunities_standard_path": sum(
                1 for item in opportunities if item.link_style == "standard_path"
            ),
        },
        "dead_links": [
            {
                "source": repo_relative_or_absolute(link.source, repo_root),
                "line": link.line,
                "raw_target": link.raw_target,
                "target": link.target,
                "resolution": link.resolution,
            }
            for link in dead_links
        ],
        "link_opportunities": [
            {
                "source": repo_relative_or_absolute(item.source, repo_root),
                "line": item.line,
                "kind": item.kind,
                "link_style": item.link_style,
                "raw_text": item.raw_text,
                "candidate": item.candidate,
                "resolution": item.resolution,
                "resolved_path": repo_relative_or_absolute(item.resolved_path, repo_root),
                "target_document": (
                    repo_relative_or_absolute(item.target_document, repo_root)
                    if item.target_document is not None
                    else None
                ),
                "suggested_markdown_link": suggested_markdown_link(
                    source=item.source,
                    resolved_path=item.resolved_path,
                    target_document=item.target_document,
                    target_h1=doc_h1.get(item.target_document) if item.target_document else None,
                ),
            }
            for item in opportunities
        ],
        "documents": documents,
    }
    return report


def render_markdown_report(report: dict[str, Any]) -> str:
    lines: list[str] = []
    summary = report["summary"]
    dead_links = report["dead_links"]
    link_opportunities = report["link_opportunities"]

    lines.append("# Markdown Link Report")
    lines.append("")
    lines.append(f"- Generated: `{report['generated_at_utc']}`")
    lines.append(f"- Repo root: `{report['repo_root']}`")
    lines.append("")
    lines.append("## Summary")
    lines.append("")
    lines.append(f"- Markdown files: `{summary['markdown_files']}`")
    lines.append(f"- Total links: `{summary['links_total']}`")
    lines.append(f"- Local links: `{summary['links_local']}`")
    lines.append(f"- Local links (alive): `{summary['links_local_alive']}`")
    lines.append(f"- Local links (dead): `{summary['links_local_dead']}`")
    lines.append(f"- External links: `{summary['links_external']}`")
    lines.append(f"- Anchor links: `{summary['links_anchor']}`")
    lines.append(f"- Link opportunities (not linked yet): `{summary['link_opportunities_total']}`")
    lines.append(
        f"- Link opportunities (Structurizr slug style): `{summary['link_opportunities_structurizr_slug']}`"
    )
    lines.append(
        f"- Link opportunities (standard path style): `{summary['link_opportunities_standard_path']}`"
    )
    lines.append("")
    lines.append("## Dead Local Links")
    lines.append("")
    if not dead_links:
        lines.append("No dead local links found.")
    else:
        for link in dead_links:
            lines.append(
                f"- `{link['source']}:{link['line']}` -> `{link['target']}` (`{link['resolution']}`)"
            )

    lines.append("")
    lines.append("## Link Opportunities")
    lines.append("")
    if not link_opportunities:
        lines.append("No link opportunities detected.")
    else:
        display = link_opportunities[:MAX_OPPORTUNITIES_IN_MARKDOWN_REPORT]
        for item in display:
            lines.append(
                f"- `{item['source']}:{item['line']}` `{item['candidate']}` "
                f"-> `{item['resolved_path']}` [{item['link_style']}] "
                f"suggested: `{item['suggested_markdown_link']}`"
            )
        if len(link_opportunities) > MAX_OPPORTUNITIES_IN_MARKDOWN_REPORT:
            lines.append(
                f"- Truncated: showing first {MAX_OPPORTUNITIES_IN_MARKDOWN_REPORT} of {len(link_opportunities)} opportunities."
            )

    lines.append("")
    lines.append("## Documents")
    lines.append("")

    for doc in report["documents"]:
        lines.append(f"### `{doc['path']}`")
        if doc.get("h1"):
            lines.append(f"- H1: `{doc['h1']}`")
        lines.append(f"- Outgoing links: `{doc['stats']['outgoing_total']}`")
        lines.append(f"- Incoming links: `{doc['stats']['incoming_total']}`")
        lines.append(f"- Dead outgoing local links: `{doc['stats']['outgoing_dead_local']}`")
        lines.append(
            f"- Outgoing link opportunities: `{doc['stats']['outgoing_link_opportunities_total']}`"
        )
        lines.append(
            f"- Incoming link opportunities: `{doc['stats']['incoming_link_opportunities_total']}`"
        )
        lines.append("- Possible link/search forms:")
        for token in doc["possible_search_tokens"]:
            lines.append(f"  - `{token}`")
        lines.append("")

    return "\n".join(lines).rstrip() + "\n"


def write_text(path: pathlib.Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8")


def write_json(path: pathlib.Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2), encoding="utf-8")


def main() -> int:
    args = parse_args()
    repo_root = normalize_path(pathlib.Path(args.repo_root))
    exclude_segments = list(dict.fromkeys(DEFAULT_EXCLUDE_SEGMENTS + args.exclude_segment))

    markdown_files = list_markdown_files(repo_root, exclude_segments)
    slug_map = build_slug_map(markdown_files)
    links = collect_links(repo_root, markdown_files, slug_map)
    opportunities = collect_link_opportunities(repo_root, markdown_files, slug_map)
    report = build_report(repo_root, markdown_files, links, opportunities)

    json_out = resolve_output_path(repo_root, args.json_out)
    md_out = resolve_output_path(repo_root, args.markdown_out)
    write_json(json_out, report)
    write_text(md_out, render_markdown_report(report))

    dead_count = report["summary"]["links_local_dead"]
    print(f"Markdown files scanned: {report['summary']['markdown_files']}")
    print(f"Links scanned: {report['summary']['links_total']}")
    print(f"Dead local links: {dead_count}")
    print(f"Link opportunities: {report['summary']['link_opportunities_total']}")
    print(
        "Structurizr slug opportunities: "
        f"{report['summary']['link_opportunities_structurizr_slug']}"
    )
    print(f"JSON report: {json_out}")
    print(f"Markdown report: {md_out}")

    if args.fail_on_dead and dead_count > 0:
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
