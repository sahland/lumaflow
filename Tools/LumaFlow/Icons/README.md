# LumaFlow icon maintenance

`icon-catalog.json` maps stable public C# names to resource identifiers. Keep
these mappings compatible when replacing artwork. Asset origins, pinned upstream
URLs, licenses and original source hashes are recorded separately in
`Tools/AssetStoreProvenance/cc0-icons.json` for the 95 CC0 Game Icon Pack assets.
The LumaFlow brand icon is declared in `generate_luma_icons.py` and is not
attributed to that collection.

Run from the development project root with Python 3:

```sh
python Tools/LumaFlow/Icons/validate_icons.py
python Tools/LumaFlow/Icons/generate_luma_icons.py --check-catalog Packages/com.sahland.lumaflow/Runtime/Controls/Button.cs
python -m unittest discover -s Tools/LumaFlow/Icons -p "test_*.py"
```

The validator checks one-to-one catalog/provenance coverage, pinned source URLs,
license records, complete SVG and Unity metadata sets, finite positive viewBox
dimensions, white tintable paint, supported SVG elements and local-only
references. CC0 assets use individual viewBox bounds; they need not be 0 0 24 24.
Pass `--package <extracted-package-directory>` to inspect an unpacked release.

Checks are offline. `sourceSha256` describes the original upstream file, not the
recolored packaged SVG. Its format is checked, but the validator does not fetch
upstream files or certify license ownership. When importing artwork, verify the
upstream revision and license and record its original hash before transformation.

`generate_luma_icons.py --output <path>` emits a standalone catalog source file.
Do not replace the entire `Button.cs` with that output: the file also defines
other controls. Only the marked catalog block is generated.

The GitHub Actions icon-validation job runs these checks independently of Unity
and requires no Unity license. Consumer packages do not include developer tools.
