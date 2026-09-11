# LumaFlow Lucide import workflow

`lucide-icons.json` is the source of truth for the curated Lucide subset. Brand
entries are declared separately in `generate_luma_icons.py` so their origin and
licensing are not attributed to Lucide.
From the repository root, run `python Tools/LumaFlow/Icons/generate_luma_icons.py --check-catalog Packages/com.sahland.lumaflow/Runtime/Controls/Button.cs`
to verify the typed catalog. `generate_luma_icons.py --output
<path>` produces the same catalog in a standalone source file when the package
layout is changed. Refreshing Lucide sources is deliberate: update the manifest,
copy only those official SVGs, then run `validate_lucide_icons.py`. The validator
also checks the separately declared brand assets.

Normal Unity/package builds never access the network.
