"""Shared Johnny.Decimal helpers: patterns, config loading, validation."""

from __future__ import annotations

import json
import re
from pathlib import Path
from typing import List

# 0x = root metadata for the whole J.D tree; 1x–9x = content areas
AREA_PATTERN = re.compile(r"^([0-9])x$")


def category_pattern(area_num: int) -> re.Pattern:
    return re.compile(r"^" + str(area_num) + r"[0-9]$")


ID_PATTERN = re.compile(r"^(\d{2})\.(\d{2})$")
DEFAULT_CONFIG_PATH = "scripts/johnny-decimal-config.json"


def load_roots_from_config(repo_root: Path, config_path: str) -> List[Path]:
    path = (repo_root / config_path).resolve()
    if not path.exists():
        return []
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except (json.JSONDecodeError, OSError):
        return []
    roots = data.get("roots") if isinstance(data, dict) else None
    if not isinstance(roots, list) or not roots:
        return []
    return [(repo_root / r).resolve() for r in roots if isinstance(r, str)]


def validate_area_name(name: str) -> bool:
    return bool(AREA_PATTERN.match(name))


def validate_category_name(area_num: int, name: str) -> bool:
    return bool(category_pattern(area_num).match(name)) and len(name) == 2


def validate_id_folder(category: str, name: str) -> bool:
    m = ID_PATTERN.match(name)
    if not m:
        return False
    return m.group(1) == category
