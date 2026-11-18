from pathlib import Path
import tempfile
import unittest

from check_api_contract import check_unchanged


class FrozenContractTests(unittest.TestCase):
    def test_keeps_existing_contract(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / "v1.txt").write_text("frozen\n", encoding="utf-8")
            self.assertEqual(check_unchanged({"v1.txt": "frozen\n"}, root), [])

    def test_cannot_rewrite_existing_contract(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / "v1.txt").write_text("changed\n", encoding="utf-8")
            self.assertTrue(check_unchanged({"v1.txt": "frozen\n"}, root))

    def test_cannot_delete_existing_contract(self):
        with tempfile.TemporaryDirectory() as directory:
            self.assertTrue(check_unchanged({"v1.txt": "frozen\n"}, Path(directory)))

    def test_new_version_does_not_replace_old_contract(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / "v1.txt").write_text("frozen\n", encoding="utf-8")
            (root / "v2.txt").write_text("new\n", encoding="utf-8")
            self.assertEqual(check_unchanged({"v1.txt": "frozen\n"}, root), [])

    def test_line_endings_do_not_change_contract(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / "v1.txt").write_text("frozen\n", encoding="utf-8")
            self.assertEqual(check_unchanged({"v1.txt": "frozen\r\n"}, root), [])
