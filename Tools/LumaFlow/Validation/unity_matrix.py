import argparse
import json
from pathlib import Path
import re


ROOT = Path(__file__).resolve().parents[3]
CONFIG = Path(__file__).with_name("unity-matrix.json")


def load(path=CONFIG):
    matrix = json.loads(path.read_text(encoding="utf-8"))
    if set(matrix) != {"unity", "testMode", "channel"}:
        raise ValueError("Unexpected Unity matrix axes")
    for axis, values in matrix.items():
        if not isinstance(values, list) or not values or any(not isinstance(value, str) for value in values):
            raise ValueError(f"Invalid matrix axis: {axis}")
        if len(set(values)) != len(values):
            raise ValueError(f"Duplicate matrix entries: {axis}")
    if any(not re.fullmatch(r"\d+\.\d+\.\d+f\d+", version) for version in matrix["unity"]):
        raise ValueError("Unity versions must be exact final-release pins")
    if set(matrix["testMode"]) != {"editmode", "playmode"}:
        raise ValueError("Both functional test modes are required")
    if set(matrix["channel"]) != {"git-upm", "asset-store"}:
        raise ValueError("Both package channels are required")
    manifest = json.loads((ROOT / "Packages/com.sahland.lumaflow/package.json").read_text(encoding="utf-8"))
    minimum = manifest["unity"] + "." + manifest["unityRelease"]
    if minimum not in matrix["unity"]:
        raise ValueError("Matrix does not cover the declared minimum Editor")
    return matrix


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--github-output", type=Path)
    args = parser.parse_args()
    matrix = load()
    if args.github_output:
        with args.github_output.open("a", encoding="utf-8") as output:
            for key, value in matrix.items():
                output.write(f"{key}={json.dumps(value, separators=(',', ':'))}\n")
    print(json.dumps(matrix))
