# Experimental C# to UXML preview

LumaFlow can export an initial visual snapshot as UXML and USS without entering
Play Mode. The exporter mounts a temporary tree through the existing runtime
nodes, serializes its native elements and inline styles, then disposes the mount.
Layout and styling therefore use the same mappers as ordinary LumaFlow code.
This is an experimental snapshot exporter, not a replacement runtime backend.

## Automatic Game View workflow

1. Create an asset using **Assets > Create > LumaFlow > UXML Preview Demo**.
2. Add a `UIDocument` to a scene GameObject and assign its Panel Settings.
3. Open **Tools > LumaFlow > UXML Preview (Experimental)**, select the factory,
   select the `UIDocument` GameObject in Hierarchy, then click
   **Bind selected UIDocument** once.
4. Close the preview window if you want. Keep Game View open outside Play Mode.
5. Edit the C# widget factory. After Unity recompiles the assembly, LumaFlow
   regenerates the same UXML/USS assets. The `UIDocument` keeps its stable asset
   reference, so UI Toolkit refreshes the Game View without entering Play Mode.

Changing serialized preview data also regenerates automatically. The factory
asset has an **Auto Generate** checkbox. Disable it to opt out, then use
**Tools > LumaFlow > Regenerate All UXML Previews** when a manual refresh is
needed. The global generator does not require the preview window to be open.
Editing one preview asset queues only that asset; a domain reload or the manual
menu command regenerates all enabled asset factories and discovered code
factories. Repeated imports are coalesced into one delayed generation pass.

Generation is transactional from the author's point of view: LumaFlow finishes
building and exporting the new snapshot before replacing either generated file.
If a factory throws or contains an unsupported widget, the bound `UIDocument`
continues showing its last successful UXML. An error notification appears in an
open Game View with the failing factory and concise reason; a later successful
generation clears that factory's notification. Full exception details remain in
the Console.

You can skip the binding button and assign the generated `Preview.uxml` directly
to a UIDocument's Source Asset. The result is the same.

## Optional authoring window

Edit preview data directly in the sidebar. **Auto refresh** regenerates after
data edits; **Generate** remains available for manual refresh. **Edit factory C#**
opens the selected factory's source.

Generated files live under `Assets/LumaFlowGenerated/<factory-asset-guid>/`.
The window opens in single-preview mode. Enable **Compare runtime** to display
both backends. Phone/tablet/desktop presets and the width/height fields set the
preview canvas; the 25–100% scale changes only its display size. Scrollbars
remain available for larger canvases. Responsive C# branches are generated
against the main Game View resolution (1280×720 in headless validation), so the
UXML bound to a `UIDocument` matches the edit-mode Game View rather than the
size of the optional authoring window.
**Open UXML**, **Open USS** and **Show generated files** provide direct access to
the output. **New demo** creates and selects a factory without leaving the window.
Unchanged files are not rewritten. Edit the factory rather than these files.
Binding uses Undo and only assigns `UIDocument.visualTreeAsset`; it does not
create or save scene objects. The selected document must already have Panel
Settings. Generated assets remain stable across regeneration.

## Your own factory

For a preview with no asset setup, place a static factory in an Editor assembly:

```csharp
using LumaFlow;
using LumaFlow.Editor;

internal static class DashboardPreviews {
    [LumaPreview("Dashboard / Default")]
    private static Widget Default() => new DashboardScreen();
}
```

The method must be static, parameterless, non-generic and return `Widget`.
It appears automatically in the window's **Code preview** list. Select it and
bind a `UIDocument` once. After later C# compilations, LumaFlow discovers the
method again and regenerates the same UXML path, so the edit-mode Game View
keeps its reference. The method and its containing class may be non-public.
Keep preview methods in an Editor assembly because `LumaPreviewAttribute` is an
editor-only API and is not included in player builds.

Use a code preview for fixed states that are naturally expressed in C#. Use a
`UxmlPreviewDefinition` asset when designers need serialized controls in the
Inspector or several editable data variants.

### Asset-backed factory

Place a subclass of `LumaFlow.Editor.UxmlPreviewDefinition` in an Editor assembly.
Add a `CreateAssetMenu` attribute and implement `public override Widget
CreateWidget()`. Use serialized fields for preview data. Existing stateless
widgets can be returned directly. Factories must be deterministic and safe to
run outside Play Mode: avoid scene mutations, network calls and runtime services.
Factory and widget initialization code executes during generation.

## Current boundary

The prototype supports native VisualElement, Label, Button, saved Image assets,
Slider, Checkbox/Toggle, Switch, TextField, Dropdown and ScrollView. This covers
basic Container, Row, Column, SizedBox, padding, positioning and initial
`LayoutBuilder` branches. Generated controls preserve their initial values,
labels, ranges, choices, placeholder configuration, scroll direction and
LumaFlow styling. Their internal UI Toolkit parts are styled through scoped USS
selectors instead of being duplicated into the UXML hierarchy.

The snapshot does not serialize callbacks, reactive subscriptions, hover
transitions or game logic. Editing a generated control does not write back to
LumaFlow state. Unsupported native element/style types, arbitrary Native
wrappers and animation nodes fail explicitly. Dropdown display labels cannot
contain commas because UI Toolkit's UXML list attribute uses commas as separators.
Inherited panel styling depends on the receiving panel's theme.

The generated USS preserves relative lengths. `LayoutBuilder` receives the
current Game View dimensions during generation. Its selected branch is a static
snapshot and regenerates after compilation; it cannot switch branches inside
the generated UXML without another generation pass.
Arbitrary C# cannot be serialized to UXML. Runtime-loaded textures without saved
asset references cannot be exported either.

## Validation

`UxmlPreviewTests` covers escaping, culture-independent numbers, deterministic
output, stable generated asset identity, authoring controls, callback isolation
and rejected Native cleanup. The standalone
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
Game View presentation after assembly reload remains an interactive Editor check;
the generated asset identity and regeneration path are automated tests.
