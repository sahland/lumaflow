"""Regression checks for invalid release inputs; fixtures never touch the package."""

import json
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

from validate_icons import PROVENANCE, validate, validate_svg


class IconValidationTests(unittest.TestCase):
    def check_svg(self, markup):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "fixture.svg"
            path.write_text(markup, encoding="utf-8")
            validate_svg(path)

    def test_accepts_nonstandard_positive_viewbox(self):
        self.check_svg('<svg xmlns="http://www.w3.org/2000/svg" viewBox="2 2 6 6" fill="#FFFFFF"><path d="M2 2h6v6z"/></svg>')

    def test_rejects_invalid_or_unsafe_svg(self):
        cases = [
            ('0 0 0 24', '<path fill="#FFFFFF"/>'),
            ('0 0 nan 24', '<path fill="#FFFFFF"/>'),
            ('0 0 24 24', '<path fill="#000000"/>'),
            ('0 0 24 24', '<script/>'),
            ('0 0 24 24', '<image href="https://example.com/icon.png"/>'),
            ('0 0 24 24', '<path fill="#FFFFFF" onload="run()"/>'),
            ('0 0 24 24', '<path fill="#FFFFFF" clip-path="url(https://example.com/clip)"/>'),
        ]
        for viewbox, child in cases:
            with self.subTest(viewbox=viewbox, child=child), self.assertRaises(ValueError):
                self.check_svg(f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="{viewbox}">{child}</svg>')

    def test_rejects_missing_provenance_and_license_changes(self):
        original = PROVENANCE.read_text(encoding="utf-8")
        for change in ("missing", "license", "revision"):
            data = json.loads(original)
            if change == "missing":
                data["icons"].pop()
            elif change == "license":
                data["icons"][0]["license"] = "different-license"
            else:
                data["icons"][0]["sourceUrl"] = "https://example.com/unpinned.svg"
            with tempfile.TemporaryDirectory() as directory:
                path = Path(directory) / "provenance.json"
                path.write_text(json.dumps(data), encoding="utf-8")
                with self.subTest(change=change), self.assertRaises(ValueError):
                    validate(provenance_path=path)

    def test_rejects_stale_generated_catalog(self):
        with patch("validate_icons.render_catalog", return_value="stale"):
            with self.assertRaisesRegex(ValueError, "stale LumaIcons"):
                validate()

    def test_current_package(self):
        self.assertEqual((95, 1), validate())


if __name__ == "__main__":
    unittest.main()
