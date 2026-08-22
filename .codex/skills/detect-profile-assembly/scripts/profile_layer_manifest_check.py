#!/usr/bin/env python3
"""Check assembly placement counts against a frozen layer/role manifest."""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path
from typing import Any


def load_json(path: Path) -> Any:
    with path.open("r", encoding="utf-8-sig") as handle:
        return json.load(handle)


def assembly_parts(payload: Any) -> list[dict[str, Any]]:
    if isinstance(payload, list):
        return [item for item in payload if isinstance(item, dict)]
    if not isinstance(payload, dict):
        raise ValueError("Assembly JSON must be an array or object.")
    for key in ("AssemblyParts", "assemblyParts", "AssemblyPlacements", "assemblyPlacements", "Parts", "parts"):
        value = payload.get(key)
        if isinstance(value, list):
            return [item for item in value if isinstance(item, dict)]
    raise ValueError("No assembly part array found.")


def part_name(part: dict[str, Any]) -> str:
    return str(part.get("PartName") or part.get("partName") or part.get("Name") or part.get("name") or "")


def is_match(name: str, rule: dict[str, Any]) -> bool:
    if "name" in rule:
        return name == str(rule["name"])
    if "name_prefix" in rule:
        return name.startswith(str(rule["name_prefix"]))
    if "name_regex" in rule:
        return re.search(str(rule["name_regex"]), name) is not None
    raise ValueError(f"Manifest role {rule.get('role', '<unknown>')} has no name matcher.")


def check(parts: list[dict[str, Any]], manifest: dict[str, Any]) -> dict[str, Any]:
    rows: list[dict[str, Any]] = []
    for rule in manifest.get("members", []):
        expected = int(rule["expected_count"])
        matches = [part_name(part) for part in parts if is_match(part_name(part), rule)]
        actual = len(matches)
        rows.append({
            "layer": str(rule.get("layer", "unassigned")),
            "role": str(rule.get("role", "unnamed")),
            "section": str(rule.get("section", "")),
            "orientation": str(rule.get("orientation", "")),
            "expected": expected,
            "actual": actual,
            "delta": actual - expected,
            "matches": matches,
        })
    return {
        "manifest_id": manifest.get("manifest_id"),
        "version": manifest.get("version"),
        "passed": all(row["delta"] == 0 for row in rows),
        "rows": rows,
    }


def markdown(result: dict[str, Any]) -> str:
    lines = [
        f"# Layer manifest check — {result.get('manifest_id') or 'assembly'}",
        "",
        f"Result: **{'PASS' if result['passed'] else 'FAIL'}**",
        "",
        "| Layer | Role | Section/orientation | Expected | Actual | Delta |",
        "|---|---|---|---:|---:|---:|",
    ]
    for row in result["rows"]:
        spec = " / ".join(value for value in (row["section"], row["orientation"]) if value)
        lines.append(f"| {row['layer']} | {row['role']} | {spec} | {row['expected']} | {row['actual']} | {row['delta']:+d} |")
    lines.append("")
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("assembly", type=Path)
    parser.add_argument("manifest", type=Path)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--json-output", type=Path)
    args = parser.parse_args()

    result = check(assembly_parts(load_json(args.assembly)), load_json(args.manifest))
    report = markdown(result)
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(report, encoding="utf-8")
    else:
        print(report)
    if args.json_output:
        args.json_output.parent.mkdir(parents=True, exist_ok=True)
        args.json_output.write_text(json.dumps(result, indent=2, ensure_ascii=False), encoding="utf-8")
    return 0 if result["passed"] else 2


if __name__ == "__main__":
    sys.exit(main())
