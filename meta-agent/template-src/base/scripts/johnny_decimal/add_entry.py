"""Johnny.Decimal add-entry logic: add area/category/id, store document in an ID folder.

Used by add-johnny-decimal-entry.py CLI and by tests.
"""

from __future__ import annotations

import shutil
from dataclasses import dataclass
from pathlib import Path
from typing import Any

from johnny_decimal.shared import AREA_PATTERN, ID_PATTERN


@dataclass
class Result:
    success: bool
    relative_path: str = ""
    id: str = ""  # 2x, 22, 22.01
    name: str = ""
    description: str = ""
    message: str = ""
    failure_reason: str = ""

    def to_dict(self) -> dict[str, Any]:
        return {
            "success": self.success,
            "relative_path": self.relative_path,
            "id": self.id,
            "name": self.name,
            "description": self.description,
            "message": self.message,
            "failure_reason": self.failure_reason or None,
        }


def _next_available_area(internal: Path) -> str | None:
    """First missing area in 1x..9x."""
    for n in range(1, 10):
        area = f"{n}x"
        if not (internal / area).is_dir():
            return area
    return None


def _next_available_category(internal: Path, area: str) -> str | None:
    """Next content category in area (N1..N9; N0 is metadata)."""
    if not AREA_PATTERN.match(area):
        return None
    area_num = int(area[0])
    area_path = internal / area
    if not area_path.is_dir():
        return None
    used = set()
    for d in area_path.iterdir():
        if not d.is_dir():
            continue
        if len(d.name) == 2 and d.name.isdigit():
            n = int(d.name)
            if n // 10 == area_num:
                used.add(n % 10)
    for i in range(1, 10):
        if i not in used:
            return f"{area_num}{i}"
    return None


def _next_available_id(internal: Path, category: str) -> str | None:
    """Next content ID in category. NN.00 is reserved for category metadata; returns .01, .02, ..."""
    if not (len(category) == 2 and category.isdigit()):
        return None
    area = f"{category[0]}x"
    cat_path = internal / area / category
    if not cat_path.is_dir():
        return None
    used = set()
    for d in cat_path.iterdir():
        if not d.is_dir():
            continue
        m = ID_PATTERN.match(d.name)
        if m and m.group(1) == category:
            used.add(int(m.group(2)))
    # .00 is reserved for category metadata; content IDs are .01, .02, ...
    for mm in range(1, 100):
        if mm not in used:
            return f"{category}.{mm:02d}"
    return None


def _relpath(path: Path, base: Path) -> str:
    try:
        return path.relative_to(base).as_posix()
    except ValueError:
        return path.as_posix()


def _append_to_root_index(internal: Path, area_id: str, title: str) -> bool:
    """Append area link to root index.md under ## Active Areas. Returns True if updated."""
    index_path = internal / "index.md"
    if not index_path.exists():
        return False
    text = index_path.read_text(encoding="utf-8")
    marker = "## Active Areas"
    if marker not in text:
        return False
    new_line = f"- [{area_id} {title}]({area_id}/index.md)"
    lines = text.splitlines()
    in_section = False
    last_bullet_idx = -1
    for i, line in enumerate(lines):
        if marker in line:
            in_section = True
            continue
        if in_section and line.strip().startswith("##"):
            break
        if in_section and line.strip().startswith("- ["):
            last_bullet_idx = i
    if last_bullet_idx >= 0:
        lines.insert(last_bullet_idx + 1, new_line)
    else:
        for i, line in enumerate(lines):
            if marker in line:
                lines.insert(i + 1, new_line)
                break
    index_path.write_text("\n".join(lines) + "\n", encoding="utf-8")
    return True


def _append_to_area_index(internal: Path, area_id: str, category_id: str, title: str) -> bool:
    """Append category link to area index.md under ## Categories."""
    index_path = internal / area_id / "index.md"
    if not index_path.exists():
        return False
    text = index_path.read_text(encoding="utf-8")
    marker = "## Categories"
    if marker not in text:
        return False
    new_line = f"- [{category_id} {title}]({category_id}/index.md)"
    lines = text.splitlines()
    in_section = False
    insert_after = -1
    for i, line in enumerate(lines):
        if marker in line:
            in_section = True
            insert_after = i
            continue
        if in_section and line.strip().startswith("##"):
            break
        if in_section and line.strip().startswith("- ["):
            insert_after = i
    if insert_after >= 0:
        lines.insert(insert_after + 1, new_line)
        index_path.write_text("\n".join(lines) + "\n", encoding="utf-8")
        return True
    return False


def _append_to_category_index(internal: Path, area_id: str, category_id: str, id_val: str, title: str) -> bool:
    """Append ID link to category index.md under ## IDs in this category."""
    index_path = internal / area_id / category_id / "index.md"
    if not index_path.exists():
        return False
    text = index_path.read_text(encoding="utf-8")
    marker = "## IDs in this category"
    if marker not in text:
        return False
    new_line = f"- **[{id_val}]({id_val}/index.md)** — {title}"
    lines = text.splitlines()
    in_section = False
    insert_after = -1
    for i, line in enumerate(lines):
        if marker in line:
            in_section = True
            insert_after = i
            continue
        if in_section and line.strip().startswith("##"):
            break
        if in_section and (line.strip().startswith("- **[") or line.strip().startswith("- ")):
            insert_after = i
    if insert_after >= 0:
        lines.insert(insert_after + 1, new_line)
        index_path.write_text("\n".join(lines) + "\n", encoding="utf-8")
        return True
    return False


def add_area(
    internal_root: Path,
    title: str,
    description: str,
    repo_base: Path | None = None,
    dry_run: bool = False,
    area_id: str | None = None,
) -> Result:
    """Add a new area. By default picks next free 1x..9x; pass area_id='0x' to create root metadata area."""
    if not internal_root.is_dir():
        return Result(
            success=False,
            message="Internal root not found.",
            failure_reason="internal_root_not_found",
        )
    if area_id is None:
        area_id = _next_available_area(internal_root)
        if not area_id:
            return Result(
                success=False,
                message="No area slot left (1x–9x all exist).",
                failure_reason="no_area_available",
            )
    else:
        if area_id != "0x":
            return Result(
                success=False,
                message="Optional area_id must be '0x' for root metadata.",
                failure_reason="invalid_area_id",
            )
        if (internal_root / "0x").is_dir():
            return Result(
                success=False,
                message="Area 0x (root metadata) already exists.",
                failure_reason="area_exists",
            )
    base = repo_base or internal_root
    rel = _relpath(internal_root / area_id, base)
    if dry_run:
        return Result(
            success=True,
            relative_path=rel,
            id=area_id,
            name=title,
            description=description,
            message="Would create area and add to root index (dry-run).",
        )
    area_num = int(area_id[0])
    meta_cat = f"{area_num}0"
    area_path = internal_root / area_id
    area_path.mkdir(parents=True, exist_ok=True)
    area_index = f"""# {area_id} {title}

This **area** holds internal documentation for **{description}**.

**Johnny.Decimal in {area_id}**: Area **{area_id}** → **Categories** {meta_cat} (metadata), {area_num}1–{area_num}9 (content) → **IDs** e.g. {area_num}1.01.

## Categories

- [{meta_cat} About the {area_id} sub-structure]({meta_cat}/index.md) — Metadata and rules for this area.

"""
    (area_path / "index.md").write_text(area_index, encoding="utf-8")
    (area_path / meta_cat).mkdir(parents=True, exist_ok=True)
    meta_index = f"""# {meta_cat} About The {area_id} Sub-Structure

This category is the **metadata and rules** bucket for the `{area_id}` area.

## Active `{area_id}` categories

- (Add links to category indexes.)

"""
    (area_path / meta_cat / "index.md").write_text(meta_index, encoding="utf-8")
    index_updated = _append_to_root_index(internal_root, area_id, title)
    return Result(
        success=True,
        relative_path=rel,
        id=area_id,
        name=title,
        description=description,
        message="Created area and added to root index." if index_updated else "Created area; root index not updated (edit manually).",
    )


def add_category(
    internal_root: Path,
    area_id: str,
    title: str,
    description: str,
    repo_base: Path | None = None,
    dry_run: bool = False,
) -> Result:
    """Add a new category under an area. Requires area (e.g. 2x), title, description. Picks next category and updates area index."""
    if not internal_root.is_dir():
        return Result(success=False, message="Internal root not found.", failure_reason="internal_root_not_found")
    if not AREA_PATTERN.match(area_id):
        return Result(success=False, message=f"Invalid area '{area_id}'.", failure_reason="invalid_area")
    area_path = internal_root / area_id
    if not area_path.is_dir():
        return Result(success=False, message=f"Area '{area_id}' does not exist.", failure_reason="area_missing")
    cat_id = _next_available_category(internal_root, area_id)
    if not cat_id:
        return Result(success=False, message=f"No category slot left in {area_id}.", failure_reason="no_category_available")
    base = repo_base or internal_root
    rel = _relpath(internal_root / area_id / cat_id, base)
    if dry_run:
        return Result(
            success=True,
            relative_path=rel,
            id=cat_id,
            name=title,
            description=description,
            message="Would create category and add to area index (dry-run).",
        )
    cat_path = internal_root / area_id / cat_id
    cat_path.mkdir(parents=True, exist_ok=True)
    meta_id = f"{cat_id}.00"
    index_md = f"""# {cat_id} {title}

This category holds documentation for **{description}**.

**Johnny.Decimal in this category**: **{meta_id}** (metadata for this category) → **{cat_id}.01**–**{cat_id}.99** (content).

## IDs in this category

- **[{meta_id} About this category (metadata)]({meta_id}/index.md)** — Rules and index for this category.

"""
    (cat_path / "index.md").write_text(index_md, encoding="utf-8")
    # Create NN.00 metadata folder for the category (like N0 for area)
    (cat_path / meta_id).mkdir(parents=True, exist_ok=True)
    meta_index = f"""# {meta_id} About This Category

This ID folder is the **metadata and rules** bucket for category **{cat_id}**.

## Contents

- Category-level rules and index (add links to content IDs as you add them).

"""
    (cat_path / meta_id / "index.md").write_text(meta_index, encoding="utf-8")
    index_updated = _append_to_area_index(internal_root, area_id, cat_id, title)
    return Result(
        success=True,
        relative_path=rel,
        id=cat_id,
        name=title,
        description=description,
        message="Created category and added to area index." if index_updated else "Created category; area index not updated (edit manually).",
    )


def add_id(
    internal_root: Path,
    category_id: str,
    title: str,
    description: str,
    repo_base: Path | None = None,
    dry_run: bool = False,
) -> Result:
    """Add a new ID under a category. Requires category (e.g. 22), title, description. Infers area; picks next ID; updates category index."""
    if not internal_root.is_dir():
        return Result(success=False, message="Internal root not found.", failure_reason="internal_root_not_found")
    if not (len(category_id) == 2 and category_id.isdigit()):
        return Result(success=False, message=f"Invalid category '{category_id}'.", failure_reason="invalid_category")
    area_id = f"{category_id[0]}x"
    cat_path = internal_root / area_id / category_id
    if not cat_path.is_dir():
        return Result(success=False, message=f"Category '{category_id}' does not exist.", failure_reason="category_missing")
    id_val = _next_available_id(internal_root, category_id)
    if not id_val:
        return Result(success=False, message=f"No ID slot left in category {category_id}.", failure_reason="no_id_available")
    base = repo_base or internal_root
    rel = _relpath(internal_root / area_id / category_id / id_val, base)
    if dry_run:
        return Result(
            success=True,
            relative_path=rel,
            id=id_val,
            name=title,
            description=description,
            message="Would create ID and add to category index (dry-run).",
        )
    id_path = cat_path / id_val
    id_path.mkdir(parents=True, exist_ok=True)
    index_md = f"""# {id_val} {title}

This ID groups tightly related documents. *You can say "look in {id_val}" to refer to this folder.*

## Contents

- {description}

"""
    (id_path / "index.md").write_text(index_md, encoding="utf-8")
    index_updated = _append_to_category_index(internal_root, area_id, category_id, id_val, title)
    return Result(
        success=True,
        relative_path=rel,
        id=id_val,
        name=title,
        description=description,
        message="Created ID and added to category index." if index_updated else "Created ID; category index not updated (edit manually).",
    )


def store_document(
    internal_root: Path,
    id_val: str,
    file_path: Path,
    repo_base: Path | None = None,
    dry_run: bool = False,
) -> Result:
    """Copy a document into the given ID folder (e.g. 33.12). Safe: copy first, verify, then remove original. Returns relative path of new file and success."""
    if not internal_root.is_dir():
        return Result(success=False, message="Internal root not found.", failure_reason="internal_root_not_found")
    m = ID_PATTERN.match(id_val)
    if not m:
        return Result(success=False, message=f"Invalid ID '{id_val}' (expected NN.MM).", failure_reason="invalid_id")
    category_id = m.group(1)
    area_id = f"{category_id[0]}x"
    id_folder = internal_root / area_id / category_id / id_val
    if not (internal_root / area_id).is_dir():
        return Result(success=False, message=f"Area '{area_id}' does not exist.", failure_reason="area_missing")
    if not (internal_root / area_id / category_id).is_dir():
        return Result(success=False, message=f"Category '{category_id}' does not exist.", failure_reason="category_missing")
    if not id_folder.is_dir():
        return Result(success=False, message=f"ID folder '{id_val}' does not exist.", failure_reason="id_missing")
    src = Path(file_path).resolve()
    if not src.exists() or not src.is_file():
        return Result(success=False, message=f"File not found: {src}", failure_reason="file_not_found")
    dest = id_folder / src.name
    base = repo_base or internal_root
    rel = _relpath(dest, base)
    if dry_run:
        return Result(
            success=True,
            relative_path=rel,
            id=id_val,
            name=src.name,
            message="Would copy file to ID folder (dry-run).",
        )
    try:
        shutil.copy2(src, dest)
    except OSError as e:
        return Result(success=False, message=str(e), failure_reason="copy_failed")
    if not dest.exists() or not dest.is_file():
        return Result(success=False, message="Copy verification failed.", failure_reason="copy_verify_failed")
    try:
        src.unlink()
    except OSError as e:
        return Result(
            success=True,
            relative_path=rel,
            id=id_val,
            name=src.name,
            message=f"File copied to ID folder; could not remove original: {e}",
        )
    return Result(
        success=True,
        relative_path=rel,
        id=id_val,
        name=src.name,
        message="File moved to ID folder (copy verified, original removed).",
    )
