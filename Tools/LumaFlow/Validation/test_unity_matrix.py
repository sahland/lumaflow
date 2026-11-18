import copy
import json
from pathlib import Path
import tempfile
import os
import subprocess
import sys
import textwrap
import unittest

from unity_matrix import load, ROOT
from prepare_suite_project import prepare
from check_suite_results import check


class UnityMatrixTests(unittest.TestCase):
    def test_reviewed_axes_are_complete(self):
        matrix = load()
        self.assertEqual(matrix["unity"], ["6000.2.2f1", "6000.4.5f1"])
        self.assertEqual(matrix["testMode"], ["editmode", "playmode"])
        self.assertEqual(matrix["channel"], ["git-upm", "asset-store"])
        self.assertEqual(len(matrix["unity"]) * len(matrix["testMode"]), 4)

    def test_invalid_or_incomplete_axes_fail_closed(self):
        for axis, value in (("unity", ["auto"]), ("unity", ["6000.4.5f1"]),
                            ("unity", ["6000.2.2f1", "6000.2.2f1"]),
                            ("testMode", ["editmode"]), ("channel", ["git-upm"])):
            with self.subTest(axis=axis, value=value), tempfile.TemporaryDirectory() as directory:
                matrix = copy.deepcopy(load())
                matrix[axis] = value
                path = Path(directory) / "matrix.json"
                path.write_text(json.dumps(matrix), encoding="utf-8")
                with self.assertRaises(ValueError):
                    load(path)

    def test_clean_project_keeps_tests_and_contract_without_development_settings(self):
        with tempfile.TemporaryDirectory() as directory:
            for version in load()["unity"]:
                destination = Path(directory) / version
                prepare(destination, version)
                manifest = json.loads((destination / "Packages/manifest.json").read_text(encoding="utf-8"))
                self.assertFalse(any("render-pipelines" in key for key in manifest["dependencies"]))
                self.assertIn("com.sahland.lumaflow", manifest["testables"])
                self.assertIn(version, (destination / "ProjectSettings/ProjectVersion.txt").read_text())
                for relative in ("Assets/LumaFlow.Tests/Editor/PublicApiBaseline.txt",
                                 "Assets/LumaFlow.Tests/Runtime/LumaFlow.Runtime.Tests.asmdef",
                                 "Packages/com.sahland.lumaflow/Editor/RuntimeApiContract.v1.txt",
                                 "Assets/GettingStarted/LumaFlowCounterSample.cs"):
                    self.assertTrue((destination / relative).is_file(), relative)
                self.assertFalse((destination / "ProjectSettings/GraphicsSettings.asset").exists())
                self.assertFalse((destination / "Assets/Scenes").exists())
                with self.assertRaises(FileExistsError):
                    prepare(destination, version)
            with self.assertRaises(ValueError):
                prepare(Path(directory) / "bad", "6000.0.0f1")
        with self.assertRaises(ValueError):
            prepare(ROOT, load()["unity"][0])

    def test_evidence_requires_correct_assembly_and_actual_passing_cases(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            with self.assertRaises(ValueError):
                check(root, "editmode")
            path = root / "results.xml"
            xml = '<test-run result="Passed"><test-suite type="Assembly" name="LumaFlow.Editor.Tests.dll" result="Passed"><test-case name="Example" result="Passed"/></test-suite></test-run>'
            path.write_text(xml, encoding="utf-8")
            self.assertEqual(check(root, "editmode"), 1)
            with self.assertRaises(ValueError):
                check(root, "playmode")
            for bad in (xml.replace('name="Example" result="Passed"', 'name="Example" result="Skipped"'),
                        xml.replace('<test-case name="Example" result="Passed"/>', ''),
                        xml.replace('test-run result="Passed"', 'test-run result="Passed" skipped="1"'),
                        xml.replace('test-run result="Passed"', 'test-run result="Failed"')):
                path.write_text(bad, encoding="utf-8")
                with self.assertRaises(ValueError):
                    check(root, "editmode")

    def test_workflow_has_unique_evidence_paths_and_an_always_gate(self):
        workflow = (ROOT / ".github/workflows/unity-validation.yml").read_text(encoding="utf-8")
        functional = workflow.split("\n  package-tests:", 1)[1].split("\n  unity-validation-gate:", 1)[0]
        self.assertIn("projectPath: Artifacts/suite", functional)
        self.assertIn("unityVersion: ${{ matrix.unity }}", functional)
        self.assertIn("check_suite_results.py", functional)
        self.assertIn("suite-${{ matrix.unity }}-${{ matrix.testMode }}-results", functional)
        self.assertNotIn("continue-on-error:", functional)
        gate = workflow.split("\n  unity-validation-gate:", 1)[1]
        self.assertIn("if: always()", gate)
        self.assertIn("job[\"result\"] != \"success\"", gate)
        for job in ("unity-matrix", "unity-license-preflight", "release-payloads", "icon-validation", "clean-package-tests", "package-tests"):
            self.assertIn(job, gate)

    def test_final_gate_rejects_failed_skipped_and_cancelled_dependencies(self):
        workflow = (ROOT / ".github/workflows/unity-validation.yml").read_text(encoding="utf-8")
        gate = workflow.split("\n  unity-validation-gate:", 1)[1]
        script = textwrap.dedent(gate.split("python3 - <<'PY'\n", 1)[1].rsplit("          PY", 1)[0])
        expected = {"unity-matrix", "unity-license-preflight", "release-payloads", "icon-validation", "clean-package-tests", "package-tests"}
        self.assertEqual({job.strip() for job in gate.split("needs: [", 1)[1].split("]", 1)[0].split(",")}, expected)
        results = {job: {"result": "success"} for job in expected}
        for status in ("success", "failure", "skipped", "cancelled"):
            with self.subTest(status=status):
                results["package-tests"]["result"] = status
                env = dict(os.environ, NEEDS_RESULTS=json.dumps(results))
                result = subprocess.run([sys.executable, "-c", script], env=env, capture_output=True, text=True)
                self.assertEqual(result.returncode == 0, status == "success", result.stderr)
        env = dict(os.environ, NEEDS_RESULTS="{}")
        self.assertNotEqual(subprocess.run([sys.executable, "-c", script], env=env, capture_output=True).returncode, 0)


if __name__ == "__main__":
    unittest.main()
