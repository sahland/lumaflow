# LumaFlow API Design Guidelines

## 1. Purpose

This document defines the public API design rules for LumaFlow.

Its goal is to ensure that all user-facing APIs remain:

- declarative;
- readable;
- consistent;
- strongly typed;
- discoverable;
- composable;
- idiomatic in C#;
- familiar to developers coming from Flutter and other declarative UI frameworks;
- natural for Unity UI Toolkit.

This document is normative for all public APIs.

When implementing a new public feature, the consumer-facing API must be designed before the internal implementation.

---

# 2. Core API Principle

LumaFlow exists primarily to improve developer experience.

Therefore:

```text
Consumer API quality
        >
Internal implementation convenience
```

If an implementation is slightly more complex internally but produces substantially cleaner user code, prefer the cleaner public API.

However, do not sacrifice:

- predictability;
- performance;
- type safety;
- lifecycle correctness;
- native UI Toolkit interoperability.

---

# 3. Desired Consumer Experience

LumaFlow code should read like a description of the interface.

Good:

```csharp
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

The structure of the UI should be obvious without reading implementation details.

Bad:

```csharp
var column = new ColumnWidget();

column.ConfigureLayout(
    new LayoutConfiguration
    {
        Direction = LayoutDirection.Vertical,
        Gap = 16
    });

column.AddChild(
    new TextWidget(
        new TextConfiguration(
            "Settings"
        )
    )
);
```

Avoid excessive configuration-object nesting.

---

# 4. C# First, Flutter Inspired

LumaFlow may borrow names and concepts from Flutter when they improve familiarity.

Examples:

```text
Row
Column
Padding
Expanded
Center
Container
BuildContext
Theme
Navigator
```

However, LumaFlow is a C# framework.

C# conventions take priority when Flutter syntax would feel unnatural or technically misleading.

The rule is:

```text
Borrow concepts.
Do not mechanically clone syntax.
```

---

# 5. Naming Conventions

Public types must use standard C# naming:

```text
PascalCase
```

Examples:

```csharp
Button
TextField
ThemeData
EdgeInsets
BuildContext
State<T>
```

Public properties and methods:

```text
PascalCase
```

Examples:

```csharp
Value
Theme
Push
Pop
Dispose
```

Method parameters:

```text
camelCase
```

Examples:

```csharp
onPressed
child
children
padding
crossAxisAlignment
```

---

# 6. Widget Naming

Widget names should be nouns describing the resulting UI concept.

Prefer:

```text
Button
Card
Text
Row
Column
Slider
Checkbox
```

Avoid:

```text
ButtonWidget
TextWidget
ColumnWidget
```

The namespace already communicates that these are UI objects.

Do not append `Widget` unless needed to resolve a real naming conflict.

---

# 7. Internal Naming

Internal runtime types may use explicit suffixes.

Examples:

```text
ButtonNode
TextNode
WidgetNode
BindingScope
StyleResolver
```

The public API should remain clean while internals can be explicit.

---

# 8. Factory Style vs Constructors

LumaFlow should optimize for readable declarative UI.

The preferred public syntax may use static factory methods or concise constructors depending on which produces the best C# experience.

Desired style:

```csharp
Button(
    "Save",
    onPressed: Save
)
```

rather than:

```csharp
new Button(
    "Save",
    Save
)
```

if the framework exposes widget factories through a base class, import, or DSL surface.

However, avoid language tricks that:

- confuse IDE navigation;
- require fragile source generators;
- make APIs ambiguous;
- prevent ordinary construction;
- break discoverability.

If direct constructors are clearer, use:

```csharp
new Button(
    "Save",
    onPressed: Save
)
```

The MVP should prefer conventional C# over clever DSL machinery.

---

# 9. Optional DSL Layer

A future convenience layer may expose:

```csharp
UI.Button(...)
UI.Column(...)
UI.Text(...)
```

or inherited/static imports.

This layer must remain syntactic sugar.

The core public API must still be understandable without it.

Do not make the framework depend architecturally on a global DSL class.

---

# 10. Named Parameters

Widgets with more than one meaningful configuration option should be designed to read well with named parameters.

Good:

```csharp
Button(
    "Delete",
    variant: ButtonVariant.Danger,
    icon: Icons.Delete,
    onPressed: Delete
)
```

Good:

```csharp
Row(
    gap: 12,
    mainAxisAlignment: MainAxisAlignment.SpaceBetween,
    children: [...]
)
```

Avoid positional arguments when their meaning is not immediately obvious.

Bad:

```csharp
Button("Delete", true, false, 2, Delete);
```

---

# 11. Required vs Optional Parameters

Only truly essential values should be required.

Example:

```csharp
Text("Hello")
```

The text value is essential.

Example:

```csharp
Button(
    "Save",
    onPressed: Save
)
```

A text-only button requires content and action.

Optional styling should not be mandatory.

Bad:

```csharp
Button(
    text: "Save",
    variant: ButtonVariant.Primary,
    size: ButtonSize.Medium,
    radius: 8,
    padding: EdgeInsets.Symmetric(...),
    enabled: true,
    icon: null,
    tooltip: null,
    onPressed: Save
)
```

when almost all values are defaults.

---

# 12. Defaults

Defaults are part of the framework's design language.

Common components should look reasonable with minimal configuration.

Example:

```csharp
Button(
    "Save",
    onPressed: Save
)
```

must already have:

- theme-defined padding;
- control height;
- typography;
- radius;
- colors;
- hover behavior;
- focus behavior.

Defaults should come from theme or framework defaults, not arbitrary scattered literals.

---

# 13. `child` and `children`

Use:

```text
child
```

when exactly one child is semantically expected.

Examples:

```csharp
Padding(
    child: content
)
```

```csharp
Center(
    child: spinner
)
```

Use:

```text
children
```

for ordered collections.

Examples:

```csharp
Row(
    children: [...]
)
```

```csharp
Column(
    children: [...]
)
```

Do not use both unless the component genuinely supports two distinct concepts.

---

# 14. Child Collection Type

Prefer modern C# collection-friendly APIs.

Desired usage:

```csharp
Column(
    children:
    [
        Text("One"),
        Text("Two")
    ]
)
```

Internally, public APIs may accept:

```csharp
IReadOnlyList<Widget>
```

or:

```csharp
IEnumerable<Widget>
```

depending on performance and lifetime requirements.

Avoid accepting mutable `List<Widget>` specifically.

If enumeration occurs more than once, materialize internally.

---

# 15. Null Children

Collections should not silently accept null child entries.

This should fail clearly in development.

Bad:

```csharp
Column(
    children:
    [
        header,
        null,
        footer
    ]
)
```

Prefer explicit conditional composition mechanisms.

Possible future helpers:

```csharp
Maybe(condition, widget)
```

or normal C# collection expressions:

```csharp
[
    header,
    ..(showSearch ? [search] : []),
    footer
]
```

Do not build null-skipping behavior into every container unless deliberately documented.

---

# 16. Callbacks

Callbacks should use semantic names.

Prefer:

```text
onPressed
onChanged
onSubmitted
onSelected
onFocusChanged
```

Avoid exposing native UI Toolkit event names directly when a higher-level semantic callback is clearer.

Example:

```csharp
Button(
    "Save",
    onPressed: Save
)
```

instead of:

```csharp
Button(
    "Save",
    clicked: Save
)
```

unless direct UI Toolkit interoperability is specifically being exposed.

---

# 17. Callback Types

Prefer standard delegates when sufficient.

Examples:

```csharp
Action
Action<T>
Func<T>
Func<T, TResult>
```

Do not create custom delegate types for every event.

Custom delegate types are justified only when semantics or documentation materially improve.

---

# 18. Async Callbacks

Async APIs should be explicit.

Possible future overload:

```csharp
Button(
    "Login",
    onPressedAsync: LoginAsync
)
```

or a unified API accepting:

```csharp
Func<Task>
```

The final design must define:

- exception handling;
- unmount behavior;
- cancellation;
- loading state;
- main-thread continuation.

Do not accept `async void` callbacks as a recommended pattern.

---

# 19. Boolean Parameters

Avoid components with many boolean flags.

Bad:

```csharp
Button(
    primary: true,
    danger: false,
    outlined: false,
    compact: true
)
```

Prefer enums or semantic variants:

```csharp
Button(
    variant: ButtonVariant.Primary,
    size: ButtonSize.Small
)
```

Booleans are acceptable for genuinely binary state:

```csharp
enabled: false
```

```csharp
readOnly: true
```

---

# 20. Component Variants

Visual semantics should use enums.

Example:

```csharp
public enum ButtonVariant
{
    Primary,
    Secondary,
    Outline,
    Ghost,
    Danger
}
```

Usage:

```csharp
Button(
    "Delete",
    variant: ButtonVariant.Danger
)
```

Do not require users to manually reconstruct standard design-system states.

---

# 21. Enum Design

Enums should represent closed semantic sets.

Good:

```text
MainAxisAlignment
CrossAxisAlignment
ButtonVariant
TextAlign
ControlSize
```

Avoid enums for concepts likely to require arbitrary extension.

For extensible design tokens or user-defined style sets, prefer objects or keys.

---

# 22. Flags Enums

Use `[Flags]` only when combinations are meaningful and intuitive.

Do not use flags merely to reduce parameter count.

---

# 23. State<T> API

The core reactive primitive should remain simple.

Desired baseline:

```csharp
var count = new State<int>(0);

count.Value++;
```

or convenience:

```csharp
State<int> count = State(0);
```

if a DSL helper exists.

The public semantics must be obvious:

- `Value` reads current state;
- assigning a changed value publishes an update;
- subscriptions are deterministic;
- State itself is not tied to UI.

---

# 24. State Naming

Use:

```csharp
State<T>
```

unless implementation realities later require a different concept.

Avoid unnecessary names like:

```text
ReactiveValue<T>
ObservableMutableValue<T>
BindableStateContainer<T>
```

The simpler name matches the framework's product goal.

---

# 25. State Binding APIs

Reactive binding syntax should be strongly typed.

Possible acceptable forms:

```csharp
Text(
    value: count,
    format: value => value.ToString()
)
```

or:

```csharp
Text(
    count.Select(value => $"Count: {value}")
)
```

or future dependency tracking:

```csharp
Text(() => $"Count: {count.Value}")
```

The selected API must satisfy:

- readable call site;
- automatic unsubscription;
- no hidden global state;
- predictable update scope;
- good debugging.

Do not finalize magical dependency tracking only for syntax beauty.

---

# 26. Mutable vs Immutable Widget Configuration

Widget descriptions should preferably be immutable after construction.

Prefer:

```csharp
var button = new Button(
    "Save",
    variant: ButtonVariant.Primary
);
```

with readonly/init-only configuration.

Avoid:

```csharp
var button = new Button();

button.Text = "Save";
button.Variant = ButtonVariant.Primary;
```

for normal usage.

Declarative descriptions should represent a complete configuration.

---

# 27. Init Properties

For larger user-defined components, `init` properties are acceptable.

Example:

```csharp
public sealed class UserCard : StatelessView
{
    public required User User { get; init; }
    public bool ShowAvatar { get; init; } = true;
}
```

However, framework primitives should generally favor concise constructors/factories where that improves usage.

---

# 28. `required`

Use `required` for properties that truly must be supplied before use.

Do not use `required` on optional visual configuration.

---

# 29. Fluent APIs

Avoid mutable fluent chains as the primary UI authoring style.

Bad primary API:

```csharp
new Button("Save")
    .WithVariant(ButtonVariant.Primary)
    .WithPadding(...)
    .WithRadius(...)
    .OnPressed(Save);
```

This obscures hierarchy and encourages mutable configuration.

Fluent APIs may be appropriate for specialized builders, but not as the default widget syntax.

---

# 30. Extension Methods

Extension methods should be used sparingly.

Good uses:

- bridging native UI Toolkit;
- small ergonomic transformations;
- optional utility APIs.

Avoid creating hidden behavior chains such as:

```csharp
widget
    .Reactive()
    .ThemeAware()
    .Mounted()
    .Styled()
```

Core behavior should be explicit in the framework architecture.

---

# 31. Layout API

Layout APIs should use familiar names.

Examples:

```csharp
Row(
    mainAxisAlignment: MainAxisAlignment.Center,
    crossAxisAlignment: CrossAxisAlignment.Stretch,
    gap: 12,
    children: [...]
)
```

```csharp
Column(
    gap: 8,
    children: [...]
)
```

Do not expose low-level Yoga terminology unless used in native escape hatches.

For example, users should normally not need:

```csharp
FlexDirection.Row
Justify.SpaceBetween
Align.Center
```

for ordinary Row/Column usage.

---

# 32. Unity-Native Behavior

Familiar names must not promise unsupported Flutter semantics.

If UI Toolkit differs, document the difference.

For example:

```csharp
Expanded(...)
```

should express flex growth in a familiar way, but its exact implementation should use UI Toolkit/Yoga semantics.

API names should be familiar without lying about behavior.

---

# 33. EdgeInsets

Padding and margin should use a typed API.

Examples:

```csharp
EdgeInsets.All(16)
```

```csharp
EdgeInsets.Symmetric(
    horizontal: 24,
    vertical: 12
)
```

```csharp
EdgeInsets.Only(
    left: 16,
    top: 8
)
```

Potential:

```csharp
EdgeInsets.Zero
```

Do not make users repeatedly specify four floats.

---

# 34. Units

Initial LumaFlow numeric layout values should default to Unity UI Toolkit pixel-like length semantics where appropriate.

Example:

```csharp
width: 320
```

should be straightforward.

When supporting percentages or other UI Toolkit lengths, use typed values rather than magic strings.

Possible future type:

```csharp
LengthValue
```

Examples:

```csharp
Width.Pixels(320)
Width.Percent(100)
```

Do not prematurely replace simple float APIs when most usage is pixel-based.

---

# 35. Colors

Public APIs should use `UnityEngine.Color` where interoperability matters.

Framework theme helpers may provide semantic convenience.

Example:

```csharp
color: context.Theme.Colors.Primary
```

Potential hex utility:

```csharp
ColorValue.FromHex("#168BFF")
```

Avoid introducing an incompatible custom color model unless necessary.

---

# 36. Typography

Text styling should use typed objects.

Example:

```csharp
Text(
    "Settings",
    style: context.Theme.Typography.TitleLarge
)
```

Overrides may be supported:

```csharp
Text(
    "Warning",
    style: new TextStyle(
        color: context.Theme.Colors.Error,
        fontWeight: FontWeight.SemiBold
    )
)
```

Do not require USS classes for every typography variant.

---

# 37. Text API

The simplest form must remain trivial:

```csharp
Text("Hello")
```

Optional configuration may include:

```text
style
align
overflow
maxLines
selectable
tooltip
```

Do not overload Text with unrelated layout responsibilities.

---

# 38. Button API

Target simplicity:

```csharp
Button(
    "Save",
    onPressed: Save
)
```

Extended usage:

```csharp
Button(
    "Delete",
    icon: Icons.Delete,
    variant: ButtonVariant.Danger,
    size: ControlSize.Medium,
    enabled: canDelete,
    onPressed: Delete
)
```

Do not expose native `Button.clicked` as the primary callback.

---

# 39. Icon Buttons

Use a separate semantic component:

```csharp
IconButton(
    icon: Icons.Settings,
    onPressed: OpenSettings
)
```

rather than requiring:

```csharp
Button(
    text: null,
    icon: Icons.Settings,
    iconOnly: true
)
```

when icon-only behavior has distinct sizing/accessibility semantics.

---

# 40. TextField API

Desired:

```csharp
TextField(
    label: "Username",
    value: username
)
```

Potential extended usage:

```csharp
TextField(
    label: "Password",
    value: password,
    placeholder: "Enter password",
    obscureText: true,
    onSubmitted: Login
)
```

The exact binding model must remain consistent with State APIs.

---

# 41. Controlled vs Uncontrolled Inputs

LumaFlow should clearly distinguish externally-controlled value bindings from one-time initial values.

Avoid ambiguous APIs like:

```csharp
TextField(value: "Alex")
```

if it is unclear whether edits mutate anything.

Possible pattern:

```csharp
TextField(
    value: usernameState
)
```

for controlled reactive binding.

And:

```csharp
TextField(
    initialValue: "Alex"
)
```

for local internal value.

Do not use the same parameter for two ownership models.

---

# 42. Disabled State

Use:

```csharp
enabled: false
```

for controls where appropriate.

When a callback is null, decide and document whether this also implies disabled state.

Avoid inconsistent component behavior.

For example, if:

```csharp
Button("Save", onPressed: null)
```

means disabled, it should do so consistently.

---

# 43. Validation

Input validation should not be baked into basic controls prematurely.

Prefer composition.

Possible future:

```csharp
FormField<T>
Validator<T>
FormState
```

Basic `TextField` should remain usable independently.

---

# 44. Container API

`Container` may combine common box concerns.

Example:

```csharp
Container(
    width: 320,
    padding: EdgeInsets.All(16),
    decoration: BoxDecoration(
        color: context.Theme.Colors.Surface,
        borderRadius: BorderRadius.All(12)
    ),
    child: content
)
```

Do not let Container grow into an everything-widget with dozens of unrelated responsibilities.

When specialized semantics exist, use dedicated widgets.

---

# 45. Specialized Widgets vs Giant Widgets

Prefer:

```csharp
Center(
    child: content
)
```

over:

```csharp
Container(
    alignment: Alignment.Center,
    ...
)
```

when the specialized widget significantly improves readability.

But do not create a specialized widget for every single property.

Use judgment based on common declarative composition patterns.

---

# 46. Styling Overrides

Explicit widget styling should override theme defaults.

Resolution order:

```text
explicit widget value
        ↓
component theme
        ↓
global theme
        ↓
framework default
```

This behavior should be consistent across components.

---

# 47. Theme APIs

Theme access should be concise.

Desired:

```csharp
context.Theme.Colors.Primary
context.Theme.Spacing.M
context.Theme.Radius.Large
context.Theme.Typography.BodyMedium
```

Avoid deep chains:

```csharp
context
    .ApplicationTheme
    .DesignSystem
    .Typography
    .Tokens
    .Body
    .Medium
```

---

# 48. Theme Overrides

Scoped theme changes should be composable.

Possible:

```csharp
Theme(
    data: darkTheme,
    child: Dialog(...)
)
```

or:

```csharp
ThemeOverride(
    colors: ...,
    child: ...
)
```

The exact API may evolve.

Do not require global theme mutation.

---

# 49. BuildContext API

BuildContext should expose common contextual values through typed properties where possible.

Good:

```csharp
context.Theme
context.Navigator
context.MediaQuery
```

Avoid string-keyed retrieval:

```csharp
context.Get("theme")
```

Typed provider APIs may exist for extensibility.

---

# 50. Navigation API

Future navigation should remain compact.

Desired:

```csharp
context.Navigator.Push(
    new SettingsScreen()
);
```

```csharp
context.Navigator.Pop();
```

```csharp
context.Navigator.Replace(
    new HomeScreen()
);
```

Avoid requiring navigation request objects for ordinary operations.

---

# 51. Route API

If named routes are introduced, keep simple cases simple.

Possible:

```csharp
Routes(
    Route("/", () => new HomeScreen()),
    Route("/settings", () => new SettingsScreen())
)
```

Do not implement web-style routing complexity unless Unity applications actually need it.

---

# 52. Keys

Keys should only be exposed when reconciliation/list identity needs them.

Potential:

```csharp
UserCard(
    key: Key(user.Id),
    user: user
)
```

Do not expose keys simply because Flutter has them.

---

# 53. Native UI Toolkit Interop

Native access must be explicit and easy.

Possible API:

```csharp
Native(
    new MyVisualElement()
)
```

or:

```csharp
VisualElementWidget(
    element: myElement
)
```

Native integrations should still participate in:

- mounting;
- parenting;
- cleanup;
- context where applicable.

---

# 54. USS Interop

Users should be able to apply USS classes when desired.

Possible properties:

```csharp
className: "settings-card"
```

or:

```csharp
classes:
[
    "settings-card",
    "compact"
]
```

Do not force users to abandon existing UI Toolkit assets.

---

# 55. Naming Native Escape Hatches

Native APIs should clearly communicate that the user is crossing abstraction boundaries.

Good:

```text
Native
VisualElementAdapter
RawStyle
```

Avoid making low-level APIs look like ordinary high-level widget APIs.

---

# 56. Exceptions

Public API misuse should result in descriptive exceptions.

Example:

```text
LumaFlow: Column.children cannot contain null widgets.
```

Avoid generic:

```text
ArgumentException
```

without useful context where framework-specific context can be added.

Use standard exception types when appropriate.

---

# 57. Argument Validation

Validate public API invariants near boundaries.

Examples:

```text
negative flex values
invalid ranges
null required children
min > max
```

Avoid silently correcting invalid configuration unless that behavior is intuitive and documented.

---

# 58. XML Documentation

Public framework APIs should include concise XML documentation.

Documentation should explain:

- purpose;
- non-obvious semantics;
- lifecycle behavior where relevant;
- defaults when important.

Do not write paragraphs for obvious properties like `Width`.

---

# 59. IntelliSense Quality

Design APIs with autocomplete in mind.

Good:

```csharp
ButtonVariant.Primary
MainAxisAlignment.SpaceBetween
EdgeInsets.All(...)
```

Avoid string-based configuration:

```csharp
variant: "primary"
alignment: "space-between"
```

unless interoperating directly with external formats.

---

# 60. Overload Philosophy

Use overloads when they materially improve common usage.

Good:

```csharp
Text(string text)
```

and:

```csharp
Text(State<string> text)
```

if both semantics are unambiguous.

Avoid dozens of overloads that create compiler ambiguity.

Named optional parameters are often better than combinatorial overloads.

---

# 61. Constructor Explosion

Avoid constructors like:

```csharp
Button(string text)
Button(string text, Action onPressed)
Button(string text, Icon icon)
Button(string text, Icon icon, Action onPressed)
Button(string text, Icon icon, ButtonVariant variant)
...
```

Prefer one concise primary API with optional named parameters.

---

# 62. Generic APIs

Use generics where they provide genuine type safety.

Good candidates:

```csharp
State<T>
Dropdown<T>
ListView<T>
ReactiveBuilder<T>
```

Avoid generics where they only complicate call sites.

---

# 63. ListView<T>

Desired future API:

```csharp
ListView(
    items: users,
    itemBuilder: user =>
        UserCard(user: user)
)
```

Potential explicit generic:

```csharp
ListView<User>(
    items: users,
    itemBuilder: ...
)
```

Type inference should work whenever practical.

---

# 64. Value Selectors

Components like Dropdown should avoid object boxing.

Desired:

```csharp
Dropdown(
    value: selectedResolution,
    items: resolutions,
    labelBuilder: resolution => resolution.Name
)
```

Use strongly typed callbacks.

---

# 65. Design Tokens

Public tokens should be semantic.

Good:

```csharp
theme.Spacing.Small
theme.Spacing.Medium
theme.Spacing.Large
```

or concise:

```csharp
theme.Spacing.S
theme.Spacing.M
theme.Spacing.L
```

Pick one convention and use it consistently.

Do not mix:

```text
Small
Md
Large
XLarge
```

---

# 66. Size Naming

If component sizes are introduced, prefer:

```text
Small
Medium
Large
```

Potential:

```text
Compact
Regular
Large
```

Do not overcomplicate with too many size tiers before needed.

---

# 67. Icons

Icon APIs should abstract icon sources without tying the framework to one proprietary set.

Potential type:

```csharp
IconData
```

Usage:

```csharp
Icon(Icons.Play)
```

Users should eventually be able to provide:

- Texture2D;
- VectorImage;
- theme icon;
- custom icon source.

Do not hardcode a mandatory third-party icon library into Core.

---

# 68. Asset References

Unity assets should use native types where possible.

Examples:

```csharp
Texture2D
VectorImage
Font
FontAsset
```

Avoid custom wrappers unless they solve cross-source behavior.

---

# 69. Animations

Future animation APIs should be declarative but should not obscure UI Toolkit capabilities.

Potential:

```csharp
AnimatedContainer(
    duration: TimeSpan.FromMilliseconds(200),
    ...
)
```

Do not design animation syntax before the underlying update model is stable.

---

# 70. Time Values

Prefer:

```csharp
TimeSpan
```

for public durations where practical.

Avoid ambiguous float milliseconds/seconds unless Unity-native API compatibility strongly favors floats.

---

# 71. Accessibility

Component APIs should leave room for:

```text
tooltip
accessible label
focus behavior
keyboard navigation
```

Do not hardcode mouse-only interaction assumptions.

Native UI Toolkit behavior should be preserved where possible.

---

# 72. Editor and Runtime Consistency

Shared components should have the same public API in Runtime and Editor when semantics are equivalent.

Do not create:

```text
RuntimeButton
EditorButton
```

unless behavior genuinely differs.

Editor-only widgets should live in Editor namespaces/modules.

---

# 73. Pipeline Independence in Public API

Ordinary component APIs must not expose URP/HDRP-specific types.

Bad:

```csharp
Card(
    rendererFeature: SomeUrpRendererFeature
)
```

Core UI remains render-pipeline agnostic.

Optional effects modules may expose pipeline-specific configuration separately.

---

# 74. Avoid Framework Leakage

Application developers should not need to understand internal types such as:

```text
WidgetNode
MountScope
BindingScope
Reconciler
StyleMapper
```

for ordinary UI development.

If these routinely leak into consumer code, the abstraction boundary is failing.

---

# 75. Avoid UI Toolkit Leakage in High-Level APIs

Use native UI Toolkit types where interoperability is genuinely valuable, but avoid unnecessary leakage.

Example:

High-level Row should not require:

```csharp
Justify.SpaceBetween
```

when:

```csharp
MainAxisAlignment.SpaceBetween
```

better communicates LumaFlow semantics.

Native escape hatches remain available.

---

# 76. API Stability

Before 1.0, breaking public API changes are allowed when they significantly improve design.

They must still be deliberate.

Do not retain a bad public abstraction solely because it was implemented early.

After stable releases, compatibility becomes a major concern.

---

# 77. Obsolete APIs

When deprecating established APIs, prefer:

```csharp
[Obsolete("Use ... instead.")]
```

and provide migration guidance.

Do not silently remove widely-used APIs in stable versions.

---

# 78. Semantic Versioning

Public API evolution should eventually follow semantic versioning.

Conceptually:

```text
PATCH
Bug fixes, compatible improvements

MINOR
Backward-compatible public features

MAJOR
Breaking public API changes
```

Pre-1.0 may evolve faster.

---

# 79. API Review Checklist

Before introducing any public type or member, answer:

1. What does the consuming code look like?
2. Is the common case concise?
3. Are parameter names obvious?
4. Are defaults sensible?
5. Is the API strongly typed?
6. Does it compose naturally?
7. Is it consistent with neighboring components?
8. Does it unnecessarily expose UI Toolkit internals?
9. Does it promise behavior Unity cannot provide?
10. Is this abstraction actually needed?
11. Can we support it long-term?
12. Does it belong in Core or an optional module?

If several answers are unclear, do not finalize the API.

---

# 80. Codex Rule: Usage Before Implementation

Before implementing a new public feature, Codex must first internally formulate at least one ideal consumer-side usage example.

For substantial APIs, formulate:

```text
minimal usage
advanced usage
```

Example:

```csharp
Button(
    "Save",
    onPressed: Save
)
```

and:

```csharp
Button(
    "Delete",
    icon: Icons.Delete,
    variant: ButtonVariant.Danger,
    size: ControlSize.Small,
    enabled: canDelete,
    onPressed: Delete
)
```

Only then should internal implementation be designed.

---

# 81. Codex Rule: Do Not Add API for Internal Convenience

Do not expose:

```csharp
public WidgetNode Node { get; }
```

only because internal code needs access to it.

Do not expose style mapper internals because a component implementation needs them.

Public API exists for consumers, not framework implementation.

---

# 82. Codex Rule: Prefer Consistency Over Local Cleverness

If `Button` uses:

```csharp
onPressed
```

another clickable control should not arbitrarily use:

```csharp
onClick
```

without a strong semantic reason.

If input widgets bind State using one convention, new input widgets should follow it.

Search existing APIs before inventing a new pattern.

---

# 83. Codex Rule: Avoid Duplicate Concepts

Before creating:

```text
Inset
Spacing
PaddingValue
Margins
```

check whether:

```text
EdgeInsets
```

already solves the concept.

Before adding:

```text
Observable<T>
```

check whether:

```text
State<T>
```

already owns that responsibility.

The framework should have a small coherent vocabulary.

---

# 84. Codex Rule: Prefer Semantic API

Prefer:

```csharp
ButtonVariant.Danger
```

over:

```csharp
backgroundColor: red
```

when expressing a standard design-system intention.

Still allow low-level override when customization is needed.

---

# 85. Codex Rule: No Magic Strings

Do not introduce public configuration like:

```csharp
alignment: "center"
variant: "danger"
size: "medium"
```

when a typed API is practical.

Strings are acceptable for:

- displayed text;
- USS class names;
- route names/paths;
- localization keys;
- user-defined identifiers.

---

# 86. Codex Rule: Minimal Surface Area

Do not make every internal capability public.

Prefer:

```text
small stable public API
large flexible internal implementation
```

Public APIs create maintenance commitments.

---

# 87. Codex Rule: No Premature Generalization

Do not turn:

```csharp
Button
```

into a generic:

```csharp
InteractiveActionSurface<TContent, TAction, TVisualState>
```

without demonstrated need.

Build concrete useful components first.

Extract shared abstractions only when real duplication appears.

---

# 88. Good API Examples

## Simple layout

```csharp
Column(
    gap: 12,
    children:
    [
        Text("Account"),
        TextField(
            label: "Name",
            value: name
        )
    ]
)
```

## Themed card

```csharp
Card(
    child: Padding(
        padding: context.Theme.Spacing.CardInsets,
        child: Text("Connected")
    )
)
```

## Reactive control

```csharp
Slider(
    value: volume,
    min: 0f,
    max: 1f
)
```

## Semantic action

```csharp
Button(
    "Remove",
    variant: ButtonVariant.Danger,
    onPressed: Remove
)
```

## Native escape hatch

```csharp
Native(
    new CustomVisualElement()
)
```

---

# 89. Bad API Examples

## Excessive configuration

```csharp
Button(
    configuration:
        new ButtonConfiguration
        {
            Appearance =
                new AppearanceConfiguration
                {
                    Variant = ...
                }
        }
)
```

## Magic strings

```csharp
Button(
    variant: "danger",
    size: "small"
)
```

## Boolean explosion

```csharp
Button(
    isPrimary: false,
    isSecondary: false,
    isDanger: true,
    isCompact: true,
    isOutlined: false
)
```

## Mutable setup

```csharp
var button = new Button();
button.Text = "Save";
button.OnPressed = Save;
button.Variant = ButtonVariant.Primary;
```

## Leaking internals

```csharp
Button(
    nodeFactory: ...
)
```

---

# 90. Final API Principle

The ideal LumaFlow API should allow a developer to read a screen from top to bottom and understand its structure without mentally translating implementation mechanics.

This:

```csharp
return Column(
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
            child: ListView(
                items: sounds,
                itemBuilder: sound =>
                    SoundCard(sound: sound)
            )
        )
    ]
);
```

is the target.

The framework should hide:

```text
VisualElement construction
style mapping
event wiring
state subscriptions
mounting
cleanup
```

without hiding the fact that UI Toolkit exists underneath.

LumaFlow's public API should feel modern, intentional, and boring in the best possible way: obvious to read, difficult to misuse, and easy to extend.