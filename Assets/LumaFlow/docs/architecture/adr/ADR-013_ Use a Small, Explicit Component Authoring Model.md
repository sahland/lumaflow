# ADR-013: Use a Small, Explicit Component Authoring Model

- **Status:** Accepted
- **Decision date:** 2026-08-11
- **Scope:** Reusable components, public widget authoring, native-backed controls, composite views
- **Affects:** Runtime, Public API, Widget hierarchy, Extensibility, Documentation, Third-Party Components
- **Related documents:** `API_DESIGN.md`, `ADR-002-code-first.md`, `ADR-007-widget-runtime-model.md`, `ADR-011-build-context-and-scoping.md`, `ADR-012-native-interoperability.md`

---

## 1. Context

LumaFlow must support reusable UI components created by:

```text
LumaFlow itself
application developers
third-party package authors
```

Possible component authoring approaches include:

```text
StatelessView
StatefulView
direct Widget subclasses
native-backed Widgets
VisualElement subclasses
single-child abstractions
multi-child abstractions
builder callbacks
composition functions
```

Without a clear model, the framework could develop several overlapping patterns for solving the same problem.

That would make:

- APIs inconsistent;
- documentation difficult;
- third-party extensions unpredictable;
- lifecycle behavior harder to reason about;
- internal implementation unnecessarily complex.

LumaFlow therefore needs a deliberately small component-authoring vocabulary.

---

## 2. Decision

LumaFlow will distinguish between four primary authoring categories:

```text
1. Stateless composite views
2. Stateful mounted views
3. Native-backed widgets
4. Structural/layout widgets
```

Application developers should primarily use composition through `StatelessView` and the future `StatefulView` model.

Direct low-level Widget implementation is reserved for framework authors and advanced component developers.

---

## 3. Authoring Hierarchy

The intended abstraction ladder is:

```text
StatelessView / StatefulView
        ↓
LumaFlow Widgets
        ↓
Native-backed Widget abstractions
        ↓
Widget runtime infrastructure
        ↓
VisualElement
```

Use the highest abstraction that cleanly solves the problem.

---

## 4. StatelessView

`StatelessView` is the default abstraction for reusable components that:

- compose other Widgets;
- do not require mounted-local mutable state;
- may read BuildContext;
- may accept State<T> owned elsewhere;
- may expose callbacks and immutable configuration.

Example:

```csharp
public sealed class UserCard : StatelessView
{
    public required User User { get; init; }

    public override Widget Build(BuildContext context)
    {
        return Card(
            child: Column(
                gap: context.Theme.Spacing.S,
                children:
                [
                    Text(
                        User.Name,
                        style: context.Theme.Typography.TitleMedium
                    ),

                    Text(
                        User.Email,
                        style: context.Theme.Typography.BodyMedium
                    )
                ]
            )
        );
    }
}
```

---

## 5. Stateless Does Not Mean Static

A StatelessView may consume reactive values.

Example:

```csharp
public sealed class CounterLabel : StatelessView
{
    public required State<int> Count { get; init; }

    public override Widget Build(BuildContext context)
    {
        return Text(
            value: Count,
            format: value => $"Count: {value}"
        );
    }
}
```

The View itself owns no mounted-local mutable state.

The passed `State<int>` is externally owned.

---

## 6. StatelessView Responsibility

A StatelessView may contain:

```text
immutable configuration
child widgets
callbacks
state references
models
BuildContext reads
composition logic
```

It should not normally contain:

```text
mounted VisualElement references
BindingScope
WidgetNode references
mount flags
native callback registrations
mounted-local lifetime data
```

Those belong to runtime nodes.

---

## 7. StatelessView Build Contract

`Build(BuildContext context)` returns a Widget description.

Conceptually:

```text
StatelessView
    ↓ Build(context)
Widget
```

It does not return:

```text
VisualElement
WidgetNode
```

This preserves the declarative runtime model from ADR-007.

---

## 8. Build Should Be Side-Effect Light

`Build()` should primarily describe UI.

Avoid performing:

```text
network requests
file writes
database mutations
analytics submission
scene changes
resource destruction
```

inside Build.

Build may read current configuration/context and construct Widgets.

---

## 9. Build Must Not Depend on One-Time Execution

Application code must not assume:

```text
Build() runs exactly once.
```

Even though LumaFlow does not currently rebuild the entire tree for every state change, a view may be rebuilt because of:

```text
structural replacement
future reconciliation
responsive boundary
navigation
theme/context changes where appropriate
development tooling
```

Therefore Build must remain safe to invoke multiple times.

---

## 10. No Subscription Creation in Build

This is dangerous:

```csharp
public override Widget Build(BuildContext context)
{
    Count.Changed += OnCountChanged;

    return Text(...);
}
```

because Build may execute multiple times.

Reactive subscriptions should be created through LumaFlow bindings or mounted lifecycle infrastructure.

---

## 11. Configuration as Properties

Reusable view configuration should use strongly typed properties.

Example:

```csharp
public sealed class AudioCard : StatelessView
{
    public required AudioClip Clip { get; init; }

    public bool ShowDuration { get; init; } = true;

    public Action? OnPlay { get; init; }
}
```

This supports readable object-initializer APIs where appropriate.

---

## 12. Constructor vs Properties

Small Widgets may use constructors for concise essential values.

Example:

```csharp
Text("Settings")
```

Views with many meaningful options may use properties or named parameters.

The rule is not:

```text
everything must use constructors
```

or:

```text
everything must use object initializers
```

The rule is:

```text
optimize for readable consumer code.
```

---

## 13. Required Configuration

Use C# `required` properties where supported by the declared Unity/C# compatibility level and where they improve safety.

Example:

```csharp
public required AudioClip Clip { get; init; }
```

Do not introduce custom runtime validation for information the compiler can enforce reliably.

---

## 14. Optional Configuration

Optional properties should have sensible defaults.

Example:

```csharp
public bool ShowIcon { get; init; } = true;
```

Avoid constructors with many optional positional parameters.

---

## 15. Immutable Configuration

Public component configuration should preferably be immutable after construction.

Use:

```text
init-only properties
readonly fields
constructor arguments
```

instead of mutable setters.

This keeps Widgets descriptive.

---

## 16. Callback Configuration

Callbacks are part of Widget configuration.

Example:

```csharp
public Action? OnPressed { get; init; }
```

or public factory syntax:

```csharp
Button(
    "Save",
    onPressed: Save
)
```

Mounted nodes adapt callbacks to native events.

---

## 17. No Controller Class Requirement

A reusable LumaFlow component should not normally require:

```text
Widget
+
Controller
+
Binder
+
Presenter
```

for simple behavior.

Composition and reactive State should cover ordinary cases.

Application architectures may still use controllers/view models where desired.

---

## 18. StatefulView

A stateful abstraction is required for components with mounted-local state.

Examples:

```text
expanded/collapsed state
local selected tab
temporary hover-independent interaction state
internal animation progress
form field local draft state
```

However, StatefulView semantics must preserve ADR-007:

```text
mounted-local state belongs to mounted runtime
```

not the reusable Widget description.

---

## 19. StatefulView Must Not Share Mount-Local State Accidentally

This is unsafe as a guaranteed pattern:

```csharp
public sealed class Counter : StatefulView
{
    private readonly State<int> _count = new(0);
}
```

because one Widget description could theoretically be mounted multiple times.

Mounted-local state must be created per mounted instance.

---

## 20. Stateful Runtime Model

Conceptually:

```text
StatefulView Widget
        ↓
StatefulViewNode
        ├── mounted local state
        └── built child subtree
```

The exact public API may evolve.

---

## 21. StatefulView API Is Deliberately Deferred

This ADR accepts the need for a stateful mounted component model.

It does not yet commit to:

```text
Flutter State<TWidget>
Hooks
context.UseState()
separate State object classes
```

The final API should be defined after basic runtime lifecycle is implemented.

---

## 22. StatefulView Must Reuse Core State Infrastructure

Local state should integrate with:

```text
State<T>
BindingScope
WidgetNode lifecycle
structural rebuild boundaries
```

Do not build an unrelated reactive system solely for StatefulView.

---

## 23. Native-Backed Widgets

A native-backed Widget represents a semantic LumaFlow component implemented using a native `VisualElement`.

Examples:

```text
Text
Button
TextField
Toggle
Slider
ListView
ScrollView
```

Conceptually:

```text
LumaFlow Button
        ↓
ButtonNode
        ↓
UnityEngine.UIElements.Button
```

---

## 24. Native-Backed Widget Responsibility

A native-backed component typically owns:

```text
native element creation
initial native configuration
state bindings
event adapters
theme/style resolution
native property updates
```

Its runtime node owns actual mount state.

---

## 25. Native-Backed Widgets Are Not VisualElements

Public API should normally not be:

```csharp
public sealed class Button : UnityEngine.UIElements.Button
```

because that merges declarative configuration with mounted native state.

This follows ADR-007.

---

## 26. Native Widget Base Abstraction

An internal or eventually protected helper may exist:

```csharp
NativeWidget<TElement>
    where TElement : VisualElement
```

Conceptually it may standardize:

```text
native creation
native root access
mount/update/unmount
style handling
lifetime ownership
```

This should be introduced only when repeated implementation demonstrates value.

---

## 27. Do Not Prematurely Expose NativeWidget<T>

The first implementation may keep this abstraction internal.

Public extension APIs should be designed after several real native-backed components exist.

Avoid freezing an immature base class.

---

## 28. Structural Widgets

Structural Widgets affect LumaFlow composition/context but may not need their own native element.

Examples:

```text
Theme
ReactiveBuilder
future Provider<T>
Expanded in an optimized implementation
```

Conceptually:

```text
ThemeNode
    ↓
derived BuildContext
    ↓
child node
```

with no native wrapper required.

---

## 29. Layout Widgets

Layout Widgets are typically semantic wrappers around native layout.

Examples:

```text
Row
Column
Padding
Stack
Container
```

They may use a VisualElement as their native root.

Their public purpose is layout/composition, not generic native control behavior.

---

## 30. Single-Child Widgets

Many widgets have exactly one child:

```text
Padding
Center
Align
Container
Theme
Expanded
```

An internal reusable abstraction may represent:

```text
SingleChildWidget
```

to reduce duplicated child validation and mount logic.

---

## 31. Multi-Child Widgets

Widgets such as:

```text
Row
Column
Stack
```

own multiple child descriptions.

An internal reusable abstraction may represent:

```text
MultiChildWidget
```

or equivalent.

---

## 32. Child Base Classes Are Implementation Helpers

Do not expose `SingleChildWidget` or `MultiChildWidget` publicly merely because they exist internally.

They become public only if third-party component authoring clearly benefits from them.

---

## 33. Composition Over Inheritance

Application developers should primarily build reusable UI through composition.

Preferred:

```csharp
public sealed class EmptyState : StatelessView
{
    public override Widget Build(BuildContext context)
    {
        return Center(
            child: Column(
                children:
                [
                    Icon(Icons.Search),
                    Text("Nothing found")
                ]
            )
        );
    }
}
```

rather than subclassing:

```text
Column
Container
Button
```

to customize their appearance.

---

## 34. Do Not Subclass Button for Variants

Bad pattern:

```text
PrimaryButton
DangerButton
SecondaryButton
GhostButton
```

implemented as inheritance purely for visual variants.

Prefer:

```csharp
Button(
    variant: ButtonVariant.Danger
)
```

or reusable composition when semantics genuinely differ.

---

## 35. When a New Component Type Is Justified

Create a reusable component when it encapsulates a meaningful combination of:

```text
semantic behavior
repeated composition
state binding
theme behavior
interaction
domain UI intent
```

Not merely to wrap one arbitrary style literal.

---

## 36. Domain Components

Application-level components are encouraged.

Examples:

```text
AudioClipCard
TrackRow
UserAvatar
InventorySlot
QuestPanel
```

These should normally be StatelessView/StatefulView composition.

They do not belong in LumaFlow Core unless they are broadly reusable UI primitives.

---

## 37. Framework Components

LumaFlow Core components should remain domain-neutral.

Good Core concepts:

```text
Card
Button
TextField
Tooltip
Dropdown
ListView
Dialog
```

Poor Core concepts:

```text
InventorySlot
CharacterHealthPanel
AudioTrackInspector
QuestCard
```

These belong to applications or domain packages.

---

## 38. Semantic Component API

A component should expose semantic configuration.

Good:

```csharp
Button(
    "Delete",
    variant: ButtonVariant.Danger
)
```

Less desirable:

```csharp
Button(
    textColor: Color.white,
    backgroundColor: Color.red,
    borderColor: Color.red,
    ...
)
```

for standard use.

Generic style override may still exist.

---

## 39. Avoid Boolean Explosion

Bad:

```csharp
Button(
    primary: false,
    secondary: false,
    danger: true,
    outlined: false,
    compact: true
)
```

Prefer enums/value objects:

```csharp
Button(
    variant: ButtonVariant.Danger,
    size: ButtonSize.Compact
)
```

---

## 40. Semantic Options Before Escape Hatches

For common configuration, provide typed semantic parameters.

For advanced native behavior, preserve native escape hatches.

Do not expose every `VisualElement` property directly through every Widget constructor.

---

## 41. Shared Widget Configuration

Some capabilities apply broadly:

```text
name
USS classes
maybe tooltip
maybe native reference
```

These may justify a shared base configuration abstraction.

Keep the common surface small.

---

## 42. Avoid Giant Widget Base Class

Do not put every possible feature on `Widget`.

Bad base:

```text
Width
Height
Padding
Margin
Color
Font
Border
Tooltip
Focusable
TabIndex
Animation
OnClick
OnHover
...
```

Different widgets have different semantics.

Shared configuration should only contain genuinely universal concerns.

---

## 43. Widget Base Responsibility

The base `Widget` should remain minimal.

Conceptually it may primarily support:

```text
runtime node creation
basic identity metadata in future
common native metadata if justified
```

Most API should live on semantic widget types.

---

## 44. Style Parameter Strategy

Avoid giving every Widget every style parameter individually.

Possible approaches include:

```text
specific semantic parameters
+
optional generic Style/Decoration
```

Example:

```csharp
Container(
    padding: ...,
    decoration: ...
)
```

while Text uses:

```csharp
Text(
    "...",
    style: TextStyle(...)
)
```

---

## 45. Do Not Invent One Universal Style Object Prematurely

A universal:

```csharp
WidgetStyle
```

with hundreds of optional fields may recreate CSS poorly.

Use focused types and widget-specific semantics first.

---

## 46. Child Naming

Public component APIs should consistently use:

```text
child
children
```

for nested Widgets.

Avoid synonyms such as:

```text
content
body
items
```

for generic containment unless the component has domain-specific semantic regions.

---

## 47. Semantic Slots

Some components legitimately have multiple semantic child slots.

Example:

```csharp
Dialog(
    title: Text("Delete item?"),
    content: Text("This cannot be undone."),
    actions:
    [
        Button("Cancel"),
        Button("Delete")
    ]
)
```

Here `title`, `content`, and `actions` are meaningful semantic roles.

This is preferable to forcing everything into one generic `children` list.

---

## 48. Required Child

If a component cannot function without content, require it.

Example:

```text
Expanded requires child
Padding requires child
```

Fail early when required content is missing.

---

## 49. Nullable Children

Do not silently accept null child entries inside ordinary child collections unless conditional composition explicitly supports them.

Bad:

```csharp
children:
[
    Text("A"),
    null,
    Text("B")
]
```

Prefer explicit filtering or future conditional helpers.

---

## 50. Conditional Composition

A future ergonomic helper may allow conditional children safely.

Potential:

```csharp
If(
    condition,
    child
)
```

or normal C# collection construction.

Do not weaken child collection invariants to support convenience.

---

## 51. Collection Expressions

If supported by the minimum C# version, prefer:

```csharp
children:
[
    Text("A"),
    Text("B")
]
```

for readability.

Compatibility policy determines whether examples can use this syntax.

---

## 52. Widget Factories

The target syntax may use helper factory functions:

```csharp
Text(...)
Row(...)
Button(...)
```

instead of explicit:

```csharp
new Text(...)
new Row(...)
new Button(...)
```

if this materially improves readability.

However, implementation should not rely on obscure C# tricks.

---

## 53. Optional UI Static DSL

A future:

```csharp
UI.Column(...)
UI.Text(...)
UI.Button(...)
```

may be offered if naming collisions become a real problem.

It should be syntax sugar over the same Widget types.

Do not create parallel behavior.

---

## 54. Naming Collisions

Names such as:

```text
Button
TextField
Slider
```

may collide with Unity types.

Documentation and namespace design must make imports manageable.

Potential namespace:

```text
LumaFlow.UI
```

may allow ergonomic usage.

Exact namespace architecture should be decided separately.

---

## 55. Widget Suffix

Public components should generally not require names such as:

```text
ButtonWidget
TextWidget
CardWidget
```

unless necessary to avoid severe ambiguity.

Concise semantic nouns are preferred.

---

## 56. Internal Node Suffix

Runtime types should use explicit internal names:

```text
ButtonNode
TextNode
ColumnNode
ThemeNode
```

because their runtime role differs from public Widgets.

---

## 57. Public Component File Structure

A component may be organized as:

```text
Button/
├── Button.cs
├── ButtonTheme.cs
└── Internal/
    ├── ButtonNode.cs
    └── ButtonStyleResolver.cs
```

or another structure.

Do not split tiny components unnecessarily.

---

## 58. File Decomposition Rule

Split component implementation when there are genuinely separate concerns:

```text
public API
runtime node
theme model
style resolver
complex behavior
```

Do not create five files for a 40-line component.

---

## 59. Native Event Translation

A public component should expose semantic callbacks.

Example:

```text
onPressed
onChanged
onSubmitted
```

rather than raw native events for ordinary usage.

The native-backed implementation translates these.

---

## 60. Raw Native Events Remain Available Through Interop

If an advanced user needs:

```text
PointerDownEvent
GeometryChangedEvent
FocusInEvent
```

native interop provides access.

The public semantic component API need not mirror every native event.

---

## 61. Value Components

Input components should clearly model controlled data.

Example:

```csharp
TextField(
    value: username
)
```

where `username` is `State<string>`.

This is preferable to hidden mutable fields inside the component.

---

## 62. Uncontrolled Components

Where useful, a component may support:

```text
initialValue
```

for internal local control state.

Controlled and uncontrolled APIs must remain distinct.

---

## 63. Do Not Mix Value Ownership Ambiguously

Bad:

```csharp
TextField(
    value: "Alex"
)
```

if it is unclear whether later edits are owned internally or externally.

The API must communicate state ownership.

---

## 64. Component-Owned Native State

Some native control state may remain internal when it is purely implementation detail.

Examples:

```text
hover state
pressed state
scrollbar internals
native cursor state
```

This does not need to become public State<T>.

---

## 65. Component-Owned Semantic State

If state materially affects application semantics and users need to observe/control it, expose an appropriate state/callback API.

Do not hide useful application state inside native controls unnecessarily.

---

## 66. Context Usage

Components may read:

```text
Theme
Navigator
MediaQuery
```

from BuildContext when those values are inherently scoped.

Do not pass them manually through every component constructor.

---

## 67. Do Not Hide Arbitrary Dependencies in Context

A domain component requiring:

```text
IAudioDatabase
```

should normally receive it through application architecture, constructor/property configuration, or a deliberate external DI system.

Do not add it to BuildContext for convenience.

---

## 68. Component Theming

Reusable framework components should integrate with `ThemeData`.

Example:

```text
Button
↓
ButtonTheme
```

Application-level domain components may compose themed primitives rather than requiring dedicated global theme entries.

---

## 69. Custom Component Themes

If a third-party reusable component requires its own theme data, future ThemeExtension infrastructure may support it.

Do not expand Core ThemeData for every package component.

---

## 70. Native Root Requirement

A custom framework Widget may have:

```text
one native root
multiple native roots
no dedicated native root
```

depending on semantics.

Public component design should not assume 1:1 Widget-to-VisualElement mapping.

This follows ADR-007.

---

## 71. Wrapper-Free Composite Views

StatelessView and StatefulView should normally avoid adding native wrappers just because they are components.

Example:

```text
UserCard ViewNode
    ↓
CardNode
```

No extra `UserCard VisualElement` is required.

---

## 72. Semantic Wrapper Only When Needed

A native wrapper is justified when the component needs:

```text
layout ownership
background
clipping
focus
input surface
USS scope
native control behavior
```

Do not create wrappers solely to mirror component boundaries.

---

## 73. Public Extensibility Levels

Long term, LumaFlow may support three extension levels:

```text
Level 1:
StatelessView / StatefulView

Level 2:
native-backed custom Widget helper

Level 3:
advanced framework extension APIs
```

Most users should remain at Level 1.

---

## 74. Level 1 Must Be Powerful

Application developers should be able to build most custom UI without learning:

```text
WidgetNode
mount internals
binding scopes
native lifecycle internals
```

This is a core usability goal.

---

## 75. Level 2 Native Components

Third-party authors building controls around custom VisualElements may need:

```text
native creation
binding registration
theme access
typed state binding
cleanup scope
```

A supported abstraction should eventually provide these capabilities safely.

---

## 76. Level 3 Must Remain Rare

Direct framework-runtime extension should be required only for unusual infrastructure such as:

```text
special structural widgets
virtualization adapters
context providers
navigation infrastructure
```

Do not make ordinary component authors work at this level.

---

## 77. Composition Functions

Simple reusable UI may also be expressed as functions.

Example:

```csharp
Widget BuildHeader(string title)
{
    return Text(title);
}
```

This is valid C# composition.

Not every reusable fragment needs a class.

---

## 78. When to Use a Function

Functions are useful when:

```text
the fragment is simple
no distinct type identity is useful
no mounted lifecycle is needed
no dedicated API/documentation is needed
```

---

## 79. When to Use StatelessView

Prefer StatelessView when the component has:

```text
meaningful reusable identity
multiple configuration properties
context use
documentation value
non-trivial composition
```

---

## 80. When to Use StatefulView

Use StatefulView when the component genuinely owns mounted-local state.

Do not choose it simply because the component reacts to externally provided State<T>.

---

## 81. When to Use Native Widget

Use native-backed Widget implementation when the component needs direct control over a native UI Toolkit control or VisualElement lifecycle.

Examples:

```text
LumaFlow Button
LumaFlow TextField
custom waveform control
specialized graph
```

---

## 82. When to Use Native(...)

Use `Native(...)` when:

```text
a native element already exists
integration is local
a dedicated reusable LumaFlow wrapper adds little value
```

---

## 83. Decision Matrix

```text
Does it only compose LumaFlow Widgets?
        ↓ yes
StatelessView.

Does it need mounted-local state?
        ↓ yes
StatefulView.

Does it wrap/control a native VisualElement?
        ↓ yes
Native-backed Widget.

Is it a one-off native integration?
        ↓ yes
Native(...).

Does it alter framework context/tree semantics?
        ↓ yes
Advanced structural Widget.
```

---

## 84. Public Documentation Must Teach This Order

Custom component documentation should begin with:

```text
1. StatelessView
2. StatefulView
3. Native integration
4. Advanced custom Widgets
```

Do not begin by teaching users runtime internals.

---

## 85. Component API Stability

Public component constructors/properties are part of LumaFlow's API surface.

Before adding a parameter, ask whether it is:

```text
common
semantic
stable
meaningful across themes
```

Avoid exposing internal implementation details.

---

## 86. Avoid Parameter Explosion

If a component constructor reaches dozens of options, consider:

```text
focused style object
theme
variant enum
configuration object
splitting responsibilities
```

Do not continue adding unrelated parameters indefinitely.

---

## 87. Configuration Objects

A focused configuration object is acceptable for complex concepts.

Example:

```text
TextFieldValidation
ListViewSelectionOptions
```

Do not create `ButtonOptions` merely to avoid five readable named parameters.

---

## 88. Generic Components

Use generics where they provide strong type value.

Example:

```csharp
ListView<T>
Dropdown<T>
State<T>
```

Avoid generic parameters that exist only to make implementation abstract.

---

## 89. Generic Constraints

Public generic constraints should be minimal and meaningful.

Do not force domain models to inherit framework interfaces unless needed.

Example:

```text
ListView<T>
```

should ideally accept arbitrary model types.

---

## 90. Model-to-Widget Builders

Collection controls may use:

```csharp
itemBuilder: item => AudioCard(item)
```

or typed view factories.

This is preferable to reflection-based template discovery.

---

## 91. Builders Are Structural Boundaries

A builder that dynamically creates child Widgets must follow lifecycle/rebuild rules from ADR-004 and ADR-008.

Do not invoke arbitrary builders every frame.

---

## 92. Builder Purity

Builder callbacks should generally behave like Build():

```text
construct description
avoid persistent side effects
```

---

## 93. Component Equality

Do not require every component to implement deep equality during MVP.

Full reconciliation is deferred.

Style/value types may still implement equality where useful.

---

## 94. Keys

Do not add key parameters to every custom component yet.

Widget identity semantics remain deferred under ADR-004.

---

## 95. Source Generation

Component authoring must not require source generators.

Future optional generators may reduce boilerplate.

Basic custom components remain ordinary C#.

---

## 96. Reflection

Custom component discovery must not depend on runtime reflection for basic usage.

A component exists because application code references it directly.

---

## 97. Registration

Do not require:

```csharp
LumaFlow.RegisterComponent<MyCard>();
```

for ordinary component usage.

Direct typed composition is sufficient.

---

## 98. Global Component Registry

Rejected as a foundational architecture.

There is no need for a global registry to render statically referenced Widget types.

---

## 99. Serialization

Reusable components do not need to be Unity-serialized objects.

Do not derive:

```text
StatelessView
StatefulView
Widget
```

from `ScriptableObject` or `MonoBehaviour` by default.

---

## 100. Editor Inspectability

Future tooling may provide component previews or inspectors.

This must not force the core component model to become serialized.

Tooling adapts to the code-first model.

---

## 101. Previewability

Good custom components should eventually be renderable in a component gallery with supplied sample props/context.

This encourages deterministic composition.

---

## 102. Testing Components

Stateless components should be mount-testable using:

```text
known BuildContext
known State values
known callbacks
```

without full application bootstrapping.

---

## 103. Native Component Tests

Native-backed components should verify:

```text
native type created
bindings work
callback translation works
theme applied
cleanup occurs
```

---

## 104. Stateful Component Tests

Once StatefulView exists, tests must verify:

```text
local state unique per mount
state disposed on unmount
multiple mount isolation
state survives allowed local updates
```

---

## 105. Multi-Mount Safety

Component authoring must never assume a description instance is globally unique.

This includes:

```text
StatelessView
StatefulView
native-backed Widgets
```

unless an explicit single-use API says otherwise.

---

## 106. No Static Mutable Component State

Framework components must not use static mutable fields for mounted state.

Example forbidden:

```csharp
private static bool _isHovered;
```

This breaks multiple components and mount trees.

---

## 107. Static Immutable Defaults

Static immutable values are valid.

Examples:

```text
default EdgeInsets
default BorderRadius
shared immutable style constants
```

where safe.

---

## 108. Services Inside Components

Framework components should depend only on framework/runtime abstractions required for their semantic behavior.

Do not make Button depend on:

```text
application DI
analytics
sound systems
scene manager
```

Those concerns belong outside Core.

---

## 109. Behavior Composition

Application developers can compose behavior through callbacks.

Example:

```csharp
Button(
    "Save",
    onPressed: () =>
    {
        Save();
        PlaySound();
        TrackAnalytics();
    }
)
```

LumaFlow Button does not need awareness of those systems.

---

## 110. Optional Convenience Components

Components such as:

```text
AsyncButton
ConfirmButton
FormField
```

may later encapsulate repeated interaction patterns.

They should compose Core primitives where practical.

---

## 111. Component Packages

Third-party LumaFlow component packages should ideally depend on:

```text
LumaFlow public API
+
UI Toolkit where native integration is needed
```

not internal runtime namespaces.

---

## 112. InternalsVisibleTo

Do not use `InternalsVisibleTo` as the general third-party extension strategy.

Public extension points should be deliberate.

---

## 113. Internal Namespaces

Runtime internals should use clearly internal namespaces/folders to discourage accidental dependency.

Example:

```text
LumaFlow.Internal
```

or assembly-internal accessibility.

---

## 114. Experimental Public APIs

If advanced component extension APIs are unstable, mark/document them as experimental rather than pretending they are stable.

Do not freeze low-level contracts too early.

---

## 115. Obsolete Migration

When component APIs change pre-1.0, provide migration notes where practical.

After 1.0, use normal deprecation/semantic-versioning policy.

---

## 116. Rejected Alternative: Every Component Is a VisualElement

Rejected.

Reason:

It merges declarative description and mounted native state.

---

## 117. Rejected Alternative: Every Component Is StatefulView

Rejected.

Reason:

Most components do not need mounted-local mutable state.

Stateful abstractions should carry semantic meaning.

---

## 118. Rejected Alternative: Every Component Is StatelessView

Also rejected.

Native-backed low-level controls need efficient direct native integration.

Wrapping every Button in composition layers would be unnecessary.

---

## 119. Rejected Alternative: Deep Component Inheritance

Rejected:

```text
Widget
↓
ControlWidget
↓
InteractiveWidget
↓
FocusableWidget
↓
ButtonBase
↓
StyledButton
↓
PrimaryButton
```

Prefer shallow inheritance plus composition and focused helpers.

---

## 120. Rejected Alternative: Massive Universal Component Base

Rejected.

A giant base with layout/style/event/state properties would:

- weaken semantics;
- expose irrelevant options;
- create implementation coupling;
- reproduce raw UI Toolkit poorly.

---

## 121. Rejected Alternative: Controller-First Components

Rejected as the default.

Simple components should not require separate controller classes.

---

## 122. Rejected Alternative: Attribute-Based Component Magic

Rejected as foundational:

```csharp
[LumaComponent]
[BindState]
[ThemeAware]
```

with reflection/code generation constructing behavior automatically.

Explicit C# composition is easier to reason about.

---

## 123. Architectural Invariants Created by This ADR

Unless superseded:

### Invariant 1

StatelessView is the default reusable composite component abstraction.

### Invariant 2

Reactive external State does not make a component stateful.

### Invariant 3

Mounted-local state belongs to StatefulView runtime, not reusable Widget description fields.

### Invariant 4

Native-backed controls remain distinct from composite Views.

### Invariant 5

Application components should prefer composition over inheritance.

### Invariant 6

Widget base classes remain small.

### Invariant 7

Advanced runtime internals are not required for normal component authoring.

### Invariant 8

Public component APIs are strongly typed and semantic.

### Invariant 9

No component registration, reflection, or source generation is required for ordinary authoring.

### Invariant 10

Custom components preserve native UI Toolkit escape hatches.

---

## 124. Codex Rules

### Rule 1

When implementing a reusable component that only composes Widgets, prefer StatelessView.

### Rule 2

Do not create StatefulView merely because a component receives State<T>.

### Rule 3

Do not store mounted-local state directly on reusable Widget descriptions unless the StatefulView contract explicitly guarantees correct per-mount semantics.

### Rule 4

Do not expose WidgetNode for normal custom-component authoring.

### Rule 5

Do not subclass VisualElement for public declarative component configuration unless implementing an explicitly native control layer.

### Rule 6

Prefer semantic options and enums over boolean explosions.

### Rule 7

Keep Widget base API minimal.

### Rule 8

Do not introduce component registration or reflection unless a concrete feature requires it.

### Rule 9

Use composition instead of subclass proliferation for visual variants.

### Rule 10

Before creating a public component base class, prove it through multiple real component implementations.

---

## 125. Decision Test

When creating a new component:

```text
Does it simply compose Widgets?
        ↓ yes
Use StatelessView.

Does it own mounted-local state?
        ↓ yes
Use StatefulView once that contract is finalized.

Does it directly wrap a VisualElement?
        ↓ yes
Use native-backed Widget infrastructure.

Is it just one local native integration?
        ↓ yes
Use Native(...).

Are you about to expose runtime internals?
        ↓ yes
Stop and design a narrower extension point.
```

---

## 126. Example: Stateless Domain Component

```csharp
public sealed class AudioClipCard : StatelessView
{
    public required AudioClip Clip { get; init; }

    public Action? OnPlay { get; init; }

    public override Widget Build(BuildContext context)
    {
        return Card(
            child: Padding(
                padding: EdgeInsets.All(
                    context.Theme.Spacing.M
                ),
                child: Row(
                    gap: context.Theme.Spacing.M,
                    children:
                    [
                        Icon(Icons.Audio),

                        Expanded(
                            child: Text(Clip.name)
                        ),

                        IconButton(
                            icon: Icons.Play,
                            onPressed: OnPlay
                        )
                    ]
                )
            )
        );
    }
}
```

No custom WidgetNode is required.

---

## 127. Example: Externally Reactive Component

```csharp
public sealed class DownloadStatus : StatelessView
{
    public required State<float> Progress { get; init; }

    public override Widget Build(BuildContext context)
    {
        return Column(
            gap: context.Theme.Spacing.S,
            children:
            [
                ProgressIndicator(
                    value: Progress
                ),

                Text(
                    value: Progress,
                    format: value =>
                        $"{value:P0}"
                )
            ]
        );
    }
}
```

The component is still stateless.

`Progress` is externally owned.

---

## 128. Example: Framework Native-Backed Control

Conceptually:

```text
TextField Widget
    ↓
TextFieldNode
    ↓
UnityEngine.UIElements.TextField
```

`TextFieldNode` handles:

```text
State<string> binding
SetValueWithoutNotify
native ChangeEvent<string>
theme
style
focus
cleanup
```

Application developers do not need to see the node.

---

## 129. Example: One-Off Native Integration

```csharp
return Card(
    child: Native(
        new CustomGraphElement()
    )
);
```

No dedicated LumaFlow Graph Widget is required unless repeated framework-level integration justifies one.

---

## 130. Example: Future Stateful Component

Conceptually:

```csharp
public sealed class ExpandableSection : StatefulView
{
    public required Widget Header { get; init; }

    public required Widget Child { get; init; }

    // Local expanded state is created per mounted instance
    // by StatefulView runtime infrastructure.
}
```

The exact local-state API remains intentionally unresolved.

---

## 131. Initial Implementation Target

Before designing broad public extension APIs, implement enough components to discover recurring patterns:

```text
Text
Button
TextField

Row
Column
Padding
Container

Theme
ReactiveBuilder

one real application component
```

Then evaluate which shared authoring bases are genuinely useful.

---

## 132. Public Extension API Gate

Do not publish a stable low-level custom Widget API until at least:

```text
several native-backed controls
several layout Widgets
one context provider
one structural reactive Widget
one third-party-like custom component
```

have been implemented.

The abstraction should emerge from proven repetition.

---

## 133. Long-Term Direction

The mature component-authoring story may become:

```text
StatelessView
StatefulView
NativeWidget<TElement>
SingleChildWidget
MultiChildWidget
custom Theme extensions
native references
safe lifecycle helpers
```

Only the first two should be normal application-level concepts.

---

## 134. Reconsideration Conditions

Revisit this ADR if:

1. StatelessView creates measurable runtime overhead;
2. StatefulView semantics require a fundamentally different component model;
3. third-party authors cannot build native controls safely without WidgetNode access;
4. composition becomes too verbose for common component patterns;
5. code-first preview/tooling requires additional metadata.

Any changes should preserve the separation between declarative component configuration and mounted runtime state.

---

## 135. Final Decision

LumaFlow component authoring follows a small hierarchy:

```text
StatelessView
    =
compose reusable declarative UI

StatefulView
    =
compose UI with mounted-local state

Native-backed Widget
    =
adapt native UI Toolkit controls

Structural Widget
    =
participate in LumaFlow runtime/context semantics
```

Application developers should be able to build the overwhelming majority of their UI using the first two levels.

The guiding rule is:

**Use composition by default.  
Use mounted state only when you own mounted state.  
Drop to native infrastructure only when the component genuinely requires native control.**