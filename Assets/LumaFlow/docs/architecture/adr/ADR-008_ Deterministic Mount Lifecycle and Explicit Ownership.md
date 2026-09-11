# ADR-008: Deterministic Mount Lifecycle and Explicit Ownership

- **Status:** Accepted
- **Decision date:** 2026-08-11
- **Scope:** Lifecycle, mounting, unmounting, disposal, resource ownership
- **Affects:** Runtime, WidgetNode, MountHandle, Bindings, Events, Native Interop, Structural Rebuilds
- **Related documents:** `ARCHITECTURE.md`, `ADR-003-reactive-state.md`, `ADR-004-reconciliation-strategy.md`, `ADR-007-widget-runtime-model.md`

---

## 1. Context

LumaFlow introduces a runtime layer between declarative Widgets and Unity UI Toolkit.

That runtime layer owns:

- mounted WidgetNodes;
- child-node hierarchies;
- native UI Toolkit elements;
- reactive subscriptions;
- event bindings;
- BuildContext relationships;
- structural rebuild boundaries;
- optional scheduled work;
- future temporary resources.

Without explicit lifecycle and ownership semantics, LumaFlow risks:

- leaked State subscriptions;
- duplicated event callbacks;
- callbacks targeting removed UI;
- orphaned VisualElements;
- stale BuildContext references;
- invalid re-mount behavior;
- difficult debugging;
- inconsistent behavior between Runtime and Editor.

Lifecycle must therefore be deterministic.

---

## 2. Decision

Every mounted WidgetNode will follow an explicit lifecycle.

Conceptually:

```text
Created
   ↓
Mounting
   ↓
Mounted / Active
   ↓
Unmounting
   ↓
Unmounted
   ↓
Disposed
```

The exact enum or implementation may be simplified, but the semantic transitions must remain explicit.

A mounted node must have one clear owner responsible for ending its lifecycle.

---

## 3. Primary Lifecycle Rule

For every resource created or subscription established during mount, the framework must know:

```text
Who owns it?
When is it released?
What happens if mount fails?
```

If those questions cannot be answered, the implementation is incomplete.

---

## 4. Mount

Mounting transforms a runtime node from an unattached object into active UI.

Conceptually:

```text
Widget
    ↓
Create WidgetNode
    ↓
Mount node
    ↓
Create/configure native element
    ↓
Attach context
    ↓
Mount children
    ↓
Attach bindings/events
    ↓
Attach native hierarchy
    ↓
Active
```

The exact ordering may differ for specific controls.

---

## 5. Mount Must Be Transactional Where Practical

The framework should avoid leaving half-mounted trees after a failure.

Example:

```text
create node
↓
create native element
↓
mount child A
↓
mount child B fails
```

The runtime should clean up:

```text
child A
bindings
events
native elements
temporary resources
```

before propagating the failure.

---

## 6. Mount Failure

A mount failure must not silently leave an active partial subtree.

Preferred behavior:

```text
Mount begins
↓
failure occurs
↓
rollback owned resources
↓
node enters failed/unmounted/disposed state
↓
exception propagates with useful diagnostics
```

Do not continue with an inconsistent node.

---

## 7. Mount Ownership

The caller that creates a root mount receives an ownership handle.

Conceptually:

```csharp
var handle = LumaFlow.Mount(
    widget,
    rootVisualElement
);
```

The returned handle owns the root LumaFlow subtree.

---

## 8. MountHandle

A public mount operation should eventually return a type similar to:

```csharp
public sealed class MountHandle : IDisposable
{
}
```

Disposing the handle should:

```text
unmount root node
↓
clean descendants
↓
detach LumaFlow-owned hierarchy
↓
release runtime references
```

The exact API may evolve.

---

## 9. Root VisualElement Ownership

The external root is not owned by LumaFlow.

Example:

```text
EditorWindow.rootVisualElement
```

or:

```text
UIDocument.rootVisualElement
```

remains externally owned.

LumaFlow owns only the subtree it mounts into that root.

---

## 10. Child Ownership

A parent WidgetNode owns the lifecycle of child WidgetNodes it mounts.

Example:

```text
ColumnNode
├── TextNode
├── ButtonNode
└── CardNode
```

Unmounting `ColumnNode` must also unmount all owned children.

Application code must not separately dispose those children.

---

## 11. Ownership Tree

Runtime ownership generally follows:

```text
MountHandle
    ↓
Root WidgetNode
    ↓
Child WidgetNodes
    ↓
Bindings / events / native elements
```

This hierarchy must be deterministic.

---

## 12. Binding Ownership

Bindings created by a node belong to that node.

Conceptually:

```text
WidgetNode
    ↓
BindingScope
    ↓
State subscriptions
```

Unmounting the node must dispose its BindingScope.

This is required by ADR-003.

---

## 13. Event Ownership

Callbacks registered by LumaFlow during mount belong to the node that registered them.

Example:

```text
ButtonNode
    ↓
nativeButton.clicked += callback
```

When the node unmounts, any registration requiring explicit removal must be removed.

Do not rely on garbage collection to define event lifetime.

---

## 14. Native UI Toolkit Event Semantics

Where UI Toolkit automatically releases certain event relationships when elements are no longer referenced, LumaFlow may rely on supported native behavior.

However, framework-managed subscriptions must still have clear ownership.

Do not assume every event cleans itself automatically.

---

## 15. Scheduled Work

If a node uses:

```text
IVisualElementScheduledItem
timers
delayed callbacks
animation tasks
```

that work belongs to the node lifecycle unless explicitly external.

Unmount must stop or invalidate scheduled work.

---

## 16. Async Work

Async operations triggered by user callbacks are normally application-owned.

Example:

```csharp
Button(
    "Login",
    onPressed: LoginAsync
)
```

Unmounting the Button does not automatically cancel arbitrary application logic.

However, framework-owned async operations must define cancellation and cleanup.

---

## 17. External State Ownership

A node subscribing to:

```csharp
State<T>
```

does not own that State unless ownership is explicitly transferred.

Unmount:

```text
dispose subscription
```

not:

```text
dispose State<T>
```

---

## 18. External Asset Ownership

A Widget referencing:

```text
Texture2D
VectorImage
StyleSheet
VisualTreeAsset
ScriptableObject
```

does not imply ownership of the asset.

LumaFlow must not destroy externally supplied Unity assets.

---

## 19. External VisualElement Ownership

For:

```csharp
Native(existingElement)
```

LumaFlow owns:

```text
mount attachment
LumaFlow-added bindings
LumaFlow-added callbacks
```

but not arbitrary external resources associated with the element.

---

## 20. External VisualElement Detach

Unmounting a Native wrapper should normally detach the element from the LumaFlow-owned hierarchy.

It should not destroy the object.

---

## 21. Reparenting External Elements

If a supplied VisualElement already has a parent, LumaFlow must not silently steal it.

Possible behavior:

```text
throw descriptive exception
```

unless an explicit reparent option is introduced later.

---

## 22. Unmount

Unmount removes a node from active UI and releases mount-owned resources.

Conceptual sequence:

```text
mark Unmounting
↓
stop new updates
↓
unmount child nodes
↓
dispose bindings
↓
detach event adapters
↓
stop scheduled work
↓
detach native hierarchy
↓
release context/native references
↓
mark Unmounted/Disposed
```

Exact ordering may vary where native semantics demand it.

---

## 23. Stop Updates Before Cleanup

Once unmount begins, reactive updates should no longer mutate the node.

A State notification arriving during teardown must not write into partially disposed UI.

Development guards may detect such behavior.

---

## 24. Child Unmount Ordering

Default strategy:

```text
parent enters Unmounting
↓
children unmount
↓
parent-owned resources clean
↓
parent native element detaches
```

This allows children to still access valid parent-native structure during their own cleanup if necessary.

---

## 25. Reverse Child Order

Unmounting children in reverse mount order may be useful for stack-like ownership.

Example:

```text
A mounted
B mounted
C mounted
```

cleanup:

```text
C
B
A
```

This may become the default if implementation benefits.

The chosen ordering must be consistent.

---

## 26. Disposal

`Dispose` represents final release of runtime-owned resources.

The framework may choose to unify:

```text
Unmount
+
Dispose
```

for nodes that are never reusable.

This is likely preferable initially.

Avoid maintaining two nearly identical cleanup paths without reason.

---

## 27. Node Reuse

General node reuse after unmount is not part of MVP.

Therefore:

```text
Unmount
≈ final runtime cleanup
```

for ordinary nodes.

If future pooling/reconciliation reuses nodes, lifecycle semantics must be revised explicitly.

---

## 28. Double Mount

Mounting the same WidgetNode twice is invalid.

Example:

```text
node.Mount(...)
node.Mount(...)
```

must not produce two native hierarchies.

Preferred development behavior:

```text
throw descriptive lifecycle exception
```

---

## 29. Double Unmount

The framework must define consistent semantics.

Recommended initial behavior:

```text
Unmount already-unmounted node
→ safe no-op in release
→ optional diagnostic in development
```

This reduces teardown fragility.

Exact behavior should be tested.

---

## 30. Dispose Idempotency

`MountHandle.Dispose()` should ideally be idempotent.

Example:

```csharp
handle.Dispose();
handle.Dispose();
```

should not corrupt lifecycle.

This is useful with standard C# ownership patterns.

---

## 31. Invalid Updates After Unmount

This is forbidden:

```text
State changes
↓
dead TextNode receives update
↓
mutates removed Label
```

Automatic subscription cleanup must prevent it.

---

## 32. Structural Rebuild Ownership

A structural rebuild boundary owns its current child subtree.

Example:

```text
ReactiveBuilderNode
    ↓
CurrentChildNode
```

On rebuild:

```text
unmount old CurrentChildNode
↓
mount replacement
```

The boundary itself remains alive.

---

## 33. Structural Build Failure

If the new subtree fails to build or mount, the runtime should avoid corrupting the boundary.

Possible safer strategy:

```text
build new description
↓
attempt prepare/mount
↓
replace old only when safe
```

The exact transaction model requires implementation validation.

---

## 34. Old Subtree Preservation

Where practical, a failed replacement may leave the previous subtree mounted.

This provides better resilience during development.

However, lifecycle correctness takes priority over visual continuity.

---

## 35. Theme Provider Ownership

A theme provider owns context propagation, not ThemeData itself unless explicitly created as owned data.

Theme objects are generally shared immutable values.

Unmounting a Theme widget does not "destroy" ThemeData.

---

## 36. BuildContext Lifetime

BuildContext exists relative to a mounted location.

Once the owning node is unmounted, its context must not be treated as a permanent valid mount handle.

Do not expose BuildContext for long-term storage by application code.

---

## 37. Context Capture

User callbacks may capture BuildContext.

This is sometimes convenient but can become stale after unmount.

Documentation should discourage storing BuildContext beyond the active view lifecycle.

Future diagnostics may detect obvious misuse.

---

## 38. Native Root Attachment

LumaFlow should attach its own root subtree in a controlled way.

Possible model:

```text
External root
└── LumaFlow mount root element
```

or direct child-native attachment if the root node naturally provides one.

The runtime should be able to identify what it owns.

---

## 39. Root Wrapper

A dedicated root wrapper VisualElement may simplify ownership.

Advantages:

- clear detach;
- isolated classes/styles;
- mount identification.

Disadvantages:

- extra hierarchy node;
- possible layout effects.

This is an implementation decision, not mandated by this ADR.

---

## 40. No Arbitrary Root Clear

Mount cleanup must not do:

```csharp
root.Clear();
```

unless LumaFlow owns the entire root by explicit contract.

It must remove only the subtree it owns.

---

## 41. Multiple Mounts in One Root

This should remain possible:

```text
root
├── Luma mount A
└── Luma mount B
```

Disposing A must not affect B.

---

## 42. Multiple Editor Windows

Each window's mount lifecycle must be independent.

Closing one EditorWindow should not impact another LumaFlow tree.

---

## 43. Domain Reload

Editor lifecycle must account for Unity domain reload.

Static mutable ownership must not be required for correct cleanup.

Editor-specific hooks may eventually assist cleanup, but Core lifecycle remains explicit.

---

## 44. Enter Play Mode Without Domain Reload

LumaFlow must not rely on domain reload to clean static state.

This reinforces the rule against global mutable mount registries.

---

## 45. Assembly Reload

Editor integrations subscribing to global Unity Editor events must unregister appropriately.

Example:

```text
AssemblyReloadEvents
EditorApplication.update
Selection.changed
```

Such subscriptions belong in Editor-only lifecycle scopes.

---

## 46. Player Shutdown

LumaFlow does not need to perfectly unmount every node during process termination for correctness, but normal application lifecycle should remain clean.

Explicit mount disposal should still be supported.

---

## 47. Scene Changes

LumaFlow Core does not automatically own scene lifecycle.

If a mount root disappears because its GameObject/UIDocument is destroyed, integration code must ensure the mount handle is disposed.

A future host component may automate this.

---

## 48. Runtime Host Component

A convenience host may eventually exist:

```text
LumaFlowHost : MonoBehaviour
```

that mounts/unmounts with Unity lifecycle.

This is optional.

Core Widgets must not require such a host.

---

## 49. EditorWindow Host

Likewise, helper base classes may later simplify:

```text
CreateGUI
OnDisable
```

mount management.

The underlying MountHandle semantics remain the source of truth.

---

## 50. Lifecycle Hooks

Potential user-facing hooks:

```text
OnMount
OnUnmount
```

may be added if real use cases require them.

Do not expose many lifecycle callbacks initially.

Each hook becomes part of the public contract.

---

## 51. Hook Ordering

If lifecycle hooks are introduced, ordering must be explicitly defined.

Example:

```text
OnMount
→ children mounted?
```

must not remain ambiguous.

This ADR does not yet accept a public hook API.

---

## 52. No Unity MonoBehaviour Semantics by Assumption

Do not assume Widget lifecycle maps directly to:

```text
Awake
OnEnable
Start
OnDisable
OnDestroy
```

LumaFlow has its own mount semantics.

Unity host integrations may bridge them.

---

## 53. Exception During Unmount

Cleanup should be best-effort.

If one child/resource throws during teardown, the runtime should attempt to clean remaining owned resources where safe.

A single cleanup failure should not automatically leak the rest of the subtree.

---

## 54. Aggregate Cleanup Errors

Development tooling may collect multiple teardown errors and report them together.

Exact behavior is deferred.

The important requirement is continued cleanup where possible.

---

## 55. User Callback Exceptions

If a user callback throws during normal interaction, LumaFlow should not automatically unmount the node unless runtime integrity is compromised.

User exceptions should propagate through normal Unity/C# error mechanisms.

---

## 56. Framework Invariant Exceptions

Invalid lifecycle operations should produce LumaFlow-specific contextual diagnostics.

Example:

```text
LumaFlow: Cannot mount ButtonNode because it is already mounted.
```

not an unexplained:

```text
NullReferenceException
```

---

## 57. Resource Scope Abstraction

A general internal lifetime scope may be useful.

Conceptually:

```csharp
internal sealed class LifetimeScope : IDisposable
{
    void Add(IDisposable resource);
    void Add(Action cleanup);
}
```

This could own:

- state subscriptions;
- event cleanup;
- schedules;
- temporary resources.

Exact implementation is optional.

---

## 58. BindingScope vs LifetimeScope

`BindingScope` may remain specialized, or a broader `LifetimeScope` may subsume it.

Avoid duplicating nearly identical disposal containers.

Choose the smallest coherent abstraction during implementation.

---

## 59. Cleanup Registration

Internal code should make cleanup registration easy.

Example conceptual pattern:

```csharp
lifetime.Add(
    state.Subscribe(UpdateText)
);
```

or:

```csharp
lifetime.Add(() =>
    button.clicked -= callback
);
```

This reduces manual leak risk.

---

## 60. Cleanup Ordering

A lifetime scope may dispose resources in reverse registration order.

This is often intuitive and safe.

If adopted, document the rule internally.

---

## 61. Ownership Transfer

Ownership transfer must be explicit.

Example future API:

```text
Owned<T>
Borrowed<T>
```

is probably unnecessary for MVP.

Instead, document ownership at API boundaries.

Do not infer ownership from parameter names.

---

## 62. Native Resources

If future Effects allocate:

```text
Material
RenderTexture
CommandBuffer
```

the node/effect backend that creates them owns cleanup unless resources are supplied externally.

This lifecycle model extends naturally to optional modules.

---

## 63. Pooling

Pooling changes lifecycle semantics.

If nodes/elements are pooled:

```text
Unmount
≠
Dispose
```

This is intentionally deferred.

Any general pooling system requires a new architectural decision or update to this ADR.

---

## 64. Virtualized List Recycling

List recycling is a specialized exception.

A recycled item may be:

```text
unbound from item A
↓
rebound to item B
```

without complete native destruction.

This requires a specialized lifecycle layer.

Do not generalize list-specific behavior to ordinary WidgetNodes.

---

## 65. List Item Binding Scope

Each recycled list item must clean item-specific subscriptions before being rebound.

Conceptually:

```text
bind item A
↓
subscriptions A
↓
unbind
↓
dispose A subscriptions
↓
bind item B
```

This prevents cross-item updates.

---

## 66. State Preservation Across Rebuild

If state should survive a local subtree rebuild, ownership must exist above that subtree.

Example:

```text
Parent owns State<T>
↓
ReactiveBuilder replaces child
```

This follows ADR-003 and ADR-004.

---

## 67. Local Node-Owned State

If a future StatefulView node owns local State, unmounting that node disposes/releases that local state according to its explicit semantics.

Externally supplied State remains untouched.

---

## 68. Memory Retention

After final unmount, a node should release references to:

```text
Widget
BuildContext
VisualElement
children
bindings
callbacks
```

where practical.

This helps GC and reduces accidental stale access.

---

## 69. Widget Description Retention

A mounted node may retain the current Widget description while active.

After disposal, retaining it is unnecessary unless diagnostics require it.

Debug builds may preserve some metadata differently.

---

## 70. Development Diagnostics

Future debug tooling may display:

```text
Node lifecycle state
mount parent
child count
active subscriptions
native element
```

This will make ownership easier to inspect.

---

## 71. Mount IDs

Each root mount may receive an internal ID for diagnostics.

Example:

```text
Mount #7
```

This is diagnostic only.

Do not make it part of user identity semantics.

---

## 72. Leak Diagnostics

Future development-mode checks may detect:

```text
subscription on disposed node
scheduled callback on disposed node
native element still attached after mount disposal
```

These are desirable but not MVP blockers.

---

## 73. Threading

Mount/unmount and VisualElement mutation occur on Unity's main thread.

Lifecycle APIs should initially enforce or document main-thread use.

Do not support cross-thread mounting implicitly.

---

## 74. Reentrant Unmount

A callback may theoretically cause its own subtree to unmount.

Example:

```text
Button clicked
↓
Navigator.Pop()
↓
ButtonNode removed
```

This is valid.

The runtime must tolerate user callbacks causing structural teardown.

---

## 75. Callback After Self-Unmount

After a callback triggers self-unmount, framework code must not continue mutating the disposed node.

Component event handlers should be written with this possibility in mind.

---

## 76. Reentrant State Change During Unmount

Unmount cleanup may indirectly trigger state changes.

Dead nodes must already be protected from processing further updates.

This is another reason to mark lifecycle state before cleanup begins.

---

## 77. Navigation Ownership

Future Navigator owns mounted screen entries.

Conceptually:

```text
Navigator
├── Screen A mount
└── Screen B mount
```

Popping a screen disposes its owned mount/subtree.

Navigation must reuse the same lifecycle infrastructure.

---

## 78. Overlay Ownership

OverlayHost owns active OverlayEntry subtrees.

Removing an overlay unmounts and cleans its subtree.

Do not build a separate disposal model for overlays.

---

## 79. Testing Requirements

Lifecycle tests must cover:

```text
successful mount
successful unmount
double dispose
mount failure rollback
child mount failure
binding cleanup
event cleanup
state update after unmount
multiple root mounts
native external element detach
structural subtree replacement
```

---

## 80. Leak Regression Tests

Any discovered lifecycle leak should receive a regression test where practical.

Lifecycle correctness is core infrastructure.

---

## 81. Mount Failure Test

Test scenario:

```text
Parent
├── valid child
└── throwing child
```

After mount fails:

```text
valid child not left mounted
subscriptions removed
root clean
```

---

## 82. Multiple Mount Isolation Test

Create:

```text
mount A
mount B
```

into the same root.

Dispose A.

Verify:

```text
B remains active
B subscriptions remain valid
B hierarchy remains attached
```

---

## 83. State Cleanup Test

Mount reactive Text.

Unmount.

Change State.

Verify:

```text
no callback into removed node
no exception
```

---

## 84. Native Adapter Test

Mount external VisualElement.

Unmount.

Verify:

```text
element detached
element object still usable externally
```

---

## 85. Rejected Alternative: GC-Driven Lifecycle

Rejected:

```text
Just stop referencing WidgetNodes and let GC handle cleanup.
```

Reasons:

- events may retain references;
- State subscriptions may retain nodes;
- native hierarchy remains attached;
- deterministic cleanup is required;
- Editor sessions are long-lived.

---

## 86. Rejected Alternative: VisualElement Parent Removal Is Enough

Rejected:

```text
parent.Remove(element)
```

as complete lifecycle management.

Removing the native element does not inherently clean:

```text
State subscriptions
framework callbacks
scheduled work
context references
child-node runtime state
```

---

## 87. Rejected Alternative: Widget Owns Its Own Lifecycle

Rejected because Widget is a declarative description.

Lifecycle belongs to WidgetNode.

This follows ADR-007.

---

## 88. Rejected Alternative: Global Cleanup Registry

Rejected as primary ownership:

```text
Global list of all active subscriptions/nodes
```

Reasons:

- poor isolation;
- multiple windows/panels;
- difficult ownership;
- hidden global state.

Tree-local ownership is preferred.

---

## 89. Rejected Alternative: Cleanup Only on Root Disposal

Rejected if child subtrees can be replaced independently.

A local structural rebuild must clean the removed child immediately.

Cleanup must follow actual subtree lifecycle.

---

## 90. Architectural Invariants Created by This ADR

Unless superseded:

### Invariant 1

Every mounted node has deterministic ownership.

### Invariant 2

Parent nodes own child-node lifecycle.

### Invariant 3

Root mount ownership is represented explicitly.

### Invariant 4

Bindings and events have disposal owners.

### Invariant 5

Unmount cleans mount-owned resources.

### Invariant 6

External State and assets are not disposed implicitly.

### Invariant 7

External root VisualElements are not owned by LumaFlow.

### Invariant 8

Structural replacement unmounts the old subtree before final release.

### Invariant 9

Invalid lifecycle transitions are guarded.

### Invariant 10

Cleanup does not depend on garbage collection.

---

## 91. Codex Rules

### Rule 1

Never create a subscription without assigning a lifetime owner.

### Rule 2

Never register native/global events without defining cleanup.

### Rule 3

Do not use `root.Clear()` to unmount LumaFlow unless ownership of the entire root is explicit.

### Rule 4

Do not dispose externally supplied State, Unity assets, or VisualElements unless API contract explicitly transfers ownership.

### Rule 5

Mark nodes as unmounting before callbacks/subscriptions are torn down.

### Rule 6

Use the shared lifecycle mechanism for structural rebuilds, navigation, overlays, and component teardown.

### Rule 7

On mount failure, clean already-created owned resources.

### Rule 8

Do not rely on GC for framework lifecycle.

### Rule 9

Keep MountHandle disposal safe and preferably idempotent.

### Rule 10

If a new feature creates a resource, document its owner and cleanup point.

---

## 92. Decision Test

Whenever implementation creates something during mount:

```text
Was it created by LumaFlow?
        ↓ yes
LumaFlow probably owns cleanup.

Was it supplied by application code?
        ↓ yes
LumaFlow usually borrows it.

Is it a subscription?
        ↓ yes
Which node/scope owns disposal?

Is it a child node?
        ↓ yes
Parent owns lifecycle.

Can mount fail after this point?
        ↓ yes
Ensure rollback path includes it.
```

---

## 93. Example: Button Lifecycle

Mount:

```text
Button Widget
↓
ButtonNode created
↓
native Button created
↓
callback registered
↓
theme applied
↓
attached to hierarchy
↓
Active
```

Unmount:

```text
ButtonNode → Unmounting
↓
binding scope disposed
↓
callback cleanup
↓
native Button detached
↓
references released
↓
Disposed
```

---

## 94. Example: Reactive Text Lifecycle

Mount:

```text
TextNode
↓
Label created
↓
initial State value read
↓
subscription registered in BindingScope
↓
Active
```

Unmount:

```text
TextNode → Unmounting
↓
BindingScope.Dispose()
↓
State subscription removed
↓
Label detached
↓
Disposed
```

---

## 95. Example: Structural Replacement

Initial:

```text
ReactiveBuilderNode
└── LoadingNode
```

State changes:

```text
ReactiveBuilderNode remains active
↓
LoadingNode unmounts completely
↓
ResultsNode mounts
```

No resources from LoadingNode survive unless externally owned.

---

## 96. Example: Root Mount

```csharp
using var handle = LumaFlow.Mount(
    new AudioLibView(),
    rootVisualElement
);
```

At scope end:

```text
handle.Dispose()
↓
entire LumaFlow subtree cleaned
↓
rootVisualElement itself remains alive
```

---

## 97. Initial Implementation Target

Phase 1 should implement enough lifecycle infrastructure to prove:

```text
MountHandle
WidgetNode lifecycle guard
child ownership
binding cleanup
native detach
failure rollback
```

before substantial component work begins.

---

## 98. Reconsideration Conditions

This ADR may be revised if:

1. general node pooling is introduced;
2. full reconciliation reuses nodes across updates;
3. virtualization requires broader lifecycle states;
4. async framework operations require cancellation scopes;
5. Unity UI Toolkit introduces lifecycle primitives LumaFlow can directly adopt.

Any revision must preserve deterministic ownership.

---

## 99. Final Decision

LumaFlow lifecycle is based on:

```text
explicit mount ownership
+
tree-based child ownership
+
deterministic unmount
+
automatic subscription/event cleanup
+
borrowed external resources
+
rollback on mount failure
```

The framework must be able to answer for every mounted object:

```text
Who owns this?
When does it die?
Who cleans it?
```

If those answers are unclear, the architecture is not complete.