import json
from pathlib import Path
import re
import unittest
from unity_matrix import load


ROOT = Path(__file__).resolve().parents[3]
PACKAGE = ROOT / "Packages/com.sahland.lumaflow"


class ReleaseMetadataTests(unittest.TestCase):
    def test_current_manifest_uses_agreed_release_version(self):
        manifest = json.loads((PACKAGE / "package.json").read_text(encoding="utf-8"))
        self.assertEqual(manifest["version"], "0.2.0",
                         "The agreed package version must not contain a preview suffix.")
        self.assertEqual(manifest["unity"], "6000.2")
        self.assertEqual(manifest["unityRelease"], "2f1")
        version = manifest["version"]
        changelog = (PACKAGE / "CHANGELOG.md").read_text(encoding="utf-8")
        self.assertEqual(re.search(r"^## \[([^]]+)\]", changelog, re.MULTILINE).group(1), version)
        self.assertIn(f"## [{version}] - Unreleased", changelog)
        for name in ("README.md", "Documentation~/Compatibility Policy.md", "Documentation~/Migration Guide.md"):
            self.assertIn(version, (PACKAGE / name).read_text(encoding="utf-8"), name)

    def test_ci_covers_the_declared_minimum_editor(self):
        workflow = (ROOT / ".github/workflows/unity-validation.yml").read_text(encoding="utf-8")
        self.assertEqual(load()["unity"], ["6000.2.2f1", "6000.4.5f1"])
        self.assertEqual(workflow.count("unity: ${{ fromJSON(needs.unity-matrix.outputs.unity) }}"), 2)
        contract = (PACKAGE / "Editor/RuntimeApiContract.v1.txt").read_text(encoding="utf-8")
        self.assertIn("# Assembly: LumaFlow.Runtime", contract)


if __name__ == "__main__":
    unittest.main()
