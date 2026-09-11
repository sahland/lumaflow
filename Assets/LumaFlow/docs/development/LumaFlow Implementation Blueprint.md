# LumaFlow Implementation Blueprint

This document defines the initial implementation order for LumaFlow.

It is not an architecture decision record.

Architecture decisions are defined by:

- `docs/architecture/ARCHITECTURE.md`
- `docs/architecture/API_DESIGN.md`
- `docs/architecture/COMPATIBILITY.md`
- `docs/architecture/adr/`

This document answers a different question:

> In what order should the accepted architecture be implemented?

The primary goal is to reach a small but complete vertical slice before expanding the framework.

---

# 1. Implementation Strategy

LumaFlow should be implemented vertically rather than by building every subsystem in isolation.

The first meaningful result must prove:

```text
Widget
    ↓
WidgetNode
    ↓
VisualElement

State<T>
    ↓
binding
    ↓
native property update
```

through real components.

The first vertical slice should eventually allow:

```csharp
Column(
    gap: 12,
    children:
    [
        Text(
            value: counter,
            format: value => $"Count: {value}"
        ),

        Button(
            "Increment",
            onPressed: () =>
                counter.Value++
        )
    ]
)
```

to mount into an existing `VisualElement`.

# Runtime Ownership Matrix

Ownership must be explicit before implementation begins.

| Resource | Created by | Owned by | Released / destroyed by |
|---|---|---|---|
| `Widget` description | Consumer / framework composition | Consumer / GC | GC |
| `WidgetNode` | LumaFlow | Parent node or root mount | LumaFlow lifecycle |
| Root `WidgetNode` | LumaFlow mount | `MountHandle` | `MountHandle.Dispose()` |
| Internal `VisualElement` | `WidgetNode` | `WidgetNode` | Node unmount/disposal |
| Mount host `VisualElement` | LumaFlow | Root mount | `MountHandle.Dispose()` |
| External mount root | Consumer / Unity | Consumer | Consumer |
| Borrowed `VisualElement` via `Native(...)` | Consumer | Consumer | Consumer |
| External `State<T>` | Consumer | Consumer | Consumer |
| State subscription | `WidgetNode` | `BindingScope` | `BindingScope.Dispose()` |
| Native event registration | `WidgetNode` | `BindingScope` / node | Node unmount |
| `BindingScope` | `WidgetNode` | `WidgetNode` | Node disposal |
| `BuildContext` | LumaFlow | Mounted scope/node | Scope/node lifecycle |
| `ThemeData` supplied externally | Consumer | Consumer/shared | Consumer / GC |
| Framework default `ThemeData` | Framework | Framework/shared | Framework / GC |
| Navigation entry | Navigator runtime | Navigator | Pop / replace / Navigator disposal |
| Route mounted subtree | Navigation entry | Navigation entry | Route entry disposal |
| Overlay entry | `OverlayHost` | `OverlayHost` | Close / host disposal |
| Overlay mounted subtree | Overlay entry | Overlay entry | Overlay entry disposal |
| List item binding scope | Recycled item host | Current item binding | Unbind/rebind/destroy |
| Editor event subscription | Editor integration object | Its lifecycle scope | Editor teardown |

Rules:

1. Creating a binding does not transfer ownership of the bound `State<T>`.
2. Mounting into a `VisualElement` does not transfer ownership of the external root.
3. `Native(existingElement)` borrows the element; it does not destroy it.
4. Parent nodes own child node lifecycle.
5. Root mount lifecycle is controlled through `MountHandle`.
6. Every event/subscription created during mount must have a deterministic cleanup owner.
7. A resource must never have two independent lifecycle owners.
8. Ownership transfer, when introduced by a future API, must be explicit in that API.
9. Static/global ownership must not be used as a substitute for unclear lifecycle.
10. If ownership is unclear during implementation, stop and resolve it before continuing.

---

# 2. Implementation Rules

During implementation:

1. Follow accepted ADRs.
2. Do not introduce abstractions only because they might be useful later.
3. Implement the smallest infrastructure required by the current vertical slice.
4. Keep Widget descriptions separate from mounted runtime objects.
5. Keep Runtime independent from `UnityEditor`.
6. Prefer native UI Toolkit behavior over custom replacements.
7. Add tests together with architecture-critical code.
8. Do not implement full reconciliation.
9. Do not implement source generation.
10. Do not implement navigation, overlays, virtualization, or advanced components before the runtime foundation is proven.

---

# 3. Initial Repository Structure

Start approximately with:

```text
Packages/
└── com.sahland.lumaflow/
    ├── package.json
    ├── README.md
    ├── CHANGELOG.md
    ├── LICENSE
    │
    ├── Runtime/
    │   ├── LumaFlow.Runtime.asmdef
    │   │
    │   ├── Core/
    │   ├── State/
    │   ├── Layout/
    │   ├── Styling/
    │   ├── Theme/
    │   └── Controls/
    │
    ├── Editor/
    │   └── LumaFlow.Editor.asmdef
    │
    └── Tests/
        ├── Runtime/
        │   └── LumaFlow.Runtime.Tests.asmdef
        │
        └── Editor/
            └── LumaFlow.Editor.Tests.asmdef
```

Do not create empty future directories unless implementation is about to use them.

---

# 4. Phase 0 — Package Foundation

## Goal

Create a valid UPM package that compiles before implementing framework behavior.

## Required Files

```text
package.json
LumaFlow.Runtime.asmdef
LumaFlow.Editor.asmdef
LumaFlow.Runtime.Tests.asmdef
LumaFlow.Editor.Tests.asmdef
README.md
LICENSE
CHANGELOG.md
```

## Requirements

`LumaFlow.Runtime`:

- player-compatible;
- no `UnityEditor` reference;
- references only required Unity runtime assemblies.

`LumaFlow.Editor`:

- Editor-only;
- depends on `LumaFlow.Runtime`.

Tests:

- Runtime tests depend on Runtime;
- Editor tests depend on Runtime + Editor.

## Exit Criteria

- package imports successfully;
- Runtime assembly compiles;
- Editor assembly compiles;
- Runtime has no Editor dependency;
- empty test assemblies run successfully.

---

# 5. Phase 1 — Core Mount Runtime

## Goal

Prove:

```text
Widget
→ WidgetNode
→ VisualElement
```

with deterministic mount and unmount behavior.

Do not implement styling, theme, layout libraries, or reactive state yet beyond what the runtime absolutely requires.

---

# 6. Implement `Widget`

Suggested location:

```text
Runtime/Core/Widget.cs
```

Conceptual responsibility:

```csharp
public abstract class Widget
{
    internal abstract WidgetNode CreateNode();
}
```

Exact API may differ.

`Widget` represents declarative configuration.

It must not contain:

- mounted `VisualElement`;
- parent node;
- BuildContext lifetime;
- BindingScope;
- mounted flags;
- runtime subscriptions.

---

# 7. Implement `WidgetNode`

Suggested location:

```text
Runtime/Core/Internal/WidgetNode.cs
```

Responsibilities:

- represent one mounted occurrence;
- track lifecycle;
- store parent/children;
- store current BuildContext;
- own binding/lifetime scope;
- attach/detach native UI;
- coordinate mount/unmount.

Conceptual lifecycle:

```text
Created
↓
Mounting
↓
Mounted
↓
Unmounting
↓
Disposed
```

Do not support node remount/reuse initially.

---

# 8. Implement Node Lifecycle State

Suggested:

```text
Runtime/Core/Internal/WidgetNodeState.cs
```

Possible internal enum:

```csharp
internal enum WidgetNodeState
{
    Created,
    Mounting,
    Mounted,
    Unmounting,
    Disposed
}
```

Transitions must be validated.

---

# 9. Implement `MountHandle`

Suggested:

```text
Runtime/Core/MountHandle.cs
```

Responsibility:

```text
external ownership handle
↓
root WidgetNode
```

Expected API shape:

```csharp
public sealed class MountHandle : IDisposable
{
    public bool IsMounted { get; }

    public void Dispose();
}
```

`Dispose()` should be idempotent.

The handle does not own the external root `VisualElement`.

---

# 10. Implement Root Mount Entry Point

Suggested:

```text
Runtime/Core/LumaFlow.cs
```

Conceptual:

```csharp
public static MountHandle Mount(
    Widget widget,
    VisualElement root
)
```

Responsibilities:

1. validate input;
2. create root runtime context;
3. create root node;
4. mount node;
5. return MountHandle;
6. rollback if mounting fails.

Do not call:

```csharp
root.Clear();
```

The root may contain unrelated native UI or another LumaFlow mount.

---

# 11. Root Native Container

The initial mount implementation may create one dedicated native host:

```text
external root
└── LumaFlow mount container
```

This simplifies ownership.

Each mount owns only its own container.

Disposing one mount removes only that container.

This behavior should remain implementation-internal unless exposed deliberately later.

---

# 12. Implement `BuildContext`

Suggested:

```text
Runtime/Core/BuildContext.cs
```

Initial implementation should be intentionally small.

At first it may only represent:

```text
mount-scoped context
```

without Theme or Navigator.

A minimal internal context scope may exist immediately so future Theme propagation does not require redesigning WidgetNode mounting.

Do not implement a generic service locator.

---

# 13. Implement `BindingScope`

Suggested:

```text
Runtime/Core/Internal/BindingScope.cs
```

Responsibilities:

- own node-bound cleanup actions;
- dispose subscriptions;
- dispose callbacks/adapters;
- be idempotent;
- continue best-effort cleanup where practical.

Possible minimal API:

```csharp
internal sealed class BindingScope : IDisposable
{
    public void Add(IDisposable disposable);

    public void Add(Action cleanup);

    public void Dispose();
}
```

Exact API may differ.

---

# 14. Implement First Native-Backed Widget: `Text`

Suggested files:

```text
Runtime/Controls/Text.cs
Runtime/Controls/Internal/TextNode.cs
```

Initial API:

```csharp
Text("Hello")
```

Native mapping:

```text
Text
↓
TextNode
↓
Label
```

The first implementation only needs static text.

No typography/theme integration yet.

---

# 15. Phase 1 Tests

Before moving forward, implement tests for:

```text
WidgetNode mount
WidgetNode unmount
double mount
double disposal
MountHandle disposal
parent-child cleanup
multiple mounts in same root
mount rollback
Text → Label
Text native detach
```

Phase 1 is not complete without rollback tests.

---

# 16. Phase 1 Exit Criteria

This should work:

```csharp
var mount = LumaFlow.Mount(
    new Text("Hello"),
    root
);
```

Native result:

```text
root
└── LumaFlow mount host
    └── Label("Hello")
```

Then:

```csharp
mount.Dispose();
```

returns the root to its previous state.

---

# 17. Phase 2 — Reactive State

## Goal

Prove granular native property updates without tree rebuilding.

Implement:

```text
State<T>
binding
Text bound to State<string>
```

---

# 18. Implement `State<T>`

Suggested:

```text
Runtime/State/State.cs
```

Conceptual:

```csharp
public sealed class State<T>
{
    public T Value { get; set; }

    public IDisposable Subscribe(
        Action<T> listener
    );
}
```

Requirements:

- use `EqualityComparer<T>.Default`;
- unchanged assignment should not notify by default;
- State owns subscriber registry;
- State does not know anything about Widgets;
- State does not depend on `VisualElement`;
- State does not require a third-party reactive library.

---

# 19. State Subscription Ownership

Binding:

```text
State<T>
↓ borrowed by Widget
↓ subscription created by WidgetNode
↓ subscription owned by BindingScope
```

Unmounting the node:

```text
disposes subscription
```

but does not dispose State.

---

# 20. Reactive `Text`

Extend Text to support reactive value binding.

Possible API:

```csharp
Text(
    value: name
)
```

and:

```csharp
Text(
    value: progress,
    format: value => $"{value:P0}"
)
```

The exact overload design should follow `API_DESIGN.md`.

---

# 21. Reactive Update Requirement

Given:

```text
TextNode
↓
Label
```

when State changes:

```text
same TextNode
same Label
new Label.text
```

There must be no subtree remount.

This test is critical.

---

# 22. Phase 2 Tests

Required:

```text
State initial value
State notification
State equality suppression
subscription disposal
multiple subscribers
Text State binding
Text update without remount
Text subscription removed on unmount
State survives UI unmount
```

---

# 23. Phase 3 — Button and Events

## Goal

Prove native event adaptation and lifecycle-safe callbacks.

Implement:

```text
Button
```

Native mapping:

```text
LumaFlow Button
↓
ButtonNode
↓
UnityEngine.UIElements.Button
```

Initial API:

```csharp
Button(
    "Save",
    onPressed: Save
)
```

---

# 24. Button Requirements

ButtonNode should own:

- native Button;
- native event registration;
- callback adaptation;
- BindingScope cleanup.

Unmounting Button must remove LumaFlow-owned callback registration.

---

# 25. Reentrant Event Requirement

This must be valid:

```text
Button callback
↓
disposes current mount
```

Framework code must not continue mutating the disposed node after callback returns.

Add an explicit test.

---

# 26. Phase 3 Tests

Required:

```text
Button → native Button
label configured
onPressed invoked
callback removed on unmount
self-unmount during callback
multiple Buttons independent
```

---

# 27. Phase 4 — Basic Layout

## Goal

Compose multiple widgets using native Flexbox.

Implement initial set:

```text
Column
Row
Padding
SizedBox
Expanded
```

Start with `Column`.

---

# 28. Implement `Column`

Suggested:

```text
Runtime/Layout/Column.cs
Runtime/Layout/Internal/ColumnNode.cs
```

Native mapping:

```text
Column
↓
VisualElement
↓
flex-direction: column
```

Support:

```text
children
gap
```

first.

Do not implement every alignment option immediately if the vertical slice does not need them.

---

# 29. Multi-Child Runtime Support

If child management was not sufficiently generalized during Phase 1, extract only the minimum reusable infrastructure now.

Potential internal abstractions:

```text
SingleChildNode
MultiChildNode
```

should only be introduced if repeated implementation proves useful.

Do not create them before that evidence exists.

---

# 30. Gap

Prefer native UI Toolkit gap support where compatibility allows.

If a fallback is required, isolate it.

Do not represent gap through permanent public `Spacer` insertion semantics.

---

# 31. First Complete Vertical Slice

At this point LumaFlow should support:

```csharp
var count = new State<int>(0);

var mount = LumaFlow.Mount(
    Column(
        gap: 12,
        children:
        [
            Text(
                value: count,
                format: value =>
                    $"Count: {value}"
            ),

            Button(
                "Increment",
                onPressed: () =>
                    count.Value++
            )
        ]
    ),
    root
);
```

Expected behavior:

```text
Button click
↓
State<int> changes
↓
Text Label updates
↓
Column remains mounted
↓
Button remains mounted
```

This is the first architectural milestone.

---

# 32. Vertical Slice Exit Criteria

Do not proceed until all of the following are true:

```text
Widget / WidgetNode separation works
MountHandle cleanup works
BindingScope cleanup works
State<T> works
Text updates without remount
Button events work
Column mounts children
multiple mounts are isolated
no root.Clear()
Runtime has no UnityEditor dependency
tests cover lifecycle
```

---

# 33. Phase 5 — Typed Styling Foundation

Only after the vertical slice is stable, implement:

```text
EdgeInsets
BorderRadius
BorderSide
Border
TextStyle
BoxDecoration
```

in small increments.

Do not implement an entire style system at once.

---

# 34. Initial Style Properties

Begin with:

```text
padding
margin
width
height
background
border radius
opacity
basic text style
```

because these are enough to dogfood real components.

---

# 35. Style Mapping

Introduce centralized mapping helpers.

Avoid property mapping logic duplicated in every WidgetNode.

Example conceptual structure:

```text
Styling/
├── EdgeInsets.cs
├── BorderRadius.cs
├── TextStyle.cs
└── Internal/
    ├── BoxStyleMapper.cs
    └── TextStyleMapper.cs
```

---

# 36. Unset Semantics

From the beginning, preserve the distinction between:

```text
unset
explicit value
```

Do not write every style property inline automatically.

Style clearing must be tested before update support is considered stable.

---

# 37. Phase 6 — Theme

Implement:

```text
ThemeData
Theme scope/provider
context.Theme
```

Start with:

```text
Colors
Spacing
Radius
Typography
```

Do not create every future theme group immediately.

---

# 38. Theme Provider

Theme should be a structural scope.

Conceptually:

```text
ThemeNode
↓
derived BuildContext
↓
child
```

No native wrapper should be created solely for context scoping.

---

# 39. Default Theme

A valid Theme must always exist.

Root context should provide:

```text
ThemeData.Default
```

when the application supplies none.

---

# 40. Phase 7 — TextField

Implement the first two-way controlled input.

This validates:

```text
State<T>
↔
native value control
```

Critical behavior:

```text
external State update
↓
SetValueWithoutNotify
↓
native field update
```

and:

```text
native user edit
↓
State update
```

without feedback loops.

---

# 41. Controlled Input First

Implement controlled:

```text
State<string>
```

before designing uncontrolled input APIs.

The controlled path is more important to validate the reactive architecture.

---

# 42. Phase 8 — Reactive Structural Boundary

Implement:

```text
ReactiveBuilder<T>
```

or equivalent.

Its semantics:

```text
State changes
↓
old local child subtree unmounted
↓
new local subtree mounted
```

No global reconciliation.

Use it for conditional UI.

---

# 43. Structural Rebuild Test

Example:

```csharp
ReactiveBuilder(
    state: isAdvanced,
    builder: value =>
        value
            ? AdvancedPanel()
            : BasicPanel()
)
```

Verify siblings outside the boundary remain mounted.

---

# 44. Phase 9 — Native Interop

Implement and prove:

```text
Native(existing VisualElement)
```

Requirements:

- borrowed ownership;
- reject existing parent;
- no silent reparent;
- detach on unmount;
- external element remains usable.

A factory overload may follow after real need.

---

# 45. Phase 10 — AudioLib Dogfooding Start

Do not wait until all LumaFlow features exist.

Begin migrating a small AudioLib surface when the following exist:

```text
Text
Button
TextField
Row
Column
Padding
Container
State<T>
Theme
Native(...)
```

Use dogfooding to expose missing APIs.

Do not add AudioLib-specific behavior to Core.

---

# 46. Feature Expansion Order

After the core slice, follow roughly:

```text
Container
Center
Align
Expanded
Spacer
SizedBox
Stack
Positioned

Toggle
Slider
Dropdown

ScrollView
ListView<T>

Overlay
Navigator

Editor-specific controls
Advanced theme/components
```

Exact ordering may change based on AudioLib needs.

---

# 47. ListView Gate

Do not implement ListView until:

```text
BindingScope
Widget lifecycle
State
native interop
multi-child mounting
```

are stable.

List recycling multiplies lifecycle mistakes.

---

# 48. Navigator Gate

Do not implement Navigator until:

```text
BuildContext scopes
structural nodes
mount/unmount
nested ownership
```

are proven.

---

# 49. Overlay Gate

Do not implement Overlay until:

```text
BuildContext scopes
root/subtree ownership
native layering
reentrant teardown
```

are stable.

---

# 50. Editor Integration Gate

Editor integration may begin early for AudioLib hosting, but Editor-specific controls should not drive Core architecture.

A minimal EditorWindow host may be implemented once mounting is stable.

---

# 51. First Editor Host

Conceptually:

```csharp
public abstract class LumaEditorWindow : EditorWindow
{
    private MountHandle? _mount;

    public void CreateGUI()
    {
        _mount = LumaFlow.Mount(
            Build(),
            rootVisualElement
        );
    }

    protected abstract Widget Build();
}
```

Do not stabilize this API until real AudioLib usage validates it.

---

# 52. Error Handling

Implement useful errors as infrastructure becomes available.

Initial high-value errors:

```text
WidgetNode already mounted
WidgetNode already disposed
borrowed VisualElement already parented
missing required child
invalid layout value
missing required context
```

Do not wait until the entire diagnostics system exists.

---

# 53. Diagnostics

Detailed Widget Inspector tooling is explicitly not part of Phase 1.

Runtime may expose minimal internal debug metadata:

```text
node ID
type
lifecycle state
native element
parent/children
```

only as implementation requires.

---

# 54. Performance Philosophy

Do not optimize before a correct vertical slice exists.

Initial priorities:

```text
correct lifecycle
correct cleanup
correct native mapping
clear public API
```

Then profile:

```text
allocations
mount cost
reactive update cost
list scrolling
```

---

# 55. Forbidden Early Optimizations

Do not initially implement:

```text
Widget pooling
WidgetNode pooling
global style caches
full reconciliation
automatic dependency tracking
source generation
runtime code generation
reflection-based binding
global registries
```

unless an accepted ADR is revised.

---

# 56. Test-First Architecture Areas

For the following systems, write the failing architectural test before or alongside implementation:

```text
mount rollback
subscription cleanup
style clearing
list recycling
controlled TextField feedback prevention
nested BuildContext scopes
navigation lifecycle
overlay host teardown
```

---

# 57. Definition of Done for a Runtime Primitive

A primitive is not complete until:

```text
public API exists
runtime behavior exists
cleanup exists
invalid configuration behavior exists
tests exist
Runtime/Editor boundary remains valid
```

A visually working component alone is insufficient.

---

# 58. Definition of Done for a Component

A standard component should have, where applicable:

```text
typed public API
native mapping
theme integration
state binding
event cleanup
style update semantics
tests
native escape hatch compatibility
```

Do not require all dimensions for components that do not use them.

---

# 59. Definition of Done for a New Subsystem

A subsystem such as Navigation or Overlay should include:

```text
runtime ownership model
BuildContext integration
failure behavior
cleanup behavior
tests
dogfood usage
documentation
```

before its API is called stable.

---

# 60. Codex Workflow

For every implementation task:

1. Read `AGENTS.md`.
2. Read relevant architecture documents.
3. Read relevant ADRs.
4. Inspect existing implementation before adding abstractions.
5. Inspect relevant tests.
6. Implement the smallest complete behavior.
7. Add/update tests.
8. Run targeted validation.
9. Run broader validation when dependencies/lifecycle changed.
10. Do not silently violate an ADR.

---

# 61. Codex Must Avoid Future Scaffolding

Do not create:

```text
INavigationService
IOverlayService
IThemeResolverFactory
IWidgetReconciler
```

before the corresponding implementation actually requires those abstractions.

Interfaces should emerge from real substitution/extensibility needs.

---

# 62. Codex Must Prefer Vertical Completion

Prefer:

```text
Text fully mounted, reactive, tested
```

over:

```text
20 empty Widget classes with TODOs
```

Depth before breadth.

---

# 63. First Milestone

The first major milestone is reached when this works reliably:

```csharp
var count = new State<int>(0);

using var mount = LumaFlow.Mount(
    Column(
        gap: 12,
        children:
        [
            Text(
                value: count,
                format: value =>
                    $"Count: {value}"
            ),

            Button(
                "Increment",
                onPressed: () =>
                    count.Value++
            )
        ]
    ),
    root
);
```

and tests prove:

```text
mount
native hierarchy
button callback
reactive text update
no rebuild
unmount
subscription cleanup
```

---

# 64. Second Milestone

The second milestone should prove a real themed form:

```text
Theme
Column
Text
TextField
Button
Padding
Container
State<T>
```

with controlled TextField input and no USS required for ordinary use.

---

# 65. Third Milestone

The third milestone should be a real AudioLib panel.

Not a synthetic demo.

This is where API ergonomics should be aggressively evaluated.

---

# 66. Implementation Stop Rule

If implementation reveals that an accepted ADR is fundamentally impractical:

```text
do not silently work around it
```

Instead:

1. document the conflict;
2. identify the smallest affected architectural decision;
3. amend/supersede the ADR;
4. then change implementation.

---

# 67. Final Direction

The implementation order is:

```text
package
↓
mount runtime
↓
Text
↓
State<T>
↓
Button
↓
Column
↓
first vertical slice
↓
styling
↓
theme
↓
inputs
↓
structural reactivity
↓
native interop
↓
AudioLib dogfooding
↓
larger systems
```

The guiding principle is:

**Prove the runtime architecture with the smallest real UI first.  
Then grow the framework around tested, dogfooded behavior.**
