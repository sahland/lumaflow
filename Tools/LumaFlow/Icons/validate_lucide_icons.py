#!/usr/bin/env python3
"""Validates the committed Lucide subset without network or UnityEditor APIs."""
import json
import re
import sys
from pathlib import Path
from xml.etree import ElementTree

from generate_luma_icons import BRAND_ENTRIES, catalog_block, load_entries, render_catalog

REPOSITORY_ROOT = Path(__file__).resolve().parents[3]
PACKAGE_ROOT = REPOSITORY_ROOT / "Packages" / "com.sahland.lumaflow"
MANIFEST = Path(__file__).with_name("lucide-icons.json")
ASSETS = PACKAGE_ROOT / "Runtime/Icons/Resources/LumaFlowIcons"
CATALOG = PACKAGE_ROOT / "Runtime/Controls/Button.cs"

def fail(message):
    print(f"icon validation failed: {message}", file=sys.stderr)
    raise SystemExit(1)

manifest = json.loads(MANIFEST.read_text())
entries = manifest["icons"]
names = [entry["name"] for entry in entries]
sources = [entry["source"] for entry in entries]
brand_sources = [entry["source"] for entry in BRAND_ENTRIES]
if len(names) != len(set(names)): fail("duplicate public name")
if len(sources) != len(set(sources)): fail("duplicate source mapping")
if not re.fullmatch(r"[0-9a-f]{40}", manifest["revision"]): fail("revision is not a SHA")
for source in [*sources, *brand_sources]:
    path = ASSETS / f"{source}.svg"
    if not path.is_file(): fail(f"missing asset {source}")
    root = ElementTree.parse(path).getroot()
    if root.tag.rsplit("}", 1)[-1] != "svg" or root.attrib.get("viewBox") != "0 0 24 24": fail(f"invalid viewBox in {source}")
    text = path.read_text()
    if any(token in text.lower() for token in ("<script", "<image", "<animate", "<text")): fail(f"unsafe SVG {source}")
    if re.search(r"(?:href|url)=[\"']https?://", text, re.IGNORECASE): fail(f"external URL in {source}")
    if "#000000" in text or "currentColor" in text: fail(f"non-tintable SVG source color in {source}")
    if "#FFFFFF" not in text: fail(f"missing white tint base in {source}")
catalog = CATALOG.read_text()
if catalog_block(catalog) != render_catalog(entries).rstrip("\n"):
    fail("LumaIcons catalog does not match generated manifest output")
if {path.stem for path in ASSETS.glob("*.svg")} != set([*sources, *brand_sources]): fail("asset set and catalog differ")
print(f"validated {len(entries)} Lucide icons and {len(brand_sources)} brand icon")
