# ADR-003: Use Explicit Reactive State with Localized UI Updates

- **Status:** Accepted
- **Decision date:** 2026-08-11
- **Scope:** Reactive state, bindings, lifecycle, update model
- **Affects:** Runtime, Widgets, Inputs, BuildContext, Lifecycle, Testing
- **Related documents:** `AGENTS.md`, `ARCHITECTURE.md`, `API_DESIGN.md`, `ADR-001-native-uitoolkit.md`, `ADR-002-code-first.md`

---

## 1. Context

LumaFlow requires a reactive state model.

A declarative UI framework becomes significantly less useful if developers must manually synchronize:

```text
application state
        ↓
VisualElement properties
```

through imperative callbacks.

At the same time, LumaFlow is built on top of Unity UI Toolkit, which already provides a retained native hierarchy.

Therefore LumaFlow does not automatically need to rebuild the entire UI tree whenever state changes.

Several possible update models exist:

### Model A — Manual imperative updates

```text
State changes
    ↓
developer finds VisualElement
    ↓
developer updates property
```

### Model B — Full declarative rebuild

```text
State changes
    ↓
Build()
    ↓
new widget tree
    ↓
diff/reconciliation
    ↓
VisualElement updates
```

### Model C — Explicit reactive bindings

```text
State<T>
    ↓
specific binding
    ↓
specific VisualElement property
```

LumaFlow will initially use Model C as its primary update mechanism.

---

## 2. Decision

LumaFlow will provide an explicit reactive state primitive:

```csharp
State<T>
```

State changes will notify subscribed mounted UI bindings.

The default update model is:

```text
State changes
        ↓
only subscribed bindings are notified
        ↓
only affected native UI properties or local subtrees update
```

LumaFlow will not require a whole-tree rebuild for ordinary state changes.

---

## 3. Core State Primitive

Conceptual API:

```csharp
var volume = new State<float>(0.8f);
```

Read:

```csharp
var current = volume.Value;
```

Write:

```csharp
volume.Value = 0.5f;
```

or:

```csharp
volume.Value++;
```

where valid for the underlying type.

The exact implementation may evolve.

The semantics should remain simple.

---

## 4. State Is UI-Agnostic

`State<T>` must not depend on:

```text
VisualElement
WidgetNode
Button
Text
BuildContext
```

It is a generic observable state primitive.

Conceptually:

```text
State<T>
    ↓
subscription mechanism
```

LumaFlow UI can consume it, but the state object itself should not know which controls depend on it.

---

## 5. State Ownership

State ownership must be explicit.

Core rule:

```text
The creator of State<T> owns State<T>.
LumaFlow owns subscriptions created by mounted UI.
```

If a parent creates:

```csharp
var volume = new State<float>(0.8f);
```

and passes it into:

```csharp
Slider(
    value: volume
)
```

unmounting the Slider must:

```text
unsubscribe from volume
```

but must not:

```text
dispose/destroy volume
```

unless ownership was explicitly transferred through a future API.

---

## 6. Subscription Ownership

Every mounted reactive consumer must have deterministic subscription ownership.

Preferred architecture:

```text
WidgetNode
    ↓
BindingScope
    ↓
subscriptions
```

When the node unmounts:

```text
BindingScope.Dispose()
    ↓
all owned subscriptions removed
```

This is a strict lifecycle requirement.

---

## 7. Automatic Cleanup

Application developers should not normally write:

```csharp
state.Changed -= OnChanged;
```

for bindings created through LumaFlow widgets.

The framework must clean them automatically.

Example:

```text
TextNode mounts
    ↓
subscribes to State<string>
    ↓
TextNode unmounts
    ↓
subscription automatically disposed
```

Failure to clean these subscriptions is considered a framework bug.

---

## 8. Localized Updates

Simple reactive updates should not rebuild surrounding UI.

Example:

```text
Column
├── Text("Volume")
├── Slider
└── Text(volume)
```

When `volume.Value` changes:

```text
Slider native value updates
Text native text updates
```

The Column itself should remain mounted.

Its unrelated children should remain untouched.

---

## 9. Example Desired Behavior

Application code:

```csharp
var volume = new State<float>(0.8f);

return Column(
    gap: 8,
    children:
    [
        Text("Volume"),

        Slider(
            value: volume,
            min: 0f,
            max: 1f
        ),

        Text(
            value: volume,
            format: value => $"{value:P0}"
        )
    ]
);
```

Conceptual subscriptions:

```text
volume
├── SliderNode
└── TextNode
```

Update:

```text
volume.Value = 0.5
        ↓
notify SliderNode
notify TextNode
        ↓
native properties updated
```

No complete widget tree rebuild occurs.

---

## 10. Explicit Binding

Initial reactive APIs should prefer explicit dependencies.

Good conceptual example:

```csharp
Text(
    value: volume,
    format: value => $"{value:P0}"
)
```

This makes the dependency clear:

```text
Text depends on volume
```

Alternative APIs may evolve, but dependency ownership must remain understandable.

---

## 11. Avoid Mandatory Magical Dependency Tracking

A syntax such as:

```csharp
Text(() => $"Count: {count.Value}")
```

is attractive.

However, automatically determining that `count` was accessed requires reactive dependency tracking.

This may involve:

```text
current reactive evaluation scope
state read interception
dependency collection
dynamic subscription replacement
```

This infrastructure increases:

- complexity;
- hidden behavior;
- debugging difficulty;
- allocation risk;
- lifecycle complexity.

Therefore implicit dependency tracking is not part of the initial architecture.

It may be added later through a separate ADR if proven valuable.

---

## 12. State Equality

`State<T>` should not notify subscribers unnecessarily when the logical value has not changed.

Initial expected semantics:

```csharp
if (EqualityComparer<T>.Default.Equals(oldValue, newValue))
    return;
```

before notification.

Exact implementation may be configurable later.

Default equality semantics should be documented.

---

## 13. Forced Notifications

Do not add forced notification APIs during MVP unless a demonstrated use case requires them.

Potential future APIs such as:

```text
Notify()
Refresh()
MarkDirty()
```

should not be added merely for convenience.

If mutable reference objects require manual refresh repeatedly, reconsider whether `State<T>` is being used at the correct granularity.

---

## 14. Mutable Reference Types

Example:

```csharp
State<List<User>>
```

is potentially dangerous if users mutate the list in place:

```csharp
users.Value.Add(user);
```

because the `State<T>` setter is not invoked.

Documentation must make this behavior clear.

Preferred patterns may include:

```csharp
users.Value = newList;
```

or future specialized collection state.

Do not add complex observable collection infrastructure during the initial State implementation.

---

## 15. Future Observable Collections

Possible future types:

```text
StateList<T>
ObservableList<T>
ReactiveList<T>
```

may be considered if real usage demonstrates repeated need.

This requires separate API review.

Do not overload `State<T>` with collection mutation interception.

---

## 16. Controlled Inputs

Input widgets should support binding directly to State.

Example:

```csharp
TextField(
    label: "Username",
    value: username
)
```

This creates two-way synchronization:

```text
State<string>
    ↓
TextField displayed value

User edits TextField
    ↓
State<string>
```

---

## 17. Two-Way Binding

Two-way binding must avoid feedback loops.

Example:

```text
State update
    ↓
TextField.SetValueWithoutNotify(...)
```

where appropriate.

Then:

```text
user-native change event
    ↓
State.Value assignment
```

Use UI Toolkit APIs designed for silent property updates where possible.

Do not create recursive update loops.

---

## 18. Input Ownership

Passing:

```csharp
value: State<T>
```

means the state is externally controlled.

The widget does not own the State.

Example:

```csharp
var username = new State<string>("Alex");

TextField(
    value: username
)
```

Unmounting TextField leaves `username` alive.

---

## 19. Controlled vs Uncontrolled State

Controls must clearly distinguish between:

```text
externally controlled reactive value
```

and:

```text
initial local value
```

Example controlled:

```csharp
TextField(
    value: username
)
```

Potential uncontrolled:

```csharp
TextField(
    initialValue: "Alex"
)
```

Do not use one ambiguous parameter for both ownership models.

---

## 20. Local State

Views may need state that conceptually belongs to the mounted view.

Potential usage:

```csharp
public sealed class CounterView : StatefulView
{
    private readonly State<int> _count = new(0);

    public override Widget Build(BuildContext context)
    {
        ...
    }
}
```

However, this syntax has lifecycle implications.

If the same Widget description instance is mounted multiple times, a field-owned State could accidentally be shared.

Therefore local-state semantics must be carefully validated before this pattern becomes a guaranteed public contract.

---

## 21. Mounted Local State

A safer long-term model may attach view-local state to the mounted runtime node.

Possible future APIs:

```csharp
var count = context.UseState(0);
```

or:

```csharp
State<int> count = State.Local(0);
```

or another mechanism.

No hook-style API is accepted by this ADR.

The architecture merely requires that mounted-local state not accidentally leak across independent mounts.

---

## 22. StatefulView Is Not Automatically Flutter State

LumaFlow does not automatically adopt:

```text
StatefulWidget
    ↓
State<TWidget>
```

as Flutter implements it.

LumaFlow already has a reactive `State<T>` concept.

The view lifecycle should be designed specifically around LumaFlow's needs.

Do not duplicate the word `State` into confusing concepts without strong justification.

---

## 23. Derived State

Applications frequently derive values.

Example:

```text
firstName
lastName
    ↓
fullName
```

LumaFlow may eventually support derived state.

Potential API:

```csharp
var fullName = State.Combine(
    firstName,
    lastName,
    (first, last) => $"{first} {last}"
);
```

or:

```csharp
var formatted = count.Select(
    value => $"Count: {value}"
);
```

No exact API is accepted yet.

Derived state should remain lazy and leak-free if introduced.

---

## 24. Reactive Mapping

If a `Select`-style API is added:

```csharp
var formatted = count.Select(
    value => $"Count: {value}"
);
```

it must clearly define:

- lifetime;
- subscription ownership;
- equality behavior;
- disposal.

Do not create immortal derived-state chains.

---

## 25. Multiple State Dependencies

Some UI depends on several values.

Example:

```text
username
password
    ↓
canLogin
```

Initial solutions may use explicit combined state or specialized reactive builders.

Do not implement global dependency tracking solely for this scenario.

---

## 26. ReactiveBuilder

Some state changes affect UI structure rather than a simple property.

Example:

```csharp
if (loading)
    show spinner;
else
    show content;
```

For such cases LumaFlow may provide a localized structural primitive:

```csharp
ReactiveBuilder(
    state: loading,
    builder: isLoading =>
        isLoading
            ? ProgressIndicator()
            : Content()
)
```

The exact public API may evolve.

---

## 27. Structural Rebuild Boundary

A structural reactive widget defines a local rebuild boundary.

Example:

```text
Application
├── Header
├── Sidebar
└── ReactiveBuilder
      └── dynamic subtree
```

When its dependency changes:

```text
only dynamic subtree rebuilt
```

not:

```text
entire application rebuilt
```

---

## 28. Initial Structural Update Strategy

During MVP, structural update may simply:

```text
unmount old child subtree
        ↓
build new widget
        ↓
mount new subtree
```

This is acceptable for local structural changes.

Correctness is more important than reconciliation during MVP.

---

## 29. Reconciliation Is Deferred

This ADR does not introduce full tree reconciliation.

LumaFlow will evaluate whether reconciliation is necessary after dogfooding.

A future reconciliation system requires separate architectural approval.

---

## 30. No Full Build on Every State Change

Forbidden as the default architecture:

```text
any State<T> changes
        ↓
root Build()
        ↓
rebuild whole widget tree
```

This would discard one of the major advantages of using native retained UI Toolkit.

Localized updates are preferred.

---

## 31. Native Unity Binding

Unity UI Toolkit may provide native data binding features depending on supported Unity versions.

LumaFlow may use or integrate with them where they simplify implementation.

However:

```text
LumaFlow public reactive API
```

should not necessarily be identical to Unity's binding API.

LumaFlow may provide a stable typed abstraction over Unity binding mechanisms.

---

## 32. Unity Binding Is an Implementation Option

The framework may implement some bindings using:

```text
native UI Toolkit data binding
```

and others through:

```text
LumaFlow subscriptions
```

Consumers should not need to know unless behavior differs.

Do not force every `State<T>` to become a Unity-specific serialized binding source.

---

## 33. State Must Remain Lightweight

`State<T>` should not become a giant application-state framework.

Core State should not automatically include:

```text
persistence
networking
undo/redo
serialization
validation
commands
history
dependency injection
```

Those concerns belong in higher layers or optional systems.

---

## 34. State Does Not Replace Application Architecture

LumaFlow should work with external architectures.

Examples:

```text
plain C# models
MVVM
Redux-like stores
game state
ScriptableObjects
service layers
custom observables
```

Adapters may be introduced later.

`State<T>` is the built-in simple reactive primitive, not a mandatory global architecture.

---

## 35. External Observable Integration

Future APIs may adapt:

```text
IObservable<T>
Unity data binding
custom observable values
```

into LumaFlow bindings.

Interop should not force consumers to convert every external model into `State<T>`.

---

## 36. BuildContext and State

BuildContext may later expose scoped state providers.

Example conceptual pattern:

```text
Provider<T>
    ↓
BuildContext
    ↓
descendant widgets
```

This is not required for MVP.

BuildContext must not become an implicit global mutable state container.

---

## 37. State Scope

Global state should not be stored in global static fields by framework convention.

Bad:

```csharp
public static State<User> CurrentUser;
```

as a LumaFlow-required pattern.

Application architecture decides global ownership.

LumaFlow should provide scoped composition tools where useful.

---

## 38. Main Thread Semantics

UI Toolkit mutations occur on Unity's main thread.

Therefore LumaFlow's initial contract is:

```text
State changes which directly affect mounted UI
must occur on Unity main thread
```

or must be marshaled there by future infrastructure.

MVP does not need transparent cross-thread scheduling.

---

## 39. Off-Thread State

State itself may eventually remain thread-safe or thread-agnostic.

However, UI subscriptions cannot mutate `VisualElement` from worker threads.

If off-thread support is added, architecture should be:

```text
worker State update
        ↓
LumaFlow scheduler
        ↓
Unity main thread
        ↓
UI mutation
```

This requires separate design work.

---

## 40. Async Workflows

Reactive state naturally supports async workflows:

```csharp
loading.Value = true;

try
{
    await LoadAsync();
}
finally
{
    loading.Value = false;
}
```

LumaFlow need not create a separate async state framework for basic use.

---

## 41. Unmount During Async Work

If async code later updates State after a widget unmounts:

```text
State remains valid
mounted subscription no longer exists
```

therefore no dead UI should be accessed.

This is an important benefit of subscription-based updates.

---

## 42. Errors in Subscribers

State notification infrastructure must not silently swallow arbitrary exceptions.

LumaFlow uses a non-transactional notification policy:

```text
State.Value commits the new value
        ↓
every active subscriber is notified synchronously
        ↓
subscriber failures are collected
        ↓
failure is rethrown after the notification pass
```

A single subscriber failure is rethrown as-is. Multiple failures are reported
through `AggregateException`. A failure never rolls back `State.Value` and never
prevents later subscribers from receiving the committed value. Subscriber side
effects are therefore explicitly non-transactional.

`ReactiveBuilder` follows the same state policy, while preserving its last valid
mounted subtree if its own replacement builder fails. Other subscribers remain
eligible to update during that notification pass.

Framework bugs must remain observable.

One broken subscriber should not corrupt subscription bookkeeping.

---

## 43. Notification Mutation

The implementation must safely handle subscriptions being added or removed during notification.

Example:

```text
Subscriber A executes
    ↓
unsubscribes itself
```

This must not corrupt iteration.

Implementation should use a robust strategy.

---

## 44. Reentrant Updates

State changes may theoretically trigger further state changes.

Example:

```text
A changes
↓
subscriber changes B
↓
B subscribers run
```

The initial implementation may allow synchronous nested notifications.

Infinite update loops remain an application error.

Do not introduce a complex scheduler before demonstrated need.

---

## 45. Batched Updates

Potential future feature:

```csharp
State.Batch(() =>
{
    firstName.Value = "Alex";
    lastName.Value = "Pavlov";
});
```

could reduce redundant updates.

This is explicitly deferred.

Do not add batching until profiling or real UX demonstrates the need.

---

## 46. Frame Scheduling

LumaFlow initially should not defer every state update until the next frame.

For ordinary UI:

```text
State.Value assignment
        ↓
synchronous UI property update
```

on main thread is easier to reason about.

Future scheduling/coalescing may be introduced if performance requires it.

---

## 47. Transactional Updates

No transaction model is required for MVP.

Do not introduce:

```text
begin transaction
commit transaction
rollback
```

into basic reactive state.

---

## 48. Undo/Redo

Unity Editor workflows may eventually require undo support.

This should be layered separately.

`State<T>` must not automatically register every value mutation with Unity Undo.

Editor-specific adapters may provide that behavior later.

---

## 49. Serialization

`State<T>` is not automatically Unity-serialized.

Do not derive it from:

```text
UnityEngine.Object
ScriptableObject
MonoBehaviour
```

for core semantics.

Serialized application data may be bound into State or adapted separately.

---

## 50. Persistence

State does not automatically persist between:

```text
play sessions
scene changes
domain reloads
application launches
```

Persistence belongs to application architecture.

---

## 51. Testing Requirements

State infrastructure must receive strong tests.

Required cases:

```text
initial value
value change
same-value assignment
multiple subscribers
unsubscribe
unsubscribe during notification
mount binding
unmount cleanup
two-way input binding
no feedback loop
structural reactive subtree replacement
```

---

## 52. Leak Testing

At least framework-level tests should validate:

```text
mount
subscribe
unmount
state changes afterwards
```

does not invoke the removed node.

This is critical.

---

## 53. Debuggability

Reactive relationships should remain understandable.

Potential future development tooling may expose:

```text
State<T>
↓
current subscribers
↓
WidgetNode
```

Do not make dependency relationships impossible to inspect.

---

## 54. Binding Diagnostics

In development builds, useful errors may include:

```text
binding attempted after node disposal
duplicate subscription ownership
invalid two-way binding
```

Do not silently recover from framework invariant violations.

---

## 55. Performance Goals

State updates should have complexity roughly proportional to:

```text
number of subscribers to that State
```

not:

```text
size of entire UI tree
```

This is a core reason for this ADR.

---

## 56. Allocation Goals

After mounting, ordinary State changes should avoid significant temporary allocations.

Avoid:

```text
LINQ chains
new widget trees
temporary lists
reflection
```

in basic property update paths.

Do not sacrifice clear architecture for micro-optimization, but keep the hot path simple.

---

## 57. Example: Reactive Text

Desired:

```csharp
var count = new State<int>(0);

Text(
    value: count,
    format: value => $"Count: {value}"
)
```

Mount:

```text
create Label
set initial text
subscribe to count
```

Update:

```text
count.Value++
↓
update Label.text
```

Unmount:

```text
unsubscribe
```

---

## 58. Example: Reactive Button Enabled State

Conceptual:

```csharp
Button(
    "Save",
    enabled: canSave,
    onPressed: Save
)
```

where:

```csharp
State<bool> canSave;
```

Update:

```text
canSave changes
↓
Button.SetEnabled(...)
```

No Button recreation required.

---

## 59. Example: Two-Way Slider

```csharp
Slider(
    value: volume,
    min: 0f,
    max: 1f
)
```

Flow A:

```text
volume changes
↓
native slider updated silently
```

Flow B:

```text
user drags slider
↓
native change event
↓
volume.Value updated
```

---

## 60. Example: Conditional Content

```csharp
ReactiveBuilder(
    state: loading,
    builder: value =>
        value
            ? ProgressIndicator()
            : ResultsView()
)
```

Only this subtree changes.

---

## 61. Rejected Alternative: Manual Imperative Synchronization

Rejected:

```csharp
state.Changed += value =>
{
    root.Q<Label>("count").text = value.ToString();
};
```

as the primary application model.

Reasons:

- query selectors;
- manual ownership;
- manual cleanup;
- structural coupling;
- boilerplate;
- poor composability.

LumaFlow should own these mechanics.

---

## 62. Rejected Alternative: Whole-Tree Rebuild by Default

Rejected initial model:

```text
any state mutation
↓
Build entire application
↓
reconcile every widget
```

Reasons:

- unnecessary complexity;
- unnecessary allocations;
- ignores retained UI Toolkit strengths;
- requires early reconciliation infrastructure;
- harder to optimize local controls;
- harder to reason about update cost.

This may be reconsidered only if real usage demonstrates stronger benefits.

---

## 63. Rejected Alternative: Reflection-Based Automatic Binding

Rejected as the core model:

```csharp
[Bind]
public int Count;
```

with runtime reflection discovering dependencies.

Reasons:

- weaker type transparency;
- IL2CPP/AOT concerns;
- code stripping complexity;
- runtime overhead;
- debugging complexity.

Optional tooling may eventually provide similar convenience without making it foundational.

---

## 64. Rejected Alternative: Make Every Widget Mutable

Rejected:

```csharp
text.Text = newValue;
```

as the primary reactive application API.

Widgets are descriptions.

Mounted native nodes are runtime implementation details.

State should drive updates through bindings.

---

## 65. Architectural Invariants Created by This ADR

Unless explicitly superseded:

### Invariant 1

`State<T>` is UI-agnostic.

### Invariant 2

State ownership remains with its creator unless explicitly transferred.

### Invariant 3

Mounted nodes own their subscriptions.

### Invariant 4

Subscriptions are automatically cleaned on unmount.

### Invariant 5

Simple state changes update localized native properties.

### Invariant 6

Whole-tree rebuild is not the default state update model.

### Invariant 7

Structural reactivity rebuilds explicit local boundaries.

### Invariant 8

Full reconciliation is deferred.

### Invariant 9

Reactive dependencies should initially remain explicit.

### Invariant 10

Core state does not require reflection.

---

## 66. Codex Rules

### Rule 1

When connecting State to UI, register the subscription with the mounted node's lifecycle scope.

### Rule 2

Never create a state subscription without defining its disposal owner.

### Rule 3

Do not rebuild parent widgets when a native property update is sufficient.

### Rule 4

Do not introduce root-level Build() calls on every State change.

### Rule 5

Use silent native updates for two-way controls where needed to prevent loops.

### Rule 6

Do not dispose externally provided State objects on widget unmount.

### Rule 7

Do not implement magical dependency tracking without a new ADR.

### Rule 8

Do not implement full reconciliation without a new ADR.

### Rule 9

Do not introduce observable collection infrastructure until justified by real usage.

### Rule 10

Keep State<T> independent from Unity-specific component classes.

---

## 67. Decision Test

When implementing reactivity, ask:

```text
Can this state change update one native property?
        ↓ yes
Bind directly.

Does it change a local UI subtree?
        ↓ yes
Use a structural reactive boundary.

Does it require rebuilding the whole application?
        ↓ probably not
Reconsider architecture.

Does the implementation create a subscription?
        ↓ yes
Who owns disposal?

Is the State externally supplied?
        ↓ yes
Do not dispose it.
```

---

## 68. Initial Target API

The exact overloads may evolve, but the target developer experience is approximately:

```csharp
public sealed class VolumeView : StatelessView
{
    public required State<float> Volume { get; init; }

    public override Widget Build(BuildContext context)
    {
        return Column(
            gap: 8,
            children:
            [
                Text(
                    value: Volume,
                    format: value => $"Volume: {value:P0}"
                ),

                Slider(
                    value: Volume,
                    min: 0f,
                    max: 1f
                )
            ]
        );
    }
}
```

The framework handles:

```text
initial synchronization
subscriptions
native control events
state propagation
feedback-loop prevention
cleanup
```

---

## 69. Long-Term Direction

LumaFlow may eventually add:

```text
derived state
combined state
observable collections
scoped providers
batching
computed values
automatic dependency tracking
state debugging tools
external observable adapters
```

Each should be driven by demonstrated needs.

The simple model must remain usable.

---

## 70. Reconsideration Conditions

This ADR may be reconsidered if:

1. localized bindings create excessive public API complexity;
2. most real-world components require structural rebuilds;
3. full declarative tree reconstruction proves substantially simpler;
4. native UI Toolkit changes make another model preferable;
5. performance measurements favor a different scheduling/reconciliation strategy.

Any major change requires a new ADR superseding this one.

---

## 71. Final Decision

LumaFlow reactivity is based on:

```text
explicit State<T>
+
deterministic subscriptions
+
automatic lifecycle cleanup
+
localized native UI updates
+
explicit structural rebuild boundaries
```

rather than:

```text
manual VisualElement synchronization
```

or:

```text
mandatory full application rebuilds
```

This allows LumaFlow to provide modern reactive UI semantics while respecting the retained architecture of Unity UI Toolkit.
