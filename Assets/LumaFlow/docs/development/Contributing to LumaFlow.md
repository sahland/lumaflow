# Contributing to LumaFlow

Thank you for contributing to LumaFlow.

LumaFlow is a declarative C# UI framework built on top of Unity UI Toolkit.

The project prioritizes:

- native UI Toolkit interoperability;
- strongly typed declarative APIs;
- deterministic lifecycle and ownership;
- Runtime and Editor compatibility;
- small reusable abstractions;
- high-quality developer experience.

Before contributing, please read the relevant architecture and development documentation.

---

## 1. Start Here

Before making changes, read:

1. `AGENTS.md`
2. `docs/architecture/ARCHITECTURE.md`
3. `docs/architecture/API_DESIGN.md`
4. `docs/development/CODING_GUIDELINES.md`

Then read the ADRs relevant to the subsystem you are modifying:

```text
docs/architecture/adr/
```

For planned implementation work, also read:

```text
docs/development/IMPLEMENTATION_BLUEPRINT.md
```

---

## 2. Architecture Decisions Are Normative

Accepted ADRs define architectural constraints.

Do not silently violate an ADR because a local implementation is easier.

If implementation reveals that an ADR is no longer appropriate:

1. identify the conflict;
2. explain why the existing decision no longer works;
3. update or supersede the ADR;
4. then change the implementation.

Architecture should evolve explicitly.

---

## 3. Before Writing Code

Inspect the existing repository first.

Check:

```text
current folder structure
asmdefs
relevant Runtime implementation
relevant Editor implementation
existing tests
related ADRs
```

Do not create a parallel subsystem because the existing one was not inspected.

---

## 4. Keep Changes Focused

Prefer pull requests that solve one coherent problem.

Good:

```text
Implement State<T> subscription cleanup
```

Good:

```text
Add TextField controlled State<string> binding
```

Less desirable:

```text
Add TextField, navigation, animations, theme refactor,
package restructuring, and new diagnostics in one PR
```

Focused changes are easier to review and test.

---

## 5. Runtime / Editor Boundary

Runtime code must never depend on UnityEditor.

If your code requires:

```text
UnityEditor
UnityEditor.UIElements
AssetDatabase
SerializedObject
EditorWindow
Undo
```

it belongs in an Editor assembly.

Editor may depend on Runtime.

Runtime may not depend on Editor.

---

## 6. Render Pipeline Independence

Core LumaFlow must remain independent from:

```text
URP
HDRP
```

Pipeline-specific behavior belongs in optional integrations.

Ordinary Widgets must continue to work in:

```text
Built-in
URP
HDRP
```

without changing Core architecture.

---

## 7. Prefer Native UI Toolkit

Before implementing custom infrastructure, ask whether UI Toolkit already provides the behavior.

Examples:

```text
layout
focus
events
ScrollView
ListView virtualization
native controls
USS
```

LumaFlow should adapt and improve the authoring experience rather than recreate these systems unnecessarily.

---

## 8. Preserve the Widget Runtime Model

LumaFlow follows:

```text
Widget
↓
WidgetNode
↓
VisualElement
```

Widget is declarative configuration.

WidgetNode is the mounted runtime occurrence.

VisualElement is the native UI Toolkit element.

Do not merge these responsibilities.

---

## 9. Ownership Must Be Explicit

Every resource must have a clear owner.

Examples:

```text
State<T>
→ usually externally owned

State subscription
→ WidgetNode / BindingScope owned

native VisualElement created by a node
→ node owned

external VisualElement passed through Native(...)
→ borrowed

MountHandle
→ caller owned
```

If ownership is unclear, stop and resolve it before adding code.

---

## 10. Do Not Introduce Global State Casually

Avoid global mutable framework state.

Especially:

```text
Theme.Current
Navigator.Current
Overlay.Current
BuildContext.Current
```

Tree-scoped UI concerns belong in BuildContext.

Application services belong in application architecture.

---

## 11. Public API Quality

LumaFlow's public API is a primary product surface.

Before adding a public type/member, inspect realistic consumer usage.

Prefer:

```csharp
Button(
    "Save",
    variant: ButtonVariant.Primary,
    onPressed: Save
)
```

over APIs that simply expose raw native implementation details.

---

## 12. Composition First

For normal custom components:

```text
StatelessView
StatefulView
```

should be preferred over low-level WidgetNode work.

Use native-backed infrastructure only when direct native control is genuinely required.

---

## 13. Avoid Premature Abstractions

Do not add:

```text
interfaces
factories
registries
service locators
base classes
generic managers
```

solely because they may be useful someday.

LumaFlow prefers abstractions proven through repeated implementation.

---

## 14. No Mandatory Reflection Magic

Core functionality must not require runtime reflection-based binding, assembly scanning, or dynamic code generation.

LumaFlow should remain friendly to:

```text
Mono
IL2CPP
AOT
```

---

## 15. Testing Is Required

Architecture-critical behavior must have automated tests.

Particularly:

```text
mount/unmount
ownership
subscription cleanup
native hierarchy
BuildContext scopes
style clearing
controlled inputs
list recycling
Runtime/Editor boundaries
```

See:

```text
docs/architecture/adr/ADR-019-testing-and-validation.md
```

---

## 16. Bug Fixes

For reproducible framework bugs:

```text
reproduce
↓
add regression test
↓
fix implementation
↓
verify test
```

where practical.

Do not only patch the visible symptom.

---

## 17. Do Not Delete Valid Tests to Fix a Build

If an existing test protects an accepted architecture invariant, fix the implementation.

If the invariant itself has changed, update the architecture decision explicitly first.

---

## 18. Tests Should Use Real UI Toolkit Where Relevant

For native integration behavior, prefer real:

```text
VisualElement
Label
Button
TextField
ListView
```

over mocks.

LumaFlow is specifically an abstraction over UI Toolkit, so the integration layer must be tested.

---

## 19. Coding Style

Follow:

```text
docs/development/CODING_GUIDELINES.md
```

Key principles include:

```text
strong typing
immutable Widget configuration
internal runtime machinery
explicit ownership
no Runtime → Editor dependencies
no hidden global context
no unnecessary reflection
```

---

## 20. Public Documentation

If you introduce or materially change a user-facing API, update relevant consumer documentation when it exists.

Architecture documentation and user documentation serve different audiences.

Engineering docs live under:

```text
docs/
```

Consumer package documentation lives under:

```text
Packages/com.sahland.lumaflow/Documentation~/
```

---

## 21. ADR Changes

Create a new ADR or supersede an existing one when a change affects a significant architectural decision such as:

```text
runtime model
lifecycle
state model
reconciliation
package dependency direction
navigation semantics
public extension model
render backend
```

Do not create ADRs for ordinary implementation details.

---

## 22. Package Structure Changes

Before adding:

```text
new asmdef
new UPM dependency
new optional module
new package
```

read:

```text
ADR-020-package-modularity-and-dependencies.md
```

A new conceptual feature does not automatically deserve its own assembly.

---

## 23. Dependency Changes

When adding a dependency, explain:

```text
why it is needed
whether it is optional
which assembly owns it
whether it affects public API
whether it affects player builds
```

Core dependency growth should remain conservative.

---

## 24. Performance Work

Performance changes should be based on evidence.

For significant optimizations, provide:

```text
measurement
before/after result
correctness tests
```

Do not add complicated caching, pooling, or reconciliation without a measured need.

---

## 25. Dogfooding

AudioLib is an important Editor dogfood project.

If AudioLib exposes a missing reusable abstraction, improve LumaFlow itself where appropriate.

Do not add application-specific branches to Core.

---

## 26. Runtime Validation

Because AudioLib is Editor-heavy, contributors should remain conscious of Runtime compatibility.

Core changes should continue to work in a player environment.

---

## 27. Pull Request Checklist

Before submitting a change, verify:

```text
[ ] Relevant ADRs were read.
[ ] The change respects Runtime/Editor boundaries.
[ ] No accidental global framework state was introduced.
[ ] Ownership and cleanup are explicit.
[ ] Public API remains strongly typed and declarative.
[ ] Native UI Toolkit functionality was reused where practical.
[ ] Relevant tests were added or updated.
[ ] Existing tests pass.
[ ] No unrelated refactor was mixed into the change.
[ ] Documentation was updated when public behavior changed.
```

---

## 28. Architecture-Sensitive Checklist

For lifecycle/runtime work additionally verify:

```text
[ ] Mount failure rolls back correctly.
[ ] Unmount removes subscriptions/events.
[ ] External resources remain untouched unless owned.
[ ] Reentrant teardown is safe.
[ ] Multiple mounts remain isolated.
```

---

## 29. ListView Checklist

For virtualized list work verify:

```text
[ ] Native UI Toolkit virtualization is used.
[ ] Old item subscriptions are removed before rebind.
[ ] Old callbacks cannot target a new item.
[ ] Semantic item state does not depend on recycled host lifetime.
[ ] Large collections do not mount one row tree per item.
```

---

## 30. Context Checklist

For BuildContext/provider work verify:

```text
[ ] Nearest scope wins.
[ ] Siblings remain isolated.
[ ] Provider does not mutate parent context.
[ ] No native wrapper is added solely for scoping.
[ ] BuildContext is not used as an application DI container.
```

---

## 31. Editor Checklist

For Editor work verify:

```text
[ ] Code is in Editor assembly.
[ ] Runtime public API does not expose UnityEditor types.
[ ] Editor lifecycle cleanup is deterministic.
[ ] Feature works without requiring Play Mode where appropriate.
```

---

## 32. Commit Quality

Commit messages should describe the actual change clearly.

Prefer:

```text
Add deterministic BindingScope cleanup
```

over:

```text
update stuff
```

There is no requirement for a particular conventional-commit format unless project tooling later adopts one.

---

## 33. Generated Code

Do not commit generated artifacts unless the repository explicitly requires them.

Generated code must not become the hidden source of truth for Core architecture.

---

## 34. Breaking Changes

Before 1.0, breaking changes are permitted.

Still:

```text
make them intentional
update tests
update docs
update examples
remove obsolete API cleanly
```

Avoid carrying several competing pre-1.0 API generations indefinitely.

---

## 35. Security and Data

Framework diagnostics should not unnecessarily print application/user data.

Password-like values must never be included in diagnostic descriptions.

Do not introduce telemetry or analytics into Core without an explicit decision.

---

## 36. Licensing

Only add third-party source/assets with compatible licensing.

Add attribution/notice where required.

Do not introduce proprietary fonts, icons, or other assets as mandatory Core dependencies.

---

## 37. If You Are Unsure

Use this decision order:

```text
Can UI Toolkit already solve it?
        ↓ yes
Wrap/adapt it.

Does the feature improve consumer code?
        ↓ no
Do not add it.

Does it need a reusable abstraction now?
        ↓ no
Keep it local.

Does it violate an ADR?
        ↓ yes
Stop and resolve the architectural conflict.

Is ownership unclear?
        ↓ yes
Resolve ownership before implementation.
```

---

## 38. Final Principle

LumaFlow should remain:

```text
declarative
typed
predictable
native-compatible
lifecycle-safe
small at the core
```

The goal is not to build the largest possible abstraction over UI Toolkit.

The goal is to make modern Unity UI authoring significantly better while preserving the strengths of the native platform.
