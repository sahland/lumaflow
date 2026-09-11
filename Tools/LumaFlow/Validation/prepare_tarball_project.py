"""Prepare a minimal Built-In Unity project that installs an actual UPM archive."""
import argparse
import json
import re
import shutil
from pathlib import Path

from package_release import ROOT, LICENSE_PATH, inspect_archive


def prepare(archive, destination, version, channel):
    if not re.fullmatch(r"\d+\.\d+\.\d+[abfp]\d+", version):
        raise ValueError("Invalid Unity version")
    files = inspect_archive(archive.read_bytes(), channel, (ROOT / LICENSE_PATH).read_bytes())
    destination.mkdir(parents=True, exist_ok=False)
    packages = destination / "Packages"
    packages.mkdir()
    # Keep a portable relative file dependency, including inside GameCI containers.
    shutil.copyfile(archive, packages / "lumaflow.tgz")
    manifest = {"dependencies": {
        "com.sahland.lumaflow": "file:./lumaflow.tgz",
        "com.unity.test-framework": "1.4.5",
        "com.unity.modules.ui": "1.0.0",
        "com.unity.modules.imgui": "1.0.0",
    }}
    (packages / "manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    settings = destination / "ProjectSettings"
    settings.mkdir()
    (settings / "ProjectVersion.txt").write_text(f"m_EditorVersion: {version}\n", encoding="utf-8")
    # Unity creates default settings; no SRP packages or render pipeline assets are copied.
    tests = destination / "Assets/TarballTests"
    tests.mkdir(parents=True)
    for template in Path(__file__).parent.glob("LumaFlowTarball*.txt"):
        shutil.copyfile(template, tests / template.name.removesuffix(".txt"))
    for name, data in files.items():
        if name.startswith("Samples~/Getting Started/"):
            target = destination / "Assets/Samples/Getting Started" / name.removeprefix("Samples~/Getting Started/")
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(data)
    print(f"Prepared {channel} clean Built-In project for Unity {version}: {destination}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--archive", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--unity", required=True)
    parser.add_argument("--channel", choices=("git-upm", "asset-store"), required=True)
    args = parser.parse_args()
    prepare(args.archive, args.output, args.unity, args.channel)
