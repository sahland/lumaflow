# ADR-004: Defer Full Reconciliation and Prefer Explicit Local Rebuild Boundaries

- **Status:** Superseded in part by ADR-021
- **Decision date:** 2026-08-11
- **Scope:** Widget updates, structural changes, identity, subtree rebuilding
- **Affects:** Runtime, WidgetNode, ReactiveBuilder, Lists, Lifecycle, Performance
- **Related documents:** `ARCHITECTURE.md`, `ADR-001-native-uitoolkit.md`, `ADR-003-reactive-state.md`

> **2026-08-20:** Dogfooding satisfied this ADR's reconciliation decision gate.
> ADR-021 now defines general local reconciliation and Widget key semantics.
> The preference for direct property updates and localized rebuild boundaries
> remains in force; the deferral of keys and compatible node reuse does not.

---

## 1. Context

Declarative UI frameworks commonly solve structural UI changes through a reconciliation system.

A typical model is:

```text
State changes
    ↓
Build()
    ↓
new widget tree
    ↓
compare with previous widget tree
    ↓
reuse compatible mounted nodes
    ↓
patch native UI
```

This model can be powerful.

However, it also introduces significant architectural complexity:

- widget identity;
- keys;
- diffing;
- child matching;
- node reuse rules;
- lifecycle transitions;
- list reconciliation;
- state retention;
- move detection;
- performance optimization;
- debugging complexity.

LumaFlow is built on top of Unity UI Toolkit, which already maintains a retained `VisualElement` hierarchy.

In addition, ADR-003 establishes localized reactive bindings as the primary state-update mechanism.

Therefore full reconciliation is not required for the initial architecture.

---

## 2. Decision

LumaFlow will **not implement full widget-tree reconciliation during the MVP and early framework stages**.

The preferred update strategy is:

```text
Property state change
        ↓
update affected native property directly
```

and for structural changes:

```text
State change
        ↓
explicit local rebuild boundary
        ↓
unmount old local subtree
        ↓
mount new local subtree
```

Full tree reconciliation is deferred until real-world usage demonstrates that it is necessary.

---

## 3. Default Update Hierarchy

LumaFlow should choose the smallest update mechanism capable of expressing the change.

Priority:

```text
1. Native property update
2. Native control state update
3. Local subtree rebuild
4. Limited reconciliation
5. Full reconciliation
```

Higher-cost mechanisms should only be introduced when lower-cost mechanisms are insufficient.

---

## 4. Property Updates

If a state change affects only a native property, update that property directly.

Example:

```text
State<string>
    ↓
Label.text
```

Do not:

```text
State changes
↓
rebuild Text widget
↓
replace Label
```

when assigning `Label.text` is sufficient.

---

## 5. Native Control Updates

Interactive controls should also update in place.

Example:

```text
State<bool>
    ↓
Button.SetEnabled(...)
```

or:

```text
State<float>
    ↓
Slider value
```

The existing mounted node remains alive.

---

## 6. Structural Changes

Some updates cannot be represented as a property mutation.

Example:

```csharp
loading
    ? ProgressIndicator()
    : ResultsView()
```

The active child type changes.

For these cases LumaFlow should use an explicit structural rebuild boundary.

Potential public concept:

```text
ReactiveBuilder<T>
```

or another equivalent abstraction.

---

## 7. Local Rebuild Boundary

A local rebuild boundary owns a dynamic subtree.

Conceptually:

```text
Static Parent
├── Static Header
├── Static Toolbar
└── Dynamic Boundary
      └── Current Child
```

When the bound state changes:

```text
Dynamic Boundary
    ↓
unmount current child
    ↓
evaluate builder
    ↓
mount replacement child
```

The surrounding tree remains untouched.

---

## 8. Example

Application:

```csharp
ReactiveBuilder(
    state: loading,
    builder: isLoading =>
        isLoading
            ? ProgressIndicator()
            : ResultsView()
)
```

Initial state:

```text
ReactiveBuilderNode
└── ProgressIndicatorNode
```

After update:

```text
ReactiveBuilderNode
└── ResultsViewNode
```

The parent of `ReactiveBuilderNode` remains mounted.

---

## 9. Why Explicit Boundaries

Explicit rebuild boundaries provide:

- predictable update scope;
- simple lifecycle semantics;
- easier debugging;
- limited allocation cost;
- no need for global diffing;
- clear subscription ownership.

They also make performance easier to reason about.

---

## 10. Stable Parent Identity

A rebuild boundary itself should remain mounted while replacing its child subtree.

This preserves:

- parent hierarchy;
- surrounding focus state where possible;
- contextual ownership;
- binding scope outside the subtree.

---

## 11. No Root Rebuild by Default

The following architecture is forbidden as the default reactive path:

```text
any State changes
    ↓
Application.Build()
    ↓
new entire Widget tree
```

LumaFlow should not recreate or diff unrelated UI for a local state change.

---

## 12. No Hidden Automatic Rebuilds

A widget should not unexpectedly trigger rebuilding of arbitrary ancestors.

Reactive behavior should have understandable ownership.

If a component creates a structural dependency, it should establish or use an explicit rebuild boundary.

---

## 13. StatelessView Rebuild Semantics

`StatelessView.Build()` does not imply that it is automatically called on every state mutation.

A `StatelessView` primarily describes a composable subtree.

Its `Build()` method is invoked when that subtree is mounted or deliberately rebuilt.

This differs from frameworks where every dependency change automatically rebuilds the view.

---

## 14. StatefulView Rebuild Semantics

Likewise, `StatefulView` does not automatically mean:

```text
setState()
↓
rebuild entire view subtree
```

unless a future API explicitly introduces such semantics.

LumaFlow's preferred model remains explicit reactive bindings.

---

## 15. Structural Builder Types

Potential structural reactive primitives include:

```text
ReactiveBuilder<T>
Conditional
Switch
AsyncBuilder
```

These must all share a clear model:

```text
stable boundary
+
replaceable child subtree
```

Do not implement each with unrelated lifecycle behavior.

---

## 16. Conditional

Potential future API:

```csharp
Conditional(
    condition: hasSelection,
    whenTrue: selection => Inspector(selection),
    whenFalse: EmptyState()
)
```

This should internally use the same structural rebuild mechanism rather than inventing a separate engine.

---

## 17. Structural Rebuild Lifecycle

Rebuilding a subtree must follow normal lifecycle.

Sequence:

```text
old subtree
    ↓
Unmount()
    ↓
dispose bindings/events
    ↓
detach native hierarchy
    ↓
build replacement Widget
    ↓
mount replacement WidgetNode
```

No old subscriptions may survive.

---

## 18. State Preservation

Local subtree replacement normally destroys the runtime state associated with the replaced subtree.

Example:

```text
LoginView
↓ replaced by
DashboardView
```

The old LoginView local mounted state is removed.

This is expected.

If state must survive structural replacement, it should be owned outside the replaced subtree.

---

## 19. State Hoisting

LumaFlow should encourage state ownership at an appropriate stable level.

Example:

```text
Parent State<User>
        ↓
ReactiveBuilder
        ↓
temporary child views
```

State that needs to survive child replacement should generally be hoisted above the rebuild boundary.

---

## 20. Keys Are Deferred

Widget keys are primarily useful when preserving identity across reconciliation.

Since full reconciliation is deferred, keys are also deferred as a general public concept.

Do not add:

```csharp
key: Key(...)
```

to every Widget API during MVP.

---

## 21. When Keys May Become Necessary

Keys may become useful for:

- reordered dynamic children;
- reconciliation;
- list item identity;
- preserving local mounted state across moves;
- stateful repeated components.

At that point, a dedicated identity ADR should define semantics.

---

## 22. Lists Are a Special Case

Large lists should not be treated as ordinary reconciliation problems by default.

Prefer native UI Toolkit virtualization.

Conceptually:

```text
LumaFlow ListView<T>
        ↓
UI Toolkit ListView
```

Item recycling already provides a specialized native update model.

---

## 23. Native List Recycling

Virtualized list elements may be reused.

LumaFlow must adapt lifecycle behavior to this model.

Conceptually:

```text
native recycled item root
        ↓
bind new item
        ↓
update/mount LumaFlow item content
```

This may require limited specialized node reuse.

That does not automatically justify global reconciliation.

---

## 24. Limited Reconciliation

A future intermediate strategy may introduce reconciliation only inside specific components.

Examples:

```text
DynamicChildren
List
Stack with dynamic children
```

This may be preferable to a global framework-wide reconciler.

Any such implementation must clearly define its scope.

---

## 25. Reconciliation Decision Gate

Full reconciliation may only be introduced if real usage demonstrates recurring problems such as:

1. frequent structural rebuilds causing visible performance issues;
2. local state repeatedly being lost where developers reasonably expect preservation;
3. dynamic child collections requiring efficient identity-based updates;
4. public API becoming awkward without declarative rebuild semantics;
5. too many specialized structural widgets duplicating the same diff logic.

A theoretical belief that declarative frameworks "should have reconciliation" is not sufficient.

---

## 26. Required Evidence

Before introducing full reconciliation, gather evidence from:

```text
dogfooding
profiling
real user APIs
large UI samples
dynamic lists
Editor tools
runtime screens
```

Document the observed problem.

---

## 27. ADR Requirement

Full reconciliation requires a new ADR.

That ADR must define at minimum:

- node identity;
- compatibility rules;
- key semantics;
- child matching;
- lifecycle during reuse;
- state preservation;
- list behavior;
- moved children;
- performance model;
- failure behavior;
- debugging strategy.

Do not add reconciliation incrementally without defining these semantics.

---

## 28. Potential Compatibility Rule

If future reconciliation is introduced, a likely first rule is:

```text
same widget runtime type
+
compatible position/key
=
candidate for node reuse
```

This is only a possible future direction.

It is not yet accepted.

---

## 29. Widget Identity

Current architecture treats Widget instances as descriptions, not persistent identity objects.

Therefore object reference equality should not become an implicit reconciliation rule.

Example:

```csharp
new Text("Hello")
```

can describe equivalent UI to another `Text("Hello")`.

Mounted identity belongs to runtime nodes.

---

## 30. No Equals-Based Widget Diffing Yet

Do not add extensive `Equals()` implementations to all Widgets in anticipation of reconciliation.

This would introduce maintenance complexity without current value.

Style/value objects may still have equality for their own semantics.

---

## 31. Diff Cost

Any future reconciliation system must avoid naïvely traversing huge unchanged trees for small state updates.

If introduced, it should be used only where its declarative benefits outweigh update cost.

---

## 32. Structural Builder Allocation

Local rebuild boundaries may allocate new Widget descriptions when rebuilt.

This is acceptable initially.

Example:

```text
loading changes
↓
builder invoked
↓
small new subtree allocated
```

This cost is bounded by the rebuild scope.

---

## 33. Wrapper Cost

Do not create rebuild boundaries around every Widget "just in case."

A rebuild boundary should exist only where structural reactivity is needed.

Bad:

```text
ReactiveBuilder
└── ReactiveBuilder
    └── ReactiveBuilder
        └── Text
```

without actual structural dependencies.

---

## 34. User Intent Should Be Visible

Application code should make expensive structural reactivity reasonably apparent.

Example:

```csharp
ReactiveBuilder(...)
```

communicates that the enclosed subtree can be replaced.

This is preferable to invisible broad rebuild behavior.

---

## 35. Focus Behavior

Replacing a subtree may affect focus.

If the currently focused element belongs to the removed subtree, losing focus is expected unless the replacement explicitly restores it.

Do not attempt magical focus preservation before concrete UX requirements exist.

---

## 36. Scroll Behavior

A local subtree rebuild may also reset scroll position if the scroll element itself is replaced.

Therefore stable scroll containers should normally exist outside dynamic boundaries when scroll preservation is desired.

Example:

```text
ScrollView
└── ReactiveBuilder
```

instead of:

```text
ReactiveBuilder
└── ScrollView
```

when scroll state should persist.

---

## 37. Native Element Reuse

Direct property bindings naturally reuse native elements.

This is the preferred reuse mechanism.

Example:

```text
TextNode
    ↓ remains mounted
Label
    ↓ remains same instance
Label.text changes
```

No reconciler is needed.

---

## 38. Component Internal Updates

A complex component may internally choose efficient native mutations instead of rebuilding itself.

Example:

```text
Tabs
```

might keep tab buttons mounted and only change active styling/content.

Component implementations are allowed to optimize locally.

---

## 39. Component Rebuild Boundaries

Composite components may use internal local rebuild boundaries when structural child content changes.

These boundaries should use the same core infrastructure as public reactive builders.

Avoid separate bespoke subtree replacement code.

---

## 40. Error Handling During Rebuild

If a structural builder throws:

- the exception should be surfaced clearly;
- the runtime should avoid leaving half-mounted state where possible;
- cleanup of the old subtree should be deterministic.

Exact transaction semantics can be refined during implementation.

---

## 41. Build Failure Strategy

A possible implementation strategy:

```text
evaluate new Widget first
↓
if successful
    unmount old
    mount new
```

This may preserve old content if building the new description fails.

However, mounting itself may still fail.

The exact strategy should prioritize consistency and useful diagnostics.

---

## 42. Reentrant Rebuilds

A builder may theoretically trigger state changes.

The implementation should avoid corrupting the subtree lifecycle.

Do not introduce a sophisticated scheduler unless required.

Development-time guards against recursive rebuild corruption may be appropriate.

---

## 43. Batch Structural Changes

No explicit structural batching system is required initially.

If multiple dependent State values cause repeated local rebuilds in practice, batching can be considered later.

---

## 44. Animation and Rebuilds

Future transition widgets may animate old/new subtree replacement.

Example:

```text
AnimatedSwitcher
```

This would intentionally retain old content temporarily during transition.

That is a higher-level feature and does not change the core reconciliation decision.

---

## 45. Overlay Rebuilds

Dialogs, menus, and overlays may be inserted or removed as whole mounted subtrees.

This is structurally similar to explicit mount/unmount rather than global reconciliation.

---

## 46. Navigation

Navigation also typically replaces or stacks whole screen subtrees.

It should use explicit lifecycle/mounting rules rather than relying on full global reconciliation.

---

## 47. Editor UI

Editor tools often contain dynamic inspectors and selections.

Use local rebuild boundaries for sections that structurally depend on selected data.

Do not rebuild the entire EditorWindow unless necessary.

---

## 48. Example: Inspector Selection

Conceptual:

```csharp
Column(
    children:
    [
        Toolbar(),

        ReactiveBuilder(
            state: selectedItem,
            builder: item =>
                item is null
                    ? EmptyInspector()
                    : ItemInspector(item)
        )
    ]
)
```

Changing selection only replaces the inspector subtree.

---

## 49. Performance Model

The desired default performance relationship is:

```text
simple property update cost
≈ affected bindings
```

and:

```text
structural update cost
≈ affected subtree size
```

not:

```text
update cost
≈ entire application widget tree size
```

---

## 50. Debugging Model

A developer should be able to reason:

```text
Why did this subtree rebuild?
```

Answer:

```text
Because this explicit ReactiveBuilder depends on State X.
```

This is preferable to hidden framework-wide dependency invalidation.

---

## 51. Instrumentation

Future debug tooling may expose:

```text
rebuild boundary
last rebuild cause
rebuild count
subtree size
```

This is particularly useful if structural rebuilding becomes frequent.

---

## 52. Rejected Alternative: Full Flutter-Like Rebuild Model from Day One

Rejected:

```text
State changes
↓
Build()
↓
reconcile everything
```

Reasons:

- premature complexity;
- requires keys early;
- forces node identity design;
- higher allocations;
- duplicates retained UI Toolkit strengths;
- unnecessary for basic reactive properties.

---

## 53. Rejected Alternative: No Structural Reactivity

Also rejected:

```text
Only direct property bindings allowed forever.
```

Reason:

Some UI changes genuinely alter hierarchy.

LumaFlow needs a first-class structural update mechanism.

The chosen solution is explicit local rebuild boundaries.

---

## 54. Rejected Alternative: Manual Child Replacement

Rejected as the application-facing model:

```csharp
container.Clear();
container.Add(newView);
```

Reason:

- leaks implementation details;
- manual lifecycle;
- manual subscription cleanup;
- breaks declarative composition.

LumaFlow runtime should own subtree replacement.

---

## 55. Rejected Alternative: Diff VisualElements Directly

Potential idea:

```text
old VisualElement tree
vs
new VisualElement tree
```

is rejected as the primary reconciliation model.

Reasons:

- high-level Widget semantics are lost;
- state ownership becomes unclear;
- native elements are implementation targets, not declarative descriptions;
- user-defined view identity becomes difficult.

If reconciliation exists later, it should primarily reason about LumaFlow runtime/widget concepts.

---

## 56. Architectural Invariants Created by This ADR

Unless superseded:

### Invariant 1

Full reconciliation is not part of MVP.

### Invariant 2

Property updates use direct native mutations.

### Invariant 3

Structural reactivity uses explicit local rebuild boundaries.

### Invariant 4

Surrounding unrelated UI remains mounted.

### Invariant 5

Replaced subtree lifecycle is deterministic.

### Invariant 6

Keys are not required until identity-based reconciliation is introduced.

### Invariant 7

Full reconciliation requires a new ADR.

### Invariant 8

Native virtualization is preferred over general reconciliation for large lists.

---

## 57. Codex Rules

### Rule 1

Do not create a global Reconciler during MVP.

### Rule 2

Do not add `Key` to base Widget solely in anticipation of future reconciliation.

### Rule 3

When only a native property changes, update it in place.

### Rule 4

When hierarchy changes, prefer the smallest explicit rebuild boundary.

### Rule 5

Do not rebuild unrelated ancestors.

### Rule 6

Use normal Unmount/Mount lifecycle for replaced subtrees.

### Rule 7

Do not implement bespoke subtree replacement in every component; reuse shared structural infrastructure.

### Rule 8

Do not create complex widget equality logic before reconciliation exists.

### Rule 9

Use UI Toolkit virtualization for large lists where possible.

### Rule 10

If full reconciliation appears necessary, stop and require architectural review.

---

## 58. Decision Test

When a UI update is required:

```text
Can one native property change?
        ↓ yes
Update directly.

Does one local child subtree change?
        ↓ yes
Use local rebuild boundary.

Are many sibling identities changing dynamically?
        ↓ maybe
Evaluate limited reconciliation.

Does this repeatedly affect large arbitrary trees?
        ↓ yes
Collect evidence and propose reconciliation ADR.
```

---

## 59. Initial Structural API Target

Conceptually:

```csharp
ReactiveBuilder(
    state: selectedClip,
    builder: clip =>
        clip is null
            ? EmptySelection()
            : ClipInspector(clip)
)
```

This should be sufficient for many dynamic layouts without introducing global reconciliation.

---

## 60. Future Evaluation Questions

During dogfooding, measure:

1. How often are structural builders required?
2. How large are rebuilt subtrees?
3. Is local state loss a recurring problem?
4. Do users need dynamic keyed sibling collections?
5. Are rebuild allocations measurable?
6. Does the explicit API become verbose?
7. Does UI Toolkit virtualization solve list cases?
8. Would reconciliation simplify consumer code enough to justify its cost?

These answers determine whether this ADR remains sufficient.

---

## 61. Reconsideration Conditions

Revisit this decision when:

- Phase 12 of `ROADMAP.md` is reached;
- significant structural performance problems appear;
- dynamic composition becomes awkward;
- state-preserving child movement becomes necessary;
- user APIs increasingly emulate reconciliation manually.

Any replacement decision must supersede this ADR explicitly.

---

## 62. Final Decision

LumaFlow will begin with a deliberately simple retained-mode update strategy:

```text
Reactive State
    ↓
Direct native updates where possible
    ↓
Explicit local subtree rebuild where necessary
```

It will not begin with:

```text
global Build
+
virtual widget diff
+
full reconciliation
```

The framework will add reconciliation only after real usage proves that the additional architectural complexity creates more value than cost.
