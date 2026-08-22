#!/usr/bin/env python3
"""CI guard for JSON validity, local references and generated repository files."""

from __future__ import annotations

import json
import re
import subprocess
import sys
from pathlib import Path
from urllib.parse import unquote

ROOT = Path(__file__).resolve().parents[1]
BANNED_PARTS = {
    "bin", "obj", ".codex-artifacts", "artifacts", "output", "outputs",
    "tmp", "backups", "__pycache__",
}
BANNED_EXACT = {"config/portal-runtime.json"}
BANNED_SUFFIXES = (
    ".pyc", ".pyo", ".inspect.ndjson", ".log",
    ".sqlite", ".sqlite-wal", ".sqlite-shm",
)


def repository_files() -> list[str]:
    result = subprocess.run(
        ["git", "ls-files", "--cached", "--others", "--exclude-standard"],
        cwd=ROOT, check=True, capture_output=True, text=True
    )
    return [line.strip().replace("\\", "/") for line in result.stdout.splitlines() if line.strip()]


def main() -> int:
    errors: list[str] = []
    repository = repository_files()
    for relative in repository:
        parts = set(Path(relative).parts)
        if relative in BANNED_EXACT or parts.intersection(BANNED_PARTS) or relative.lower().endswith(BANNED_SUFFIXES):
            errors.append(f"Gegenereerd/lokaal bestand wordt gevolgd: {relative}")

    for json_file in sorted((ROOT / "config").rglob("*.json")):
        try:
            json.loads(json_file.read_text(encoding="utf-8-sig"))
        except Exception as exc:
            errors.append(f"Ongeldige JSON {json_file.relative_to(ROOT)}: {exc}")

    markdown_files = [ROOT / "AGENTS.md", ROOT / "README.md"] + sorted((ROOT / "docs").rglob("*.md"))
    link_pattern = re.compile(r"\[[^\]]+\]\(([^)]+)\)")
    for markdown in markdown_files:
        text = markdown.read_text(encoding="utf-8")
        for raw_target in link_pattern.findall(text):
            target = raw_target.strip().strip("<>").split("#", 1)[0]
            if not target or "://" in target or target.startswith("mailto:"):
                continue
            candidate = (markdown.parent / unquote(target)).resolve()
            if not candidate.exists():
                errors.append(f"Gebroken documentverwijzing in {markdown.relative_to(ROOT)}: {raw_target}")

    required = [
        ROOT / "AGENTS.md",
        ROOT / "docs" / "README.md",
        ROOT / "docs" / "Masterdata-beheer.md",
        ROOT / ".codex" / "skills" / "detect-profile-assembly" / "SKILL.md",
    ]
    for path in required:
        if not path.is_file():
            errors.append(f"Verplichte projectinstructie ontbreekt: {path.relative_to(ROOT)}")

    if errors:
        print("REPOSITORYCONTROLE MISLUKT")
        for error in errors:
            print("- " + error)
        return 1
    print(f"REPOSITORY GELDIG: {len(repository)} bronbestanden, JSON en lokale verwijzingen gecontroleerd")
    return 0


if __name__ == "__main__":
    sys.exit(main())
