# Getting started with LumaFlow

LumaFlow mounts a declarative widget tree into a caller-owned UI Toolkit
`VisualElement`. The caller also owns the returned `MountHandle` and must
dispose it when the host is disabled, destroyed, or rebuilt.

## Mount a tree

```csharp
using LumaFlow;
using UnityEngine;
using UnityEngine.UIElements;
using Framework = LumaFlow.LumaFlow;

[RequireComponent(typeof(UIDocument))]
public sealed class ExampleScreen : MonoBehaviour {
    private MountHandle _mount;

    private void OnEnable() {
        var root = GetComponent<UIDocument>().rootVisualElement;
        _mount = Framework.Mount(
            new Column(
                gap: 12f,
                children: new Widget[] {
                    new Text("Hello, LumaFlow"),
                    new Button("Continue", Continue),
                }),
            root);
    }

    private void OnDisable() {
        _mount?.Dispose();
        _mount = null;
    }

    private void Continue() {
    }
}
```

`Mount` creates one LumaFlow-owned host below the supplied root. Disposing the
handle removes that host and releases framework subscriptions; it does not
clear unrelated native children owned by the application.

## Rebuild or restart a PlayMode preview

Call `mount.Rebuild()` to re-run declarative builder boundaries while retaining
compatible mounted `WidgetState`. Call `mount.Restart(newRoot)` when the preview
must start from fresh mount-local state. Restart mounts the replacement before
releasing the previous root, so a replacement mount failure leaves the current
screen active.

These are explicit application lifecycle operations, not a C# compiler or VM
patcher. See [PlayMode Preview Lifecycle](PlayMode%20Preview.md) for Unity's
script-recompilation boundary and a header-button integration pattern.

## Keep mutable values in State

Widgets are immutable descriptions. Put mutable data in `State<T>` and rebuild
only the branch that reads it.

```csharp
var count = new State<int>(0);

Widget Counter() => new Column(
    gap: 8f,
    children: new Widget[] {
        new ReactiveBuilder<int>(count, value => new Text($"Count: {value}")),
        new Button("Increment", () => count.Value++),
    });
```

Use `Widget.WithKey(new WidgetKey(id))` when sibling identity must survive a
reorder. Compatible widgets reconcile in place; a different widget type or an
explicitly incompatible configuration remounts.

## Compose responsive layout

`LayoutBuilder` observes resolved width by default. Put `Expanded` or
`Flexible` directly below a `Row` or `Column`.

```csharp
Widget ResponsiveCards(Widget first, Widget second) =>
    new LayoutBuilder((context, constraints) =>
        constraints.MaxWidth < 640f
            ? new Column(new[] { first, second }, gap: 12f)
            : new Row(
                new Widget[] {
                    new Expanded(first),
                    new Expanded(second),
                },
                gap: 12f));
```

Read [Layout Contract](Layout%20Contract.md) before mixing flex, explicit
constraints, scrolling, and height-driven builders.

## Native interop

Use `Native` for a detached UI Toolkit element that LumaFlow does not yet wrap.
The element must not already have a parent. Prefer a factory when every mount
needs its own native instance.

```csharp
new Native(() => new Label("Native UI Toolkit content"));
```

Keep native access at integration boundaries. Application widget trees should
remain declarative so reconciliation, context, lifecycle, and tests still apply.

## Import the sample

In Package Manager, select LumaFlow, open **Samples**, and import
**Getting Started**. Add `LumaFlowCounterSample` to the same GameObject as its
`UIDocument`; no field assignment is required. The sample demonstrates idempotent mounting, reactive state,
declarative composition, and deterministic `MountHandle` disposal.

## Inspect a mounted tree

Open **Window > LumaFlow > Widget Inspector** and press **Refresh**. The tool
captures immutable snapshots of active mounts, including widget/state/native
identity, resolved layout, inherited environment, and actionable key or native
ownership findings. It does not continuously poll the Editor or expose internal
`WidgetNode`/`VisualElement` ownership to application code.

For programmatic issue reports, call `mount.CaptureDiagnostics()` or
`LumaFlowDiagnostics.CaptureActiveTrees()`. See
[Diagnostics and Widget Inspector](Diagnostics.md) for the snapshot contract and
finding codes.
