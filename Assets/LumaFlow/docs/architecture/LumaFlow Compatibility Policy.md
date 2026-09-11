# LumaFlow Compatibility Policy

## 1. Purpose

This document defines the compatibility policy for LumaFlow.

It describes the environments in which LumaFlow is expected to:

- compile;
- import;
- run;
- render correctly;
- preserve its public API;
- avoid unnecessary platform or render-pipeline dependencies.

This document is normative for framework architecture and release validation.

Compatibility is not an afterthought.

A change that breaks a supported environment without an explicit compatibility decision is considered a regression.

---

# 2. Compatibility Goals

LumaFlow should be usable in a broad range of Unity projects without forcing developers to restructure their project around the framework.

The framework should aim to support:

```text
Unity Runtime UI
Unity Editor UI

Built-in Render Pipeline
Universal Render Pipeline
High Definition Render Pipeline

Mono
IL2CPP

Windows
macOS
Linux

Android
iOS
WebGL
```

Not every platform requires identical validation depth during early development.

However, architecture must avoid decisions that unnecessarily prevent future support.

---

# 3. Compatibility Principle

LumaFlow Core must depend on the smallest reasonable Unity API surface.

Preferred dependency direction:

```text
LumaFlow
    ↓
Unity UI Toolkit
    ↓
Unity Engine
```

Avoid:

```text
LumaFlow Core
    ↓
URP
HDRP
UnityEditor
platform-specific package
third-party framework
```

unless the dependency belongs to an explicitly optional integration module.

---

# 4. Supported Unity Versions

LumaFlow must define an explicit minimum Unity version.

During early development, the exact minimum version may change.

The initial policy should prefer:

```text
Unity 6+
```

unless compatibility testing demonstrates that supporting an earlier LTS version is inexpensive and architecturally clean.

Do not claim compatibility with a Unity version that is not tested or at least compiled against.

---

# 5. Version Support Strategy

The compatibility strategy should prioritize:

```text
current Unity LTS / stable generation
        ↓
recent supported Unity versions
        ↓
older versions only when maintenance cost remains reasonable
```

Do not introduce extensive compatibility layers for obsolete Unity versions during MVP development.

---

# 6. Unity Version Defines

Version-specific code must be isolated.

Example:

```csharp
#if UNITY_6000_0_OR_NEWER
    // Unity 6 implementation
#else
    // fallback if officially supported
#endif
```

Do not scatter version checks throughout unrelated components.

Prefer dedicated compatibility adapters.

Example:

```text
Runtime/
└── Compatibility/
    ├── UiToolkitCompatibility.cs
    └── UnityVersionCompatibility.cs
```

Only create compatibility abstraction when there is an actual version difference to isolate.

---

# 7. Unsupported Unity Versions

If a Unity version is below the official minimum:

LumaFlow does not need to provide runtime workarounds.

Package metadata and documentation should make the minimum version clear.

Failing clearly is preferable to pretending unsupported versions work.

---

# 8. UI Toolkit Requirement

LumaFlow is fundamentally built on Unity UI Toolkit.

Therefore:

```text
UI Toolkit is required.
```

LumaFlow does not support:

```text
uGUI as its rendering backend
IMGUI as its rendering backend
third-party canvas renderers as its rendering backend
```

Interop may be possible, but these systems are not LumaFlow's underlying architecture.

---

# 9. Runtime UI Support

LumaFlow should support UI Toolkit runtime interfaces.

Runtime support includes:

```text
game menus
settings screens
HUDs
inventory screens
launchers
application-like interfaces
runtime tools
```

Core runtime widgets must not require Editor assemblies.

---

# 10. Editor UI Support

LumaFlow should support Unity Editor interfaces where UI Toolkit supports the required behavior.

Possible targets:

```text
EditorWindow
custom inspectors
settings pages
package tooling
asset management tools
debugging panels
```

Editor-specific controls belong in:

```text
LumaFlow.Editor
```

Shared widgets should remain in Runtime whenever possible.

---

# 11. Runtime / Editor Boundary

This is a strict architectural invariant.

Allowed:

```text
LumaFlow.Editor
    ↓
LumaFlow.Runtime
```

Forbidden:

```text
LumaFlow.Runtime
    ↓
UnityEditor
```

Forbidden:

```text
LumaFlow.Runtime
    ↓
LumaFlow.Editor
```

Any Runtime source file containing:

```csharp
using UnityEditor;
```

must be treated as suspicious and should normally be rejected.

---

# 12. Editor Assembly Constraints

Editor-only assemblies must be explicitly restricted.

Typical asmdef configuration should ensure:

```text
Include Platforms:
Editor
```

or equivalent Unity assembly constraints.

Editor APIs must never accidentally enter player builds.

---

# 13. Render Pipeline Support

LumaFlow Core must support:

```text
Built-in Render Pipeline
Universal Render Pipeline
High Definition Render Pipeline
```

Core framework behavior must be render-pipeline agnostic.

---

# 14. Built-in Render Pipeline

A project using the Built-in Render Pipeline must be able to:

- install LumaFlow;
- compile Runtime;
- compile Editor modules;
- use standard widgets;
- use themes;
- use state;
- build player applications.

Built-in support must not require installing URP or HDRP packages.

---

# 15. Universal Render Pipeline

A URP project must be able to use LumaFlow without special configuration for ordinary UI.

LumaFlow Core must not assume:

- a specific URP Renderer;
- a Forward or Deferred renderer;
- a Renderer Feature;
- a specific pipeline asset;
- a camera stack;
- post-processing.

---

# 16. High Definition Render Pipeline

An HDRP project must be able to use LumaFlow Core without special configuration for ordinary UI.

LumaFlow must not assume:

- HDR output;
- HDRP Custom Passes;
- post-processing;
- specific frame settings;
- a specific HDRP asset.

---

# 17. Pipeline-Specific Dependencies

The following references are forbidden in Core:

```text
UnityEngine.Rendering.Universal
UnityEngine.Rendering.HighDefinition
```

unless protected by a deliberately isolated optional assembly.

Core asmdefs must not require URP/HDRP assembly references.

---

# 18. Optional Render Pipeline Integrations

Future advanced visual modules may use:

```text
LumaFlow.Effects
LumaFlow.Effects.URP
LumaFlow.Effects.HDRP
```

or equivalent packages/assemblies.

Dependency direction:

```text
LumaFlow.Effects.URP
        ↓
LumaFlow.Effects
        ↓
LumaFlow.Runtime
```

and:

```text
LumaFlow.Effects.HDRP
        ↓
LumaFlow.Effects
        ↓
LumaFlow.Runtime
```

Never:

```text
LumaFlow.Runtime
        ↓
LumaFlow.Effects.URP
```

---

# 19. Optional Dependency Rule

An optional feature must not make its dependency mandatory for the entire package.

Example:

If `BackdropBlur` requires URP-specific behavior:

do not make:

```text
LumaFlow.Runtime
```

depend on URP.

Instead isolate the feature.

---

# 20. Graceful Degradation

Optional visual effects should degrade gracefully where reasonable.

Example:

```text
GlassSurface
```

may use:

```text
URP/HDRP advanced blur
```

where supported and:

```text
translucent surface fallback
```

elsewhere.

But ordinary widgets must not degrade.

These must remain fully functional:

```text
Text
Button
TextField
Toggle
Slider
Card
Row
Column
Container
State<T>
Theme
Navigation
```

---

# 21. Shader Policy

LumaFlow Core should avoid custom shaders unless absolutely necessary.

If shaders are introduced:

- prefer pipeline-independent shaders;
- isolate pipeline-specific variants;
- document shader compatibility;
- provide reasonable fallback behavior.

Do not add a shader dependency for functionality achievable through standard UI Toolkit styling.

---

# 22. Color Space

LumaFlow must not assume one project color space.

Supported projects may use:

```text
Gamma
Linear
```

Theme colors and controls should rely on Unity-supported color behavior.

Any effect sensitive to color space must be tested separately.

---

# 23. Mono Support

LumaFlow should compile and run under the Mono scripting backend where Unity supports it.

Avoid runtime implementation choices that unnecessarily require IL2CPP-specific behavior.

---

# 24. IL2CPP Support

LumaFlow must aim for IL2CPP compatibility.

Core architecture should avoid unnecessary reliance on:

- runtime dynamic code generation;
- unsupported Reflection.Emit;
- JIT-only behavior;
- dynamic assembly generation.

Reflection should be minimized.

If reflection is eventually used for convenience APIs, verify IL2CPP and code stripping behavior.

---

# 25. AOT Compatibility

Public APIs should be designed with AOT environments in mind.

Be cautious with:

```text
runtime generic type construction
reflection-based serializers
dynamic invocation
unbounded generic reflection
```

Typed APIs are preferred.

---

# 26. Code Stripping

LumaFlow should not require users to add large `link.xml` files for normal usage.

If functionality depends on reflected types, stripping requirements must be:

- minimized;
- isolated;
- documented.

Avoid reflection-driven architecture that causes unpredictable IL2CPP stripping failures.

---

# 27. Managed Code Requirements

LumaFlow should remain managed C# wherever possible.

Avoid native plugins for Core functionality.

A declarative UI framework should not require platform-specific native libraries.

---

# 28. Windows Support

Windows desktop should be considered a primary validation platform.

Relevant targets may include:

```text
Windows x86_64 player
Unity Editor on Windows
```

Core behavior should remain platform-independent.

---

# 29. macOS Support

LumaFlow should support:

```text
Unity Editor on macOS
macOS player
```

Do not use Windows-specific filesystem, process, input, or graphics APIs in Core.

---

# 30. Linux Support

LumaFlow should aim to support:

```text
Unity Editor on Linux
Linux player
```

subject to Unity UI Toolkit capabilities.

Avoid assumptions about:

- path separators;
- installed system fonts;
- native window APIs.

---

# 31. Android Support

Runtime LumaFlow should be architecturally compatible with Android.

Important considerations:

- touch input;
- display scaling;
- orientation;
- safe areas;
- performance;
- IL2CPP;
- memory allocations.

Editor-only functionality is naturally not relevant to Android builds.

---

# 32. iOS Support

Runtime LumaFlow should be architecturally compatible with iOS.

Consider:

- IL2CPP/AOT;
- touch input;
- safe areas;
- display scaling;
- orientation;
- code stripping.

Avoid runtime dynamic functionality incompatible with AOT.

---

# 33. WebGL Support

LumaFlow should aim to remain WebGL-compatible for standard UI.

Avoid relying on:

- threads;
- native plugins;
- unsupported filesystem operations;
- JIT-only runtime code generation.

Core UI behavior should be sufficiently lightweight for browser builds.

---

# 34. WebGL Threads

Core architecture must not require multithreading.

State propagation and UI updates should work on Unity's main thread.

If background-thread features are introduced later, WebGL limitations must be considered.

---

# 35. Consoles

Console platforms may not be part of the initial public compatibility guarantee because access and testing can be restricted.

However, architecture should avoid unnecessary assumptions that prevent future console support.

Core should remain platform-neutral.

---

# 36. Input Compatibility

LumaFlow should primarily rely on UI Toolkit's event/input abstraction.

Do not manually couple ordinary controls to:

```text
Input.GetKey
Input.GetMouseButton
new Input System
old Input Manager
```

unless implementing a deliberately specialized integration.

This preserves compatibility with Unity's own UI event handling.

---

# 37. New Input System

LumaFlow should not require the New Input System package for ordinary UI functionality unless UI Toolkit itself requires it in a specific supported scenario.

Optional integrations may be added separately.

---

# 38. Legacy Input Manager

LumaFlow must not directly depend on the legacy Input Manager for core widgets.

UI Toolkit should remain responsible for ordinary interaction.

---

# 39. Keyboard Support

Desktop and Editor controls should preserve UI Toolkit keyboard interaction.

Avoid wrapping native controls in ways that break:

- focus;
- tab navigation;
- Enter;
- Space;
- Escape;
- arrow navigation.

---

# 40. Pointer Support

LumaFlow should preserve UI Toolkit support for:

```text
mouse
touch
pen
```

where the underlying Unity environment supports them.

Avoid mouse-specific assumptions in high-level components.

---

# 41. DPI and Scaling

LumaFlow must not assume a fixed physical display density.

Layout should remain based on UI Toolkit panel semantics.

Responsive design should react to logical panel size rather than hardcoded monitor assumptions.

---

# 42. Safe Areas

Mobile safe-area support may be introduced through:

```text
MediaQuery
SafeArea
```

or equivalent.

Core architecture should leave room for this feature.

Do not hardcode mobile safe area behavior into generic Container or Padding components.

---

# 43. Orientation

Runtime UI should not assume landscape or portrait orientation.

Responsive features should eventually allow layout adaptation.

---

# 44. UI Scale

LumaFlow should respect UI Toolkit panel scaling.

Do not introduce an unrelated global scaling system unless real requirements demonstrate a need.

---

# 45. Fonts

Core must not depend on a particular bundled font.

Framework default themes may rely on Unity-compatible defaults or explicitly documented included assets if licensing permits.

User-defined typography must allow custom Unity font resources.

---

# 46. Icons

LumaFlow Core must not require a third-party icon package.

Icon APIs should support Unity-native sources such as:

```text
Texture2D
VectorImage
```

A default icon set, if added later, should be optional or properly licensed.

---

# 47. Localization Package

LumaFlow Core should not require Unity Localization.

A future integration may support it.

Possible:

```text
LumaFlow.Localization.Unity
```

or an adapter layer.

Core text widgets must work without that package installed.

---

# 48. Addressables

LumaFlow Core must not require Addressables.

Users may load UI-related assets through Addressables themselves.

Optional helper integrations may exist later.

---

# 49. TextMeshPro

LumaFlow UI Toolkit support must not require TextMeshPro.

Do not couple LumaFlow Text to TMP APIs.

Use UI Toolkit text capabilities.

---

# 50. Third-Party Package Policy

Core dependencies should remain minimal.

Before adding a dependency, evaluate:

1. Is this capability essential?
2. Does Unity already provide it?
3. Is the dependency runtime or editor only?
4. Does it support all targeted platforms?
5. Does it support IL2CPP/AOT?
6. Does it create licensing issues?
7. Does it significantly increase package maintenance risk?

Do not add dependencies for trivial utilities.

---

# 51. Package Optionality

Feature modules should be structured so users only pay dependency cost for features they use.

Long-term conceptual structure:

```text
com.sahland.lumaflow
com.lumaflow.effects
com.lumaflow.effects.urp
com.lumaflow.effects.hdrp
```

or equivalent assembly-level separation inside one package.

The final distribution model may evolve.

---

# 52. asmdef Philosophy

Assembly definitions are compatibility boundaries.

Use them intentionally.

Initial:

```text
LumaFlow.Runtime
LumaFlow.Editor
LumaFlow.Tests.Runtime
LumaFlow.Tests.Editor
```

Do not create one asmdef per folder.

---

# 53. asmdef References

Runtime asmdef should only reference required runtime Unity modules.

Editor asmdef may reference:

```text
LumaFlow.Runtime
UnityEditor assemblies
```

Optional integrations may reference additional packages.

---

# 54. Version Defines in asmdef

Unity asmdef Version Defines may be used when an optional package genuinely changes available functionality.

Do not use Version Defines merely to hide poor dependency structure.

---

# 55. Define Constraints

Define Constraints are appropriate for truly optional assemblies.

Example conceptual flow:

```text
URP package installed
        ↓
URP integration assembly enabled
```

Core must still compile if the condition is false.

---

# 56. No Phantom Dependencies

A clean project must not produce errors such as:

```text
The type or namespace name 'Universal' could not be found
```

because URP is absent.

Likewise:

```text
HighDefinition could not be found
```

must never occur in Core.

This is considered a critical compatibility bug.

---

# 57. Unity Package Manager

LumaFlow should be compatible with standard Unity Package Manager installation.

Primary development distribution may use:

```text
Git URL
```

Package structure should follow Unity package conventions.

---

# 58. Asset Store Distribution

If LumaFlow is also distributed through Unity Asset Store, the Asset Store version should preserve the same architecture and package behavior where practical.

Do not maintain two incompatible codebases.

---

# 59. Source Distribution

LumaFlow should remain source-accessible when distributed as an open-source Unity package.

Avoid precompiled DLL-only architecture unless an optional module has a strong reason.

Source distribution improves:

- debugging;
- contribution;
- compatibility investigation;
- trust.

---

# 60. Namespace Compatibility

Avoid public namespaces containing Unity version numbers or render pipeline names unless the module is specifically tied to them.

Good:

```text
LumaFlow
LumaFlow.Editor
LumaFlow.Effects.URP
```

Bad:

```text
LumaFlow.Unity6000
```

for ordinary public APIs.

---

# 61. Serialization

LumaFlow core widget descriptions do not need to depend on Unity serialization unless a feature explicitly requires persistent serialized data.

Avoid designing ordinary widgets around:

```text
MonoBehaviour
ScriptableObject
SerializedObject
```

These remain useful for integration but should not define Core architecture.

---

# 62. Domain Reload

Editor-related runtime state should behave correctly across Unity domain reloads.

Do not assume static state survives safely.

Avoid critical global mutable static framework state.

---

# 63. Enter Play Mode Options

LumaFlow should avoid architecture that assumes domain reload always occurs when entering Play Mode.

Static caches, if introduced, must be robust when:

```text
Domain Reload disabled
```

is used.

---

# 64. Assembly Reload

Editor tools must clean subscriptions properly before/through assembly reload where relevant.

Avoid static Editor event leaks.

---

# 65. Scene Independence

LumaFlow Core should not require a specific scene hierarchy.

A UI framework should be mountable wherever a valid UI Toolkit root exists.

Do not require:

```text
LumaFlowManager MonoBehaviour
```

in every scene unless future application-level features genuinely need one.

---

# 66. PanelSettings

Runtime UI may depend on normal UI Toolkit `PanelSettings`.

LumaFlow should not require one specific global PanelSettings asset.

Users must remain free to configure their UI Toolkit panel architecture.

---

# 67. Multiple Panels

Architecture should not assume only one UI Toolkit panel exists.

Long-term LumaFlow should tolerate:

```text
multiple UIDocuments
multiple panels
multiple editor windows
```

BuildContext and mounting state must remain tree/panel scoped.

---

# 68. Multiple Windows

Editor usage must not rely on a single global root or global current window.

A LumaFlow tree should belong to a specific mount root.

---

# 69. Static State Policy

Static readonly utility data is acceptable.

Global mutable framework state should be avoided.

Bad:

```csharp
public static ThemeData CurrentTheme;
```

Good:

```text
Theme propagated through BuildContext
```

This improves compatibility with:

```text
multiple panels
multiple editor windows
tests
domain reload behavior
```

---

# 70. Threading Policy

UI Toolkit mutations must be treated as main-thread operations.

Core LumaFlow must not require worker threads.

If state can be modified off-thread in the future:

```text
State change
    ↓
main-thread scheduling
    ↓
VisualElement mutation
```

must be defined explicitly.

---

# 71. Jobs / Burst

LumaFlow Core should not require:

```text
Unity Jobs
Burst
Native Collections
```

for ordinary UI.

These technologies do not provide enough value for the main declarative UI path to justify becoming required dependencies.

---

# 72. Reflection Compatibility

Reflection must not be a foundational requirement for ordinary widget construction.

Typed APIs should remain the primary path.

This improves:

```text
IL2CPP
AOT
code stripping
debuggability
performance
```

---

# 73. Source Generators

Source generators are explicitly optional future tooling.

LumaFlow Core must not require source generation merely to build standard UI.

Any future generator must provide clear value and a non-generator fallback where practical.

---

# 74. C# Language Version

LumaFlow may use the C# language level supported by the minimum targeted Unity version.

Do not use language features unsupported by the declared minimum Unity environment.

Consumer-facing examples must compile under the declared supported configuration.

---

# 75. Collection Expressions

If examples use modern syntax such as:

```csharp
children:
[
    Text("One"),
    Text("Two")
]
```

the minimum Unity/C# compatibility policy must support that syntax.

If not, documentation must use compatible alternatives.

Do not advertise syntax that users cannot compile in supported Unity versions.

---

# 76. Nullable Reference Types

LumaFlow should consider enabling nullable annotations where supported and practical.

Public nullability contracts should be explicit.

Examples:

```text
required child → non-null
optional icon → nullable
optional callback → nullable
```

Do not rely on undocumented null semantics.

---

# 77. Exception Compatibility

Core exceptions should use standard managed exception types where practical.

Do not introduce platform-specific exception behavior.

Framework diagnostics should behave similarly across Editor and Runtime development environments.

---

# 78. Development vs Release Behavior

Additional invariant checks may be more aggressive during development.

However, behavior should not differ so dramatically between Editor and player that bugs become impossible to reproduce.

Do not hide correctness failures only because code is in a release build.

---

# 79. Testing Matrix

The test matrix should grow with project maturity.

Initial minimum:

```text
Unity Editor
Runtime assembly compilation
Editor assembly compilation
Built-in Render Pipeline project
URP project
HDRP project
```

Later:

```text
Windows player
macOS player
Linux player
Android
iOS
WebGL
Mono
IL2CPP
```

---

# 80. Render Pipeline Test Matrix

For each supported Unity generation, where practical:

```text
Built-in
    package imports
    assemblies compile
    sample mounts

URP
    package imports
    assemblies compile
    sample mounts

HDRP
    package imports
    assemblies compile
    sample mounts
```

Core UI visual behavior should not unexpectedly differ.

---

# 81. CI Strategy

Future CI should validate at least:

```text
compilation
tests
package structure
forbidden dependencies
```

Additional matrix builds may validate supported Unity versions and platforms.

---

# 82. Forbidden Dependency Checks

Automated tests or CI scripts should eventually detect accidental Core references to:

```text
UnityEditor
Unity.RenderPipelines.Universal
Unity.RenderPipelines.HighDefinition
```

where they do not belong.

This is especially valuable when Codex or contributors add new features.

---

# 83. Compatibility Samples

Samples should not depend on URP or HDRP unless they specifically demonstrate optional effects.

Core samples must open under any supported render pipeline.

---

# 84. Documentation Compatibility

Every optional integration must clearly state its requirements.

Example:

```text
BackdropBlur URP implementation

Requires:
Universal Render Pipeline
```

Do not make users discover missing dependencies through compiler errors.

---

# 85. Compatibility Levels

LumaFlow documentation may eventually classify support as:

```text
Tier 1
actively tested and officially supported

Tier 2
expected to work and periodically tested

Experimental
architecturally supported but not guaranteed
```

This is preferable to pretending every possible Unity platform receives identical validation.

---

# 86. Initial Recommended Support Tiers

During early development:

## Tier 1

```text
Unity Editor
Windows Editor
Runtime UI
Editor UI
Built-in
URP
HDRP
```

## Tier 2

```text
macOS Editor
Linux Editor
Windows player
macOS player
Linux player
```

## Experimental until validated

```text
Android
iOS
WebGL
```

This classification may evolve.

Do not publicly claim this exact matrix until actual testing begins.

---

# 87. Release Compatibility Gate

Before a public release, verify:

1. package imports;
2. Runtime compiles;
3. Editor compiles;
4. tests pass;
5. no Runtime → UnityEditor reference exists;
6. no Core → URP reference exists;
7. no Core → HDRP reference exists;
8. core sample runs;
9. compatibility documentation matches reality.

---

# 88. Breaking Compatibility Changes

Dropping support for:

```text
a Unity version
a scripting backend
a platform
a render pipeline
```

is a deliberate compatibility decision.

It must be:

- documented;
- justified;
- reflected in package metadata;
- mentioned in release notes.

---

# 89. Compatibility ADR Rule

A major compatibility decision should receive an ADR when it affects architecture.

Examples:

```text
dropping an older Unity generation
requiring Unity 6
adding source generation
making an optional package required
changing IL2CPP support
introducing pipeline-specific rendering architecture
```

---

# 90. Codex Compatibility Checklist

Before implementing a feature, Codex must ask internally:

1. Does this code belong in Runtime or Editor?
2. Does it introduce a new package dependency?
3. Does it introduce URP/HDRP coupling?
4. Does it work without that optional package installed?
5. Does it rely on reflection?
6. Does it affect IL2CPP/AOT?
7. Does it assume one operating system?
8. Does it assume one render pipeline?
9. Does it assume one UI panel?
10. Does it assume domain reload?
11. Can the feature be isolated if compatibility differs?

If compatibility changes materially, update this document or create an ADR.

---

# 91. Codex Rule: Never Fix Compatibility by Polluting Core

Bad solution:

```text
URP feature needed
        ↓
add URP reference to Runtime
```

Correct direction:

```text
URP feature needed
        ↓
create isolated integration
        ↓
Core remains unchanged
```

Compatibility boundaries must remain architectural boundaries.

---

# 92. Codex Rule: No Unverified Claims

Documentation must distinguish:

```text
Supported
Tested
Expected
Experimental
```

Do not mark a platform as fully supported simply because the code appears portable.

---

# 93. Codex Rule: Prefer Public Unity APIs

Avoid undocumented Unity internals for compatibility-sensitive functionality.

If internal APIs become unavoidable:

- isolate them;
- guard by Unity version;
- document them;
- create fallback behavior where possible.

---

# 94. Codex Rule: Compatibility Before Convenience

If a convenience feature requires adding a large mandatory dependency, reconsider the design.

Example:

```text
tiny icon helper
```

should not require an entire external UI package.

---

# 95. Codex Rule: Optional Means Optional

If a module is documented as optional, removing it from the project must not break Core compilation.

This must be testable.

---

# 96. Compatibility Invariants

Unless explicitly changed through an architectural decision:

### Invariant 1

Runtime does not depend on UnityEditor.

### Invariant 2

Core does not depend on URP.

### Invariant 3

Core does not depend on HDRP.

### Invariant 4

Built-in Render Pipeline remains supported for ordinary UI.

### Invariant 5

URP remains supported for ordinary UI.

### Invariant 6

HDRP remains supported for ordinary UI.

### Invariant 7

Core does not require third-party packages.

### Invariant 8

Core does not require native plugins.

### Invariant 9

Core architecture remains compatible with IL2CPP/AOT principles.

### Invariant 10

UI mutation remains compatible with Unity main-thread requirements.

### Invariant 11

Ordinary widgets do not depend on one operating system.

### Invariant 12

Ordinary widgets do not depend on one global panel/window.

---

# 97. Target Compatibility Architecture

```text
                        LumaFlow Core
                             │
                             ▼
                       UI Toolkit
                             │
            ┌────────────────┼────────────────┐
            ▼                ▼                ▼
        Built-in            URP              HDRP


                        LumaFlow Core
                             │
             ┌───────────────┴───────────────┐
             ▼                               ▼
          Runtime                          Editor
                                             │
                                             ▼
                                        UnityEditor


                     Optional Integrations
                             │
             ┌───────────────┼───────────────┐
             ▼               ▼               ▼
           URP             HDRP        Other packages
```

The arrows must never reverse back into Core.

---

# 98. Final Compatibility Principle

LumaFlow should be easy to add to an existing Unity project.

Installing the framework should not force the developer to:

```text
change render pipeline
install URP
install HDRP
switch scripting backend
add a manager MonoBehaviour
rewrite input architecture
install third-party packages
change scene structure
```

The framework should adapt to the Unity project rather than requiring the Unity project to adapt to the framework.

Compatibility is successful when:

```text
Install LumaFlow
        ↓
write declarative UI
        ↓
UI Toolkit continues behaving like UI Toolkit
```

regardless of whether the project uses:

```text
Built-in
URP
HDRP
```

and regardless of whether LumaFlow is being used for:

```text
Runtime UI
Editor tooling
```

within the environments officially declared as supported.
