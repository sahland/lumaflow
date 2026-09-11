# LumaFlow Development Roadmap

## 1. Purpose

This document defines the development order for LumaFlow.

Its purpose is to prevent premature work on advanced features before the core architecture has been validated.

LumaFlow must be developed incrementally.

Each phase should:

- introduce a small coherent capability;
- validate architecture through real usage;
- add tests;
- avoid unnecessary public API commitments;
- establish clear criteria before moving forward.

Codex must follow this roadmap unless a task explicitly requires changing priorities.

## 1.1. Active stabilization track (locked 2026-08-20)

The framework has moved beyond the original prototype milestones below. The active
work order is now the following stabilization track. This section takes precedence
over the historical phase numbering when the two conflict.

### S0 — General reconciliation and lifecycle correctness (completed 2026-08-24)

- reconcile compatible widgets by type, position and explicit key;
- preserve native element identity, focus and controlled state;
- update inherited scopes without remounting unaffected descendants;
- make form, focus, responsive-builder, scaffold, overlay and reactive nodes
  participate in the same update contract;
- retain inactive Navigator route nodes instead of reconstructing route state
  (implemented under ADR-023; lifecycle edge cases are contract-covered and a
  named performance baseline suite is available; measured budgets remain);
- prove deterministic subscription, event-handler and child-node cleanup.

Primary decisions:

- `ADR-021_ Introduce General Local Reconciliation and Stable Widget Keys.md`;
- `ADR-022_ Standardize Interactive Styling with WidgetStateProperty.md`.

### S1 — Flutter-inspired declarative API consistency (completed 2026-08-24)

- keep `BuildContext`-based inherited data predictable and locally overridable;
- finish `WidgetStateProperty<T>` coverage for every interactive component;
- standardize controlled/uncontrolled value ownership, focus, validation and
  callbacks across inputs;
- remove one-off component update paths and ambiguous public APIs;
- compare ergonomics with current Flutter APIs, while adapting semantics to UI
  Toolkit instead of copying Flutter internals mechanically.

Current progress: `TextFieldStyle` and `DropdownStyle` now share the
state-property contract used by buttons, Checkbox, Radio, Switch, and Slider.
TextField retains its legacy constructor fallback; Dropdown has a tree-scoped
component theme for its native anchor. The remaining interactive API surface is
being audited for intentional native aliases and missing adapters. The ambiguous
checkbox-shaped `Toggle` is deprecated, dogfood has migrated to explicit
`Checkbox`/`Switch` semantics, and removal is reserved for a documented pre-1.0
breaking-change window.

Controlled ownership is standardized under ADR-024: every active value input
accepts external `State<T>` or `FormField<T>`, optional `onChanged` reports only
user-originated commits, and disabled/unmounted adapters reject synthetic events.
Slider now participates in form validation and error-state styling. Single-line
TextField submission and Slider start/end interaction boundaries are standardized
for compatible updates, pointer capture, and non-pointer native changes. Richer
IME/editing actions and input formatting remain later input-API audits rather
than blockers for the S2 layout pass.

Primary S1 decision:

- `ADR-024_ Standardize Controlled Input Ownership and User Change Callbacks.md`.

### S2 — Layout correctness and dogfood ergonomics (completed 2026-08-24)

- harden Flex/Yoga mapping, constraints, intrinsic assumptions and responsive
  rebuild behavior;
- validate state preservation across width, theme and content changes;
- rebuild the real dogfood application with reusable feature-first widgets;
- treat repeated dogfood workarounds as framework defects or missing APIs;
- add visual and contract regressions for representative application screens.

### S3 — Accessibility and localization (completed 2026-08-24)

- define semantics, labels, roles, focus order, keyboard operation and disabled
  behavior for all core controls;
- add scalable text and localization adapters without forcing an optional Unity
  package dependency into Core;
- validate language changes and accessibility state without subtree loss.

Completion record: LumaFlow now owns one retained Unity accessibility hierarchy
per mount, exposes explicit semantics/exclusion and platform-aware announcements,
maps core controls and navigation selection into roles/state/actions, preserves
native focus and keyboard operation, provides typed zero-dependency locale
scopes, and scales widget-owned typography through `TextScaler`. The final S3
Windows Player run passed 273/273 Runtime tests. Native screen-reader speech is
still a documented manual Android/iOS device gate, not a desktop claim.

### S4 — Lists and virtualization (completed 2026-08-24)

- preserve keyed item identity and selection through recycling and collection
  mutations;
- define item-node ownership, scroll restoration and binding cleanup;
- benchmark large collections using native UI Toolkit virtualization.

Completion record: `ListView<T>` now retains compatible keyed state only across
native realized/recycle hosts, uses externally owned key-based single/multiple
selection, restores scroll offset through `ListViewController`, and releases
row bindings deterministically on recycle, removal, source replacement, and
unmount. Performance coverage records 100, 1,000, and 10,000-item mount cases
plus a 24-host recycling workload without introducing a node-per-item cache.

### S5 — Navigation and overlays (completed 2026-08-24)

- extend retained route lifecycle with route keys, transitions and restoration
  semantics;
- harden push/pop/replace/clear, back handling and focus restoration;
- validate dialog, drawer, popover, toast and modal cleanup, z-order and input
  blocking under reconciliation.

The implemented contract adds unique keyed `Route` entries, immutable in-memory
`NavigationSnapshot` restoration, observable current-route state, route-scoped
fade activation, and `ClearAndPush`. Overlay ownership now includes explicit
modal dismissal/focus options, left/right drawers, top-modal input isolation,
non-dismissible back consumption, ordered cleanup, and matching Runtime and
performance coverage. General animation composition remains isolated to S6.

### S6 — Animation foundation (completed 2026-08-24)

- provide a small declarative transition model integrated with widget updates;
- respect reduced-motion policy and deterministic cancellation;
- benchmark scheduling and allocation behavior before expanding the API.

Completion record: LumaFlow now exposes immutable curves, typed tweens and
`AnimationSpec`, plus `TweenAnimationBuilder<T>` as the general local implicit
transition boundary. Compatible updates retarget from the currently displayed
value. `AnimatedOpacity` and route fades share one scheduler-independent driver,
exact completion, deterministic cancellation, and the tree-scoped
`MediaQueryData.DisableAnimations` policy; `AnimationBehavior.Preserve` is the
explicit semantic-motion opt-out. Runtime coverage passed 300/300 in the S6
isolated Unity batch gate. Named performance cases cover 10,000 driver
start/sample operations and sixteen builders over sixty frames without yet
claiming portable budgets.

### S7 — Developer experience and adoption (completed 2026-08-24)

- add widget/state/theme/layout diagnostics and actionable error messages;
- maintain migration notes, API reference, examples and a component gallery;
- add analyzers or tooling for keys, ownership and unsupported lifecycle patterns;
- keep the package usable without knowledge of internal `VisualElement` plumbing.

Completion record: `MountHandle` now exposes an immutable, non-owning diagnostic
snapshot of widget/node identity, mounted state, native element, resolved layout,
inherited scopes, theme, media, locale, and text scale. A weak active-mount
registry powers the explicit-refresh Widget Inspector without retaining mounts or
polling the Editor. Runtime findings cover ambiguous unkeyed stateful siblings
and native ownership boundaries; duplicate-key errors identify the exact key and
indexes. Migration notes, categorized API reference, diagnostics guide, updated
Getting Started documentation, and an importable reactive counter sample complete
the adoption vertical. The isolated Unity gate passed 307/307 checks and all six
generated projects compiled with zero warnings and errors. Automatic Roslyn
analysis is not claimed; S7 deliberately uses capture-time tooling.

### PlayMode preview lifecycle (implemented before S8 validation)

- expose a state-preserving `MountHandle.Rebuild()` boundary that re-evaluates
  declarative builders and reconciles compatible nodes;
- expose transactional `MountHandle.Restart(widget)` for a fresh application
  mount without leaving duplicate or half-mounted roots;
- dogfood both actions as Editor-only LumaFlow controls immediately after the
  Unity 6000.4+ Play/Pause/Step toolbar group;
- document that standard Unity C# recompilation can refresh the managed domain
  and is not equivalent to Dart VM method patching.

The lifecycle API and dogfood integration are implemented. Full Unity EditMode,
PlayMode, Player, and script-reload validation remain part of S8.

### S8 — Beta and stable release gates

No stable API or `1.0` claim is allowed until all applicable gates are green:

- clean package import and assembly-boundary validation;
- full Unity EditMode and PlayMode suites, not merely C# compilation;
- Player validation for supported backends, including IL2CPP where applicable;
- lifecycle, focus, reconciliation and leak regression suites;
- performance baselines for mount, update, inherited propagation and lists;
- public API compatibility review and documented breaking-change policy;
- CI compatibility matrix, samples, documentation and dogfood acceptance.

Every stabilization stage must ship with tests and documentation, and must not
advance by hiding or weakening an earlier regression. The reconciliation and
lifecycle contract baseline is green; retained-route profiling remains an
independent beta gate. The next active engineering priority is S8 beta and
stable release validation.

---

# 2. Development Philosophy

LumaFlow should be built from the inside out.

The order is:

```text
Architecture
    ↓
Mounting
    ↓
Basic widgets
    ↓
Layout
    ↓
Styling
    ↓
Theme
    ↓
Reactive state
    ↓
Inputs
    ↓
Real-world dogfooding
    ↓
Advanced components
    ↓
Navigation
    ↓
Animations
    ↓
Developer tooling
```

Do not reverse this order without strong justification.

---

# 3. Milestone Philosophy

Each milestone must answer a concrete question.

Examples:

```text
Can a Widget reliably mount?
Can it unmount without leaks?
Can layout compose?
Can theme values propagate?
Can State<T> update only affected UI?
Can a real editor tool be built with it?
```

A milestone is complete only when the question has been answered through working code and tests.

---

# 4. Phase 0 — Repository and Package Foundation

## Goal

Create a clean Unity package foundation without implementing meaningful framework behavior yet.

## Deliverables

```text
package.json
README.md
LICENSE.md
CHANGELOG.md

AGENTS.md
ARCHITECTURE.md
API_DESIGN.md
ROADMAP.md
COMPATIBILITY.md

Runtime/
Editor/
Tests/
Samples~/
Documentation~/
```

Initial assembly definitions:

```text
LumaFlow.Runtime
LumaFlow.Editor
LumaFlow.Tests.Runtime
LumaFlow.Tests.Editor
```

## Requirements

- Runtime must not reference `UnityEditor`.
- Core package must not reference URP or HDRP assemblies.
- Package must import into a clean Unity project.
- Naming and namespace conventions must be established.
- Test assemblies must compile.

## Exit Criteria

Phase 0 is complete when:

- package imports without errors;
- asmdefs compile;
- no unnecessary dependencies exist;
- project structure matches documentation.

---

# 5. Phase 1 — Core Widget Runtime

## Goal

Prove the basic architecture:

```text
Widget
↓
WidgetNode
↓
VisualElement
```

## Deliverables

Initial internal types:

```text
Widget
WidgetNode
BuildContext
WidgetMount
BindingScope
```

Basic lifecycle:

```text
Mount
Unmount
Dispose
```

Minimal native adapter:

```text
Native
```

or equivalent.

## Required Tests

Test:

- widget can mount into a root `VisualElement`;
- correct VisualElement is created;
- child ownership works;
- unmount removes hierarchy;
- repeated mount misuse is detected;
- subscriptions registered through node scope are cleaned;
- native VisualElement integration works.

## Not Allowed Yet

Do not implement:

- full reconciliation;
- navigation;
- animation framework;
- complex state;
- theme inheritance;
- virtualization;
- source generation.

## Exit Criteria

Phase 1 is complete when a widget tree can be mounted and unmounted deterministically without lifecycle leaks.

---

# 6. Phase 2 — Basic Content Widgets

## Goal

Prove that native UI Toolkit controls can be wrapped with clean declarative APIs.

## Widgets

Implement:

```text
Text
Button
Icon
```

Possible minimal native controls:

```text
Label
UnityEngine.UIElements.Button
VisualElement
```

## Desired Usage

```csharp
Column(
    children:
    [
        Text("Hello"),

        Button(
            "Continue",
            onPressed: Continue
        )
    ]
)
```

Column may still be provisional if layout work is split into the next phase.

## Requirements

- callback ownership is deterministic;
- button events are cleaned correctly;
- text updates can be performed internally without exposing `Label`;
- native UI Toolkit debugging remains usable.

## Exit Criteria

A minimal working UI can contain text and clickable interaction entirely through LumaFlow.

---

# 7. Phase 3 — Layout Primitives

## Goal

Prove declarative composition over native UI Toolkit Flexbox.

## Widgets

Implement:

```text
Row
Column
Padding
Center
Align
Spacer
Expanded
Flexible
Container
SizedBox
```

Potential later addition:

```text
Stack
Positioned
```

## Requirements

Map behavior to UI Toolkit/Yoga.

Do not implement a custom layout engine.

## Required Validation

Test:

- nested rows and columns;
- flex growth;
- alignment;
- spacing;
- padding;
- stretch behavior;
- minimum/maximum sizing;
- dynamic hierarchy;
- deeply nested composition.

## Sample

Create a Settings panel using only:

```text
Text
Button
Row
Column
Padding
Container
Spacer
Expanded
```

## Exit Criteria

A normal application screen can be expressed cleanly without manual `VisualElement` layout code.

---

# 8. Phase 4 — Typed Styling

## Goal

Remove repetitive low-level style manipulation.

## Deliverables

Implement foundational value types:

```text
EdgeInsets
Alignment
BorderRadius
Radius
Border
BorderSide
BoxDecoration
TextStyle
```

Central style mapping:

```text
PaddingStyleMapper
BorderStyleMapper
TextStyleMapper
DecorationStyleMapper
```

Exact internal organization may differ.

## Requirements

- no giant style utility class;
- typed style APIs;
- no arbitrary string-based style configuration for normal use;
- native USS remains usable.

## Sample

Desired:

```csharp
Container(
    padding: EdgeInsets.All(16),
    decoration: BoxDecoration(
        color: Color.gray,
        borderRadius: BorderRadius.All(12)
    ),
    child: Text("Card")
)
```

## Exit Criteria

A polished component can be styled without manually touching `IStyle` or USS.

---

# 9. Phase 5 — Theme System

## Goal

Establish LumaFlow as a design-system-aware framework.

## Deliverables

Implement:

```text
ThemeData
ColorScheme
Typography
SpacingScheme
RadiusScheme
ComponentThemes
```

BuildContext theme propagation.

Possible scoped widget:

```text
Theme
ThemeOverride
```

## Required Resolution Order

```text
explicit widget override
        ↓
component theme
        ↓
global theme
        ↓
framework default
```

## Requirements

- no global mutable theme singleton;
- nested theme overrides work;
- components use semantic defaults;
- design tokens are centralized.

## Sample

Desired:

```csharp
Text(
    "Settings",
    style: context.Theme.Typography.TitleLarge
)
```

and:

```csharp
Button(
    "Save",
    variant: ButtonVariant.Primary,
    onPressed: Save
)
```

with no manual visual configuration.

## Exit Criteria

An application can change visual identity primarily by replacing `ThemeData`.

---

# 10. Phase 6 — State<T> Foundation

## Goal

Introduce reactive UI without full tree rebuilding.

## Deliverables

Implement:

```text
State<T>
Subscription
BindingScope integration
```

State semantics:

```text
read Value
write Value
notify subscribers
```

## Requirements

- subscriptions are deterministic;
- node-owned subscriptions are automatically cleaned;
- external State ownership remains external;
- equality behavior is documented;
- main-thread assumptions are explicit.

## Tests

Test:

- updates notify;
- same-value assignment behavior;
- multiple subscribers;
- subscriber removal;
- unmount cleanup;
- state surviving UI unmount;
- no invocation after unsubscribe.

## Exit Criteria

State is stable as an independent reactive primitive before being deeply integrated into controls.

---

# 11. Phase 7 — Reactive Bindings

## Goal

Connect State<T> to mounted UI.

## Initial Reactive Targets

```text
Text
enabled state
visibility
basic style/property values
```

Example desired APIs must be evaluated.

Possible:

```csharp
Text(
    state: count,
    text: value => $"Count: {value}"
)
```

or another strongly typed equivalent.

## Structural Reactivity

Introduce a minimal subtree rebuild primitive only if required.

Potential:

```text
ReactiveBuilder<T>
```

Example:

```csharp
ReactiveBuilder(
    state: loading,
    builder: isLoading =>
        isLoading
            ? ProgressIndicator()
            : Content()
)
```

## Explicit Non-Goal

Do not implement whole-tree reconciliation.

## Exit Criteria

Simple state changes update specific native elements, while structural changes can rebuild isolated subtrees.

---

# 12. Phase 8 — Input Components

## Goal

Build useful interactive application UI.

## Components

Implement:

```text
TextField
Toggle
Checkbox
Radio
Slider
Dropdown
```

## State Integration

Inputs should support explicit controlled state.

Example:

```csharp
TextField(
    label: "Username",
    value: username
)
```

```csharp
Slider(
    value: volume,
    min: 0,
    max: 1
)
```

## Requirements

Clearly distinguish:

```text
controlled state
initial/default value
```

Avoid ambiguous value ownership.

## Tests

Test:

- native event → State update;
- State update → native control;
- no feedback loops;
- disabled/read-only behavior;
- focus remains stable;
- unmount cleans subscriptions.

## Exit Criteria

A full settings form can be implemented with LumaFlow.

---

# 13. Phase 9 — First Real Dogfooding Milestone

## Goal

Stop designing LumaFlow only through synthetic examples.

Build a meaningful interface using LumaFlow.

Primary target:

```text
AudioLib Editor UI
```

or a representative subset of it.

## Required Screen Characteristics

The dogfood interface should include:

- nested layouts;
- multiple cards;
- input controls;
- list content;
- reactive state;
- theme;
- actions;
- conditional content;
- scrollable content.

## Questions to Answer

During dogfooding evaluate:

1. Is widget construction too verbose?
2. Are too many wrappers required?
3. Are theme APIs convenient?
4. Does State<T> feel natural?
5. Are event callbacks concise?
6. Does BuildContext help?
7. Is debugging difficult?
8. Are native escape hatches sufficient?
9. Are style APIs missing common capabilities?
10. Does the framework genuinely feel better than raw UI Toolkit?

## Rule

Pain discovered during dogfooding has higher priority than speculative advanced features.

## Exit Criteria

A non-trivial production-style UI exists and the core API has been refined from real use.

---

# 14. Phase 10 — Core Component Library

## Goal

Provide a coherent base component set.

## Components

Add:

```text
Card
Divider
Tooltip
ProgressIndicator
ScrollView
ListView
Select
SearchField
```

Potential additional:

```text
Badge
Chip
Avatar
```

only if justified by real use.

## Requirements

Components must:

- use ThemeData;
- support semantic variants;
- compose existing primitives;
- avoid unnecessary native wrappers;
- preserve keyboard/focus behavior.

## Exit Criteria

LumaFlow can cover most ordinary application UI without requiring users to build every control themselves.

---

# 15. Phase 11 — List Virtualization

## Goal

Support large dynamic collections efficiently.

## Architecture

Prefer:

```text
LumaFlow ListView<T>
        ↓
Unity UI Toolkit ListView
```

Use native item recycling/virtualization.

## Desired API

```csharp
ListView(
    items: sounds,
    itemBuilder: sound =>
        SoundCard(sound)
)
```

## Challenges to Resolve

- node lifetime during recycling;
- state bindings;
- item identity;
- native item roots;
- changing collections;
- selection;
- scroll restoration.

## Rule

Do not emulate virtualization with thousands of ordinary mounted children.

## Exit Criteria

Large lists remain performant and recyclable.

---

# 16. Phase 12 — Structural Rebuild Evaluation

## Goal

Determine whether the current reactive architecture is sufficient.

This is an evaluation phase, not an automatic implementation phase.

## Investigate

Real projects should reveal whether LumaFlow frequently needs:

```text
old Widget tree
↓
new Widget tree
↓
diff
```

If explicit reactive bindings and subtree builders solve most needs, do not add full reconciliation.

## Possible Outcome A

No reconciliation needed.

Continue with localized state bindings.

## Possible Outcome B

Introduce limited reconciliation.

Possible identity:

```text
type
position
key
```

## Possible Outcome C

Introduce full declarative rebuild model.

This requires a new ADR and substantial architectural review.

## Exit Criteria

A deliberate decision is recorded.

---

# 17. Phase 13 — Navigation

## Goal

Provide optional screen/navigation abstractions for application-style UI.

## Potential API

```csharp
context.Navigator.Push(
    new SettingsScreen()
);

context.Navigator.Pop();
```

Possible:

```text
Navigator
Route
NavigationStack
```

## Requirements

Navigation must remain optional.

Core widgets must not depend on Navigation.

## Features

Initial:

```text
Push
Pop
Replace
Clear
```

Later if useful:

```text
named routes
parameters
guards
transitions
```

## Exit Criteria

Multi-screen UI can be built without users manually swapping root hierarchies.

---

# 18. Phase 14 — Overlay System

## Goal

Provide reusable transient UI infrastructure.

Components may include:

```text
Dialog
Modal
Popover
ContextMenu
Tooltip
Toast
Notification
```

Underlying concept:

```text
Overlay
OverlayEntry
OverlayHost
```

## Requirements

Must handle:

- z-order;
- input blocking;
- focus;
- dismissal;
- escape key;
- click outside;
- cleanup.

## Exit Criteria

Transient UI does not require ad hoc root hierarchy manipulation.

---

# 19. Phase 15 — Animation Foundation

## Goal

Introduce deliberate motion without creating a custom rendering system.

## Potential Features

```text
AnimatedContainer
AnimatedOpacity
AnimatedSize
Transition
```

Use UI Toolkit transitions or supported scheduling mechanisms where practical.

## Theme Integration

Potential tokens:

```text
Motion.DurationFast
Motion.DurationNormal
Motion.DurationSlow
Motion.CurveStandard
```

## Rule

Do not copy Flutter's animation subsystem wholesale.

## Exit Criteria

Common state transitions can be expressed declaratively and remain performant.

---

# 20. Phase 16 — Editor-Specific Components

## Goal

Make LumaFlow genuinely useful for Unity tooling.

Possible components:

```text
ObjectField
PropertyField adapter
InspectorSection
Toolbar
SplitView
TreeView
EditorSearchField
AssetPicker
```

## Architecture

Editor components belong in:

```text
LumaFlow.Editor
```

Runtime must remain independent.

## Dogfood

Expand AudioLib Editor UI further.

## Exit Criteria

A sophisticated Unity Editor extension can be built mostly with LumaFlow.

---

# 21. Phase 17 — Advanced Styling

## Goal

Improve design capability after core behavior is stable.

Potential:

```text
gradients
advanced shadows
transitions
pseudo-state styling helpers
responsive constraints
theme variants
```

## Rule

Prefer UI Toolkit-native capabilities.

Do not introduce render pipeline dependencies into Core.

---

# 22. Phase 18 — Optional Effects Module

## Goal

Support visually advanced effects without compromising Core compatibility.

Potential effects:

```text
BackdropBlur
GlassSurface
Glow
AdvancedShadow
CustomMask
```

Architecture:

```text
LumaFlow.Effects
        ↓
pipeline-independent contract
```

Optional implementations:

```text
Built-in compatible
URP
HDRP
```

## Strict Rule

Installing LumaFlow Core must never require:

```text
URP
HDRP
```

## Exit Criteria

Advanced visuals remain optional and isolated.

---

# 23. Phase 19 — Responsive UI

## Goal

Provide better adaptation to panel size.

Potential concepts:

```text
MediaQuery
Breakpoints
ResponsiveBuilder
Visibility
Flexible constraints
```

Example:

```csharp
ResponsiveBuilder(
    builder: context =>
        context.Width < 800
            ? MobileLayout()
            : DesktopLayout()
)
```

## Rule

Do not imitate web breakpoints blindly.

Design around Unity runtime and Editor window realities.

---

# 24. Phase 20 — Localization Integration

## Goal

Allow UI text to react cleanly to localization.

LumaFlow should integrate with Unity localization where possible rather than replacing it.

Potential API:

```csharp
Text(
    context.Localization["settings.audio"]
)
```

or typed adapters.

## Requirements

Changing language should update relevant mounted UI.

Core must not hard-depend on an optional localization package unless isolated appropriately.

---

# 25. Phase 21 — Forms

## Goal

Add higher-level form composition after input APIs are stable.

Potential:

```text
Form
FormField<T>
Validator<T>
ValidationResult
```

Possible usage:

```csharp
Form(
    children:
    [
        TextField(...),
        TextField(...),
        Button("Submit", ...)
    ]
)
```

Do not bake form concerns into every primitive input.

---

# 26. Phase 22 — Developer Tools

## Goal

Improve debugging and framework adoption.

Potential tools:

```text
Widget Tree Inspector
State Inspector
Theme Inspector
Binding Inspector
Layout Debugger
Performance Diagnostics
```

A developer should be able to inspect:

```text
Widget
WidgetNode
VisualElement
bindings
context
```

relationships.

## Exit Criteria

Debugging LumaFlow internals is significantly easier than reading raw generated hierarchy manually.

---

# 27. Phase 23 — Component Gallery

## Goal

Create a polished showcase and visual test surface.

Provide an Editor/runtime sample showing every standard component and state.

Examples:

```text
Buttons
Inputs
Cards
Typography
Colors
Dialogs
Lists
Navigation
```

This becomes useful for:

- visual QA;
- documentation;
- screenshots;
- regression checking.

---

# 28. Phase 24 — Documentation and Adoption

## Goal

Make the project usable by developers who did not build it.

Documentation should include:

```text
Getting Started
Installation
Core Concepts
Layout
Styling
Theme
State
Inputs
Lists
Editor Usage
Native UI Toolkit Interop
Migration from raw UI Toolkit
```

Examples should be copy-paste friendly.

---

# 29. Phase 25 — Package Distribution

## Goal

Prepare reliable public distribution.

Primary:

```text
GitHub
Unity Package Manager via Git URL
```

Optional:

```text
Unity Asset Store
```

Potential future:

```text
OpenUPM
```

if desirable.

## Release Requirements

Before public release:

- license included;
- package metadata correct;
- documentation available;
- samples import correctly;
- compatibility matrix documented;
- no accidental editor/runtime references;
- no pipeline-specific core dependencies.

---

# 30. Version Strategy

Suggested development stages:

```text
0.0.x
Architecture prototypes

0.1.x
Core declarative UI usable

0.2.x
Theme + state + inputs

0.3.x
Dogfood-ready

0.4.x
Component library

0.5.x
Lists / advanced runtime UI

0.6.x
Editor tooling

0.7.x
Navigation / overlays

0.8.x
Animation / responsive UI

0.9.x
API stabilization

1.0.0
Stable public framework
```

Exact version numbers may change.

The important idea is progressive stabilization.

---

# 31. Pre-1.0 Rule

Before version 1.0, LumaFlow should be willing to break poor APIs.

Do not preserve bad architecture because of premature compatibility fear.

However, breaking changes must be:

- deliberate;
- documented;
- motivated by real improvement.

---

# 32. 1.0 Definition

LumaFlow should not reach 1.0 merely because it has many widgets.

Version 1.0 means:

- core architecture is proven;
- public API is coherent;
- lifecycle is reliable;
- state system is stable;
- theming is stable;
- runtime support is reliable;
- editor support is defined;
- native UI Toolkit interop works;
- package has been dogfooded in real tools;
- compatibility policy exists;
- tests cover critical infrastructure;
- documentation is sufficient for external adoption.

---

# 33. Features Explicitly Deferred Until After Core Validation

Do not prioritize the following during early development:

```text
complex navigation
routing with parameters
large animation framework
custom renderer
shader framework
visual node editor
code generation
source generators
runtime/VM code-patching hot reload system
full virtual DOM
custom layout engine
dependency injection container
forms framework
data grid
advanced table
localization framework
game-specific HUD framework
```

These may become useful later.

They are not prerequisites for proving LumaFlow.

---

# 34. Anti-Roadmap Rule

Do not implement features merely because they appear in this roadmap.

Every phase remains subject to:

```text
real user need
dogfooding evidence
architecture compatibility
maintenance cost
```

The roadmap defines order, not obligation.

---

# 35. Technical Debt Rule

During early development, small intentional technical debt may be accepted to validate APIs.

However, infrastructure debt around:

```text
lifecycle
subscription cleanup
ownership
assembly boundaries
state semantics
```

must not be deferred casually.

These areas form the framework foundation.

---

# 36. Testing Growth

Testing depth should increase with each phase.

Suggested progression:

```text
Phase 1:
mount lifecycle tests

Phase 3:
layout mapping tests

Phase 5:
theme/context propagation tests

Phase 6:
State<T> tests

Phase 7:
binding cleanup tests

Phase 8:
input synchronization tests

Phase 11:
virtualization/recycling tests

Later:
integration and regression tests
```

---

# 37. Sample Growth

Samples should evolve incrementally.

```text
Samples~/
├── 01_Basics
├── 02_Layout
├── 03_Styling
├── 04_Theming
├── 05_State
├── 06_Inputs
├── 07_Lists
├── 08_Navigation
└── 09_Dashboard
```

Do not create empty sample folders ahead of implementation.

---

# 38. Performance Validation

Performance work should also follow phases.

Early:

```text
avoid obvious leaks and unnecessary rebuilds
```

Later benchmark:

```text
mounting large trees
state update propagation
list virtualization
layout wrappers
binding allocations
theme resolution
```

Do not optimize theoretical hot paths before they exist.

---

# 39. Compatibility Validation

At meaningful milestones, validate:

```text
Runtime
Editor

Built-in Render Pipeline
URP
HDRP
```

Later extend platform validation as defined in `COMPATIBILITY.md`.

Pipeline compatibility failures in Core are release blockers.

---

# 40. Codex Phase Rule

Before implementing a requested feature, Codex must determine:

1. Which roadmap phase does it belong to?
2. Are required earlier foundations implemented?
3. Does the feature require an architectural decision not yet made?
4. Can it be implemented without prematurely introducing future infrastructure?

If prerequisites are missing, implement the smallest required foundation first.

Do not jump multiple phases unnecessarily.

---

# 41. Codex Scope Rule

When working on one phase:

Do not opportunistically implement unrelated future systems.

Example:

Task:

```text
Implement Row and Column.
```

Do not also add:

```text
navigation
animation
dependency injection
routing
```

Keep changes coherent.

---

# 42. Codex Refactoring Rule

Refactoring is allowed when required to preserve architectural consistency.

However:

```text
feature task
```

should not automatically become:

```text
rewrite the whole framework
```

Prefer incremental structural improvement.

---

# 43. Codex Dogfooding Rule

Once Phase 9 begins, Codex should treat real LumaFlow usage as a source of truth.

If a public API repeatedly produces awkward AudioLib code, reconsider the API.

Do not defend an abstraction merely because it looked elegant in isolation.

---

# 44. Decision Gates

Major architecture features require explicit decision gates.

## Gate A — State Model

Before advanced reactivity:

```text
State<T> semantics proven
subscription ownership proven
```

## Gate B — Reconciliation

Before virtual DOM/diff:

```text
real structural rebuild problem demonstrated
```

## Gate C — Navigation

Before navigation:

```text
core component API stable enough
```

## Gate D — Animation

Before animation:

```text
style/update model stable
```

## Gate E — Effects

Before render-pipeline-specific effects:

```text
Core remains pipeline independent
optional module design established
```

---

# 45. First Major Target

The first major target is not:

```text
100 widgets
```

It is:

```text
one excellent real application UI
```

built using LumaFlow.

Target:

```text
AudioLib Editor
```

or equivalent complex Unity tool.

If LumaFlow makes that UI significantly easier to build and maintain than raw UI Toolkit, the project is proving its value.

---

# 46. MVP Definition

The practical MVP includes:

```text
Core mount lifecycle

Text
Button

Row
Column
Padding
Center
Expanded
Container

EdgeInsets
BorderRadius
BoxDecoration
TextStyle

ThemeData
ColorScheme
Spacing
Typography

State<T>
Reactive binding

TextField
Toggle
Slider

ScrollView
```

The MVP should be enough to build a polished settings/editor interface.

---

# 47. MVP Exclusions

MVP does not require:

```text
full navigation
routing
animations
DataGrid
TreeView
advanced virtualization
localization
forms
URP/HDRP effects
developer inspector tools
full reconciliation
```

---

# 48. MVP Success Metrics

The MVP succeeds if:

1. A developer can build a useful UI almost entirely from C#.
2. UI hierarchy is easy to read.
3. UI Toolkit remains accessible underneath.
4. State changes do not rebuild unrelated UI.
5. unmounting does not leak subscriptions.
6. theming removes repeated literals.
7. the same Core works under Built-in, URP, and HDRP.
8. AudioLib or another real tool can use it productively.

---

# 49. Roadmap Summary

```text
Phase 0
Package foundation

Phase 1
Widget runtime

Phase 2
Basic content

Phase 3
Layout

Phase 4
Styling

Phase 5
Theme

Phase 6
State<T>

Phase 7
Reactive bindings

Phase 8
Inputs

Phase 9
Real dogfooding

Phase 10
Core components

Phase 11
Virtualized lists

Phase 12
Reconciliation decision

Phase 13
Navigation

Phase 14
Overlays

Phase 15
Animations

Phase 16
Editor components

Phase 17
Advanced styling

Phase 18
Optional effects

Phase 19
Responsive UI

Phase 20
Localization

Phase 21
Forms

Phase 22
Developer tools

Phase 23
Component gallery

Phase 24
Documentation

Phase 25
Public distribution
```

---

# 49.1 Current Dogfood Priority Batch

This batch is executed before the remaining S8 release gates because it removes
known friction from the real application rather than freezing an inconvenient
surface.

| Priority | Capability | Status |
| --- | --- | --- |
| P0 | Arbitrary-child `Button` and behavior-only `Pressable` | Implemented; runtime suite passed 311/311 before the P1 additions |
| P0 | `Text` wrapping, `maxLines`, and overflow | Implemented with documented UI Toolkit multiline limits |
| P0 | Shared `Container`/`Card` background, radius, and border | Implemented |
| P0 | State-aware `ButtonStyle` border | Implemented |
| P1 | `LinearProgressIndicator` | Implemented; Unity runtime regression run pending |
| P1 | Flexible `ListTile` slots | Implemented; Unity runtime regression run pending |
| P1 | Theme defaults for Checkbox/Switch/Radio/Slider | Implemented; Unity runtime regression run pending |
| P1 | Sizing/alignment conveniences | Implemented; Unity runtime regression run pending |
| P2 | Image/avatar/application media | Implemented; Unity runtime regression run pending |
| P2 | Hover/focus/cursor semantics for interactive surfaces | Implemented with custom-cursor support; Unity runtime regression run pending |
| P2 | Layout/reconciliation DevTools | Layout properties implemented in S7 Inspector; rebuild/reconcile event tracing remains future work |

Shadow/elevation is deliberately not marked implemented: Unity 6.0 UI Toolkit
does not expose a public box-shadow style. It requires a real renderer-backed
primitive and visual/performance tests, not a no-op API.

---

# 50. Final Roadmap Principle

LumaFlow should become powerful by accumulating proven abstractions, not by designing every possible framework subsystem in advance.

The development loop is:

```text
Build minimum capability
        ↓
Use it in real UI
        ↓
Observe pain
        ↓
Improve API
        ↓
Extract only proven abstractions
        ↓
Test
        ↓
Continue
```

The goal is not to finish the roadmap as quickly as possible.

The goal is to reach each stage with a framework whose existing foundation remains simple, reliable, and pleasant to use.
