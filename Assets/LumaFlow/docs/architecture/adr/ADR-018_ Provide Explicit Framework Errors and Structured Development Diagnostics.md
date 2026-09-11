# ADR-018: Provide Explicit Framework Errors and Structured Development Diagnostics

- **Status:** Accepted
- **Decision date:** 2026-08-11
- **Scope:** Errors, invariant violations, diagnostics, logging, development tooling
- **Affects:** Runtime, Editor, WidgetNode lifecycle, Bindings, Styling, Lists, Navigation, Overlays, Testing
- **Related documents:** `ADR-007-widget-runtime-model.md`, `ADR-008-lifecycle-and-ownership.md`, `ADR-010-styling-model.md`, `ADR-014-list-virtualization.md`, `ADR-017-runtime-editor-boundary.md`

---

## 1. Context

LumaFlow adds a declarative runtime layer on top of Unity UI Toolkit.

This introduces framework-specific invariants such as:

```text
WidgetNode mounted only once
subscriptions disposed on unmount
external VisualElement ownership respected
context scopes valid
list bindings cleared before recycling
overlay handles closed safely
runtime assemblies free from Editor dependencies
```

When these invariants are violated, generic exceptions such as:

```text
NullReferenceException
InvalidOperationException
ArgumentException
```

without useful context make debugging unnecessarily difficult.

At the same time, excessive framework logging would create noise and reduce trust in diagnostics.

LumaFlow therefore requires a deliberate error and diagnostics model.

---

## 2. Decision

LumaFlow will distinguish between:

```text
programmer/configuration errors
framework invariant violations
recoverable runtime conditions
development diagnostics
normal application exceptions
```

Errors should fail close to their source with descriptive context.

Diagnostics should be structured and development-oriented rather than emitted continuously during normal operation.

---

## 3. Core Principle

When LumaFlow detects an invalid framework state, the error should explain:

```text
what failed
which framework object was involved
what invariant was violated
what the likely correction is
```

where that information is reliably available.

---

## 4. Error Quality Target

Avoid:

```text
NullReferenceException
```

when LumaFlow can instead report:

```text
LumaFlow: ButtonNode cannot be mounted because this node is already mounted.

Widget: Button("Save")
Node state: Mounted

A Widget description may be mounted multiple times by creating separate WidgetNodes,
but the same WidgetNode instance cannot be mounted twice.
```

The exact formatting may vary.

The diagnostic content is the important part.

---

## 5. Framework Error Categories

LumaFlow should conceptually classify errors into categories such as:

```text
Lifecycle
Binding
Context
Layout
Styling
NativeInterop
ListVirtualization
Navigation
Overlay
Configuration
Compatibility
InternalInvariant
```

A public enum is not required initially.

This classification is primarily useful for internal structure and diagnostics.

---

## 6. Programmer Errors

Programmer errors are invalid API usage that can be detected deterministically.

Examples:

```text
Expanded flex <= 0
minWidth > maxWidth
required child missing
same WidgetNode mounted twice
native element already parented
duplicate list keys
missing required Navigator scope
```

These should normally fail immediately with clear exceptions.

---

## 7. Do Not Silently Repair Invalid Semantics

Avoid behavior such as:

```text
Expanded(flex: -4)
↓
silently use flex = 1
```

or:

```text
minWidth = 500
maxWidth = 200
↓
silently swap values
```

unless the API explicitly defines normalization.

Invalid configuration should be visible during development.

---

## 8. Framework Invariant Violations

An invariant violation means LumaFlow's internal runtime reached a state that should be impossible if its own logic is correct.

Examples:

```text
disposed node remains registered as mounted
child node has two owning parents
binding scope survives final disposal
navigation entry exists without owning navigator
recycled row has two active item scopes
```

These should be treated more seriously than ordinary invalid user parameters.

---

## 9. Internal Invariant Failure

An internal invariant failure should provide enough information for a framework bug report.

Conceptually:

```text
LumaFlow internal invariant violation.

Invariant:
A WidgetNode may have only one owner.

Node:
TextNode #142

Current owner:
ColumnNode #61

Attempted owner:
RowNode #77
```

Do not conceal these failures with silent fallback behavior.

---

## 10. Recoverable Runtime Conditions

Some conditions are not programming errors and may be handled gracefully.

Examples:

```text
optional visual effect unavailable
anchor disappears while Popover is open
optional Navigator absent when using TryGet
resource no longer available
unsupported optional feature on current Unity version
```

These may:

```text
degrade gracefully
close affected UI
return false
return null/optional value
```

depending on the API contract.

---

## 11. Exceptions Are Not for Normal Control Flow

Do not use exceptions for expected operations such as:

```text
Navigator cannot pop root
TryGetProvider failed
OverlayHandle already closed
```

where a normal return value can represent the condition.

Example:

```csharp
bool Pop();
```

may be preferable to throwing when the stack is already at root.

---

## 12. Invalid Required Context

If a Widget requires a context service and none exists:

```text
context.Navigator
```

should fail with a descriptive LumaFlow error if the contract says Navigator is required.

Avoid delayed:

```text
NullReferenceException
```

from unrelated code.

---

## 13. Optional Context Lookup

Where absence is valid, expose an explicit optional path.

Conceptually:

```csharp
context.TryGetNavigator(out var navigator)
```

or equivalent.

Required and optional lookup semantics must be distinct.

---

## 14. Exception Type Strategy

LumaFlow may define a small set of framework exception types.

Potential:

```csharp
LumaFlowException

LumaFlowLifecycleException
LumaFlowConfigurationException
LumaFlowInvariantException
```

Do not create dozens of tiny exception subclasses.

---

## 15. Base Exception

A framework base exception may help consumers identify LumaFlow-originated failures:

```csharp
public class LumaFlowException : Exception
{
}
```

This is optional but likely useful.

---

## 16. Specialized Exceptions

Specialized types are justified when callers or tests may meaningfully distinguish them.

Strong candidates:

```text
Lifecycle violation
Configuration error
Internal invariant failure
```

Avoid types that only duplicate the message category.

---

## 17. Argument Exceptions

For ordinary invalid method arguments, standard .NET exceptions such as:

```text
ArgumentNullException
ArgumentOutOfRangeException
```

may remain appropriate.

LumaFlow-specific exceptions are not required for every validation failure.

---

## 18. Error Wrapping

Do not wrap every exception merely to prepend "LumaFlow".

Bad:

```text
catch (Exception ex)
{
    throw new LumaFlowException("Failed", ex);
}
```

at every layer.

This destroys useful stack clarity and creates noise.

Wrap only when LumaFlow can add meaningful architectural context.

---

## 19. Preserve Inner Exceptions

When wrapping is justified:

```text
preserve original exception as InnerException
```

so root cause remains accessible.

---

## 20. Mount Error Context

Mount failures are a high-value location for contextual wrapping.

Example:

```text
Failed to mount TextField.

Widget path:
SettingsView
→ AccountSection
→ TextField

Cause:
...
```

where Widget path information is available cheaply.

---

## 21. Partial Mount Rollback

ADR-008 requires cleanup on mount failure.

If cleanup also fails:

```text
original mount exception
```

must remain the primary failure.

Cleanup failures may be attached/logged as secondary diagnostics.

Do not replace the original cause.

---

## 22. Multiple Cleanup Failures

During unmount:

```text
child cleanup fails
binding cleanup fails
event cleanup fails
```

the runtime should continue best-effort cleanup where safe.

A development diagnostic may aggregate failures afterward.

---

## 23. Cleanup Error Reporting

Conceptually:

```text
LumaFlow encountered 2 errors while unmounting DialogNode.

1. ...
2. ...
```

The exact mechanism may use:

```text
AggregateException
```

or structured diagnostics.

Implementation should prioritize cleanup completion.

---

## 24. User Callback Exceptions

If application code throws from:

```text
onPressed
onChanged
item callback
dialog action
```

LumaFlow should normally allow that exception to propagate through normal Unity/C# mechanisms.

Do not mislabel user callback failures as framework bugs.

---

## 25. Callback Context

Where practical, LumaFlow may annotate diagnostics indicating the callback source.

Example:

```text
Exception occurred while invoking Button.onPressed for Button("Save").
```

This is useful but should not require aggressive wrapping of every callback.

---

## 26. Build Exceptions

If:

```csharp
Build(BuildContext context)
```

throws, the framework may add context such as:

```text
View: SettingsView
Build operation failed.
```

while preserving the original exception.

---

## 27. Item Builder Exceptions

A virtualized item builder failure should identify:

```text
ListView type
item index if available
item key if available
item model type
```

without serializing arbitrary object contents.

---

## 28. Do Not Dump Arbitrary Models

Diagnostics should not automatically call:

```csharp
model.ToString()
```

for every model if that may:

```text
leak sensitive data
be expensive
throw
produce huge output
```

Prefer safe identifiers and types.

---

## 29. Widget Debug Description

Widgets may provide a lightweight debug description.

Conceptually:

```text
Button("Save")
Text("Settings")
ListView<AudioClip>
```

This representation is diagnostic only.

It must not define Widget identity.

---

## 30. Node Debug Description

Internal nodes may expose:

```text
type
internal ID
lifecycle state
native element type
child count
```

to diagnostics.

Example:

```text
ButtonNode #118 [Mounted] → UnityEngine.UIElements.Button
```

---

## 31. Runtime Node IDs

Internal monotonic or otherwise unique node IDs may be useful.

They are:

```text
debug identifiers
```

not:

```text
semantic identity
serialization IDs
reconciliation keys
```

---

## 32. Mount IDs

Root mount operations may likewise receive internal debug IDs.

Example:

```text
Mount #4
```

This helps distinguish independent trees.

---

## 33. Navigation Entry IDs

Navigation entries may have diagnostic IDs.

Again:

```text
debugging only
```

Do not expose them as route semantics.

---

## 34. Overlay Entry IDs

Same rule applies to overlays.

---

## 35. Diagnostic Paths

A node may be described through a logical path:

```text
AudioLibView
→ Sidebar
→ LibraryList
→ AudioClipRow
→ PlayButton
```

This can dramatically improve debugging.

---

## 36. Path Is LumaFlow Tree Path

The diagnostic path should follow the WidgetNode tree.

It does not necessarily equal the native VisualElement hierarchy.

This follows ADR-007.

---

## 37. Path Generation Cost

Do not continuously construct full diagnostic paths during normal operation.

Build paths lazily when reporting an error or when diagnostics tooling requests them.

---

## 38. Development Diagnostics

Not every suspicious condition should throw.

Some are better represented as development warnings.

Examples:

```text
Expanded used outside expected flex parent
very deep Widget tree
duplicate stylesheet attachment
potentially stale NativeRef access
unsupported optional visual capability
large collection rendered through Column
```

Warnings must only be emitted when detection is reliable.

---

## 39. Warning Quality

A warning should include:

```text
what was detected
why it may be a problem
where it occurred
how to address it
```

Avoid vague logs such as:

```text
LumaFlow warning: bad layout.
```

---

## 40. Avoid Warning Spam

Repeated warning conditions must not log every frame or every layout pass.

Possible strategies:

```text
warn once per node
warn once per mount
rate limit
deduplicate diagnostic signature
```

Do not flood the Unity Console.

---

## 41. Diagnostics Are Event-Driven

Forbidden:

```text
Update()
→ inspect entire Widget tree
→ generate warnings every frame
```

Normal diagnostics should trigger around relevant lifecycle/actions.

---

## 42. Diagnostics Levels

A useful conceptual model:

```text
Info
Warning
Error
Invariant
```

The exact public logging API is deferred.

Most normal framework operation should produce no logs.

---

## 43. No Success Logging by Default

Do not emit:

```text
Mounted Button successfully.
State binding created.
Theme applied.
```

during ordinary operation.

These messages create noise.

Detailed lifecycle tracing may exist behind explicit debug options.

---

## 44. Diagnostic Sink

Core should avoid hard-coding all diagnostics directly to one presentation mechanism.

A small internal/public abstraction may eventually allow:

```text
Unity console
tests
Editor diagnostic window
custom application sink
```

Conceptually:

```csharp
IDiagnosticSink
```

This is optional and should remain small.

---

## 45. Default Diagnostic Output

In ordinary Unity development, warnings/errors may eventually flow through:

```text
Debug.LogWarning
Debug.LogError
```

or appropriate exception reporting.

Core must remain Runtime-compatible.

No Editor console API dependency.

---

## 46. Editor Diagnostics Layer

`LumaFlow.Editor` may provide richer presentation:

```text
Widget Inspector
Mount tree viewer
binding inspector
theme inspector
diagnostic list
```

without changing Runtime semantics.

This follows ADR-017.

---

## 47. Runtime Diagnostics Data

Runtime may expose read-only diagnostic snapshots or internal interfaces consumed by Editor tooling.

Do not expose mutable WidgetNode internals.

---

## 48. Widget Inspector

A future LumaFlow Widget Inspector should conceptually display:

```text
Mount
WidgetNode tree
Widget descriptions
native VisualElement mappings
BuildContext scopes
bindings
theme/style resolution
lifecycle states
```

This complements UI Toolkit Debugger.

---

## 49. Do Not Replace UI Toolkit Debugger

UI Toolkit Debugger remains authoritative for:

```text
native hierarchy
resolved USS
native layout
native styles
```

LumaFlow tooling should explain framework semantics above that layer.

---

## 50. Cross-Linking Diagnostics

Long-term, it would be useful to select:

```text
WidgetNode
```

and locate:

```text
VisualElement
```

or vice versa.

This is desirable tooling but not an MVP requirement.

---

## 51. Binding Diagnostics

A node diagnostic may show:

```text
active binding count
binding target
State<T> type
last update
```

where safe.

Avoid displaying arbitrary State values by default if they may contain sensitive or large data.

---

## 52. Binding Leak Detection

Development mode may detect:

```text
disposed node still receives State notification
```

This should be treated as a framework invariant violation.

---

## 53. Stale Callback Detection

Similarly:

```text
callback targets disposed node
```

should produce strong diagnostics if detectable.

---

## 54. List Recycling Diagnostics

High-value diagnostics include:

```text
host currently bound to index/key
active item scope
bind generation
```

A generation counter can help detect stale callbacks.

---

## 55. Binding Generation

A recycled item host may internally increment:

```text
binding generation
```

on each rebind.

Development assertions can verify a callback belongs to the current generation.

This is an implementation option.

---

## 56. Native Interop Diagnostics

Useful errors include:

```text
external VisualElement already has parent
same native element mounted twice
NativeRef accessed after unmount
framework-owned hierarchy externally modified
```

where these conditions can be reliably observed.

---

## 57. External Hierarchy Corruption

If LumaFlow detects that a framework-owned child VisualElement disappeared without node unmount:

```text
report framework/native hierarchy mismatch
```

Do not attempt arbitrary reconstruction unless explicitly designed.

---

## 58. Style Diagnostics

Potential diagnostics include:

```text
invalid constraint
unsupported typed style mapping
LumaFlow inline style blocks expected USS override
```

The latter may be primarily Editor tooling rather than runtime warnings.

---

## 59. Style Source Inspector

A future inspector may show:

```text
BackgroundColor:
Theme.Button.Primary.Background
→ resolved #...
→ applied inline
```

This is valuable for complex themes.

---

## 60. Context Diagnostics

A selected WidgetNode may expose:

```text
Theme provider source
Navigator scope
OverlayHost scope
MediaQuery source
```

This makes nested scope behavior inspectable.

---

## 61. Missing Provider Error

When a required provider is absent, include:

```text
requested provider
Widget path
nearest relevant scope if known
```

Example:

```text
No Navigator is available for SettingsBackButton.

Widget path:
PreviewRoot
→ SettingsBackButton

Mount this subtree inside a NavigatorHost or avoid requiring Navigator.
```

---

## 62. Navigation Diagnostics

Potential development information:

```text
stack depth
entry types
current entry
nested Navigator relationship
```

Do not print the whole stack on every Push/Pop by default.

---

## 63. Overlay Diagnostics

Potential information:

```text
host
entry count
topmost entry
modal status
anchor status
```

Again, inspector-oriented rather than default log spam.

---

## 64. Diagnostic Feature Flags

Detailed diagnostics may be controlled through:

```text
development build
editor
explicit LumaFlow diagnostics setting
```

Exact configuration is deferred.

---

## 65. Release Behavior

Release builds may disable expensive assertions and verbose diagnostic metadata.

However, correctness must never rely on diagnostics being enabled.

---

## 66. Development Assertions

Internal checks may use:

```text
Debug.Assert
custom invariant helpers
conditional diagnostics
```

depending on whether failure should continue or stop.

Critical invariant violations should not disappear entirely in release builds if continuing would corrupt runtime state.

---

## 67. Validation vs Assertions

Use validation for user-provided configuration.

Use invariant assertions/exceptions for impossible framework state.

Example:

```text
flex <= 0
→ configuration validation

node has two owners
→ internal invariant
```

---

## 68. Central Invariant Helper

An internal helper may standardize errors.

Conceptually:

```csharp
LumaInvariant.Require(
    condition,
    node,
    "A WidgetNode cannot have multiple owners."
);
```

This can improve consistency.

Do not turn it into an opaque macro system.

---

## 69. Source File and Line

C# caller information such as:

```text
CallerFilePath
CallerLineNumber
```

may be useful in selected diagnostics APIs.

Do not require every Widget to capture source location during normal construction unless profiling shows negligible cost and tooling value is high.

---

## 70. Future Source Mapping

Source-generation or compiler features may eventually improve Widget source mapping.

This is optional tooling and not part of core correctness.

---

## 71. Exception Message Stability

Exception messages are primarily for humans.

Do not make tests depend on complete exact message strings where avoidable.

Prefer testing:

```text
exception type
error code/category if introduced
important substrings
```

---

## 72. Diagnostic Codes

A future stable code system may help documentation.

Example:

```text
LF1001 - WidgetNode already mounted
LF2004 - Native VisualElement already parented
```

Do not introduce codes until the error catalog is large enough to justify them.

---

## 73. Documentation Links

Diagnostics may eventually reference documentation by error code.

Avoid hardcoding fragile URLs throughout Runtime.

---

## 74. Logging Abstraction Scope

Do not build a full general-purpose logging framework.

LumaFlow diagnostics exist only for LumaFlow concerns.

Applications may continue using their own logging systems.

---

## 75. No Analytics

Diagnostics must not secretly transmit telemetry.

LumaFlow Core should not collect or send usage/error data by default.

---

## 76. Privacy

Diagnostic output should avoid unnecessarily printing:

```text
user-entered text
tokens
passwords
large object contents
personal data
```

Framework diagnostics usually need structural metadata, not application content.

---

## 77. TextField Diagnostics

Never include password field contents in exceptions or debug descriptions.

A secure field should be represented structurally:

```text
TextField [Password]
```

not by current value.

---

## 78. Object Names

Unity object names may be useful if explicitly available.

Avoid recursively serializing Unity objects for diagnostics.

---

## 79. Performance

Diagnostics must not significantly affect normal runtime performance.

Avoid:

```text
continuous stack traces
per-frame tree serialization
large strings built on every update
reflection-based graph inspection
```

unless explicit diagnostics tooling is active.

---

## 80. Lazy Formatting

Diagnostic messages involving expensive details should be formatted only when emitted.

Potential helpers may accept deferred message generation internally.

Do not over-engineer until needed.

---

## 81. Reflection

Runtime diagnostics should not rely heavily on reflection.

Type names via:

```text
typeof(T).Name
GetType().Name
```

are acceptable.

Avoid general object graph reflection.

---

## 82. AOT Safety

Diagnostics must remain compatible with IL2CPP/AOT.

No dynamic code generation is required.

---

## 83. Error Recovery Boundary

LumaFlow should not globally swallow exceptions in order to "keep UI alive."

Silent corruption is worse than visible failure during development.

Recover only from conditions with defined recovery semantics.

---

## 84. Error Boundary Component

A future component similar to an error boundary may be useful:

```text
ErrorBoundary
```

that catches failures while building/mounting a local subtree and renders fallback UI.

This is deferred.

---

## 85. ErrorBoundary Is Not Global Catch-All

If added, it should not hide:

```text
internal invariant violations
memory corruption
framework ownership bugs
```

without clear reporting.

Its likely purpose is application/view failure containment.

---

## 86. Editor Error Placeholder

Development tooling may optionally render an error placeholder where a subtree failed.

Example:

```text
SettingsPanel failed to build.
See Console.
```

This can improve Editor workflow.

It must not leave partially mounted resources behind.

---

## 87. Mount Failure and Previous UI

For structural replacement, ADR-004 allows potentially preserving the old subtree if mounting the replacement fails.

Diagnostics should clearly report:

```text
new subtree failed
old subtree retained
```

if that policy is used.

---

## 88. Runtime Error Policy Must Be Predictable

Do not sometimes:

```text
throw
```

and elsewhere:

```text
log and continue
```

for the same category of violation.

Each API should have documented semantics.

---

## 89. Native API Exceptions

Exceptions thrown directly by UI Toolkit should generally remain visible.

Wrap only when LumaFlow can meaningfully explain which framework operation triggered them.

---

## 90. Unsupported Feature Errors

If an explicitly requested feature is unavailable on the current environment:

```text
Dynamic list height is unavailable on this Unity version.
```

should be clear.

If graceful degradation is part of that feature's contract, use the documented fallback instead.

---

## 91. Compatibility Diagnostics

Version adapters may expose capability information for diagnostics.

Avoid scattering version-specific warning strings throughout components.

---

## 92. Editor-Only Diagnostics

Editor may additionally report:

```text
assembly dependency violations
invalid package structure
USS extension-point issues
theme inspection
```

These remain outside Runtime where appropriate.

---

## 93. Diagnostic Settings

A future settings object may control:

```text
enable verbose lifecycle tracing
enable expensive tree validation
enable style source tracking
```

These are developer options.

Default should remain lightweight.

---

## 94. Expensive Tree Validation

Development mode may optionally verify:

```text
node parent relationships
native hierarchy correspondence
disposed status
binding scope state
```

after structural operations.

Do not run full validation after every state update by default.

---

## 95. Validation Checkpoints

Good points for optional expensive validation include:

```text
after root mount
after structural rebuild
after navigation mutation
after overlay mutation
after list recycle stress tests
```

particularly in tests.

---

## 96. Testing Errors

Tests must cover expected invalid states.

Examples:

```text
double mount
invalid Expanded flex
native element with existing parent
missing required Navigator
duplicate list key
update after disposal
```

---

## 97. Testing Message Quality

At least selected tests should verify that diagnostics contain useful identifying information.

Do not test every punctuation detail.

---

## 98. Testing Cleanup After Failure

A thrown mount error must be followed by verification that:

```text
native hierarchy is clean
bindings are gone
child nodes are disposed
```

Error reporting and lifecycle correctness must be tested together.

---

## 99. Testing User Callback Failure

If a button callback throws:

```text
framework does not incorrectly classify node as lifecycle-corrupt
```

unless callback-triggered teardown actually violated an invariant.

---

## 100. Testing Diagnostic Deduplication

If warnings are deduplicated:

```text
same condition repeated
```

should not spam the log.

The deduplication scope should be deterministic.

---

## 101. Testing in Player Builds

Critical configuration and invariant errors must remain understandable outside Editor.

Do not make all useful error messages Editor-only.

---

## 102. Debug Naming

Users may be able to assign:

```text
name
debugLabel
```

to key UI elements.

Avoid a dedicated debugLabel API unless native name or normal component metadata is insufficient.

---

## 103. User-Facing Errors vs Developer Errors

LumaFlow framework exceptions are developer-facing.

They are not automatically suitable for displaying to end users.

Applications decide how to present operational failures.

---

## 104. No Automatic Error Dialog

Do not show framework dialogs automatically when an exception occurs.

Log/throw through developer channels.

Applications may create their own ErrorBoundary/fallback UI.

---

## 105. No Global Try/Catch Around UI

A framework-wide catch that swallows all event/build exceptions is rejected.

It would make failures difficult to diagnose.

---

## 106. Rejected Alternative: Generic Exceptions Everywhere

Rejected because framework context is frequently available and valuable.

---

## 107. Rejected Alternative: Log and Continue for Invariant Violations

Rejected because continuing may corrupt node ownership/lifecycle.

---

## 108. Rejected Alternative: Throw for Every Warning

Rejected because some suspicious patterns are valid and should remain advisory.

---

## 109. Rejected Alternative: Logging Every Lifecycle Event

Rejected due to noise and runtime overhead.

Detailed tracing should be opt-in.

---

## 110. Rejected Alternative: Editor-Only Diagnostics

Rejected because Runtime/player failures still need useful messages.

Editor provides richer presentation, not the only diagnostics.

---

## 111. Rejected Alternative: Full Logging Framework

Rejected.

LumaFlow needs framework diagnostics, not replacement infrastructure for application logging.

---

## 112. Rejected Alternative: Reflection-Based Inspector Core

Rejected as the fundamental diagnostics mechanism.

LumaFlow already knows its own runtime structures and should expose explicit diagnostic data.

---

## 113. Architectural Invariants Created by This ADR

Unless superseded:

### Invariant 1

Detected invalid framework usage fails close to its source.

### Invariant 2

Internal invariant violations are clearly distinguished from normal conditions.

### Invariant 3

Diagnostics include useful framework context where practical.

### Invariant 4

Cleanup continues best-effort after teardown failures.

### Invariant 5

User callback exceptions are not silently swallowed.

### Invariant 6

Normal operation does not spam logs.

### Invariant 7

Expensive diagnostics are opt-in/development-oriented.

### Invariant 8

Runtime diagnostics remain independent from UnityEditor.

### Invariant 9

Diagnostic metadata does not define semantic Widget identity.

### Invariant 10

Diagnostics avoid unnecessary application-data exposure.

---

## 114. Codex Rules

### Rule 1

Do not replace clear framework failures with generic `NullReferenceException` when the invalid state is already known.

### Rule 2

Validate public configuration at the nearest reasonable boundary.

### Rule 3

Do not silently normalize invalid configuration unless the API explicitly defines normalization.

### Rule 4

Preserve original exceptions when adding framework context.

### Rule 5

Do not swallow user callback exceptions globally.

### Rule 6

Do not emit informational lifecycle logs by default.

### Rule 7

Deduplicate or rate-limit repeatable diagnostics.

### Rule 8

Do not use UnityEditor APIs from Runtime diagnostics.

### Rule 9

Do not expose arbitrary model values in diagnostics unnecessarily.

### Rule 10

Any internal invariant helper must fail loudly enough to prevent runtime corruption.

---

## 115. Decision Test

When handling an abnormal condition:

```text
Is the caller configuration invalid?
        ↓ yes
Validate and report clearly.

Is this expected normal control flow?
        ↓ yes
Return a normal result.

Is this an impossible internal state?
        ↓ yes
Invariant failure.

Can LumaFlow recover with defined semantics?
        ↓ yes
Recover and optionally warn.

Would continuing risk corrupt lifecycle/ownership?
        ↓ yes
Stop rather than silently continue.
```

---

## 116. Example: Double Mount

Bad operation:

```text
ButtonNode #12 [Mounted]
↓
Mount()
```

Expected diagnostic:

```text
LumaFlow lifecycle error:
ButtonNode #12 cannot be mounted because it is already mounted.

A WidgetNode represents one mounted occurrence.
Create another node from the Widget description for another mount.
```

---

## 117. Example: Missing Navigator

```csharp
context.Navigator.Pop();
```

outside a Navigator scope.

Expected:

```text
LumaFlow context error:
No Navigator is available for SettingsBackButton.

Mount this subtree inside a NavigatorHost or use an optional Navigator lookup.
```

---

## 118. Example: Invalid Native Interop

```text
Native(existingElement)
```

where `existingElement.parent != null`.

Expected:

```text
LumaFlow native interop error:
The supplied VisualElement already belongs to another hierarchy.

Element type:
CustomGraphElement

LumaFlow does not silently reparent borrowed VisualElements.
```

---

## 119. Example: List Recycling Violation

If stale item binding is detected:

```text
LumaFlow invariant violation:
A recycled ListView host received an update from a previous item binding.

Host: #17
Current binding generation: 42
Callback generation: 41
```

This indicates a framework bug or incorrect custom binding adapter.

---

## 120. Example: Cleanup Failure

Mount fails while creating Child C.

LumaFlow attempts to clean A and B.

If cleanup B also throws:

```text
Primary:
Failed to mount Child C.

Secondary cleanup diagnostic:
An exception occurred while rolling back Child B.
```

The original mount failure remains primary.

---

## 121. Initial Runtime Diagnostic Set

Before Phase 1 is considered stable, implement clear diagnostics for at least:

```text
double mount
use after dispose
child ownership violation
mount rollback failure
missing required context
external native element already parented
invalid basic layout values
```

This covers the highest-risk runtime mistakes.

---

## 122. Initial Editor Diagnostic Set

Early Editor tooling may remain minimal.

Useful first additions:

```text
inspect mounted node tree
inspect native mapping
inspect lifecycle state
inspect BuildContext/theme source
```

Only after Core runtime is stable.

---

## 123. Long-Term Direction

Diagnostics may eventually include:

```text
Widget Inspector
context inspector
theme/style source inspector
binding inspector
navigation stack inspector
overlay inspector
list recycling inspector
performance counters
source navigation
diagnostic codes
```

These should be layered over explicit Runtime diagnostic data.

---

## 124. Reconsideration Conditions

Revisit this ADR if:

1. public users need pluggable diagnostic sinks;
2. error volume justifies stable diagnostic codes;
3. ErrorBoundary semantics are introduced;
4. source mapping becomes important for tooling;
5. production builds need configurable invariant handling.

Any evolution should preserve descriptive errors and deterministic failure semantics.

---

## 125. Final Decision

LumaFlow's error model is:

```text
validate bad configuration early
+
fail clearly on lifecycle/invariant violations
+
recover only when recovery semantics are defined
+
keep normal operation quiet
+
provide structured development diagnostics
```

The guiding rule is:

**A framework error should tell the developer what invariant was broken and where, not force them to reconstruct LumaFlow internals from a generic exception.**