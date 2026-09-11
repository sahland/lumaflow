# ADR-007: Separate Widget Description, Mounted Runtime Node, and Native VisualElement

- **Status:** Accepted
- **Decision date:** 2026-08-11
- **Scope:** Core widget model, lifecycle, ownership, mounting
- **Affects:** Runtime, Widget, WidgetNode, BuildContext, State, Rebuilds, Native Interop
- **Related documents:** `ARCHITECTURE.md`, `ADR-001-native-uitoolkit.md`, `ADR-003-reactive-state.md`, `ADR-004-reconciliation-strategy.md`

---

## 1. Context

LumaFlow needs a core representation for declarative UI.

The simplest possible implementation would be to let every public Widget directly own a `VisualElement`.

Example:

```csharp id="x7d961"
public abstract class Widget
{
    public VisualElement Element { get; }
}
```

This appears attractive because implementation is straightforward.

However, it creates several architectural problems:

- a Widget becomes tied to one native element;
- mounting the same Widget twice becomes ambiguous;
- runtime lifecycle becomes mixed with configuration;
- state subscriptions become attached to description objects;
- rebuilding becomes difficult;
- native element ownership becomes unclear;
- local mounted state cannot be cleanly separated;
- future reconciliation becomes harder;
- immutable widget descriptions become impossible or awkward.

LumaFlow therefore needs a clear separation between declarative description and mounted runtime state.

---

## 2. Decision

LumaFlow will use three distinct conceptual layers:

```text id="ey43r2"
Widget
    ↓
WidgetNode
    ↓
VisualElement
```

Where:

```text id="hq0s6a"
Widget
=
declarative description
```

```text id="8g8d4v"
WidgetNode
=
mounted runtime representation
```

```text id="6d2ufk"
VisualElement
=
native UI Toolkit object
```

These responsibilities must remain separate.

---

## 3. Widget Responsibility

A `Widget` describes what UI should exist.

A Widget may contain configuration such as:

```text id="0e7my9"
text
child
children
padding
variant
style
callbacks
state references
```

A Widget should not normally contain:

```text id="ps59pm"
mounted parent
native hierarchy ownership
subscription lists
BuildContext lifetime
current mount state
VisualElement references
disposal state
```

Those belong to the mounted runtime.

---

## 4. WidgetNode Responsibility

A `WidgetNode` represents one mounted occurrence of a Widget.

It owns runtime concerns such as:

```text id="u8etlu"
mount state
VisualElement reference
BuildContext
child nodes
binding scope
event subscriptions
lifecycle
native hierarchy attachment
update logic
subtree replacement
```

A WidgetNode is internal framework infrastructure.

Application developers should not normally interact with it.

---

## 5. VisualElement Responsibility

`VisualElement` remains the native UI Toolkit object responsible for:

```text id="xg5qps"
rendering participation
layout participation
event participation
focus
native hierarchy
native style
native control behavior
```

LumaFlow does not replace this responsibility.

---

## 6. Core Model

The conceptual runtime relationship is:

```text id="4cwl83"
Widget description
        ↓ create
WidgetNode
        ↓ create
VisualElement
        ↓ attach
UI Toolkit hierarchy
```

The exact factory methods may evolve.

The separation itself is architectural.

---

## 7. Widget Must Not Be the Mount

A Widget instance must not itself represent "currently mounted UI".

This distinction is important.

Example:

```csharp id="0jpysl"
var button = Button(
    "Save",
    onPressed: Save
);
```

The variable `button` is a description.

It is not:

```text id="mf1tv4"
the actual native Button currently on screen
```

until the runtime mounts it.

---

## 8. Multiple Mounts

The architecture should allow one Widget description to theoretically be mounted more than once if its semantics permit it.

Conceptually:

```text id="jd8g3p"
Widget
   ↙   ↘
Node A Node B
  ↓      ↓
VE A   VE B
```

Each mounted occurrence has independent runtime state.

This is one reason Widget must not own one VisualElement directly.

---

## 9. Widget Reuse

Even if application code usually creates fresh Widget instances, the architecture should not depend on object identity for correctness.

Example:

```csharp id="xcqbt7"
var divider = Divider();
```

Using the same declarative description in multiple places should not silently share mounted state.

---

## 10. Widget Mutability

Widget descriptions should preferably be immutable after construction.

Good:

```csharp id="m5lq4c"
var button = new Button(
    "Save",
    variant: ButtonVariant.Primary,
    onPressed: Save
);
```

Avoid:

```csharp id="uub4zw"
button.Text = "Delete";
button.Variant = ButtonVariant.Danger;
```

as the primary update mechanism.

Runtime UI changes should flow through:

```text id="ujx7hx"
State
bindings
controlled updates
structural rebuilds
```

not arbitrary mutation of Widget configuration after mount.

---

## 11. WidgetNode Creation

Each Widget type must provide or participate in a mechanism that creates an appropriate runtime node.

Conceptually:

```csharp id="zmzi9t"
internal abstract WidgetNode CreateNode();
```

or:

```csharp id="v0a1wm"
internal abstract WidgetNode CreateNode(
    BuildContext context
);
```

Exact API is deferred to implementation.

The public Widget API must not expose node construction unless there is a compelling extensibility reason.

---

## 12. WidgetNode Base Contract

Conceptual internal base:

```csharp id="78xpjc"
internal abstract class WidgetNode
{
    public Widget Widget { get; protected set; }

    public VisualElement VisualElement { get; protected set; }

    public BuildContext Context { get; protected set; }

    public abstract void Mount(
        VisualElement parent,
        BuildContext context
    );

    public abstract void Update(Widget widget);

    public abstract void Unmount();
}
```

This is illustrative only.

Implementation may split these concerns differently.

---

## 13. Node Owns Native Element Reference

Once mounted, the runtime node owns the reference to its native VisualElement.

Example:

```text id="qblp47"
ButtonNode
    ↓
UnityEngine.UIElements.Button
```

This ownership includes:

```text id="oea8ip"
configuration
event wiring
style application
binding updates
hierarchy attachment
```

but not arbitrary destruction of unrelated external resources.

---

## 14. Native Element Creation

For native-backed widgets, the WidgetNode creates the native element.

Example:

```text id="7fv2jl"
TextNode
    ↓
new Label(...)
```

```text id="lv7dqo"
ButtonNode
    ↓
new UnityEngine.UIElements.Button(...)
```

---

## 15. Layout Nodes

Layout widgets may also create native VisualElements.

Example:

```text id="4f34cf"
ColumnNode
    ↓
VisualElement
    ↓
flex-direction: column
```

This is acceptable.

Not every node must map 1:1 to a visually distinct element.

---

## 16. Structural-Only Nodes

Future optimizations may allow some Widgets to have runtime nodes without their own VisualElement.

Conceptually:

```text id="o90q1w"
ExpandedNode
    ↓
modifies child/parent flex semantics
    ↓
no extra native wrapper
```

This is optional.

The architecture must not require every WidgetNode to own exactly one VisualElement forever.

---

## 17. Node-to-Element Cardinality

Allowed future mappings include:

```text id="4w0d62"
1 WidgetNode
→ 1 VisualElement
```

or:

```text id="dbdmca"
1 WidgetNode
→ multiple VisualElements
```

or:

```text id="d5cjst"
1 WidgetNode
→ no dedicated VisualElement
```

where justified.

Therefore the architecture should avoid assuming strict 1:1 mapping in every internal API.

---

## 18. Primary Native Root

Even if a node internally uses several VisualElements, it may expose a primary root element for hierarchy attachment.

Conceptually:

```text id="8pp8mf"
WidgetNode
    ↓
RootVisualElement
    ├── child A
    └── child B
```

This can simplify mounting.

---

## 19. Child Ownership

A parent WidgetNode owns the mounted lifecycle of its child nodes.

Conceptually:

```text id="mc4ntn"
ColumnNode
├── TextNode
├── ButtonNode
└── CardNode
```

Unmounting `ColumnNode` must cause its owned child nodes to unmount.

---

## 20. Child Widget Descriptions

Parent Widget configuration may contain child Widgets.

Example:

```csharp id="z0r4h1"
Column(
    children:
    [
        Text("Hello"),
        Button("Save", onPressed: Save)
    ]
)
```

At mount time:

```text id="8ozjxc"
child Widgets
    ↓
child WidgetNodes
    ↓
child VisualElements
```

---

## 21. Runtime Child Collection

WidgetNode should maintain mounted child nodes in a runtime collection.

Conceptually:

```csharp id="hm6ckh"
List<WidgetNode>
```

or another optimized structure.

This is internal.

Application developers work with Widget children, not WidgetNode children.

---

## 22. BuildContext Ownership

Each mounted node receives a BuildContext associated with its location in the mounted tree.

Conceptually:

```text id="6cbrfc"
parent context
    ↓
child context
    ↓
WidgetNode
```

A Widget description should not permanently own a BuildContext.

Context belongs to the mount location.

---

## 23. Why Context Cannot Live in Widget

If a Widget can be mounted in two places:

```text id="dp9o58"
same Widget description
    ↓
Theme A context

same Widget description
    ↓
Theme B context
```

it must resolve differently.

Therefore:

```text id="gq7ndg"
Widget.Context
```

as persistent configuration is invalid architecture.

The mounted node receives context.

---

## 24. BindingScope Ownership

Each mounted node should have access to a lifecycle-owned binding scope.

Conceptually:

```text id="lobowk"
WidgetNode
    ↓
BindingScope
    ↓
subscriptions
```

When node unmounts:

```text id="wbs4kb"
BindingScope.Dispose()
```

This is required by ADR-003.

---

## 25. Event Ownership

Native events wired by LumaFlow belong to the WidgetNode lifecycle.

Example:

```text id="btdpfr"
ButtonNode mounts
    ↓
nativeButton.clicked += callback
```

Unmount:

```text id="9iozwj"
remove callback if necessary
```

The Widget description only stores the declared callback.

---

## 26. Widget Callbacks Are Configuration

Example:

```csharp id="zwr7jx"
Button(
    "Save",
    onPressed: Save
)
```

`Save` is declarative configuration.

The runtime node determines how that callback is attached to native UI Toolkit events.

---

## 27. Update Responsibility

If a mounted WidgetNode receives a compatible updated Widget description, the node may update itself in place.

Conceptually:

```text id="qrbohm"
old Button Widget
    ↓
ButtonNode
    ↓
new Button Widget
```

Node may update:

```text id="fr9hf0"
text
variant
enabled state
callback reference
```

without replacing the native button.

This supports future limited update/reconciliation mechanisms.

---

## 28. Update Does Not Imply Global Reconciliation

The existence of:

```csharp id="xxldn1"
WidgetNode.Update(...)
```

does not mean LumaFlow currently performs global widget-tree diffing.

It is an internal capability useful for:

```text id="51rrfj"
controlled updates
native list recycling
future reconciliation
component-specific updates
```

---

## 29. Widget Type Compatibility

If future node reuse occurs, a node can only update from a Widget type it understands.

Example:

```text id="ep7f5k"
ButtonNode
← Button
```

not:

```text id="z0ujfy"
ButtonNode
← Text
```

unless a specialized polymorphic node explicitly supports that behavior.

---

## 30. Node Mount State

A WidgetNode should have deterministic mount state.

Conceptual states:

```text id="44ru4f"
Created
Mounted
Unmounted
Disposed
```

Exact representation may be simpler.

The runtime must prevent invalid transitions.

---

## 31. Double Mount

This should be considered invalid:

```text id="frwhjh"
same WidgetNode
Mount()
Mount()
```

A Widget description may be mounted more than once by creating multiple nodes.

A WidgetNode itself represents one mounted occurrence.

---

## 32. Double Unmount

Unmount should be deterministic.

Implementation may:

```text id="6p4phx"
throw clear development exception
```

or:

```text id="bczbx9"
be safely idempotent
```

depending on final lifecycle policy.

The behavior must be consistent.

---

## 33. Node Lifetime

Conceptually:

```text id="qbkdsi"
Widget description
    ↓
Create node
    ↓
Mount node
    ↓
Use/update node
    ↓
Unmount node
    ↓
Dispose/release
```

A node should not be reused after final disposal unless explicitly designed for pooling.

---

## 34. Pooling Is Deferred

Do not add general WidgetNode pooling during MVP.

Reasons:

- lifecycle complexity;
- stale subscriptions;
- stale context;
- stale native state;
- debugging difficulty.

Native UI Toolkit virtualization may reuse specialized list nodes separately.

---

## 35. Native Interop Nodes

Wrapping an externally created VisualElement requires specialized ownership semantics.

Conceptually:

```csharp id="oeb25i"
Native(
    existingElement
)
```

The runtime creates:

```text id="xej64i"
NativeWidget
    ↓
NativeNode
    ↓
existing VisualElement
```

---

## 36. External VisualElement Ownership

By default, LumaFlow owns:

```text id="0izsxf"
hierarchy attachment
bindings added by LumaFlow
event adapters added by LumaFlow
```

It does not own arbitrary external resources associated with that VisualElement.

Unmount should normally detach it.

It should not perform unrelated destruction.

---

## 37. Existing Parent Validation

A native VisualElement supplied to LumaFlow may already have a parent.

This must be handled explicitly.

Possible behavior:

```text id="ag6hve"
throw descriptive exception
```

or:

```text id="yo6x0r"
allow explicit reparent option
```

Do not silently steal elements from another hierarchy.

---

## 38. Native Element Reuse

The same external VisualElement cannot normally be simultaneously mounted in two places.

Therefore:

```text id="j0m46d"
Native(existingElement)
```

has stricter reuse semantics than ordinary Widget descriptions.

Documentation must make that distinction clear.

---

## 39. StatelessView Runtime

A `StatelessView` is still a Widget description.

Its mounted node may:

```text id="nblmgs"
call Build(context)
    ↓
receive child Widget
    ↓
mount child WidgetNode
```

Conceptually:

```text id="y8boxu"
StatelessView
    ↓
ViewNode
    ↓
built child node
```

---

## 40. StatelessView Does Not Own Native Element Directly

A StatelessView may not have a dedicated native element.

It can be structural:

```text id="0vhu2f"
UserCard ViewNode
    ↓
CardNode
    ↓
VisualElement
```

This avoids unnecessary hierarchy wrappers.

---

## 41. StatefulView Runtime

A StatefulView may require a runtime node that owns mounted-local lifecycle state.

Conceptually:

```text id="3807xb"
StatefulView
    ↓
StatefulViewNode
    ├── local runtime state
    └── built child node
```

Exact public StatefulView API is still subject to validation.

---

## 42. Mounted Local State Belongs to Node

If local state is meant to exist per mount, it belongs conceptually to the node.

This ensures:

```text id="mob3he"
same StatefulView description
mounted twice
    ↓
two independent local states
```

rather than accidental sharing.

---

## 43. Widget Field State Warning

This pattern:

```csharp id="6164x8"
private readonly State<int> _count = new(0);
```

inside a reusable Widget description may share state across mounts.

Therefore LumaFlow must not automatically promise that all Widget fields represent mounted-local state.

This must be resolved through the eventual StatefulView API.

---

## 44. Native Element Naming

Nodes may assign useful names/classes to generated VisualElements for debugging.

Example:

```text id="f9wx8s"
lumaflow-button
lumaflow-column
```

Exact naming conventions may be defined later.

Generated hierarchy should remain understandable.

---

## 45. Native Element Classes

Framework classes may be useful for:

```text id="islyu0"
default USS
pseudo-state styling
debugging
component identification
```

These remain implementation details unless documented as stable extension points.

---

## 46. Node IDs

WidgetNodes may eventually have internal IDs for diagnostics.

Example:

```text id="ctl1lr"
Node #142
```

This can help with:

```text id="a0dfqg"
debug inspector
binding diagnostics
rebuild tracking
```

IDs should not become consumer identity semantics unless explicitly designed.

---

## 47. Widget Identity vs Node Identity

Important distinction:

```text id="muyq5h"
Widget object reference
≠
mounted node identity
```

Node identity belongs to one mounted occurrence.

Widget descriptions can be recreated.

---

## 48. Node Identity Across Rebuilds

Under current ADR-004 behavior, replacing a structural subtree typically creates new nodes.

Example:

```text id="kv203l"
old ResultsViewNode
    ↓ unmount

new ResultsViewNode
    ↓ mount
```

Node identity is not preserved automatically.

---

## 49. Future Reconciliation

If reconciliation is introduced later, node reuse may preserve identity.

This ADR supports that possibility by separating Widget from WidgetNode.

No reconciliation semantics are introduced here.

---

## 50. Node Parent Reference

WidgetNode may maintain an internal parent-node reference if required.

Potential uses:

```text id="20524a"
context lookup
lifecycle traversal
debugging
structural updates
```

Avoid relying on native VisualElement parent alone for all framework relationships, because some nodes may not map 1:1 to native elements.

---

## 51. Framework Tree vs Native Tree

LumaFlow may maintain a framework runtime tree distinct from the native tree.

Example:

```text id="lw8fb7"
WidgetNode tree:

PaddingNode
└── ExpandedNode
    └── TextNode
```

while native tree may be:

```text id="y82h1c"
VisualElement
└── Label
```

if structural nodes are optimized away.

Therefore framework hierarchy and VisualElement hierarchy must not be assumed identical.

---

## 52. Why Separate Trees Are Acceptable

The WidgetNode tree represents:

```text id="ka7t2u"
LumaFlow semantic/lifecycle structure
```

The VisualElement tree represents:

```text id="g1kfyi"
UI Toolkit native rendering/layout structure
```

They may differ while still mapping coherently.

---

## 53. Debug Mapping

Future developer tooling should ideally map:

```text id="8cm1et"
Widget
↔
WidgetNode
↔
VisualElement
```

so developers can inspect the relationship.

This separation should improve diagnostics rather than obscure them.

---

## 54. Node Disposal

A node must clean resources it owns.

Potential owned resources:

```text id="lae0qf"
subscriptions
event adapters
scheduled callbacks
temporary native resources
child nodes
```

The exact disposal mechanism must be deterministic.

---

## 55. Node Does Not Own External State

A WidgetNode subscribing to:

```csharp id="j9g7xh"
State<T>
```

owns only the subscription.

It does not own the external State unless an explicit ownership API says otherwise.

This follows ADR-003.

---

## 56. Node Does Not Own Theme

BuildContext may reference ThemeData.

The node does not automatically dispose shared theme objects.

---

## 57. Node Does Not Own Application Services

Contextual services passed through BuildContext remain externally owned unless explicitly documented.

---

## 58. Mount Root

LumaFlow must support mounting into an existing native root.

Conceptually:

```csharp id="oo9p08"
var handle = LumaFlow.Mount(
    new SettingsScreen(),
    rootVisualElement
);
```

The mount runtime creates:

```text id="y3hruu"
root WidgetNode
```

associated with that mount operation.

---

## 59. Mount Handle

A public mount operation may return a handle.

Potential:

```csharp id="ejvl4s"
public sealed class MountHandle : IDisposable
{
}
```

Disposing it would:

```text id="25zd01"
unmount root node
clean all descendants
```

Exact API is deferred.

---

## 60. Root Ownership

LumaFlow should not assume ownership of the external root VisualElement itself.

It owns the subtree it mounts into that root.

Example:

```text id="5ci7gq"
EditorWindow.rootVisualElement
    ↓ external owner

LumaFlow mounted subtree
    ↓ LumaFlow owner
```

---

## 61. Multiple LumaFlow Trees

A single native root may potentially contain multiple independent LumaFlow mount subtrees.

Example:

```text id="a92k40"
root
├── LumaFlow mount A
└── LumaFlow mount B
```

Each must have independent:

```text id="g23r00"
WidgetNode tree
BuildContext root
binding scopes
lifecycle
```

---

## 62. Multiple Windows

This model naturally supports multiple EditorWindows.

Each mount gets separate runtime nodes and context.

No global current widget tree is required.

---

## 63. Multiple Runtime Panels

The same applies to multiple runtime UI documents/panels.

Mount state remains local.

---

## 64. No Global Mount Singleton

Rejected:

```csharp id="isx3xe"
LumaFlowRuntime.Instance.CurrentRoot
```

as the fundamental architecture.

A global helper may exist for specific application frameworks later, but Core mounting is explicit and scoped.

---

## 65. Mounting Order

Parent nodes must establish sufficient runtime state before mounting children.

Conceptual sequence:

```text id="ctt1w1"
create node
↓
assign context
↓
create native root if needed
↓
attach/setup runtime ownership
↓
mount children
↓
activate bindings/events
```

Exact ordering must avoid exposing partially initialized nodes to callbacks.

---

## 66. Event During Mount

Native events should not accidentally invoke user callbacks while the node is only partially mounted.

Where native property initialization may emit change events, use silent setters where appropriate.

---

## 67. Unmount Order

Recommended conceptual order:

```text id="wxspml"
mark node unmounting
↓
unmount children
↓
dispose bindings/events
↓
detach native elements
↓
release references
↓
mark disposed/unmounted
```

Exact details may vary by component.

---

## 68. Parent Removal vs Explicit Unmount

Removing a VisualElement from the native hierarchy externally does not automatically guarantee LumaFlow lifecycle cleanup.

Therefore application code should not manipulate LumaFlow-owned native hierarchy directly.

The framework's mount handle/runtime should own removal.

---

## 69. External Hierarchy Mutation

If developers manually move/remove generated VisualElements through native UI Toolkit APIs, behavior may become undefined unless an explicit supported API exists.

Native escape hatches apply to interoperability, not arbitrary corruption of framework-owned hierarchy.

---

## 70. Public Native Access

If LumaFlow later exposes access to underlying VisualElement:

```csharp id="9bzcso"
node.VisualElement
```

or a callback such as:

```csharp id="phzlx8"
onCreated: element => ...
```

it must clearly document ownership boundaries.

Do not expose internal WidgetNode solely for native access.

---

## 71. Native Customization Hook

A future safe pattern may be:

```csharp id="j4tb4v"
Button(
    "Save",
    configureNative: button =>
    {
        ...
    },
    onPressed: Save
)
```

This is not yet accepted.

Such APIs must be reviewed carefully because they can bypass theme and lifecycle assumptions.

---

## 72. Extending Widget

Third-party developers should eventually be able to implement custom LumaFlow Widgets.

The extension contract must not require them to manipulate unrelated runtime internals.

Potential public/protected abstractions may include:

```text id="prpj19"
NativeWidget<TElement>
SingleChildWidget
MultiChildWidget
StatelessView
```

Exact API should emerge after Core implementation.

---

## 73. Custom Widget Safety

Public extensibility must preserve:

```text id="clc30v"
mount lifecycle
binding cleanup
context
native ownership
child ownership
```

Do not simply expose raw WidgetNode implementation and expect plugin authors to get every invariant right.

---

## 74. NativeWidget Base

A future abstraction may simplify native-backed components:

```csharp id="m50j6u"
public abstract class NativeWidget<TElement> : Widget
    where TElement : VisualElement
{
}
```

with runtime infrastructure handling common lifecycle.

This is a possible optimization, not yet a required API.

---

## 75. SingleChildWidget Base

Likewise, common hierarchy patterns may eventually use:

```text id="7lh46k"
SingleChildWidget
MultiChildWidget
```

to reduce internal duplication.

Do not expose them publicly until their semantics are stable.

---

## 76. Avoid Giant WidgetNode

The base WidgetNode must not become a god object owning every framework subsystem.

Prefer composed internal services:

```text id="f10nrk"
BindingScope
StyleResolver
Context handling
child mounting helpers
```

where useful.

Keep base lifecycle coherent.

---

## 77. Avoid WidgetNode Per Feature Layer

Do not create unnecessary chains like:

```text id="zsslry"
ButtonNode
→ ReactiveButtonNode
→ StyledReactiveButtonNode
→ ThemeAwareStyledReactiveButtonNode
```

Prefer composition over deep runtime inheritance.

---

## 78. Runtime Inheritance

Some inheritance is natural:

```text id="w8qzn8"
WidgetNode
├── SingleChildNode
├── MultiChildNode
└── NativeControlNode<T>
```

but depth should remain modest.

---

## 79. Public Widget Inheritance

Public framework Widgets may use shared abstract bases internally, but consumer code should not need to understand complex inheritance trees.

---

## 80. Error Diagnostics

Lifecycle errors should identify both Widget and node where useful.

Example:

```text id="z3mfh5"
LumaFlow: ButtonNode cannot be mounted because it is already mounted.
Widget: Button("Save")
```

Exact diagnostics may evolve.

---

## 81. Source Location

Future tooling may capture source information for Widgets.

Potential benefit:

```text id="4g3m97"
Widget tree inspector
↓
click widget
↓
navigate to source
```

This is optional developer tooling and should not affect Core architecture.

---

## 82. Runtime Performance

The WidgetNode layer introduces runtime objects.

This cost is accepted because it provides:

- lifecycle ownership;
- subscription ownership;
- context;
- structural identity;
- future update capability.

However, nodes should remain lightweight.

---

## 83. Allocation Strategy

WidgetNodes are typically allocated at mount time, not every frame.

Ordinary State property updates should not allocate new nodes.

This aligns with ADR-003.

---

## 84. Structural Rebuild Allocation

Local subtree rebuilds may allocate new:

```text id="ksepo4"
Widgets
WidgetNodes
VisualElements
```

only inside the rebuilt boundary.

This is accepted initially.

---

## 85. Node Pooling for Lists

Native virtualized list integration may require specialized node reuse.

This should be designed separately.

Do not generalize list recycling into global WidgetNode pooling prematurely.

---

## 86. Rejected Alternative: Widget Owns VisualElement

Rejected:

```csharp id="9zsecl"
class Button : Widget
{
    public UnityEngine.UIElements.Button Element;
}
```

Reasons:

- one mount only;
- lifecycle/configuration mixed;
- no independent context;
- state subscription ambiguity;
- poor reuse;
- poor future reconciliation support.

---

## 87. Rejected Alternative: Return VisualElement Directly

Rejected as the primary Widget API:

```csharp id="kl5bt0"
VisualElement Build(BuildContext context)
```

for all public views.

Reason:

This makes application code declarative only superficially.

It removes framework-level semantics for:

```text id="tf8y9p"
state bindings
node lifecycle
theme updates
structural composition
future reconciliation
```

Widgets should build Widgets, not immediately collapse everything to native elements.

---

## 88. Rejected Alternative: Only Widget and VisualElement

Rejected conceptual model:

```text id="ac8crh"
Widget
    ↓
VisualElement
```

with runtime state attached through dictionaries or side tables.

Reason:

A dedicated runtime node provides clearer ownership and lifecycle.

---

## 89. Rejected Alternative: VisualElement Subclasses as Widgets

Rejected as the primary framework model:

```csharp id="bfy8gu"
public class LumaButton : VisualElement
```

for every public Widget.

Custom VisualElements remain useful internally and for native extension.

But making all Widgets native element subclasses would merge description and runtime concerns.

---

## 90. Rejected Alternative: Global Widget Registry

Rejected:

```text id="xbnxt7"
Widget ID
    ↓
global dictionary
    ↓
VisualElement
```

as the core ownership model.

Mount trees should own their own runtime nodes.

Global registries may exist only for diagnostics if needed.

---

## 91. Architectural Invariants Created by This ADR

Unless superseded:

### Invariant 1

Widget is a declarative description.

### Invariant 2

WidgetNode is a mounted runtime representation.

### Invariant 3

VisualElement is the native UI Toolkit representation.

### Invariant 4

Widget does not directly own mount lifecycle.

### Invariant 5

WidgetNode owns binding/event lifecycle.

### Invariant 6

BuildContext belongs to mounted location, not Widget description.

### Invariant 7

One Widget description may conceptually create multiple mounted nodes.

### Invariant 8

One WidgetNode represents one mounted occurrence.

### Invariant 9

Framework tree and native VisualElement tree may differ.

### Invariant 10

Structural runtime identity belongs to nodes, not Widget object references.

---

## 92. Codex Rules

### Rule 1

Do not add persistent `VisualElement` fields to ordinary public Widget descriptions.

### Rule 2

Do not store BuildContext permanently inside Widget configuration.

### Rule 3

Mount-specific subscriptions belong to WidgetNode or its owned scopes.

### Rule 4

Do not use Widget object identity as mounted identity.

### Rule 5

Do not create a global current node tree.

### Rule 6

Keep root mount ownership explicit.

### Rule 7

If a Widget can be structural-only, do not force a native wrapper purely because the current base class assumes one.

### Rule 8

Do not expose WidgetNode publicly for implementation convenience.

### Rule 9

Preserve native VisualElement interoperability through deliberate adapters.

### Rule 10

When extending lifecycle, update the WidgetNode model rather than pushing mount state into Widget.

---

## 93. Decision Test

When adding runtime data, ask:

```text id="wdf2pb"
Is this immutable/declarative configuration?
        ↓ yes
Widget.

Does this exist only while mounted?
        ↓ yes
WidgetNode.

Is this native UI Toolkit rendering/layout state?
        ↓ yes
VisualElement.

Does it depend on mount location/context?
        ↓ yes
WidgetNode.

Does it survive independent mounts intentionally?
        ↓ maybe
external/shared state, not mount-local Widget field by default.
```

---

## 94. Example: Button

Consumer:

```csharp id="xkps9z"
Button(
    "Save",
    onPressed: Save
)
```

Description:

```text id="dqbhum"
Button Widget
├── text = "Save"
└── callback = Save
```

Runtime:

```text id="53jofm"
ButtonNode
├── context
├── binding scope
└── native button
```

Native:

```text id="myzi4n"
UnityEngine.UIElements.Button
```

---

## 95. Example: Column

Consumer:

```csharp id="7xrpr4"
Column(
    gap: 12,
    children:
    [
        Text("A"),
        Text("B")
    ]
)
```

Description:

```text id="fm1kvv"
Column Widget
├── gap
└── child Widgets
```

Runtime:

```text id="52i0rs"
ColumnNode
├── TextNode
└── TextNode
```

Native:

```text id="9zd9nl"
VisualElement
├── Label
└── Label
```

---

## 96. Example: StatelessView

```csharp id="4umlws"
public sealed class Header : StatelessView
{
    public override Widget Build(BuildContext context)
    {
        return Text(
            "Audio Library",
            style: context.Theme.Typography.TitleLarge
        );
    }
}
```

Conceptual runtime:

```text id="axd1tx"
Header Widget
    ↓
StatelessViewNode
    ↓ Build(context)
Text Widget
    ↓
TextNode
    ↓
Label
```

Header does not require an extra VisualElement wrapper.

---

## 97. Example: Native Element Adapter

```csharp id="9k4z0i"
Native(
    customElement
)
```

Conceptual:

```text id="is85sg"
NativeWidget
    ↓
NativeNode
    ↓
customElement
```

LumaFlow manages mount attachment and its own bindings.

External ownership remains explicit.

---

## 98. Initial Implementation Target

The first Core implementation should prove:

```text id="3r9ai5"
Widget
WidgetNode
MountHandle
BuildContext
BindingScope

Text
Button
Column
Native
```

with deterministic:

```text id="vmc859"
mount
child mounting
state/context assignment
unmount
cleanup
```

before adding broad component coverage.

---

## 99. Reconsideration Conditions

Revisit this ADR only if:

1. WidgetNode proves unnecessary in a working prototype;
2. node allocation cost becomes a demonstrated major performance problem;
3. UI Toolkit lifecycle changes substantially;
4. another model provides equally clear ownership with less complexity;
5. future reconciliation requires refinements to node semantics.

The description/runtime/native separation should remain unless strong evidence disproves it.

---

## 100. Final Decision

LumaFlow's core runtime model is:

```text id="2ry0ik"
Widget
    =
what UI should exist

WidgetNode
    =
the mounted LumaFlow runtime instance

VisualElement
    =
the native Unity UI Toolkit object
```

This separation provides the foundation for:

```text id="jsau5l"
declarative composition
reactive bindings
BuildContext
lifecycle
cleanup
native interoperability
local rebuilding
future reconciliation
```

without turning public Widget objects into mutable UI Toolkit wrappers.

This model is the foundation of `LumaFlow.Core`.