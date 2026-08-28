# Controls and UI composition

LumaFlow controls own interaction, state, semantics and lifecycle. Product UI
kits should wrap these controls with application tokens rather than replacing
their behavior with native UI Toolkit code.

## Controlled values

Text fields, checkbox, radio, switch, slider, dropdown, tabs and segmented
controls receive an externally owned `State<T>` or `FormField<T>`. A user
interaction commits the value before invoking `onChanged`. Programmatic state
changes update the mounted control without invoking the callback.

Keep controllers and state outside `Build` when they must survive a rebuild.

## Buttons and custom interaction

`Button` accepts either text or an arbitrary child. Arbitrary content requires
a semantic label:

```csharp
new Button(
    new Row(
        new Widget[] {
            new Icon(LumaIcons.Plus),
            new Text("New project")
        },
        gap: 8f),
    onPressed: CreateProject,
    semanticsLabel: "Create project");
```

Use `IconButton` for icon-only actions and `Pressable` when a custom surface
needs pointer, keyboard, focus and button semantics without native button
painting.

## Tabs

`TabBar<T>` selects a typed value held by `State<T>`. `TabView<T>` observes
the same state and builds its content. The two widgets may be placed in separate
containers.

```csharp
var section = new State<string>("overview");
var tabs = new[] {
    new TabItem<string>("overview", "Overview"),
    new TabItem<string>("activity", "Activity")
};

new Column(new Widget[] {
    new TabBar<string>(section, tabs),
    new Expanded(new TabView<string>(
        section,
        value => value == "overview"
            ? new OverviewPanel()
            : new ActivityPanel()))
});
```

Use the `TabItem<T>(value, child, semanticsLabel)` overload for an icon, label
or badge composition. Duplicate values and a selected value absent from the
item list are rejected.

`ThemeData.TabBarTheme` supplies defaults. Explicit `TabBarStyle` fields
override the corresponding theme fields.

## Segmented control

`SegmentedControl<T>` represents a small mutually exclusive setting or filter.
It is distinct from navigation tabs.

```csharp
var period = new State<string>("day");

new SegmentedControl<string>(
    period,
    new[] {
        new SegmentedControlItem<string>("day", "Day"),
        new SegmentedControlItem<string>("week", "Week"),
        new SegmentedControlItem<string>("month", "Month")
    },
    onChanged: ReloadMetrics);
```

Items accept text or an arbitrary child with a semantic label.
`ThemeData.SegmentedControlTheme` supplies defaults, and explicit
`SegmentedControlStyle` fields take precedence.

## Component themes

`ThemeData` contains defaults for buttons, icons, text fields, checkbox, radio,
switch, slider, dropdown, tabs and segmented controls. A component style is
resolved field by field, so an explicit value does not discard unrelated theme
defaults.

Use `WidgetStateProperty<T>` for values that depend on interaction state:

```csharp
var checkboxStyle = new CheckboxStyle(
    fillColorByState: WidgetStateProperty<Color?>.ResolveWith(states =>
        (states & WidgetStates.Disabled) != 0 ? disabled :
        (states & WidgetStates.Selected) != 0 ? selected : surface));
```

## Reusable product widgets

A product UI kit can expose small compositions such as a user row, status badge,
progress row or form section. Keep product colors, spacing and typography in the
application assembly. Pass domain data and callbacks through constructors, and
let the wrapped LumaFlow controls retain ownership of focus, input and
semantics.

## Tooltips and overlays

`TooltipAnchor` displays a themed tooltip over any child on hover or focus and
also supplies UI Toolkit's native tooltip fallback:

```csharp
new TooltipAnchor(
    new IconButton(LumaIcons.Settings, OpenSettings),
    new Tooltip("Settings"),
    overlays);
```

The anchor closes its overlay on pointer leave, focus loss, controller changes
and unmount. `ModalOptions` configures barrier dismissal, back dismissal,
focus and barrier color.

