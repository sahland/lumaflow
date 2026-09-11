"""Require all package smoke tests to execute and pass; an empty run is a failure."""
import argparse
from pathlib import Path
from xml.etree import ElementTree

EXPECTED = {"EditorAssemblyAndBrandAssetAreAvailable", "PackedPackageBuildsInAStandalonePlayer",
            "RuntimeResourcesAndWidgetMountAreAvailable"}


def check(directory):
    passed = set()
    for path in directory.rglob("*.xml"):
        root = ElementTree.parse(path).getroot()
        if root.tag != "test-run":
            continue
        if root.get("result") != "Passed" or int(root.get("failed", "0")):
            raise ValueError(f"Unity suite did not pass: {path}")
        for case in root.iter("test-case"):
            if case.get("result") == "Passed":
                passed.add(case.get("methodname", case.get("name")))
    if EXPECTED - passed:
        raise ValueError(f"Missing passing smoke tests: {sorted(EXPECTED - passed)}")
    print("All three package smoke tests passed, including standalone build.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("directory", type=Path)
    args = parser.parse_args()
    check(args.directory)
