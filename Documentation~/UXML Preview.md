# Experimental C# to UXML preview

LumaFlow can export an initial visual snapshot as UXML and USS without entering
Play Mode. The exporter mounts a temporary tree through the existing runtime
nodes, serializes its native elements and inline styles, then disposes the mount.
Layout and styling therefore use the same mappers as ordinary LumaFlow code.
This is an experimental snapshot exporter, not a replacement runtime backend.

## Automatic Game View workflow

### Automatic startup and self-hosted authoring

**Tools > LumaFlow > UXML** opens a LumaFlow-authored window. Its controls,
layout, theme, source selector and reactive status use the same runtime widgets
as applications. Only the native UXML canvas and Unity's serialized-object
inspector use UI Toolkit/IMGUI interop.

The window opens automatically once per interactive Editor project session,
including the first package import. It waits for compilation/import and does
not reopen after every assembly reload or in batch mode. **Show on project
startup** is a per-user, per-project EditorPrefs setting; uncheck it to opt out.
Reopen the tool from the menu to change the preference later.

Saved definitions and `[LumaPreview]` methods are discovered automatically. The
last selected source is restored; otherwise an available project source is
selected. A built-in **LumaFlow / Welcome** preview makes a clean installation
usable without creating an asset. Factory data remains opt-in: the tool does
not execute arbitrary MonoBehaviour lifecycle code to guess the app's root.

The initial viewport is configured from the factory and fitted to the window.
Presets, editable width/height, **Apply size**, **Fit preview**, and zoom remain
available. Startup does not create a new application scene or save it. When a
unique existing AppBootstrap and UIDocument expose a supported factory, the
window adopts that application's source and connects its edit-mode snapshot.

### One-button scene setup

Press **Автонастройка сцены** in **Tools > LumaFlow > UXML**, outside Play Mode:

1. Existing AppBootstrap, UIDocument and Camera components in the active scene
   are reused. Missing objects/components are created, and the bootstrap's empty
   serialized UIDocument reference is filled. An existing reference is preserved.
2. Existing PanelSettings are preserved. Otherwise the generated default panel
   and theme under `Assets/LumaFlowGenerated/EditorTools/` are assigned.
3. With no AppBootstrap script in the project, a small editable starter is
   generated at `Assets/LumaFlowApp/AppBootstrap.cs`. It contains AppBootstrap
   and AppRoot. After compilation, scene setup continues automatically. These
   are application files, not new application classes inside the framework.
4. Keep **Game** open without Play Mode. Edit AppRoot, return to Unity, and wait
   for C# compilation: the snapshot is regenerated and the scene document is
   refreshed. No application lifecycle or networking is run in Edit Mode.
5. Save the scene to retain the connection. Object/component configuration is
   Undoable; generated scripts, PanelSettings and output assets remain on disk.

An existing bootstrap needs a pure parameterless static factory returning Widget
(or a subclass): `CreateApplication()`, or a method marked `[LumaPreview]`.
The setup registers that factory for automatic generation; the starter does not
require an Editor-assembly reference. Do not put scene searches, networking or
other side effects in the factory. Runtime callbacks are available in Play Mode;
the edit-mode UXML is still a visual snapshot.

The `UxmlPreviewBinding` component stores the source, bootstrap and original
document asset. Before Play Mode and in Player startup it restores the original
asset, preventing the editor snapshot from duplicating the runtime application.
On leaving Play Mode, reopening the scene or recompiling, the edit snapshot is
restored. No per-frame preview polling is installed.

If several bootstraps/documents exist, select the intended object in Hierarchy;
the tool will not guess and overwrite an unrelated UI. Setup is disabled in
Prefab Mode and Play Mode. A newly created Camera uses the normal MainCamera
tag and default settings; existing cameras are not reconfigured.

To validate clean-project creation, compilation and runtime/edit transitions,
run `Invoke-EditorToolsSmoke.ps1` with `-Mode QuickStart` and a new output directory.

For an edit-mode Game View preview, select any scene GameObject and press
**Bind selected UIDocument**. A missing UIDocument and Panel Settings are
configured automatically. Existing Panel Settings are preserved. This is an
explicit, Undoable action outside Play Mode. Default assets are reused under
`Assets/LumaFlowGenerated/EditorTools/`; Undo reverses the document changes,
not the reusable generated assets.

### Existing-document Inspector alternative

1. Create an asset using **Assets > Create > LumaFlow > UXML Preview Demo**.
2. Add a `UIDocument` to a scene GameObject and assign its Panel Settings.
3. Select the `UIDocument` component in the Inspector. In its **LumaFlow
   Preview** row, choose an asset or `[LumaPreview]` code factory. LumaFlow
   generates and binds the UXML immediately. Use **Regenerate** there whenever
   you want a manual refresh.
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

The Inspector control supports multi-object selection and uses Undo. It only
assigns `UIDocument.visualTreeAsset`; every selected document must already have
Panel Settings. You can also assign the generated `Preview.uxml` directly to a
UIDocument's Source Asset. The result is the same.

## Authoring controls

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
The window's **Bind selected UIDocument** button additionally configures missing
components/settings as described above. The Inspector alternative still requires
existing Panel Settings. Neither action saves a scene automatically. Generated
assets remain stable across regeneration.

## Your own factory

For a preview with no asset setup, place a static factory in an Editor assembly:

```csharp
using LumaFlow;
using LumaFlow.Editor;

internal static class DashboardPreviews {
    [LumaPreview(
        "Dashboard / Default",
        Width = 390,
        Height = 844,
        Locale = "ru-RU",
        TextScale = 1.15f)]
    private static Widget Default() => new DashboardScreen();

    [LumaPreview("Dashboard / Loading", Width = 390, Height = 844)]
    private static Widget Loading() => new DashboardScreen(isLoading: true);
}
```

The method must be static, parameterless, non-generic and return `Widget`.
It appears automatically in the window's **Code preview** list. Select it and
bind a `UIDocument` once. After later C# compilations, LumaFlow discovers the
method again and regenerates the same UXML path, so the edit-mode Game View
keeps its reference. The method and its containing class may be non-public.
Keep preview methods in an Editor assembly because `LumaPreviewAttribute` is an
editor-only API and is not included in player builds.

Use `Screen / Scenario` names to keep related states together in the selector.
`Width` and `Height` must either both be positive or both be omitted. Omitted
dimensions follow the main Game View. `Locale` accepts values such as `en`,
`ru-RU` and `zh-Hans-CN`; `TextScale` defaults to `1`. These settings wrap only
the preview subtree with the existing `MediaQuery`, `Localizations` and
`TextScale` widgets. A preview that needs strongly typed localization resources
should still create its normal inner `Localizations` widget. Likewise, select a
theme by returning the screen below the ordinary LumaFlow `Theme` widget—the
preview system does not introduce a second theme format.

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
Asset-backed factories expose the same preview width, height, locale and text
scale as serialized Inspector fields. Zero width and height follow Game View.

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
asset references cannot be exported either. This currently includes procedural
`LinearGradient` backgrounds; they remain available in the normal runtime mount.

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
isolated project. Existing output directories are never overwritten. Twenty-one
regression cases and the imported layout comparison passed on Unity 6000.4.5f1.
Game View presentation after assembly reload remains an interactive Editor check;
the generated asset identity and regeneration path are automated tests.

For the self-hosted windows, source discovery, startup preference, viewport,
comparison, restoration and automatic Play Mode capture lifecycle, use
`Tools/LumaFlow/Validation/Invoke-EditorToolsSmoke.ps1`. See [GUI Performance](GUI%20Performance.md)
for the command and measurement limitations.
