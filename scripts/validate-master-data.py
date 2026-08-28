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


def detect_image_extension(path: Path) -> str:
    with path.open("rb") as stream:
        signature = stream.read(12)
    if signature.startswith(b"\x89PNG\r\n\x1a\n"):
        return ".png"
    if signature.startswith(b"\xff\xd8\xff"):
        return ".jpg"
    return ""


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
    input_contracts = table(sheets["Product-invoer"], "Invoercontract-ID")
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
    unique(input_contracts, "Invoercontract-ID", errors)
    unique(preferences, "Voorkeur-ID", errors)
    unique(offers, "Aanbieding-ID", errors)

    card_contract = schema.get("productCardImageContract", {})
    card_path_field = card_contract.get("pathField", "Kaartafbeelding-pad")
    card_alt_field = card_contract.get("altField", "Kaartafbeelding-alt")
    card_source_field = card_contract.get("sourceField", "Kaartafbeelding-bron")
    card_revision_field = card_contract.get("revisionField", "Kaartafbeelding-revisie")
    card_status_field = card_contract.get("statusField", "Kaartafbeelding-status")
    card_available = card_contract.get("availableStatus", "Beschikbaar")
    card_missing = card_contract.get("missingStatus", "Ontbreekt")
    card_asset_root = ROOT / card_contract.get("assetRoot", "src/SWWerkplaats.Configurator/Portal")
    card_allowed_extensions = {str(value).lower() for value in card_contract.get("allowedExtensions", [])}
    if not card_allowed_extensions:
        errors.append("Schema mist productCardImageContract.allowedExtensions")
    for product in products:
        product_id = product["Product-ID"]
        status = product.get(card_status_field, "").strip()
        if status not in {card_available, card_missing}:
            errors.append(f"Product {product_id} heeft ongeldige {card_status_field}: {status!r}")
            continue
        values = {
            card_path_field: product.get(card_path_field, "").strip(),
            card_alt_field: product.get(card_alt_field, "").strip(),
            card_source_field: product.get(card_source_field, "").strip(),
            card_revision_field: product.get(card_revision_field, "").strip(),
        }
        if status == card_available:
            for field, value in values.items():
                if not value:
                    errors.append(f"Product {product_id} mist {field} bij {card_available}")
            asset_path = values[card_path_field]
            if asset_path:
                asset_file = card_asset_root / asset_path.lstrip("/")
                extension = asset_file.suffix.lower()
                if not asset_path.startswith("/images/") or not asset_file.is_file():
                    errors.append(f"Product {product_id} verwijst naar ontbrekende kaartafbeelding: {asset_path}")
                elif extension not in card_allowed_extensions:
                    errors.append(f"Product {product_id} gebruikt niet-toegestane kaartafbeeldingsextensie: {extension!r}")
                else:
                    detected_extension = detect_image_extension(asset_file)
                    normalized_extension = ".jpg" if extension == ".jpeg" else extension
                    if not detected_extension:
                        errors.append(f"Product {product_id} heeft een onbekend of beschadigd kaartafbeeldingsformaat: {asset_path}")
                    elif detected_extension != normalized_extension:
                        errors.append(
                            f"Product {product_id} heeft extensie {extension} maar bestandssignatuur {detected_extension}: {asset_path}"
                        )
        elif any(values.values()):
            errors.append(f"Product {product_id} heeft afbeeldingsvelden terwijl status {card_missing} is")

    material_appearance_contract = schema.get("materialAppearanceContract", {})
    material_appearance_field = material_appearance_contract.get("field", "Renderweergave")
    allowed_material_appearances = set(material_appearance_contract.get("allowedValues", []))
    if not allowed_material_appearances:
        errors.append("Schema mist toegestane materialAppearanceContract-waarden")
    for material in materials:
        appearance = material.get(material_appearance_field, "").strip()
        if appearance not in allowed_material_appearances:
            errors.append(
                f"Materiaal {material['Materiaal-ID']} heeft ongeldige {material_appearance_field}: {appearance!r}"
            )

    material_customer_name_contract = schema.get("materialCustomerNameContract", {})
    material_customer_name_field = material_customer_name_contract.get("field", "Klantnaam")
    material_customer_name_maximum = int(material_customer_name_contract.get("maximumLength", 80))
    for material in materials:
        customer_name = material.get(material_customer_name_field, "").strip()
        if len(customer_name) > material_customer_name_maximum:
            errors.append(
                f"Materiaal {material['Materiaal-ID']} heeft een te lange {material_customer_name_field}"
            )

    render_contract = schema.get("componentRenderContract", {})
    render_status_field = render_contract.get("statusField", "Renderstatus")
    open_render_field = render_contract.get("openDataField", "Open renderdata")
    provisional_status = render_contract.get("provisionalStatus", "ProvisionalRenderEnvelope")
    render_numeric_fields = render_contract.get("requiredPositiveNumericFields", [])
    for component in components:
        status = component.get(render_status_field, "").strip()
        if not status:
            continue
        for field in render_numeric_fields:
            try:
                value = float(component.get(field, ""))
                if value <= 0:
                    raise ValueError
            except ValueError:
                errors.append(f"Component {component['Component-ID']} mist positieve renderwaarde {field}")
        if status == provisional_status and not component.get(open_render_field, "").strip():
            errors.append(f"Component {component['Component-ID']} mist open renderdata bij {provisional_status}")
        try:
            rotation_step = float(component.get("Draaistap °", ""))
            if rotation_step > 0 and abs((360 / rotation_step) - round(360 / rotation_step)) > 1e-9:
                errors.append(f"Component {component['Component-ID']} heeft een draaistap die 360 graden niet deelt")
        except ValueError:
            pass

    hardware_contract = schema.get("assemblyHardwareRenderContract", {})
    hardware_family_field = hardware_contract.get("familyField", "Hardwarefamilie")
    hardware_source_field = hardware_contract.get("sourceField", "Hardwaregeometrie-bron")
    hardware_open_field = hardware_contract.get("openDataField", "Open hardwaregeometrie")
    connector_fields = hardware_contract.get("connectorFields", [])
    bolt_fields = hardware_contract.get("boltFields", [])
    for component in components:
        family = component.get(hardware_family_field, "").strip()
        if family not in {"StandardConnector", "ButtonHeadHexSocketBolt"}:
            continue
        if not component.get(hardware_source_field, "").strip():
            errors.append(f"Component {component['Component-ID']} mist Hardwaregeometrie-bron")
        required_fields = connector_fields if family == "StandardConnector" else bolt_fields
        missing_fields = []
        for field in required_fields:
            try:
                if float(component.get(field, "")) <= 0:
                    raise ValueError
            except ValueError:
                missing_fields.append(field)
        open_data = component.get(hardware_open_field, "").strip()
        if missing_fields and not open_data:
            errors.append(f"Component {component['Component-ID']} mist open hardwaregeometrie bij ontbrekende velden: {', '.join(missing_fields)}")
        if not missing_fields and open_data:
            errors.append(f"Component {component['Component-ID']} heeft complete hardwarevelden maar nog open hardwaregeometrie")

    primitive_contract = schema.get("componentPrimitiveRenderContract", {})
    primitive_geometry_field = primitive_contract.get("geometryField", "Renderprimitieven JSON")
    primitive_status_field = primitive_contract.get("statusField", "Primitieve renderstatus")
    primitive_source_field = primitive_contract.get("sourceField", "Primitieve renderbron")
    primitive_open_field = primitive_contract.get("openDataField", "Open primitieve renderdata")
    primitive_version = primitive_contract.get("contractVersion", 1)
    primitive_shapes = set(primitive_contract.get("allowedShapes", ["box", "cylinder"]))
    primitive_provisional = primitive_contract.get("provisionalStatus", "ProvisionalRenderEnvelope")
    primitive_exact = primitive_contract.get("exactStatus", "ExactSupplierGeometry")
    for component in components:
        raw_geometry = component.get(primitive_geometry_field, "").strip()
        if not raw_geometry:
            continue
        component_id = component.get("Component-ID", "")
        status = component.get(primitive_status_field, "").strip()
        source = component.get(primitive_source_field, "").strip()
        open_data = component.get(primitive_open_field, "").strip()
        if status not in {primitive_provisional, primitive_exact}:
            errors.append(f"Component {component_id} heeft ongeldige primitieve renderstatus {status!r}")
        if not source:
            errors.append(f"Component {component_id} mist primitieve renderbron")
        if status == primitive_provisional and not open_data:
            errors.append(f"Component {component_id} mist Open primitieve renderdata bij {primitive_provisional}")
        if status == primitive_exact and open_data:
            errors.append(f"Component {component_id} is exact gemarkeerd maar heeft nog Open primitieve renderdata")
        try:
            geometry = json.loads(raw_geometry)
        except json.JSONDecodeError as exc:
            errors.append(f"Component {component_id} heeft ongeldige Renderprimitieven JSON: {exc.msg}")
            continue
        primitives = geometry.get("primitives", []) if isinstance(geometry, dict) else []
        if not isinstance(geometry, dict) or geometry.get("version") != primitive_version or not isinstance(primitives, list) or not primitives:
            errors.append(f"Component {component_id} mist primitief rendercontract versie {primitive_version} of primitives")
            continue
        primitive_ids: set[str] = set()
        for primitive in primitives:
            if not isinstance(primitive, dict):
                errors.append(f"Component {component_id} bevat een ongeldige renderprimitive")
                continue
            primitive_id = str(primitive.get("id", "")).strip()
            if not primitive_id or primitive_id in primitive_ids:
                errors.append(f"Component {component_id} bevat een leeg of dubbel primitive-ID {primitive_id!r}")
            primitive_ids.add(primitive_id)
            if primitive.get("shape") not in primitive_shapes:
                errors.append(f"Component {component_id}/{primitive_id} gebruikt ongeldige primitivevorm")
            if not str(primitive.get("role", "")).strip():
                errors.append(f"Component {component_id}/{primitive_id} mist presentatierol")
            inherit_placement_dimensions = primitive.get("inheritPlacementDimensions", False)
            if not isinstance(inherit_placement_dimensions, bool):
                errors.append(f"Component {component_id} primitive {primitive_id} heeft ongeldige inheritPlacementDimensions")
                inherit_placement_dimensions = False
            for field in (() if inherit_placement_dimensions else ("sizeX", "sizeY", "sizeZ")):
                try:
                    if float(primitive.get(field, 0)) <= 0:
                        raise ValueError
                except (TypeError, ValueError):
                    errors.append(f"Component {component_id}/{primitive_id} mist positieve {field}")
            holes = primitive.get("holes", [])
            if not isinstance(holes, list):
                errors.append(f"Component {component_id}/{primitive_id} heeft ongeldige gatenlijst")
                continue
            hole_ids: set[str] = set()
            for hole in holes:
                if not isinstance(hole, dict):
                    errors.append(f"Component {component_id}/{primitive_id} bevat een ongeldig gat")
                    continue
                hole_id = str(hole.get("id", "")).strip()
                if not hole_id or hole_id in hole_ids:
                    errors.append(f"Component {component_id}/{primitive_id} bevat leeg of dubbel gat-ID {hole_id!r}")
                hole_ids.add(hole_id)
                if hole.get("plane") not in {"x", "y", "z"}:
                    errors.append(f"Component {component_id}/{primitive_id}/{hole_id} heeft ongeldig gatvlak")
                try:
                    if float(hole.get("diameter", 0)) <= 0:
                        raise ValueError
                except (TypeError, ValueError):
                    errors.append(f"Component {component_id}/{primitive_id}/{hole_id} mist positieve gatdiameter")

    receiving_thread_contract = schema.get("profileFastenerReceivingThreadContract", {})
    thread_diameter_field = receiving_thread_contract.get("threadDiameterField", "Draad Ø mm")
    usable_thread_zone_field = receiving_thread_contract.get("usableThreadZoneField", "Bruikbare draadzone mm")
    thread_inlet_offset_field = receiving_thread_contract.get("threadInletOffsetField", "Draadinlaat vanaf profielvlak mm")
    through_thread_field = receiving_thread_contract.get("throughThreadField", "Draadgat doorlopend")
    thread_source_field = receiving_thread_contract.get("sourceField", "Draadzone-bron")
    components_by_id = {component.get("Component-ID", ""): component for component in components}
    for requirement in receiving_thread_contract.get("requiredComponents", []):
        component_id = str(requirement.get("componentId", "")).strip()
        component = components_by_id.get(component_id)
        if component is None:
            errors.append(f"Vereist profielmoercomponent ontbreekt: {component_id}")
            continue
        try:
            diameter = float(component.get(thread_diameter_field, ""))
            expected_diameter = float(requirement.get("threadDiameterMm", 0))
            if diameter <= 0 or abs(diameter - expected_diameter) > 1e-9:
                raise ValueError
        except (TypeError, ValueError):
            errors.append(f"Component {component_id} mist de verwachte draadmaat in {thread_diameter_field}")
        try:
            if float(component.get(usable_thread_zone_field, "")) <= 0:
                raise ValueError
        except (TypeError, ValueError):
            errors.append(f"Component {component_id} mist positieve {usable_thread_zone_field}")
        try:
            if float(component.get(thread_inlet_offset_field, "")) < 0:
                raise ValueError
        except (TypeError, ValueError):
            errors.append(f"Component {component_id} mist geldige niet-negatieve {thread_inlet_offset_field}")
        if component.get(through_thread_field, "").strip().lower() not in ("ja", "nee"):
            errors.append(f"Component {component_id} mist Ja/Nee in {through_thread_field}")
        if not component.get(thread_source_field, "").strip():
            errors.append(f"Component {component_id} mist {thread_source_field}")

    profile_geometry_ids = {
        "alu_system_40x40": (4, 1),
        "alu_system_80x40": (6, 2),
        "alu_system_80x80": (8, 4),
        "alu_system_160x40": (10, 4),
    }
    for material in materials:
        material_id = material.get("Materiaal-ID", "")
        series = material.get("Profielserie", "").strip()
        if material_id in profile_geometry_ids and not series:
            errors.append(f"Materiaal {material_id} mist vrijgegeven sleufasgeometrie")
            continue
        if not series:
            continue
        try:
            width = float(material.get("Breedte mm", ""))
            height = float(material.get("Hoogte mm", ""))
            edge = float(material.get("Sleufas-randafstand mm", ""))
            pitch = float(material.get("Sleufas-raster mm", ""))
            expected = int(float(material.get("Sleufassen rondom", "")))
            slot_width = float(material.get("Sleufmaat mm", ""))
            expected_core = int(float(material.get("Kernboringen per kop", "")))
        except ValueError:
            errors.append(f"Materiaal {material_id} heeft onvolledige numerieke sleufasgeometrie")
            continue
        axes_width = 1 + int(((width - edge) - edge + 1e-9) // pitch)
        axes_height = 1 + int(((height - edge) - edge + 1e-9) // pitch)
        calculated = 2 * axes_width + 2 * axes_height
        calculated_core = axes_width * axes_height
        if edge != 20 or pitch != 40:
            errors.append(f"Materiaal {material_id} wijkt af van 20 mm randafstand / 40 mm raster")
        if slot_width not in (8, 10):
            errors.append(f"Materiaal {material_id} heeft geen vrijgegeven sleufmaat 8 of 10")
        if calculated != expected:
            errors.append(f"Materiaal {material_id}: {calculated} sleufassen berekend, {expected} opgeslagen")
        if calculated_core != expected_core:
            errors.append(f"Materiaal {material_id}: {calculated_core} kernboringen per kop berekend, {expected_core} opgeslagen")
        expected_tap = "M8" if slot_width == 8 else "M12"
        if material.get("Kopse tapdraad", "").strip() != expected_tap:
            errors.append(f"Materiaal {material_id} mist kopse tapdraad {expected_tap} voor groef {slot_width:g}")
        profile_contract = schema.get("profileRenderContract", {})
        profile_status = material.get(profile_contract.get("statusField", "Profielgeometrie-status"), "").strip()
        profile_open = material.get(profile_contract.get("openDataField", "Open profielgeometrie"), "").strip()
        profile_source = material.get(profile_contract.get("sourceField", "Profielgeometrie-bron"), "").strip()
        missing_profile_fields = []
        for field in profile_contract.get("positiveNumericFields", []):
            try:
                if float(material.get(field, "")) <= 0:
                    raise ValueError
            except ValueError:
                missing_profile_fields.append(field)
        exact_profile_status = profile_contract.get("exactStatus", "ExactSupplierGeometry")
        provisional_profile_status = profile_contract.get("provisionalStatus", "ProvisionalRenderEnvelope")
        if profile_status == exact_profile_status:
            if missing_profile_fields or not profile_source or profile_open:
                errors.append(f"Materiaal {material_id} is exact gemarkeerd maar profielgeometrie is niet compleet")
        elif missing_profile_fields and (profile_status != provisional_profile_status or not profile_open):
            errors.append(f"Materiaal {material_id} mist exacte profielgeometrie zonder actieve ProvisionalRenderEnvelope/OpenData")
        frozen = profile_geometry_ids.get(material_id)
        if frozen is not None and expected != frozen[0]:
            errors.append(f"Materiaal {material_id}: contract verwacht {frozen[0]} sleufassen rondom")
        if frozen is not None and expected_core != frozen[1]:
            errors.append(f"Materiaal {material_id}: contract verwacht {frozen[1]} kernboringen per kop")

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

    input_signatures: set[tuple[str, str]] = set()
    allowed_input_types = {"number", "select", "checkbox", "hidden"}
    for contract in input_contracts:
        contract_id = contract.get("Invoercontract-ID", "")
        product_id = contract.get("Product-ID", "").strip()
        input_id = contract.get("Invoer-ID", "").strip()
        if product_id not in product_ids:
            errors.append(f"Invoercontract {contract_id} heeft onbekend Product-ID")
        signature = (product_id.lower(), input_id.lower())
        if signature in input_signatures:
            errors.append(f"Dubbel actief invoercontract voor {product_id}.{input_id}")
        input_signatures.add(signature)
        if not input_id or not contract.get("Request-veld", "").strip():
            errors.append(f"Invoercontract {contract_id} mist Invoer-ID of Request-veld")
        if contract.get("Invoertype", "").strip() not in allowed_input_types:
            errors.append(f"Invoercontract {contract_id} heeft ongeldig Invoertype")
        if contract.get("Actief", "").strip() != "Ja":
            errors.append(f"Invoercontract {contract_id} is niet actief en hoort niet in de canonieke tabel")
        if contract.get("Blokkeert configuratie", "").strip() == "Ja" and not contract.get("Toelichting", "").strip():
            errors.append(f"Blokkerend invoercontract {contract_id} mist een concrete toelichting")
        if contract.get("Invoertype", "").strip() == "select":
            has_options = bool(contract.get("Opties", "").strip() or contract.get("Optiebron", "").strip())
            if not has_options and contract.get("Blokkeert configuratie", "").strip() != "Ja":
                errors.append(f"Selectiecontract {contract_id} mist opties of optiebron")

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
