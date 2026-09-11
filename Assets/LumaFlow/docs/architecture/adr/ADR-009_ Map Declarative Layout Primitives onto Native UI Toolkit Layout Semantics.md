# ADR-009: Map Declarative Layout Primitives onto Native UI Toolkit Layout Semantics

- **Status:** Accepted
- **Decision date:** 2026-08-11
- **Scope:** Layout primitives, sizing, alignment, flex behavior, positioning
- **Affects:** Runtime, Widgets, Layout, Styling, Responsive UI, Testing
- **Related documents:** `ARCHITECTURE.md`, `API_DESIGN.md`, `ADR-001-native-uitoolkit.md`, `ADR-007-widget-runtime-model.md`

---

## 1. Context

LumaFlow aims to provide familiar declarative layout primitives such as:

```text
Row
Column
Expanded
Flexible
Padding
Center
Align
Spacer
SizedBox
Container
Stack
Positioned
```

These concepts are intentionally familiar to developers coming from Flutter and other declarative UI systems.

However, Unity UI Toolkit already has its own layout system based primarily on Flexbox/Yoga semantics.

LumaFlow must therefore decide whether to:

1. reproduce Flutter layout behavior exactly;
2. implement its own layout engine;
3. or expose familiar declarative concepts that map predictably onto UI Toolkit.

Implementing a second layout engine would violate ADR-001.

Trying to reproduce Flutter semantics exactly would also create misleading behavior where UI Toolkit and Flutter differ.

---

## 2. Decision

LumaFlow will expose high-level declarative layout primitives while delegating actual layout calculation to Unity UI Toolkit.

The architectural relationship is:

```text
LumaFlow Layout API
        ↓
typed semantic mapping
        ↓
UI Toolkit style/layout properties
        ↓
Yoga / UI Toolkit layout
```

LumaFlow does not calculate ordinary layout itself.

---

## 3. Core Principle

LumaFlow layout names may be Flutter-inspired.

LumaFlow layout behavior must remain compatible with UI Toolkit.

The rule is:

```text
Familiar API
+
Unity-native semantics
```

not:

```text
Flutter API
+
forced Flutter implementation
```

---

## 4. Layout Responsibilities

LumaFlow owns:

```text
declarative layout vocabulary
typed parameters
defaults
style mapping
component composition
validation
developer ergonomics
```

UI Toolkit owns:

```text
actual measurements
flex layout
size negotiation
native geometry
final positions
native hierarchy layout
```

---

## 5. No Custom General Layout Solver

Forbidden:

```text
Row
↓
measure all children manually
↓
calculate positions
↓
assign absolute coordinates
```

for ordinary Row/Column behavior.

Preferred:

```text
Row
↓
VisualElement
↓
flexDirection = Row
↓
UI Toolkit calculates layout
```

---

## 6. Row

`Row` represents a horizontal flex layout.

Conceptually:

```csharp
Row(
    children:
    [
        Text("Left"),
        Text("Right")
    ]
)
```

maps to approximately:

```text
VisualElement
flex-direction: row
```

The exact native configuration may additionally include framework defaults.

---

## 7. Column

`Column` represents a vertical flex layout.

Conceptually:

```csharp
Column(
    children:
    [
        Text("Title"),
        Text("Subtitle")
    ]
)
```

maps to approximately:

```text
VisualElement
flex-direction: column
```

---

## 8. Main Axis

The meaning of the main axis depends on layout direction.

For:

```text
Row
```

main axis is horizontal.

For:

```text
Column
```

main axis is vertical.

LumaFlow should expose this through:

```text
MainAxisAlignment
```

rather than requiring consumers to reason directly about Flexbox terminology.

---

## 9. MainAxisAlignment

Initial semantic values should include:

```csharp
public enum MainAxisAlignment
{
    Start,
    Center,
    End,
    SpaceBetween,
    SpaceAround,
    SpaceEvenly
}
```

Support depends on what can be represented reliably through targeted UI Toolkit versions.

Where a requested semantic does not map exactly, LumaFlow must document the difference rather than secretly implementing a second layout system.

---

## 10. Main Axis Mapping

Conceptually:

```text
Start
    ↓
flex-start

Center
    ↓
center

End
    ↓
flex-end

SpaceBetween
    ↓
space-between

SpaceAround
    ↓
space-around

SpaceEvenly
    ↓
native equivalent where supported
```

Exact mapping belongs in one centralized layout mapper.

---

## 11. Cross Axis

Cross-axis alignment should be exposed through:

```text
CrossAxisAlignment
```

Initial values:

```csharp
public enum CrossAxisAlignment
{
    Start,
    Center,
    End,
    Stretch
}
```

Potential baseline support may be introduced only if UI Toolkit semantics and text layout make it reliable.

---

## 12. Cross Axis Mapping

Conceptually:

```text
Start
    ↓
flex-start

Center
    ↓
center

End
    ↓
flex-end

Stretch
    ↓
stretch
```

Do not duplicate mapping logic inside Row and Column independently if a shared semantic mapper is appropriate.

---

## 13. Layout Defaults

Defaults must be predictable.

Suggested defaults:

```text
Row:
mainAxisAlignment = Start
crossAxisAlignment = Center or Stretch depending final validation

Column:
mainAxisAlignment = Start
crossAxisAlignment = Stretch
```

The exact defaults must be validated using real UI Toolkit behavior.

Do not copy Flutter defaults mechanically if they lead to unintuitive Unity UI.

---

## 14. Default Cross-Axis Decision

During implementation, cross-axis defaults must be tested against:

```text
text
buttons
inputs
containers
nested layouts
editor controls
```

A default that causes frequent explicit overrides is a bad default.

The public API should optimize for common Unity application layouts.

---

## 15. Gap

`Row` and `Column` should support:

```csharp
gap: 12
```

as a first-class API.

Desired:

```csharp
Column(
    gap: 16,
    children:
    [
        Text("One"),
        Text("Two"),
        Text("Three")
    ]
)
```

This is preferable to users manually adding Spacer widgets between every child.

---

## 16. Native Gap Preference

If the targeted UI Toolkit version provides suitable native gap behavior, LumaFlow should use it.

Preferred:

```text
gap
↓
native layout gap
```

not:

```text
gap
↓
insert additional VisualElements
```

---

## 17. Gap Fallback

If native gap is unavailable or insufficient in a supported Unity version, LumaFlow may emulate gap.

Preferred fallback:

```text
modify spacing between actual children
```

rather than introducing decorative Spacer nodes when possible.

The implementation must preserve:

- first child behavior;
- last child behavior;
- dynamic children;
- layout direction;
- no accidental double spacing.

---

## 18. Gap Is Not Margin

Gap represents spacing between siblings.

It must not automatically alter:

```text
outer container spacing
first child leading spacing
last child trailing spacing
```

unless explicitly documented.

---

## 19. Padding

`Padding` adds inner space around one child.

Desired:

```csharp
Padding(
    padding: EdgeInsets.All(16),
    child: content
)
```

Conceptually:

```text
PaddingNode
    ↓
VisualElement
    ↓
native padding
    ↓
child
```

---

## 20. EdgeInsets

Padding should use typed values.

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
    left: 8,
    top: 4,
    right: 8
)
```

---

## 21. Padding Wrapper Optimization

Initial implementation may use a wrapper VisualElement.

This is acceptable.

Do not prematurely optimize it away if doing so complicates ownership or composition.

Later optimization may fold padding into another compatible node if profiling proves value.

---

## 22. Margin

Margin is a styling/layout property, not necessarily a dedicated Widget.

Potential usage:

```csharp
Container(
    margin: EdgeInsets.All(8),
    child: content
)
```

or through a typed style API.

A dedicated `Margin` Widget may be added only if it materially improves readability.

---

## 23. Center

`Center` is a semantic convenience for centering one child.

Desired:

```csharp
Center(
    child: spinner
)
```

The implementation should use native flex/alignment behavior.

Do not manually calculate coordinates.

---

## 24. Center Semantics

Center should mean approximately:

```text
center child on both axes inside available space
```

where the parent constraints allow it.

Exact behavior remains governed by UI Toolkit sizing semantics.

---

## 25. Align

`Align` allows explicit child alignment.

Desired:

```csharp
Align(
    alignment: Alignment.TopRight,
    child: content
)
```

The exact Alignment model should map onto native layout/positioning capabilities.

---

## 26. Alignment Type

Potential API:

```csharp
Alignment.Center
Alignment.TopLeft
Alignment.TopCenter
Alignment.TopRight
Alignment.CenterLeft
Alignment.CenterRight
Alignment.BottomLeft
Alignment.BottomCenter
Alignment.BottomRight
```

Do not introduce arbitrary normalized coordinate alignment until needed.

---

## 27. Expanded

`Expanded` represents a child that consumes remaining flex space.

Desired:

```csharp
Row(
    children:
    [
        Text("Name"),

        Expanded(
            child: TextField(...)
        )
    ]
)
```

---

## 28. Expanded Mapping

Conceptually:

```text
Expanded
    ↓
flex-grow > 0
```

Potential defaults:

```text
flex-grow = 1
flex-shrink = 1
```

with basis behavior chosen after UI Toolkit testing.

The final implementation must match practical Unity layout expectations rather than Flutter internals.

---

## 29. Expanded Flex Factor

Support:

```csharp
Expanded(
    flex: 2,
    child: content
)
```

where:

```text
flex > 0
```

determines relative distribution among expanded siblings.

Invalid zero/negative values should fail clearly.

---

## 30. Expanded Is Contextual

`Expanded` only makes semantic sense inside compatible flex layouts.

Example:

```text
Row
Column
```

If used where its behavior cannot be represented meaningfully, the framework may:

- allow native semantics if harmless;
- or provide development diagnostics.

Do not implement expensive runtime ancestry policing unless real misuse is common.

---

## 31. Flexible

`Flexible` allows a child to participate in flexible sizing without necessarily forcing it to consume all remaining space.

Potential API:

```csharp
Flexible(
    flex: 1,
    fit: FlexFit.Loose,
    child: content
)
```

This concept must be validated carefully against UI Toolkit flex-basis/grow/shrink behavior.

---

## 32. FlexFit

If introduced:

```csharp
public enum FlexFit
{
    Tight,
    Loose
}
```

must map predictably to UI Toolkit behavior.

Do not expose `FlexFit` merely because Flutter has it.

If UI Toolkit cannot represent the semantics cleanly, redesign the API.

---

## 33. Spacer

`Spacer` is a convenience flexible empty region.

Desired:

```csharp
Row(
    children:
    [
        Text("Left"),
        Spacer(),
        Button("Right")
    ]
)
```

Conceptually equivalent to a flexible empty element.

---

## 34. Spacer Flex

Potential:

```csharp
Spacer(flex: 1)
```

Multiple spacers may divide available space proportionally.

---

## 35. SizedBox

`SizedBox` provides explicit size constraints or empty fixed space.

Examples:

```csharp
SizedBox(
    width: 200,
    child: content
)
```

or:

```csharp
SizedBox(height: 16)
```

for explicit empty spacing.

---

## 36. SizedBox vs Spacer

These semantics differ:

```text
SizedBox(height: 16)
=
fixed spacing
```

```text
Spacer()
=
flexible remaining spacing
```

Documentation should make this distinction obvious.

---

## 37. Container

`Container` is a convenience composition widget combining common box concerns.

Potential properties:

```text
width
height
minWidth
minHeight
maxWidth
maxHeight

padding
margin
alignment
decoration

child
```

Do not let Container absorb every layout feature in the framework.

---

## 38. Container Responsibility

Container may combine frequently co-occurring behavior where that significantly reduces nesting.

Good:

```csharp
Container(
    padding: EdgeInsets.All(16),
    decoration: BoxDecoration(...),
    child: content
)
```

But specialized semantics should remain separate where they improve readability.

---

## 39. Constraints

LumaFlow should eventually support typed constraints.

Potential:

```csharp
BoxConstraints(
    minWidth: 200,
    maxWidth: 600
)
```

or direct Container parameters.

The initial implementation should prefer the simplest API that maps cleanly to UI Toolkit.

---

## 40. Width and Height

Simple fixed dimensions should remain concise.

Example:

```csharp
SizedBox(
    width: 320,
    height: 64
)
```

Numeric values initially represent UI Toolkit pixel-like lengths.

---

## 41. Percentage Sizes

Percent-based sizing should use typed values once supported.

Avoid:

```csharp
width: "100%"
```

Potential future:

```csharp
width: Length.Percent(100)
```

while still supporting:

```csharp
width: 320
```

through ergonomic overloads or conversions if safe.

---

## 42. Auto Size

UI Toolkit's automatic sizing behavior should remain accessible.

Do not turn unspecified width/height into zero or explicit dimensions.

Unspecified means:

```text
let UI Toolkit resolve size
```

---

## 43. Min/Max Sizes

Expose:

```text
minWidth
maxWidth
minHeight
maxHeight
```

through typed style/layout APIs where useful.

Do not create a custom constraint solver.

---

## 44. Fit Behavior

Concepts such as:

```text
fit content
fill available
fixed size
percentage
```

should map to native style semantics rather than internal layout calculations.

---

## 45. Stack

`Stack` represents overlapping children.

Desired:

```csharp
Stack(
    children:
    [
        background,

        Positioned(
            top: 12,
            right: 12,
            child: badge
        )
    ]
)
```

---

## 46. Stack Implementation

Stack should use UI Toolkit positioning.

Conceptually:

```text
Stack root
    ↓
position context
```

Children may remain normally positioned unless wrapped/configured as `Positioned`.

---

## 47. Positioned

`Positioned` expresses absolute offsets inside a Stack or compatible positioned container.

Potential API:

```csharp
Positioned(
    top: 8,
    right: 8,
    child: content
)
```

It maps to native absolute positioning.

---

## 48. Positioned Properties

Potential:

```text
left
top
right
bottom
width
height
```

The API must validate contradictory or unsupported combinations where necessary.

---

## 49. No Custom Stack Solver

LumaFlow does not manually compute:

```text
x
y
width
height
```

for Positioned children if UI Toolkit can resolve them through style properties.

---

## 50. Absolute Positioning Is Explicit

Normal LumaFlow layout should remain flex-based.

Absolute positioning should require explicit intent:

```text
Stack
Positioned
```

Avoid components silently using absolute coordinates for general layout.

---

## 51. ScrollView

`ScrollView` should adapt UI Toolkit's native ScrollView.

Desired:

```csharp
ScrollView(
    child: Column(...)
)
```

or multi-child convenience depending final API.

---

## 52. Scroll Direction

Potential:

```csharp
ScrollView(
    direction: Axis.Vertical,
    child: content
)
```

Possible enum:

```csharp
public enum Axis
{
    Horizontal,
    Vertical
}
```

Bidirectional scrolling may use a separate mode if supported.

---

## 53. Native Scroll Semantics

Scrolling, clipping, wheel events, touch handling, and scrollbars should remain UI Toolkit-native.

Do not implement manual scroll offsets.

---

## 54. ListView

Large repeated collections use native UI Toolkit virtualization.

`ListView` is not merely:

```text
Column + ScrollView
```

for large data sets.

This follows ADR-004.

---

## 55. Wrap Layout

A future `Wrap` widget may be useful.

Example:

```csharp
Wrap(
    gap: 8,
    children: tags
)
```

It should only be implemented if UI Toolkit can support equivalent wrapping semantics predictably.

Do not emulate complex line-breaking manually unless justified.

---

## 56. Direction

Row/Column direction is semantic.

Avoid exposing:

```csharp
flexDirection: FlexDirection.Row
```

on Row itself.

That would make the component internally contradictory.

Use lower-level Container/native styling when direct flex control is needed.

---

## 57. Reverse Direction

Potential future:

```csharp
Row(
    reverse: true,
    ...
)
```

or:

```text
RowReverse
```

should only be introduced when real use cases exist.

Do not expose every Flexbox feature immediately.

---

## 58. Flex Wrap

Flex wrapping may be supported by a dedicated `Wrap` component or lower-level Flex widget.

Avoid bloating Row/Column before requirements are clear.

---

## 59. Low-Level Flex Widget

A future lower-level layout primitive may expose native concepts:

```csharp
Flex(
    direction: Axis.Horizontal,
    ...
)
```

This can support advanced cases.

Row and Column remain ergonomic high-level wrappers.

---

## 60. High-Level vs Low-Level Layout

The intended layering is:

```text
Row / Column / Expanded / Padding
        ↓
high-level semantic API

Flex / raw style configuration
        ↓
advanced API

VisualElement
        ↓
native escape hatch
```

Users should not need low-level APIs for ordinary layouts.

---

## 61. Overflow

LumaFlow should not invent layout overflow behavior inconsistent with UI Toolkit.

If content exceeds available size, native UI Toolkit semantics apply unless a component explicitly manages scrolling/clipping.

---

## 62. Clip Behavior

Clipping should be an explicit typed styling/layout option where useful.

Do not automatically clip every Container.

Native default behavior should be respected.

---

## 63. Intrinsic Measurement

Do not implement Flutter-style intrinsic measurement APIs unless UI Toolkit provides a reliable equivalent and real user need exists.

Features such as:

```text
IntrinsicWidth
IntrinsicHeight
```

are deferred.

---

## 64. Baseline Alignment

Text baseline alignment can be complex.

Do not expose:

```text
CrossAxisAlignment.Baseline
```

until verified against native UI Toolkit support.

An API that only approximately works is worse than not exposing it initially.

---

## 65. AspectRatio

A future:

```csharp
AspectRatio(
    ratio: 16f / 9f,
    child: content
)
```

may be added if UI Toolkit supports a clean implementation.

This is not required for MVP.

---

## 66. FractionallySizedBox

Flutter-specific layout helpers should not automatically be copied.

Prefer general typed width/height percentages if they solve the same problem cleanly.

---

## 67. FittedBox

Likewise, `FittedBox` should not exist merely for API familiarity.

Add only if a common Unity UI use case requires the semantic behavior.

---

## 68. ConstrainedBox

Potential constraint widgets may be useful, but avoid duplicating functionality already expressible concisely through Container/SizedBox.

---

## 69. Layout Literals

Common dimensions can be explicit application values:

```csharp
width: 320
```

but framework default control dimensions should come from theme/design tokens where they express design-system defaults.

Layout and theming responsibilities must remain distinct.

---

## 70. Layout vs Theme

Example:

```text
Row gap chosen by screen composition
```

may be an explicit layout value.

Example:

```text
default Button height
```

belongs to ButtonTheme.

Not every numeric layout value must come from ThemeData.

---

## 71. Theme Spacing Convenience

Application code may use:

```csharp
gap: context.Theme.Spacing.M
```

when desired.

LumaFlow should not require all application-specific layout to use design tokens.

---

## 72. Responsive Layout

Future responsive functionality should build on panel size and existing layout primitives.

Potential:

```csharp
ResponsiveBuilder(
    builder: context =>
        context.Width < 800
            ? Column(...)
            : Row(...)
)
```

This creates structural changes through explicit rebuild boundaries.

---

## 73. No Device-Type Assumptions

Responsive layout should not primarily ask:

```text
IsMobile
IsDesktop
```

where panel width/available space is the real layout constraint.

Unity applications may run:

- windowed;
- embedded;
- inside Editor;
- on unusual aspect ratios.

Prefer available layout metrics.

---

## 74. MediaQuery

A future `MediaQuery` may expose:

```text
panel width
panel height
pixel scaling
safe area
orientation
```

It does not replace Yoga layout.

It supports higher-level responsive composition decisions.

---

## 75. Geometry Callbacks

UI Toolkit geometry-change callbacks may be used internally for features requiring resolved dimensions.

Avoid using them for normal Row/Column layout.

---

## 76. Layout Feedback Loops

Any feature reacting to measured geometry must avoid:

```text
geometry changes
↓
state changes
↓
layout changes
↓
geometry changes forever
```

Responsive infrastructure must be carefully scoped.

---

## 77. Layout Mapping Layer

Native layout assignments should be centralized where practical.

Potential:

```text
FlexLayoutMapper
AlignmentMapper
SizeMapper
PositionMapper
```

Avoid each component reimplementing enum-to-native mappings.

---

## 78. Avoid Giant LayoutMapper

Do not create one enormous switch-based class responsible for every style and layout concept.

Keep concerns focused.

---

## 79. Immutable Layout Values

Types such as:

```text
EdgeInsets
BoxConstraints
Alignment
```

should preferably be immutable value types or immutable classes.

This improves:

- sharing;
- equality;
- predictable behavior;
- future caching.

---

## 80. Validation

Invalid layout configuration should fail clearly.

Examples:

```text
Expanded flex <= 0
negative sizes where unsupported
minWidth > maxWidth
minHeight > maxHeight
```

Do not silently reinterpret invalid values unless native semantics clearly justify it.

---

## 81. Negative Margin

If UI Toolkit supports negative margin, LumaFlow may expose it.

Do not arbitrarily prohibit native-supported advanced behavior unless it breaks framework invariants.

---

## 82. NaN and Infinity

Public numeric layout APIs should validate obviously invalid values where appropriate.

Example:

```text
NaN width
```

should not silently propagate into unpredictable runtime behavior.

---

## 83. Native Escape Hatch

Advanced users must still be able to manipulate native styles through deliberate APIs or custom VisualElements.

LumaFlow high-level layout does not attempt to expose every UI Toolkit property.

---

## 84. Custom VisualElements

A native custom element participating in LumaFlow layout should remain a normal flex child.

Example:

```csharp
Row(
    children:
    [
        Native(customElement),
        Expanded(
            child: Text("Info")
        )
    ]
)
```

No special layout bridge should be required for ordinary native elements.

---

## 85. Layout Node Hierarchy

Conceptually:

```text
Column Widget
    ↓
ColumnNode
    ↓
VisualElement
```

with mounted child nodes attached beneath the native container.

This follows ADR-007.

---

## 86. Structural Layout Nodes

Some layout semantics may later avoid native wrappers.

Example:

```text
ExpandedNode
```

could configure its child root rather than introduce:

```text
extra VisualElement
```

if lifecycle and style semantics permit.

This is an optimization.

---

## 87. Wrapper Reduction

Do not optimize away wrappers unless there is evidence of:

- hierarchy performance issues;
- styling limitations;
- debugging noise;
- significant memory overhead.

Correctness and simple semantics come first.

---

## 88. Wrapper Elimination Safety

If a wrapper is removed, ensure semantics still hold for:

```text
padding
background
focus
pointer picking
USS classes
native access
layout
```

A seemingly redundant wrapper may carry behavior.

---

## 89. Picking/Input

Layout wrappers should not accidentally intercept pointer input unless intended.

Native `pickingMode` and related behavior should be considered where wrappers exist solely for layout.

---

## 90. Focus

Pure layout containers should generally not become focusable by default.

They exist to structure UI, not alter keyboard navigation.

---

## 91. Accessibility

Layout composition should preserve semantic controls.

Do not turn Buttons into generic VisualElements merely to simplify flex behavior.

---

## 92. Layout Debugging

Generated native hierarchy should remain understandable in UI Toolkit Debugger.

A developer should be able to inspect:

```text
Row container
Column container
native styles
resolved geometry
```

when debugging layout.

---

## 93. Development Diagnostics

Future diagnostics may detect common mistakes:

```text
Expanded outside flex parent
absolute child outside positioning context
impossible constraints
```

These should be warnings/errors only when reliable.

---

## 94. Performance

Ordinary layout updates should rely on UI Toolkit's native invalidation/layout pipeline.

LumaFlow should not perform its own per-frame layout passes.

---

## 95. No Update Loop

Forbidden:

```text
Update()
↓
calculate all widget sizes
↓
assign positions every frame
```

for normal layout.

The retained native layout system owns this.

---

## 96. Dynamic Layout Properties

Reactive bindings may update layout properties.

Example:

```text
State<float> sidebarWidth
↓
native width style
↓
UI Toolkit relayout
```

No Widget reconstruction is needed if the hierarchy remains the same.

---

## 97. Structural Layout Changes

Switching:

```text
Column
```

to:

```text
Row
```

may be implemented either by:

- updating flex direction in place when the same semantic component supports it;
- or using an explicit responsive structural boundary.

Choose the smallest clean update.

---

## 98. Row/Column Identity

Row and Column may internally share a common Flex implementation.

Publicly they remain separate semantic Widgets because:

```text
Row(...)
```

and:

```text
Column(...)
```

are easier to read.

---

## 99. Internal Reuse

Potential:

```text
FlexNode
```

with Row/Column configuration is acceptable internally.

Do not expose implementation complexity unnecessarily.

---

## 100. API Readability

Prefer:

```csharp
Row(
    gap: 12,
    mainAxisAlignment: MainAxisAlignment.SpaceBetween,
    children:
    [
        Text("Status"),
        Badge(...)
    ]
)
```

over:

```csharp
Flex(
    direction: FlexDirection.Row,
    justifyContent: Justify.SpaceBetween,
    alignItems: Align.Center,
    ...
)
```

for common UI.

---

## 101. Native Terminology Escape Hatch

Advanced consumers who know Flexbox/UI Toolkit may use lower-level styling where needed.

LumaFlow does not need to make every native property part of Row/Column.

---

## 102. Layout Consistency Across Runtime and Editor

Shared layout widgets should have the same semantic behavior in:

```text
Runtime UI
Editor UI
```

where UI Toolkit itself behaves consistently.

Do not create separate layout APIs for Editor.

---

## 103. Render Pipeline Independence

Layout APIs must remain independent from:

```text
Built-in
URP
HDRP
```

No ordinary layout implementation should reference rendering pipelines.

---

## 104. Platform Independence

Layout should not contain OS-specific logic for:

```text
Windows
macOS
Linux
Android
iOS
WebGL
```

except optional responsive/environment adapters where genuinely required.

---

## 105. Testing Strategy

Layout tests should verify native style mappings and resulting hierarchy behavior where feasible.

Required areas:

```text
Row direction
Column direction
main-axis mapping
cross-axis mapping
gap
padding
Expanded
Spacer
fixed size
min/max size
Stack
Positioned
nested layouts
```

---

## 106. Visual Integration Tests

Some layout behavior is difficult to prove with property tests alone.

Representative sample layouts should be used to detect regressions.

Examples:

```text
settings screen
toolbar
sidebar layout
card grid
nested form
stacked badge
```

---

## 107. Dogfooding Questions

During AudioLib UI development, evaluate:

1. Are Row/Column defaults convenient?
2. Is `Expanded` predictable?
3. Is `gap` sufficient?
4. Are too many Padding/Container wrappers required?
5. Are common UI Toolkit flex features missing?
6. Does Stack behave naturally?
7. Are layout errors understandable?
8. Does the API remain easier than raw UI Toolkit?

---

## 108. Rejected Alternative: Flutter Layout Engine Clone

Rejected:

```text
LumaFlow
↓
Flutter-like constraint solver
↓
manual child measurements
↓
native element positioning
```

Reasons:

- duplicates UI Toolkit;
- creates complex measurement semantics;
- creates performance risk;
- conflicts with ADR-001;
- makes native interop harder.

---

## 109. Rejected Alternative: Raw Flexbox Only

Also rejected as the primary API:

```csharp
new Flex(
    direction: ...,
    grow: ...,
    shrink: ...
)
```

for every layout.

Reason:

LumaFlow exists to provide a more semantic declarative developer experience.

Raw Flexbox remains available as an advanced layer.

---

## 110. Rejected Alternative: Absolute Positioning by Default

Rejected:

```text
each Widget has x/y/width/height
```

as general layout.

Reasons:

- poor responsiveness;
- difficult composition;
- duplicates native layout;
- unsuitable for Editor windows and variable panels.

---

## 111. Rejected Alternative: Spacer Elements for Every Gap

Rejected as preferred implementation when native gap is available.

Artificial Spacer children:

- increase hierarchy;
- complicate dynamic children;
- affect indexing;
- create semantic noise.

Use native spacing first.

---

## 112. Rejected Alternative: Copy Every Flutter Layout Widget

Rejected.

LumaFlow should not automatically implement:

```text
IntrinsicHeight
IntrinsicWidth
FractionallySizedBox
FittedBox
LimitedBox
OverflowBox
Baseline
CustomMultiChildLayout
```

solely because they exist in Flutter.

Add abstractions based on Unity use cases.

---

## 113. Architectural Invariants Created by This ADR

Unless superseded:

### Invariant 1

UI Toolkit calculates ordinary layout.

### Invariant 2

Row maps to horizontal flex semantics.

### Invariant 3

Column maps to vertical flex semantics.

### Invariant 4

Expanded maps to native flexible sizing.

### Invariant 5

Stack/Positioned use native positioning.

### Invariant 6

LumaFlow does not implement a general layout solver.

### Invariant 7

High-level layout APIs use semantic names.

### Invariant 8

Advanced native flex behavior remains accessible.

### Invariant 9

Layout behavior prioritizes Unity-native predictability over exact Flutter compatibility.

### Invariant 10

Render pipelines do not affect ordinary layout.

---

## 114. Codex Rules

### Rule 1

Do not manually calculate normal Row/Column child geometry.

### Rule 2

Map layout semantics to supported UI Toolkit styles.

### Rule 3

Do not add a custom measurement/layout engine.

### Rule 4

Do not expose raw Flexbox terminology when an existing LumaFlow semantic type already covers the use case.

### Rule 5

Do not promise exact Flutter behavior when UI Toolkit differs.

### Rule 6

Centralize enum/style mappings rather than duplicating them across widgets.

### Rule 7

Use native gap where supported before emulating it.

### Rule 8

Do not insert extra native wrapper elements without purpose.

### Rule 9

Do not optimize away wrappers before validating lifecycle and style semantics.

### Rule 10

When adding a Flutter-inspired layout widget, first prove a Unity UI Toolkit use case for it.

---

## 115. Decision Test

When adding layout behavior:

```text
Can UI Toolkit Flexbox express it directly?
        ↓ yes
Map to native styles.

Can a simple semantic wrapper express it?
        ↓ yes
Create small LumaFlow abstraction.

Does it require manual geometry calculation?
        ↓ yes
Is this truly a specialized layout?
        ↓ no
Redesign.

Is the API being added only because Flutter has it?
        ↓ yes
Do not add it without a Unity use case.
```

---

## 116. Initial MVP Layout Set

The first stable layout set should include:

```text
Row
Column
Padding
Center
Align
Expanded
Spacer
SizedBox
Container
```

Then:

```text
Stack
Positioned
```

after basic flex semantics are proven.

---

## 117. Example Target

```csharp
return Column(
    gap: 16,
    padding: EdgeInsets.All(24),
    children:
    [
        Text(
            "Audio Library",
            style: context.Theme.Typography.TitleLarge
        ),

        Row(
            gap: 12,
            children:
            [
                Expanded(
                    child: TextField(
                        label: "Search",
                        value: search
                    )
                ),

                Button(
                    "Refresh",
                    onPressed: Refresh
                )
            ]
        ),

        Expanded(
            child: ScrollView(
                child: AudioList()
            )
        )
    ]
);
```

The LumaFlow tree expresses intent.

The resulting native hierarchy remains UI Toolkit.

UI Toolkit calculates the actual geometry.

---

## 118. Final Decision

LumaFlow's layout model is:

```text
semantic declarative layout API
+
typed configuration
+
native UI Toolkit Flexbox/Yoga
```

not:

```text
custom layout engine
```

The framework should make common layout dramatically easier to author while remaining predictable to developers who understand Unity UI Toolkit.

The guiding rule is:

**LumaFlow names the layout intent.  
UI Toolkit performs the layout.**