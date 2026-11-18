import argparse
from pathlib import Path
from xml.etree import ElementTree


ASSEMBLIES = {"editmode": "LumaFlow.Editor.Tests", "playmode": "LumaFlow.Runtime.Tests"}


def check(directory, mode):
    expected = ASSEMBLIES[mode]
    passed = 0
    found = False
    for path in directory.rglob("*.xml"):
        root = ElementTree.parse(path).getroot()
        if root.tag != "test-run":
            continue
        if root.get("result") != "Passed" or any(int(root.get(name, "0")) for name in ("failed", "skipped", "inconclusive")):
            raise ValueError(f"Unity suite failed or skipped tests: {path}")
        cases = list(root.iter("test-case"))
        if not cases or any(case.get("result") != "Passed" for case in cases):
            raise ValueError(f"Empty or non-passing test cases: {path}")
        for suite in root.iter("test-suite"):
            name = suite.get("name", "").removesuffix(".dll")
            if suite.get("type") == "Assembly" and name == expected:
                suite_cases = list(suite.iter("test-case"))
                if suite.get("result") != "Passed" or not suite_cases:
                    raise ValueError(f"Missing passing assembly cases: {path}")
                found = True
                passed += len(suite_cases)
    if not found:
        raise ValueError(f"Missing actual passing results for {expected}")
    print(f"{expected}: {passed} passing test cases")
    return passed


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("directory", type=Path)
    parser.add_argument("--mode", choices=tuple(ASSEMBLIES), required=True)
    args = parser.parse_args()
    check(args.directory, args.mode)
