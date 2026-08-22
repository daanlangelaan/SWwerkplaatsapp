#!/usr/bin/env python3
"""Validates the canonical product-master workbook and external image registry."""

from __future__ import annotations

import json
import sys
import zipfile
from pathlib import Path
from xml.etree import ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
WORKBOOK = ROOT / "config" / "product-master-data.xlsx"
IMAGE_CATALOG = ROOT / "config" / "catalog-images" / "image-catalog.json"
SCHEMA = ROOT / "config" / "master-data-schema.json"
MAIN_NS = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
REL_NS = "http://schemas.openxmlformats.org/package/2006/relationships"
DOC_REL_NS = "http://schemas.openxmlformats.org/officeDocument/2006/relationships"


def column_index(reference: str) -> int:
    value = 0
    for char in reference:
        if not ("A" <= char <= "Z"):
            break
        value = value * 26 + ord(char) - ord("A") + 1
    return value - 1


def read_workbook(path: Path) -> dict[str, list[list[str]]]:
    with zipfile.ZipFile(path) as archive:
        shared: list[str] = []
        if "xl/sharedStrings.xml" in archive.namelist():
            root = ET.fromstring(archive.read("xl/sharedStrings.xml"))
            for item in root.findall(f"{{{MAIN_NS}}}si"):
                shared.append("".join(node.text or "" for node in item.iter(f"{{{MAIN_NS}}}t")))

        workbook = ET.fromstring(archive.read("xl/workbook.xml"))
        rels = ET.fromstring(archive.read("xl/_rels/workbook.xml.rels"))
        targets = {node.attrib["Id"]: node.attrib["Target"] for node in rels.findall(f"{{{REL_NS}}}Relationship")}
        result: dict[str, list[list[str]]] = {}
        sheets = workbook.find(f"{{{MAIN_NS}}}sheets")
        for sheet in sheets if sheets is not None else []:
            name = sheet.attrib["name"]
            target = targets[sheet.attrib[f"{{{DOC_REL_NS}}}id"]].replace("\\", "/").lstrip("/")
            if not target.startswith("xl/"):
                target = "xl/" + target
            xml = ET.fromstring(archive.read(target))
            rows: list[list[str]] = []
            for row_node in xml.findall(f".//{{{MAIN_NS}}}sheetData/{{{MAIN_NS}}}row"):
                cells: dict[int, str] = {}
                for cell in row_node.findall(f"{{{MAIN_NS}}}c"):
                    index = column_index(cell.attrib.get("r", ""))
                    cell_type = cell.attrib.get("t", "")
                    if cell_type == "inlineStr":
                        value = "".join(node.text or "" for node in cell.iter(f"{{{MAIN_NS}}}t"))
                    else:
                        node = cell.find(f"{{{MAIN_NS}}}v")
                        value = node.text if node is not None and node.text is not None else ""
                        if cell_type == "s" and value.isdigit() and int(value) < len(shared):
                            value = shared[int(value)]
                    cells[index] = value
                width = max(cells, default=-1) + 1
                rows.append([cells.get(index, "") for index in range(width)])
            result[name] = rows
        return result


def table(rows: list[list[str]], key_header: str) -> list[dict[str, str]]:
    header_index = next((index for index, row in enumerate(rows) if key_header in row), None)
    if header_index is None:
        raise ValueError(f"Kolomkop {key_header!r} ontbreekt")
    headers = rows[header_index]
    result = []
    for row in rows[header_index + 1 :]:
        values = {header: (row[index] if index < len(row) else "") for index, header in enumerate(headers) if header}
        if not values.get(key_header):
            break
        result.append(values)
    return result


def unique(records: list[dict[str, str]], key: str, errors: list[str]) -> set[str]:
    values = [record.get(key, "").strip() for record in records]
    for value in sorted({value for value in values if values.count(value) > 1}):
        errors.append(f"Dubbele {key}: {value}")
    if any(not value for value in values):
        errors.append(f"Lege {key}")
    return set(values)


def main() -> int:
    errors: list[str] = []
    sheets = read_workbook(WORKBOOK)
    schema = json.loads(SCHEMA.read_text(encoding="utf-8"))
    if schema.get("workbook") != WORKBOOK.relative_to(ROOT).as_posix():
        errors.append("Schema verwijst niet naar config/product-master-data.xlsx")
    for table_name, contract in schema.get("canonicalTables", {}).items():
        sheet_name = contract.get("sheet", "")
        primary_key = contract.get("primaryKey", "")
        if sheet_name not in sheets:
            errors.append(f"Schema-tabel {table_name} verwijst naar ontbrekend werkblad {sheet_name}")
            continue
        try:
            table(sheets[sheet_name], primary_key)
        except ValueError:
            errors.append(f"Schema-tabel {table_name} mist primaire sleutel {primary_key} op {sheet_name}")
    for forbidden in schema.get("forbiddenWorksheets", []):
        if forbidden in sheets:
            errors.append(f"Verboden legacy-tab aanwezig: {forbidden}")

    products = table(sheets["Producten"], "Product-ID")
    materials = table(sheets["Materialen"], "Materiaal-ID")
    components = table(sheets["Componenten"], "Component-ID")
    tools = table(sheets["Gereedschappen"], "Gereedschap-ID")
    recipes = table(sheets["Verbindingsrecepten"], "Recept-ID")
    rules = table(sheets["Product-regels"], "Regel-ID")
    suppliers = table(sheets["Leveranciers"], "Leverancier-ID")
    preferences = table(sheets["Leveranciers"], "Voorkeur-ID")
    offers = table(sheets["Prijs & inkoop"], "Aanbieding-ID")

    product_ids = unique(products, "Product-ID", errors)
    material_ids = unique(materials, "Materiaal-ID", errors)
    component_ids = unique(components, "Component-ID", errors)
    tool_ids = unique(tools, "Gereedschap-ID", errors)
    recipe_ids = unique(recipes, "Recept-ID", errors)
    supplier_ids = unique(suppliers, "Leverancier-ID", errors)
    unique(rules, "Regel-ID", errors)
    unique(preferences, "Voorkeur-ID", errors)
    unique(offers, "Aanbieding-ID", errors)

    for product in products:
        base = product.get("Basisproduct-ID", "").strip()
        if base and base not in product_ids:
            errors.append(f"Product {product['Product-ID']} verwijst naar onbekend basisproduct {base}")

    references = {
        "Materiaal": material_ids,
        "Component": component_ids,
        "Gereedschap": tool_ids,
        "Leverancier": supplier_ids,
        "Product": product_ids,
    }
    for rule in rules:
        if rule.get("Product-ID", "") not in product_ids:
            errors.append(f"Regel {rule['Regel-ID']} heeft onbekend Product-ID")
        reference_type = rule.get("Referentietype", "")
        ids = [value.strip() for value in rule.get("Referentie-ID(s)", "").split(";") if value.strip()]
        if reference_type in references:
            for value in ids:
                if value not in references[reference_type]:
                    errors.append(f"Regel {rule['Regel-ID']} heeft onbekende {reference_type}-ID {value}")
        recipe = rule.get("Recept-ID", "").strip()
        if recipe and recipe not in recipe_ids:
            errors.append(f"Regel {rule['Regel-ID']} heeft onbekend Recept-ID {recipe}")

    active_ranks: set[tuple[str, ...]] = set()
    for preference in preferences:
        supplier = preference.get("Leverancier-ID", "")
        if supplier not in supplier_ids:
            errors.append(f"Voorkeur {preference['Voorkeur-ID']} heeft onbekende leverancier {supplier}")
        if preference.get("Scope-type") == "Product" and preference.get("Scope-ID") not in product_ids:
            errors.append(f"Voorkeur {preference['Voorkeur-ID']} heeft onbekende productscope")
        if preference.get("Status") == "Actief":
            signature = tuple(preference.get(key, "") for key in ("Categorie", "Subcategorie", "Scope-type", "Scope-ID", "Rang"))
            if signature in active_ranks:
                errors.append(f"Dubbele actieve leveranciersrang: {signature}")
            active_ranks.add(signature)

    image_data = json.loads(IMAGE_CATALOG.read_text(encoding="utf-8"))
    image_records = image_data.get("images", [])
    image_ids = unique(image_records, "imageId", errors)
    for image in image_records:
        path = ROOT / image.get("localPath", "")
        if not path.is_file():
            errors.append(f"Afbeeldingsbestand ontbreekt: {image.get('localPath')}")
        if image.get("supplierId") not in supplier_ids:
            errors.append(f"Afbeelding {image.get('imageId')} heeft onbekende leverancier")

    for offer in offers:
        supplier = offer.get("Leverancier-ID", "").strip()
        if supplier and supplier not in supplier_ids:
            errors.append(f"Aanbieding {offer['Aanbieding-ID']} heeft onbekende leverancier {supplier}")
        image_id = offer.get("Afbeelding-ID", "").strip()
        if image_id and image_id not in image_ids:
            errors.append(f"Aanbieding {offer['Aanbieding-ID']} heeft onbekende afbeelding {image_id}")
        record_type = offer.get("Recordtype", "")
        internal_id = offer.get("Interne-ID", "")
        if record_type == "Materiaal" and internal_id not in material_ids:
            errors.append(f"Aanbieding {offer['Aanbieding-ID']} heeft onbekend Materiaal-ID {internal_id}")
        if record_type == "Component" and internal_id not in component_ids:
            errors.append(f"Aanbieding {offer['Aanbieding-ID']} heeft onbekend Component-ID {internal_id}")

    if errors:
        print("MASTERDATA ONGELDIG")
        for error in errors:
            print("- " + error)
        return 1
    print(
        "MASTERDATA GELDIG: "
        f"{len(products)} producten, {len(suppliers)} leveranciers, "
        f"{len(preferences)} voorkeuren, {len(offers)} aanbiedingen, {len(image_records)} afbeeldingen"
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
