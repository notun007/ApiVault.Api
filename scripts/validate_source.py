#!/usr/bin/env python3
"""Dependency-free structural validation for the ApiVault source package."""
from __future__ import annotations

import json
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
errors: list[str] = []

for path in sorted(ROOT.rglob("*.json")):
    try:
        json.loads(path.read_text(encoding="utf-8"))
    except Exception as exc:  # noqa: BLE001
        errors.append(f"Invalid JSON: {path.relative_to(ROOT)}: {exc}")

for pattern in ("*.csproj", "*.props", "*.targets"):
    for path in sorted(ROOT.rglob(pattern)):
        try:
            ET.parse(path)
        except Exception as exc:  # noqa: BLE001
            errors.append(f"Invalid XML: {path.relative_to(ROOT)}: {exc}")

# A lightweight C# lexical pass: strip strings/comments, then check delimiters.
def stripped_csharp(text: str) -> str:
    result: list[str] = []
    i = 0
    state = "code"
    while i < len(text):
        ch = text[i]
        nxt = text[i + 1] if i + 1 < len(text) else ""
        if state == "code":
            if ch == "/" and nxt == "/":
                state = "line_comment"; result.extend("  "); i += 2; continue
            if ch == "/" and nxt == "*":
                state = "block_comment"; result.extend("  "); i += 2; continue
            if ch == '@' and nxt == '"':
                state = "verbatim"; result.extend("  "); i += 2; continue
            if ch == '"':
                state = "string"; result.append(" "); i += 1; continue
            if ch == "'":
                state = "char"; result.append(" "); i += 1; continue
            result.append(ch); i += 1; continue
        if state == "line_comment":
            if ch == "\n": state = "code"; result.append("\n")
            else: result.append(" ")
            i += 1; continue
        if state == "block_comment":
            if ch == "*" and nxt == "/":
                state = "code"; result.extend("  "); i += 2
            else:
                result.append("\n" if ch == "\n" else " "); i += 1
            continue
        if state == "verbatim":
            if ch == '"' and nxt == '"': result.extend("  "); i += 2; continue
            if ch == '"': state = "code"
            result.append("\n" if ch == "\n" else " "); i += 1; continue
        if state in {"string", "char"}:
            if ch == "\\": result.extend("  "); i += 2; continue
            closing = '"' if state == "string" else "'"
            if ch == closing: state = "code"
            result.append("\n" if ch == "\n" else " "); i += 1; continue
    if state not in {"code", "line_comment"}:
        errors.append(f"Unterminated C# token state: {state}")
    return "".join(result)

pairs = {"{": "}", "(": ")", "[": "]"}
closers = {v: k for k, v in pairs.items()}
for path in sorted(ROOT.rglob("*.cs")):
    original = path.read_text(encoding="utf-8")
    clean = stripped_csharp(original)
    stack: list[tuple[str, int]] = []
    for index, ch in enumerate(clean):
        if ch in pairs:
            stack.append((ch, index))
        elif ch in closers:
            if not stack or stack[-1][0] != closers[ch]:
                errors.append(f"Unbalanced delimiter {ch}: {path.relative_to(ROOT)}")
                break
            stack.pop()
    if stack:
        errors.append(f"Unclosed delimiter {stack[-1][0]}: {path.relative_to(ROOT)}")
    if re.search(r"\b(?:class|struct|interface)\s+\w+[^\n{]*;\s*$", clean, re.MULTILINE):
        errors.append(f"Suspicious semicolon-only type declaration: {path.relative_to(ROOT)}")

required = [
    ROOT / "ApiVault.sln",
    ROOT / "src/ApiVault.Api/Program.cs",
    ROOT / "src/ApiVault.Infrastructure/Persistence/ApiVaultDbContext.cs",
    ROOT / "src/ApiVault.Infrastructure/Http/SsrfGuard.cs",
]
for path in required:
    if not path.exists(): errors.append(f"Missing required file: {path.relative_to(ROOT)}")

if errors:
    print("Validation failed:")
    for error in errors:
        print(f"- {error}")
    sys.exit(1)

print("ApiVault structural validation passed.")
print(f"C# files: {len(list(ROOT.rglob('*.cs')))}")
print(f"JSON files: {len(list(ROOT.rglob('*.json')))}")
print(f"Project XML files: {sum(len(list(ROOT.rglob(p))) for p in ('*.csproj', '*.props', '*.targets'))}")
