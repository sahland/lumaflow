# API reference

This page summarizes the supported preview API. Constructor edge cases are
documented on the corresponding public members.

## Runtime and state

- `LumaFlow.Mount`, `MountHandle`, `Widget`, `WidgetKey`, `KeyedSubtree`
- `StatelessWidget`, `StatefulWidget<TState>`, `WidgetState`
- `State<T>`, `ReactiveBuilder<T>`
- `BuildContext`, `Theme`, `MediaQuery`, `Localizations`, `TextScale`

`MountHandle.Rebuild()` re-evaluates declarative builders while retaining
compatible mounted state. `MountHandle.Restart(widget)` transactionally mounts
a fresh root and resets mount-local state; a failed replacement leaves the
previous tree active. See [PlayMode Preview Lifecycle](PlayMode%20Preview.md).

## Layout

- Flex: `Row`, `Column`, `Expanded`, `Flexible`, `Spacer`
- Constraints: `SizedBox`, `FractionallySizedBox`, `ConstrainedBox`, `LayoutBuilder`
- Positioning: `Align`, `Center`, `Stack`, `Positioned`
- Decoration: `Padding`, `Container`, `Card`, `Opacity`
- Scrolling: `ScrollView`, `ListView<T>`, `ListViewController`
- Application shell: `Scaffold`, `AppBar`, `AdaptiveScaffold`,
  `NavigationRail`, `NavigationBar`

See [Layout Contract](Layout%20Contract.md) and
[Lists and Virtualization](Lists%20and%20Virtualization.md).

Common sizing compositions have named factories: `SizedBox.Square`, `.Expand`,
`.ExpandWidth`, `.ExpandHeight`, `.Shrink`, plus `ConstrainedBox.Tight`,
`.AtMost`, and `.AtLeast`. `Align` exposes named factories for every non-center
alignment; use `Center` for the centered case.

## Styling and themes

- `ThemeData`, `ColorScheme`, `TypographyTheme`, `SpacingTheme`, `RadiusTheme`
- `TextStyle`, `TextOverflow`, `BoxDecoration`, `Border`, `BorderSide`, `BorderRadius`,
  `EdgeInsets`, `Alignment`
- `WidgetStates`, `WidgetStateProperty<T>`
- `ButtonStyle`, `ButtonTheme`, `IconThemeData`, `TextFieldStyle`,
  `TextFieldTheme`, `CheckboxStyle`, `RadioStyle`, `SwitchStyle`, `SliderStyle`,
  `CheckboxTheme`, `RadioTheme`, `SwitchTheme`, `SliderTheme`,
  `DropdownStyle`, `DropdownTheme`, `LinearProgressIndicatorStyle`,
  `ProgressIndicatorTheme`

## Controls and forms

- `Text`, `Icon`, `IconButton`, `Button`, `Pressable`, `AsyncButton`
- Application media: `Image`, `ImageFit`, `CircleAvatar`
- `TextField`, `Checkbox`, `Radio<T>`, `Switch`, `Slider`, `Dropdown<T>`,
  `LinearProgressIndicator`, `TabBar<T>`, `TabItem<T>`, `TabView<T>`,
  `SegmentedControl<T>`, `SegmentedControlItem<T>`
- `Form`, `FormState`, `FormField<T>`, `FocusNode`, `FocusTraversalGroup`
- `AsyncAction`

`Toggle` is a deprecated migration alias and is not part of the intended stable
surface.

### Composable interaction and text

`Button` accepts either a string label or an arbitrary widget tree. Its resolved
foreground and typography become defaults for nested `Text` and `Icon` widgets;
an explicit child style remains stronger. `Pressable` adds native pointer,
keyboard, focus, disabled, and button-semantics behavior without painting a
surface:

```csharp
new Button(
    new Row(new Widget[] { new Icon(LumaIcons.Plus), new Text("New project") }, gap: 8f),
    CreateProject,
    style: new ButtonStyle(
        border: Border.All(theme.Colors.Outline),
        borderByState: WidgetStateProperty<Border?>.ResolveWith(states =>
            (states & WidgetStates.Focused) != 0
                ? Border.All(theme.Colors.Primary, 2f)
                : null)),
    semanticsLabel: "Create project");

new Pressable(
    new Container(projectRow, new BoxDecoration(
        backgroundColor: theme.Colors.Surface,
        borderRadius: theme.Radius.Small,
        border: Border.All(theme.Colors.Outline))),
    OpenProject,
    semanticsLabel: "Open project");
```

`Text` supports `softWrap`, `maxLines`, and `TextOverflow.Clip`, `.Ellipsis`, or
`.Visible`. In Unity UI Toolkit, ellipsis is native for no-wrap text; bounded
multi-line text uses a scaled line cap and clipping because UI Toolkit does not
expose a public multi-line ellipsis or line-height primitive.

`Container` and `Card` share `BoxDecoration` for solid backgrounds, cached
two-color `LinearGradient` backgrounds, radius and typed per-side borders.
Use `ClipBehavior.HardEdge` when a `Container` must clip descendants to its
bounds and rounded corners:

```csharp
new Container(
    content,
    ClipBehavior.HardEdge,
    new BoxDecoration(
        new LinearGradient(
            new Color(1f, 1f, 1f, 0.92f),
            new Color(1f, 1f, 1f, 0.55f),
            angle: 90f),
        borderRadius: BorderRadius.All(24f)));
```

Equal gradients reuse one small immutable texture instead of allocating during
widget rebuilds. Unity 6.0 UI Toolkit does not expose a public box-shadow style,
so `BoxShadow` and elevation are not part of the current API.

`LinearProgressIndicator` accepts either a value or `State<float>` in `0..1`.
Track color, value color, height, and radius resolve from its explicit
`LinearProgressIndicatorStyle`, then `ThemeData.ProgressIndicatorTheme`:

```csharp
new LinearProgressIndicator(
    uploadProgress,
    new LinearProgressIndicatorStyle(minHeight: 3f),
    semanticsLabel: "Upload progress");
```

`ListTile` retains its string convenience constructor and also accepts widget
slots for `title`, `subtitle`, `leading`, and `trailing`. Compatible slot
updates preserve nested widget state, including when optional edge slots appear
or disappear.

Checkbox, radio, switch, slider, tab bar, and segmented-control values resolve visual properties in this
order: the control's explicit style field, the matching tree theme field, then
semantic `ColorScheme`/geometry defaults. This permits partial overrides:

```csharp
new ThemeData(
    colors, typography, spacing, radius, buttonTheme,
    checkboxTheme: new CheckboxTheme(new CheckboxStyle(size: 20f)),
    sliderTheme: new SliderTheme(new SliderStyle(
        trackHeight: WidgetStateProperty<float?>.All(3f))),
    tabBarTheme: new TabBarTheme(new TabBarStyle(indicatorColor: colors.Primary)),
    segmentedControlTheme: new SegmentedControlTheme(
        new SegmentedControlStyle(selectedBackground: colors.Primary)));

new ListTile(
    new Text("Project", maxLines: 1, overflow: TextOverflow.Ellipsis),
    subtitle: new Text("Updated now"),
    leading: new Icon(LumaIcons.Folder),
    trailing: new Icon(LumaIcons.ChevronRight));
```

`ThemeData.CopyWith(...)` creates a local immutable variant without repeating
the complete constructor. Unspecified theme objects are retained; explicit
arguments replace only their matching slots.

`Image` renders an application-owned `Texture`, `Sprite`, or `VectorImage` with
fill/contain/cover fit. `CircleAvatar` clips an optional `Image` background and
an arbitrary foreground widget. LumaFlow never loads or destroys those Unity
assets; asset addressing, caching, and lifetime remain application concerns.

`LumaIcons` exposes the built-in icon catalog and the LumaFlow brand mark.
Use `new Icon(LumaIcons.LumaFlow)` when the mark is part of a widget layout.

For desktop interactive surfaces, `Pressable` also accepts a
`Func<WidgetStates, Widget>` builder. It rebuilds on hover, focus, press, and
disabled transitions while reconciling compatible nested state. An optional
`PointerCursor` applies a Unity custom cursor texture and hotspot. UI Toolkit
does not expose a portable public system-hand cursor identifier.

`PointerRegion` adds raw pointer callbacks and drag recognition without button
semantics or visual chrome. It captures only the pointer that starts a drag,
waits for `dragThreshold`, and reports both frame-local `Delta` and
gesture-wide `TotalDelta`. `OnDragEnd` receives `Cancelled = true` if capture is
lost, the region is disabled, or its node is unmounted. Event details are value
snapshots and remain safe after UI Toolkit releases its pooled event.

```csharp
new PointerRegion(
    child: new Container(child: new Text("Drag me")),
    onDragUpdate: details => position += details.Delta,
    onDragEnd: details => SavePosition(position),
    dragThreshold: 3f);
```

`Native` accepts optional `onMounted`, `onUpdated`, and `onUnmounted` hooks for
bridging third-party `VisualElement` APIs. Hooks run while the element is still
attached. Compatible declarative updates retain the element, invoke the newest
`onUpdated`, and use the newest `onUnmounted` callback during cleanup. A failed
`onMounted` is rolled back and still invokes `onUnmounted` exactly once.

## Navigation and overlays

- `Navigator`, `NavigatorHost`, `Route`, `RouteTransition`,
  `NavigationSnapshot`
- `OverlayController`, `OverlayHost`, `OverlayHandle`, `ModalOptions`
- `Dialog`, `ConfirmDialog`, `Toast`, `TooltipAnchor`, popover/context-menu/tooltip operations,
  and left/right drawers

See [Navigation and Overlays](Navigation%20and%20Overlays.md).

## Animations

- `Curve`, `Cubic`, `Curves`
- `Tween<T>`, `FloatTween`, `ColorTween`, `EdgeInsetsTween`,
  `BorderRadiusTween`
- `AnimationSpec`, `AnimationBehavior`
- `TweenAnimationBuilder<T>`, `AnimatedOpacity`

See [Animations](Animations.md).

## Accessibility and localization

- `Semantics`, `SemanticsProperties`, `ExcludeSemantics`, `SemanticsService`
- `Locale`, `Localizations`, `TextScaler`, `TextScale`

See [Accessibility and Localization](Accessibility%20and%20Localization.md).

## Diagnostics

`WidgetDiagnosticsNode.Properties` exposes a deterministic read-only map of
layout configuration and resolved flex data. The Widget Inspector includes
Row/Column axis and gap, alignment, flex/fit, box constraints, exact/expand
sizing, scroll direction, and the latest `LayoutBuilder` constraints.

- `MountHandle.CaptureDiagnostics`, `WidgetTreeDiagnostics`
- `WidgetDiagnosticsNode`, `LumaFlowDiagnostic`,
  `LumaFlowDiagnosticSeverity`, `LumaFlowDiagnostics`
- Editor: **Window > LumaFlow > Widget Inspector**

See [Diagnostics and Widget Inspector](Diagnostics.md).
