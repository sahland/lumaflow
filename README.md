<p align="center">
  <img src="Documentation~/Images/lumaflow.png" alt="LumaFlow" width="144" />
</p>

<h1 align="center">LumaFlow</h1>

<p align="center">
  Declarative, reactive C# UI for Unity UI Toolkit.
</p>

<p align="center">
  <a href="https://sahland.github.io/lumaflow-web/">Website</a>
  · <a href="Documentation~/index.md">Documentation</a>
  · <a href="CHANGELOG.md">Changelog</a>
  · <a href="LICENSE">MIT License</a>
</p>

LumaFlow is a Unity Package Manager package for building retained UI Toolkit
interfaces with immutable widget descriptions and explicit reactive state. It
keeps Unity's native layout, rendering, input and accessibility systems in
place, while providing a cohesive layer for composition, state and application
UI.

## What it includes

- Flex and constraint layouts: `Row`, `Column`, `Stack`, `ScrollView`,
  `LayoutBuilder`, `Scaffold` and adaptive scaffolds.
- Reactive state with `State<T>`, `ReactiveBuilder<T>` and form fields.
- Controls for buttons, text input, toggles, sliders, dropdowns, tabs,
  segmented controls, progress indicators and virtualized lists.
- Tree-scoped themes, localization, media settings and accessibility semantics.
- Retained navigation plus dialogs, drawers, popovers, tooltips and toasts.
- Implicit animations, typed icons and a Widget Inspector for mounted trees.

## Requirements

- Unity 6 or newer
- Unity UI Toolkit

The runtime assembly has no dependency on `UnityEditor` or a render pipeline.

## Install

In Unity, open **Window → Package Manager**, choose **Add package from git
URL…**, then enter:

```text
https://github.com/sahland/lumaflow.git
```

You can also install the package from a local `.tgz` archive or a scoped
registry. After installation, import **Getting Started** from the package's
Samples tab.

## First screen

Mount a widget tree into a `UIDocument` that your application owns and keep the
returned handle for the same lifetime:

```csharp
using LumaFlow;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class ApplicationRoot : MonoBehaviour {
    [SerializeField] private UIDocument document = null!;

    private readonly State<int> _count = new(0);
    private MountHandle? _mount;

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

The imported sample is a complete, small application. Add
`LumaFlowCounterSample` to the same GameObject as its `UIDocument`; the sample
resolves that document automatically.

## How it works

```text
Widget configuration  →  mounted WidgetNode  →  Unity VisualElement
```

Widgets are immutable descriptions. Application state belongs to explicit
owners such as `State<T>`, form fields and controllers. When a reactive value
changes, LumaFlow rebuilds the branch that read it and updates compatible native
UI Toolkit elements in place.

Use a stable `WidgetKey` where sibling widgets can move, be inserted or be
removed while carrying local state.

## Documentation

Start with [Getting Started](Documentation~/Getting%20Started.md), then refer
to the [API Reference](Documentation~/API%20Reference.md) and the
[Layout Contract](Documentation~/Layout%20Contract.md). The
[documentation index](Documentation~/index.md) contains the remaining guides.

The package also includes the **Widget Inspector** under
**Window → LumaFlow → Widget Inspector**. It shows active mounts, widget and
state types, native UI Toolkit identity, layout values and lifecycle findings.

## Package layout

```text
Runtime/              Framework runtime
Editor/               Widget Inspector and Play Mode tools
Samples~/             Importable examples
Documentation~/       Guides and API reference
```

## Versioning

LumaFlow is currently in the `0.x` preview phase. Public runtime changes are
tracked in the [changelog](CHANGELOG.md) and compatibility notes.

## License

The repository source is released under the [MIT License](LICENSE). LumaFlow
includes a compact built-in icon catalog, and applications can supply custom
icons through `IconData`.
