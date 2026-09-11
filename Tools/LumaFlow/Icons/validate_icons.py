#!/usr/bin/env python3
"""Validate packaged SVGs, stable catalog mappings and CC0 provenance offline."""

import argparse
import json
import math
import re
import sys
from pathlib import Path
from xml.etree import ElementTree

from generate_luma_icons import BRAND_ENTRIES, catalog_block, load_entries, render_catalog

ROOT = Path(__file__).resolve().parents[3]
PACKAGE = ROOT / "Packages/com.sahland.lumaflow"
MANIFEST = Path(__file__).with_name("icon-catalog.json")
PROVENANCE = ROOT / "Tools/AssetStoreProvenance/cc0-icons.json"
ASSET_DIRECTORY = "Runtime/Icons/Resources/LumaFlowIcons"


def require(condition, message):
    if not condition:
        raise ValueError(message)


def validate_svg(path):
    text = path.read_text(encoding="utf-8")
    require("<!DOCTYPE" not in text.upper() and "<!ENTITY" not in text.upper(),
            f"DTD/entity declaration in {path.name}")
    root = ElementTree.fromstring(text)
    require(root.tag == "{http://www.w3.org/2000/svg}svg", f"invalid SVG root in {path.name}")
    bounds = [float(value) for value in re.split(r"[\s,]+", root.get("viewBox", "").strip())]
    require(len(bounds) == 4 and all(math.isfinite(value) for value in bounds)
            and bounds[2] > 0 and bounds[3] > 0, f"invalid viewBox in {path.name}")
    white = False
    for element in root.iter():
        tag = element.tag.rsplit("}", 1)[-1].lower()
        require(tag in {"svg", "g", "path", "rect", "circle", "ellipse", "line",
                        "polyline", "polygon", "defs", "clippath", "title", "desc"},
                f"unsupported SVG element {tag} in {path.name}")
        for key, value in element.attrib.items():
            key = key.rsplit("}", 1)[-1].lower()
            require(not key.startswith("on") and key not in {"href", "style", "base"},
                    f"unsupported SVG attribute {key} in {path.name}")
            if "url(" in value.lower():
                require(re.fullmatch(r"url\(#[\w.-]+\)", value) is not None,
                        f"external/unsupported reference in {path.name}")
            if key in {"fill", "stroke"}:
                require(value.lower() in {"none", "#ffffff", "#fff", "white"},
                        f"non-tintable paint in {path.name}")
                white |= value.lower() != "none"
    require(white, f"missing white tint base in {path.name}")


def validate(package=PACKAGE, manifest_path=MANIFEST, provenance_path=PROVENANCE):
    entries = load_entries(manifest_path)
    all_entries = [*entries, *BRAND_ENTRIES]
    names = [entry["name"] for entry in all_entries]
    sources = [entry["source"] for entry in all_entries]
    require(len(names) == len(set(names)), "duplicate public name")
    require(len(sources) == len(set(sources)), "duplicate resource mapping")
    require(all(re.fullmatch(r"[A-Z][A-Za-z0-9]*", name) for name in names), "invalid C# name")
    require(all(re.fullmatch(r"[a-z0-9]+(?:-[a-z0-9]+)*", source) for source in sources),
            "invalid resource name")

    provenance = json.loads(provenance_path.read_text(encoding="utf-8"))
    require(provenance["schemaVersion"] == 1, "unsupported provenance schema")
    require(provenance["license"] == "CC0-1.0", "unexpected collection license")
    revision = provenance["sourceCommit"]
    require(re.fullmatch(r"[0-9a-f]{40}", revision), "source commit is not a full SHA")
    repository = provenance["sourceRepository"]
    require(repository == "https://github.com/Nieobie/game-icon-pack", "unexpected source repository")
    records = provenance["icons"]
    public_ids = [record["publicIcon"] for record in records]
    require(len(public_ids) == len(set(public_ids)), "duplicate provenance entry")
    require(set(public_ids) == {entry["source"] for entry in entries}, "provenance/catalog mismatch")
    for record in records:
        source = record["publicIcon"]
        require(record["packagedFile"] == f"{ASSET_DIRECTORY}/{source}.svg",
                f"incorrect packaged path for {source}")
        require(record["license"] == "CC0-1.0" and record["licenseUrl"] == provenance["licenseUrl"],
                f"incorrect license record for {source}")
        require(record["sourceUrl"] == f"https://raw.githubusercontent.com/Nieobie/game-icon-pack/{revision}/{record['sourceFile']}",
                f"source URL is not pinned to collection revision for {source}")
        require(re.fullmatch(r"[0-9a-f]{64}", record["sourceSha256"]), f"invalid upstream hash for {source}")

    assets = package / ASSET_DIRECTORY
    require({path.stem for path in assets.glob("*.svg")} == set(sources), "asset set and catalog differ")
    for source in sources:
        path = assets / f"{source}.svg"
        require(path.with_suffix(".svg.meta").is_file(), f"missing Unity metadata for {source}")
        validate_svg(path)
    catalog = (package / "Runtime/Controls/Button.cs").read_text(encoding="utf-8")
    require(catalog_block(catalog) == render_catalog(entries).rstrip("\n"), "stale LumaIcons catalog")
    return len(entries), len(BRAND_ENTRIES)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--package", type=Path, default=PACKAGE)
    args = parser.parse_args()
    try:
        icons, brands = validate(args.package)
    except (ValueError, KeyError, TypeError, OSError, ElementTree.ParseError) as error:
        print(f"icon validation failed: {error}", file=sys.stderr)
        return 1
    print(f"validated {icons} CC0 icons and {brands} separate brand icon")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
