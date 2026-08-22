#!/usr/bin/env python3
"""Audit axis-aligned T-slot profile assembly geometry from portal-style JSON."""

from __future__ import annotations

import argparse
import json
import math
import sys
from pathlib import Path
from typing import Any

AXES = ("X", "Y", "Z")
KNOWN_SECTIONS = ((40.0, 40.0), (40.0, 80.0), (80.0, 80.0), (40.0, 160.0))


def number(value: Any, field: str) -> float:
    try:
        result = float(value)
    except (TypeError, ValueError) as exc:
        raise ValueError(f"Ongeldige numerieke waarde voor {field}: {value!r}") from exc
    if not math.isfinite(result):
        raise ValueError(f"Niet-eindige waarde voor {field}: {value!r}")
    return result


def load_parts(source: str) -> list[dict[str, Any]]:
    raw = sys.stdin.read() if source == "-" else Path(source).read_text(encoding="utf-8-sig")
    data = json.loads(raw)
    if isinstance(data, list):
        parts = data
    elif isinstance(data, dict):
        parts = next((data[key] for key in ("AssemblyParts", "assemblyParts", "Parts", "parts") if isinstance(data.get(key), list)), None)
        if parts is None:
            raise ValueError("JSON bevat geen AssemblyParts/assemblyParts/Parts/parts-array.")
    else:
        raise ValueError("JSON-hoofdwaarde moet een array of object zijn.")
    return [p for p in parts if isinstance(p, dict) and str(p.get("Kind", p.get("kind", ""))).lower() == "profile"]


def get(part: dict[str, Any], name: str, default: Any = None) -> Any:
    return part.get(name, part.get(name[0].lower() + name[1:], default))


def classify_section(a: float, b: float, tolerance: float) -> tuple[str, float]:
    lo, hi = sorted((a, b))
    best = min(KNOWN_SECTIONS, key=lambda s: abs(lo - s[0]) + abs(hi - s[1]))
    error = max(abs(lo - best[0]), abs(hi - best[1]))
    return (f"{int(best[0])}x{int(best[1])}" if error <= tolerance else f"{lo:g}x{hi:g} onbekend", error)


def interval_overlap(a0: float, a1: float, b0: float, b1: float) -> float:
    return min(a1, b1) - max(a0, b0)


def interval_gap(a0: float, a1: float, b0: float, b1: float) -> float:
    return max(0.0, max(a0, b0) - min(a1, b1))


def build_records(parts: list[dict[str, Any]], section_tolerance: float) -> list[dict[str, Any]]:
    records = []
    for index, part in enumerate(parts, 1):
        center = {axis: number(get(part, axis + "mm"), axis + "mm") for axis in AXES}
        size = {axis: number(get(part, "Size" + axis + "mm"), "Size" + axis + "mm") for axis in AXES}
        if any(value <= 0 for value in size.values()):
            raise ValueError(f"Profiel {index} heeft een niet-positieve buitenmaat.")
        axis = max(AXES, key=lambda candidate: size[candidate])
        cross_axes = [candidate for candidate in AXES if candidate != axis]
        section, section_error = classify_section(size[cross_axes[0]], size[cross_axes[1]], section_tolerance)
        bounds = {candidate: (center[candidate] - size[candidate] / 2.0, center[candidate] + size[candidate] / 2.0) for candidate in AXES}
        ordered_sizes = sorted(size.values(), reverse=True)
        axis_confidence = "confirmed" if ordered_sizes[0] > ordered_sizes[1] * 1.1 else ("probable" if ordered_sizes[0] > ordered_sizes[1] + section_tolerance else "unresolved")
        start_point = dict(center); end_point = dict(center)
        start_point[axis] = bounds[axis][0]; end_point[axis] = bounds[axis][1]
        records.append({"id": f"P{index:02d}", "name": str(get(part, "Name", f"Profiel {index}")), "axis": axis,
                        "cross_axes": cross_axes, "section": section, "section_error_mm": section_error,
                        "axis_confidence": axis_confidence, "start_point": start_point, "end_point": end_point,
                        "center": center, "size": size, "bounds": bounds})
    return records


def analyse_relations(records: list[dict[str, Any]], tolerance: float, near: float) -> dict[str, list[dict[str, Any]]]:
    relations: dict[str, list[dict[str, Any]]] = {"contacts": [], "edge_contacts": [], "point_contacts": [], "coplanar": [], "gaps": [], "overlaps": []}
    for index, a in enumerate(records):
        for b in records[index + 1:]:
            overlaps = {axis: interval_overlap(*a["bounds"][axis], *b["bounds"][axis]) for axis in AXES}
            if all(overlaps[axis] > tolerance for axis in AXES):
                relations["overlaps"].append({"a": a["id"], "b": b["id"], "depth_mm": {k: round(v, 3) for k, v in overlaps.items()}})
            touching_axes = [axis for axis in AXES if abs(overlaps[axis]) <= tolerance]
            separated_axes = [axis for axis in AXES if overlaps[axis] < -tolerance]
            positive_axes = [axis for axis in AXES if overlaps[axis] > tolerance]
            if not separated_axes and len(touching_axes) == 2 and len(positive_axes) == 1:
                relations["edge_contacts"].append({"a": a["id"], "b": b["id"], "touching_axes": touching_axes,
                                                   "shared_edge_axis": positive_axes[0], "shared_length_mm": round(overlaps[positive_axes[0]], 3)})
            elif not separated_axes and len(touching_axes) == 3:
                relations["point_contacts"].append({"a": a["id"], "b": b["id"], "touching_axes": touching_axes})
            for axis in AXES:
                others = [candidate for candidate in AXES if candidate != axis]
                projection_is_relevant = all(interval_gap(*a["bounds"][other], *b["bounds"][other]) <= near for other in others)
                if projection_is_relevant:
                    for sign, ai in (("-", 0), ("+", 1)):
                        for bsign, bi in (("-", 0), ("+", 1)):
                            if abs(a["bounds"][axis][ai] - b["bounds"][axis][bi]) <= tolerance:
                                relations["coplanar"].append({"a": a["id"], "a_face": axis + sign, "b": b["id"],
                                                              "b_face": axis + bsign,
                                                              "plane_mm": round((a["bounds"][axis][ai] + b["bounds"][axis][bi]) / 2.0, 3)})
                if not all(overlaps[other] > tolerance for other in others):
                    continue
                a0, a1 = a["bounds"][axis]; b0, b1 = b["bounds"][axis]
                opposing = ((abs(a1 - b0), "+", "-"), (abs(b1 - a0), "-", "+"))
                distance, a_face, b_face = min(opposing, key=lambda item: item[0])
                if distance <= tolerance:
                    plane = (a1 + b0) / 2.0 if a_face == "+" else (a0 + b1) / 2.0
                    relations["contacts"].append({"a": a["id"], "a_face": axis + a_face, "b": b["id"], "b_face": axis + b_face,
                                                   "plane_mm": round(plane, 3), "overlap_mm": {o: round(overlaps[o], 3) for o in others}})
                else:
                    gap = interval_gap(a0, a1, b0, b1)
                    if tolerance < gap <= near:
                        relations["gaps"].append({"a": a["id"], "b": b["id"], "axis": axis, "gap_mm": round(gap, 3)})
    for key, items in relations.items():
        unique, seen = [], set()
        for item in items:
            signature = json.dumps(item, sort_keys=True)
            if signature not in seen:
                seen.add(signature); unique.append(item)
        relations[key] = unique
    return relations


def markdown(records: list[dict[str, Any]], relations: dict[str, list[dict[str, Any]]], tolerance: float) -> str:
    lines = ["# Profielgeometrie-audit", "", f"Tolerantie: {tolerance:g} mm", "", "## Profielen", "",
             "| ID | Naam | Type | As | Zekerheid | Doorsnede-oriëntatie | Begin → einde |", "|---|---|---|---|---|---|---|"]
    for record in records:
        first, second = record["cross_axes"]
        start, end = record["bounds"][record["axis"]]
        orientation = f"{first}={record['size'][first]:g}, {second}={record['size'][second]:g} mm"
        lines.append(f"| {record['id']} | {record['name']} | {record['section']} | {record['axis']} | {record['axis_confidence']} | {orientation} | {start:g} → {end:g} mm |")
        faces = ", ".join(f"{axis}-={record['bounds'][axis][0]:g}; {axis}+={record['bounds'][axis][1]:g}" for axis in AXES)
        lines.append(f"\n{record['id']} vlakken: {faces}.\n")
    for key, label in (("contacts", "Vlakcontacten"), ("edge_contacts", "Lijncontacten (onvoldoende als dragend knooppunt)"),
                       ("point_contacts", "Puntcontacten (onvoldoende als dragend knooppunt)"),
                       ("coplanar", "Vlakgelijkheid"), ("gaps", "Nabije spleten"), ("overlaps", "Overlaps")):
        lines.extend(["", f"## {label}", ""])
        lines.extend(["- `" + json.dumps(item, ensure_ascii=False, sort_keys=True) + "`" for item in relations[key]] or ["Geen gedetecteerd."])
    return "\n".join(lines) + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("input", help="Portal-style assembly JSON, of - voor stdin")
    parser.add_argument("--output", type=Path, help="Markdownrapport")
    parser.add_argument("--json-output", type=Path, help="Machineleesbaar rapport")
    parser.add_argument("--tolerance", type=float, default=1.0, help="Vlak-/contacttolerantie in mm")
    parser.add_argument("--near", type=float, default=10.0, help="Maximale gerapporteerde spleet in mm")
    parser.add_argument("--section-tolerance", type=float, default=2.0, help="Tolerantie bekende doorsnede in mm")
    args = parser.parse_args()
    records = build_records(load_parts(args.input), args.section_tolerance)
    relations = analyse_relations(records, args.tolerance, args.near)
    report = {"profiles": records, "relations": relations, "tolerance_mm": args.tolerance}
    report_markdown = markdown(records, relations, args.tolerance)
    if args.output: args.output.write_text(report_markdown, encoding="utf-8")
    else: print(report_markdown, end="")
    if args.json_output: args.json_output.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
