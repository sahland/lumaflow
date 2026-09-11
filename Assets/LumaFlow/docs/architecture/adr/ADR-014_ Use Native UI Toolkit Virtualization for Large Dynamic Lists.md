# ADR-014: Use Native UI Toolkit Virtualization for Large Dynamic Lists

- **Status:** Accepted
- **Decision date:** 2026-08-11
- **Scope:** Lists, virtualization, recycling, item binding, collection updates
- **Affects:** Runtime, ListView<T>, WidgetNode lifecycle, State bindings, Performance, Editor, Runtime UI
- **Related documents:** `ADR-001-native-uitoolkit.md`, `ADR-003-reactive-state.md`, `ADR-004-reconciliation-strategy.md`, `ADR-007-widget-runtime-model.md`, `ADR-008-lifecycle-and-ownership.md`

---

## 1. Context

LumaFlow will frequently be used to display collections.

Examples include:

```text
audio clips
project assets
inventory items
settings entries
logs
search results
file lists
editor data
player lists
leaderboards
```

A naïve declarative implementation could render a collection as:

```csharp
Column(
    children: items
        .Select(item => ItemCard(item))
        .ToArray()
)
```

For small collections, this may be perfectly acceptable.

For large or frequently changing collections, however, this creates:

```text
one Widget
+
one WidgetNode
+
one or more VisualElements
```

for every item.

A list containing thousands of entries could therefore create thousands of mounted UI objects even though only a small visible range is on screen.

Unity UI Toolkit already provides specialized list controls with virtualization and recycling behavior.

LumaFlow should use that capability instead of recreating large-list rendering through ordinary Widget trees.

---

## 2. Decision

LumaFlow will provide a typed `ListView<T>` abstraction backed by native UI Toolkit list virtualization wherever practical.

The intended architecture is:

```text
IReadOnlyList<T>
        ↓
LumaFlow ListView<T>
        ↓
item adapter / binding layer
        ↓
Unity UI Toolkit ListView
        ↓
native virtualization and recycling
```

LumaFlow will not render large lists by permanently mounting one normal Widget subtree per collection item.

---

## 3. Core Principle

For ordinary repeated collections:

```text
small collection
→ normal declarative composition may be acceptable

large / scrolling / dynamic collection
→ ListView<T>
→ native virtualization
```

The user should not need to manually implement recycling.

---

## 4. Why Native Virtualization

Native UI Toolkit list infrastructure already solves important problems such as:

```text
visible-range management
native scrolling
item recycling
selection
scroll position
large collection handling
native event integration
```

Reimplementing these capabilities would violate the general architectural principle from ADR-001:

```text
if UI Toolkit already solves it,
wrap it instead of replacing it.
```

---

## 5. Public API Goal

The target API should remain declarative and typed.

Conceptually:

```csharp
ListView(
    items: clips,
    itemBuilder: clip =>
        AudioClipRow(
            clip: clip
        )
)
```

or:

```csharp
ListView<AudioClip>(
    items: clips,
    itemBuilder: (context, clip) =>
        AudioClipRow(
            clip: clip
        )
)
```

The exact syntax may evolve.

The public API should hide native `makeItem` / `bindItem` complexity for common usage.

---

## 6. Native Backing

Conceptually:

```text
LumaFlow ListView<T>
        ↓
ListViewNode<T>
        ↓
UnityEngine.UIElements.ListView
```

The native `ListView` remains responsible for:

```text
scrolling
visible-range calculation
item reuse
native selection
native list hierarchy
```

---

## 7. Virtualized Item Lifecycle

A virtualized list item does not have ordinary permanent Widget lifecycle.

Instead, one native row may be reused.

Conceptually:

```text
native row instance
    ↓
bind item A
    ↓
visible
    ↓
unbind item A
    ↓
bind item B
    ↓
visible
```

This is different from:

```text
mount once
↓
remain associated with same model forever
↓
unmount once
```

Therefore list item lifecycle requires a specialized adapter.

---

## 8. Item Host

Each native recycled list slot should conceptually have an item host.

Example:

```text
Native ListView
└── RecycledItemHost
    └── current LumaFlow item subtree
```

The host remains reusable.

The currently bound item content may change.

---

## 9. Item Binding Scope

Every item host must own an item-specific lifetime scope.

Conceptually:

```text
ItemHost
├── native root
├── current item
├── item BindingScope
└── current item subtree
```

Before rebinding:

```text
dispose old item bindings
↓
remove/replace old item association
↓
bind new item
```

No subscription from item A may survive when the slot represents item B.

---

## 10. Critical Recycling Rule

This is forbidden:

```text
row initially bound to item A
↓
State subscription to A remains
↓
row recycled for item B
↓
A changes
↓
row displaying B updates from A
```

This is a severe correctness bug.

Item-specific subscriptions must be destroyed or rebound before reuse.

---

## 11. Native makeItem / bindItem Model

Internally, LumaFlow may adapt native semantics similar to:

```text
makeItem
→ create reusable native host

bindItem
→ bind host to collection index/item

unbindItem
→ clear item-specific bindings

destroyItem
→ final host cleanup
```

Exact API usage depends on supported Unity versions.

The LumaFlow abstraction must preserve equivalent lifecycle semantics.

---

## 12. makeItem Responsibility

The native host creation phase should create only reusable infrastructure.

Conceptually:

```text
create host VisualElement
create host runtime object
prepare reusable mounting container
```

It should not permanently capture one collection item.

---

## 13. bindItem Responsibility

Binding should associate the host with the current item.

Conceptually:

```text
host
+
item[index]
        ↓
build/bind current item UI
```

All item-specific subscriptions belong to the binding lifetime.

---

## 14. unbindItem Responsibility

Unbinding must release:

```text
item State subscriptions
item-specific callbacks
item-specific context adapters
temporary item references
```

before another item is bound.

---

## 15. destroyItem Responsibility

When the native list permanently destroys a recycled host:

```text
dispose active item binding
↓
unmount active item subtree
↓
dispose host-level lifetime
↓
release native references
```

This follows ADR-008.

---

## 16. Item Widget Strategy

There are two possible implementation strategies.

### Strategy A — Rebuild local item subtree on rebind

```text
host bound to A
↓
mount Widget(A)

recycled
↓
unmount Widget(A)
↓
mount Widget(B)
```

### Strategy B — Reuse compatible item WidgetNode

```text
host bound to A
↓
WidgetNode

recycled
↓
WidgetNode.Update(B)
```

Strategy A is simpler and should be preferred initially unless profiling proves it too expensive.

---

## 17. No Global Reconciliation Requirement

List recycling does not require global Widget-tree reconciliation.

A list may use specialized local node reuse while ADR-004 still defers application-wide reconciliation.

This is an explicit exception boundary.

---

## 18. Initial Item Rebind Strategy

The initial implementation should prefer:

```text
unbind old item subtree
↓
mount new local subtree
```

if performance is acceptable.

This provides:

```text
simple ownership
predictable subscriptions
easy correctness
```

before introducing item-level reconciliation.

---

## 19. Reuse Optimization Gate

Item WidgetNode reuse should only be added if profiling demonstrates meaningful benefit.

Before implementing reuse, define:

```text
compatible item widget types
state retention rules
callback replacement
style update rules
child update rules
```

Do not casually mutate arbitrary old subtrees into new item descriptions.

---

## 20. Collection Source

Initial `ListView<T>` should accept a simple typed collection.

Potential:

```csharp
IReadOnlyList<T>
```

or another collection interface suitable for native UI Toolkit.

Avoid requiring models to implement LumaFlow-specific interfaces.

---

## 21. Collection Ownership

The collection is externally owned by default.

Unmounting ListView must not:

```text
clear collection
dispose models
destroy ScriptableObjects
```

LumaFlow owns only list UI lifecycle.

---

## 22. Mutable Collections

Collection mutation requires an explicit refresh/update strategy.

Examples:

```text
item added
item removed
item replaced
collection reordered
```

LumaFlow must not assume `IReadOnlyList<T>` itself can notify changes.

---

## 23. Initial Refresh Model

MVP may support explicit collection refresh.

Conceptually:

```csharp
listController.Refresh();
```

or a reactive collection state replacement:

```csharp
itemsState.Value = newItems;
```

Exact API is deferred.

Do not introduce a complex observable collection system before needed.

---

## 24. State<IReadOnlyList<T>>

A simple reactive pattern may be:

```csharp
State<IReadOnlyList<AudioClip>> Clips;
```

Then:

```text
Clips.Value changes
↓
ListView updates collection source
↓
native list refreshes
```

In-place mutation remains a separate concern under ADR-003.

---

## 25. Observable Collection Future

Future abstractions may include:

```text
ObservableList<T>
StateList<T>
CollectionChange<T>
```

if real usage shows that replacing whole collection references is insufficient.

This is explicitly deferred.

---

## 26. Collection Update Granularity

Long-term, efficient updates may distinguish:

```text
insert
remove
replace
move
reset
```

rather than treating every mutation as:

```text
refresh entire collection
```

Do not implement this until native APIs and real use cases justify it.

---

## 27. Item Identity

Index is not always stable semantic identity.

Example:

```text
index 4
```

may represent one item before insertion and another after insertion.

Therefore item identity may eventually require a typed key selector.

Potential:

```csharp
itemKey: clip => clip.Guid
```

---

## 28. Keys Are List-Scoped First

ADR-004 defers general Widget keys.

List item identity is a specialized use case and may introduce list-scoped keys before a global Widget `Key` API exists.

This must not automatically leak into every Widget.

---

## 29. When Item Keys Are Needed

Keys become important for:

```text
selection preservation
reordering
local item state preservation
animated moves
stable item tracking
incremental collection updates
```

MVP may initially operate on native index/item identity where sufficient.

---

## 30. Key Requirements

If keys are introduced, they must be:

```text
stable
unique within the current collection
cheap to compare
```

Duplicate keys should produce a clear diagnostic.

---

## 31. Item Local State

A virtualized item must not assume its mounted node remains associated with one model forever.

Therefore mounted-local state inside an item requires careful semantics.

Example:

```text
row expanded/collapsed state
```

If that state must survive recycling, it should generally be stored outside the recycled row and keyed by item identity.

---

## 32. State Hoisting in Lists

Preferred:

```text
model/item state
        ↓
externally owned
        ↓
recycled item UI binds to it
```

rather than:

```text
recycled row owns semantic item state
```

for information that should persist when the item scrolls off screen.

---

## 33. Ephemeral Row State

Purely visual temporary state may remain host-local if losing it during recycling is acceptable.

Examples might include:

```text
temporary hover visuals
native pressed state
```

but semantic model state should not depend on slot lifetime.

---

## 34. Selection

List selection should prefer native UI Toolkit capabilities.

LumaFlow may expose semantic typed configuration.

Potential:

```csharp
selectionMode: SelectionMode.Single
```

and:

```csharp
selectedItem: selectedClip
```

Exact API is deferred.

---

## 35. Selected Item State

A typed API should preferably expose model values rather than forcing application code to work with native indices.

Example:

```text
State<AudioClip?> selectedClip
```

where practical.

---

## 36. Multiple Selection

Future support may use:

```text
IReadOnlyCollection<T>
```

or item keys.

Do not design multiple-selection API until native semantics and common use cases are tested.

---

## 37. Selection Must Survive Recycling

Recycling visual rows must not alter logical selection.

Selection belongs to list/model state, not the current row object.

---

## 38. Scroll Position

Scroll position should remain native UI Toolkit state.

Rebinding visible rows must not reset the entire native ListView scroll offset.

---

## 39. Collection Replacement and Scroll

Replacing the complete collection may preserve or reset scroll position depending on explicit semantics and native behavior.

This must be documented.

Do not silently invent behavior different from UI Toolkit unless needed.

---

## 40. Fixed vs Dynamic Item Height

Native UI Toolkit list controls may support different virtualization modes depending on Unity version/configuration.

LumaFlow should expose only what it can support predictably.

Potential options:

```text
fixed item height
dynamic item height
```

should map directly to native capabilities.

---

## 41. Fixed Height Optimization

If a list uses uniform rows, fixed item height may provide better performance.

LumaFlow may allow:

```csharp
itemHeight: 36
```

or equivalent.

Do not require fixed height for every list unless native compatibility requires it.

---

## 42. Theme and Item Height

Standard component row height may come from theme.

Example:

```text
ListTheme.ItemHeight
```

where the list uses a standard style.

Application-specific lists may override it explicitly.

---

## 43. Dynamic Height

Dynamic-height rows should only be enabled when native UI Toolkit supports them reliably in the declared compatibility range.

If unsupported, LumaFlow must not fake it through a custom entire list engine.

---

## 44. Item Builder

The item builder should receive the typed model.

Preferred conceptual API:

```csharp
itemBuilder: item =>
    AudioClipRow(
        clip: item
    )
```

Potentially BuildContext is available through normal View composition and need not be passed explicitly if the builder runs in a scoped context.

---

## 45. Index Parameter

Some users need the current index.

Potential:

```csharp
itemBuilder: (item, index) => ...
```

This may be offered if common.

Do not make index mandatory.

---

## 46. Index Is Not Identity

Documentation must state:

```text
index
≠
stable item identity
```

Do not store long-lived semantic state keyed only by index if collection order can change.

---

## 47. Builder Side Effects

As with `Build()`, an item builder may run multiple times.

Do not put permanent side effects inside it.

Bad:

```csharp
itemBuilder: item =>
{
    analytics.TrackVisible(item);
    return ItemRow(item);
}
```

unless repeated invocation is explicitly intended.

---

## 48. Visibility Events

If users need:

```text
item became visible
item left visible range
```

that should be represented by dedicated future APIs rather than relying on itemBuilder side effects.

---

## 49. List Empty State

An empty collection often needs alternate content.

Potential API:

```csharp
ListView(
    items: items,
    itemBuilder: ...,
    empty: EmptyState()
)
```

or composition outside:

```csharp
ReactiveBuilder(
    state: items,
    builder: value =>
        value.Count == 0
            ? EmptyState()
            : ListView(...)
)
```

The latter may be enough initially.

Do not overload ListView prematurely.

---

## 50. Loading State

Likewise, loading belongs to application composition rather than native list mechanics.

Example:

```text
loading
→ ProgressIndicator

loaded empty
→ EmptyState

loaded non-empty
→ ListView
```

---

## 51. Headers and Footers

Headers/footers may be composed outside native list initially.

Example:

```csharp
Column(
    children:
    [
        Header(),

        Expanded(
            child: ListView(...)
        )
    ]
)
```

Only add dedicated header/footer APIs if they materially improve scrolling semantics.

---

## 52. Sectioned Lists

Grouped/sectioned lists are a future higher-level component.

Do not complicate base `ListView<T>` with sections during MVP.

---

## 53. Grid

Grid virtualization is a separate problem.

Do not pretend `ListView<T>` automatically solves virtualized grids.

A future:

```text
GridView<T>
```

may adapt appropriate UI Toolkit behavior if available.

---

## 54. Tree Views

Hierarchical data should use a dedicated native-backed abstraction.

Potential:

```text
TreeView<T>
```

Do not emulate hierarchy by flattening every use case into ListView without reason.

---

## 55. Editor List Use Cases

Editor tooling is a major target.

Examples:

```text
AudioLib asset list
package browser
build logs
tool registries
scene objects
```

These can contain hundreds or thousands of entries.

Virtualization is therefore a core practical requirement, not an optional optimization.

---

## 56. Runtime List Use Cases

Runtime examples include:

```text
inventory
leaderboard
server browser
quest list
chat history
save files
```

The same typed `ListView<T>` semantics should apply where UI Toolkit supports them.

---

## 57. Runtime and Editor API Consistency

The public list abstraction should be shared between Runtime and Editor where possible.

Editor-only native data-source features may be layered separately.

---

## 58. Drag Reordering

Native reorderable ListView behavior may be exposed later.

Potential:

```csharp
reorderable: true
```

But model ownership and collection mutation semantics must be clear first.

---

## 59. Reordering Contract

If users reorder rows, LumaFlow must know whether it:

```text
mutates supplied collection
emits reorder callback
updates State collection
```

Do not implicitly mutate arbitrary externally supplied collections.

---

## 60. Preferred Reorder Semantics

A callback-driven design may be safer:

```csharp
onReorder: (oldIndex, newIndex) => ...
```

Application code owns the underlying collection mutation.

Exact API remains deferred.

---

## 61. Item Activation

Double-click or submit activation may expose:

```csharp
onItemActivated: item => Open(item)
```

rather than forcing raw native event handling.

Native events remain accessible through interop.

---

## 62. Context Menus

Row context menus should compose through normal item Widget behavior or future semantic menu APIs.

Virtualization must ensure callbacks are rebound to the correct current item.

---

## 63. Callback Capture Hazard

This is dangerous during recycling if implemented incorrectly:

```text
host created once
↓
callback permanently captures item A
↓
host rebound to B
↓
click still operates on A
```

Item-specific callbacks must be replaced/rebound with item lifetime.

---

## 64. BindingScope Solves Callback Lifetime

The item binding scope should own both:

```text
State subscriptions
item-specific event adapters
```

When rebinding:

```text
scope.Dispose()
↓
new scope
↓
new item callbacks
```

---

## 65. Native Host-Level Events

Events independent of the current item may be registered once at host creation.

Example:

```text
generic pointer behavior
```

provided they resolve current item dynamically and safely.

Prefer the simplest ownership model.

---

## 66. Theme Changes

A theme update should restyle visible/recycled hosts as needed.

It should not require creating a native row for every item in the collection.

Only mounted visible/recycled native elements can require immediate style updates.

---

## 67. Theme and Off-Screen Items

Off-screen logical items have no active native UI to update.

When later bound to a host, they receive the current theme.

This is a key advantage of virtualization.

---

## 68. Collection State Changes

If a model exposes reactive state:

```csharp
clip.Name
clip.IsPlaying
```

the visible bound row may subscribe to that state.

When recycled, subscriptions are removed.

Off-screen items consume no UI subscription resources.

---

## 69. Performance Goal

List UI cost should be roughly related to:

```text
visible item count
+
small recycle buffer
```

rather than:

```text
total collection size
```

for native virtualized cases.

---

## 70. Example

Collection:

```text
10,000 audio clips
```

Visible region:

```text
20 rows
```

Desired conceptual runtime:

```text
10,000 data items
+
approximately visible/recycled row hosts
```

not:

```text
10,000 WidgetNode trees
+
10,000 native row hierarchies
```

---

## 71. Allocation Goal

Scrolling should reuse native host structures where possible.

Avoid:

```text
new root VisualElement for every scroll step
```

if native recycling already provides reusable hosts.

---

## 72. Local Widget Allocation

If initial Strategy A rebuilds item Widget descriptions/subtrees during rebind, some allocations are accepted.

These should be measured during dogfooding.

Only optimize after evidence.

---

## 73. Do Not Cache Every Item Widget

Avoid "optimization":

```text
Dictionary<Item, Widget>
```

for the entire collection.

That may reintroduce memory scaling with total item count and stale state problems.

---

## 74. Do Not Keep All Item Nodes Detached

Likewise, do not virtualize by:

```text
create every WidgetNode
↓
detach invisible ones
```

This is not meaningful virtualization.

Invisible items should generally have no mounted row subtree.

---

## 75. Native ListView Internal Hierarchy

Implementation should avoid relying unnecessarily on undocumented internal native hierarchy names.

Use supported native ListView APIs.

This reduces Unity-version fragility.

---

## 76. Version Compatibility

List virtualization behavior may vary across supported Unity releases.

Any version-specific adapter must remain localized.

Do not scatter Unity-version checks through the public component implementation.

---

## 77. Fallback Behavior

If a targeted Unity version lacks a specific optional ListView feature:

```text
dynamic height
reordering
special selection behavior
```

LumaFlow may:

```text
disable that option
provide a documented fallback
```

rather than building a full custom list renderer.

Core virtualization itself should rely on the native capability available in the minimum supported Unity version.

---

## 78. Small List Alternative

Users remain free to intentionally write:

```csharp
Column(
    children: items.Select(BuildItem)
)
```

for small static collections.

LumaFlow should not forbid normal composition.

Documentation should explain when to use each pattern.

---

## 79. ListView vs Column

General guideline:

```text
small, bounded, composition-heavy list
→ Column / Row / Wrap

large, scrolling, data-driven collection
→ ListView<T>
```

Exact item-count thresholds should not be hardcoded globally because item complexity varies.

---

## 80. Nested ListView

Nested scrolling lists can create difficult UX/native behavior.

LumaFlow does not need special magic to make every nested ListView configuration valid.

Follow UI Toolkit/native scrolling constraints.

---

## 81. ListView Inside Expanded

Common pattern:

```csharp
Column(
    children:
    [
        Toolbar(),

        Expanded(
            child: ListView(...)
        )
    ]
)
```

This should work naturally through native flex sizing.

---

## 82. Full Height Lists

Do not assign arbitrary fixed list heights internally.

Sizing belongs to parent layout unless explicitly configured.

---

## 83. Reactive Filters

Filtering a collection should normally produce a new view/data list and update ListView.

Example:

```text
search State
↓
filtered collection
↓
ListView source update
```

Do not hide search/filtering logic inside core ListView.

---

## 84. Sorting

Same principle:

```text
application/model layer
↓
sorted collection
↓
ListView
```

A higher-level data table may later own sort semantics.

---

## 85. Pagination

Pagination/infinite loading is an application or higher-level component concern.

Base ListView handles already available collection data.

---

## 86. Infinite Scroll

Future APIs may expose near-end scroll notifications.

Do not bake networking/paging into ListView Core.

---

## 87. Async Data

ListView should not directly own async fetching.

Application state can transition:

```text
loading
loaded
error
```

through existing reactive composition.

---

## 88. Error State

Likewise, errors belong to surrounding UI composition.

---

## 89. Data Mutation During Bind

Item builders/binders should not structurally mutate the backing collection during native bind callbacks.

This can cause native list inconsistency.

If application behavior requires mutation, schedule it through an appropriate user action/update boundary.

---

## 90. Reentrant Refresh

Refreshing the list while native binding is already in progress may require guards.

Implementation must avoid recursive refresh corruption.

Do not introduce complex scheduling until real behavior is understood.

---

## 91. Main Thread

List data-to-UI binding occurs on Unity's main thread.

Background collection processing must marshal final UI changes appropriately.

This follows ADR-003 and ADR-008.

---

## 92. Item Context

An item Widget subtree receives normal BuildContext from the ListView's location.

Conceptually:

```text
ListView BuildContext
        ↓
item host
        ↓
item subtree
```

Theme and other scoped UI values therefore work normally.

---

## 93. Item-Specific Context

Do not add the current item into BuildContext automatically.

The model is already supplied directly to the item builder.

Using context for item data would turn BuildContext into a generic data locator.

---

## 94. Index Context

Likewise, index should be passed explicitly if needed, not hidden in BuildContext.

---

## 95. Native Item Root

A recycled host may contain one stable root `VisualElement`.

This root belongs to the host lifecycle.

The current LumaFlow item subtree lives inside it.

---

## 96. Wrapper Cost Is Accepted

One stable host wrapper per recycled slot is acceptable.

Do not prematurely eliminate it if it simplifies:

```text
mounting
cleanup
rebind
diagnostics
```

---

## 97. Focus

Recycling a focused list row requires care.

Native UI Toolkit focus semantics should remain authoritative.

LumaFlow should not attempt to preserve focus on an item that no longer has a visible native row unless a clear feature requires it.

---

## 98. Keyboard Navigation

Native ListView keyboard navigation should remain functional.

Do not replace it with LumaFlow-specific selection key handling unless needed.

---

## 99. Accessibility

Virtualization must preserve native control semantics for visible rows.

Do not use generic VisualElements in place of semantic controls merely to simplify recycling.

---

## 100. Drag and Drop

Editor list rows may participate in drag-and-drop.

Native events and custom item components should remain available through ADR-012 interoperability.

---

## 101. Debugging

Future developer tooling should allow inspection of:

```text
collection count
active recycled hosts
current index/item bound to each host
active item subscriptions
```

This would be valuable for diagnosing recycling bugs.

---

## 102. Development Diagnostics

Potential diagnostics include:

```text
duplicate item keys
item callback fired for stale binding
binding scope not cleared
unsupported dynamic height mode
```

These should be precise.

---

## 103. Tests

Required tests should include:

```text
basic typed collection binding
item builder receives correct item
scroll/recycling
old subscriptions removed on rebind
old callbacks removed on rebind
theme available inside item
selection follows correct item
collection replacement
list unmount cleanup
```

---

## 104. Recycling Subscription Test

Scenario:

```text
host binds item A
↓
subscribe to A.State

host rebinds item B
↓
A.State changes
```

Expected:

```text
row does not update
```

Then:

```text
B.State changes
```

Expected:

```text
row updates correctly
```

---

## 105. Callback Rebind Test

Scenario:

```text
row bound A
button callback opens A

row recycled B
button clicked
```

Expected:

```text
opens B
```

Never A.

---

## 106. List Unmount Test

Unmount entire ListView.

Then mutate any previously bound item State.

Expected:

```text
no item UI callbacks
no native mutation
no exception
```

---

## 107. Collection Replacement Test

Initial:

```text
[A, B, C]
```

Replace with:

```text
[D, E]
```

Verify:

```text
native source refreshed
stale A/B/C item subscriptions removed
visible rows bind D/E
```

---

## 108. Multi-List Isolation

Two ListViews may bind the same source collection.

Unmounting one must not affect the other.

Each owns its own native hosts and subscriptions.

---

## 109. Large Data Smoke Test

Use at least a large synthetic collection such as:

```text
10,000+ entries
```

and verify LumaFlow does not create one mounted row tree per item.

---

## 110. Dogfooding

AudioLib should use virtualized lists for real asset collections.

This should validate:

```text
row API
selection
search/filter refresh
state binding
scrolling
dynamic names/durations
Editor performance
```

Real AudioLib usage should guide API refinement.

---

## 111. Performance Instrumentation

During dogfooding, measure:

```text
host count
mounts per scroll
GC allocations
binding churn
scroll frame time
refresh cost
```

Do not optimize based only on intuition.

---

## 112. Rejected Alternative: Column for Every Collection

Rejected as the standard large-list architecture.

Reason:

```text
memory and runtime cost scale with total item count
```

instead of visible range.

---

## 113. Rejected Alternative: Custom LumaFlow Virtualization Engine

Rejected initially.

Reasons:

```text
UI Toolkit already provides virtualization
complex scrolling behavior
complex geometry management
focus/input difficulty
large maintenance burden
```

Use native ListView.

---

## 114. Rejected Alternative: Keep Every Item Node Mounted

Rejected:

```text
all nodes mounted
+
only hide invisible elements
```

This defeats the purpose of virtualization.

---

## 115. Rejected Alternative: Permanent Item-to-Host Mapping

Rejected:

```text
one recycled host permanently belongs to one model
```

because native virtualization reuses visual slots.

---

## 116. Rejected Alternative: Bind Without Unbind

Rejected.

Every item-specific resource requires deterministic cleanup before reuse.

---

## 117. Rejected Alternative: Reflection-Based Row Templates

Rejected:

```text
inspect T
↓
automatically generate fields
```

as the foundational ListView API.

Use explicit typed item builders.

Higher-level inspector/table tools may add reflection separately.

---

## 118. Rejected Alternative: Require Models to Implement IListItem

Rejected as the default.

Arbitrary model types should work.

---

## 119. Rejected Alternative: General Widget Keys First

List identity does not justify adding Key to every Widget immediately.

Introduce list-specific identity first if required.

---

## 120. Architectural Invariants Created by This ADR

Unless superseded:

### Invariant 1

Large data-driven lists use native UI Toolkit virtualization.

### Invariant 2

LumaFlow does not mount one permanent row subtree per large collection item.

### Invariant 3

Recycled hosts have item-specific binding lifetimes.

### Invariant 4

Old item subscriptions are removed before host rebind.

### Invariant 5

Old item callbacks are removed/replaced before host rebind.

### Invariant 6

Collection/model ownership remains external by default.

### Invariant 7

List virtualization does not require global reconciliation.

### Invariant 8

Semantic item state that must survive recycling lives outside transient row hosts.

### Invariant 9

ListView public API remains typed.

### Invariant 10

Native scrolling, virtualization, and selection are preferred over custom implementations.

---

## 121. Codex Rules

### Rule 1

Do not implement `ListView<T>` as `ScrollView + Column` for the virtualized path.

### Rule 2

Use native UI Toolkit ListView virtualization where available.

### Rule 3

Every recycled row must have deterministic item-binding cleanup.

### Rule 4

Never allow callbacks/subscriptions from the previously bound item to survive rebind.

### Rule 5

Do not create one WidgetNode tree for every item in a large collection.

### Rule 6

Do not store semantic item state solely on a recycled visual host if it must survive scrolling.

### Rule 7

Do not implement global reconciliation merely to support ListView.

### Rule 8

Prefer explicit typed item builders over reflection-based templating.

### Rule 9

Do not mutate externally owned collections implicitly.

### Rule 10

Profile before introducing item-level WidgetNode reuse.

---

## 122. Decision Test

When displaying repeated data:

```text
Is the collection small and bounded?
        ↓ yes
Normal Widget composition may be enough.

Can the collection become large or scroll extensively?
        ↓ yes
Use ListView<T>.

Does UI Toolkit already virtualize this shape?
        ↓ yes
Adapt the native control.

Is item state required after its visual row is recycled?
        ↓ yes
Store it outside the recycled row.

Is performance pressure coming from item rebind mounting?
        ↓ yes
Profile, then evaluate local node reuse.
```

---

## 123. Initial Target API

Conceptually:

```csharp
ListView(
    items: clips,
    itemBuilder: clip =>
        AudioClipRow(
            clip: clip,
            onPlay: () => Play(clip)
        )
)
```

Possible controlled selection later:

```csharp
ListView(
    items: clips,
    selectedItem: selectedClip,
    itemBuilder: clip =>
        AudioClipRow(
            clip: clip
        )
)
```

The exact overloads remain subject to API review.

---

## 124. Example Runtime Model

Collection:

```text
[A, B, C, D, E, F, ...]
```

Visible region:

```text
B
C
D
```

Native/runtime structure may be:

```text
ListViewNode
└── native ListView
    ├── ItemHost #1 → B
    ├── ItemHost #2 → C
    ├── ItemHost #3 → D
    └── recycle buffer
```

After scroll:

```text
C
D
E
```

A host may transition:

```text
Host #1
B
↓ unbind
E
↓ bind
```

without creating native rows for the entire collection.

---

## 125. Example Reactive Item

```csharp
public sealed class DownloadRow : StatelessView
{
    public required DownloadItem Item { get; init; }

    public override Widget Build(BuildContext context)
    {
        return Row(
            children:
            [
                Expanded(
                    child: Text(Item.Name)
                ),

                Text(
                    value: Item.Progress,
                    format: value => $"{value:P0}"
                )
            ]
        );
    }
}
```

When this row is recycled:

```text
subscription to Item.Progress
```

must be disposed before the host binds another DownloadItem.

---

## 126. Example With Persistent Selection

```text
selectedClip State
        ↓
ListView selection model
```

When the row representing that clip scrolls off-screen:

```text
visual row disappears/recycles
```

but:

```text
selectedClip
```

remains unchanged.

When the item becomes visible again, its row reflects current selection state.

---

## 127. Initial Implementation Target

The first ListView implementation should prove:

```text
typed items
native ListView backing
item host creation
item subtree mount
item unbind cleanup
host recycling
list unmount cleanup
basic selection
```

before adding:

```text
reordering
multiple selection
dynamic item height
observable collection diffs
animated insertion
sectioning
```

---

## 128. Future Features

Potential later additions:

```text
item keys
incremental collection diffs
multiple selection
reordering
dynamic item heights
scroll controllers
scroll-to-item
visibility notifications
sectioned lists
TreeView<T>
GridView<T>
```

Each should build on the same native-virtualization principle.

---

## 129. Reconsideration Conditions

Revisit this ADR if:

1. native UI Toolkit virtualization fundamentally cannot support common LumaFlow use cases;
2. native recycling introduces unacceptable limitations;
3. cross-platform behavior is unreliable;
4. a future Unity API provides a superior virtualized collection architecture;
5. specialized high-performance UI requires an alternative module.

A limitation in one optional feature is not sufficient reason to replace native virtualization entirely.

---

## 130. Final Decision

LumaFlow's large-list architecture is:

```text
typed collection
+
declarative item builder
+
native UI Toolkit ListView
+
recycled visual hosts
+
deterministic item binding cleanup
```

not:

```text
collection
↓
mount every item forever
```

The guiding rule is:

**Data size may scale to thousands of items.  
Mounted UI should scale primarily with what is visible.**