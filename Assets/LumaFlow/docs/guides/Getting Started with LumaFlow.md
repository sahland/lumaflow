# Getting Started with LumaFlow

LumaFlow is mounted into a UI Toolkit `VisualElement`; it does not own an
application window, scene, or panel. The caller owns the native root and the
returned `MountHandle`.

## 1. Add the package

Add `com.sahland.lumaflow` through your Unity project's package workflow, then
reference the `LumaFlow` namespace from a runtime or Editor assembly as needed.

```csharp
using LumaFlow;
using Framework = LumaFlow.LumaFlow;
```

## 2. Mount a widget tree

This minimal EditorWindow is intentionally small. It proves the ownership
contract: mount in `CreateGUI`, dispose the previous tree before remounting,
and dispose when the window goes away.

```csharp
using LumaFlow;
using UnityEditor;
using Framework = LumaFlow.LumaFlow;

public sealed class HelloLumaFlowWindow : EditorWindow
{
    private MountHandle _mount;

    [MenuItem("Window/Examples/Hello LumaFlow")]
    public static void Open() => GetWindow<HelloLumaFlowWindow>();

    private void CreateGUI()
    {
        _mount?.Dispose();
        rootVisualElement.Clear();

        _mount = Framework.Mount(
            new Column(
                gap: 12f,
                children: new Widget[]
                {
                    new Text("Hello, LumaFlow"),
                    new Button("Close", Close)
                }),
            rootVisualElement);
    }

    private void OnDisable() => _mount?.Dispose();
}
```

`Mount` adds one LumaFlow-owned host below `rootVisualElement`. `Dispose` only
removes that host and cleans subscriptions owned by its WidgetNodes; it does
not clear unrelated native elements.

## Layout contract

`Row` and `Column` are native Flexbox parents. `Expanded` and `Flexible` must
be their immediate children: `Expanded` and `Flexible(fit: FlexFit.Tight)` fill
their flex allocation; `Flexible(fit: FlexFit.Loose)` receives an allocation
but lets its child keep an intrinsic size. Use `SizedBox` for a tight explicit
width and/or height, and `ConstrainedBox` for optional min/max bounds.

`Align` and `Center` position one child inside their native wrapper. `Stack`
creates a relative positioning context; use `Positioned` for explicit offsets.
`ScrollView` owns the overflow boundary and has a single child. In vertical
scroll content, use `Expanded` only below an explicitly height-bounded parent.

`LayoutBuilder` rebuilds only when its observed axis changes. The default
horizontal mode is intended for responsive width breakpoints; vertical mode
requires a parent that explicitly bounds height.

## 3. Add a theme before using semantic components

Components such as `Button`, `TextField`, icons, `Card`, and application-shell
widgets resolve their semantic values from the nearest `Theme`.

```csharp
_mount = Framework.Mount(
    new Theme(
        AppTheme.Data,
        new Column(
            gap: AppTheme.Data.Spacing.Medium,
            children: new Widget[]
            {
                new Text("Settings", AppTheme.Data.Typography.Title),
                new Button("Save", Save, variant: ButtonVariant.Primary)
            })),
    rootVisualElement);
```

`ThemeData` owns colors, typography, spacing, radii, button styles, icon
defaults, and text-field defaults. See the Component Gallery's
`LumaFlowGalleryTheme.cs` for a complete concrete theme.

## 4. Keep state outside widgets

Widget objects are descriptions, not mutable mounted controls. Own values in
`State<T>` and rebuild only the local visual branch that depends on them.

```csharp
private readonly State<int> _count = new(0);

private Widget BuildCounter() => new Column(
    gap: 8f,
    children: new Widget[]
    {
        new ReactiveBuilder<int>(_count, value => new Text($"Count: {value}")),
        new Button("Increment", () => _count.Value++)
    });
```

`ReactiveBuilder<T>` rebuilds its own local description when the State changes.
Compatible descendants that implement the ADR-021 update contract are retained;
incompatible or not-yet-migrated nodes remount during the current alpha rollout.

For state that belongs to one component rather than the application, use
`StatefulWidget<TState>`. Its `WidgetState` receives `InitState`, may call
`SetState` to rebuild only its local subtree, and receives `Dispose` at unmount.
That state remains local to one mounted widget. Ordinary `Row`, `Column`, and
`Stack` children match by position unless a stable key is supplied:

```csharp
new Row(new Widget[]
{
    ProjectCard(first).WithKey(new WidgetKey(first.Id)),
    ProjectCard(second).WithKey(new WidgetKey(second.Id))
});
```

Use `KeyedColumn` or `KeyedRow` when an externally owned
`State<IReadOnlyList<KeyedChild>>` should directly control the collection.

## 5. Use native UI Toolkit when it is the right primitive

LumaFlow is interoperable by design. Embed a detached native element directly,
or provide a factory when each mount needs a new element.

```csharp
var nativeStatus = new UnityEngine.UIElements.Label("Connected");

new Column(new Widget[]
{
    new Text("Service status"),
    new Native(nativeStatus)
});
```

Do not give `Native` an element that is already attached to another parent;
LumaFlow rejects silent reparenting.

## Next guides

- [State, Forms, and Async](State,%20Forms,%20and%20Async.md)
- [Implementation Status](../architecture/LumaFlow%20Implementation%20Status.md)
- [Threading and Async Policy](../development/LumaFlow%20Threading%20and%20Async%20Policy.md)
