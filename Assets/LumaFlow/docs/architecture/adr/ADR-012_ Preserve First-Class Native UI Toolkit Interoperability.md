# ADR-012: Preserve First-Class Native UI Toolkit Interoperability

- **Status:** Accepted
- **Decision date:** 2026-08-11
- **Scope:** Native UI Toolkit integration, custom VisualElements, UXML, USS, escape hatches
- **Affects:** Runtime, Editor, Widget model, Lifecycle, Styling, Extensibility, Migration
- **Related documents:** `ADR-001-native-uitoolkit.md`, `ADR-002-code-first.md`, `ADR-007-widget-runtime-model.md`, `ADR-008-lifecycle-and-ownership.md`, `ADR-010-styling-model.md`

---

## 1. Context

LumaFlow is built on top of Unity UI Toolkit.

It must therefore coexist with:

```text
VisualElement
Label
Button
TextField
ListView
ScrollView
custom VisualElement subclasses
UXML
USS
Editor UI Toolkit controls
third-party UI Toolkit controls
```

A framework that hides native UI Toolkit completely would create several problems:

- existing projects would be difficult to migrate;
- custom native controls would become unusable;
- third-party UI Toolkit packages would become harder to integrate;
- developers would lose important Unity APIs;
- UXML/USS assets would become isolated;
- unsupported framework features would become blockers.

LumaFlow must provide a deliberate native escape hatch.

---

## 2. Decision

Native UI Toolkit interoperability is a first-class architectural requirement.

LumaFlow must support both directions:

```text
Native UI Toolkit
        ↓
embedded inside LumaFlow
```

and:

```text
LumaFlow
        ↓
mounted inside existing UI Toolkit hierarchy
```

The relationship is cooperative rather than exclusive.

---

## 3. Core Principle

LumaFlow should make common UI easier without preventing advanced native usage.

The intended progression is:

```text
LumaFlow high-level API
        ↓
lower-level LumaFlow API
        ↓
native UI Toolkit
```

A developer should never need to abandon the framework merely because one screen requires a custom `VisualElement`.

---

## 4. Native Widget

LumaFlow should provide an explicit adapter for native UI Toolkit elements.

Conceptual API:

```csharp
Native(
    customElement
)
```

Example:

```csharp
return Column(
    children:
    [
        Text("Profiler"),

        Native(
            new CustomProfilerElement()
        )
    ]
);
```

The exact public name may evolve.

The capability is required.

---

## 5. Native Adapter Model

Conceptually:

```text
NativeWidget
    ↓
NativeWidgetNode
    ↓
existing VisualElement
```

The adapter participates in:

```text
LumaFlow hierarchy
BuildContext location
mount lifecycle
parent ownership
native hierarchy attachment
```

without pretending that the underlying element was created by LumaFlow.

---

## 6. Borrowed Ownership

An externally supplied `VisualElement` is borrowed by default.

Example:

```csharp
var element = new CustomVisualElement();

var widget = Native(element);
```

LumaFlow does not own arbitrary destruction of `element`.

Unmount normally means:

```text
detach from LumaFlow hierarchy
+
remove LumaFlow-owned bindings/callbacks
```

not:

```text
destroy the external object
```

---

## 7. Native Element Parent

A `VisualElement` cannot normally exist in two native hierarchy locations simultaneously.

Therefore a supplied element with an existing parent must be handled deliberately.

Default preferred behavior:

```text
existing parent detected
        ↓
throw descriptive error
```

Do not silently remove it from its existing hierarchy.

---

## 8. Explicit Reparenting

A future API may allow explicit reparenting.

Conceptually:

```csharp
Native(
    element,
    allowReparent: true
)
```

or another clearly named mechanism.

This should not be the default.

Reparenting changes external hierarchy ownership and must be intentional.

---

## 9. Same Native Element Mounted Twice

This is invalid:

```text
Native(element)
        ↓ mount A

Native(element)
        ↓ mount B
```

at the same time.

LumaFlow should detect this where practical or allow UI Toolkit to report a clear hierarchy error.

---

## 10. Native Factory

For reusable native components, a factory-based adapter may be safer.

Conceptually:

```csharp
Native(
    create: () => new CustomVisualElement()
)
```

This allows:

```text
one Widget description
        ↓
multiple mounts
        ↓
multiple native elements
```

The exact API should be decided during implementation.

---

## 11. Existing Instance vs Factory

These are different semantics:

```text
Native(existingElement)
=
borrow this exact instance
```

```text
Native(() => new CustomElement())
=
create one instance per mount
```

If both APIs exist, the distinction must be obvious.

---

## 12. Custom Native Controls

Developers must be able to integrate native subclasses:

```csharp
public sealed class WaveformElement : VisualElement
{
    ...
}
```

inside LumaFlow.

Example:

```csharp
Card(
    child: Native(
        new WaveformElement()
    )
)
```

No special inheritance from LumaFlow should be required.

---

## 13. Third-Party Controls

The same applies to third-party UI Toolkit controls.

If a library exposes:

```text
SomeVendorGraph : VisualElement
```

it should be embeddable through the same Native adapter.

LumaFlow must not require third-party authors to know about LumaFlow.

---

## 14. Native Elements Participate in Layout

A Native element should behave like any normal native child.

Example:

```csharp
Row(
    children:
    [
        Expanded(
            child: Native(graph)
        ),

        InspectorPanel()
    ]
)
```

UI Toolkit remains responsible for the layout.

No special layout bridge should be required.

---

## 15. Native Element Styling

Externally supplied elements may already have:

```text
USS classes
inline styles
custom stylesheets
native internal hierarchy
```

LumaFlow must not reset these indiscriminately.

---

## 16. Do Not Normalize External Elements

Forbidden default behavior:

```text
Native(element)
↓
clear classes
clear styles
reset focus
replace internal hierarchy
```

The adapter should interfere as little as possible.

---

## 17. Optional LumaFlow Styling Around Native

If developers want LumaFlow styling, they can compose around it.

Example:

```csharp
Container(
    padding: EdgeInsets.All(16),
    decoration: BoxDecoration(
        color: context.Theme.Colors.Surface
    ),
    child: Native(customElement)
)
```

This is preferable to mutating the native control unexpectedly.

---

## 18. Native Configuration Hooks

A future API may allow safe native configuration.

Potential:

```csharp
Native(
    create: () => new CustomElement(),
    configure: element =>
    {
        element.focusable = true;
    }
)
```

This remains optional.

Do not expose configuration hooks if they create unclear lifecycle semantics.

---

## 19. Native Access from Framework Widgets

Some users may need the underlying native element of a LumaFlow component.

Examples:

```text
focus control
custom UI Toolkit APIs
vendor integration
debug tooling
native experimental APIs
```

LumaFlow should eventually provide a deliberate mechanism.

Do not expose WidgetNode as the solution.

---

## 20. Possible Native Ref

A future API may use a reference abstraction.

Conceptually:

```csharp
var buttonRef = new NativeRef<UnityEngine.UIElements.Button>();

Button(
    "Save",
    nativeRef: buttonRef
)
```

Then while mounted:

```csharp
buttonRef.Value?.Focus();
```

The exact API is deferred.

---

## 21. Native Ref Lifecycle

A native reference must follow mount lifecycle.

Conceptually:

```text
before mount
Value = null

mounted
Value = native element

unmounted
Value = null
```

This prevents stale native access.

---

## 22. No Permanent Native Reference on Widget

Do not implement:

```csharp
button.VisualElement
```

on Widget descriptions.

This violates ADR-007 because Widgets are not mounted runtime instances.

---

## 23. UXML Interoperability

Existing `VisualTreeAsset` content must be usable inside LumaFlow.

Conceptually:

```csharp
Uxml(
    asset: settingsTemplate
)
```

or:

```csharp
Native(
    settingsTemplate.CloneTree()
)
```

The exact convenience API is secondary.

---

## 24. UXML Is Cloned Per Mount

A `VisualTreeAsset` is a template.

Each independent mount should normally create a new native hierarchy.

Conceptually:

```text
VisualTreeAsset
      ↓ CloneTree()
VisualElement hierarchy A

VisualTreeAsset
      ↓ CloneTree()
VisualElement hierarchy B
```

Do not reuse one cloned hierarchy across independent mounts.

---

## 25. UXML Wrapper

A dedicated `Uxml` widget may eventually provide:

```text
asset cloning
optional root query
optional stylesheet integration
mount lifecycle
```

if that produces enough value over `Native(asset.CloneTree())`.

Do not add it solely for API count.

---

## 26. Existing UXML Application

LumaFlow must be mountable inside a hierarchy created by UXML.

Example:

```text
UIDocument
    ↓
VisualTreeAsset
    ↓
existing root VisualElement
    ↓
LumaFlow.Mount(...)
```

This enables gradual migration.

---

## 27. Hybrid UXML Screen

A UXML screen may contain a named host element.

Example:

```xml
<ui:VisualElement name="luma-host" />
```

Application code can obtain it and mount LumaFlow into it.

Conceptually:

```csharp
var host = root.Q("luma-host");

LumaFlow.Mount(
    new SettingsPanel(),
    host
);
```

This is a supported architectural pattern.

---

## 28. LumaFlow with UXML Subtrees

The reverse is also supported:

```text
LumaFlow screen
├── LumaFlow header
├── cloned UXML subtree
└── LumaFlow footer
```

No architectural boundary prevents this.

---

## 29. USS Interoperability

USS must remain usable for both:

```text
LumaFlow-generated VisualElements
external native VisualElements
```

This follows ADR-010.

---

## 30. User USS Classes

LumaFlow widgets should allow user classes where appropriate.

Conceptually:

```csharp
Card(
    classes:
    [
        "inventory-card"
    ],
    child: ...
)
```

These classes exist on the generated native element.

---

## 31. Stable USS Extension Points

Framework-generated classes should only be documented as public extension points intentionally.

Once documented, renaming them may become a breaking change.

Internal classes should not accidentally become API contracts.

---

## 32. Stylesheet Scope

Existing `StyleSheet` assets may be attached at:

```text
mount root
subtree scope
native element
```

depending on user needs and native UI Toolkit semantics.

LumaFlow should provide enough escape hatches without inventing a parallel stylesheet system.

---

## 33. UQuery

Developers may still use UI Toolkit's query APIs on native hierarchies where needed.

Example:

```csharp
root.Q<Button>("save-button");
```

LumaFlow does not forbid this.

However, LumaFlow component logic should prefer typed composition/state rather than requiring queries for normal behavior.

---

## 34. Querying LumaFlow-Generated Elements

If users query generated native elements, they may become coupled to framework hierarchy details.

Therefore only documented:

```text
names
classes
native refs
extension points
```

should be considered stable.

Internal wrapper hierarchy may evolve.

---

## 35. Element Names

LumaFlow may allow assigning a native UI Toolkit name.

Potential:

```csharp
Button(
    "Save",
    name: "save-button"
)
```

This can aid:

```text
USS selectors
testing
native queries
debugging
```

Exact base-widget API should remain concise.

---

## 36. Name Semantics

`name` should map directly to native `VisualElement.name` where a widget has a clear native root.

For structural-only widgets, behavior must be documented or unsupported.

Do not create hidden wrapper elements solely to satisfy `name`.

---

## 37. Native Events

External elements may expose native UI Toolkit events.

Application code may subscribe directly when necessary.

However, if the subscription is created as part of LumaFlow mounting, its cleanup should be tied to lifecycle.

---

## 38. Native Event Adapter

A helper may eventually simplify lifecycle-safe event registration.

Conceptually:

```csharp
Native(
    create: () => new CustomElement(),
    onMount: (element, scope) =>
    {
        scope.RegisterCallback<SomeEvent>(element, OnEvent);
    }
)
```

Exact API is deferred.

The lifecycle principle is mandatory.

---

## 39. Do Not Hide Native Events Behind Reflection

If adapting a native control, use strongly typed event APIs.

Avoid reflection-based discovery of arbitrary control events.

---

## 40. Focus Interop

Native elements must remain part of the same UI Toolkit focus system.

A Native wrapper must not isolate them from:

```text
keyboard navigation
focus events
tab navigation
```

unless explicitly requested.

---

## 41. Pointer Interop

Likewise, pointer input remains native UI Toolkit input.

LumaFlow must not proxy all pointer events through a separate event layer.

---

## 42. Drag-and-Drop

Editor UI often uses native drag-and-drop behavior.

Custom/native controls requiring it must remain compatible.

LumaFlow should not intercept drag events globally.

---

## 43. Context Menus

Native contextual menus should remain usable.

Future LumaFlow ContextMenu APIs may wrap native mechanisms but must coexist with native controls.

---

## 44. Native ListView

LumaFlow's future `ListView<T>` should adapt native UI Toolkit `ListView`.

This is itself a form of native interoperability.

Do not recreate virtualization simply to keep implementation "pure LumaFlow."

---

## 45. Native ScrollView

Likewise, ScrollView should use the native control rather than a custom scroll implementation.

---

## 46. Editor Controls

Editor-only controls may exist under:

```text
UnityEditor.UIElements
```

LumaFlow.Editor may wrap or embed them.

Runtime assemblies must not reference them.

This follows `COMPATIBILITY.md`.

---

## 47. Editor Native Adapter

`Native(...)` in Editor context may accept editor-only VisualElement subclasses because the call occurs from Editor assemblies.

Core Native abstractions should not themselves depend on `UnityEditor`.

---

## 48. Runtime Native Adapter

Runtime uses the same conceptual adapter with runtime-compatible `VisualElement` types.

This keeps API semantics consistent.

---

## 49. Native Interop Across Render Pipelines

Ordinary native UI Toolkit interoperability is independent from:

```text
Built-in
URP
HDRP
```

This follows ADR-006.

---

## 50. Custom Rendering Elements

A custom `VisualElement` may implement specialized rendering behavior.

LumaFlow may embed it.

That does not mean the rendering feature becomes part of LumaFlow Core.

Example:

```csharp
Native(
    new CustomMeshElement()
)
```

The external element owns its specialized rendering semantics.

---

## 51. Experimental UI Toolkit APIs

Advanced developers may use newer UI Toolkit APIs unavailable in LumaFlow's high-level wrappers.

Native interop allows this without requiring LumaFlow to immediately expose every API.

This is strategically important for framework longevity.

---

## 52. Progressive Adoption

Interop enables:

```text
existing UI Toolkit project
↓
replace one control
↓
replace one panel
↓
introduce Theme
↓
introduce State
↓
migrate larger surfaces
```

LumaFlow must support incremental migration rather than requiring all-or-nothing adoption.

---

## 53. Progressive Escape

The reverse also matters.

A LumaFlow project can use native UI Toolkit for one advanced area without abandoning LumaFlow elsewhere.

Example:

```text
LumaFlow application
├── declarative settings
├── declarative navigation
├── native node graph editor
└── declarative toolbar
```

---

## 54. Interop Must Not Create a Second Lifecycle

Native wrappers must participate in standard LumaFlow lifecycle.

Do not create a separate NativeWidget lifecycle model.

The same:

```text
Mount
Unmount
BindingScope
Ownership
```

rules apply.

---

## 55. Interop Must Not Create a Second Context System

Native elements embedded in LumaFlow do not automatically gain BuildContext awareness.

If native code needs contextual values, application/framework adapters may pass them explicitly.

Do not attach arbitrary LumaFlow context objects globally to every VisualElement.

---

## 56. Context-Aware Custom Components

If a user wants a reusable component that consumes Theme/Context, the preferred abstraction is a LumaFlow Widget/View wrapping the native element.

Example:

```csharp
public sealed class Waveform : StatelessView
{
    public override Widget Build(BuildContext context)
    {
        var element = new WaveformElement
        {
            AccentColor = context.Theme.Colors.Primary
        };

        return Native(element);
    }
}
```

This keeps BuildContext semantics in LumaFlow.

---

## 57. Native Control Wrappers

Frequently used native controls may receive dedicated LumaFlow wrappers.

Example:

```text
Unity TextField
        ↓
LumaFlow TextField
```

The wrapper provides:

```text
theme
State<T> binding
typed options
lifecycle-safe callbacks
```

while retaining the native control underneath.

---

## 58. Wrapper Value Test

A native control should only receive a dedicated LumaFlow wrapper when the wrapper adds meaningful value.

Good reasons:

```text
reactive binding
theme integration
semantic API
common configuration
lifecycle handling
```

Bad reason:

```text
every VisualElement must have a LumaFlow class
```

---

## 59. Low-Level Native Escape Hatch

`Native` should remain available even when a dedicated wrapper exists.

Example:

```text
LumaFlow TextField
```

covers common cases.

But a developer may still use:

```csharp
Native(
    customConfiguredTextField
)
```

when they need native-specific behavior.

---

## 60. No Closed Component Ecosystem

LumaFlow must not require every usable UI element to be registered in a central component catalog.

Any valid compatible `VisualElement` should remain embeddable.

---

## 61. No Proprietary Base Class Requirement

Third-party controls do not need to inherit:

```text
LumaVisualElement
LumaControl
LumaNativeElement
```

to work inside LumaFlow.

Standard UI Toolkit inheritance remains sufficient.

---

## 62. Public Widget Extensibility

For deeper integration, LumaFlow may later expose supported custom-widget base classes.

Possible:

```text
NativeWidget<TElement>
SingleChildWidget
MultiChildWidget
```

This is distinct from the simple Native escape hatch.

---

## 63. Escape Hatch vs Extension API

These solve different problems:

```text
Native(...)
=
embed existing native UI quickly
```

```text
custom LumaFlow Widget
=
build reusable framework-level component
```

Both are needed.

---

## 64. Custom Widget Contract

A future public extension API must provide safe access to:

```text
native element creation
style application
child mounting
bindings
lifecycle scope
```

without exposing all internal WidgetNode implementation.

---

## 65. Do Not Expose WidgetNode Directly

Third-party extension pressure must not result in:

```csharp
public abstract class WidgetNode
```

solely because it is convenient.

WidgetNode is core runtime infrastructure.

Create narrower extension abstractions.

---

## 66. Native Hierarchy Mutation

Application code that obtains a LumaFlow-generated native root must not arbitrarily mutate framework-owned child hierarchy and expect LumaFlow to remain consistent.

Example dangerous operation:

```csharp
element.Clear();
```

on a LumaFlow-owned container.

This may remove child VisualElements without unmounting WidgetNodes.

---

## 67. Ownership Boundary

Safe rule:

```text
You may mutate native state you explicitly own.

Do not mutate framework-owned hierarchy structure behind LumaFlow's back.
```

This boundary must be documented.

---

## 68. Native Style Mutation

Direct style mutation is less dangerous than hierarchy mutation, but can conflict with LumaFlow.

If LumaFlow owns a style property, a future reactive/theme update may overwrite native manual changes.

This follows ADR-010.

---

## 69. Native Class Mutation

Adding a user class is generally safe.

Removing LumaFlow-owned classes may break component behavior.

Internal classes should therefore be treated as framework-owned unless documented.

---

## 70. Native Event Mutation

Adding independent callbacks is generally possible.

Removing framework-owned callbacks through unsupported native manipulation may break component behavior.

---

## 71. Native Control Internal Hierarchy

Many UI Toolkit controls have internal child structures.

LumaFlow wrappers should avoid depending on undocumented internal element names/classes where possible.

Use supported public APIs first.

---

## 72. Unity Version Changes

Native internal hierarchy may change between Unity versions.

This is another reason LumaFlow must avoid brittle native queries inside wrappers.

---

## 73. Native Feature Adapters

If version-specific UI Toolkit behavior is required, isolate it behind compatibility helpers.

Do not scatter Unity-version conditionals through all components.

---

## 74. UXML Queries

When wrapping UXML, the application may need named elements.

Example:

```csharp
var root = template.CloneTree();

var save = root.Q<Button>("save");
```

This remains valid.

LumaFlow does not prohibit native query-based code inside explicitly native integration surfaces.

---

## 75. UXML Binding

A future helper may connect `State<T>` to elements inside a cloned UXML hierarchy.

Conceptually:

```text
UXML subtree
+
LumaFlow BindingScope
```

This could aid migration.

It is not required for MVP.

---

## 76. UXML Controller Pattern

Existing UXML/controller architecture may continue inside a Native wrapper.

LumaFlow should not force immediate rewrite.

---

## 77. LumaFlow State with Native Controls

A future low-level binding helper may allow:

```csharp
Bind(
    state,
    nativeField,
    ...
)
```

inside custom widget implementations.

This should reuse the same BindingScope infrastructure as framework controls.

---

## 78. Native Two-Way Binding

For native value controls implementing UI Toolkit value semantics, LumaFlow may provide generic adapters.

Potential:

```text
State<T>
↔
INotifyValueChanged<T>
```

This could dramatically simplify custom native component integration.

---

## 79. Generic Value Binding

A future internal/public helper may conceptually support:

```csharp
BindValue(
    state,
    field,
    setWithoutNotify: ...
)
```

The exact contract must avoid feedback loops under ADR-003.

---

## 80. Do Not Require Reflection for Native Binding

Use generic interfaces and strongly typed callbacks where available.

Avoid runtime reflection to discover value properties.

---

## 81. Testing Native Interop

Required test areas include:

```text
embed native VisualElement
native element receives layout
unmount detaches native element
external element remains alive
existing parent rejection
same instance ownership behavior
UXML CloneTree integration
USS class preservation
native event cleanup
multiple mounts using factory
```

---

## 82. UXML Integration Test

Test:

```text
VisualTreeAsset
↓
clone/mount in LumaFlow
↓
interact with native Button
↓
unmount
```

Verify hierarchy cleanup is correct.

---

## 83. Hybrid Root Test

Create native hierarchy:

```text
root
├── native header
├── host
└── native footer
```

Mount LumaFlow into host.

Dispose LumaFlow.

Verify:

```text
header remains
footer remains
host remains
LumaFlow subtree removed
```

---

## 84. Native Instance Test

Mount a borrowed element.

Dispose mount.

Verify:

```text
element.parent == null
```

and the element can be attached elsewhere afterward.

---

## 85. Factory Multi-Mount Test

If Native factory API exists:

```text
same Native Widget description
↓
mount A
↓
mount B
```

Verify two independent VisualElement instances exist.

---

## 86. User Class Preservation Test

Apply user classes to LumaFlow wrapper/native element.

Perform reactive/theme updates.

Verify user classes remain.

---

## 87. Focus Test

Embed a focusable native control between LumaFlow controls.

Verify keyboard focus navigation remains functional.

---

## 88. Editor Interop Test

Embed an Editor-only UI Toolkit control in a LumaFlow Editor view.

Verify Runtime assembly remains free from Editor references.

---

## 89. Migration Documentation

Documentation should include:

```text
Using LumaFlow in an existing UIDocument
Embedding UXML inside LumaFlow
Embedding custom VisualElement controls
Using USS with LumaFlow
Accessing native UI Toolkit when necessary
```

Interop should not be treated as an obscure edge case.

---

## 90. Public Messaging

LumaFlow should clearly state:

```text
Native UI Toolkit underneath.
Native UI Toolkit always available.
```

This reduces fear of framework lock-in.

---

## 91. Rejected Alternative: Hide VisualElement Completely

Rejected.

Reasons:

- blocks advanced UI Toolkit use;
- creates framework lock-in;
- makes migration harder;
- requires LumaFlow to wrap every possible API.

---

## 92. Rejected Alternative: Make Users Convert Native Controls

Rejected architecture:

```text
VisualElement
↓
special registration
↓
generated Luma wrapper
```

for every custom control.

A direct adapter is simpler.

---

## 93. Rejected Alternative: Own External Elements Fully

Rejected default:

```text
Native(element)
↓
LumaFlow becomes responsible for destroying element/resources
```

Ownership transfer should never be implicit.

---

## 94. Rejected Alternative: Silent Reparenting

Rejected.

Moving external UI unexpectedly is difficult to debug and may violate assumptions of the original owner.

---

## 95. Rejected Alternative: UXML Unsupported

Rejected because it would:

- block existing projects;
- block UI Builder workflows;
- make gradual adoption harder;
- unnecessarily reject native UI Toolkit capabilities.

---

## 96. Rejected Alternative: UXML as Primary Runtime Representation

Also rejected under ADR-002.

Interop does not change LumaFlow's code-first primary model.

---

## 97. Rejected Alternative: Mirror Every Native API

Rejected.

LumaFlow does not need wrappers for every:

```text
VisualElement method
style property
event
experimental API
```

Native interoperability is the escape hatch for uncommon cases.

---

## 98. Rejected Alternative: Allow Arbitrary Mutation of Framework Tree

Rejected.

Direct native access must not invalidate WidgetNode ownership.

Native interoperability does not mean ownership rules disappear.

---

## 99. Architectural Invariants Created by This ADR

Unless superseded:

### Invariant 1

Any compatible native VisualElement can be embedded in LumaFlow.

### Invariant 2

LumaFlow can mount into existing native UI Toolkit hierarchy.

### Invariant 3

UXML remains interoperable.

### Invariant 4

USS remains interoperable.

### Invariant 5

Externally supplied native elements are borrowed by default.

### Invariant 6

Existing native parents are not silently replaced.

### Invariant 7

Native interop follows normal LumaFlow lifecycle.

### Invariant 8

WidgetNode is not exposed merely for native access.

### Invariant 9

Framework-owned native hierarchy must not be mutated arbitrarily from outside.

### Invariant 10

Native interoperability is part of the framework contract, not an implementation accident.

---

## 100. Codex Rules

### Rule 1

Do not implement a feature in a way that prevents embedding ordinary custom VisualElements.

### Rule 2

Do not destroy externally supplied VisualElements during normal unmount.

### Rule 3

Do not silently reparent externally supplied native elements.

### Rule 4

Do not reset external classes/styles unless explicitly required by the adapter contract.

### Rule 5

Use normal WidgetNode lifecycle for Native wrappers.

### Rule 6

Do not expose internal WidgetNode to solve native interoperability.

### Rule 7

Do not require UXML users to rewrite existing screens before adopting LumaFlow.

### Rule 8

Do not create wrappers for every native API solely for conceptual purity.

### Rule 9

Do not allow native escape hatches to bypass subscription cleanup or ownership rules.

### Rule 10

When native UI Toolkit already solves an advanced use case, preserve access to it rather than recreating it unnecessarily.

---

## 101. Decision Test

When LumaFlow does not expose a required capability:

```text
Does UI Toolkit already expose it?
        ↓ yes
Can the user access it safely through Native interop?
        ↓ yes
Use the escape hatch.

Is this capability used frequently enough to justify a LumaFlow wrapper?
        ↓ yes
Design a semantic wrapper.

Would the wrapper merely mirror the native API?
        ↓ yes
Probably keep it native.
```

---

## 102. Example: Custom Waveform

```csharp
public sealed class AudioWaveform : StatelessView
{
    public required AudioClip Clip { get; init; }

    public override Widget Build(BuildContext context)
    {
        var waveform = new WaveformElement
        {
            Clip = Clip,
            AccentColor = context.Theme.Colors.Primary
        };

        return Container(
            height: 160,
            decoration: BoxDecoration(
                color: context.Theme.Colors.Surface
            ),
            child: Native(waveform)
        );
    }
}
```

LumaFlow owns composition.

The custom element owns waveform rendering.

UI Toolkit remains the shared backend.

---

## 103. Example: Existing UXML

```csharp
public sealed class LegacyInspector : StatelessView
{
    public required VisualTreeAsset Template { get; init; }

    public override Widget Build(BuildContext context)
    {
        return Native(
            Template.CloneTree()
        );
    }
}
```

This allows existing UI to participate in a LumaFlow screen.

---

## 104. Example: LumaFlow Inside Existing UI Toolkit

```csharp
public void CreateGUI()
{
    var host = rootVisualElement.Q<VisualElement>(
        "lumaflow-host"
    );

    _mount = LumaFlow.Mount(
        new AudioLibView(),
        host
    );
}
```

The surrounding window may remain native/UXML-based.

---

## 105. Example: Mixed Hierarchy

LumaFlow:

```csharp
return Column(
    gap: 12,
    children:
    [
        Toolbar(),

        Expanded(
            child: Native(
                new GraphView()
            )
        ),

        StatusBar()
    ]
);
```

Native hierarchy remains a normal UI Toolkit hierarchy.

---

## 106. Initial MVP Requirement

Before calling the Core mounting model stable, prove at minimum:

```text
Native(existing VisualElement)
Native element mount/unmount
external ownership preservation
LumaFlow mount into arbitrary VisualElement root
user USS classes
custom VisualElement inside Row/Column
```

UXML convenience wrappers may follow shortly after.

---

## 107. Long-Term Direction

Native interoperability may later include:

```text
native references
safe native configuration hooks
generic value bindings
UXML widgets
StyleSheet scopes
custom Widget authoring base classes
UI Builder integration
```

These should extend the same ownership model rather than create parallel architecture.

---

## 108. Reconsideration Conditions

Revisit this ADR if:

1. UI Toolkit significantly changes its custom-control model;
2. Native adapter ownership proves too restrictive;
3. public custom-widget authoring requires deeper runtime access;
4. UI Builder becomes important enough to require stronger integration;
5. native element reuse/recycling requires revised semantics.

The first-class interoperability requirement itself should remain.

---

## 109. Final Decision

LumaFlow is not a closed UI ecosystem.

Its interoperability model is:

```text
LumaFlow
↔
Unity UI Toolkit
```

Developers may:

```text
embed native controls
embed UXML
use USS
mount into native trees
use third-party VisualElements
drop to native APIs when necessary
```

without abandoning the framework.

The guiding rule is:

**LumaFlow should remove UI Toolkit boilerplate, not remove UI Toolkit itself.**