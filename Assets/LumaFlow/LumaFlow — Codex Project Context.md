# LumaFlow — Codex Project Context

## 1. Project Identity

**Project name:** LumaFlow  
**Project type:** Open-source Unity framework / package  
**Primary technology:** Unity UI Toolkit  
**Language:** C#  
**Distribution target:** Unity Package Manager first, Unity Asset Store optionally  
**License target:** MIT

LumaFlow is a declarative, reactive, component-oriented UI framework built **on top of Unity UI Toolkit**.

Its purpose is to make building interfaces in Unity feel closer to Flutter, SwiftUI, or modern declarative UI frameworks while preserving UI Toolkit as the underlying native rendering and layout system.

LumaFlow is **not a replacement renderer for UI Toolkit**.

LumaFlow is an ergonomic architecture and API layer over UI Toolkit.

Core idea:

```text
Developer
    ↓
LumaFlow declarative API
    ↓
LumaFlow runtime
    ↓
Unity VisualElement tree
    ↓
Unity UI Toolkit
```

The developer should normally interact with LumaFlow rather than manually managing `VisualElement`, UXML, USS classes, callbacks, query selectors, and UI lifecycle logic.

---

# 2. Product Vision

The desired developer experience should feel approximately like this:

```csharp
public sealed class SettingsScreen : StatelessView
{
    public override Widget Build(BuildContext context)
    {
        return Column(
            gap: 16,
            padding: EdgeInsets.All(24),
            children:
            [
                Text(
                    "Settings",
                    style: context.Theme.Typography.TitleLarge
                ),

                TextField(
                    label: "Player name"
                ),

                Row(
                    gap: 12,
                    children:
                    [
                        Button(
                            "Cancel",
                            onPressed: Cancel
                        ),

                        Button(
                            "Save",
                            variant: ButtonVariant.Primary,
                            onPressed: Save
                        )
                    ]
                )
            ]
        );
    }
}
```

Instead of:

```csharp
var root = rootVisualElement;

var title = root.Q<Label>("title");
var saveButton = root.Q<Button>("save");

title.AddToClassList("settings-title");
saveButton.clicked += Save;
```

combined with multiple UXML and USS files.

LumaFlow should make simple UI simple while still allowing advanced Unity UI Toolkit functionality when required.

---

# 3. Fundamental Principle

## LumaFlow wraps UI Toolkit. It does not fight it.

This principle has the highest architectural priority.

We should reuse:

- `VisualElement`
- Yoga/Flexbox layout
- UI Toolkit events
- UI Toolkit panels
- UI Toolkit focus system
- Unity styles
- Unity runtime data binding where appropriate
- Editor UI Toolkit support
- runtime UI Toolkit support

We should NOT create:

- a custom rendering engine;
- a replacement layout engine;
- a Canvas-like immediate renderer;
- our own input/event system when UI Toolkit already provides one;
- an unnecessary copy of Flutter's RenderObject architecture;
- an unnecessarily heavy virtual DOM.

Whenever UI Toolkit already solves a problem well, LumaFlow should expose a cleaner API around it instead of rebuilding the subsystem.

---

# 4. Primary Design Goals

LumaFlow must prioritize the following goals, roughly in this order:

1. Excellent developer experience.
2. Declarative composition.
3. Strong typing.
4. Minimal boilerplate.
5. Predictable behavior.
6. Native UI Toolkit compatibility.
7. Reusable components.
8. Reactive state.
9. Centralized design system / theming.
10. Runtime and Editor compatibility where technically reasonable.
11. Good performance.
12. Extensibility.
13. Easy debugging.

API elegance is a core product feature, not cosmetic polish.

If two implementations provide equivalent behavior, prefer the one that results in cleaner application code.

---

# 5. Non-Goals

LumaFlow is NOT intended to:

- recreate Flutter internally;
- recreate CSS;
- replace UI Toolkit rendering;
- hide Unity APIs so aggressively that interoperability becomes impossible;
- become an enormous dependency-injection/application framework;
- introduce unnecessary reflection;
- require code generation for basic usage;
- require UXML;
- require USS;
- require MonoBehaviours for ordinary UI components;
- force users into one state-management architecture;
- rebuild every UI Toolkit feature immediately.

LumaFlow should remain focused.

---

# 6. API Philosophy

LumaFlow API should follow several rules.

## 6.1 Declarative first

Prefer:

```csharp
return Column(
    children:
    [
        Text("Hello"),
        Button("Continue", onPressed: Continue)
    ]
);
```

over:

```csharp
var column = new VisualElement();
var label = new Label("Hello");
var button = new Button(Continue);

column.Add(label);
column.Add(button);

return column;
```

---

## 6.2 Composition over inheritance

User interfaces should primarily be built by composing widgets.

Prefer:

```csharp
Card(
    child: Padding(
        padding: EdgeInsets.All(16),
        child: Text("Hello")
    )
);
```

rather than creating a subclass for every minor visual variation.

Inheritance is acceptable for framework primitives such as:

```text
Widget
StatelessView
StatefulView
```

but ordinary UI composition should not require inheritance.

---

## 6.3 Common things must be concise

A button must not require ten lines of configuration.

Good:

```csharp
Button(
    "Save",
    onPressed: Save
)
```

Also good:

```csharp
Button(
    "Delete",
    variant: ButtonVariant.Danger,
    icon: Icons.Delete,
    onPressed: Delete
)
```

Bad:

```csharp
new ButtonWidget(
    new ButtonConfiguration
    {
        Content = new ButtonContentConfiguration
        {
            Text = "Save"
        }
    }
);
```

Do not create abstraction for abstraction's sake.

---

# 7. Widget Model

The public API revolves around a lightweight `Widget` abstraction.

Conceptually:

```text
Widget
    ↓
Build / Mount
    ↓
VisualElement
```

A Widget describes UI.

A VisualElement performs actual UI Toolkit rendering.

Widgets should remain lightweight.

Avoid putting Unity rendering responsibility inside every high-level widget.

Potential base abstraction:

```csharp
public abstract class Widget
{
    internal abstract VisualElement CreateElement(BuildContext context);
}
```

The exact implementation may evolve.

Do not lock the architecture prematurely if reconciliation or state requirements demand another representation.

---

# 8. Views

LumaFlow should provide at minimum:

```text
StatelessView
StatefulView
```

## StatelessView

Represents UI derived entirely from input/state supplied externally.

Example:

```csharp
public sealed class UserCard : StatelessView
{
    public required User User { get; init; }

    public override Widget Build(BuildContext context)
    {
        return Card(
            child: Column(
                children:
                [
                    Text(User.Name),
                    Text(User.Email)
                ]
            )
        );
    }
}
```

## StatefulView

Represents UI owning local state or lifecycle.

Desired usage:

```csharp
public sealed class CounterView : StatefulView
{
    private readonly State<int> _counter = new(0);

    public override Widget Build(BuildContext context)
    {
        return Column(
            children:
            [
                Text(() => $"Count: {_counter.Value}"),

                Button(
                    "Increment",
                    onPressed: () => _counter.Value++
                )
            ]
        );
    }
}
```

The precise state API should be designed based on performance and lifecycle requirements rather than copied blindly from Flutter.

---

# 9. Reactivity

Reactive state is a core LumaFlow capability.

Initial desired primitive:

```csharp
State<T>
```

Example:

```csharp
State<int> count = new(0);
State<string> username = new("");
State<bool> loading = new(false);
```

Potential API:

```csharp
count.Value++;
```

Bound UI:

```csharp
Text(() => count.Value.ToString());
```

or:

```csharp
Text(count.Select(value => value.ToString()));
```

The final syntax must be determined experimentally.

Important architectural requirements:

- subscriptions must have clear ownership;
- destroyed/unmounted UI must unsubscribe automatically;
- state changes must not leak delegates;
- avoid rebuilding the entire UI tree when only a property changed;
- updates should be as localized as possible;
- debugging reactive dependencies should remain understandable.

Do not add a complex reactive graph before simpler mechanisms have been validated.

---

# 10. Reconciliation Strategy

LumaFlow must NOT blindly recreate the entire `VisualElement` tree on every state update.

The framework should prefer localized updates.

Possible mechanisms include:

```text
Reactive property binding
        ↓
specific widget/element update
```

and later, if necessary:

```text
new widget description
        ↓
reconciliation/diff
        ↓
minimal VisualElement mutations
```

Do not implement a full virtual DOM simply because Flutter or React have analogous concepts.

First determine whether direct state bindings solve the majority of UI updates.

Optimize architecture for UI Toolkit rather than copying architecture from another framework.

---

# 11. BuildContext

LumaFlow should provide a scoped context system inspired by Flutter.

Potential API:

```csharp
context.Theme
context.Navigator
context.MediaQuery
context.Localization
context.Services
```

BuildContext should allow contextual dependencies to flow down the UI tree without relying on global singletons.

Example:

```csharp
Text(
    "Settings",
    style: context.Theme.Typography.TitleLarge
)
```

BuildContext should remain lightweight and understandable.

Do not turn it into an uncontrolled service locator.

---

# 12. Layout System

LumaFlow layout components should map closely to UI Toolkit Flexbox concepts while exposing a friendly declarative API.

Initial primitives:

```text
Row
Column
Stack
Expanded
Flexible
Spacer
Padding
Center
Align
SizedBox
Container
ScrollView
ListView
```

Example:

```csharp
Row(
    mainAxisAlignment: MainAxisAlignment.SpaceBetween,
    crossAxisAlignment: CrossAxisAlignment.Center,
    children:
    [
        Text("Volume"),

        Slider(
            value: volume
        )
    ]
);
```

Mappings should use UI Toolkit layout internally.

Do not implement a second independent layout algorithm.

---

# 13. Styling System

One of the major goals of LumaFlow is eliminating unnecessary USS boilerplate for component-level styling.

Desired API concepts:

```text
EdgeInsets
Alignment
Border
BorderSide
BorderRadius
Radius
BoxDecoration
BoxShadow
TextStyle
FontWeight
Color
Size
Constraints
```

Example:

```csharp
Container(
    width: 320,
    padding: EdgeInsets.Symmetric(
        horizontal: 24,
        vertical: 16
    ),
    decoration: BoxDecoration(
        color: context.Theme.Colors.Surface,
        borderRadius: BorderRadius.All(12)
    ),
    child: ...
)
```

Internally these should map efficiently to UI Toolkit style properties.

USS must remain interoperable.

Users should still be able to:

- attach USS stylesheets;
- specify USS classes;
- access underlying VisualElements where necessary.

LumaFlow should reduce the need for USS, not make USS impossible.

---

# 14. Theme System

LumaFlow must provide a first-class design-system layer.

No component should contain arbitrary visual literals when the value logically belongs to the theme.

Potential model:

```csharp
ThemeData
{
    Colors,
    Typography,
    Spacing,
    Radius,
    Shadows,
    Motion,
    Components
}
```

Example:

```csharp
public sealed class ColorScheme
{
    public Color Primary { get; init; }
    public Color OnPrimary { get; init; }

    public Color Surface { get; init; }
    public Color SurfaceContainer { get; init; }

    public Color TextPrimary { get; init; }
    public Color TextSecondary { get; init; }

    public Color Border { get; init; }

    public Color Success { get; init; }
    public Color Warning { get; init; }
    public Color Error { get; init; }
}
```

Usage:

```csharp
context.Theme.Colors.Primary
context.Theme.Spacing.M
context.Theme.Radius.Large
context.Theme.Typography.BodyMedium
```

Components should provide sensible theme-driven defaults.

Application code should not need to repeatedly specify:

```text
padding = 12
radius = 8
fontSize = 14
```

for every component.

---

# 15. Design Tokens

Design tokens must be centralized.

Do not scatter literals throughout framework components.

Typical categories:

```text
Colors
Spacing
Radius
Typography
Borders
Shadows
Motion durations
Motion curves
Control heights
Icon sizes
```

Framework defaults should live in clearly identifiable theme/token classes.

---

# 16. Components

LumaFlow should eventually provide reusable modern UI components.

Initial MVP components:

```text
Text
Icon
Button
IconButton
TextField
Toggle
Checkbox
Radio
Slider
Dropdown
Card
Divider
Tooltip
ScrollView
ListView
ProgressIndicator
```

Later:

```text
Tabs
Dialog
Modal
ContextMenu
Select
SearchField
Breadcrumbs
NavigationRail
NavigationBar
TreeView
Table
DataGrid
Toast
Notification
Popover
Menu
```

Do not implement large numbers of components before the core architecture stabilizes.

---

# 17. Component Variants

Components should support semantic variants rather than forcing users to manually restyle everything.

Example:

```csharp
Button(
    "Save",
    variant: ButtonVariant.Primary
);

Button(
    "Cancel",
    variant: ButtonVariant.Secondary
);

Button(
    "Delete",
    variant: ButtonVariant.Danger
);
```

Possible variants:

```text
Primary
Secondary
Ghost
Outline
Danger
```

Theme should control their actual appearance.

---

# 18. Navigation

Navigation should eventually be a first-class optional module.

Desired API:

```csharp
context.Navigator.Push(
    new SettingsScreen()
);

context.Navigator.Pop();

context.Navigator.Replace(
    new HomeScreen()
);
```

Potential route system:

```csharp
Routes(
    Route("/", () => new HomeScreen()),
    Route("/settings", () => new SettingsScreen())
);
```

Navigation is not required for the first minimal prototype.

Do not allow navigation work to delay validation of the core widget/state architecture.

---

# 19. Native UI Toolkit Escape Hatch

Every abstraction must provide a reasonable way to access UI Toolkit directly.

Possible mechanisms:

```csharp
Native(
    new VisualElement()
);
```

or:

```csharp
widget.Element
```

or controlled extension hooks.

Users must be able to integrate:

- custom VisualElements;
- third-party UI Toolkit controls;
- existing UXML;
- existing USS;
- Unity Editor controls;
- specialized Unity APIs.

LumaFlow must never create an architectural prison.

---

# 20. UXML Philosophy

UXML support is optional and secondary.

The primary LumaFlow development experience is code-first declarative C#.

Users should NOT need:

```text
Foo.uxml
Foo.uss
Foo.cs
FooController.cs
```

for every component.

However, interoperability with existing UXML should be possible.

Possible future capability:

```csharp
Uxml("SettingsPanel.uxml")
```

or custom LumaFlow controls exposed to UI Builder.

Do not sacrifice the code-first architecture to accommodate UXML.

---

# 21. USS Philosophy

USS remains useful for:

- global themes;
- transitions;
- large shared style definitions;
- application-specific styling;
- compatibility with existing projects.

But LumaFlow must allow developers to build polished interfaces without constantly switching between C# and USS files.

Inline typed styling should be considered a first-class feature.

---

# 22. Runtime and Editor

LumaFlow should aim to support both:

```text
Runtime UI
Editor UI
```

because UI Toolkit is used in both environments.

However:

- shared functionality belongs in `Runtime`;
- editor-only APIs belong in `Editor`;
- Runtime assemblies must never depend on `UnityEditor`;
- editor conveniences must remain optional.

Example package layout:

```text
LumaFlow/
├── package.json
├── README.md
├── CHANGELOG.md
├── LICENSE.md
│
├── Runtime/
│   ├── Core/
│   ├── Widgets/
│   ├── Layout/
│   ├── Styling/
│   ├── Reactive/
│   ├── Theming/
│   ├── Navigation/
│   └── LumaFlow.Runtime.asmdef
│
├── Editor/
│   ├── Widgets/
│   ├── Inspectors/
│   └── LumaFlow.Editor.asmdef
│
├── Tests/
│   ├── Runtime/
│   └── Editor/
│
├── Samples~/
│   ├── Basics/
│   ├── Components/
│   └── Dashboard/
│
└── Documentation~/
```

---

# 23. Namespace Policy

Namespaces should follow predictable boundaries.

Recommended root:

```csharp
LumaFlow
```

Possible modules:

```csharp
LumaFlow.Core
LumaFlow.Widgets
LumaFlow.Layout
LumaFlow.Styling
LumaFlow.Reactive
LumaFlow.Theming
LumaFlow.Navigation
LumaFlow.Editor
```

Avoid excessive namespace fragmentation.

If users routinely require ten `using` directives for basic UI, the namespace structure is too fragmented.

Consider exposing the common public API from a small number of namespaces.

---

# 24. Public API Quality Rules

Any public API added to LumaFlow should be evaluated against:

### Readability

Can a developer understand UI structure by reading the code?

### Discoverability

Can IDE autocomplete help find the functionality?

### Brevity

Does a common operation require unnecessary boilerplate?

### Consistency

Does the API behave similarly to related APIs?

### Type safety

Can invalid configuration be detected at compile time where practical?

### Extensibility

Can users extend the behavior without modifying framework internals?

### Unity compatibility

Does the abstraction work naturally with UI Toolkit?

---

# 25. Naming Rules

Prefer names familiar from modern UI development when the concept is equivalent.

Examples:

```text
Row
Column
Padding
Center
Align
Container
Stack
Text
Button
Card
Theme
BuildContext
State
```

Do not rename a known concept merely to make LumaFlow appear unique.

However, do not copy Flutter APIs mechanically if Unity semantics differ.

The goal is familiarity, not imitation.

---

# 26. Architecture Boundaries

Expected dependency direction:

```text
Components
      ↓
Layout / Styling / Reactive / Theme
      ↓
Core
      ↓
UI Toolkit
```

High-level modules may depend on low-level modules.

Core must NOT depend on high-level components.

Navigation should remain relatively isolated.

Editor code may depend on Runtime.

Runtime must never depend on Editor.

Avoid circular dependencies.

---

# 27. Performance Rules

UI frameworks can easily become allocation-heavy.

Pay attention to:

- unnecessary widget allocations;
- unnecessary VisualElement recreation;
- delegate allocations in frequent updates;
- event subscription leaks;
- GC pressure;
- excessive LINQ in hot paths;
- reflection;
- rebuilding unchanged trees;
- repeated style assignment;
- unnecessary hierarchy mutations.

Do not prematurely optimize tiny code paths at the cost of architecture clarity.

But avoid architectural decisions that inherently require large-scale rebuilding or allocation.

Performance-sensitive mechanisms should be benchmarked rather than guessed.

---

# 28. Lifecycle

LumaFlow needs explicit lifecycle semantics.

Potential concepts:

```text
Mount
Build
Update
Unmount
Dispose
```

State subscriptions and event handlers must follow lifecycle ownership.

A widget/view removed from the hierarchy must not keep:

- UI event subscriptions;
- state subscriptions;
- references to dead VisualElements;
- timers;
- callbacks;
- unmanaged resources.

Lifecycle behavior must be deterministic.

---

# 29. Testing

Framework functionality should be testable without manually opening demo scenes whenever possible.

Tests should prioritize:

- state propagation;
- mount/unmount lifecycle;
- reconciliation;
- style mapping;
- event cleanup;
- widget composition;
- theme propagation;
- BuildContext propagation.

Any bug in framework infrastructure should receive a regression test when practical.

---

# 30. Samples

Samples are part of the product.

The framework should eventually include:

### Basics

Shows:

```text
Text
Row
Column
Padding
Button
State
```

### Components

Shows the component library.

### Dashboard

A visually polished real interface demonstrating that LumaFlow can handle production UI.

Potential dogfooding target:

**AudioLib Editor UI**

LumaFlow should eventually be used to build a real complex Unity tool instead of existing only as synthetic demos.

---

# 31. Documentation Philosophy

Public APIs should have XML documentation when useful.

Documentation should focus on examples.

Prefer:

```csharp
Button(
    "Save",
    variant: ButtonVariant.Primary,
    onPressed: Save
);
```

plus a concise explanation over long theoretical descriptions.

Documentation must distinguish:

```text
LumaFlow concept
Unity UI Toolkit behavior
Flutter inspiration
```

Never imply that LumaFlow literally uses Flutter internally.

---

# 32. Codex Development Rules

When modifying this repository, follow these rules.

## Before implementing

1. Inspect the existing architecture.
2. Search for related implementations before creating new abstractions.
3. Understand public API conventions already established.
4. Determine which assembly/module owns the feature.
5. Check whether UI Toolkit already provides the underlying capability.

Do not immediately add a new framework abstraction because a task appears difficult.

---

## While implementing

Prefer small coherent changes.

Do not:

- create duplicate utilities;
- introduce global singleton state without justification;
- add public APIs unnecessarily;
- use reflection when typed APIs are possible;
- couple Runtime to UnityEditor;
- hide performance-sensitive behavior;
- scatter magic numbers;
- duplicate theme values;
- build features that UI Toolkit already provides sufficiently.

---

## Public API changes

Before introducing or changing public API, consider:

```text
What does application code look like?

Is it pleasant?

Is it obvious?

Does it compose?

Does it resemble the rest of LumaFlow?

Can we preserve this API long-term?
```

Public API design is more important than minimizing framework implementation code.

Do not expose implementation details merely because doing so is convenient internally.

---

# 33. Codex Must Preserve the Framework Philosophy

When asked to implement a feature, Codex must not blindly produce the most straightforward Unity code.

It must implement the feature according to LumaFlow's abstraction philosophy.

For example, if asked:

> Add padding support.

Do not merely expose:

```csharp
element.style.paddingLeft = ...
```

everywhere.

Determine whether padding belongs to:

```csharp
EdgeInsets
Padding widget
Container
Style mapping
Theme
```

and implement it consistently.

Similarly, if asked:

> Add a loading button.

Do not create an isolated special-case component if the same behavior could be elegantly expressed through existing Button state and composition.

---

# 34. Avoid Overengineering

LumaFlow should be ambitious but incremental.

Before implementing infrastructure ask:

> What current user-facing capability requires this abstraction?

Bad development pattern:

```text
Need button
↓
Design generic widget compiler
↓
Create virtual DOM
↓
Create dependency graph
↓
Create code generator
↓
Create custom scheduler
↓
Still no button
```

Good development pattern:

```text
Build basic widgets
↓
Discover repeated problem
↓
Extract minimal abstraction
↓
Test against real UI
↓
Iterate
```

Architecture should emerge from validated requirements.

---

# 35. MVP

The first usable milestone should prove the developer experience.

Suggested MVP:

### Core

```text
Widget
BuildContext
Mounting
Lifecycle basics
```

### Layout

```text
Row
Column
Padding
Center
Spacer
Expanded
Container
```

### Content

```text
Text
Icon
```

### Inputs

```text
Button
TextField
Toggle
```

### Styling

```text
EdgeInsets
Alignment
BorderRadius
Border
BoxDecoration
TextStyle
```

### Theme

```text
ThemeData
ColorScheme
Spacing
Radius
Typography
```

### Reactive

```text
State<T>
basic reactive binding
automatic cleanup
```

This should be sufficient to build a real Settings screen or AudioLib panel.

Do not implement routing, animation systems, complex forms, data grids, or advanced virtualization before this milestone works cleanly.

---

# 36. MVP Success Criterion

The framework is successful when a developer can build a polished UI primarily from C# and the resulting code is significantly easier to read and maintain than equivalent raw UI Toolkit code.

The following should feel natural:

```csharp
return Container(
    padding: EdgeInsets.All(24),
    child: Column(
        gap: 16,
        children:
        [
            Text(
                "Audio Library",
                style: context.Theme.Typography.TitleLarge
            ),

            TextField(
                label: "Search",
                value: search
            ),

            Expanded(
                child: AudioList(
                    items: filteredItems
                )
            )
        ]
    )
);
```

If accomplishing this requires users to understand the internal reconciliation engine, manually manipulate VisualElements, or constantly fall back to USS/UXML, the abstraction is failing.

---

# 37. Long-Term Direction

Possible later modules:

```text
LumaFlow.Animation
LumaFlow.Navigation
LumaFlow.Localization
LumaFlow.Forms
LumaFlow.Components
LumaFlow.Editor
LumaFlow.DevTools
```

Potential developer tooling:

```text
Widget tree inspector
State inspector
Layout debugging
Theme inspector
Hot-reload-like iteration tools
Component gallery
UI preview window
```

These are long-term possibilities, not initial requirements.

---

# 38. Compatibility Philosophy

LumaFlow should respect Unity package conventions.

Avoid relying on undocumented Unity internals unless absolutely necessary.

When an internal Unity API appears attractive, first look for a public supported alternative.

If an unsupported API is unavoidable:

1. isolate it;
2. document it;
3. guard it by Unity version;
4. provide a fallback when possible.

The framework should aim to survive Unity upgrades.

---

# 39. Dependency Philosophy

Keep dependencies minimal.

Core LumaFlow should ideally depend only on Unity modules required for UI Toolkit.

Third-party dependencies require strong justification.

Do not add a package simply to solve a small utility problem that can be cleanly handled internally.

At the same time, do not reimplement large mature systems without reason.

---

# 40. Source-Code Standards

Prefer:

- clear types;
- small focused classes;
- explicit ownership;
- readonly fields where appropriate;
- immutable value objects where useful;
- descriptive names;
- predictable nullability behavior;
- XML docs for public APIs;
- tests around framework infrastructure.

Avoid:

- giant utility classes;
- miscellaneous `Helpers`;
- hidden global mutable state;
- magic strings;
- magic numbers;
- excessive inheritance;
- clever code that sacrifices readability.

---

# 41. User Experience Is the Product

LumaFlow exists because standard UI Toolkit authoring can become verbose, fragmented, and uncomfortable for developers who prefer code-driven declarative UI.

Therefore always judge framework features from the consuming developer's point of view.

The important code is not:

```text
LumaFlow internal implementation
```

but:

```text
the code users write with LumaFlow
```

A feature should ideally be designed from the usage example backwards.

Before implementing a substantial public feature, write the desired consumer-side code first.

Example:

```csharp
var volume = State(0.8f);

return Column(
    children:
    [
        Text(() => $"Volume: {volume.Value:P0}"),

        Slider(
            value: volume,
            min: 0,
            max: 1
        )
    ]
);
```

Then design internals capable of supporting that API cleanly.

---

# 42. Final Architectural Rule

Whenever there is uncertainty, use this decision hierarchy:

```text
1. Is there already a good UI Toolkit mechanism?
        ↓ yes
   Wrap or expose it elegantly.

2. Does LumaFlow need a reusable abstraction?
        ↓ yes
   Create the smallest coherent abstraction.

3. Does the abstraction improve consumer code?
        ↓ no
   Do not add it.

4. Does it preserve native UI Toolkit interoperability?
        ↓ no
   Redesign it.

5. Is it required now?
        ↓ no
   Defer it.
```

LumaFlow should remain:

**Declarative.  
Reactive.  
Composable.  
Typed.  
Native to UI Toolkit.  
Pleasant to use.**

The framework exists to make Unity UI development feel modern without replacing the engine underneath it.