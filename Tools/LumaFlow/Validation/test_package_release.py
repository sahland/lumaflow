import io
import subprocess
import tarfile
import unittest

from package_release import ROOT, PREFIX, LICENSE_PATH, allowed, encode, inspect_archive


class ReleaseTests(unittest.TestCase):
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

    def test_rejects_unexpected_and_traversal_entries(self):
        for name in ("../escape", "Runtime/../../escape", "Tools/private.txt", "Runtime/.env"):
            with self.subTest(name=name), self.assertRaisesRegex(ValueError, "unsafe/unexpected"):
                inspect_archive(encode(dict(self.files, **{name: b"bad"})), "asset-store", self.license)


if __name__ == "__main__":
    unittest.main()
