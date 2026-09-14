# Experimental C# to UXML preview

LumaFlow can export an initial visual snapshot as UXML and USS without entering
Play Mode. The exporter mounts a temporary tree through the existing runtime
nodes, serializes its native elements and inline styles, then disposes the mount.
Layout and styling therefore use the same mappers as ordinary LumaFlow code.
This is an experimental snapshot exporter, not a replacement runtime backend.

## Try the prototype

1. Create an asset using **Assets > Create > LumaFlow > UXML Preview Demo**.
2. Open **Tools > LumaFlow > UXML Preview (Experimental)** and select that asset.
3. Edit preview data directly in the window's sidebar. **Auto refresh** regenerates
   after data edits; **Generate** remains available for manual refresh.
   **Edit factory C#** opens the selected factory's source.
4. Edit the asset's preview data, or edit its C# factory and let Unity recompile.
   With the window open, preview generation runs again after assembly reload.
5. Click **Toggle Game View preview** to display the generated tree through a
   temporary UIDocument in Game View. A Theme Style Sheet asset is required;
   the generated PanelSettings asset can be configured for your project's theme.

Generated files live under `Assets/LumaFlowGenerated/<factory-asset-guid>/`.
The window opens in single-preview mode. Enable **Compare runtime** to display
both backends. Phone/tablet/desktop presets and the width/height fields set the
preview viewport; the 25–100% scale changes only its display size. Scrollbars
remain available for larger canvases. These settings affect the editor canvas,
not the actual Game View resolution or the generated layout rules.
**Open UXML**, **Open USS** and **Show generated files** provide direct access to
the output. **New demo** creates and selects a factory without leaving the window.
Unchanged files are not rewritten. Edit the factory rather than these files.
You can also assign `Preview.uxml` to your own UIDocument. The temporary preview
   object is removed when the window closes or Play Mode starts; it is not saved
into your scene. PanelSettings and generated assets remain available.

## Your own factory

Place a subclass of `LumaFlow.Editor.UxmlPreviewDefinition` in an Editor assembly.
Add a `CreateAssetMenu` attribute and implement `public override Widget
CreateWidget()`. Use serialized fields for preview data. Existing stateless
widgets can be returned directly. Factories must be deterministic and safe to
run outside Play Mode: avoid scene mutations, network calls and runtime services.
Factory and widget initialization code executes during generation.

## Current boundary

The prototype supports native VisualElement, Label, Button and saved Image
assets, covering basic Container, Row, Column, SizedBox, padding and positioning
layouts. It serializes the initial state and supported inline styles. It does
not serialize callbacks, reactive subscriptions, hover transitions or game logic.
Unsupported native element/style types, Native wrappers, LayoutBuilder and
animation nodes fail explicitly. Controls with generated internal hierarchies,
such as sliders, switches and scrolling, need dedicated adapters in a later pass.
Inherited panel styling depends on the receiving panel's theme.

The generated USS preserves relative lengths. LayoutBuilder/responsive logic
requiring an attached panel is not evaluated by this detached snapshot prototype.
Arbitrary C# cannot be serialized to UXML. Runtime-loaded textures without saved
asset references cannot be exported either.

## Validation

`UxmlPreviewTests` covers escaping, culture-independent numbers, deterministic
output, callback isolation and rejected Native cleanup. The standalone
`Tools/LumaFlow/Validation/UxmlPreviewSmoke.cs.txt` harness imports the generated
assets and compares native hierarchy, text, geometry and colors on a real Unity
Editor panel. It can run in an isolated project with `-noUpm`; that bypasses local
package-manager problems, but does not validate UPM installation or hosted CI.

On Windows, run the reproducible harness from the development root:

```powershell
./Tools/LumaFlow/Validation/Invoke-UxmlPreviewSmoke.ps1 -UnityPath 'C:/Program Files/Unity/Hub/Editor/6000.4.5f1/Editor/Unity.exe' -OutputDirectory 'E:/LumaFlow/Artifacts/new-preview-smoke'
```

It requires a .NET SDK and the development project's cached NUnit assembly,
compiles the current runtime and preview sources, and writes results into a new
isolated project. Existing output directories are never overwritten. Four
regression methods and the imported layout comparison passed on Unity 6000.4.5f1.
Game View presentation and automatic reload in the interactive editor remain
manual checks for this prototype.
