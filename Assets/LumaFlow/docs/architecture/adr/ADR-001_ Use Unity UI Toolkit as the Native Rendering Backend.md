# ADR-001: Use Unity UI Toolkit as the Native Rendering Backend

- **Status:** Accepted
- **Decision date:** 2026-08-11
- **Scope:** Core architecture
- **Affects:** Runtime, Editor, Widgets, Layout, Styling, Rendering
- **Related documents:** `AGENTS.md`, `ARCHITECTURE.md`, `COMPATIBILITY.md`

---

## 1. Context

LumaFlow is intended to provide a modern declarative UI development experience for Unity.

The framework should make UI authoring feel closer to systems such as Flutter, SwiftUI, and other component-oriented declarative frameworks.

A major architectural choice is whether LumaFlow should:

1. build its own rendering and layout system;
2. partially replace Unity UI Toolkit;
3. or use Unity UI Toolkit as the native backend and provide a higher-level abstraction over it.

Unity UI Toolkit already provides:

- `VisualElement`;
- native retained-mode hierarchy;
- Yoga/Flexbox-based layout;
- event propagation;
- pointer input;
- keyboard input;
- focus management;
- text rendering;
- standard controls;
- panel infrastructure;
- runtime UI;
- Editor UI;
- styling;
- virtualization;
- Unity integration.

Reimplementing these capabilities would dramatically increase the scope, maintenance cost, compatibility risk, and complexity of LumaFlow.

---

## 2. Decision

LumaFlow will use **Unity UI Toolkit as its native rendering, layout, event, and panel backend**.

The architectural relationship is:

```text id="zckf5x"
Application Code
        ↓
LumaFlow
        ↓
Unity UI Toolkit
        ↓
Unity
```

LumaFlow will provide higher-level abstractions around UI Toolkit.

It will not replace the underlying UI system.

---

## 3. Core Mapping

Visible LumaFlow widgets should eventually map to native UI Toolkit elements.

Examples:

```text id="0tm23y"
LumaFlow Text
      ↓
Label
```

```text id="gsa4pv"
LumaFlow Button
      ↓
UnityEngine.UIElements.Button
```

```text id="jtvntc"
LumaFlow Row
      ↓
VisualElement
      ↓
flex-direction: row
```

```text id="jsc8i6"
LumaFlow Column
      ↓
VisualElement
      ↓
flex-direction: column
```

UI Toolkit remains responsible for actual rendering and layout calculation.

---

## 4. What LumaFlow Owns

LumaFlow may own abstractions for:

```text id="myd6fa"
declarative widget descriptions
component composition
mounting
unmounting
lifecycle
reactive state
reactive bindings
BuildContext
theme propagation
design tokens
typed styling
navigation
overlays
developer ergonomics
```

These features exist to make UI Toolkit easier and more structured to use.

---

## 5. What UI Toolkit Owns

LumaFlow must prefer UI Toolkit for:

```text id="hxqsci"
actual rendering
layout calculation
VisualElement hierarchy
native controls
pointer events
keyboard events
focus
panel management
text rendering
native scrolling
native virtualization
native UI Toolkit debugging
```

LumaFlow may adapt these systems but should not duplicate them without demonstrated necessity.

---

## 6. No Custom Rendering Engine

LumaFlow Core must not implement its own general-purpose renderer.

Forbidden architectural direction:

```text id="eky3ib"
LumaFlow Widgets
        ↓
Custom Render Tree
        ↓
Custom Renderer
        ↓
Draw UI manually
```

LumaFlow is not intended to become:

- a Canvas renderer;
- a mesh-based UI system;
- an immediate-mode UI renderer;
- a replacement for UI Toolkit;
- a Unity equivalent of Flutter's complete rendering stack.

---

## 7. No Custom General-Purpose Layout Engine

LumaFlow must not implement a second general layout engine for ordinary widgets.

Forbidden:

```text id="1e408d"
Row
Column
Expanded
Stack
        ↓
LumaFlow custom layout solver
```

Preferred:

```text id="zf4x06"
Row
Column
Expanded
Stack
        ↓
mapping
        ↓
UI Toolkit layout/style behavior
```

LumaFlow APIs may expose familiar concepts, but layout calculations should be delegated to UI Toolkit wherever possible.

---

## 8. Flutter Is API Inspiration, Not Runtime Architecture

LumaFlow may borrow useful concepts from Flutter such as:

```text id="y2r1y4"
Widget
BuildContext
Row
Column
Expanded
Padding
Theme
Navigator
State
```

However, LumaFlow must not reproduce Flutter internals simply for conceptual similarity.

Specifically, LumaFlow does not automatically require equivalents of:

```text id="i8dwd4"
RenderObject
RenderBox
Flutter pipeline owner
Flutter element architecture
Flutter painting pipeline
Flutter compositing pipeline
```

Any similar abstraction must be justified by Unity-specific requirements.

---

## 9. Native Interoperability

A LumaFlow application must retain access to native UI Toolkit functionality.

Users must be able to integrate:

- custom `VisualElement` subclasses;
- existing UI Toolkit controls;
- third-party UI Toolkit controls;
- UXML;
- USS;
- Unity Editor UI;
- runtime UI Toolkit features.

Potential API:

```csharp id="3iqsuy"
Native(
    new CustomVisualElement()
)
```

The exact API may evolve.

The interoperability requirement does not.

---

## 10. Generated Hierarchy

LumaFlow-generated interfaces should remain valid native UI Toolkit hierarchies.

This means developers should still be able to use:

- UI Toolkit Debugger;
- native hierarchy inspection;
- USS debugging;
- native focus debugging;
- standard Unity profiling tools.

LumaFlow must avoid hiding the generated UI behind an opaque rendering layer.

---

## 11. Styling

Typed LumaFlow styling should eventually map to UI Toolkit styles.

Example:

```text id="n5xlwh"
EdgeInsets
BorderRadius
BoxDecoration
TextStyle
        ↓
LumaFlow style mapping
        ↓
VisualElement.style
```

USS remains supported as an interoperability mechanism.

LumaFlow reduces the need for USS.

It does not invalidate USS.

---

## 12. Native Controls

When UI Toolkit already provides a mature control, LumaFlow should prefer adapting that control.

Examples:

```text id="mh5l3y"
Button
TextField
Toggle
Slider
ScrollView
ListView
```

Do not rebuild such controls from primitive elements unless there is a concrete behavioral or API reason.

---

## 13. Virtualization

Large collection rendering should use native UI Toolkit virtualization where practical.

Preferred:

```text id="aa1s2j"
LumaFlow ListView<T>
        ↓
UI Toolkit ListView
```

Forbidden default:

```text id="8eljme"
10,000 items
        ↓
10,000 ordinary LumaFlow child widgets
        ↓
10,000 permanent VisualElements
```

unless the user explicitly chooses non-virtualized behavior.

---

## 14. Events

LumaFlow high-level semantic events should wrap UI Toolkit events.

Example:

```csharp id="u14iox"
Button(
    "Save",
    onPressed: Save
)
```

internally maps to appropriate native UI Toolkit interaction.

LumaFlow must not create an unrelated global event system for ordinary controls.

---

## 15. Focus

LumaFlow must preserve UI Toolkit focus behavior.

Custom components must not casually bypass:

- focusability;
- keyboard navigation;
- tab order;
- focus events.

If LumaFlow adds focus abstractions later, they should build on UI Toolkit focus semantics.

---

## 16. Runtime and Editor

Because UI Toolkit supports both Runtime and Editor environments, this ADR applies to both.

Architecture:

```text id="iu1p40"
LumaFlow Runtime
       ↓
UI Toolkit Runtime
```

and:

```text id="t4ow79"
LumaFlow Editor
       ↓
UI Toolkit Editor
```

Editor functionality may use `UnityEditor` APIs in editor-only assemblies.

Runtime must remain independent from `UnityEditor`.

---

## 17. Render Pipeline Independence

Using UI Toolkit as the core backend also supports LumaFlow's render-pipeline compatibility goals.

Ordinary LumaFlow UI must remain compatible with:

```text id="yfx90r"
Built-in Render Pipeline
URP
HDRP
```

Core must not introduce pipeline dependencies merely to render normal UI.

---

## 18. Optional Advanced Effects

This ADR does not prohibit advanced effects.

Future modules may provide:

```text id="s5xvt7"
blur
glassmorphism
advanced glow
custom masks
shader-driven surfaces
```

Some may require render-pipeline-specific implementations.

They must remain optional.

Architecture:

```text id="dkoj8d"
LumaFlow Core
      │
      └─────────────── independent

LumaFlow Effects
      ↓
optional implementation

URP integration
HDRP integration
Built-in compatible fallback
```

Advanced effects must not turn Core into a custom renderer.

---

## 19. Consequences — Positive

This decision provides several benefits.

### Native Unity compatibility

LumaFlow works with Unity's supported UI infrastructure instead of bypassing it.

### Lower implementation complexity

The project does not need to solve:

- text rendering;
- layout;
- focus;
- pointer dispatch;
- panel rendering;
- clipping;
- scrolling;
- basic virtualization.

### Better Editor integration

Existing UI Toolkit Editor behavior remains usable.

### Better interoperability

Existing Unity and third-party UI Toolkit controls can be embedded.

### Better debugging

Developers retain access to UI Toolkit debugging tools.

### Better future compatibility

Unity improvements to UI Toolkit can often benefit LumaFlow automatically.

---

## 20. Consequences — Negative

This decision also imposes constraints.

### LumaFlow inherits UI Toolkit limitations

If UI Toolkit cannot perform something efficiently or cleanly, LumaFlow may also be constrained.

### Flutter behavior cannot always be reproduced exactly

Some familiar Flutter concepts must be adapted to Yoga/UI Toolkit semantics.

### Advanced visuals may require special integrations

Certain effects may need optional pipeline-specific implementations.

### Framework design must respect VisualElement lifecycle

LumaFlow cannot treat UI Toolkit as an implementation detail that can be arbitrarily ignored.

These tradeoffs are accepted.

---

## 21. Rejected Alternative: Full Custom UI Framework

Rejected architecture:

```text id="79nz3z"
LumaFlow
   ↓
custom widget tree
   ↓
custom layout
   ↓
custom render objects
   ↓
custom mesh renderer
```

### Reason for rejection

This would substantially increase:

- codebase size;
- rendering complexity;
- maintenance;
- platform risk;
- Unity version compatibility risk;
- Editor integration difficulty.

It would also move LumaFlow away from its original purpose.

LumaFlow exists to make UI Toolkit pleasant, not to replace Unity's UI stack.

---

## 22. Rejected Alternative: Compile LumaFlow into UXML/USS

Potential architecture:

```text id="t90aqn"
C# declarative tree
        ↓
generated UXML
generated USS
        ↓
UI Toolkit
```

This is rejected as the primary runtime architecture.

Reasons:

- introduces generated artifacts;
- complicates dynamic state;
- complicates runtime composition;
- creates synchronization complexity;
- slows iteration;
- creates unnecessary intermediate representation.

Code generation may still be useful for tooling in the future.

It is not the core rendering model.

---

## 23. Rejected Alternative: Pure UXML Framework

LumaFlow will not require applications to define every UI hierarchy in UXML.

UXML remains supported as an integration mechanism.

The primary LumaFlow experience is declarative C#.

---

## 24. Rejected Alternative: Immediate Mode DSL

An API like:

```csharp id="14zo6p"
UI.BeginColumn();

UI.Text("Settings");
UI.Button("Save");

UI.EndColumn();
```

is rejected as the primary framework model.

Reasons:

- hierarchy is less composable;
- reusable components become less natural;
- retained lifecycle becomes harder;
- state ownership becomes less clear;
- structure is less strongly represented in code.

LumaFlow remains retained/declarative.

---

## 25. Architectural Invariants Created by This ADR

Unless this ADR is explicitly superseded:

### Invariant 1

Every normal visible LumaFlow UI eventually resolves into native UI Toolkit objects.

### Invariant 2

UI Toolkit remains responsible for normal layout calculation.

### Invariant 3

LumaFlow does not implement a general custom renderer.

### Invariant 4

LumaFlow does not implement a general replacement layout engine.

### Invariant 5

Existing `VisualElement` controls remain embeddable.

### Invariant 6

UI Toolkit debugging remains meaningful.

### Invariant 7

Core controls should prefer native UI Toolkit implementations where appropriate.

### Invariant 8

Flutter influences API design, not mandatory internal architecture.

---

## 26. Codex Rules

When implementing LumaFlow features, Codex must follow these rules.

### Rule 1

Before implementing a custom rendering solution, check whether UI Toolkit already provides the required functionality.

### Rule 2

Before implementing a custom layout algorithm, determine whether UI Toolkit Flexbox/Yoga can represent the behavior.

### Rule 3

Do not create a second event system for ordinary controls.

### Rule 4

Do not recreate native controls without clear justification.

### Rule 5

Do not bypass `VisualElement` solely to make the architecture look more similar to Flutter.

### Rule 6

When wrapping native UI Toolkit controls, preserve native focus and event behavior.

### Rule 7

Keep native interoperability available.

### Rule 8

If a requirement appears impossible under UI Toolkit, document the limitation before introducing a replacement subsystem.

---

## 27. Decision Test

When deciding whether functionality belongs in LumaFlow or UI Toolkit, ask:

```text id="8zf7a1"
Does Unity UI Toolkit already own this responsibility?
        ↓ yes
Use it.

Does LumaFlow need a nicer abstraction around it?
        ↓ yes
Wrap it.

Does wrapping fail to satisfy the requirement?
        ↓ yes
Investigate an isolated extension.

Would the solution create a second general renderer/layout/event system?
        ↓ yes
Stop and require a new architectural decision.
```

---

## 28. Example

Application code:

```csharp id="kuq3c9"
return Column(
    gap: 16,
    children:
    [
        Text("Audio Library"),

        Button(
            "Play",
            onPressed: Play
        )
    ]
);
```

Internal conceptual result:

```text id="y71cnd"
Column Widget
    ↓
ColumnNode
    ↓
VisualElement
    flex-direction = column

Text Widget
    ↓
TextNode
    ↓
Label

Button Widget
    ↓
ButtonNode
    ↓
UnityEngine.UIElements.Button
```

Final native hierarchy:

```text id="4up6hy"
VisualElement
├── Label
└── Button
```

Unity UI Toolkit performs rendering and layout.

That is the intended architecture.

---

## 29. Reconsideration Conditions

This decision should only be reconsidered if one or more of the following become true:

1. UI Toolkit fundamentally cannot support LumaFlow's core product requirements.
2. Critical platform support becomes impossible through UI Toolkit.
3. Unity deprecates UI Toolkit.
4. A large portion of LumaFlow requires bypassing UI Toolkit anyway.
5. Performance data proves UI Toolkit is fundamentally unsuitable for the intended framework.

A dislike of one UI Toolkit API or limitation is not sufficient reason to replace the backend.

---

## 30. Final Decision

LumaFlow is a declarative framework **for Unity UI Toolkit**.

It is not a separate Unity UI renderer.

Its value comes from transforming:

```text id="33ciik"
verbose imperative UI Toolkit authoring
```

into:

```text id="n3o8tu"
declarative
reactive
composable
typed
theme-driven
C# UI
```

while preserving the native Unity UI stack underneath.

This architectural boundary is foundational and must remain stable.