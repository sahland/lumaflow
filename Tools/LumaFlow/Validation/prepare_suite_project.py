import argparse
import json
from pathlib import Path
import shutil

from package_release import ROOT, PREFIX, allowed
from unity_matrix import load


def prepare(destination, version):
    if version not in load()["unity"]:
        raise ValueError("Editor version is outside the reviewed CI matrix")
    destination = destination.resolve()
    if destination == ROOT or destination in ROOT.parents or destination.is_relative_to(ROOT / "Assets") or destination.is_relative_to(ROOT / "Packages"):
        raise ValueError("Cannot prepare a validation project over source assets")
    destination.mkdir(parents=True, exist_ok=False)
    package = destination / PREFIX
    source = ROOT / PREFIX
    for path in source.rglob("*"):
        relative = path.relative_to(source).as_posix()
        if path.is_file() and allowed(relative):
            target = package / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(path, target)
    tests = destination / "Assets/LumaFlow.Tests"
    shutil.copytree(ROOT / "Assets/LumaFlow.Tests", tests)
    sample = destination / "Assets/GettingStarted"
    shutil.copytree(source / "Samples~/Getting Started", sample)
    packages = destination / "Packages"
    manifest = {"dependencies": {
        "com.unity.test-framework": "1.6.0",
        "com.unity.test-framework.performance": "3.4.0",
        "com.unity.modules.ui": "1.0.0",
        "com.unity.modules.imgui": "1.0.0",
    }, "testables": ["com.sahland.lumaflow"]}
    (packages / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    settings = destination / "ProjectSettings"
    settings.mkdir()
    (settings / "ProjectVersion.txt").write_text(f"m_EditorVersion: {version}\n", encoding="utf-8")
    print(f"Prepared isolated functional suites for Unity {version}: {destination}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--unity", required=True)
    args = parser.parse_args()
    prepare(args.output, args.unity)
