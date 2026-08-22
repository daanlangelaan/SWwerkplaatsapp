#!/usr/bin/env python3
"""Compare expected and actual profile_geometry_audit JSON reports."""

from __future__ import annotations

import argparse
import json
import math
import re
from pathlib import Path
from typing import Any

AXES = ("X", "Y", "Z")


def load(path: Path) -> dict[str, Any]:
    data = json.loads(path.read_text(encoding="utf-8-sig"))
    if not isinstance(data, dict) or not isinstance(data.get("profiles"), list):
        raise ValueError(f"Geen geldig geometrie-auditrapport: {path}")
    return data


def key(name: Any) -> str:
    return re.sub(r"\s+", " ", str(name or "").strip().lower())


def distance(a: dict[str, Any], b: dict[str, Any]) -> float:
    return math.sqrt(sum((float(a["center"][axis]) - float(b["center"][axis])) ** 2 for axis in AXES))


def match_profiles(expected: list[dict[str, Any]], actual: list[dict[str, Any]]) -> tuple[list[tuple[dict[str, Any], dict[str, Any]]], list[dict[str, Any]], list[dict[str, Any]]]:
    expected_groups: dict[str, list[dict[str, Any]]] = {}
    actual_groups: dict[str, list[dict[str, Any]]] = {}
    for profile in expected: expected_groups.setdefault(key(profile.get("name")), []).append(profile)
    for profile in actual: actual_groups.setdefault(key(profile.get("name")), []).append(profile)
    pairs, missing, extra = [], [], []
    for name in sorted(set(expected_groups) | set(actual_groups)):
        remaining_expected = list(expected_groups.get(name, [])); remaining_actual = list(actual_groups.get(name, []))
        while remaining_expected and remaining_actual:
            best = min(((distance(e, a), ei, ai) for ei, e in enumerate(remaining_expected) for ai, a in enumerate(remaining_actual)), key=lambda item: item[0])
            _, expected_index, actual_index = best
            pairs.append((remaining_expected.pop(expected_index), remaining_actual.pop(actual_index)))
        missing.extend(remaining_expected); extra.extend(remaining_actual)
    return pairs, missing, extra


def close(a: Any, b: Any, tolerance: float) -> bool:
    return abs(float(a) - float(b)) <= tolerance


def profile_differences(expected: dict[str, Any], actual: dict[str, Any], tolerance: float) -> list[str]:
    differences = []
    if expected.get("section") != actual.get("section"): differences.append(f"section {expected.get('section')} != {actual.get('section')}")
    if expected.get("axis") != actual.get("axis"): differences.append(f"axis {expected.get('axis')} != {actual.get('axis')}")
    for axis in AXES:
        if not close(expected["size"][axis], actual["size"][axis], tolerance):
            differences.append(f"Size{axis} {expected['size'][axis]:g} != {actual['size'][axis]:g} mm")
        for index, sign in ((0, "-"), (1, "+")):
            if not close(expected["bounds"][axis][index], actual["bounds"][axis][index], tolerance):
                differences.append(f"{axis}{sign} {expected['bounds'][axis][index]:g} != {actual['bounds'][axis][index]:g} mm")
    return differences


def contact_signature(item: dict[str, Any], id_map: dict[str, str] | None = None) -> tuple[str, str, str, str]:
    a = id_map.get(item["a"], item["a"]) if id_map else item["a"]
    b = id_map.get(item["b"], item["b"]) if id_map else item["b"]
    first = (a, item.get("a_face", "")); second = (b, item.get("b_face", ""))
    if second < first: first, second = second, first
    return first[0], first[1], second[0], second[1]


def pair_signature(item: dict[str, Any], id_map: dict[str, str] | None = None) -> tuple[Any, ...]:
    a = id_map.get(item["a"], item["a"]) if id_map else item["a"]
    b = id_map.get(item["b"], item["b"]) if id_map else item["b"]
    first, second = sorted((a, b))
    touching = tuple(sorted(item.get("touching_axes", [])))
    return first, second, touching, item.get("shared_edge_axis", "")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("expected", type=Path)
    parser.add_argument("actual", type=Path)
    parser.add_argument("--tolerance", type=float, default=1.0)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--json-output", type=Path)
    parser.add_argument("--allow-differences", action="store_true")
    args = parser.parse_args()
    expected = load(args.expected); actual = load(args.actual)
    pairs, missing, extra = match_profiles(expected["profiles"], actual["profiles"])
    mismatches = []
    expected_to_actual, actual_to_expected = {}, {}
    for expected_profile, actual_profile in pairs:
        expected_to_actual[expected_profile["id"]] = actual_profile["id"]
        actual_to_expected[actual_profile["id"]] = expected_profile["id"]
        differences = profile_differences(expected_profile, actual_profile, args.tolerance)
        if differences:
            mismatches.append({"expected_id": expected_profile["id"], "actual_id": actual_profile["id"],
                               "name": expected_profile["name"], "differences": differences})
    expected_contacts = {contact_signature(item) for item in expected.get("relations", {}).get("contacts", [])}
    actual_contacts = {contact_signature(item, actual_to_expected) for item in actual.get("relations", {}).get("contacts", [])}
    missing_contacts = sorted(expected_contacts - actual_contacts)
    extra_contacts = sorted(actual_contacts - expected_contacts)
    expected_coplanar = {contact_signature(item) for item in expected.get("relations", {}).get("coplanar", [])}
    actual_coplanar = {contact_signature(item, actual_to_expected) for item in actual.get("relations", {}).get("coplanar", [])}
    expected_edges = {pair_signature(item) for item in expected.get("relations", {}).get("edge_contacts", [])}
    actual_edges = {pair_signature(item, actual_to_expected) for item in actual.get("relations", {}).get("edge_contacts", [])}
    expected_points = {pair_signature(item) for item in expected.get("relations", {}).get("point_contacts", [])}
    actual_points = {pair_signature(item, actual_to_expected) for item in actual.get("relations", {}).get("point_contacts", [])}
    expected_overlaps = {pair_signature(item)[:2] for item in expected.get("relations", {}).get("overlaps", [])}
    actual_overlaps = {pair_signature(item, actual_to_expected)[:2] for item in actual.get("relations", {}).get("overlaps", [])}
    result = {
        "matches": len(pairs),
        "missing_profiles": [{"id": item["id"], "name": item["name"]} for item in missing],
        "extra_profiles": [{"id": item["id"], "name": item["name"]} for item in extra],
        "profile_mismatches": mismatches,
        "missing_contacts": missing_contacts,
        "extra_contacts": extra_contacts,
        "missing_coplanar_faces": sorted(expected_coplanar - actual_coplanar),
        "extra_coplanar_faces": sorted(actual_coplanar - expected_coplanar),
        "missing_edge_contacts": sorted(expected_edges - actual_edges),
        "extra_edge_contacts": sorted(actual_edges - expected_edges),
        "missing_point_contacts": sorted(expected_points - actual_points),
        "extra_point_contacts": sorted(actual_points - expected_points),
        "missing_overlaps": sorted(expected_overlaps - actual_overlaps),
        "extra_overlaps": sorted(actual_overlaps - expected_overlaps),
    }
    compared_fields = ("missing_profiles", "extra_profiles", "profile_mismatches", "missing_contacts", "extra_contacts",
                       "missing_coplanar_faces", "extra_coplanar_faces",
                       "missing_edge_contacts", "extra_edge_contacts", "missing_point_contacts", "extra_point_contacts",
                       "missing_overlaps", "extra_overlaps")
    difference_count = sum(len(result[field]) for field in compared_fields)
    result["difference_count"] = difference_count
    lines = ["# Profielreferentie-delta", "", f"Verschillen: {difference_count}", "", f"Gekoppelde profielen: {len(pairs)}", ""]
    for field, title in (("missing_profiles", "Ontbrekende profielen"), ("extra_profiles", "Extra profielen"),
                         ("profile_mismatches", "Profielafwijkingen"), ("missing_contacts", "Ontbrekende contactvlakken"),
                         ("extra_contacts", "Extra contactvlakken"), ("missing_coplanar_faces", "Ontbrekende gelijkliggende buitenvlakken"),
                         ("extra_coplanar_faces", "Extra gelijkliggende vlakken"), ("missing_edge_contacts", "Ontbrekende referentie-lijncontacten"),
                         ("extra_edge_contacts", "Extra lijncontacten"), ("missing_point_contacts", "Ontbrekende referentie-puntcontacten"),
                         ("extra_point_contacts", "Extra puntcontacten"), ("missing_overlaps", "Ontbrekende referentie-overlap"),
                         ("extra_overlaps", "Extra overlap")):
        lines.extend([f"## {title}", ""])
        items = result[field]
        lines.extend(["- `" + json.dumps(item, ensure_ascii=False, sort_keys=True) + "`" for item in items] or ["Geen."])
        lines.append("")
    markdown = "\n".join(lines)
    if args.output: args.output.write_text(markdown, encoding="utf-8")
    else: print(markdown)
    if args.json_output: args.json_output.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")
    return 0 if difference_count == 0 or args.allow_differences else 2


if __name__ == "__main__":
    raise SystemExit(main())
