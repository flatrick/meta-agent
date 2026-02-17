"""Johnny.Decimal helpers: shared validation/config and add-entry logic.

Used by validate-johnny-decimal.py and add-johnny-decimal-entry.py.
"""

from johnny_decimal.shared import (
    DEFAULT_CONFIG_PATH,
    load_roots_from_config,
    validate_area_name,
    validate_category_name,
    validate_id_folder,
)
from johnny_decimal.add_entry import (
    add_area,
    add_category,
    add_id,
    store_document,
    Result,
)

__all__ = [
    "DEFAULT_CONFIG_PATH",
    "load_roots_from_config",
    "validate_area_name",
    "validate_category_name",
    "validate_id_folder",
    "add_area",
    "add_category",
    "add_id",
    "store_document",
    "Result",
]
