<p align="center">
  <img src="Documentation~/Images/lumaflow.png" alt="LumaFlow" width="160" />
</p>

# LumaFlow

LumaFlow is a declarative C# UI framework built on Unity UI Toolkit. It provides
immutable widget descriptions, retained native elements, reactive state,
component themes, navigation, overlays, animation, accessibility semantics and
virtualized lists.

## Requirements

- Unity 6 or newer
- Unity UI Toolkit

The Runtime assembly does not reference `UnityEditor` or a render-pipeline
package.

## Installation

Install the package from a local archive, a Git URL or a scoped registry through
Unity Package Manager. Import **Getting Started** from the Samples tab to add a
minimal counter application to the project.

## Quick start

Mount a widget tree into the root of a `UIDocument` and retain the returned
handle for the same lifetime as the document:

```csharp
using LumaFlow;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class ApplicationRoot : MonoBehaviour {
    [SerializeField] private UIDocument document = null!;

    private MountHandle? _mount;
    private readonly State<int> _count = new(0);

    private void OnEnable() {
        _mount = LumaFlow.LumaFlow.Mount(
            new Column(
                new Widget[] {
                    new ReactiveBuilder<int>(
                        _count,
                        value => new Text($"Count: {value}")),
                    new Button("Increment", () => _count.Value++)
                },
                gap: 12f),
            document.rootVisualElement);
    }

    private void OnDisable() {
        _mount?.Dispose();
        _mount = null;
    }
}
```

## Packages and assemblies

- `LumaFlow.Runtime` contains the runtime framework.
- `LumaFlow.Editor` contains the Widget Inspector and Play Mode preview toolbar.
- `Samples~/Getting Started` contains an importable example.

## Framework model

LumaFlow maps declarative widgets onto retained UI Toolkit elements:

```text
Widget configuration -> mounted WidgetNode -> VisualElement
```

Widgets are immutable descriptions. Mutable values belong to `State<T>`, form
fields, controllers or application services. Compatible widget updates retain
their mounted node and local state; incompatible widget types or keys establish
a new identity.

`Theme`, `MediaQuery`, `Localizations` and `TextScale` are inherited scopes.
Only descendants that read a changed scope are rebuilt.

## Layout

The layout API follows UI Toolkit flex layout and adds typed composition:

- `Row`, `Column`, `Expanded`, `Flexible` and `Spacer`;
- `SizedBox`, `ConstrainedBox`, `Padding`, `Margin`, `Align` and `Center`;
- `Stack` and `Positioned`;
- `ScrollView`, `LayoutBuilder`, `Scaffold` and `AdaptiveScaffold`.

`Expanded` and `Flexible` must be immediate children of a flex parent. Use
stable `WidgetKey` values for collections that can reorder, insert or remove
stateful children.

## Controls and styling

Built-in controls include buttons, text fields, checkbox, radio, switch,
slider, dropdown, tabs, segmented controls, progress indicators and
virtualized lists. Controlled inputs commit their value to external state
before invoking the user callback.

`ThemeData` supplies typography, colors and component defaults. Explicit style
fields override the corresponding theme fields. `WidgetStateProperty<T>` can
resolve values from combined states such as hovered, focused, pressed,
selected, error and disabled.

```csharp
var style = new ButtonStyle(
    backgroundColor: WidgetStateProperty<Color?>.ResolveWith(states =>
        (states & WidgetStates.Disabled) != 0
            ? disabledSurface
            : primarySurface));
```

## Navigation and overlays

`NavigatorHost` owns a retained route stack. Use keyed `Route` values when a
stack must be inspected or restored in memory. `OverlayHost` and
`OverlayController` provide dialogs, drawers, popovers, tooltips and toasts.
Keep controllers outside `Build` so their lifetime matches the mounted host.

## Animation

`TweenAnimationBuilder<T>` and `AnimatedOpacity` provide implicit animations.
Compatible updates retarget a running animation from its displayed value.
`MediaQueryData.DisableAnimations` supplies the reduced-motion policy for a
subtree.

## Diagnostics

`MountHandle.CaptureDiagnostics()` captures an immutable description of a
mounted tree. Open **Window > LumaFlow > Widget Inspector** to inspect active
mounts, keys, state types, native elements, layout values and lifecycle
findings.

The Play Mode toolbar exposes:

- **Reassemble**, which rebuilds the current widget tree while preserving
  compatible mount-local state;
- **Compile & Restart**, which exits Play Mode, waits for compilation and
  starts Play Mode again with fresh application state.

C# recompilation does not provide Dart VM-style method patching. These commands
operate within Unity's supported compilation and Play Mode lifecycle.

## Icons and media

LumaFlow includes a compact built-in icon catalog for common interface actions:

```csharp
new Icon(LumaIcons.Settings);
new IconButton(LumaIcons.Close, Close, tooltip: "Close dialog");
```

The LumaFlow brand mark is available to application layouts through
`LumaIcons.LumaFlow`:

```csharp
new Icon(LumaIcons.LumaFlow, size: 32f);
```

Applications may also provide their own `IconData`, textures, sprites and vector
images.

## Documentation

Start with [Getting Started](Documentation~/Getting%20Started.md), then use the
[API Reference](Documentation~/API%20Reference.md) and
[Layout Contract](Documentation~/Layout%20Contract.md). The
[documentation index](Documentation~/index.md) links the remaining guides.

## Versioning

Version `0.x` is a preview API. Public Runtime changes are checked against the
reviewed API baseline and documented in the changelog and migration guide.
