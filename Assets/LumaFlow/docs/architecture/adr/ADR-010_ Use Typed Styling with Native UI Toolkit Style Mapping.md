# ADR-010: Use Typed Styling with Native UI Toolkit Style Mapping

- **Status:** Accepted
- **Decision date:** 2026-08-11
- **Scope:** Styling, decorations, text styles, USS interoperability, style updates
- **Affects:** Runtime, Widgets, Layout, Theme, Components, Performance
- **Related documents:** `ARCHITECTURE.md`, `API_DESIGN.md`, `ADR-001-native-uitoolkit.md`, `ADR-005-theme-system.md`, `ADR-009-layout-model.md`

---

## 1. Context

Unity UI Toolkit provides several ways to style UI:

```text
USS
VisualElement.style
resolvedStyle
style classes
inline style values
```

These mechanisms are powerful, but using them directly throughout application code can become verbose and fragmented.

A typical raw UI Toolkit component may require code such as:

```csharp
element.style.paddingLeft = 16;
element.style.paddingRight = 16;
element.style.paddingTop = 12;
element.style.paddingBottom = 12;

element.style.backgroundColor = color;

element.style.borderTopLeftRadius = 8;
element.style.borderTopRightRadius = 8;
element.style.borderBottomLeftRadius = 8;
element.style.borderBottomRightRadius = 8;
```

LumaFlow should replace this repetition with typed declarative styling.

At the same time, USS is an important part of UI Toolkit and must remain interoperable.

---

## 2. Decision

LumaFlow will provide a typed styling layer that maps declarative style values into native UI Toolkit style properties.

Conceptually:

```text
LumaFlow style values
        ↓
Style Resolver
        ↓
Style Mapper
        ↓
VisualElement.style
        ↓
UI Toolkit
```

USS remains supported as an additional styling mechanism.

LumaFlow will not create a separate CSS engine.

---

## 3. Core Styling Principle

Application code should express intent through typed values.

Preferred:

```csharp
Container(
    padding: EdgeInsets.All(16),
    decoration: BoxDecoration(
        color: context.Theme.Colors.Surface,
        borderRadius: BorderRadius.All(12)
    ),
    child: content
)
```

instead of:

```csharp
var element = new VisualElement();

element.style.paddingLeft = 16;
element.style.paddingRight = 16;
element.style.paddingTop = 16;
element.style.paddingBottom = 16;

element.style.backgroundColor = surfaceColor;

element.style.borderTopLeftRadius = 12;
element.style.borderTopRightRadius = 12;
element.style.borderBottomLeftRadius = 12;
element.style.borderBottomRightRadius = 12;
```

---

## 4. Styling Architecture

The preferred architecture is:

```text
Widget configuration
        ↓
Theme resolution
        ↓
Resolved style
        ↓
Style mapping
        ↓
VisualElement.style
```

Example:

```text
Button Widget
        ↓
ButtonTheme
        ↓
ResolvedButtonStyle
        ↓
ButtonStyleMapper
        ↓
native Button
```

---

## 5. Typed Style Values

LumaFlow should provide typed style concepts where they significantly improve ergonomics.

Initial types include:

```text
EdgeInsets
BorderRadius
Radius
Border
BorderSide
BoxDecoration
TextStyle
Alignment
```

Future types may include:

```text
Shadow
Gradient
Constraints
Length
Transform
```

only when needed.

---

## 6. Style Values Are Data

Types such as:

```text
EdgeInsets
BorderRadius
TextStyle
BoxDecoration
```

should primarily describe values.

They should not directly own VisualElements.

Preferred:

```text
BoxDecoration
        ↓
DecorationStyleMapper
        ↓
VisualElement
```

rather than:

```text
BoxDecoration.Apply(element)
```

if central mapping provides better separation.

The exact method structure may vary, but style data and native mutation should remain conceptually separate.

---

## 7. Immutability

Style value objects should preferably be immutable.

Examples:

```csharp
public readonly struct EdgeInsets
{
    public float Left { get; }
    public float Top { get; }
    public float Right { get; }
    public float Bottom { get; }
}
```

Benefits:

```text
predictable equality
safe sharing
easy theme reuse
future caching
reliable diffing
```

---

## 8. Struct vs Class

Small value-like styling concepts are good candidates for readonly structs.

Examples:

```text
EdgeInsets
Radius
BorderSide
```

Larger or optional-rich structures may use immutable classes.

Examples:

```text
TextStyle
BoxDecoration
```

Choose based on semantics and copying cost rather than enforcing one representation everywhere.

---

## 9. EdgeInsets

`EdgeInsets` represents four-sided spacing.

Required constructors/helpers should include approximately:

```csharp
EdgeInsets.Zero

EdgeInsets.All(16)

EdgeInsets.Symmetric(
    horizontal: 24,
    vertical: 12
)

EdgeInsets.Only(
    left: 8,
    top: 4,
    right: 8,
    bottom: 4
)
```

This type may be used for:

```text
padding
margin
```

where appropriate.

---

## 10. BorderRadius

`BorderRadius` describes corner radii.

Desired:

```csharp
BorderRadius.All(12)
```

and potentially:

```csharp
BorderRadius.Only(
    topLeft: 12,
    topRight: 12
)
```

Internally it maps to UI Toolkit corner radius properties.

---

## 11. Radius

A smaller `Radius` value type may represent one radius if it improves API clarity.

Do not introduce both `Radius` and `BorderRadius` unless each has a useful semantic role.

Avoid redundant abstraction.

---

## 12. BorderSide

Conceptually:

```csharp
new BorderSide(
    width: 1,
    color: context.Theme.Colors.Border
)
```

Potential properties:

```text
width
color
```

Do not model unsupported border styles merely because CSS supports them.

---

## 13. Border

`Border` may represent four sides.

Convenience:

```csharp
Border.All(
    color: context.Theme.Colors.Border,
    width: 1
)
```

Possible specialized configuration:

```text
left
top
right
bottom
```

when UI Toolkit supports the required behavior.

---

## 14. BoxDecoration

`BoxDecoration` represents common container visual properties.

Initial responsibilities may include:

```text
background color
border
border radius
shadow where natively supported
```

Example:

```csharp
BoxDecoration(
    color: context.Theme.Colors.Surface,
    border: Border.All(
        color: context.Theme.Colors.Border,
        width: 1
    ),
    borderRadius: BorderRadius.All(12)
)
```

---

## 15. BoxDecoration Must Stay Focused

Do not turn `BoxDecoration` into an unbounded bag containing:

```text
layout
padding
margin
alignment
input
focus
animation
navigation
```

Decoration describes visual box appearance.

Layout concerns belong elsewhere.

---

## 16. TextStyle

`TextStyle` should encapsulate typography-related styling.

Potential properties:

```text
color
font
font size
font weight
line height
letter spacing
text alignment where semantically appropriate
```

Example:

```csharp
Text(
    "Settings",
    style: context.Theme.Typography.TitleLarge
)
```

---

## 17. TextStyle Overrides

A theme style may be partially overridden.

Possible:

```csharp
context.Theme.Typography.BodyMedium.CopyWith(
    color: context.Theme.Colors.Error
)
```

Exact API may evolve.

Avoid forcing creation of entirely new style structures for one changed value.

---

## 18. Native Unity Types

Where Unity already exposes an appropriate value type, LumaFlow should reuse it when this preserves interoperability.

Examples:

```text
Color
VectorImage
Texture2D
Font
```

Do not create incompatible wrappers without a clear benefit.

---

## 19. Length Values

Simple numeric values should remain ergonomic.

Example:

```csharp
width: 320
```

For advanced length semantics, LumaFlow may introduce typed values.

Potential:

```csharp
Length.Pixels(320)
Length.Percent(100)
Length.Auto
```

Do not force verbose Length wrappers for basic pixel-like usage unless required by API consistency.

---

## 20. Raw String Styles

Normal public styling must not require:

```csharp
width: "100%"
padding: "8px 16px"
```

Use typed APIs.

Strings remain appropriate for:

```text
USS class names
element names
display text
resource identifiers
```

---

## 21. Style Mapping

Mapping code converts typed values into UI Toolkit style assignments.

Example:

```text
EdgeInsets
    ↓
paddingLeft
paddingTop
paddingRight
paddingBottom
```

This mapping must be centralized where practical.

---

## 22. Mapping Ownership

Good internal structure:

```text
StyleMapping/
├── BoxStyleMapper
├── TextStyleMapper
├── BorderStyleMapper
├── FlexStyleMapper
└── SizeStyleMapper
```

Exact folder layout may differ.

Avoid every widget independently duplicating the same assignments.

---

## 23. No Giant StyleMapper

Also avoid:

```text
StyleMapper.cs
```

with thousands of lines and responsibility for every style property.

Split by semantic concern.

---

## 24. Resolved Style Layer

Components may need a resolved representation after combining:

```text
framework defaults
theme
component theme
widget overrides
interaction state
```

Conceptually:

```text
Button Widget
        ↓
ButtonStyleResolver
        ↓
ResolvedButtonStyle
        ↓
ButtonStyleMapper
```

This distinction is especially useful for themed components.

---

## 25. Public vs Internal Styles

`TextStyle`, `Border`, etc. may be public application-facing values.

Types such as:

```text
ResolvedButtonStyle
ResolvedInputStyle
```

should normally remain internal.

They represent framework implementation results.

---

## 26. Style Resolution Precedence

Follow ADR-005:

```text
explicit widget value
        ↓
component theme
        ↓
global theme token
        ↓
framework default
```

This must remain consistent.

---

## 27. USS Interoperability

LumaFlow must support USS.

Users should be able to:

```text
attach StyleSheet
apply USS classes
use existing project styles
use pseudo states
use selectors
```

LumaFlow does not replace UI Toolkit's style system.

---

## 28. USS Classes

A common API may expose:

```csharp
classes:
[
    "audio-card",
    "selected"
]
```

or similar.

The exact Widget base API should be designed carefully so every component can support classes without duplicating code.

---

## 29. Single Class Convenience

Potential:

```csharp
className: "audio-card"
```

may be unnecessary if `classes` is already concise.

Avoid overlapping APIs unless common usage benefits significantly.

---

## 30. Stylesheet Attachment

A subtree may need to attach a StyleSheet.

Potential future API:

```csharp
StyleScope(
    styleSheets: [sheet],
    child: content
)
```

or mount-level stylesheet configuration.

Do not attach the same stylesheet redundantly to every element.

---

## 31. USS Is an Escape Hatch and Extension Layer

Typed styling should solve ordinary component-level styling.

USS is especially appropriate for:

```text
pseudo states
complex selectors
shared application styling
existing projects
advanced transitions
native UI Toolkit extensions
```

---

## 32. Inline Style Precedence

UI Toolkit inline styles generally have high precedence over USS.

Therefore LumaFlow must be careful when applying typed styles directly to `VisualElement.style`.

If LumaFlow writes a property inline, USS may no longer be able to override it normally.

This is an important design constraint.

---

## 33. Do Not Set Every Property Inline

LumaFlow must not eagerly assign every conceivable style property.

Example:

If a Button does not explicitly or thematically specify margin, do not write:

```text
margin = 0
```

merely to initialize it.

Leaving properties unset preserves:

```text
USS inheritance
native defaults
external styling
```

---

## 34. Explicit Style Ownership

For each style property, distinguish:

```text
unset
explicitly set
theme-resolved
native/USS-controlled
```

Do not collapse these concepts prematurely.

---

## 35. Optional Style Values

Resolved configuration may need optional values.

Conceptually:

```text
float? Width
Color? BackgroundColor
EdgeInsets? Margin
```

where null/unset means:

```text
do not override native/USS value
```

This is preferable to writing arbitrary defaults into every VisualElement.

---

## 36. Theme Defaults and USS

Framework component themes may intentionally provide inline defaults.

However, if LumaFlow wants USS customization to remain meaningful, it should carefully decide which defaults belong in:

```text
inline style
```

and which may be represented through framework USS classes.

This tradeoff should be evaluated during implementation.

---

## 37. Hybrid Default Styling

A possible architecture is:

```text
structural/common default styles
→ framework USS classes

dynamic/typed overrides
→ VisualElement.style
```

This could preserve pseudo-state behavior and reduce repeated style writes.

This ADR does not require that exact implementation.

It requires USS interoperability and deliberate precedence.

---

## 38. Framework USS

LumaFlow may ship internal USS styles for native controls.

Examples:

```text
lumaflow-button
lumaflow-text-field
lumaflow-card
```

This is acceptable where USS is the best mechanism for:

```text
hover
pressed
focused
disabled
transition states
```

---

## 39. Internal USS Is an Implementation Detail

Consumers should not normally need to understand internal class names.

If specific classes are documented as public extension points, they become compatibility commitments.

Avoid exposing internal selectors casually.

---

## 40. Typed API Still Remains Primary

Even if LumaFlow internally uses USS for implementation:

```csharp
Button(
    variant: ButtonVariant.Primary
)
```

remains the consumer API.

Consumers should not need:

```csharp
classes: ["lumaflow-button-primary"]
```

for normal framework behavior.

---

## 41. Pseudo States

UI Toolkit's native pseudo-state/selector mechanisms should be preferred for interaction styles where they work well.

Examples:

```text
hover
active/pressed
focus
disabled
checked
```

Do not update hover colors manually every frame.

---

## 42. Theme State Styling

Component themes may define semantic state styles:

```text
Normal
Hover
Pressed
Focused
Disabled
```

The internal implementation may map these into:

```text
USS rules
native classes
inline transitions
```

depending on UI Toolkit capability.

---

## 43. Dynamic Classes

LumaFlow may toggle classes to represent semantic component state.

Example conceptual:

```text
lumaflow-button
lumaflow-button--primary
lumaflow-button--disabled
```

Class toggling may be preferable to rewriting many inline properties.

---

## 44. Class Naming

Internal classes should follow one consistent convention.

Possible:

```text
luma-button
luma-button--primary
luma-button--danger
luma-card
```

Exact prefix should be decided once.

Avoid generic class names such as:

```text
button
card
selected
```

that may collide with user USS.

---

## 45. User Classes Must Coexist

User classes should not be overwritten by LumaFlow.

Framework state updates must preserve externally supplied class lists.

Do not call operations that clear all classes indiscriminately.

---

## 46. Style Diffing

Repeatedly assigning identical style values can:

```text
create unnecessary native/style invalidation
trigger redundant layout work
increase CPU cost
make profiling noisy
```

LumaFlow should avoid unnecessary repeated style writes.

---

## 47. Initial Diff Strategy

For style-bearing nodes, retain the last resolved style state when useful.

Conceptually:

```text
previous resolved style
        ↓ compare
new resolved style
        ↓
apply only changed properties
```

Do not automatically reapply every style property after every reactive update.

---

## 48. Property-Level Diffing

Example:

Previous:

```text
background = Surface
radius = 12
padding = 16
```

New:

```text
background = Primary
radius = 12
padding = 16
```

LumaFlow should ideally update only:

```text
background
```

not radius and padding again.

---

## 49. Style Diffing Is Not Widget Reconciliation

Important distinction:

```text
style value comparison
```

does not imply:

```text
widget-tree reconciliation
```

This ADR permits localized style comparison even though ADR-004 defers full reconciliation.

---

## 50. Diff Granularity

Do not build an excessively complex generic style diff engine before profiling.

Simple component-specific or category-specific equality checks may be sufficient.

Start with:

```text
clear immutable values
+
equality
+
apply if changed
```

---

## 51. Immutable Values Enable Diffing

This is one reason style values should be immutable.

Example:

```text
oldPadding == newPadding
```

becomes cheap and predictable.

---

## 52. Equality

Small style value structs should implement useful equality semantics.

Examples:

```text
EdgeInsets
BorderSide
BorderRadius
```

Large styles may use value equality where practical.

Avoid reflection-based equality.

---

## 53. Floating-Point Equality

Layout/style values often use floats.

For configuration identity, exact float equality may be acceptable when values are assigned declaratively.

Do not automatically introduce epsilon comparison without a real reason.

The correct semantics should reflect whether the values represent authored configuration or measured runtime geometry.

---

## 54. Clear vs Set

Style updates must handle removal of previous overrides.

Example:

Old Widget:

```csharp
Container(
    width: 300
)
```

New Widget:

```csharp
Container()
```

LumaFlow must not leave the old inline width permanently applied.

The mapper must support:

```text
set property
clear/reset property
leave untouched
```

as distinct operations where required.

---

## 55. Unset Semantics

An unset LumaFlow style value may mean:

```text
return control to theme/native/USS behavior
```

The implementation must use correct UI Toolkit style clearing APIs rather than assigning arbitrary zero/default values.

---

## 56. Initial Mount vs Update

Style mapping has two modes:

```text
initial application
```

and:

```text
update from previous style
```

Initial mount can apply resolved explicit values directly.

Updates should apply changes and clear removed values where needed.

---

## 57. Reactive Styling

Style values may depend on State.

Example conceptual API:

```csharp
Container(
    backgroundColor: selected.Select(
        value => value
            ? context.Theme.Colors.Primary
            : context.Theme.Colors.Surface
    )
)
```

Exact syntax is deferred.

Simple reactive style changes should update the native property directly.

---

## 58. Structural Rebuild Is Not Needed for Style Changes

Changing:

```text
background
opacity
width
padding
text color
```

should normally not rebuild a subtree.

Update native styles directly.

This follows ADR-003 and ADR-004.

---

## 59. Layout-Impacting Styles

Some style changes cause UI Toolkit to relayout.

Examples:

```text
width
height
padding
margin
flex properties
```

This is expected.

LumaFlow updates the style.

UI Toolkit performs layout invalidation and recalculation.

LumaFlow does not manually recalculate geometry.

---

## 60. Rendering-Impacting Styles

Some changes affect visual rendering only.

Examples:

```text
background color
border color
opacity
```

Again, UI Toolkit owns rendering invalidation.

---

## 61. ResolvedStyle

LumaFlow should not use `resolvedStyle` as the primary source of declarative state.

`resolvedStyle` represents computed native outcome.

Application configuration should remain represented by LumaFlow values.

---

## 62. When resolvedStyle Is Appropriate

It may be used for:

```text
measurement
debugging
responsive behavior
native integration
```

where actual computed values are needed.

Do not read back `resolvedStyle` after every style assignment merely to maintain LumaFlow state.

---

## 63. Style Ownership and External Mutation

If users directly modify the underlying VisualElement style after LumaFlow mount, they may conflict with LumaFlow-controlled properties.

The framework should document this boundary.

If LumaFlow owns a property reactively, later native manual mutation may be overwritten.

---

## 64. Safe Native Customization

Future native customization APIs should clarify whether a property is:

```text
LumaFlow controlled
user controlled
```

Avoid promising arbitrary mutation interoperability for the same property simultaneously.

---

## 65. Raw Style Escape Hatch

A low-level typed escape hatch may eventually exist.

Example conceptual:

```csharp
NativeStyle(
    element =>
    {
        element.style.someProperty = ...;
    }
)
```

This should be clearly marked as advanced/native behavior.

It is not the primary styling API.

---

## 66. Custom Style Properties

If UI Toolkit custom USS properties become useful, LumaFlow may expose integration.

This is not part of MVP.

Do not invent a new CSS variable system.

---

## 67. Theme Tokens

Theme values and styling values must work together naturally.

Example:

```csharp
Container(
    padding: EdgeInsets.All(
        context.Theme.Spacing.L
    ),
    decoration: BoxDecoration(
        color: context.Theme.Colors.Surface,
        borderRadius: BorderRadius.All(
            context.Theme.Radius.Large
        )
    )
)
```

---

## 68. Semantic Component Styling

For standard components, prefer:

```csharp
Button(
    "Delete",
    variant: ButtonVariant.Danger
)
```

over manually assembling its standard destructive style through generic decoration properties.

Typed generic style remains available for custom components.

---

## 69. Component Theme Resolver

A Button may resolve:

```text
variant
size
enabled
focus state
theme
explicit overrides
```

into a final resolved style.

Avoid scattering this logic between event handlers and native property setters.

---

## 70. Style State Machine

Complex components may internally model style state.

Conceptually:

```text
ButtonVisualState
=
Normal
Hover
Pressed
Focused
Disabled
```

The resolver determines appropriate style.

Where USS selectors handle these states better, prefer USS.

---

## 71. Animation / Transitions

Future style animations should operate on the same style model.

Potential:

```text
old resolved style
↓
transition
↓
new resolved style
```

Do not create a parallel incompatible animation styling API.

---

## 72. Motion Tokens

Animation duration/curves should use ADR-005 theme Motion tokens where appropriate.

---

## 73. Gradients

Gradients should only be introduced if UI Toolkit provides a clean pipeline-independent representation or an optional effects implementation exists.

Do not add a `Gradient` API that cannot actually be implemented reliably.

---

## 74. Shadows

Standard shadow support should map to native UI Toolkit features where available.

Advanced shadows requiring rendering integration belong outside Core under ADR-006.

---

## 75. Blur

Backdrop blur is not part of Core styling.

It belongs to optional Effects.

Do not hide pipeline-specific rendering behind `BoxDecoration` if that makes Core dependent on rendering backends.

---

## 76. BoxDecoration and Effects Boundary

Core `BoxDecoration` should contain pipeline-independent properties.

Potential effects-specific wrappers should remain separate.

Good:

```csharp
GlassSurface(
    child: Container(
        decoration: BoxDecoration(...)
    )
)
```

instead of putting URP-specific blur configuration into `BoxDecoration`.

---

## 77. Transforms

If UI Toolkit transform APIs are used, LumaFlow may later provide typed wrappers.

Examples:

```text
translation
rotation
scale
transform origin
```

Do not add until real use cases exist.

---

## 78. Opacity

Opacity is a useful generic style property.

Potential API:

```csharp
Opacity(
    value: 0.5f,
    child: content
)
```

or a Container/style property.

Choose based on readability and native hierarchy cost.

---

## 79. Visibility

Visibility semantics differ from opacity.

LumaFlow should distinguish:

```text
hidden/not displayed
invisible but occupying layout
disabled
opacity zero
```

Do not collapse them all into one boolean without considering UI Toolkit behavior.

---

## 80. Display

A typed visibility/display API may map to UI Toolkit `display` or `visibility` semantics.

Exact API should be designed later.

---

## 81. Style Scope

If a subtree requires common native classes or stylesheets, LumaFlow may provide a scope abstraction.

Do not make every child repeat the same configuration.

---

## 82. Style Inheritance

Text and certain UI Toolkit style properties may inherit natively.

LumaFlow should respect native inheritance where practical rather than writing values redundantly to every descendant.

---

## 83. Theme Inheritance vs Native Style Inheritance

These are different systems.

```text
ThemeData
```

is LumaFlow context data.

```text
USS inherited properties
```

are native UI Toolkit style semantics.

They may cooperate but must not be conflated.

---

## 84. Typography Inheritance

Where native text styling inheritance works well, LumaFlow may leverage it.

However, `TextStyle` supplied directly to a Text Widget should remain deterministic.

---

## 85. StyleSheet Lifetime

Externally supplied StyleSheets are borrowed assets.

LumaFlow may attach/detach them but must not destroy them.

This follows ADR-008.

---

## 86. Duplicate StyleSheet Attachment

Avoid repeatedly adding the same StyleSheet to the same native scope.

Use appropriate checks or deterministic ownership.

---

## 87. Runtime Theme Switching

When theme changes, nodes whose resolved styles depend on that theme must update.

Do not blindly rebuild every Widget description.

Prefer:

```text
theme change
↓
style dependents invalidated
↓
resolved style recalculated
↓
changed native properties applied
```

---

## 88. Theme Versioning Optimization

A future ThemeData implementation may expose immutable identity or revision tokens to speed style invalidation.

This is deferred.

Do not prematurely build a complex global theme cache.

---

## 89. Style Caching

Resolved style caching may be valuable for common identical configurations.

Possible cache key:

```text
theme identity
component type
variant
size
state
```

This is an optimization.

Do not make correctness depend on caching.

---

## 90. Cache Safety

Caching requires immutable style/theme inputs.

Do not cache references to mutable objects that can change silently.

---

## 91. Cache Scope

Avoid global unbounded caches.

Possible scopes:

```text
theme-level
mount-level
component static immutable defaults
```

should be evaluated through profiling.

---

## 92. Style Allocation

Ordinary state changes should avoid recreating large style graphs if possible.

For example, changing Button enabled state should not allocate an entire new ThemeData.

---

## 93. Style Objects in Hot Paths

Small value types are acceptable.

Avoid repeated LINQ or reflection during high-frequency style resolution.

---

## 94. Style Application Must Be Deterministic

Applying the same resolved style twice should produce the same native outcome.

Avoid mappers with hidden global state.

---

## 95. No Per-Frame Full Style Reapply

Forbidden default behavior:

```text
Update()
↓
iterate every widget
↓
apply every style property
```

LumaFlow is retained-mode.

Style application is event/change driven.

---

## 96. Diagnostics

Development tooling may eventually show:

```text
Widget explicit style
Theme style
Resolved style
Applied native properties
USS classes
```

This would significantly improve debugging.

---

## 97. Style Source Diagnostics

A future inspector may answer:

```text
Why is this Button blue?
```

with:

```text
ButtonVariant.Primary
→ ButtonTheme.Primary
→ Theme Colors.Primary
```

This is a desirable long-term capability.

---

## 98. USS Conflict Diagnostics

If a user expects USS to override an inline LumaFlow style, the framework should document why it does not.

Future tooling may indicate:

```text
property currently controlled by LumaFlow inline style
```

---

## 99. Public API Should Avoid USS Leakage

A component should not expose:

```csharp
stylePropertyName: "--unity-background-image-tint-color"
```

for common configuration.

Use semantic typed APIs.

---

## 100. Native Style API Availability

LumaFlow should only map style features supported by its declared minimum Unity version or isolate version-specific behavior through compatibility adapters.

---

## 101. Runtime and Editor Consistency

Typed styles should behave consistently between Runtime and Editor where UI Toolkit itself supports equivalent behavior.

Do not create separate styling models.

---

## 102. Render Pipeline Independence

Core style types must remain independent from:

```text
URP
HDRP
Built-in renderer implementation
```

This follows ADR-006.

---

## 103. Platform Independence

Core styling APIs must not contain OS-specific types.

Platform-specific theme adaptation should happen at a higher contextual level if needed.

---

## 104. Tests

Required style tests should include:

```text
EdgeInsets mapping
BorderRadius mapping
border mapping
TextStyle mapping
style precedence
clear previously set value
identical style does not reapply unnecessarily
theme override resolution
USS class preservation
```

---

## 105. Style Diff Tests

Example test:

Initial:

```text
padding = 16
background = Surface
```

Update:

```text
padding = 16
background = Primary
```

Verify:

```text
background changed
padding not redundantly mutated
```

where instrumentation makes this practical.

---

## 106. Style Clear Tests

Initial:

```text
width = 300
```

Update:

```text
width = unset
```

Verify native inline width is cleared appropriately.

---

## 107. Class Preservation Tests

Given:

```text
framework classes
+
user classes
```

state/variant updates must not remove user classes.

---

## 108. Theme Update Tests

Switch theme.

Verify:

```text
theme-dependent styles update
explicit widget overrides remain
unrelated native properties remain unchanged
```

---

## 109. Dogfooding

AudioLib should be used to validate whether:

- BoxDecoration is expressive enough;
- TextStyle is ergonomic;
- USS escape hatches remain usable;
- too many literals remain;
- style precedence is understandable;
- theme changes are cheap enough;
- native UI Toolkit Debugger remains useful.

---

## 110. Rejected Alternative: USS-Only Styling

Rejected as the primary LumaFlow authoring model.

Reasons:

- breaks code-first goal;
- weakens type safety;
- requires file switching;
- dynamic values are harder;
- theme composition is less direct.

USS remains supported.

---

## 111. Rejected Alternative: Inline Style Everything

Also rejected.

Reason:

Writing every property inline would:

- block USS customization;
- increase repeated assignments;
- create unnecessary style invalidation;
- make pseudo-state styling cumbersome.

Use inline style deliberately.

---

## 112. Rejected Alternative: Custom CSS Engine

Rejected:

```text
LumaFlow CSS parser
↓
custom selectors
↓
custom cascade
```

Reasons:

- duplicates USS;
- huge maintenance burden;
- diverges from UI Toolkit;
- unnecessary.

---

## 113. Rejected Alternative: Mutable Style Objects

Rejected as preferred API:

```csharp
style.Padding = ...
style.Color = ...
```

after sharing style references across components.

Immutable values provide safer declarative semantics.

---

## 114. Rejected Alternative: Reflection-Based Style Mapping

Rejected as a foundational mechanism.

Example:

```text
inspect every property of BoxDecoration by reflection
↓
find matching IStyle property
```

Reasons:

- performance;
- IL2CPP/AOT;
- weaker compile-time safety;
- poor diagnostics.

Use typed explicit mapping.

---

## 115. Rejected Alternative: Rebuild Widget for Every Style Update

Rejected.

Style updates should usually mutate native style properties directly.

---

## 116. Rejected Alternative: Hardcode USS Class Names in User APIs

Users should select semantic concepts:

```text
ButtonVariant.Primary
```

not framework internal classes.

---

## 117. Architectural Invariants Created by This ADR

Unless superseded:

### Invariant 1

LumaFlow styling is strongly typed for common use cases.

### Invariant 2

Typed styles map to UI Toolkit native styling.

### Invariant 3

LumaFlow does not implement a separate CSS engine.

### Invariant 4

USS remains interoperable.

### Invariant 5

Not every native style property is eagerly assigned inline.

### Invariant 6

Style updates should apply only actual changes where practical.

### Invariant 7

Previously applied overrides can be cleared.

### Invariant 8

Theme and component styles resolve before native application.

### Invariant 9

Core styling remains render-pipeline independent.

### Invariant 10

Style application is change-driven, not per-frame.

---

## 118. Codex Rules

### Rule 1

Do not duplicate native style mapping code across components.

### Rule 2

Do not introduce string-based styling when a typed API is practical.

### Rule 3

Do not eagerly write every style property to `VisualElement.style`.

### Rule 4

Preserve USS interoperability.

### Rule 5

Do not clear user USS classes during framework state changes.

### Rule 6

When updating styles, compare against previous resolved values where practical.

### Rule 7

Support clearing a previously applied inline override.

### Rule 8

Do not use reflection for core style mapping.

### Rule 9

Do not introduce pipeline-specific effects into Core style objects.

### Rule 10

Before adding a new style abstraction, verify that UI Toolkit can represent it reliably.

---

## 119. Decision Test

When adding a style feature:

```text
Is this common and strongly representable?
        ↓ yes
Create typed style API.

Does UI Toolkit support it natively?
        ↓ yes
Map it.

Is USS better for interaction selectors?
        ↓ yes
Use framework USS internally if appropriate.

Does it require render pipeline integration?
        ↓ yes
Move to Effects.

Is this value unchanged from previous resolved style?
        ↓ yes
Do not reapply unnecessarily.
```

---

## 120. Example: Container Mount

Consumer:

```csharp
Container(
    padding: EdgeInsets.All(16),
    decoration: BoxDecoration(
        color: theme.Colors.Surface,
        borderRadius: BorderRadius.All(12)
    ),
    child: content
)
```

Resolution:

```text
Container Widget
        ↓
ResolvedBoxStyle
        ↓
BoxStyleMapper
```

Native result approximately:

```text
padding-left = 16
padding-top = 16
padding-right = 16
padding-bottom = 16

background-color = Surface

border radii = 12
```

---

## 121. Example: Style Update

State causes background to change.

Previous:

```text
background = Surface
padding = 16
radius = 12
```

Next:

```text
background = Primary
padding = 16
radius = 12
```

LumaFlow applies:

```text
background = Primary
```

and leaves unchanged properties untouched.

---

## 122. Example: Removing Override

Previous Widget:

```csharp
Container(
    width: 300,
    child: content
)
```

Updated Widget:

```csharp
Container(
    child: content
)
```

LumaFlow clears the previously owned inline width so native/USS sizing can take effect again.

---

## 123. Example: USS Interop

```csharp
Container(
    classes:
    [
        "audio-card"
    ],
    decoration: BoxDecoration(
        borderRadius: BorderRadius.All(12)
    ),
    child: content
)
```

USS may control:

```text
background
hover
transition
```

while LumaFlow explicitly controls:

```text
border radius
```

The developer must be able to reason about this precedence.

---

## 124. Initial MVP Styling Set

MVP should initially implement:

```text
EdgeInsets
BorderSide
Border
BorderRadius
BoxDecoration
TextStyle

padding
margin
width
height
min/max size
background color
border
radius
opacity
basic text styling
USS classes
```

Do not attempt every UI Toolkit style property immediately.

---

## 125. Final Decision

LumaFlow's styling model is:

```text
typed declarative style values
+
theme resolution
+
component style resolution
+
targeted native style mapping
+
USS interoperability
+
localized style updates
```

The framework should make styling pleasant without hiding UI Toolkit's native style system or replacing it.

The guiding rule is:

**LumaFlow describes visual intent.  
The style layer resolves it.  
UI Toolkit renders it.**