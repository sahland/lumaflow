import argparse
from pathlib import Path
import re
import subprocess


ROOT = Path(__file__).resolve().parents[3]
PATTERN = re.compile(r"^Packages/com\.sahland\.lumaflow/Editor/RuntimeApiContract\.v[1-9][0-9]*\.txt$")


def check_unchanged(previous, root):
    errors = []
    for name, expected in previous.items():
        path = root / name
        if not path.is_file():
            errors.append(f"Frozen API contract was removed: {name}")
        elif path.read_text(encoding="utf-8").replace("\r\n", "\n") != expected.replace("\r\n", "\n"):
            errors.append(f"Frozen API contract was rewritten: {name}. Add a new contract version for a deliberate major release.")
    return errors


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--base", default="")
    args = parser.parse_args()
    base = args.base.strip()
    if not base or set(base) == {"0"}:
        base = "origin/main"
    subprocess.run(["git", "rev-parse", "--verify", f"{base}^{{commit}}"], cwd=ROOT, check=True, capture_output=True)
    names = subprocess.check_output(["git", "ls-tree", "-r", "--name-only", base], cwd=ROOT, text=True).splitlines()
    previous = {
        name: subprocess.check_output(["git", "show", f"{base}:{name}"], cwd=ROOT).decode("utf-8")
        for name in names if PATTERN.fullmatch(name)
    }
    errors = check_unchanged(previous, ROOT)
    current = list((ROOT / "Packages/com.sahland.lumaflow/Editor").glob("RuntimeApiContract.v*.txt"))
    if not current:
        errors.append("No frozen Runtime API contract is present.")
    for error in errors:
        print(error)
    if errors:
        raise SystemExit(1)
    print(f"Frozen API contracts unchanged: {len(previous)} existing; {len(current)} current.")


if __name__ == "__main__":
    main()
