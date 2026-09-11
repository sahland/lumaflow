import io
import subprocess
import tarfile
import unittest
import tempfile
import json
from pathlib import Path

from package_release import ROOT, PREFIX, LICENSE_PATH, allowed, encode, inspect_archive
from prepare_tarball_project import prepare
from check_smoke_results import check, EXPECTED


class ReleaseTests(unittest.TestCase):
    def test_smoke_evidence_requires_every_test(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            with self.assertRaises(ValueError):
                check(root)
            cases = ''.join(f'<test-case methodname="{name}" result="Passed"/>' for name in EXPECTED)
            path = root / 'results.xml'
            path.write_text(f'<test-run result="Passed">{cases}</test-run>', encoding='utf-8')
            check(root)
            path.write_text(f'<test-run result="Failed">{cases}</test-run>', encoding='utf-8')
            with self.assertRaises(ValueError):
                check(root)

    @classmethod
    def setUpClass(cls):
        raw = subprocess.check_output(["git", "archive", "HEAD", PREFIX.rstrip("/")], cwd=ROOT)
        with tarfile.open(fileobj=io.BytesIO(raw)) as archive:
            cls.files = {entry.name.removeprefix(PREFIX): archive.extractfile(entry).read()
                         for entry in archive if entry.isfile()
                         and entry.name.startswith(PREFIX) and allowed(entry.name.removeprefix(PREFIX))}
        cls.license = (ROOT / LICENSE_PATH).read_bytes()

    def test_channel_rules_and_reproducibility(self):
        store = encode(self.files)
        git = encode(dict(self.files, LICENSE=self.license))
        self.assertEqual(store, encode(self.files))
        self.assertEqual(self.files, inspect_archive(store, "asset-store", self.license))
        self.assertEqual(dict(self.files, LICENSE=self.license), inspect_archive(git, "git-upm", self.license))
        with self.assertRaisesRegex(ValueError, "independent root license"):
            inspect_archive(git, "asset-store", self.license)
        with self.assertRaisesRegex(ValueError, "license missing"):
            inspect_archive(store, "git-upm", self.license)

    def test_rejects_missing_metadata(self):
        files = dict(self.files)
        del files["Runtime/Controls/Button.cs.meta"]
        with self.assertRaisesRegex(ValueError, "missing metadata"):
            inspect_archive(encode(files), "asset-store", self.license)

    def test_clean_project_uses_archive_and_no_srp(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            archive = root / "input.tgz"
            archive.write_bytes(encode(self.files))
            destination = root / "consumer"
            prepare(archive, destination, "6000.0.0f1", "asset-store")
            dependencies = json.loads((destination / "Packages/manifest.json").read_text())["dependencies"]
            self.assertEqual("file:./lumaflow.tgz", dependencies["com.sahland.lumaflow"])
            self.assertFalse(any("render-pipelines" in name for name in dependencies))
            self.assertEqual(archive.read_bytes(), (destination / "Packages/lumaflow.tgz").read_bytes())
            self.assertTrue((destination / "Assets/Samples/Getting Started/LumaFlowCounterSample.cs").is_file())
            self.assertTrue((destination / "Assets/TarballTests/LumaFlowTarballEditorSmokeTests.cs").is_file())
            with self.assertRaises(FileExistsError):
                prepare(archive, destination, "6000.0.0f1", "asset-store")

    def test_rejects_unexpected_and_traversal_entries(self):
        for name in ("../escape", "Runtime/../../escape", "Tools/private.txt", "Runtime/.env"):
            with self.subTest(name=name), self.assertRaisesRegex(ValueError, "unsafe/unexpected"):
                inspect_archive(encode(dict(self.files, **{name: b"bad"})), "asset-store", self.license)


if __name__ == "__main__":
    unittest.main()
