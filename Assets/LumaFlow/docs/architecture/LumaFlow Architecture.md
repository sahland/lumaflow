# LumaFlow Architecture

## 1. Purpose

This document defines the internal architecture of LumaFlow.

It describes how the framework translates declarative C# UI descriptions into native Unity UI Toolkit hierarchies and how state, lifecycle, styling, context, and updates are managed.

This document is normative.

When implementation details conflict with this document, either:

1. the implementation must be changed;
2. or this document must be intentionally updated through an architectural decision.

LumaFlow must not evolve through accidental architecture.

---

# 2. Architectural Goal

LumaFlow is a declarative UI framework built on top of Unity UI Toolkit.

The architecture must provide a developer experience similar to modern declarative frameworks while keeping Unity UI Toolkit responsible for:

- rendering;
- layout;
- input;
- focus;
- panel integration;
- native controls;
- editor integration;
- runtime UI integration.

Conceptually:

```text
Application Code
      ↓
LumaFlow Widgets
      ↓
LumaFlow Mount / Update Runtime
      ↓
VisualElement Hierarchy
      ↓
Unity UI Toolkit
```

LumaFlow must not introduce a second rendering engine.

---

# 3. Core Architectural Principles

## 3.1 UI Toolkit remains the rendering backend

Every visible LumaFlow control eventually corresponds to one or more native UI Toolkit `VisualElement` instances.

Examples:

```text
LumaFlow Text
    ↓
Label

LumaFlow Button
    ↓
UnityEngine.UIElements.Button

LumaFlow Row
    ↓
VisualElement
    flex-direction: row

LumaFlow Column
    ↓
VisualElement
    flex-direction: column
```

LumaFlow does not draw pixels itself.

---

## 3.2 Widgets are descriptions

A `Widget` represents a declaration of UI.

It is not itself the rendered UI element.

Conceptually:

```text
Widget
   ↓
Mounted Widget
   ↓
VisualElement
```

The public application API deals primarily with Widgets.

The internal framework runtime deals with mounted widget instances and `VisualElement`.

---

## 3.3 Mounted state must be explicit

A critical distinction must exist between:

```text
Widget description

and

Widget instance currently mounted into UI Toolkit
```

The runtime must not store lifecycle and subscription information directly in immutable or reusable Widget configuration objects.

Mounted UI needs its own runtime representation.

Working name:

```text
WidgetNode
```

or:

```text
ElementNode
```

The final public/internal naming may evolve.

---

# 4. Core Runtime Model

The preferred architecture is:

```text
Widget
   ↓
WidgetNode
   ↓
VisualElement
```

Responsibilities:

## Widget

Describes desired UI.

Contains configuration.

Examples:

```text
Text
Button
Column
Padding
Container
```

Widgets should ideally be lightweight.

---

## WidgetNode

Represents a mounted Widget.

Responsible for runtime behavior such as:

- lifecycle;
- child nodes;
- BuildContext;
- state subscriptions;
- event subscriptions;
- updates;
- disposal;
- reconciliation where required.

WidgetNode is an internal framework concept.

Application developers should normally not interact with it.

---

## VisualElement

The actual UI Toolkit object.

Responsible for native:

- rendering;
- Yoga layout;
- events;
- focus;
- styles;
- hierarchy integration.

---

# 5. Widget Base Type

Initial conceptual API:

```csharp
public abstract class Widget
{
}
```

The base `Widget` should remain minimal.

Avoid adding large amounts of runtime state to Widget itself.

Creation and mounting logic should be handled by framework runtime types.

Possible internal contract:

```csharp
internal abstract class WidgetNode
{
    public abstract VisualElement VisualElement { get; }

    public abstract void Mount(BuildContext context);
    public abstract void Update(Widget widget);
    public abstract void Unmount();
}
```

Exact implementation details may change during prototyping.

The architectural requirement is the separation between:

```text
description
runtime instance
native element
```

---

# 6. Why We Do Not Store VisualElement Directly in Widget

A tempting implementation is:

```csharp
public abstract class Widget
{
    public VisualElement Element { get; }
}
```

This should not be the default architecture.

Reasons:

- widgets become tied to a single mount location;
- widget reuse becomes difficult;
- lifecycle becomes ambiguous;
- state subscriptions can leak;
- rebuilding becomes harder;
- reconciliation becomes harder;
- component descriptions become mutable runtime objects;
- testability becomes worse.

Instead:

```text
Widget
```

should describe what should exist.

A runtime node should own what actually exists.

---

# 7. Widget Categories

LumaFlow widgets can conceptually fall into several categories.

## 7.1 Native widgets

Directly map to UI Toolkit controls.

Examples:

```text
Text
Button
TextField
Toggle
Slider
```

Example:

```text
Text
 ↓
Label
```

---

## 7.2 Layout widgets

Configure hierarchy and layout.

Examples:

```text
Row
Column
Padding
Center
Expanded
Spacer
Container
Stack
```

Some layout widgets may create their own `VisualElement`.

Others may be optimized later to alter parent/child layout behavior.

Start with correctness and simplicity.

Optimize structural wrappers only when profiling justifies it.

---

## 7.3 Composite widgets

Build other widgets.

Examples:

```text
Card
Dialog
SettingsTile
NavigationItem
```

These should usually be implemented through composition.

---

## 7.4 View widgets

User-defined reusable UI.

Examples:

```text
StatelessView
StatefulView
```

They expose:

```csharp
Widget Build(BuildContext context)
```

---

# 8. StatelessView

A StatelessView describes UI derived entirely from current inputs and context.

Conceptual API:

```csharp
public abstract class StatelessView : Widget
{
    public abstract Widget Build(BuildContext context);
}
```

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

A StatelessView should not own mutable UI lifecycle state.

---

# 9. StatefulView

A StatefulView may own local state and lifecycle.

Conceptual API:

```csharp
public abstract class StatefulView : Widget
{
    public abstract Widget Build(BuildContext context);
}
```

However, runtime state must belong to the mounted node associated with the view.

Do not blindly copy Flutter's `StatefulWidget + State<T>` model unless it proves necessary.

LumaFlow already plans to expose reactive `State<T>` values.

Therefore the initial implementation should prefer a simpler ownership model.

Example desired usage:

```csharp
public sealed class CounterView : StatefulView
{
    private readonly State<int> _count = new(0);

    public override Widget Build(BuildContext context)
    {
        return Column(
            children:
            [
                Text(() => $"Count: {_count.Value}"),

                Button(
                    "Increment",
                    onPressed: () => _count.Value++
                )
            ]
        );
    }
}
```

This syntax is conceptual until lifecycle ownership is validated.

---

# 10. Mounting

Mounting converts a Widget description into active UI.

Conceptually:

```text
Widget
 ↓
Create WidgetNode
 ↓
Create VisualElement
 ↓
Attach children
 ↓
Subscribe bindings/events
 ↓
Attach to parent VisualElement
```

The framework should have a single well-defined mounting system.

Possible central service:

```text
WidgetMount
```

or:

```text
WidgetRuntime
```

Do not allow individual components to invent incompatible lifecycle rules.

---

# 11. Mount Operation

Conceptually:

```csharp
var handle = LumaFlow.Mount(
    widget,
    rootVisualElement
);
```

The application may eventually use higher-level abstractions, but internally mount must return or retain ownership of the mounted tree.

A mount operation should establish:

- root WidgetNode;
- root BuildContext;
- native hierarchy;
- reactive subscription scope;
- lifecycle ownership.

---

# 12. Unmounting

Unmounting must be deterministic.

When a widget subtree is removed, LumaFlow must release:

- State subscriptions;
- event handlers;
- callbacks;
- scheduled tasks;
- context listeners;
- references to removed VisualElements;
- child nodes.

Conceptually:

```text
Unmount node
   ↓
Unmount children
   ↓
Dispose bindings
   ↓
Dispose event subscriptions
   ↓
Detach VisualElement
   ↓
Release references
```

Unmount must be safe to call exactly once.

Where practical, repeated calls should fail safely or become no-ops.

---

# 13. Lifecycle

Initial internal lifecycle:

```text
Created
   ↓
Mounted
   ↓
Active
   ↓
Updated zero or more times
   ↓
Unmounted
   ↓
Disposed
```

Not every state necessarily needs a public enum.

The important requirement is deterministic ownership.

Potential view lifecycle hooks may eventually include:

```csharp
OnMount()
OnUnmount()
```

Potential future hooks:

```csharp
OnContextChanged()
OnDependenciesChanged()
```

Do not expose many lifecycle hooks before there is a demonstrated use case.

---

# 14. BuildContext

BuildContext carries contextual information through the mounted tree.

Conceptually:

```csharp
public sealed class BuildContext
{
    public ThemeData Theme { get; }
    public MediaQueryData MediaQuery { get; }
    public Navigator Navigator { get; }
}
```

The exact properties will evolve.

BuildContext must support hierarchical overrides.

Example:

```text
Root Theme
   ↓
Application
   ↓
ThemeOverride
   ↓
Dialog
```

A widget below `ThemeOverride` sees the nearest theme value.

---

# 15. Context Propagation

Context should use immutable or persistent inheritance semantics where practical.

Conceptually:

```text
Parent BuildContext
        ↓
WithTheme(...)
        ↓
Child BuildContext
```

Example:

```csharp
var childContext = context.WithTheme(darkTheme);
```

Avoid global mutable singleton state.

Widgets should resolve contextual dependencies from their mounted location.

---

# 16. Context Scope

BuildContext is not a general-purpose dependency injection container.

It exists for tree-scoped UI concerns.

Good candidates:

```text
Theme
MediaQuery
Navigator
Localization
Focus scope
UI density
Platform information
```

Potential application services should use a deliberate provider mechanism if introduced.

Avoid:

```csharp
context.GetAnything<MyRandomService>();
```

as the default architecture.

---

# 17. State<T>

Reactive state is a fundamental primitive.

Conceptual public API:

```csharp
public sealed class State<T>
{
    public T Value { get; set; }
}
```

Setting a different value notifies subscribers.

Example:

```csharp
var volume = new State<float>(0.8f);

volume.Value = 0.5f;
```

State must not know about specific widgets.

It should expose a generic subscription mechanism.

---

# 18. State Notifications

Conceptually:

```text
State<T>
   ↓
Value changed
   ↓
Subscribers notified
   ↓
Affected mounted nodes update
```

State updates should ideally affect only UI depending on that state.

Avoid global `Build()` invocation unless necessary.

---

# 19. Reactive Binding

LumaFlow should initially prefer direct reactive bindings over complete tree reconstruction.

Example:

```csharp
Text(() => $"Volume: {volume.Value:P0}")
```

Conceptually creates:

```text
Text widget
   ↓
Reactive binding
   ↓
Label.text
```

When `volume` changes:

```text
State<float>
   ↓
binding callback
   ↓
Label.text updated
```

The surrounding Column does not need rebuilding.

---

# 20. Explicit Binding Is Preferable to Magical Dependency Tracking

The framework should avoid sophisticated hidden dependency tracking in the first versions.

For example:

```csharp
Text(() => state.Value)
```

looks convenient, but automatically discovering which State objects were accessed requires dependency tracking infrastructure.

That may eventually be justified.

Initially, strongly consider explicit variants such as:

```csharp
Text(
    state: count,
    text: value => $"Count: {value}"
)
```

or:

```csharp
count.Bind(
    value => Text(value.ToString())
)
```

or another typed API.

The final consumer syntax must be evaluated for ergonomics.

Do not introduce implicit reactive dependency tracking until its lifecycle, performance, and debugging behavior are understood.

---

# 21. State Ownership

State ownership must be clear.

Possible sources:

```text
View-local state
Parent-owned state
Application-owned state
External model
Unity binding source
```

LumaFlow must not assume all state belongs to the UI.

State objects supplied from outside a widget must not automatically be disposed when the widget unmounts.

Subscriptions to them must be disposed.

Ownership principle:

```text
LumaFlow owns subscriptions.

The creator of State<T> owns State<T>,
unless ownership is explicitly transferred.
```

---

# 22. Binding Scope

Each mounted WidgetNode should have a disposable binding scope.

Conceptually:

```csharp
internal sealed class BindingScope : IDisposable
{
    private readonly List<IDisposable> _bindings;
}
```

When the node unmounts:

```csharp
_bindingScope.Dispose();
```

This should remove every reactive subscription owned by that node.

This prevents event and state leaks.

---

# 23. Events

UI Toolkit remains responsible for native events.

LumaFlow adapts them to ergonomic callbacks.

Example:

```csharp
Button(
    "Save",
    onPressed: Save
)
```

Internally:

```text
LumaFlow Button
   ↓
UI Toolkit Button.clicked
   ↓
user callback
```

Event subscriptions must be cleaned up on unmount where required.

---

# 24. Layout Architecture

LumaFlow does not calculate normal layout.

UI Toolkit Yoga/Flexbox remains responsible.

Example:

```csharp
Row(...)
```

maps to:

```text
VisualElement
flex-direction: row
```

Column maps to:

```text
VisualElement
flex-direction: column
```

---

# 25. MainAxisAlignment Mapping

Conceptually:

```text
Start        → flex-start
Center       → center
End          → flex-end
SpaceBetween → space-between
SpaceAround  → space-around
```

Exact UI Toolkit mappings must be implemented using public supported APIs.

---

# 26. CrossAxisAlignment Mapping

Conceptually:

```text
Start   → flex-start
Center  → center
End     → flex-end
Stretch → stretch
```

Again, UI Toolkit performs actual layout.

---

# 27. Expanded

`Expanded` should use native flex behavior.

Conceptually:

```csharp
Expanded(
    child: content,
    flex: 1
)
```

maps approximately to:

```text
flex-grow: 1
flex-shrink: appropriate value
```

The exact behavior must be validated against UI Toolkit semantics.

Do not recreate Flutter layout semantics if Yoga behaves differently.

LumaFlow should provide predictable Unity-native behavior with familiar naming.

---

# 28. Gap

Row and Column should support:

```csharp
gap: 12
```

If the targeted UI Toolkit version supports suitable native gap behavior, use it.

Otherwise LumaFlow may emulate gap using controlled child spacing.

Any emulation must:

- preserve first/last child behavior;
- update correctly when children change;
- avoid unnecessary wrapper elements where possible.

---

# 29. Stack

Stack requires special handling using UI Toolkit positioning.

Conceptually:

```csharp
Stack(
    children:
    [
        background,
        Positioned(
            right: 16,
            top: 16,
            child: badge
        )
    ]
)
```

The Stack container may establish the positioning context.

Positioned children should map to native absolute positioning.

Do not create a custom layout algorithm.

---

# 30. Styling Architecture

LumaFlow exposes typed style models and maps them into UI Toolkit styles.

Conceptually:

```text
EdgeInsets
BorderRadius
BoxDecoration
TextStyle
        ↓
Style Mapper
        ↓
VisualElement.style
```

Style objects should primarily be data.

Mapping logic should remain centralized where practical.

---

# 31. Style Mapping

Avoid components repeatedly doing low-level conversions.

Bad:

```csharp
element.style.paddingLeft = padding.Left;
element.style.paddingTop = padding.Top;
...
```

duplicated in many controls.

Prefer reusable mapping utilities such as:

```csharp
StyleMapper.ApplyPadding(element, padding);
StyleMapper.ApplyBorder(element, border);
StyleMapper.ApplyTextStyle(label, textStyle);
```

Do not create a giant universal utility class.

Split mapping by clear concern.

---

# 32. Typed Style Values

Prefer typed values.

Examples:

```csharp
EdgeInsets
BorderRadius
Border
BorderSide
TextStyle
BoxDecoration
```

Avoid APIs requiring arbitrary string values such as:

```csharp
padding: "10px 20px"
```

unless deliberately supporting raw USS escape hatches.

---

# 33. Immutable Style Objects

Style/configuration value objects should preferably be immutable.

Example:

```csharp
public readonly struct EdgeInsets
{
    public float Left { get; }
    public float Top { get; }
    public float Right { get; }
    public float Bottom { get; }
}
```

This improves:

- predictable comparisons;
- sharing;
- caching;
- thread-independent construction;
- debugging.

Use classes when struct copying becomes inappropriate.

---

# 34. Theme Architecture

Theme data flows through BuildContext.

Conceptually:

```text
ThemeData
   ↓
BuildContext
   ↓
Widget
   ↓
Component style resolver
   ↓
UI Toolkit styles
```

Components should resolve defaults from theme.

Example:

```csharp
Button("Save")
```

implicitly uses:

```text
Theme.Button.Primary
```

or equivalent theme configuration.

---

# 35. Theme Resolution

Component style resolution should conceptually follow:

```text
explicit widget property
        ↓
component theme
        ↓
global theme token
        ↓
framework default
```

Example:

```csharp
Button(
    "Save",
    radius: 20
)
```

Explicit `radius` overrides theme.

Without it, Button uses the theme-provided radius.

---

# 36. Native Escape Hatch

Advanced users must be able to integrate native UI Toolkit.

Possible widget:

```csharp
Native(
    new CustomVisualElement()
)
```

This wraps an existing VisualElement.

The runtime must define ownership:

- whether it may remove the element;
- whether it disposes anything;
- whether the element may already have a parent.

Native interop must not bypass framework lifecycle silently.

---

# 37. Native Widget Ownership

By default, a VisualElement supplied to LumaFlow should not be destroyed by the framework beyond normal hierarchy removal.

Unity UI Toolkit VisualElements are managed objects.

LumaFlow owns:

```text
mounting
parenting
subscriptions added by LumaFlow
```

It does not claim arbitrary external resource ownership.

---

# 38. Reconciliation

Full tree reconciliation is intentionally deferred.

Initial architecture should prioritize:

```text
stable mounted hierarchy
+
localized reactive updates
```

A complete:

```text
Build
↓
diff
↓
patch
```

system should only be introduced if real component requirements demand structural rebuilding.

---

# 39. Structural State Changes

Some state changes necessarily affect tree structure.

Example:

```csharp
if (loading.Value)
    return ProgressIndicator();

return Content();
```

This cannot always be solved by updating one property.

LumaFlow therefore needs a controlled mechanism for rebuilding a subtree.

Possible future primitive:

```text
ReactiveBuilder
```

Example:

```csharp
ReactiveBuilder(
    state: loading,
    builder: value =>
        value
            ? ProgressIndicator()
            : Content()
)
```

Only that subtree is rebuilt.

This is preferred over rebuilding the entire application tree.

---

# 40. Rebuild Boundary

Structural reactive widgets should establish explicit rebuild boundaries.

Conceptually:

```text
Application
 ├─ Header
 ├─ Sidebar
 └─ ReactiveBuilder
       ↓
       rebuilt subtree only
```

This provides predictable update cost.

---

# 41. Future Reconciliation

If future API requirements make declarative rebuilds common, LumaFlow may introduce reconciliation.

If introduced, reconciliation should compare:

```text
old widget description
vs
new widget description
```

and reuse compatible WidgetNodes/VisualElements.

Possible identity rules may include:

```text
runtime widget type
key
position
```

However this architecture must be justified by actual needs.

Do not implement reconciliation in MVP simply because other declarative frameworks use it.

---

# 42. Widget Keys

Keys may eventually be necessary for dynamic lists and reconciliation.

Potential API:

```csharp
UserCard(
    key: Key(User.Id),
    user: User
)
```

Keys are not required until structural reconciliation exists.

Do not expose a public concept before it has semantic purpose.

---

# 43. Lists

Small static lists can use regular child mounting.

Large data sets should eventually rely on native UI Toolkit virtualization where possible.

LumaFlow `ListView` should preferably adapt Unity's existing list virtualization rather than rendering thousands of child widgets manually.

Conceptually:

```text
LumaFlow ListView
        ↓
UI Toolkit ListView
```

while exposing a declarative item builder.

---

# 44. List Item Builder

Desired API concept:

```csharp
ListView(
    items: users,
    itemBuilder: user =>
        UserCard(user)
)
```

The implementation should respect native recycling behavior.

This may require specialized mounting semantics for recycled item containers.

Do not assume ordinary static child mounting works correctly for virtualized controls.

---

# 45. Runtime vs Editor Architecture

Shared architecture belongs in Runtime.

```text
LumaFlow.Runtime
```

must not depend on:

```text
UnityEditor
```

Editor-specific integrations live in:

```text
LumaFlow.Editor
```

Dependency direction:

```text
Editor
  ↓
Runtime
```

Never:

```text
Runtime
  ↓
Editor
```

---

# 46. Render Pipeline Independence

Core architecture must remain independent from:

```text
Built-in Render Pipeline
URP
HDRP
```

LumaFlow Core must not directly reference:

```text
UnityEngine.Rendering.Universal
UnityEngine.Rendering.HighDefinition
```

Ordinary UI must work under all supported render pipelines.

Pipeline-specific effects belong in optional isolated modules.

---

# 47. Render Pipeline Architecture

If advanced rendering features are introduced:

```text
LumaFlow effect API
        ↓
effect abstraction
        ↓
pipeline implementation
```

Potential modules:

```text
LumaFlow.Effects
LumaFlow.Effects.URP
LumaFlow.Effects.HDRP
```

Built-in support should use pipeline-independent or Built-in-compatible implementations.

The framework must remain usable when none of those optional effect modules are installed.

---

# 48. Assembly Architecture

Initial proposed assemblies:

```text
LumaFlow.Runtime
LumaFlow.Editor
LumaFlow.Tests.Runtime
LumaFlow.Tests.Editor
```

Do not prematurely create many assemblies.

As the package grows, Runtime may be split into stable logical modules.

Possible future split:

```text
LumaFlow.Core
LumaFlow.Components
LumaFlow.Navigation
LumaFlow.Editor
```

Only split when:

- compile boundaries matter;
- optional dependencies require isolation;
- architectural ownership becomes clearer.

Too many asmdefs increase maintenance cost.

---

# 49. Error Handling

Framework errors should fail clearly.

Bad:

```text
NullReferenceException inside MountNode.cs:183
```

Better:

```text
LumaFlow: Button requires a non-null child.
```

or:

```text
LumaFlow: This WidgetNode is already mounted.
```

Framework-level invariant violations should produce descriptive exceptions in development.

Avoid swallowing exceptions.

---

# 50. Debuggability

LumaFlow must remain inspectable using normal UI Toolkit debugging tools wherever possible.

The generated hierarchy should contain understandable element names/classes where useful.

Do not produce inscrutable hierarchy wrappers without purpose.

Potential future debug metadata:

```text
Widget type
Widget source
State bindings
Theme source
```

may be exposed through dedicated dev tools.

---

# 51. Allocation Philosophy

Declarative APIs naturally create configuration objects.

Some allocation is acceptable.

Do not compromise public API quality to eliminate insignificant allocations.

However, avoid:

- allocating every frame;
- rebuilding complete trees unnecessarily;
- repeatedly allocating subscriptions;
- LINQ inside hot state propagation;
- unnecessary temporary collections;
- wrapper VisualElements with no behavioral purpose.

Measure before major optimization.

---

# 52. Main Thread Requirement

UI Toolkit mutations occur on Unity's main thread.

LumaFlow must treat native UI mutation as main-thread work.

State values may eventually be changed from asynchronous workflows.

If cross-thread updates become supported, LumaFlow must marshal resulting UI mutations safely.

Do not silently permit unsafe VisualElement modification from worker threads.

MVP may explicitly require State changes affecting UI to occur on Unity's main thread.

---

# 53. Async Operations

Widgets such as buttons may trigger asynchronous work:

```csharp
Button(
    "Login",
    onPressed: LoginAsync
)
```

Async callback ergonomics may be added later.

Do not make async execution part of core widget lifecycle until API semantics are defined.

Potential concerns include:

- exceptions;
- cancellation;
- unmount during operation;
- loading state;
- main-thread continuation.

---

# 54. Disposal

Framework runtime nodes should use deterministic cleanup.

Potential internal contract:

```csharp
internal interface IDisposableNode
{
    void Dispose();
}
```

Unmount and Dispose responsibilities must be clearly separated or intentionally unified.

Do not maintain two lifecycle methods with overlapping unclear behavior.

---

# 55. Architecture of a Simple Button

Example internal flow:

```text
Application:
Button(
    "Save",
    onPressed: Save
)

        ↓

Button Widget

        ↓

ButtonNode

        ↓

new UnityEngine.UIElements.Button()

        ↓

button.text = "Save"

        ↓

button.clicked += callback

        ↓

theme/style mapping

        ↓

VisualElement mounted
```

Unmount:

```text
ButtonNode.Unmount()

        ↓

remove managed subscriptions if necessary

        ↓

remove hierarchy relationship

        ↓

release references
```

---

# 56. Architecture of Reactive Text

Conceptual flow:

```text
State<int>
    ↓
Text binding
    ↓
Label
```

Initial mount:

```text
read current value
↓
set Label.text
↓
subscribe to state
```

Update:

```text
State changes
↓
binding receives value
↓
Label.text updated
```

Unmount:

```text
binding disposed
↓
state subscription removed
```

This is the model LumaFlow should prefer for simple reactive values.

---

# 57. Architecture of Structural Reactivity

Example:

```csharp
ReactiveBuilder(
    state: authenticated,
    builder: isAuthenticated =>
        isAuthenticated
            ? Dashboard()
            : LoginScreen()
)
```

Mount:

```text
ReactiveBuilderNode
        ↓
evaluate builder
        ↓
mount child subtree
```

State update:

```text
authenticated changes
        ↓
unmount current child subtree
        ↓
evaluate builder
        ↓
mount new subtree
```

Future reconciliation may optimize replacement.

Initial correctness is more important.

---

# 58. Design Rule: Consumer API First

Architecture decisions must begin from desired application code.

For any significant feature:

1. write ideal consumer-facing usage;
2. verify that it is understandable;
3. determine required semantics;
4. design minimal internal infrastructure;
5. implement;
6. test against real UI.

Never expose awkward API merely because internal implementation is easier.

---

# 59. Design Rule: Unity Semantics Beat Flutter Semantics

LumaFlow borrows vocabulary and UX ideas from Flutter.

Flutter is not the source of truth for internal behavior.

When Flutter semantics conflict with Unity UI Toolkit semantics:

```text
Prefer predictable UI Toolkit-native behavior.
```

Document differences.

Example:

`Expanded` should feel familiar but must ultimately obey Yoga/UI Toolkit layout.

---

# 60. Design Rule: No Hidden Architecture

Avoid framework behavior that developers cannot reason about.

Be cautious with:

- reflection-driven bindings;
- automatic global dependency detection;
- implicit service location;
- invisible tree reconstruction;
- magic naming conventions;
- code generation without necessity.

LumaFlow should feel high-level but remain understandable.

---

# 61. Prototype Strategy

Core architecture must be validated using real implementations before becoming permanent.

Recommended first prototype:

```text
Widget
WidgetNode
BuildContext
Mount
Unmount

Column
Row
Padding
Container

Text
Button

State<T>
State binding

ThemeData
```

Then build:

```text
Settings screen
```

and a small part of:

```text
AudioLib Editor UI
```

Use dogfooding to expose architectural problems.

---

# 62. Architectural Questions to Validate During MVP

The first implementation should explicitly test:

1. Is `Widget → WidgetNode → VisualElement` sufficiently simple?
2. Should every Widget create a VisualElement?
3. Can some layout widgets be structural-only?
4. How should reactive binding syntax look?
5. How should State ownership work inside user-defined views?
6. Does BuildContext require parent-linked lookup or copied values?
7. Do component styles need caching?
8. How should dynamic children be updated?
9. When is subtree rebuilding necessary?
10. Can UI Toolkit native virtualization integrate cleanly?
11. How should custom VisualElements be wrapped?
12. Which lifecycle hooks are actually necessary?

Do not prematurely encode unanswered questions into public API.

---

# 63. Architecture Stability

Internal APIs may evolve aggressively during pre-1.0 development.

Public API should evolve more carefully.

Before 1.0:

```text
Internal architecture:
free to improve

Public API:
allowed to change,
but deliberately
```

Once stable versions are released, backward compatibility becomes more important.

---

# 64. Architectural Invariants

The following must remain true unless explicitly changed through an ADR:

### Invariant 1

UI Toolkit is the native rendering/layout backend.

### Invariant 2

LumaFlow Core has no URP/HDRP dependency.

### Invariant 3

Runtime has no UnityEditor dependency.

### Invariant 4

Mounted lifecycle is separate from Widget configuration.

### Invariant 5

Subscriptions are automatically cleaned on unmount.

### Invariant 6

Simple state updates should not rebuild the whole UI tree.

### Invariant 7

UI Toolkit interoperability remains available.

### Invariant 8

Public API is designed from consumer usage backwards.

### Invariant 9

No full custom renderer or layout engine.

### Invariant 10

Complex infrastructure requires demonstrated need.

---

# 65. Target Architecture

The intended long-term conceptual structure is:

```text
                    Application
                         │
                         ▼
                  StatelessView
                  StatefulView
                         │
                         ▼
                       Widget
                         │
                         ▼
                    WidgetNode
                ┌────────┼────────┐
                │        │        │
                ▼        ▼        ▼
             Context   State    Styling
                │        │        │
                └────────┼────────┘
                         │
                         ▼
                   VisualElement
                         │
                         ▼
                  Unity UI Toolkit
                         │
          ┌──────────────┼──────────────┐
          ▼              ▼              ▼
       Runtime         Editor      Native Layout
                                      & Rendering
```

Optional future architecture:

```text
LumaFlow
├── Core
├── Widgets
├── Reactive
├── Theming
├── Navigation
├── Editor
└── Effects
      ├── Built-in compatible
      ├── URP integration
      └── HDRP integration
```

Core remains pipeline-independent.

---

# 66. Final Architecture Principle

LumaFlow should feel like a modern declarative UI framework from the outside while behaving like a disciplined UI Toolkit abstraction on the inside.

The ideal architecture is not the one that most closely reproduces Flutter.

The ideal architecture is the smallest system that allows developers to write:

```csharp
return Column(
    gap: 16,
    children:
    [
        Text(
            "Settings",
            style: context.Theme.Typography.TitleLarge
        ),

        TextField(
            label: "Username",
            value: username
        ),

        Button(
            "Save",
            variant: ButtonVariant.Primary,
            onPressed: Save
        )
    ]
);
```

while LumaFlow handles:

```text
VisualElement creation
layout mapping
theme resolution
event wiring
state subscriptions
lifecycle
cleanup
```

and Unity UI Toolkit continues handling:

```text
rendering
layout calculation
input
focus
platform integration
```

That boundary is the foundation of LumaFlow.