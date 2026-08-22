#!/usr/bin/env python3
"""Builds the deterministic runtime snapshot from the canonical Excel workbook."""

from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
VALIDATOR_PATH = ROOT / "scripts" / "validate-master-data.py"
WORKBOOK = ROOT / "config" / "product-master-data.xlsx"
SCHEMA_PATH = ROOT / "config" / "master-data-schema.json"
IMAGE_CATALOG = ROOT / "config" / "catalog-images" / "image-catalog.json"
OUTPUT = ROOT / "config" / "runtime" / "masterdata-runtime.json"


def load_validator():
    spec = importlib.util.spec_from_file_location("masterdata_validator", VALIDATOR_PATH)
    if spec is None or spec.loader is None:
        raise RuntimeError("Masterdatavalidator kan niet worden geladen")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def build_snapshot() -> dict:
    validator = load_validator()
    sheets = validator.read_workbook(WORKBOOK)
    schema = json.loads(SCHEMA_PATH.read_text(encoding="utf-8"))
    images = json.loads(IMAGE_CATALOG.read_text(encoding="utf-8"))

    table_specs = {
        "products": ("Producten", "Product-ID"),
        "materials": ("Materialen", "Materiaal-ID"),
        "components": ("Componenten", "Component-ID"),
        "tools": ("Gereedschappen", "Gereedschap-ID"),
        "connectionRecipes": ("Verbindingsrecepten", "Recept-ID"),
        "productRules": ("Product-regels", "Regel-ID"),
        "suppliers": ("Leveranciers", "Leverancier-ID"),
        "supplierPreferences": ("Leveranciers", "Voorkeur-ID"),
        "offers": ("Prijs & inkoop", "Aanbieding-ID"),
        "validationRules": ("Controles", "Controle-ID"),
        "changeLog": ("Wijzigingslog", "Wijziging-ID"),
        "camParameters": ("CAM-parameters", "Sleutel"),
    }
    tables = {
        name: validator.table(sheets[sheet], primary_key)
        for name, (sheet, primary_key) in table_specs.items()
    }
    return {
        "schemaVersion": schema.get("schemaVersion", ""),
        "source": {
            "workbook": WORKBOOK.relative_to(ROOT).as_posix(),
            "sha256": hashlib.sha256(WORKBOOK.read_bytes()).hexdigest().upper(),
            "imageCatalog": IMAGE_CATALOG.relative_to(ROOT).as_posix(),
        },
        "tables": tables,
        "images": images.get("images", []),
    }


def serialize(snapshot: dict) -> str:
    return json.dumps(snapshot, ensure_ascii=False, indent=2, sort_keys=False) + "\n"


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--check", action="store_true", help="Fail when the committed snapshot is stale")
    args = parser.parse_args()

    expected = serialize(build_snapshot())
    if args.check:
        if not OUTPUT.is_file():
            print(f"RUNTIME-SNAPSHOT ONTBREEKT: {OUTPUT.relative_to(ROOT)}")
            return 1
        if OUTPUT.read_text(encoding="utf-8") != expected:
            print("RUNTIME-SNAPSHOT VEROUDERD: voer scripts/generate-masterdata-runtime.py uit")
            return 1
        print("RUNTIME-SNAPSHOT ACTUEEL")
        return 0

    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT.write_text(expected, encoding="utf-8", newline="\n")
    print(f"RUNTIME-SNAPSHOT GESCHREVEN: {OUTPUT.relative_to(ROOT)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
