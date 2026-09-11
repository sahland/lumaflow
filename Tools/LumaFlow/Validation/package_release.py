#!/usr/bin/env python3
"""Build and inspect reproducible UPM payloads from a committed development tree."""

import argparse
import gzip
import hashlib
import io
import json
import subprocess
import sys
import tarfile
import tempfile
from pathlib import Path, PurePosixPath

ROOT = Path(__file__).resolve().parents[3]
PREFIX = "Packages/com.sahland.lumaflow/"
LICENSE_PATH = "Tools/LumaFlow/Validation/Git-UPM-LICENSE.txt"
FOLDERS = {"Runtime", "Editor", "Samples~", "Documentation~"}
ROOT_FILES = {"package.json", "README.md", "CHANGELOG.md", "Runtime.meta", "Editor.meta"}
DEV_DOCS = {"Documentation~/Continuous Integration.md", "Documentation~/Validation.md"}
REQUIRED = {
    "package.json", "Runtime/LumaFlow.Runtime.asmdef", "Editor/LumaFlow.Editor.asmdef",
    "Runtime/Controls/Button.cs", "Documentation~/index.md",
    "Samples~/Getting Started/LumaFlowCounterSample.cs",
}
sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "Icons"))
from validate_icons import validate as validate_icons


def require(condition, message):
    if not condition:
        raise ValueError(message)


def allowed(name):
    path = PurePosixPath(name)
    base = name.removesuffix(".meta")
    return (not path.is_absolute() and ".." not in path.parts
            and "\\" not in name
            and (path.parts[0] in FOLDERS or name in ROOT_FILES or base in ROOT_FILES)
            and base not in DEV_DOCS
            and not any(part.startswith(".") for part in path.parts)
            and not name.endswith((".tgz", ".tgz.meta")))


def validate_files(files, channel, license_bytes):
    require(REQUIRED <= files.keys(), f"missing required files: {REQUIRED - files.keys()}")
    require(all(allowed(name) or name == "LICENSE" for name in files), "unexpected package files")
    if channel == "git-upm":
        require(files.get("LICENSE") == license_bytes, "Git-UPM license missing or altered")
    else:
        require(channel == "asset-store", "unknown channel")
        require("LICENSE" not in files, "independent root license in Asset Store payload")
    manifest = json.loads(files["package.json"])
    require(manifest["name"] == "com.sahland.lumaflow", "wrong package identity")
    require(all(not str(value).startswith("file:") for value in manifest.get("dependencies", {}).values()),
            "local dependency in consumer package")
    for name in files:
        if name.endswith((".cs", ".asmdef", ".svg", ".png")):
            require(name + ".meta" in files, f"missing metadata: {name}")
    with tempfile.TemporaryDirectory(prefix="lumaflow-icons-") as directory:
        package = Path(directory)
        for name, data in files.items():
            path = package / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(data)
        validate_icons(package)


def encode(files):
    output = io.BytesIO()
    with gzip.GzipFile(fileobj=output, mode="wb", filename="", mtime=0) as compressed:
        with tarfile.open(fileobj=compressed, mode="w", format=tarfile.PAX_FORMAT) as archive:
            for name, data in sorted(files.items()):
                entry = tarfile.TarInfo("package/" + name)
                entry.size = len(data)
                entry.mode = 0o644
                archive.addfile(entry, io.BytesIO(data))
    return output.getvalue()


def inspect_archive(data, channel, license_bytes):
    files = {}
    with tarfile.open(fileobj=io.BytesIO(data), mode="r:gz") as archive:
        for entry in archive:
            require(entry.isfile() and entry.name.startswith("package/"), "invalid archive member")
            name = entry.name.removeprefix("package/")
            require(name not in files, "duplicate archive member")
            require(allowed(name) or name == "LICENSE", f"unsafe/unexpected member: {name}")
            files[name] = archive.extractfile(entry).read()
    validate_files(files, channel, license_bytes)
    return files


def build(commit, destination):
    revision = subprocess.check_output(["git", "rev-parse", "--verify", commit + "^{commit}"], cwd=ROOT, text=True).strip()
    raw = subprocess.check_output(["git", "archive", revision, PREFIX.rstrip("/"), LICENSE_PATH], cwd=ROOT)
    files = {}
    license_bytes = None
    with tarfile.open(fileobj=io.BytesIO(raw)) as archive:
        for entry in archive:
            if entry.name == LICENSE_PATH:
                license_bytes = archive.extractfile(entry).read()
            elif entry.isfile() and entry.name.startswith(PREFIX):
                name = entry.name.removeprefix(PREFIX)
                if allowed(name):
                    files[name] = archive.extractfile(entry).read()
    require(license_bytes is not None, "commit does not contain Git-UPM license template")
    version = json.loads(files["package.json"])["version"]
    require(all(c.isalnum() or c in ".-+" for c in version), "unsafe package version")
    destination.mkdir(parents=True, exist_ok=False)
    report = {"sourceCommit": revision, "version": version, "validation": "archive contents and icon checks; Unity import not run", "artifacts": {}}
    for channel in ("git-upm", "asset-store"):
        payload = dict(files)
        if channel == "git-upm":
            payload["LICENSE"] = license_bytes
        data = encode(payload)
        checked = inspect_archive(data, channel, license_bytes)
        require(checked == payload, "archive roundtrip changed file contents")
        name = f"com.sahland.lumaflow-{version}-{channel}.tgz"
        (destination / name).write_bytes(data)
        report["artifacts"][channel] = {"file": name, "sha256": hashlib.sha256(data).hexdigest(), "files": len(checked)}
    (destination / "release-report.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--commit", default="HEAD")
    parser.add_argument("--output", type=Path, required=True, help="New output directory; existing paths are never overwritten")
    args = parser.parse_args()
    try:
        build(args.commit, args.output)
    except (ValueError, OSError, KeyError, subprocess.CalledProcessError, tarfile.TarError) as error:
        parser.exit(1, f"release packaging failed: {error}\n")
