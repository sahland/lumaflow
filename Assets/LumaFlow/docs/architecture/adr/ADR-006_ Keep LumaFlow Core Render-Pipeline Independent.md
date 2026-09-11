# ADR-006: Keep LumaFlow Core Render-Pipeline Independent

- **Status:** Accepted
- **Decision date:** 2026-08-11
- **Scope:** Rendering compatibility, package dependencies, optional effects
- **Affects:** Runtime, Editor, Styling, Effects, Package Structure, CI
- **Related documents:** `COMPATIBILITY.md`, `ARCHITECTURE.md`, `ADR-001-native-uitoolkit.md`

---

## 1. Context

Unity projects may use one of several render pipelines:

```text
Built-in Render Pipeline
Universal Render Pipeline (URP)
High Definition Render Pipeline (HDRP)
```

LumaFlow is built on top of Unity UI Toolkit.

Ordinary UI Toolkit functionality does not require LumaFlow to depend directly on a specific render pipeline.

However, future visual features may create pressure to introduce:

- custom shaders;
- blur;
- glassmorphism;
- glow;
- custom passes;
- renderer features;
- render textures;
- post-processing integration.

If those capabilities are added carelessly, the entire framework could become dependent on URP or HDRP.

That would violate LumaFlow's compatibility goals.

---

## 2. Decision

LumaFlow Core will remain **render-pipeline independent**.

Core functionality must support:

```text
Built-in Render Pipeline
URP
HDRP
```

without requiring pipeline-specific package references.

The architecture is:

```text
LumaFlow Core
      ↓
Unity UI Toolkit
      ↓
Unity
```

not:

```text
LumaFlow Core
      ↓
URP / HDRP
      ↓
Unity
```

---

## 3. Core Definition

For the purpose of this ADR, Core includes the framework functionality required for ordinary LumaFlow usage.

Examples:

```text
Widget
WidgetNode
BuildContext
State<T>
Bindings
ThemeData

Text
Button
TextField
Toggle
Slider

Row
Column
Padding
Container
ScrollView
ListView

Navigation
Overlays
```

These features must not depend on a render pipeline.

---

## 4. Forbidden Core References

Core assemblies must not directly reference:

```text
UnityEngine.Rendering.Universal
UnityEngine.Rendering.HighDefinition
```

or pipeline package assemblies required only by URP/HDRP.

Example of forbidden Core code:

```csharp
using UnityEngine.Rendering.Universal;
```

or:

```csharp
using UnityEngine.Rendering.HighDefinition;
```

unless that file belongs to a deliberately isolated optional integration assembly.

---

## 5. Built-in Render Pipeline Requirement

A Unity project using only the Built-in Render Pipeline must be able to:

- install LumaFlow;
- compile the package;
- use Runtime widgets;
- use Editor widgets;
- use themes;
- use state;
- build a player.

It must not need URP or HDRP installed.

---

## 6. URP Requirement

A URP project must be able to use normal LumaFlow UI without:

- adding a Renderer Feature;
- changing its Renderer Asset;
- changing camera configuration;
- changing Pipeline Asset settings;
- enabling post-processing.

Ordinary UI must work as ordinary UI Toolkit UI.

---

## 7. HDRP Requirement

A HDRP project must be able to use normal LumaFlow UI without:

- adding Custom Passes;
- changing HDRP frame settings;
- enabling HDR;
- changing camera setup;
- configuring post-processing.

Pipeline-specific configuration must only be required for optional effects that explicitly need it.

---

## 8. Core Visual Features

Core styling should prefer UI Toolkit-native capabilities.

Examples:

```text
background colors
borders
radius
opacity
text styling
layout
transitions supported natively
images
vector images
clipping supported by UI Toolkit
```

If UI Toolkit can express a visual effect sufficiently, do not introduce pipeline-specific rendering.

---

## 9. Optional Effects

Advanced effects may be added later.

Examples:

```text
BackdropBlur
GlassSurface
GlowSurface
AdvancedShadow
CustomMask
ShaderSurface
```

These must not alter Core dependency requirements.

---

## 10. Effects Architecture

Preferred long-term architecture:

```text
LumaFlow Runtime
        ↑
LumaFlow Effects API
      ↙     ↓      ↘
Built-in   URP    HDRP
```

Or:

```text
LumaFlow.Effects
├── Core contracts
├── BuiltIn implementation
├── URP integration
└── HDRP integration
```

Exact package structure may evolve.

The isolation requirement does not.

---

## 11. Optional Assembly Structure

Possible assemblies:

```text
LumaFlow.Runtime
LumaFlow.Effects
LumaFlow.Effects.URP
LumaFlow.Effects.HDRP
```

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

## 12. Optional Means Optional

If an optional integration assembly is absent:

```text
LumaFlow Core must still compile.
```

Removing URP from a project must not cause Core compilation errors.

Removing HDRP must not cause Core compilation errors.

---

## 13. Assembly Definition Constraints

Pipeline-specific assemblies should use appropriate asmdef references and optional package constraints.

Example conceptual behavior:

```text
URP installed
    ↓
LumaFlow.Effects.URP available

URP not installed
    ↓
assembly excluded / unavailable
    ↓
Core unaffected
```

Do not solve optional dependencies with broad `#if` blocks spread through Core.

---

## 14. Conditional Compilation

Pipeline-specific conditional compilation is allowed inside dedicated integration code.

Example:

```csharp
#if LUMAFLOW_URP
...
#endif
```

or Unity/package version constraints.

However, pipeline defines should not dominate the Core codebase.

Bad:

```csharp
#if URP
...
#elif HDRP
...
#else
...
#endif
```

inside ordinary Button, Card, or Container implementations.

---

## 15. Abstraction Boundary

If an effect has multiple pipeline implementations, Core-facing code should depend on a pipeline-neutral contract.

Conceptually:

```csharp
internal interface IBackdropBlurBackend
{
    bool IsSupported { get; }

    void Attach(VisualElement element, BlurSettings settings);
    void Detach(VisualElement element);
}
```

The exact implementation may differ.

The important rule is:

```text
consumer-facing effect
        ↓
pipeline-neutral abstraction
        ↓
pipeline-specific implementation
```

---

## 16. No Pipeline Types in Core Public API

Core public APIs must not expose pipeline-specific types.

Bad:

```csharp
GlassSurface(
    ScriptableRendererFeature rendererFeature
)
```

inside Core.

Bad:

```csharp
Card(
    HDRenderPipelineAsset hdrp
)
```

Pipeline integration types belong to optional modules.

---

## 17. Pipeline-Neutral Public Effects API

A future effect should preferably look like:

```csharp
BackdropBlur(
    intensity: 0.65f,
    child: content
)
```

rather than exposing implementation details.

The backend determines whether the requested effect can be supported.

---

## 18. Graceful Degradation

Optional visual effects should degrade gracefully when possible.

Example:

```text
Backdrop blur unavailable
        ↓
use translucent themed surface
```

rather than:

```text
framework throws and entire UI fails
```

This applies only to optional visual enhancement.

Core semantic behavior must remain correct.

---

## 19. Degradation Semantics

Graceful degradation must preserve:

- layout;
- input;
- text;
- control usability;
- hierarchy;
- accessibility behavior.

Only the unavailable visual enhancement may differ.

---

## 20. Unsupported Effect Behavior

If an effect cannot degrade meaningfully, it must fail clearly.

Prefer:

```text
LumaFlow Effects: BackdropBlur is not supported by the active rendering backend.
```

over:

```text
NullReferenceException
```

or silent broken visuals.

---

## 21. Effects Capability Query

A future effects layer may expose capability information.

Conceptually:

```csharp
context.Effects.Supports(EffectCapability.BackdropBlur)
```

or:

```csharp
BackdropBlur.IsSupported(context)
```

Exact API is deferred.

This allows user code to choose alternatives intentionally.

---

## 22. No Active Pipeline Detection in Ordinary Widgets

Ordinary widgets should not query:

```text
GraphicsSettings.currentRenderPipeline
```

or equivalent just to decide how they work.

Button behavior should be the same regardless of pipeline.

Pipeline detection belongs in optional rendering integrations.

---

## 23. Shader Policy

Custom shaders are allowed only when a feature clearly requires them.

Before adding a shader:

1. determine whether UI Toolkit-native styling can solve the problem;
2. determine whether one pipeline-independent shader can work;
3. only then consider per-pipeline variants.

---

## 24. Shader Variants

If shader variants are required:

```text
LumaFlow Effects
├── BuiltIn shader
├── URP shader
└── HDRP shader
```

The correct backend should be selected by the integration layer.

Ordinary consumer API should remain the same.

---

## 25. Shader Naming

Pipeline-specific shaders should be clearly isolated and named.

Example:

```text
LumaFlow/Effects/BuiltIn/BackdropBlur
LumaFlow/Effects/URP/BackdropBlur
LumaFlow/Effects/HDRP/BackdropBlur
```

Avoid generic shader names with hidden pipeline assumptions.

---

## 26. Material Ownership

If effects create materials dynamically, ownership and cleanup must be explicit.

Do not leak:

- Material instances;
- RenderTextures;
- command buffers;
- render pass registrations.

Effect lifecycle must follow LumaFlow mount/unmount semantics where possible.

---

## 27. RenderTexture Usage

RenderTextures should not be introduced for ordinary Core UI.

If an optional effect requires them:

- reuse where practical;
- clean them deterministically;
- avoid creating one every frame;
- document memory cost.

---

## 28. Camera Dependencies

Core must not depend on a specific Camera.

Optional effects should also avoid assuming:

```text
Camera.main
```

as a universal architecture.

If camera access is needed, it should be configurable or scoped.

---

## 29. World-Space UI

If LumaFlow later supports specialized world-space effects, that work must not alter ordinary screen-space Core compatibility.

World-space integrations should be separate concerns.

---

## 30. Post-Processing

LumaFlow Core must not require post-processing.

Optional effects may integrate with pipeline-specific systems, but such integration must remain optional.

---

## 31. Bloom-Like Effects

A component called:

```text
Glow
```

must not assume project Bloom is enabled.

If the visual result depends on Bloom, documentation must say so or the effect must provide a non-Bloom fallback.

---

## 32. HDR

LumaFlow Core must not require HDR output.

Theme colors should remain valid in standard SDR UI contexts.

HDR-specific features belong to optional integrations.

---

## 33. Color Space

Pipeline independence also means not assuming one color space.

Effects should be tested under:

```text
Gamma
Linear
```

where relevant.

Ordinary theme/UI behavior should use Unity's normal color handling.

---

## 34. Editor UI

Editor UI must remain render-pipeline independent.

An AudioLib EditorWindow should work regardless of what render pipeline the current project uses.

This is particularly important because Editor tooling is often unrelated to game rendering.

---

## 35. Runtime UI

Runtime UI should also remain independent unless the user explicitly uses an optional visual effect.

Example:

```text
Main Menu
Settings
Inventory
HUD
```

should not care whether the game is Built-in, URP, or HDRP.

---

## 36. Package Installation

Installing:

```text
com.sahland.lumaflow
```

must not cause Unity Package Manager to install URP or HDRP.

This is a strict rule.

---

## 37. Effects Package Installation

If future effects are distributed separately:

```text
com.lumaflow.effects.urp
```

may depend on URP.

Likewise:

```text
com.lumaflow.effects.hdrp
```

may depend on HDRP.

That dependency is acceptable because the package is explicitly pipeline-specific.

---

## 38. Single Package Alternative

If all modules remain inside one package, optional pipeline integrations must still be isolated by assemblies and package version constraints.

The physical package layout does not remove the architectural boundary.

---

## 39. CI Validation

CI should eventually validate:

```text
Built-in project
URP project
HDRP project
```

At minimum:

- package imports;
- Core compiles;
- Runtime tests compile;
- Editor tests compile;
- simple sample mounts.

---

## 40. Forbidden Reference CI Check

CI should eventually fail if `LumaFlow.Runtime` gains accidental references to:

```text
Unity.RenderPipelines.Universal
Unity.RenderPipelines.HighDefinition
```

This is a high-value automated invariant.

---

## 41. Built-in Smoke Test

A minimal Built-in project should be able to display:

```text
Text
Button
TextField
Row
Column
Theme
State<T>
```

with no SRP package installed.

---

## 42. URP Smoke Test

A minimal URP project should display the same Core sample with no special LumaFlow setup.

---

## 43. HDRP Smoke Test

A minimal HDRP project should display the same Core sample with no special LumaFlow setup.

---

## 44. Visual Consistency

Core widgets should be semantically and visually consistent across render pipelines within normal UI Toolkit differences.

LumaFlow must not ship radically different default Button behavior depending on pipeline.

---

## 45. Optional Visual Differences

Optional effects may naturally differ between pipelines.

For example:

```text
Blur quality
shadow quality
HDR glow appearance
```

may vary.

These differences should be documented where meaningful.

---

## 46. API Consistency Across Pipelines

Consumer code should ideally remain the same.

Good:

```csharp
BackdropBlur(
    intensity: 0.5f,
    child: panel
)
```

across supported backends.

Avoid:

```csharp
UrpBackdropBlur(...)
HdrpBackdropBlur(...)
BuiltInBackdropBlur(...)
```

in normal application code unless the user explicitly needs backend-specific control.

---

## 47. Backend Registration

A future effects system may register the correct backend at initialization.

This registration must not become global mutable state shared incorrectly across multiple contexts if that would create problems.

Keep backend resolution predictable.

---

## 48. Automatic Backend Detection

Automatic backend detection is acceptable inside optional effects infrastructure if:

- it is reliable;
- it does not pollute Core;
- users can override behavior when needed.

---

## 49. Pipeline Changes During Editor Session

Unity projects may switch render pipeline assets in the Editor.

Optional effect infrastructure should avoid caching assumptions permanently if the active pipeline can change.

Core is unaffected because it remains independent.

---

## 50. No Feature Creep Into Core

A feature should not be moved into Core merely because it looks visually important.

Example:

```text
BackdropBlur
```

does not become Core-required just because modern UI often uses glassmorphism.

Core is defined by essential UI architecture, not visual trend popularity.

---

## 51. Semantic Core vs Visual Extras

Core responsibilities:

```text
layout
controls
state
theme
input
navigation
composition
```

Optional visual extras:

```text
blur
glow
special shaders
camera-based effects
```

This distinction should remain clear.

---

## 52. Theme Compatibility

`ThemeData` must remain pipeline-independent.

Forbidden Core theme properties:

```text
UrpRendererFeature
HdrpCustomPass
HDRenderPipelineAsset
UniversalRenderPipelineAsset
```

Theme may define semantic visual intent:

```text
GlassOpacity
GlowStrength
```

only if the effect system can interpret or ignore it safely without making Core dependent on a pipeline.

---

## 53. Pipeline-Specific Theme Extensions

If pipeline-specific effect configuration becomes necessary, it should live in optional extensions.

Conceptually:

```text
UrpEffectTheme
HdrpEffectTheme
```

not in Core `ThemeData`.

---

## 54. No Compile-Time Breakage From Missing Pipeline

This scenario is forbidden:

```text
Install LumaFlow in Built-in project
        ↓
compiler error:
Universal namespace not found
```

This is considered a release-blocking bug.

---

## 55. No Runtime Breakage From Missing Pipeline

Also forbidden:

```text
Core Button mounted
        ↓
runtime checks for URP
        ↓
throws because none installed
```

Ordinary Core widgets must not care.

---

## 56. No Hidden Pipeline Requirement in Samples

Core samples must not silently require a URP/HDRP scene.

If a sample is pipeline-specific, its name and documentation must say so.

Examples:

```text
Effects_URP
Effects_HDRP
```

---

## 57. Documentation

Public docs must distinguish:

```text
Core — works on Built-in / URP / HDRP

Optional Effects — may require specific integration
```

Do not make users inspect asmdefs to understand requirements.

---

## 58. Asset Store Packaging

If distributed through Unity Asset Store, Core should remain usable immediately after import regardless of render pipeline.

Do not bundle mandatory pipeline setup steps for ordinary use.

---

## 59. Git/UPM Packaging

UPM installation should have the same behavior.

The core package should not transitively install an SRP simply because LumaFlow supports visual effects.

---

## 60. Rejected Alternative: URP as the Minimum Requirement

Rejected:

```text
LumaFlow requires URP
```

Reason:

- excludes Built-in projects;
- excludes some Editor-only tooling use cases;
- adds unnecessary package dependency;
- ordinary UI Toolkit does not require it.

---

## 61. Rejected Alternative: Separate Entire Framework Per Pipeline

Rejected:

```text
LumaFlow BuiltIn
LumaFlow URP
LumaFlow HDRP
```

as separate versions of the full framework.

Reason:

Most functionality is identical.

This would create:

- code duplication;
- API drift;
- maintenance overhead;
- fragmented community.

Only specialized effects should vary.

---

## 62. Rejected Alternative: `#if` Everywhere

Rejected architecture:

```csharp
#if URP
...
#elif HDRP
...
#else
...
#endif
```

throughout Core widgets.

Reason:

- poor maintainability;
- hidden complexity;
- hard testing;
- easy regressions.

Use module boundaries instead.

---

## 63. Rejected Alternative: Ignore Built-in Pipeline

Rejected unless Unity itself removes practical support in a future environment and LumaFlow intentionally revises its compatibility policy.

As long as Built-in remains part of the declared compatibility matrix, Core must support it.

---

## 64. Rejected Alternative: Lowest-Common-Denominator Core Only

Pipeline independence does not mean LumaFlow can never offer advanced visuals.

Instead:

```text
Core remains portable
+
optional modules unlock richer visuals
```

This provides both compatibility and capability.

---

## 65. Architectural Invariants Created by This ADR

Unless superseded:

### Invariant 1

LumaFlow Core supports Built-in, URP, and HDRP.

### Invariant 2

Core does not reference URP assemblies.

### Invariant 3

Core does not reference HDRP assemblies.

### Invariant 4

Installing Core does not install URP or HDRP.

### Invariant 5

Pipeline-specific effects are isolated.

### Invariant 6

Ordinary widgets never require pipeline setup.

### Invariant 7

Core ThemeData contains no pipeline-specific types.

### Invariant 8

Missing optional effect integrations do not break Core.

### Invariant 9

Pipeline-specific shaders do not become Core requirements.

### Invariant 10

Render pipeline compatibility is a release requirement.

---

## 66. Codex Rules

### Rule 1

Do not add URP/HDRP references to `LumaFlow.Runtime`.

### Rule 2

When implementing a visual feature, first determine whether UI Toolkit-native styling can provide it.

### Rule 3

If pipeline-specific APIs are required, isolate them in an optional integration assembly.

### Rule 4

Do not expose pipeline types in Core public APIs.

### Rule 5

Do not make optional effects required dependencies of ordinary components.

### Rule 6

Provide graceful degradation where practical.

### Rule 7

Do not assume `Camera.main`, a specific Renderer, or post-processing in Core.

### Rule 8

Do not scatter pipeline `#if` blocks through normal components.

### Rule 9

Update compatibility tests when adding rendering integrations.

### Rule 10

Treat accidental Core pipeline coupling as an architectural regression.

---

## 67. Decision Test

When implementing a visual capability, ask:

```text
Can UI Toolkit do it natively?
        ↓ yes
Use UI Toolkit.

Does it require rendering integration?
        ↓ yes
Is it essential to normal UI semantics?
        ↓ no
Move it to optional Effects.

Does one implementation work across pipelines?
        ↓ yes
Use pipeline-neutral implementation.

Does it require URP/HDRP-specific APIs?
        ↓ yes
Create isolated backend.
```

---

## 68. Example: Standard Card

This belongs entirely in Core:

```csharp
Card(
    child: content
)
```

because its normal appearance can use:

```text
background
border
radius
padding
```

through UI Toolkit.

---

## 69. Example: Glass Card

Potential future:

```csharp
GlassSurface(
    blur: 18,
    opacity: 0.7f,
    child: content
)
```

This may belong in `LumaFlow.Effects`.

Possible backend behavior:

```text
URP
→ real blur implementation

HDRP
→ HDRP-compatible implementation

Built-in
→ compatible implementation or translucent fallback
```

Core remains unchanged.

---

## 70. Example: Glow Button

A `ButtonVariant.Primary` should not require Bloom.

If an optional `Glow` wrapper is used:

```csharp
Glow(
    intensity: 0.8f,
    child: Button(...)
)
```

then the effect layer owns pipeline concerns.

The Button itself remains pipeline-neutral.

---

## 71. Reconsideration Conditions

Revisit this ADR only if:

1. Unity changes UI Toolkit so ordinary UI itself becomes pipeline-specific;
2. Built-in Render Pipeline becomes technically impossible to support within the declared Unity versions;
3. advanced rendering becomes a fundamental product requirement rather than an optional layer;
4. maintaining three pipeline paths becomes prohibitively costly;
5. Unity introduces a new unified rendering abstraction that changes the architecture.

Any change requires an explicit superseding ADR.

---

## 72. Final Decision

LumaFlow Core will remain:

```text
render-pipeline independent
```

and will support:

```text
Built-in
URP
HDRP
```

for ordinary UI.

Advanced visuals may use optional isolated integrations.

The framework must never reach a state where adding a beautiful effect accidentally changes:

```text
LumaFlow
```

from:

```text
UI Toolkit framework
```

into:

```text
URP-only UI framework
```

Pipeline independence is a permanent architectural boundary unless deliberately superseded.
