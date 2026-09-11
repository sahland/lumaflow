# LumaFlow Coding Guidelines

This document defines coding conventions for the LumaFlow codebase.

Architecture is governed by the architecture documents and ADRs.

These guidelines define how accepted architecture should be expressed in code.

---

# 1. Priorities

When implementation choices conflict, prioritize:

1. correctness;
2. lifecycle safety;
3. API clarity;
4. native UI Toolkit interoperability;
5. maintainability;
6. performance based on evidence;
7. conciseness.

Do not sacrifice architectural correctness for shorter code.

---

# 2. Language

LumaFlow is written in C#.

Use the C# language version supported by the minimum Unity version declared by the project.

Do not introduce syntax unavailable in the supported compatibility range.

---

# 3. Nullable Reference Types

Use nullable reference type annotations where supported by the project configuration.

Represent optional values explicitly.

Prefer:

```csharp
Action? onPressed
```

over undocumented null acceptance.

---

# 4. Naming

Use standard C# naming.

```text
Types                PascalCase
Methods              PascalCase
Properties           PascalCase
Public fields         PascalCase
Parameters            camelCase
Local variables       camelCase
Private fields        _camelCase
Constants             PascalCase
```

Avoid Hungarian notation.

---

# 5. Public Widget Naming

Public Widget types should use concise semantic nouns:

```text
Text
Button
Card
Row
Column
TextField
ListView<T>
```

Avoid unnecessary suffixes such as:

```text
ButtonWidget
TextWidget
ColumnWidget
```

unless ambiguity makes them necessary.

---

# 6. Runtime Node Naming

Internal mounted runtime types should use `Node`.

Examples:

```text
TextNode
ButtonNode
ColumnNode
ThemeNode
```

This visually separates declarative descriptions from runtime instances.

---

# 7. Interface Naming

Use normal C# `I` prefix:

```text
IDiagnosticSink
IDisposable
```

Do not create interfaces for every class by default.

An interface should represent a real substitution or abstraction boundary.

---

# 8. Namespace Design

Public namespaces should remain shallow and discoverable.

Prefer:

```text
LumaFlow
LumaFlow.UI
LumaFlow.Editor
```

over deeply nested namespaces mirroring every folder.

Physical folder structure and namespace structure do not need to match exactly.

---

# 9. Runtime / Editor Separation

Runtime code must never reference:

```text
UnityEditor
UnityEditor.UIElements
LumaFlow.Editor
```

If code requires UnityEditor, move it into the Editor assembly.

Do not use `#if UNITY_EDITOR` to hide substantial Editor behavior inside Runtime.

---

# 10. Public vs Internal

Default implementation detail visibility should be:

```csharp
internal
```

Make a type public only when it belongs to supported consumer API.

Public API is a compatibility commitment.

---

# 11. Runtime Internals

Types such as:

```text
WidgetNode
BindingScope
style resolvers
mount coordinator
navigation entries
overlay entries
```

should remain internal unless a dedicated extension API requires otherwise.

---

# 12. Widget Configuration

Widget descriptions should preferably be immutable after construction.

Use:

```text
constructor arguments
init-only properties
readonly values
```

where appropriate.

Avoid ordinary mutable setters for declarative configuration.

---

# 13. Mounted State

Never store mounted runtime state directly on ordinary reusable Widget descriptions.

Forbidden examples:

```text
VisualElement _element;
WidgetNode _node;
BuildContext _context;
bool _isMounted;
```

These belong to runtime nodes.

---

# 14. Static Mutable State

Avoid static mutable framework state.

Forbidden unless explicitly justified:

```csharp
static BuildContext Current;
static Navigator Current;
static ThemeData CurrentTheme;
```

Multiple mounts/windows must remain isolated.

---

# 15. Static Immutable Values

Static immutable defaults are acceptable.

Examples:

```text
EdgeInsets.Zero
BorderRadius.Zero
ThemeData default constants
```

provided they are safe to share.

---

# 16. Constructor Design

Required primary values may be positional.

Example:

```csharp
Text("Settings")
Button("Save")
```

Use named parameters for optional values.

Avoid constructors with many positional parameters.

---

# 17. Boolean Parameters

Avoid boolean explosion.

Bad:

```csharp
Button(
    primary: false,
    danger: true,
    outlined: false,
    compact: true
)
```

Prefer:

```csharp
Button(
    variant: ButtonVariant.Danger,
    size: ButtonSize.Compact
)
```

---

# 18. Collections

Use `child` for one generic child.

Use `children` for multiple generic children.

Use semantic slot names only when they represent actual semantic roles:

```text
title
content
actions
leading
trailing
```

---

# 19. Null Children

Do not silently ignore arbitrary null values in child collections unless the API explicitly supports them.

Invalid child configuration should fail clearly.

---

# 20. Build Methods

`Build(BuildContext context)` should be side-effect-light.

Do not perform:

```text
network requests
file writes
analytics
scene changes
permanent subscription registration
```

inside Build.

Build may construct Widget descriptions and read contextual data.

---

# 21. Build Reentrancy

Never assume Build executes exactly once.

Code inside Build must remain safe if invoked again.

---

# 22. Subscription Registration

Every subscription must have an explicit owner.

Preferred:

```text
WidgetNode
↓
BindingScope
↓
subscription
```

Never create an event or State subscription without knowing how it is removed.

---

# 23. Event Registration

Native callbacks registered during mount must be unregistered during unmount.

Prefer lifecycle helpers that make this ownership obvious.

---

# 24. `IDisposable`

Use `IDisposable` for deterministic ownership/lifetime handles where appropriate.

Examples:

```text
MountHandle
State subscription
OverlayHandle
BindingScope
```

Dispose methods should be idempotent where practical.

---

# 25. Cleanup

Cleanup code should attempt to release all owned resources even when one cleanup action fails, where doing so is safe.

Do not leave later subscriptions active merely because an earlier disposal threw.

---

# 26. Exceptions

Validate invalid public configuration early.

Use standard exceptions for standard argument failures when appropriate.

Use LumaFlow-specific exceptions for meaningful framework lifecycle/invariant errors.

Do not wrap every exception unnecessarily.

---

# 27. Error Messages

Framework errors should explain:

```text
what failed
what invariant was violated
which Widget/Node was involved
how to fix it
```

where available.

Avoid vague errors.

---

# 28. Logging

Normal framework operation should be quiet.

Do not log successful mounts, bindings, or updates by default.

Warnings should be actionable and deduplicated where necessary.

---

# 29. Comments

Write comments to explain:

```text
why
invariants
non-obvious constraints
native API quirks
compatibility workarounds
```

Do not comment code that is already self-explanatory.

Bad:

```csharp
// Increment index.
index++;
```

Good:

```csharp
// Recycled hosts may receive callbacks from a previous binding if
// the item scope is not disposed before this generation changes.
_bindingGeneration++;
```

---

# 30. XML Documentation

Public APIs should receive XML documentation when their semantics are not obvious.

Architecture-critical public types should be documented.

Do not write meaningless XML comments such as:

```text
Gets or sets the value.
```

unless additional semantics are explained.

---

# 31. Regions

Avoid `#region` unless a file has a rare legitimate need.

Prefer smaller cohesive types/files instead.

---

# 32. File Size

There is no hard line-count limit.

Split files when responsibilities become meaningfully distinct.

Do not split a small component into many files just to satisfy arbitrary size rules.

---

# 33. One Main Type Per File

Prefer one primary public/internal class per file.

Small tightly coupled private/internal helper types may remain nearby if separation would reduce readability.

---

# 34. Partial Classes

Avoid partial classes unless:

```text
generated code
large tooling integration
clear platform-specific split
```

requires them.

Do not use partial classes to avoid designing coherent files.

---

# 35. Utility Classes

Avoid generic dumping grounds named:

```text
Utils
Helpers
Common
Misc
```

A helper should have a clear semantic owner.

---

# 36. Extension Methods

Use extension methods when they improve a natural API.

Do not hide important lifecycle or ownership behavior behind surprising extension calls.

---

# 37. LINQ

LINQ is acceptable for non-hot-path, clear code.

Be cautious in:

```text
per-frame paths
list recycling
frequent State updates
layout/style mapping loops
```

where allocations matter.

Do not micro-optimize ordinary initialization code without profiling.

---

# 38. Allocation Awareness

Avoid obvious unnecessary allocations in hot paths.

Examples:

```text
new arrays every State update
string formatting when no diagnostic is emitted
temporary collections during list recycling
```

But correctness comes before speculative allocation optimization.

---

# 39. Reflection

Do not use reflection for normal Widget binding, component registration, or discovery.

Reflection may be used selectively in development tooling when justified.

Core runtime behavior must remain AOT-friendly.

---

# 40. Dynamic Code Generation

Do not require:

```text
Reflection.Emit
runtime expression compilation
JIT-generated delegates
```

for Core behavior.

Mono/IL2CPP compatibility is required.

---

# 41. Source Generation

Do not introduce source generators as mandatory infrastructure.

Optional generation may be considered later through a separate design decision.

---

# 42. Unity Object Ownership

Do not destroy externally supplied Unity assets or VisualElements unless ownership transfer is explicit.

Borrowed means borrowed.

---

# 43. Native VisualElement Ownership

A supplied `VisualElement` with an existing parent must not be silently reparented.

Framework-owned hierarchy must not be modified behind WidgetNode lifecycle.

---

# 44. Root Ownership

Never use:

```csharp
root.Clear();
```

during normal LumaFlow mount/unmount unless the root is explicitly documented as exclusively owned by LumaFlow.

Multiple mounts must coexist safely.

---

# 45. Style Mapping

Centralize style mapping by concern.

Avoid duplicating native UI Toolkit style writes across many components.

---

# 46. Style Unset Semantics

Do not treat unspecified style properties as explicit defaults.

Unset should preserve USS/native behavior.

When LumaFlow previously owned an inline property and later releases it, clear that inline value.

---

# 47. Theme Values

Prefer semantic tokens over visual literals.

Bad:

```csharp
backgroundColor = new Color(...);
borderRadius = 6;
padding = 12;
```

throughout components.

Prefer:

```text
Theme colors
Theme spacing
Theme radius
component theme
```

unless the value is part of a low-level explicit override.

---

# 48. No Magic Numbers

Repeated visual/layout values should have a semantic owner.

Algorithmic constants may remain local when they genuinely belong to the algorithm and are clearly named.

---

# 49. Unity API Wrapping

Do not wrap every UI Toolkit API.

Create LumaFlow abstractions when they improve:

```text
declarative composition
reactivity
theme integration
lifecycle
semantic clarity
```

Use native interoperability for uncommon cases.

---

# 50. Native Controls

When a native UI Toolkit control already provides appropriate semantics, adapt it rather than rebuilding it.

Examples:

```text
Button
TextField
ScrollView
ListView
```

---

# 51. Layout

Use UI Toolkit/Yoga.

Do not implement a custom general layout engine.

Specialized overlay positioning is allowed where required.

---

# 52. Reactive Updates

Simple property changes should update native properties directly.

Do not rebuild a subtree for:

```text
text change
color change
opacity change
width change
enabled change
```

unless the structure itself changes.

---

# 53. Structural Updates

Structural reactivity should remain localized through explicit boundaries.

Do not introduce whole-tree rebuild behavior without an ADR change.

---

# 54. Equality

Use explicit equality semantics for value types and State.

Do not use reflection-based deep equality.

---

# 55. Threading

Mounted UI mutation occurs on Unity's main thread.

Do not imply thread safety for WidgetNode, BuildContext, or native UI operations.

---

# 56. Async Code

Async operations should not retain BuildContext blindly across `await`.

Framework-owned async work must define cancellation/teardown behavior.

Do not add fire-and-forget async operations without ownership.

---

# 57. Tests

Architecture-critical code requires tests.

Particularly:

```text
lifecycle
bindings
native ownership
context
style clearing
list recycling
Runtime/Editor boundaries
```

---

# 58. Regression Tests

When fixing an important framework bug, add a test reproducing it where practical.

Do not only patch the symptom.

---

# 59. Test Naming

Prefer behavioral test names:

```text
Unmount_RemovesStateSubscription
Mount_WhenNodeAlreadyMounted_Throws
ThemeScope_UsesNearestTheme
```

---

# 60. Tests and Private State

Prefer testing observable behavior.

Inspect internals only for architecture infrastructure where external behavior cannot prove the invariant sufficiently.

---

# 61. Consumer API Test

Before accepting a public API, read realistic usage code.

Ask:

```text
Is this pleasant without framework-internal knowledge?
Is this strongly typed?
Does IntelliSense guide the user?
Is there unnecessary boilerplate?
```

Consumer code quality is a first-class design criterion.

---

# 62. Public API Stability

Do not add public members casually.

Before 1.0, breaking changes are allowed.

Still prefer deliberate migration rather than accumulating obsolete API shapes.

---

# 63. Experimental API

Low-level extension APIs may be marked experimental while their shape is still being proven.

Do not imply stability prematurely.

---

# 64. Dependency Rules

Before adding a package or assembly dependency, verify:

```text
which layer actually needs it
whether it is optional
whether it leaks into public API
whether it creates cycles
```

---

# 65. Optional Dependencies

URP, HDRP, Localization, Addressables, third-party DI, and reactive libraries must not become Core dependencies accidentally.

---

# 66. Render Pipeline

Core code must remain pipeline-independent.

No URP/HDRP namespace in foundational Runtime.

---

# 67. Editor Integrations

Editor adapters should transform Editor-specific data/services into Runtime-compatible abstractions where appropriate.

Core should not be made Editor-aware.

---

# 68. Formatting

Use the project's configured formatter.

Until automated formatting exists, follow common C# formatting:

```csharp
if (condition)
{
    DoSomething();
}
```

Use braces consistently.

Avoid compressed one-line control flow in framework code when it reduces readability.

---

# 69. `var`

Use `var` when the type is obvious from the right-hand side or improves readability.

Prefer explicit type where inference hides important semantic information.

Both are acceptable.

Consistency within a method is more important than ideology.

---

# 70. Expression-Bodied Members

Use for very small obvious members.

Avoid long or complex expression-bodied implementations.

---

# 71. Method Size

A method should generally perform one coherent task.

Extract helpers when they clarify invariants.

Do not create microscopic methods that only obscure control flow.

---

# 72. Early Returns

Prefer early validation/returns when they reduce nesting.

Example:

```csharp
if (_state == WidgetNodeState.Disposed)
{
    throw ...
}
```

rather than deeply nested lifecycle logic.

---

# 73. Switches

Prefer exhaustive switches for framework enums where practical.

This makes new enum values visible at compile time.

---

# 74. Enums

Use enums for small closed semantic sets.

Do not use strings for:

```text
button variants
alignment
overlay placement
selection mode
```

when the set is known.

---

# 75. Flags

Use `[Flags]` only for genuinely combinable independent values.

Do not use flags as a substitute for a proper configuration object.

---

# 76. Structs

Use small immutable structs for compact value types where appropriate.

Examples:

```text
EdgeInsets
Radius
```

Avoid large structs with expensive copying.

---

# 77. Classes

Use classes when:

```text
identity matters
data is larger
inheritance/interface semantics are useful
copying would be undesirable
```

ThemeData is likely better represented as an immutable class/value graph.

---

# 78. Records

Use records only if the supported Unity/C# compatibility level and semantics make them appropriate.

Do not introduce them solely because they are concise.

---

# 79. Unity Serialization

Core Widgets do not need `[Serializable]` by default.

Do not shape declarative APIs around Unity serialization unless a feature specifically requires it.

---

# 80. ScriptableObject

Do not make ThemeData, Widget, or State inherit ScriptableObject by default.

Asset-backed variants may be layered later.

---

# 81. MonoBehaviour

Core UI composition must not require one MonoBehaviour per Widget/component.

MonoBehaviour host helpers are acceptable at application mount boundaries.

---

# 82. Internal Assertions

Use internal invariant validation around impossible states.

Do not rely solely on `Debug.Assert` for conditions that would corrupt runtime if ignored in a release build.

---

# 83. Version-Specific Unity Code

Isolate Unity-version compatibility logic.

Avoid version conditionals spread across standard components.

---

# 84. TODO Comments

Use TODOs only when:

```text
the work is concrete
the missing behavior is intentionally deferred
```

Prefer referencing an issue/roadmap item when possible.

Do not leave vague:

```text
TODO: improve this
```

inside critical runtime code.

---

# 85. Dead Code

Remove dead experiments.

Do not keep multiple unused implementations "just in case."

Git history already preserves previous versions.

---

# 86. Backward Compatibility Hacks

Before 1.0, prefer cleaning an API over retaining awkward legacy behavior.

After 1.0, use deliberate deprecation.

---

# 87. Performance Changes

Any complex optimization should answer:

```text
What was measured?
What is improved?
What invariant becomes more complicated?
What test protects correctness?
```

Do not trade lifecycle clarity for speculative speed.

---

# 88. Framework Philosophy

When unsure, ask:

```text
Can UI Toolkit already do this?
Can LumaFlow expose it more declaratively?
Can this remain strongly typed?
Can this stay local rather than global?
Can ownership be explicit?
Can the API remain small?
```

---

# 89. Final Rule

LumaFlow code should make ownership and intent obvious.

A reader should be able to understand:

```text
who owns this object
who disposes it
whether it is configuration or mounted state
whether it belongs to Runtime or Editor
whether an update is structural or direct
```

without reconstructing hidden framework magic.

The guiding principle is:

**Explicit lifecycle, typed intent, native interoperability, and small abstractions beat clever framework magic.**