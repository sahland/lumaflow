# ADR-017: Enforce a Strict Runtime and Editor Assembly Boundary

- **Status:** Accepted
- **Decision date:** 2026-08-11
- **Scope:** Assembly separation, Editor-only APIs, shared components, package structure
- **Affects:** Runtime, Editor, asmdefs, CI, Components, Testing, Distribution
- **Related documents:** `ARCHITECTURE.md`, `COMPATIBILITY.md`, `ADR-006-render-pipeline-independence.md`, `ADR-012-native-interoperability.md`

---

## 1. Context

LumaFlow is intended to support both:

```text
Runtime UI
Editor UI
```

Unity UI Toolkit itself is used in both environments, but Unity exposes some APIs only through:

```text
UnityEditor
UnityEditor.UIElements
```

These APIs must never leak into runtime assemblies.

A weak boundary can produce:

- player build failures;
- runtime assembly references to Editor-only APIs;
- accidental dependency cycles;
- unnecessary platform restrictions;
- confusing component availability;
- difficult package maintenance.

LumaFlow therefore requires a strict Runtime/Editor architecture.

---

## 2. Decision

LumaFlow will separate Runtime and Editor functionality into distinct assemblies.

Conceptually:

```text
LumaFlow.Runtime
        ↑
LumaFlow.Editor
```

Editor may depend on Runtime.

Runtime must never depend on Editor.

Dependency direction is one-way.

---

## 3. Core Invariant

This is valid:

```text
LumaFlow.Editor
    ↓
LumaFlow.Runtime
```

This is forbidden:

```text
LumaFlow.Runtime
    ↓
LumaFlow.Editor
```

The rule applies to:

```text
assemblies
source files
public APIs
helper types
tests
optional modules
```

---

## 4. Runtime Assembly

`LumaFlow.Runtime` contains functionality valid in built players.

Examples:

```text
Widget
WidgetNode runtime
BuildContext
State<T>
Bindings
Theme
Styling
Row
Column
Button
Text
TextField
ScrollView
ListView
Navigator
Overlay
```

where those features rely only on runtime-compatible Unity APIs.

---

## 5. Editor Assembly

`LumaFlow.Editor` contains Unity Editor-specific functionality.

Examples:

```text
EditorWindow integrations
UnityEditor.UIElements controls
Editor-only property fields
SerializedObject integration
AssetDatabase adapters
Selection integration
Undo adapters
Editor theme integration
custom inspectors
Editor tooling
```

This assembly may reference:

```text
UnityEditor
UnityEditor.UIElements
```

---

## 6. Shared UI Components

A component that works equally in Runtime and Editor belongs in Runtime.

Example:

```text
Button
Card
Column
Text
Toggle
Slider
```

Do not duplicate shared components inside Editor merely because they are also used there.

---

## 7. Editor-Specific Components

A component that fundamentally requires UnityEditor belongs in Editor.

Examples may include:

```text
PropertyField adapters
SerializedProperty controls
Asset picker integrations
Editor-only object fields
AssetDatabase-backed browsers
```

These must not appear in Runtime assembly APIs.

---

## 8. Source Folder Structure

A possible package structure:

```text
Runtime/
├── Core/
├── Widgets/
├── State/
├── Theme/
├── Styling/
├── Navigation/
└── Overlay/

Editor/
├── Widgets/
├── Integration/
├── Windows/
├── Diagnostics/
└── Tooling/
```

Exact folders may evolve.

The assembly boundary is mandatory.

---

## 9. Assembly Definitions

Initial assemblies may include:

```text
LumaFlow.Runtime
LumaFlow.Runtime.Tests

LumaFlow.Editor
LumaFlow.Editor.Tests
```

Optional modules may add more assemblies later.

---

## 10. Runtime asmdef

`LumaFlow.Runtime.asmdef` must not reference:

```text
UnityEditor
LumaFlow.Editor
Editor-only third-party assemblies
```

It must compile for player platforms.

---

## 11. Editor asmdef

`LumaFlow.Editor.asmdef` should be restricted to:

```text
Editor
```

platform inclusion.

It may reference:

```text
LumaFlow.Runtime
UnityEditor
```

and other explicitly Editor-only integrations.

---

## 12. No UnityEditor Namespace in Runtime

Runtime source must not contain:

```csharp
using UnityEditor;
```

or:

```csharp
using UnityEditor.UIElements;
```

This should be treated as an architectural violation.

---

## 13. Avoid `#if UNITY_EDITOR` as a Substitute for Separation

This is not preferred inside Runtime:

```csharp
#if UNITY_EDITOR
using UnityEditor;
#endif
```

followed by mixed Runtime/Editor behavior in the same class.

Conditional compilation is not a replacement for proper assembly boundaries.

---

## 14. When UNITY_EDITOR Is Acceptable

`#if UNITY_EDITOR` may be acceptable for small diagnostic or compatibility details that do not introduce structural coupling.

However:

```text
significant Editor behavior
```

belongs in Editor assemblies.

---

## 15. No Editor Types in Runtime Public API

Forbidden:

```csharp
public void Bind(SerializedProperty property)
```

inside Runtime.

Forbidden:

```csharp
public EditorWindow Window { get; }
```

inside Runtime.

Any public type exposed by Runtime must remain usable without UnityEditor assemblies.

---

## 16. Runtime Types in Editor API

This is valid:

```csharp
public sealed class LumaEditorWindow : EditorWindow
{
    public Widget BuildContent()
    {
        ...
    }
}
```

Editor code may freely consume Runtime Widgets.

---

## 17. EditorWindow Integration

A convenience base class may eventually exist:

```csharp
public abstract class LumaEditorWindow : EditorWindow
{
    protected abstract Widget Build(BuildContext context);
}
```

Conceptually it may manage:

```text
CreateGUI
MountHandle
OnDisable / teardown
Editor context
```

This belongs entirely in `LumaFlow.Editor`.

---

## 18. Runtime Host Integration

Runtime mounting may use:

```text
UIDocument
VisualElement roots
MonoBehaviour host helpers
```

without any Editor dependency.

A Runtime host helper should live in Runtime.

---

## 19. No MonoBehaviour Requirement for Editor

EditorWindow-based LumaFlow UI must work without:

```text
GameObject
MonoBehaviour
Scene
Play Mode
```

This is a core requirement.

---

## 20. No EditorWindow Requirement for Runtime

Likewise Runtime APIs must remain independent from EditorWindow concepts.

---

## 21. Shared BuildContext

Runtime and Editor should use the same fundamental:

```text
BuildContext
Theme
State<T>
Widget
WidgetNode
```

architecture.

Do not create:

```text
RuntimeBuildContext
EditorBuildContext
```

unless genuinely different semantics emerge.

---

## 22. Editor Context Extensions

Editor-specific scoped information may be layered through Editor-only context extensions.

Examples:

```text
Editor selection context
Undo scope
SerializedObject context
```

These must not force Runtime BuildContext to reference UnityEditor types.

---

## 23. Context Extension Boundary

If generic context providers exist later, Editor may provide:

```text
EditorSelectionContext
```

through that mechanism.

This allows:

```text
shared BuildContext infrastructure
+
Editor-only value type
```

without modifying Runtime API.

---

## 24. Theme Sharing

Runtime and Editor should share the same ThemeData architecture.

Editor may provide an Editor-specific default theme or theme adapter.

Example:

```text
LumaFlow.EditorTheme
```

may derive values from Unity Editor appearance.

The Theme system itself remains Runtime-compatible.

---

## 25. Editor Theme Adaptation

Editor integration may inspect:

```text
EditorGUIUtility
Editor skin
Editor preferences
```

to create an appropriate ThemeData.

That logic belongs in Editor.

---

## 26. Runtime Default Theme

Runtime must not depend on Editor appearance.

It uses framework or application-provided ThemeData.

---

## 27. Shared Components in Editor

Editor users should be able to write:

```csharp
Column(
    children:
    [
        Text("AudioLib"),
        Button("Refresh", onPressed: Refresh)
    ]
)
```

using the same Runtime widgets.

This is intentional.

---

## 28. Editor-Specific Native Controls

Some Unity Editor controls have no runtime equivalent.

LumaFlow.Editor may wrap them with semantic Widgets.

Conceptually:

```text
SerializedPropertyField
AssetField
EditorObjectField
```

if real use cases justify them.

---

## 29. Editor Native Interop

ADR-012 still applies.

Editor code may embed:

```text
UnityEditor.UIElements
```

native controls through Editor-side Native integration.

---

## 30. Runtime Native Interop

Runtime Native integration remains limited to runtime-compatible `VisualElement` types.

The core `Native(...)` abstraction should not directly know about Editor-only classes.

---

## 31. Generic VisualElement Compatibility

Because Editor UIElements still derive from compatible UI Toolkit types where appropriate, Editor code may pass them into shared native adapters without Runtime needing compile-time UnityEditor references.

This is acceptable when type relationships allow it.

---

## 32. SerializedObject Integration

Bindings to:

```text
SerializedObject
SerializedProperty
```

belong in Editor.

They may adapt Editor data into:

```text
LumaFlow State/bindings
native PropertyField behavior
```

but must not modify Runtime State<T> semantics.

---

## 33. Undo Integration

Unity Undo is Editor-only.

A future Editor adapter may expose:

```text
Undo-aware State mutation
SerializedProperty binding
```

without making State<T> itself Editor-aware.

---

## 34. AssetDatabase Integration

Any use of:

```text
AssetDatabase
GUIDToAssetPath
FindAssets
LoadAssetAtPath
```

belongs in Editor.

Core components may receive the resulting assets through ordinary parameters.

---

## 35. Selection Integration

Unity Editor Selection is Editor-only.

A tool may adapt:

```text
Selection.activeObject
```

into application state.

Do not put Selection logic into generic LumaFlow components.

---

## 36. EditorApplication Events

Subscriptions to:

```text
EditorApplication.update
playModeStateChanged
projectChanged
hierarchyChanged
```

belong to Editor lifecycle scopes.

They must be unsubscribed deterministically.

ADR-008 applies.

---

## 37. Assembly Reload Events

Editor integrations may use:

```text
AssemblyReloadEvents
```

for cleanup where necessary.

This must not leak into Runtime.

---

## 38. Runtime Domain Reload Independence

Runtime architecture must not rely on Editor assembly reload events for correct cleanup.

Its ownership/lifecycle is explicit.

---

## 39. Play Mode

LumaFlow Runtime must work normally in Play Mode.

Editor tooling that hosts Runtime widgets outside Play Mode must also work.

This dual usage is a major reason shared Core must remain environment-neutral.

---

## 40. Enter Play Mode Options

Editor integrations must not assume domain reload or scene reload always occurs.

Static mutable state should remain minimized.

---

## 41. Player Builds

A project using only Runtime components must build players without including `LumaFlow.Editor`.

Editor assemblies must be excluded automatically through asmdef configuration.

---

## 42. Build Validation

Player build validation must include checking that:

```text
LumaFlow.Runtime
```

has no accidental Editor references.

This should eventually be part of CI.

---

## 43. WebGL / Mobile / Desktop

Runtime assembly must remain valid for supported Runtime platforms.

Editor assembly compatibility is irrelevant to player target platform.

---

## 44. Optional Runtime Modules

Future optional Runtime modules may include:

```text
LumaFlow.Effects
LumaFlow.Navigation
LumaFlow.Localization
```

if modularization becomes useful.

These must still remain independent from Editor.

---

## 45. Optional Editor Modules

Editor-specific integrations may similarly be split:

```text
LumaFlow.Editor
LumaFlow.Editor.Diagnostics
LumaFlow.Editor.ComponentGallery
```

only when package scale justifies it.

Do not fragment assemblies prematurely.

---

## 46. Dependency Direction

Preferred long-term graph:

```text
LumaFlow.Runtime
        ↑
LumaFlow.Editor
```

Optional:

```text
LumaFlow.Runtime
        ↑
LumaFlow.Effects
        ↑
LumaFlow.Editor.Effects
```

where each direction remains intentional.

---

## 47. No Cycles

Assembly dependency cycles are forbidden.

Example:

```text
Runtime
→ Editor
→ Runtime
```

must never exist.

Similarly optional modules must be arranged as a DAG.

---

## 48. Shared Internal Utilities

A utility needed by Runtime and Editor belongs in Runtime or a shared runtime-safe assembly if it has no Editor dependency.

Do not copy/paste utilities between assemblies.

---

## 49. Editor Helper Should Stay Editor

A helper using UnityEditor APIs must not be moved into Runtime just because a Runtime component wants to call it.

Instead redesign the boundary.

---

## 50. Dependency Inversion for Optional Editor Behavior

If Runtime needs optional behavior supplied by Editor, prefer inversion.

Conceptually:

```text
Runtime contract
↑
Editor implementation
```

rather than:

```text
Runtime calls Editor static class
```

Use only when genuinely necessary.

---

## 51. Example: Editor Theme Provider

Runtime defines:

```text
ThemeData
```

Editor defines:

```text
EditorThemeFactory
    ↓
ThemeData
```

Runtime never needs to know how Editor theme was produced.

---

## 52. Example: Serialized Property Widget

Editor-only:

```csharp
SerializedPropertyField(
    property: property
)
```

may internally use:

```text
UnityEditor.UIElements.PropertyField
```

but can still participate inside:

```text
Column
Card
Theme
Overlay
```

from Runtime.

---

## 53. Runtime Tests

Runtime tests must compile without Editor-only product dependencies except when running through Unity's test environment itself.

Test code may use Editor test infrastructure where Unity requires it, but product Runtime assembly remains clean.

---

## 54. Editor Tests

Editor-specific components receive separate tests.

Examples:

```text
EditorWindow mounting
SerializedProperty adapters
Editor theme
AssetDatabase integration
```

---

## 55. Test Assembly Boundaries

Suggested:

```text
LumaFlow.Runtime.Tests
→ LumaFlow.Runtime

LumaFlow.Editor.Tests
→ LumaFlow.Runtime
→ LumaFlow.Editor
```

No Runtime product dependency on test assemblies.

---

## 56. Samples

Samples should clearly state whether they are:

```text
Runtime
Editor
Shared
```

Do not create a sample whose code looks runtime-compatible but silently depends on UnityEditor.

---

## 57. AudioLib Dogfooding

AudioLib Editor UI can heavily exercise `LumaFlow.Editor`.

However, framework Core design must not become Editor-only merely because the first major dogfood application is an Editor tool.

This is an important constraint.

---

## 58. Runtime Dogfood Requirement

Alongside AudioLib, maintain at least one small Runtime sample.

This prevents architecture from drifting toward Editor-only assumptions.

---

## 59. Runtime Sample Scope

A simple Runtime sample should exercise:

```text
mount
Theme
State<T>
Button
TextField
layout
ListView
Overlay
```

as the framework matures.

---

## 60. Editor Sample Scope

Editor sample should exercise:

```text
EditorWindow mount
shared widgets
Editor-only controls
Undo/SerializedObject integration
multiple windows
```

where available.

---

## 61. Naming

Editor-only public types should live in clear namespaces.

Example:

```text
LumaFlow.Editor
LumaFlow.Editor.Controls
```

Runtime:

```text
LumaFlow
LumaFlow.UI
```

Exact namespace design may evolve.

---

## 62. Avoid `Editor` Suffix Everywhere

Namespace/assembly separation already communicates environment.

Use names that remain readable.

Example:

```text
LumaFlow.Editor.SerializedPropertyField
```

is clearer than:

```text
LumaFlow.Editor.EditorSerializedPropertyField
```

unless ambiguity requires it.

---

## 63. API Documentation

Documentation must label Editor-only APIs clearly.

Users should know whether a component can be included in player code.

---

## 64. Compiler as Boundary Enforcement

Prefer architecture that lets the compiler enforce separation.

Asmdefs are stronger than documentation such as:

```text
"please don't call this at runtime"
```

---

## 65. No Reflection Bridge to Editor

Do not bypass the boundary with runtime reflection such as:

```text
Type.GetType("UnityEditor.EditorWindow...")
```

to secretly use Editor APIs from Runtime.

This defeats the architecture.

---

## 66. No Dynamic Editor Loading

Runtime should not attempt to dynamically load Editor assemblies.

Editor-specific behavior belongs in Editor compile-time code.

---

## 67. No Editor Symbols in Runtime Public Contracts

Even optional generic constraints or attributes referring to Editor types are forbidden in Runtime APIs.

---

## 68. Editor Attributes

Editor-only attributes belong to Editor.

Runtime serializable attributes may remain Runtime if valid in players.

---

## 69. UnityEngine vs UnityEditor

`UnityEngine` does not imply Runtime-safe automatically.

Some UnityEngine APIs may still have platform/version constraints.

But the fundamental forbidden dependency is `UnityEditor`.

---

## 70. Internal Access

Editor may require access to some Runtime internals.

Prefer public/protected extension points only where they represent legitimate product API.

Do not expose internals just to simplify Editor implementation.

---

## 71. InternalsVisibleTo for LumaFlow.Editor

Using:

```text
InternalsVisibleTo("LumaFlow.Editor")
```

may be acceptable for tightly coupled framework-owned Editor tooling.

This should be limited and documented.

Do not use it as the third-party extension strategy.

---

## 72. Internal Runtime Diagnostics

Editor diagnostic tooling may need read-only access to runtime node information.

A carefully designed internal diagnostics interface may be preferable to exposing WidgetNode publicly.

---

## 73. Diagnostics Bridge

Potential:

```text
Runtime internal diagnostics snapshot
        ↑
Editor inspector reads it
```

rather than:

```text
Editor mutates WidgetNode directly
```

This protects runtime invariants.

---

## 74. Editor Tooling Must Be Observational by Default

Widget inspectors/debuggers should initially observe runtime state.

Mutation tools require deliberate APIs.

Do not allow Editor diagnostics to arbitrarily alter private runtime structures.

---

## 75. Runtime Logging

Runtime diagnostic messages must not depend on:

```text
Editor console APIs
EditorGUI
```

Use standard Unity/runtime logging abstractions where necessary.

Editor can enhance presentation separately.

---

## 76. Gizmos

If visual debugging later uses SceneView/Gizmos, that belongs in Editor.

Core may expose data needed by the visualization.

---

## 77. UI Toolkit Debugger

Native UI Toolkit Debugger remains available because generated elements are native.

LumaFlow Editor tooling complements it rather than replacing it.

---

## 78. Editor-Only Performance Tools

Detailed WidgetNode inspectors, theme inspectors, or profiling windows likely belong in Editor.

Instrumentation hooks may live in Runtime if lightweight and safe.

---

## 79. Development-Only Runtime Instrumentation

Some diagnostics may compile only in development/editor configurations.

Do not let debug instrumentation alter normal lifecycle semantics.

---

## 80. Conditional Attributes

A runtime diagnostics call may use `[Conditional]` where appropriate.

This does not alter the Runtime/Editor assembly rule.

---

## 81. Distribution

UPM package must place Editor code in directories/asmdefs recognized as Editor-only.

Asset Store distribution must preserve the same logical separation.

---

## 82. Package Installation

A user installing LumaFlow in a runtime-only project still receives Editor source physically if distributed together, but that code must not enter player compilation.

---

## 83. Optional Editor Package Split

If package size or dependency needs justify it later, LumaFlow.Editor could become a separate UPM package.

This is not required initially.

Architectural separation makes such a split possible.

---

## 84. Shared Package First

Initial distribution may keep:

```text
Runtime/
Editor/
```

inside one UPM package for convenience.

This does not weaken assembly boundaries.

---

## 85. Versioning

Runtime and Editor APIs may version together while distributed as one package.

If split later, compatibility constraints must be explicit.

---

## 86. Dependency Changes

Adding a new Editor dependency must not automatically add it to Runtime package dependencies unless Runtime genuinely needs it.

---

## 87. Third-Party Editor Integrations

Optional integrations with other Editor packages should live in isolated Editor assemblies.

Example:

```text
LumaFlow.Editor.SomePackage
```

if needed.

Core Editor must not require every optional tool package.

---

## 88. Third-Party Runtime Integrations

Likewise Runtime integrations remain separate from Editor integrations.

Dependency boundaries should be explicit.

---

## 89. CI Forbidden Reference Test

CI should eventually detect:

```text
UnityEditor reference
```

inside Runtime assembly source or compiled references.

This is a release-blocking violation.

---

## 90. Player Compilation Gate

Every meaningful release should compile at least one player target using LumaFlow.Runtime.

This catches accidental Editor leakage.

---

## 91. Editor Compilation Gate

The same release should compile Editor integration.

Both directions matter.

---

## 92. Assembly Definition Tests

CI or validation scripts may inspect `.asmdef` dependency graphs.

Detect:

```text
Runtime → Editor
cycles
optional dependency leaks
```

automatically.

---

## 93. Runtime API Reflection Check

A future validation test may inspect public Runtime API types and ensure none reference UnityEditor assemblies.

This is a strong safety gate.

---

## 94. Dogfooding Check

When adding an Editor-specific feature during AudioLib work, ask:

```text
Could this concept be useful at Runtime?
```

If yes, split:

```text
Runtime abstraction
+
Editor adapter
```

instead of placing the entire concept in Editor.

---

## 95. Example: Asset Picker

Editor:

```text
AssetPicker
↓
AssetDatabase / ObjectField
```

Runtime:

```text
no AssetDatabase dependency
```

A generic runtime Dropdown or Select remains separate.

---

## 96. Example: Undo-Aware Slider

Core:

```text
Slider
↔ State<float>
```

Editor integration:

```text
SerializedProperty
↔ Editor binding adapter
↔ Slider
```

Do not add Undo logic to Core Slider.

---

## 97. Example: Editor Window

```csharp
public sealed class AudioLibWindow : EditorWindow
{
    private MountHandle? _mount;

    public void CreateGUI()
    {
        _mount = LumaFlow.Mount(
            new AudioLibView(),
            rootVisualElement,
            theme: EditorThemeFactory.Create()
        );
    }

    private void OnDisable()
    {
        _mount?.Dispose();
        _mount = null;
    }
}
```

Conceptual example only.

The core mount API remains Runtime-compatible.

---

## 98. Example: Runtime Host

```csharp
public sealed class MainMenuHost : MonoBehaviour
{
    [SerializeField]
    private UIDocument document;

    private MountHandle? _mount;

    private void OnEnable()
    {
        _mount = LumaFlow.Mount(
            new MainMenu(),
            document.rootVisualElement
        );
    }

    private void OnDisable()
    {
        _mount?.Dispose();
        _mount = null;
    }
}
```

No UnityEditor reference exists.

---

## 99. Rejected Alternative: One Assembly for Everything

Rejected.

Reasons:

```text
Editor references become dangerous
player compatibility becomes fragile
boundaries become social rather than compiler-enforced
```

---

## 100. Rejected Alternative: Runtime Uses `#if UNITY_EDITOR` Everywhere

Rejected as the primary strategy.

This leads to mixed responsibilities and difficult code maintenance.

Use assemblies first.

---

## 101. Rejected Alternative: Duplicate Runtime and Editor Frameworks

Rejected:

```text
LumaFlowRuntimeButton
LumaFlowEditorButton
```

for shared components.

Most UI architecture is common.

Reuse Runtime Core.

---

## 102. Rejected Alternative: Editor Core with Runtime Adapter

Rejected.

Runtime must be foundational because it has the stricter environment.

Editor builds upward from Runtime.

---

## 103. Rejected Alternative: Make Core Depend on Editor for Better Tooling

Rejected.

Tooling must adapt to Core, not make Core player-incompatible.

---

## 104. Rejected Alternative: Hide Editor Calls Behind Reflection

Rejected because it bypasses compile-time dependency safety.

---

## 105. Architectural Invariants Created by This ADR

Unless superseded:

### Invariant 1

Runtime never depends on Editor.

### Invariant 2

Editor may depend on Runtime.

### Invariant 3

Runtime product assemblies contain no UnityEditor references.

### Invariant 4

Shared UI primitives live in Runtime.

### Invariant 5

Editor-only controls live in Editor.

### Invariant 6

Runtime BuildContext/State/Theme architecture is reused by Editor.

### Invariant 7

Editor-specific integration is layered above Core.

### Invariant 8

Player builds must succeed without compiling Editor assemblies.

### Invariant 9

Asmdefs enforce environment boundaries.

### Invariant 10

Editor dogfooding must not distort Core into an Editor-only framework.

---

## 106. Codex Rules

### Rule 1

Never add `using UnityEditor` to Runtime source.

### Rule 2

Never reference `LumaFlow.Editor` from Runtime.

### Rule 3

If a feature requires UnityEditor, place it in Editor assembly.

### Rule 4

If an Editor feature contains reusable environment-neutral logic, extract that logic into Runtime.

### Rule 5

Do not use `#if UNITY_EDITOR` as a substitute for moving substantial Editor behavior out of Runtime.

### Rule 6

Keep shared components implemented once in Runtime.

### Rule 7

Do not expose Editor types through Runtime public APIs.

### Rule 8

Do not introduce reflection bridges from Runtime into UnityEditor.

### Rule 9

Maintain independent Runtime and Editor tests.

### Rule 10

Treat any Runtime → Editor assembly dependency as a release-blocking architectural regression.

---

## 107. Decision Test

When adding code:

```text
Does it reference UnityEditor?
        ↓ yes
Editor assembly.

Does it work in a player without Editor APIs?
        ↓ yes
Runtime candidate.

Is most logic runtime-safe but one adapter is Editor-only?
        ↓ yes
Split abstraction and adapter.

Does moving it to Runtime introduce an Editor type in public API?
        ↓ yes
Redesign boundary.
```

---

## 108. Initial Assembly Target

Initial package should begin approximately with:

```text
LumaFlow.Runtime
LumaFlow.Editor
LumaFlow.Runtime.Tests
LumaFlow.Editor.Tests
```

Avoid excessive assembly fragmentation before the codebase requires it.

---

## 109. Future Assembly Direction

As the package grows:

```text
LumaFlow.Runtime
├── optional runtime modules

LumaFlow.Editor
├── diagnostics
├── component gallery
└── optional editor integrations
```

may emerge.

Dependency directions remain explicit.

---

## 110. Initial Validation

Before Phase 1 is considered stable:

```text
Runtime assembly compiles
Editor assembly compiles
Editor depends on Runtime
Runtime does not depend on Editor
simple player build succeeds
simple EditorWindow mount succeeds
```

---

## 111. Reconsideration Conditions

Revisit this ADR only if:

1. Unity fundamentally changes Runtime/Editor assembly architecture;
2. package distribution requires physically separate packages;
3. Editor-only tooling needs a carefully designed runtime diagnostics bridge;
4. optional module scale justifies additional assembly layers.

The one-way dependency principle should remain.

---

## 112. Final Decision

LumaFlow's environment architecture is:

```text
Runtime Core
    ↑
Editor Integration
```

not:

```text
mixed Runtime + Editor code
```

The guiding rule is:

**If code can run in a player, it belongs below the boundary.  
If it requires UnityEditor, it belongs above it.  
The dependency arrow only points downward toward Runtime.**