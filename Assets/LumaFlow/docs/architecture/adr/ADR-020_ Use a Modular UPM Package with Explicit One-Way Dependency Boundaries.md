# ADR-020: Use a Modular UPM Package with Explicit One-Way Dependency Boundaries

- **Status:** Accepted
- **Decision date:** 2026-08-12
- **Scope:** Package architecture, modules, assemblies, dependencies, UPM distribution
- **Affects:** Runtime, Editor, Effects, Tests, Samples, Distribution, CI, Third-Party Extensions
- **Related documents:** `ARCHITECTURE.md`, `ROADMAP.md`, `COMPATIBILITY.md`, `ADR-006-render-pipeline-independence.md`, `ADR-017-runtime-editor-boundary.md`, `ADR-019-testing-and-validation.md`

---

## 1. Context

LumaFlow is intended to grow beyond a handful of UI helpers.

Its long-term scope may include:

```text
Core runtime
Layout
Styling
Theme
Reactive state
Controls
Lists
Navigation
Overlays
Editor integration
Diagnostics
Optional rendering effects
Samples
Testing utilities
```

Without explicit package and dependency boundaries, the project could gradually develop:

```text
cyclic asmdef references
Runtime → Editor dependencies
Core → URP/HDRP dependencies
optional features becoming mandatory
large monolithic assemblies
unnecessary package dependencies
difficult third-party extension points
slow compilation
fragile distribution
```

LumaFlow therefore needs a package architecture that remains understandable as the framework grows.

---

## 2. Decision

LumaFlow will be distributed primarily as a Unity Package Manager package.

The package will use explicit assembly definitions and one-way dependency boundaries.

The fundamental dependency direction is:

```text
foundational Runtime
        ↑
higher-level Runtime modules
        ↑
optional integrations

foundational Runtime
        ↑
Editor integration
```

Specialized modules may depend on foundational modules.

Foundational modules must not depend on specialized modules.

---

## 3. Core Principle

Dependencies should point toward more fundamental abstractions.

Conceptually:

```text
Application
    ↓
LumaFlow high-level modules
    ↓
LumaFlow Runtime foundation
    ↓
Unity UI Toolkit
```

Not:

```text
LumaFlow Runtime
    ↓
optional Effects
    ↓
URP/HDRP
```

---

## 4. Initial Distribution Model

The initial product should be one UPM package.

Conceptually:

```text
com.sahland.lumaflow/
```

containing:

```text
Runtime/
Editor/
Tests/
Samples~/
Documentation~/
package.json
README.md
LICENSE
CHANGELOG.md
```

Exact package identifier must be checked before public release.

---

## 5. Do Not Split Packages Prematurely

Initial development should prefer one consumer-facing package.

Do not immediately create:

```text
com.lumaflow.core
com.lumaflow.widgets
com.lumaflow.navigation
com.lumaflow.editor
com.lumaflow.effects
```

unless separate installation provides real value.

Too many packages create:

- version synchronization complexity;
- installation friction;
- dependency management overhead;
- documentation complexity;
- release overhead.

Assembly boundaries can provide modularity inside one UPM package first.

---

## 6. Package Modularity vs Assembly Modularity

These are separate concerns.

LumaFlow may have:

```text
one UPM package
```

with:

```text
multiple asmdef assemblies
```

This is the preferred initial model.

Physical package splitting remains a future optimization.

---

## 7. Initial Assembly Strategy

Start with the smallest useful assembly graph.

Recommended initial assemblies:

```text
LumaFlow.Runtime
LumaFlow.Editor

LumaFlow.Runtime.Tests
LumaFlow.Editor.Tests
```

Do not create an assembly per folder or feature during Phase 0.

---

## 8. Runtime Assembly

`LumaFlow.Runtime` initially contains shared player-compatible framework functionality:

```text
Widget
WidgetNode runtime
MountHandle
BuildContext
State<T>
Bindings

Layout
Styling
Theme

Core Controls
ListView
Navigation
Overlay
Native Interop
```

as those features are implemented.

---

## 9. Editor Assembly

`LumaFlow.Editor` contains:

```text
EditorWindow integration
Editor-only controls
SerializedObject adapters
AssetDatabase adapters
Editor diagnostics UI
Editor theme adapters
```

It depends on:

```text
LumaFlow.Runtime
```

Runtime does not depend on Editor.

---

## 10. Initial Monolith Is Deliberate

Keeping Runtime in one assembly initially provides:

```text
faster architecture iteration
simpler internal refactoring
fewer visibility problems
less asmdef maintenance
```

before public module boundaries are understood.

Do not modularize based only on hypothetical future scale.

---

## 11. Assembly Split Gate

Split `LumaFlow.Runtime` only when at least one concrete benefit exists.

Examples:

```text
optional dependency isolation
significantly improved compile times
clear feature opt-in
public extension boundary
render-pipeline isolation
independent versioning pressure
```

"Folder is getting large" alone is not sufficient.

---

## 12. Potential Future Runtime Modules

Possible future assemblies may include:

```text
LumaFlow.Runtime
LumaFlow.Navigation
LumaFlow.Localization
LumaFlow.Effects
```

but only if their separation provides value.

Layout, Theme, State, and ordinary Controls should not automatically become separate assemblies.

---

## 13. Foundational Runtime

If Runtime is later decomposed, the foundational assembly should contain only low-level shared contracts.

Conceptually:

```text
LumaFlow.Core
├── Widget
├── WidgetNode abstractions/internal runtime
├── BuildContext core
├── lifecycle
├── MountHandle
└── shared low-level primitives
```

However, creating `LumaFlow.Core` as a separate assembly is deferred until real pressure exists.

---

## 14. Avoid Premature Core Assembly

Do not create a tiny Core assembly merely to make the architecture diagram look clean.

Assembly boundaries impose:

```text
public/internal API pressure
cross-assembly calls
asmdef complexity
testing complexity
```

Use them only when the boundary is meaningful.

---

## 15. Dependency Graph Must Be Acyclic

The complete asmdef graph must always form a directed acyclic graph.

Forbidden:

```text
A → B
B → A
```

or indirect cycles:

```text
A → B
B → C
C → A
```

Any cycle indicates unclear architectural ownership.

---

## 16. Runtime and Editor Direction

Mandatory:

```text
LumaFlow.Editor
        ↓
LumaFlow.Runtime
```

Forbidden:

```text
LumaFlow.Runtime
        ↓
LumaFlow.Editor
```

This repeats ADR-017 because package architecture must enforce it physically.

---

## 17. Effects Direction

Optional rendering effects must depend on Runtime.

Conceptually:

```text
LumaFlow.Effects
        ↓
LumaFlow.Runtime
```

Potential pipeline-specific modules:

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

---

## 18. Forbidden Effects Dependency

Core Runtime must never depend on:

```text
LumaFlow.Effects
LumaFlow.Effects.URP
LumaFlow.Effects.HDRP
```

Otherwise optional visuals become required architecture.

---

## 19. URP Dependency Isolation

If an optional URP integration exists, only the URP-specific assembly may reference:

```text
Unity.RenderPipelines.Universal.Runtime
```

or equivalent URP assemblies.

Do not leak those references into:

```text
Runtime
Effects abstractions
Editor Core
```

unless explicitly needed by an isolated Editor URP adapter.

---

## 20. HDRP Dependency Isolation

The same rule applies to HDRP.

Only HDRP-specific integration assemblies may reference HDRP implementation assemblies.

---

## 21. Built-in Pipeline

The foundational LumaFlow package must compile and operate in a project with neither URP nor HDRP installed.

This is a release invariant.

---

## 22. Optional Dependencies

Optional features must remain genuinely optional.

Installing LumaFlow Core should not automatically install:

```text
URP
HDRP
Unity Localization
Addressables
third-party icon libraries
third-party DI
third-party reactive libraries
```

unless one becomes an explicit mandatory architectural dependency through a future ADR.

---

## 23. Dependency Minimalism

Every package dependency increases:

```text
installation cost
version conflicts
maintenance
compatibility testing
consumer risk
```

Before adding one, ask whether the capability can be provided through Unity/Core APIs or optional integration.

---

## 24. UI Toolkit Dependency

UI Toolkit is the fundamental backend under ADR-001.

This dependency is intentional and non-optional.

LumaFlow does not attempt to abstract across:

```text
UI Toolkit
uGUI
IMGUI
```

as interchangeable rendering backends.

---

## 25. No uGUI Dependency

Core LumaFlow must not require:

```text
UnityEngine.UI
Canvas
RectTransform
```

for ordinary operation.

uGUI interoperability, if ever needed, would be a separate integration concern.

---

## 26. No TextMeshPro Requirement

LumaFlow Core should not require TextMeshPro.

UI Toolkit native text remains the default backend.

If future custom controls use TMP, they belong to optional integrations.

---

## 27. No Addressables Requirement

Assets used by Theme/components may be supplied directly.

Core does not require Addressables for loading them.

Applications remain free to use Addressables externally.

---

## 28. No Localization Requirement

Core Text does not require Unity Localization.

Localization integration may become optional.

---

## 29. No DI Requirement

LumaFlow does not require:

```text
Zenject
VContainer
Extenject
custom DI package
```

Applications may use any DI architecture externally.

---

## 30. No Reactive Package Requirement

`State<T>` and bindings must not require:

```text
UniRx
R3
Reactive Extensions
```

as mandatory dependencies.

Adapters may be added later.

---

## 31. Optional Reactive Integration

A future integration could support:

```text
Observable<T>
third-party reactive streams
```

without changing Core State semantics.

Such adapters belong outside foundational Runtime if they require package dependencies.

---

## 32. Internal Dependency Direction

Within Runtime code, conceptual dependencies should also flow downward.

Example:

```text
Button
↓
Theme / State / Styling / Widget Runtime
```

not:

```text
State<T>
↓
Button
```

Foundational systems should know nothing about high-level components.

---

## 33. State Layer

`State<T>` should remain independent from:

```text
Button
TextField
Navigator
Overlay
```

These systems consume State.

State does not consume them.

---

## 34. Theme Layer

Theme primitives may define generic design-system structures.

Specific component themes may depend on Theme infrastructure.

Avoid making foundational Theme resolution depend on individual runtime WidgetNodes unnecessarily.

---

## 35. Styling Layer

Typed style primitives should not depend on:

```text
Navigator
Overlay
ListView
```

Those systems consume styling.

---

## 36. Navigation Layer

Navigator may depend on:

```text
Widget runtime
BuildContext
lifecycle
```

It should not depend on Overlay unless explicit cooperation is needed.

---

## 37. Overlay Layer

Overlay may depend on:

```text
Widget runtime
BuildContext
lifecycle
layout/styling primitives
```

It should not require Navigator.

They are sibling high-level systems.

---

## 38. Avoid Navigation ↔ Overlay Cycle

Potential integrations must not create:

```text
Navigation → Overlay
Overlay → Navigation
```

If both need common functionality, extract a lower-level shared abstraction.

---

## 39. Lists

ListView may depend on:

```text
Widget runtime
State/binding infrastructure
UI Toolkit native controls
```

Generic runtime lifecycle must not depend on ListView.

---

## 40. Editor Dependency Direction

Editor controls may depend on:

```text
Runtime components
Runtime Theme
Runtime State
```

Runtime never depends on Editor adapters.

---

## 41. Diagnostics Dependency

Runtime diagnostics may observe foundational Runtime concepts.

High-level Runtime features may emit diagnostic data.

Avoid making Core lifecycle depend on Editor diagnostic UI.

---

## 42. Tests Depend on Product

Correct:

```text
Runtime.Tests
↓
Runtime
```

Correct:

```text
Editor.Tests
↓
Editor
↓
Runtime
```

Forbidden:

```text
Runtime
↓
Runtime.Tests
```

---

## 43. Samples Depend on Public API

Samples should primarily consume the same public API users consume.

Avoid samples importing:

```text
LumaFlow.Internal
```

unless the sample explicitly demonstrates framework development internals.

---

## 44. Samples Are Consumers

Treat Samples as consumer projects.

They should reveal whether public API is usable without privileged access.

---

## 45. Documentation Is a Consumer

Likewise documentation examples should use supported public APIs.

Do not document internal helpers merely because they simplify examples.

---

## 46. Runtime Folder Structure

A reasonable initial structure:

```text
Runtime/
├── Core/
│   ├── Widget.cs
│   ├── BuildContext.cs
│   ├── MountHandle.cs
│   └── Internal/
│       ├── WidgetNode.cs
│       └── ...
│
├── State/
├── Styling/
├── Theme/
├── Layout/
├── Controls/
├── Collections/
├── Navigation/
├── Overlay/
└── Interop/
```

Exact naming may evolve with implementation.

---

## 47. Editor Folder Structure

Potential:

```text
Editor/
├── Integration/
├── Controls/
├── Theme/
├── Diagnostics/
└── Windows/
```

Do not mirror Runtime folders mechanically if no Editor-specific code exists there.

---

## 48. Internal Folder Convention

Runtime implementation details may live under:

```text
Internal/
```

and remain assembly-internal.

Folder naming does not enforce access.

C# accessibility and asmdefs remain authoritative.

---

## 49. Namespace Strategy

Namespaces should broadly reflect stable conceptual API rather than every physical folder.

Avoid deeply nested namespaces such as:

```text
LumaFlow.Runtime.Controls.Basic.Text.Internal
```

unless there is a real ambiguity problem.

---

## 50. Public Namespace Goal

Ordinary consumer usage should require a small number of namespaces.

Potential:

```csharp
using LumaFlow;
using LumaFlow.UI;
```

rather than ten feature imports for basic UI.

Exact namespace layout should be validated through consumer code.

---

## 51. Runtime Namespace Must Not Say Runtime Necessarily

Package assembly may be:

```text
LumaFlow.Runtime
```

while public namespace remains:

```text
LumaFlow
```

This is normal and keeps consumer API cleaner.

---

## 52. Editor Namespace

Editor-only APIs should clearly communicate environment.

Example:

```text
LumaFlow.Editor
```

is appropriate.

---

## 53. Assembly Name vs Namespace

Do not force assembly names and namespaces to match exactly.

They serve different concerns:

```text
assembly
=
compilation/dependency boundary

namespace
=
API organization
```

---

## 54. Public API Across Assemblies

If Runtime is later split, consumers should not necessarily need to know which assembly owns:

```text
Widget
Button
Navigator
```

Namespaces and package installation should remain coherent.

Physical modularity should not degrade developer ergonomics.

---

## 55. Internal Accessibility

Prefer:

```csharp
internal
```

for runtime machinery such as:

```text
WidgetNode
BindingScope internals
style resolvers
mount coordinator
```

unless third-party extension use is explicitly designed.

---

## 56. Public API Minimalism

Every public type is a compatibility obligation.

Do not make something public merely because another assembly currently needs it.

Alternative options include:

```text
same assembly
InternalsVisibleTo for framework-owned assembly
narrow public interface
refactoring module boundary
```

---

## 57. InternalsVisibleTo

Framework-owned sibling assemblies may use carefully limited:

```csharp
InternalsVisibleTo
```

when necessary.

This must not become a general mechanism for external plugins.

---

## 58. Third-Party Extension Boundary

Third-party packages should consume documented public LumaFlow APIs.

They must not require:

```text
reflection into internals
InternalsVisibleTo
copying framework source
```

for ordinary custom component development.

---

## 59. Extension API Stability

Low-level third-party extension points should not become stable until ADR-013 readiness gates are met.

Modularization must not force premature publication of runtime internals.

---

## 60. Package Manifest

`package.json` should remain minimal and explicit.

Conceptual fields include:

```text
name
version
displayName
description
unity
author
license information
dependencies
samples
```

according to UPM requirements and chosen distribution strategy.

---

## 61. Semantic Versioning

Public releases should use semantic versioning.

Before 1.0:

```text
breaking API changes are allowed
```

but must be deliberate and documented.

After 1.0:

```text
major
minor
patch
```

semantics should be respected.

---

## 62. Pre-1.0 Versioning

Suggested progression may use:

```text
0.1.x
0.2.x
...
```

based on milestones rather than pretending API stability too early.

Exact release numbering is not mandated here.

---

## 63. Package Lockstep Versioning

While LumaFlow ships as one UPM package, its internal assemblies share the package version.

Do not independently version every assembly.

---

## 64. Future Multi-Package Versioning

If physical packages split later, compatibility between versions must be explicit.

Example:

```text
LumaFlow.Effects 1.3
requires LumaFlow Core >= 1.3 < 2.0
```

This complexity is another reason to defer package splitting.

---

## 65. Source Distribution

LumaFlow should remain source-based through UPM rather than requiring precompiled DLLs as the default.

Benefits include:

```text
Unity compatibility
debugging
source inspection
open-source contribution
platform compilation
```

---

## 66. Precompiled DLLs

Precompiled assemblies may be appropriate for specialized optional tooling in the future.

They are not required for Core.

---

## 67. Open-Source Layout

The Git repository should closely resemble the distributable package where practical.

Avoid a complex transformation pipeline just to publish UPM.

---

## 68. Git UPM Installation

The package should support Git-based UPM installation if repository structure permits.

This is valuable for early adoption.

---

## 69. Scoped Registry

Publishing to a scoped registry may be added later.

It should distribute the same package architecture rather than a separate codebase.

---

## 70. Asset Store Distribution

If LumaFlow is also distributed through Unity Asset Store:

```text
same logical source
same runtime/editor boundaries
same public API
```

should be preserved.

Do not maintain an Asset Store fork.

---

## 71. One Codebase

Distribution targets may differ in packaging metadata but not framework architecture.

Avoid:

```text
GitHub version
Asset Store version
internal version
```

with divergent code.

---

## 72. License

The repository should contain one clear open-source license.

If MIT remains the chosen license, include the standard license file and reference it clearly.

The final license decision must be explicit before public release.

---

## 73. Third-Party Notices

Any bundled third-party code/assets must have:

```text
compatible license
notice/attribution where required
```

Do not casually copy assets into the package.

---

## 74. Fonts

Do not bundle proprietary fonts as framework dependencies.

Theme APIs should accept user-provided/native fonts.

---

## 75. Icons

Core should not depend on a proprietary icon set.

Possible options:

```text
native Unity assets
user-provided icons
separate open-source icon package/sample
```

depending on future design.

---

## 76. Sample Assets

Samples may include appropriately licensed example assets.

Do not make Runtime depend on Samples assets.

---

## 77. Samples Folder

UPM samples should live under:

```text
Samples~/
```

or the appropriate Unity package convention.

They should not automatically compile into consumer projects unless imported.

---

## 78. Documentation Folder

Package documentation may live under:

```text
Documentation~/
```

while top-level:

```text
README.md
```

provides quick adoption guidance.

---

## 79. ADR Location

Architecture documentation may live under something such as:

```text
Documentation~/Architecture/
```

or repository-level:

```text
docs/architecture/
```

depending on repository organization.

Keep one canonical copy.

---

## 80. Do Not Duplicate ADRs

Avoid maintaining:

```text
docs ADR
+
package ADR copy
```

manually.

Duplicate architecture documentation will drift.

---

## 81. Changelog

Public releases should maintain:

```text
CHANGELOG.md
```

with meaningful API and behavioral changes.

Do not treat Git commit history as sufficient consumer documentation.

---

## 82. Migration Notes

Breaking pre-1.0 changes affecting common consumer code should include concise migration guidance.

As 1.0 approaches, migration discipline becomes more important.

---

## 83. Runtime Resources

Avoid hidden dependence on Unity `Resources` folders for Core framework operation where possible.

Explicit references and package-relative loading are easier to reason about.

---

## 84. Required Internal Assets

If LumaFlow eventually ships:

```text
USS
shaders
icons
textures
```

needed by Core, their loading strategy must be deterministic and package-safe.

Do not rely on application AssetDatabase behavior at Runtime.

---

## 85. Core USS Assets

Framework internal USS may be acceptable under ADR-010.

They should be treated as package implementation assets and referenced through a stable package-safe mechanism.

---

## 86. No AssetDatabase Runtime Loading

Runtime assets must not be loaded using:

```text
AssetDatabase
```

This is Editor-only.

---

## 87. Optional Effects Assets

Pipeline-specific shaders/materials belong to optional modules/folders.

Core installation and ordinary UI must remain functional without them being activated.

---

## 88. Shader Inclusion

Optional effects must avoid forcing every LumaFlow user to include unrelated shaders in player builds where practical.

Exact stripping/inclusion strategy belongs to Effects implementation.

---

## 89. Define Constraints

Asmdef `defineConstraints` may be used for optional integration assemblies.

Example conceptual:

```text
LUMAFLOW_URP
```

or package-generated symbols where appropriate.

Do not scatter preprocessor conditionals through Core.

---

## 90. Version Defines

Unity asmdef Version Defines may be useful for optional package presence.

Prefer isolated integration assemblies over:

```csharp
#if URP
```

inside ordinary Core components.

---

## 91. Scripting Defines

Do not require users to manually configure numerous global scripting symbols merely to install Core.

Optional integrations should configure themselves as cleanly as Unity packaging permits.

---

## 92. Assembly Auto References

Asmdef `autoReferenced` behavior should be chosen deliberately.

Core consumer assemblies must be able to reference LumaFlow ergonomically.

Advanced optional modules may choose stricter behavior if needed.

---

## 93. Unsafe Code

Core should not require:

```text
Allow 'unsafe' Code
```

unless a demonstrated performance requirement appears.

Ordinary LumaFlow functionality should remain safe C#.

---

## 94. Engine References

Assemblies should reference only necessary Unity modules.

Do not add broad dependencies merely because they are available.

---

## 95. Testable Dependency Graph

Dependency graph should be simple enough to validate automatically.

Example conceptual future graph:

```text
Unity UI Toolkit
        ↑
LumaFlow.Runtime
        ↑                ↑
LumaFlow.Editor      LumaFlow.Effects
                         ↑       ↑
                       URP      HDRP
```

The exact modules may differ.

The direction is the invariant.

---

## 96. Optional Module Absence Test

CI should verify Core compiles when optional modules/packages are absent.

Examples:

```text
no URP
no HDRP
no Localization
no Addressables
```

according to current integrations.

---

## 97. Optional Module Presence Test

When an integration exists, CI should also test it with the required dependency installed.

This catches conditional assembly mistakes.

---

## 98. Clean Consumer Project

A clean consumer project should be used to validate package structure.

The test should not inherit development-repository-only references.

---

## 99. Package Validation

Release validation should inspect:

```text
package manifest
asmdefs
dependencies
Samples~
Documentation~
license
changelog
```

and perform a clean import/build.

---

## 100. Compile Time

Assembly splits may later improve incremental compile times.

However, excessive small assemblies can also increase complexity.

Measure before splitting.

---

## 101. Internal Compile Isolation

Potential future split candidates should correspond to areas that change independently.

For example, a large optional Editor diagnostics package may justify separation more than tiny styling primitives.

---

## 102. API Cross-Talk

If two proposed modules require extensive access to each other's internals, they probably should not be separate assemblies.

Do not fight the compiler with dozens of internal bridges to preserve an artificial module diagram.

---

## 103. Module Cohesion

A module should group functionality that:

```text
changes together
depends on similar lower layers
forms a coherent consumer capability
```

not merely share a naming prefix.

---

## 104. Dependency Inversion

If a high-level optional subsystem needs to plug into Core:

```text
Core defines minimal abstraction
↑
optional subsystem implements it
```

where appropriate.

Do not make Core discover optional modules through reflection automatically unless necessary.

---

## 105. Explicit Registration

Optional integrations may use explicit setup if required.

Example conceptual:

```csharp
LumaFlowEffects.Configure(...);
```

is preferable to broad reflection-based plugin discovery when only a few integrations exist.

---

## 106. No Runtime Assembly Scanning

Core must not scan all loaded assemblies to discover Widgets/plugins as a foundational mechanism.

This would add:

```text
reflection
startup cost
AOT complexity
hidden behavior
```

without need.

---

## 107. Plugin Ecosystem

Third-party LumaFlow packages can simply reference the public package and expose their own Widgets/components.

No central plugin registry is required.

---

## 108. Third-Party Package Example

Conceptually:

```text
com.vendor.lumaflow.charts
        ↓
depends on LumaFlow public Runtime API
```

It may provide:

```text
Chart
Graph
Timeline
```

without modification to LumaFlow Core.

---

## 109. Third-Party Native Dependencies

If a third-party component package requires another dependency, that dependency belongs to the third-party package.

Core does not absorb it.

---

## 110. Framework Optional Integrations

Official integrations should follow the same rule as third-party packages.

They are consumers/extensions of foundational Runtime.

---

## 111. No Mega-Assembly Forever Requirement

This ADR does not require LumaFlow.Runtime to remain one assembly forever.

It requires:

```text
split only when boundaries are proven
```

and:

```text
keep dependency direction explicit
```

---

## 112. No Micro-Package Requirement

Likewise, modularity does not mean every feature deserves independent installation.

Consumer ergonomics matter.

---

## 113. Core Installation Experience

Installing the primary LumaFlow package should provide a useful framework immediately.

Users should not need to discover and install six packages before they can render:

```text
Text
Button
Row
Column
State<T>
Theme
```

---

## 114. Advanced Feature Installation

Optional heavy integrations may remain opt-in.

Examples:

```text
advanced effects
specific render pipeline integrations
optional localization adapter
```

This keeps the base package lightweight.

---

## 115. Component Availability

Ordinary components documented as Core must not disappear based on unrelated optional packages.

Example:

```text
Button
```

must not require URP merely because one Button theme can use a glow effect.

Optional effects compose around/extend Core behavior.

---

## 116. Graceful Optional Integration

If an optional effect API is referenced only when the module is installed, Core remains unaffected.

Do not expose public Core properties whose types belong to absent optional assemblies.

---

## 117. Public Type Dependency Rule

A public API in assembly A must not expose a type from optional assembly B unless A explicitly depends on B.

This prevents hidden mandatory dependencies.

---

## 118. Theme Optional Type Rule

Core `ThemeData` must not contain fields typed as:

```text
UrpBlurSettings
HdrpGlowSettings
```

Optional modules may provide separate extension theme data.

---

## 119. Navigation Optionality

Navigation may initially remain part of the main Runtime package even though not every user needs it.

It is small and foundational enough that separate installation may create more friction than value.

Reevaluate only after real package growth.

---

## 120. Overlay Optionality

Same principle applies to Overlay.

Logical modularity does not necessarily require physical assembly/package separation.

---

## 121. ListView Optionality

Native list virtualization remains part of normal UI functionality.

No separate collection package is required initially.

---

## 122. Editor Diagnostics Optionality

Heavy Editor diagnostics may become a separate assembly later if:

```text
compile cost
package dependencies
tooling complexity
```

justify it.

Basic Editor integration can remain one assembly initially.

---

## 123. Test Utilities Package

A public `LumaFlow.Testing` package/assembly may eventually help application developers test their own Widgets.

Do not publish one before internal test helpers stabilize.

---

## 124. Public Testing API Gate

Only expose testing helpers that:

```text
work through supported lifecycle
are useful outside LumaFlow repository
can be supported long-term
```

Do not export internal testing hacks.

---

## 125. Package API Boundaries

UPM package folders are not security boundaries.

Architecture must rely on:

```text
asmdefs
C# accessibility
dependency graph
tests
```

rather than folder conventions alone.

---

## 126. Circular Concept Detection

If implementation requires:

```text
Theme knows ButtonNode
ButtonNode knows Theme
```

consider whether:

```text
component theme model
style resolver
```

should mediate the dependency.

Dependency cycles often reveal mixed responsibilities.

---

## 127. Cross-Feature Shared Helpers

Shared helpers should move downward only if they are genuinely generic.

Do not create:

```text
CommonUtils
```

as a dumping ground.

Prefer focused concepts.

---

## 128. Utility Dumping Ground Is Forbidden

Avoid files/namespaces such as:

```text
Utils
Helpers
Misc
Common
```

containing unrelated functionality.

Shared code should have a clear semantic owner.

---

## 129. Module Internal Organization

Inside a feature folder/module, prefer grouping by coherent behavior.

Example:

```text
Navigation/
├── Navigator.cs
├── NavigatorHost.cs
└── Internal/
    ├── NavigationEntry.cs
    └── ...
```

rather than creating generalized abstraction layers before necessary.

---

## 130. Public vs Internal Files

Public API files should be easy to locate.

Runtime machinery may remain under `Internal`.

This improves contribution and review ergonomics.

---

## 131. Package Documentation Structure

Documentation should eventually include:

```text
Getting Started
Core Concepts
State
Layout
Styling
Theme
Components
Lists
Navigation
Overlay
Native Interop
Editor Integration
Architecture
Migration
```

Actual documentation should grow with implemented features.

---

## 132. Do Not Document Unimplemented APIs as Stable

Architecture documents may describe future direction.

Consumer documentation must clearly distinguish:

```text
available
planned
experimental
```

Do not let ADR design be mistaken for shipped API.

---

## 133. Samples Track Implemented Features

Samples should compile against actual public API.

Do not ship mock samples for future components.

---

## 134. Phase-Based Package Growth

Phase 0:

```text
package manifest
Runtime asmdef
Editor asmdef
test asmdefs
basic directories
```

Phase 1:

```text
Widget runtime foundation
```

Later phases add feature directories only as implemented.

---

## 135. Phase 0 Must Stay Small

Do not create dozens of empty folders and assemblies for planned future modules.

The repository structure should reflect real code.

---

## 136. Architecture Documentation May Lead Code

ADRs may define future boundaries ahead of implementation.

Physical source structure should still evolve incrementally.

---

## 137. Package Name Stability

Before first public release, verify package identifiers and product naming for conflicts.

Changing:

```text
displayName
```

is easier than changing a widely adopted UPM package identifier.

Treat final package name as a release decision.

---

## 138. Assembly Name Stability

Assembly names may be referenced by consumers, tests, or reflection.

Choose public assembly names deliberately before 1.0.

Before then, changes are allowed.

---

## 139. Namespace Stability

Namespace changes are highly disruptive.

Avoid repeatedly reorganizing public namespaces once adoption begins.

Use pre-1.0 dogfooding to settle them.

---

## 140. Internal Namespace Freedom

Internal namespaces can evolve more freely.

Do not expose them as supported extension contracts.

---

## 141. Package Dependency Versions

Dependency versions should be as permissive as safely possible while preserving compatibility.

Do not pin unnecessary packages to one exact patch release without reason.

Exact UPM rules depend on the dependency involved.

---

## 142. Unity Minimum Version

`package.json` minimum Unity version should match the actually tested compatibility floor from `COMPATIBILITY.md` and ADR-019.

Do not publish an aspirational minimum.

---

## 143. Upgrade Testing

Before increasing minimum Unity version or dependency requirements:

```text
document why
update compatibility policy
test migration
```

This is a meaningful consumer-impacting decision.

---

## 144. Deprecation

If a module/API is superseded, use normal deprecation rather than leaving two permanent competing implementations.

Architectural clarity matters.

---

## 145. Experimental Modules

New optional integrations may initially be labeled experimental.

This allows iteration without implying the same stability as Core.

---

## 146. Stable Core vs Experimental Edge

Long-term LumaFlow should have:

```text
small stable foundation
+
more rapidly evolving optional edges
```

This reduces risk for consumers.

---

## 147. Release Scope

A release does not need to update every optional module unless they are physically versioned together and affected.

While one package is used, all included modules still share one release version.

---

## 148. CI Dependency Validation

CI should eventually validate:

```text
no Runtime → Editor
no Runtime → URP/HDRP
no cycles
optional integrations compile only with required deps
```

This follows ADR-019.

---

## 149. CI Package Validation

CI/release should verify package installation into a clean project.

Local development success alone is insufficient.

---

## 150. CI Samples Validation

Important samples should compile.

They serve as public API smoke tests.

---

## 151. CI Documentation Fixture Validation

Selected documentation code examples should compile where practical.

---

## 152. Distribution Artifact Consistency

Git/registry/Asset Store versions of the same release should contain equivalent functional source.

Do not patch one distribution manually after release.

Fix source and republish.

---

## 153. Generated Files

Avoid committing generated package artifacts unless required by Unity tooling/distribution.

Source of truth should remain clear.

---

## 154. Code Generation Modules

If optional source generation appears later, generated output should not become required source-of-truth for Core architecture.

The package must define whether generation occurs:

```text
at development time
at import time
at compile time
```

through a separate architectural decision.

---

## 155. Assembly Reload Cost

Editor compile/reload time should be measured as the project grows.

This may become a legitimate reason to split large Editor tooling from Runtime.

---

## 156. Dependency Change Review

Adding an assembly/package dependency is an architectural change worth deliberate review.

Before adding it, document:

```text
why needed
whether optional
which layer owns it
whether it leaks into public API
```

Not every dependency requires a new ADR, but it requires thought.

---

## 157. Codex Repository Rule

Codex must inspect existing:

```text
package.json
asmdefs
folder structure
dependency graph
```

before creating a new assembly or module.

Do not invent module boundaries from scratch inside an isolated task.

---

## 158. Codex Assembly Rule

Codex must not create a new asmdef merely because new files belong to a new conceptual feature.

First ask whether compilation/dependency isolation is actually required.

---

## 159. Codex Dependency Rule

When adding a dependency:

```text
prefer the lowest specialized layer that needs it
```

Never add an optional dependency to Runtime simply because a higher-level feature uses it.

---

## 160. Codex Internal API Rule

Do not make runtime internals public merely to cross an avoidable assembly boundary.

Reconsider whether the assembly split is justified.

---

## 161. Codex Optional Integration Rule

Optional integrations must compile away cleanly when their dependency is absent.

Do not leave unresolved types in Core.

---

## 162. Codex Folder Rule

Do not create empty speculative module folders.

Create structure as implementation arrives.

---

## 163. Codex Package Rule

Consumer-facing samples and documentation must not reference internal namespaces.

---

## 164. Codex Dependency Cycle Rule

If a new feature creates a dependency cycle:

```text
stop
```

and redesign ownership.

Do not solve the cycle using:

```text
service locator
reflection
static registry
```

without a separate justified architectural decision.

---

## 165. Rejected Alternative: One Giant Assembly Forever

Rejected as a permanent rule.

As LumaFlow grows, real optional dependencies or compile-time boundaries may justify splits.

---

## 166. Rejected Alternative: Assembly Per Feature Immediately

Rejected.

Premature splitting creates artificial public/internal boundaries and slows iteration.

---

## 167. Rejected Alternative: Package Per Feature Immediately

Rejected because it dramatically increases adoption and release complexity.

---

## 168. Rejected Alternative: Runtime Depends on Optional Effects

Rejected under ADR-006.

Optional visual features must remain above Core.

---

## 169. Rejected Alternative: Core Depends on Editor Utilities

Rejected under ADR-017.

---

## 170. Rejected Alternative: Install URP/HDRP Automatically

Rejected.

Core LumaFlow is pipeline-independent.

---

## 171. Rejected Alternative: Mandatory Third-Party DI

Rejected.

LumaFlow must remain application-architecture neutral.

---

## 172. Rejected Alternative: Mandatory Third-Party Reactive Library

Rejected.

Core owns its minimal reactive primitive.

---

## 173. Rejected Alternative: Runtime Reflection Plugin Discovery

Rejected as foundational modularity architecture.

Explicit typed references are simpler and more AOT-friendly.

---

## 174. Rejected Alternative: Separate Asset Store Fork

Rejected.

One source architecture should serve all distribution channels.

---

## 175. Rejected Alternative: Publicize All Internals for Modules

Rejected.

Physical assembly modularity must not dictate bad public API design.

---

## 176. Architectural Invariants Created by This ADR

Unless superseded:

### Invariant 1

LumaFlow is UPM-first.

### Invariant 2

Initial distribution uses one primary package.

### Invariant 3

Initial assembly graph remains small.

### Invariant 4

Assembly dependencies form a DAG.

### Invariant 5

Specialized modules depend on foundational modules.

### Invariant 6

Runtime never depends on Editor.

### Invariant 7

Core never depends on URP/HDRP integrations.

### Invariant 8

Optional package dependencies remain isolated.

### Invariant 9

Assembly/package splitting requires demonstrated value.

### Invariant 10

Public API design must not be degraded merely to satisfy an artificial module boundary.

---

## 177. Decision Test

When considering a new module:

```text
Does this require an optional dependency?
        ↓ yes
Isolation may justify an assembly.

Does it need independent compilation?
        ↓ yes
Consider an assembly.

Does it need independent installation/versioning?
        ↓ yes
Consider a package.

Is the only reason that the folder is large?
        ↓ yes
Do not split yet.

Would the split require exposing many internals?
        ↓ yes
The boundary may be wrong.

Would Core need to depend upward on the new module?
        ↓ yes
Redesign.
```

---

## 178. Initial Repository Target

A practical Phase 0 structure is approximately:

```text
Packages/
└── com.sahland.lumaflow/
    ├── package.json
    ├── README.md
    ├── CHANGELOG.md
    ├── LICENSE
    │
    ├── Runtime/
    │   ├── LumaFlow.Runtime.asmdef
    │   └── ...
    │
    ├── Editor/
    │   ├── LumaFlow.Editor.asmdef
    │   └── ...
    │
    ├── Tests/
    │   ├── Runtime/
    │   │   └── LumaFlow.Runtime.Tests.asmdef
    │   │
    │   └── Editor/
    │       └── LumaFlow.Editor.Tests.asmdef
    │
    ├── Samples~/
    └── Documentation~/
```

The actual Git repository may use another root arrangement.

The package-internal structure is the important part.

---

## 179. Initial Runtime Target

Do not create all future feature directories immediately.

The earliest Runtime implementation may begin with:

```text
Runtime/
├── Core/
├── State/
└── LumaFlow.Runtime.asmdef
```

Then add:

```text
Layout
Styling
Theme
Controls
```

as implementation progresses.

---

## 180. Initial Dependency Graph

Phase 0:

```text
UnityEngine / UI Toolkit
        ↑
LumaFlow.Runtime
        ↑
LumaFlow.Editor

LumaFlow.Runtime
        ↑
LumaFlow.Runtime.Tests

LumaFlow.Editor
        ↑
LumaFlow.Editor.Tests
```

This is intentionally simple.

---

## 181. Future Dependency Graph

If optional modules eventually justify separation:

```text
                         LumaFlow.Editor
                               │
                               ▼
                        LumaFlow.Runtime
                         ▲      ▲      ▲
                         │      │      │
                Localization  Effects  Other Integrations
                              ▲    ▲
                              │    │
                             URP  HDRP
```

Exact assembly names are illustrative.

Dependencies must still point toward the foundation.

---

## 182. 1.0 Package Goal

By 1.0, package architecture should provide:

```text
clean UPM installation
minimal mandatory dependencies
stable Runtime/Editor separation
documented optional modules
clean public namespace surface
tested player builds
tested Editor integration
clear license
samples
documentation
```

A large number of assemblies is not a success metric.

---

## 183. Reconsideration Conditions

Revisit this ADR if:

1. Runtime compile time becomes materially problematic;
2. Effects require physical package separation;
3. Editor tooling acquires heavy optional dependencies;
4. third-party extensions require a new stable abstraction assembly;
5. distribution channels require separate package artifacts;
6. a module develops a genuinely independent release lifecycle.

Any split must preserve the one-way dependency principle.

---

## 184. Final Decision

LumaFlow's package architecture begins simple:

```text
one UPM package
+
small asmdef graph
+
strict Runtime/Editor separation
+
optional integrations above Core
```

and becomes more modular only when real implementation pressure proves the need.

The dependency rule is:

```text
specialized
    ↓
general
    ↓
foundation
```

never:

```text
foundation
    ↓
optional specialization
```

The guiding principle is:

**Modularity exists to isolate real dependencies and responsibilities, not to maximize the number of packages or assemblies.**
