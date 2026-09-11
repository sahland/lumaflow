# ADR-019: Validate LumaFlow Through Layered Automated Testing and Compatibility Gates

- **Status:** Accepted
- **Decision date:** 2026-08-11
- **Scope:** Testing strategy, validation, CI, compatibility, regressions
- **Affects:** Runtime, Editor, Core architecture, Components, Distribution, Releases
- **Related documents:** `ROADMAP.md`, `COMPATIBILITY.md`, `ADR-003-reactive-state.md`, `ADR-007-widget-runtime-model.md`, `ADR-008-lifecycle-and-ownership.md`, `ADR-014-list-virtualization.md`, `ADR-017-runtime-editor-boundary.md`, `ADR-018-error-and-diagnostics.md`

---

## 1. Context

LumaFlow introduces infrastructure that is deceptively easy to make appear correct in simple demos while still containing serious architectural defects.

Examples include:

```text
State subscriptions that leak after unmount

callbacks attached more than once

WidgetNodes surviving disposal

VisualElements left in native hierarchy

incorrect scoped BuildContext resolution

style overrides that cannot be cleared

recycled ListView rows receiving stale updates

Editor dependencies leaking into Runtime

features working in Editor but failing in player builds
```

These failures may not be immediately visible during ordinary UI usage.

LumaFlow therefore requires testing at several architectural layers.

---

## 2. Decision

LumaFlow will use a layered validation strategy:

```text
Unit tests
    ↓
Runtime integration tests
    ↓
Editor integration tests
    ↓
UI hierarchy/lifecycle tests
    ↓
compatibility/build tests
    ↓
real application dogfooding
```

No single category is sufficient on its own.

---

## 3. Core Principle

Tests should verify observable architectural contracts rather than internal implementation trivia.

Prefer testing:

```text
subscription is removed after unmount
```

over:

```text
private field _subscriptionCount equals zero
```

unless internal-state assertions are specifically required by framework tests.

---

## 4. Testing Priorities

The highest-priority test areas are:

```text
Lifecycle correctness
Ownership
Reactive binding cleanup
Context scoping
Native hierarchy mapping
Style application/removal
Virtualization recycling
Runtime/Editor boundaries
Player compilation
```

Visual polish is secondary to these invariants.

---

## 5. Test Layers

LumaFlow should distinguish approximately:

```text
Pure unit tests
Mounted runtime tests
Native UI Toolkit integration tests
Editor-specific tests
Build/compatibility validation
Dogfood validation
```

Each has different responsibilities.

---

## 6. Pure Unit Tests

Pure tests should cover value-like and environment-independent behavior.

Examples:

```text
EdgeInsets equality
BorderRadius equality
Theme resolution
style precedence
state equality suppression
constraint validation
layout enum mapping
configuration validation
```

These tests should be fast and deterministic.

---

## 7. State<T> Tests

`State<T>` tests must include:

```text
initial value
subscription
value mutation
equality suppression
unsubscribe
multiple subscribers
subscriber mutation ordering if specified
```

Example:

```text
State<int>(1)
↓
set 2
↓
subscriber notified exactly once
```

Then:

```text
set 2 again
↓
no notification
```

when equality semantics suppress unchanged values.

---

## 8. State Ownership Tests

Verify that disposing a UI binding:

```text
removes the subscription
```

without:

```text
disposing externally owned State<T>
```

This distinction is fundamental.

---

## 9. BindingScope Tests

Required tests:

```text
register one subscription
dispose
subscription removed

register multiple resources
dispose all

double dispose
safe

resource cleanup throws
remaining cleanup attempted where contract requires
```

---

## 10. WidgetNode Lifecycle Tests

Every core runtime node should satisfy generic lifecycle tests.

Required:

```text
Created
↓
Mount
↓
Mounted
↓
Unmount
↓
Disposed
```

Verify invalid transitions.

---

## 11. Double Mount Test

Given one WidgetNode:

```text
Mount
Mount again
```

Expected:

```text
descriptive lifecycle failure
```

No duplicate native hierarchy should be created.

---

## 12. Double Disposal Test

If disposal is intended to be idempotent:

```text
Dispose
Dispose
```

must not throw or corrupt runtime ownership.

---

## 13. Use-After-Dispose Test

After final disposal:

```text
attempt update
attempt child mount
attempt native mutation through runtime node
```

must be prevented where the API permits detection.

---

## 14. Parent/Child Ownership Tests

Given:

```text
ParentNode
└── ChildNode
```

unmounting Parent must:

```text
unmount Child
dispose Child resources
detach child native UI
```

automatically.

---

## 15. Ownership Violation Test

A child node must not be owned by two parents simultaneously.

Attempting this should fail clearly.

---

## 16. Mount Rollback Tests

Mount failure is a critical architectural test.

Example:

```text
Parent
├── Child A mounts
├── Child B mounts
└── Child C throws
```

After failure:

```text
A cleaned
B cleaned
C not active
parent not active
native host clean
subscriptions removed
```

---

## 17. Rollback Failure Tests

If cleanup itself fails during rollback, verify:

```text
original mount failure remains primary
remaining cleanup continues where possible
secondary error is retained/reported
```

This follows ADR-018.

---

## 18. Native Hierarchy Tests

Mounted Widgets should create expected native element types.

Examples:

```text
Text
→ Label

Button
→ UnityEngine.UIElements.Button

Column
→ VisualElement with vertical flex semantics
```

Tests should not depend excessively on private internal wrappers.

---

## 19. Native Detach Tests

Unmounting a mounted subtree must remove only the native hierarchy owned by that subtree.

It must not:

```text
clear unrelated siblings
clear external root
remove another mount
```

---

## 20. Multiple Root Mount Test

Given:

```text
root
├── mount A
└── mount B
```

dispose A.

Verify:

```text
B remains mounted
B remains reactive
B native hierarchy remains intact
```

---

## 21. Native Borrowed Element Test

Given:

```text
Native(externalElement)
```

mount and unmount.

Verify:

```text
element detached
element not destroyed
element remains reusable externally
```

---

## 22. Existing Parent Native Test

If a borrowed VisualElement already has a parent:

```text
Native(element)
```

should fail according to ADR-012.

Verify the original hierarchy remains unchanged.

---

## 23. BuildContext Tests

BuildContext scoping must be tested independently from VisualElement hierarchy.

Required:

```text
root Theme A
child inherits A

nested Theme B
descendant resolves B

sibling outside nested scope resolves A
```

---

## 24. Structural Context Node Test

A provider with no dedicated native VisualElement must still create the correct context scope.

This proves context follows WidgetNode semantics rather than native parent traversal.

---

## 25. Multiple Context Tree Test

Two separate LumaFlow mounts must be able to use:

```text
Theme A
Theme B
```

simultaneously without global interference.

---

## 26. Context Lifetime Test

After a context-owning subtree unmounts:

```text
context dependents removed
subscriptions cleaned
```

No descendant should continue receiving contextual updates.

---

## 27. Theme Resolution Tests

Verify precedence:

```text
explicit Widget override
>
component theme
>
global semantic token
>
framework default
```

for representative components.

---

## 28. Nested Theme Tests

Nested theme must only affect its subtree.

No global mutation is allowed.

---

## 29. Theme Update Tests

If theme switching is implemented reactively:

```text
Theme A
→
Theme B
```

verify:

```text
mounted dependent components update
explicit overrides remain
unrelated component state remains
```

---

## 30. Style Mapping Tests

Required basic mappings:

```text
padding
margin
width
height
min/max size
background
border
radius
opacity
text styling
```

Map typed values into native style values.

---

## 31. Style Update Tests

Previous:

```text
padding = 16
background = A
```

Next:

```text
padding = 16
background = B
```

Verify background changes correctly.

Where instrumentation exists, verify unchanged padding is not redundantly reapplied.

---

## 32. Style Clearing Tests

Previous:

```text
width = 300
```

Next:

```text
width = unset
```

Verify the native inline override is actually cleared.

This test is mandatory because stale inline styles are easy to introduce.

---

## 33. USS Interop Tests

Verify:

```text
user classes preserved
framework class updates do not delete user classes
unset typed values allow USS/native behavior
```

where possible.

---

## 34. Layout Mapping Tests

Required mappings:

```text
Row
Column
MainAxisAlignment
CrossAxisAlignment
Expanded
Spacer
Padding
SizedBox
Stack
Positioned
```

Tests should verify native layout configuration rather than duplicating Yoga's own layout engine tests.

---

## 35. Do Not Re-Test Yoga

LumaFlow does not need comprehensive tests for:

```text
how Flexbox itself computes every geometry case
```

Unity owns that behavior.

LumaFlow tests whether its semantic API maps correctly to native properties.

---

## 36. Representative Geometry Tests

A limited set of integration tests may verify final resolved geometry for important compositions.

Examples:

```text
Row with Expanded
centered dialog
sidebar + content
Stack badge
```

This catches incorrect native mapping.

---

## 37. Reactive Property Tests

For a simple reactive property:

```text
State<string>
↓
Text
```

change State.

Verify:

```text
same TextNode remains mounted
same Label remains
text changes
```

No structural rebuild should occur.

---

## 38. Structural Rebuild Tests

For:

```text
ReactiveBuilder
```

change structural State.

Verify:

```text
boundary node remains
old child subtree unmounted
new child subtree mounted
outside siblings untouched
```

---

## 39. State Preservation Tests

State owned above a structural rebuild boundary must survive child replacement.

This validates ADR-003 and ADR-004 together.

---

## 40. StatefulView Tests

Once StatefulView is finalized, test:

```text
local state created per mount
two mounts do not share local state
local state cleaned on unmount
```

This is mandatory before StatefulView API becomes stable.

---

## 41. Button Tests

Representative component tests:

```text
native Button created
label configured
onPressed invoked
disabled state prevents semantic action as expected
theme applied
callback removed on unmount
```

---

## 42. TextField Controlled Binding Tests

Critical two-way test:

```text
State<string> = "A"
↓
field displays A
```

User/native change:

```text
field → "B"
↓
State becomes B
```

External state change:

```text
State → "C"
↓
field becomes C
```

No feedback loop or duplicate notification should occur.

---

## 43. SetValueWithoutNotify Tests

Where native controls require silent assignment, tests should verify external state synchronization does not recursively invoke user change handlers.

---

## 44. Controlled vs Uncontrolled Tests

If a component supports both modes:

```text
controlled
uncontrolled
```

they must be tested separately.

Behavior must not silently switch ownership modes.

---

## 45. ListView Virtualization Tests

Required:

```text
native ListView backing
host creation
item binding
rebind
unbind cleanup
item callback correctness
list disposal
```

This is one of the highest-risk components.

---

## 46. Stale Subscription List Test

Host binds item A.

Then host binds item B.

Change State owned by A.

Verify:

```text
host displaying B does not update
```

Then change B.

Verify correct update.

---

## 47. Stale Callback List Test

Host binds A with button callback.

Rebind B.

Click button.

Expected:

```text
callback receives B
```

not A.

---

## 48. Large Collection Test

Use a synthetic data set such as:

```text
10,000 items
```

Verify the number of mounted row roots does not scale linearly with total collection size.

The exact number may depend on native ListView behavior.

The test should assert an upper bound appropriate to the visible/recycle range, not one exact implementation count.

---

## 49. Navigation Tests

Once navigation exists:

```text
initial destination
Push
Pop
Replace
CanPop
nested Navigator
independent Navigators
```

must be covered.

---

## 50. Navigation Lifecycle Test

Push B from A.

Then Pop B.

Verify all B-specific:

```text
subscriptions
events
native hierarchy
overlay scopes if nested
```

follow documented cleanup policy.

---

## 51. Navigation Scope Test

A nested screen must resolve the nearest Navigator.

A sibling must resolve the outer Navigator.

---

## 52. Overlay Tests

Required:

```text
show
close
double close
modal barrier
nested OverlayHost
host teardown
anchor disappearance
```

---

## 53. Overlay Modal Input Test

Underlying control should not receive pointer interaction while a modal barrier is active.

After modal close, interaction must resume.

---

## 54. Overlay Host Teardown Test

Unmounting OverlayHost with several entries must close all owned entries and stop timers/subscriptions.

---

## 55. Tooltip/Toast Timing Tests

Once time-based overlay components exist, timing behavior should use controllable/testable scheduling where possible.

Avoid tests that rely on fragile real-time sleeps.

---

## 56. Error Tests

ADR-018 errors must be tested.

Examples:

```text
double mount
invalid layout configuration
missing required context
already-parented native element
```

Tests should verify correct exception category/type and meaningful identifying content.

---

## 57. Do Not Over-Test Exact Error Strings

Avoid brittle tests requiring every error message to match exactly.

Verify meaningful fragments and exception classification.

---

## 58. Diagnostics Deduplication Tests

If a warning is intended to occur once per node/mount:

```text
trigger condition repeatedly
```

verify it does not spam diagnostics.

---

## 59. Runtime Assembly Tests

Validate Runtime public API contains no UnityEditor types.

This may be implemented through:

```text
assembly metadata inspection
source validation
dependency validation
```

---

## 60. Editor Boundary Compile Test

A Runtime-only compilation target must succeed with:

```text
LumaFlow.Editor
```

excluded from the compilation.

---

## 61. Player Build Gate

A player build is stronger evidence than Editor compilation.

CI/release validation should build at least one actual player target with LumaFlow Runtime.

This detects:

```text
Editor type leaks
unsupported runtime APIs
assembly configuration errors
```

---

## 62. Editor Build Gate

Editor assemblies must also compile and their integration tests run.

Runtime compatibility must not come at the expense of broken Editor tooling.

---

## 63. Render Pipeline Tests

Core behavior should be validated under:

```text
Built-in
URP
HDRP
```

where practical.

The goal is primarily to ensure Core does not acquire pipeline-specific dependencies.

---

## 64. Pipeline Test Scope

Do not duplicate every UI test three times unless needed.

A reasonable matrix may use:

```text
full core tests once
+
smoke/build tests per render pipeline
```

unless a feature is known to interact with rendering.

---

## 65. Optional Effects Tests

Pipeline-specific Effects modules require their own pipeline-specific integration tests.

Core does not.

---

## 66. Unity Version Matrix

LumaFlow should maintain a tested Unity version matrix.

At minimum:

```text
minimum supported Unity version
current primary development version
```

Potentially:

```text
latest stable supported version
```

as resources permit.

---

## 67. Do Not Claim Untested Versions

Documentation should distinguish:

```text
supported and tested
expected to work
unsupported
```

Do not advertise compatibility based only on assumption.

---

## 68. Version Adapter Tests

If compatibility adapters exist:

```text
minimum supported Unity version path
newer Unity path
```

must both be tested where feasible.

---

## 69. Platform Matrix

Runtime platform validation may include representative targets such as:

```text
Windows/macOS/Linux desktop
WebGL
Android/iOS
```

according to actual support commitments.

Full matrix depth depends on available CI infrastructure.

---

## 70. Core vs Platform-Specific Testing

Most Core correctness can be tested once.

Platform-specific builds primarily verify:

```text
compilation
AOT
package compatibility
runtime initialization
```

unless behavior is known to differ.

---

## 71. IL2CPP/AOT Validation

Because Core should remain AOT-friendly, at least one IL2CPP build should eventually be part of release validation where infrastructure permits.

This is particularly important before stable 1.0 claims.

---

## 72. Reflection Regression Gate

Core implementation should not accidentally gain mandatory runtime reflection/code generation.

This may be validated by code review/static checks and AOT builds.

---

## 73. Package Installation Test

A clean test project should be able to install LumaFlow through its intended UPM mechanism and compile.

This catches package metadata errors not visible inside the source repository.

---

## 74. Clean Project Test

Do not validate only inside the LumaFlow development repository.

A consumer-like project is important because it reveals:

```text
missing asmdef references
missing package dependencies
incorrect Samples paths
internal asset assumptions
```

---

## 75. Runtime Smoke Scene

Maintain a minimal Runtime smoke sample/project containing:

```text
UIDocument
LumaFlow mount
Text
Button
State
layout
```

as a build validation target.

---

## 76. Editor Smoke Window

Maintain a minimal Editor test window containing:

```text
LumaFlow mount
shared Runtime widgets
Editor-only integration
cleanup
```

This validates Editor adoption independently from AudioLib.

---

## 77. Dogfooding

AudioLib remains a primary real-world dogfood target.

It should test:

```text
complex layout
theming
lists
inputs
EditorWindow lifecycle
native interop
context menus/dialogs
reactive state
```

Real product pressure is valuable evidence.

---

## 78. Dogfooding Is Not a Substitute for Tests

AudioLib working correctly does not prove:

```text
double mount behavior
rare cleanup paths
multiple independent mount isolation
player compatibility
```

Automated tests remain required.

---

## 79. Component Gallery

A future component gallery/sample application should render framework components in representative states.

Examples:

```text
normal
hover
disabled
error
focused
compact
dark theme
light theme
```

This is useful for visual regression and manual review.

---

## 80. Visual Regression Tests

Automated screenshot comparison may eventually be useful.

However it is not the first testing priority.

Rendering can vary across:

```text
Unity versions
OS
font rasterization
graphics backends
```

Lifecycle and semantic correctness are more stable early targets.

---

## 81. Golden Tests

If screenshot/golden tests are introduced:

```text
use them selectively
```

for stable component surfaces.

Do not make the entire test suite fragile to one-pixel rendering differences.

---

## 82. Interaction Tests

Representative interaction flows should include:

```text
click button
edit text
toggle checkbox
scroll list
open dialog
navigate
```

where Unity test infrastructure allows deterministic input simulation.

---

## 83. Focus Tests

Once keyboard/focus features stabilize, add integration tests for:

```text
focusable controls
tab navigation
modal focus behavior
focus after list recycling
```

Do not assume pointer-only UI.

---

## 84. Performance Tests

Performance is part of framework correctness, but optimize measured bottlenecks.

Useful benchmarks include:

```text
mount 1,000 simple widgets
state update throughput
large ListView scrolling
theme switch
structural subtree rebuild
```

---

## 85. Performance Baselines

Store meaningful baselines where practical.

A regression should be investigated when a release significantly changes:

```text
mount time
GC allocations
list scrolling cost
state update cost
```

---

## 86. Do Not Test FPS Alone

Frame rate is influenced by too many external factors.

Prefer more direct metrics such as:

```text
elapsed operation time
allocation count
mounted node count
native element count
```

when measuring framework behavior.

---

## 87. Allocation Tests

High-value allocation checks include:

```text
simple State property update
list row recycling
theme update
```

Ordinary reactive property updates should ideally avoid unnecessary WidgetNode allocation.

---

## 88. Stress Tests

Stress tests may repeatedly:

```text
mount/unmount trees
push/pop routes
open/close overlays
rebind lists
change themes
```

to expose leaks and stale subscriptions.

---

## 89. Leak Detection

Tests should verify objects become collectible where practical.

Examples:

```text
mounted node
BindingScope
View model captured only by disposed callback
```

WeakReference-based tests may be useful selectively.

---

## 90. GC Tests Are Delicate

Garbage collection tests can be nondeterministic.

Use them only where ordinary ownership assertions cannot detect retention.

Do not rely on GC tests as the only leak detection strategy.

---

## 91. Static State Tests

Multiple independent test runs/mounts should reveal accidental static mutable state.

Tests should deliberately create:

```text
two themes
two navigators
two overlay hosts
two windows
```

simultaneously.

---

## 92. Test Isolation

Each test must clean mounted UI/resources it creates.

Framework tests themselves should not leave static state that contaminates later tests.

---

## 93. Deterministic Scheduling

Features using timers/animation should eventually abstract scheduling enough to test without long wall-clock delays.

Do not introduce a broad scheduler abstraction until needed.

---

## 94. Async Tests

Async framework features require cancellation/teardown tests.

Do not consider an async feature stable merely because the successful path works.

---

## 95. Compile-Time API Tests

Some public API ergonomics can be validated through sample compilation.

Example target usages:

```csharp
Column(
    gap: 16,
    children:
    [
        Text("Settings"),
        Button("Save", onPressed: Save)
    ]
);
```

should compile under the supported C# version.

---

## 96. API Usage Fixtures

Maintain small representative source files showing intended consumer syntax.

They serve as:

```text
compile tests
documentation examples
regression guards
```

---

## 97. Public API Snapshot

As the project approaches 1.0, consider tracking the public API surface.

This helps detect accidental breaking changes.

Before 1.0, deliberate breaking changes are allowed but should still be visible.

---

## 98. Test Internal Implementation Sparingly

Internal tests are appropriate for critical infrastructure such as:

```text
WidgetNode transitions
BindingScope
style mapper
```

But most component behavior should be tested through observable native/runtime results.

---

## 99. Mocking Policy

Prefer lightweight fakes over extensive mocking.

LumaFlow's architecture should not require mocking dozens of services to mount a Button.

---

## 100. Native UI Toolkit in Tests

Where native integration matters, use real `VisualElement` instances.

Mocking `VisualElement` would fail to test the actual integration layer.

---

## 101. Test Helpers

LumaFlow tests may provide reusable helpers such as:

```text
TestMount
AssertNodeDisposed
AssertNativeChildCount
CreateTestContext
```

to keep tests readable.

These helpers belong to test assemblies.

---

## 102. TestMount

A useful helper may:

```text
create native root
mount Widget
return handle
expose safe diagnostic inspection
```

without bypassing normal public/runtime lifecycle.

---

## 103. Do Not Create Test-Only Runtime Semantics

Test helpers must use the same lifecycle rules as production.

Avoid hidden APIs that make tests pass while real mounts behave differently.

---

## 104. Development Invariant Validation

Tests may enable expensive runtime invariant checks automatically.

This is a good environment for:

```text
tree ownership verification
binding leak assertions
native mapping consistency
```

even if disabled by default in production.

---

## 105. CI Stages

A mature CI pipeline may conceptually contain:

```text
1. format/static checks
2. compile
3. Runtime tests
4. Editor tests
5. package validation
6. player build
7. compatibility matrix
8. optional performance checks
```

Not every stage must exist on day one.

---

## 106. Fast Pull Request Gate

Normal pull requests should receive fast feedback.

Initial essential gate:

```text
compile
core tests
Editor tests
```

before slower platform builds.

---

## 107. Release Gate

A release should require stronger validation:

```text
all automated tests
clean package installation
Runtime player build
Editor smoke validation
supported Unity version checks
forbidden dependency checks
```

---

## 108. Release Blocking Failures

Examples that must block release:

```text
Runtime references UnityEditor
lifecycle regression
binding leak regression
player build failure
package cannot install cleanly
minimum supported Unity version fails
```

---

## 109. Warning-Level Release Issues

Some visual/performance concerns may initially be non-blocking if explicitly documented.

Do not downgrade correctness failures to warnings.

---

## 110. Minimum Supported Unity Version

Before publishing a firm minimum version:

```text
run test/build matrix
```

Do not choose the number only from documentation assumptions.

---

## 111. Compatibility Claims

Release documentation should state actual tested combinations.

Example structure:

```text
Unity X.Y — supported/tested
Unity Z.Y — supported/tested
Earlier versions — not supported
```

Exact versions are defined outside this ADR.

---

## 112. Pipeline Independence Gate

A CI/static validation should ensure Core does not reference:

```text
UnityEngine.Rendering.Universal
UnityEngine.Rendering.HighDefinition
```

unless inside isolated optional modules.

---

## 113. Editor Boundary Gate

Similarly Core/Runtime must not reference:

```text
UnityEditor
```

This can be automated.

---

## 114. Dependency Graph Gate

Validate asmdefs form the intended acyclic dependency graph.

Forbidden:

```text
Runtime → Editor
Core → optional pipeline module
dependency cycle
```

---

## 115. Package Manifest Validation

Validate:

```text
package.json
asmdefs
Samples~
Documentation~
LICENSE
README
```

where applicable before release.

---

## 116. Documentation Examples Must Compile

High-value public documentation snippets should periodically be compiled as fixtures or copied from tested sample code.

This prevents documentation drifting away from API reality.

---

## 117. Example Code as Contract

If documentation repeatedly teaches:

```csharp
Text("Hello")
```

then that syntax becomes an important usability contract even before 1.0.

Tests should preserve intentional examples.

---

## 118. Regression Policy

Every important discovered framework bug should receive a regression test where practical.

Especially:

```text
lifecycle leaks
list recycling bugs
style clearing bugs
context scope bugs
build compatibility bugs
```

---

## 119. Bug Fix Definition

A bug fix is not considered complete when only:

```text
the example appears to work
```

The preferred completion state is:

```text
bug reproduced by test
↓
implementation fixed
↓
test passes
```

where the defect is automatable.

---

## 120. Architecture Test Coverage

ADRs create invariants.

Tests should explicitly cover those invariants.

Examples:

```text
ADR-007:
Widget description does not represent mount identity

ADR-008:
subscriptions cleaned on unmount

ADR-011:
nearest context scope wins

ADR-014:
old item bindings do not survive recycle

ADR-017:
Runtime has no Editor dependency
```

---

## 121. ADR-to-Test Traceability

Tests do not need literal ADR numbers in every method name.

However, architecture-critical test suites should make it easy to understand which contract they protect.

---

## 122. Test Naming

Prefer behavior names.

Example:

```text
Unmount_RemovesStateSubscription
```

over:

```text
TestCase17
```

---

## 123. Test Failure Quality

A failed test should indicate:

```text
expected architectural behavior
actual result
relevant component
```

Test helpers should not hide useful failure information.

---

## 124. No Flaky Tests Accepted Casually

Flaky lifecycle/timing tests reduce trust in the suite.

If a test flakes:

```text
identify deterministic synchronization
or remove/rewrite the test
```

Do not normalize repeated retries as the permanent solution.

---

## 125. Test Time

Core unit/runtime tests should remain fast enough for frequent local execution.

Slow matrix/build tests belong in CI/release workflows.

---

## 126. Local Developer Workflow

A developer should be able to run:

```text
core tests
targeted component tests
Editor tests
```

without building every platform.

---

## 127. Codex Workflow

Before implementing a feature, Codex should inspect:

```text
relevant ADR
existing tests
related runtime helpers
```

Before declaring completion, Codex should:

```text
add/update relevant tests
run the narrowest relevant suite
run broader compilation/validation if architecture changed
```

---

## 128. Codex Must Not Remove Tests to Make Code Pass

If an existing test protects an accepted ADR invariant:

```text
fix implementation
```

rather than weakening/removing the test.

If the architecture itself changes, update ADR first or explicitly document the decision.

---

## 129. Codex Must Add Regression Tests

When fixing:

```text
lifecycle bug
binding bug
native ownership bug
context bug
list recycling bug
```

add a regression test unless technically impossible.

---

## 130. Test Coverage Percentage

LumaFlow will not use one global coverage percentage as the definition of quality.

A framework may have:

```text
90% line coverage
```

and still fail to test lifecycle semantics.

Coverage metrics may be informative but are not the primary acceptance criterion.

---

## 131. Critical Path Coverage

More important than raw line coverage is explicit coverage of:

```text
mount
unmount
failure rollback
state update
structural rebuild
native ownership
list recycle
context scope
```

---

## 132. Manual Validation

Some behavior still requires manual review.

Examples:

```text
feel of controls
visual hierarchy
focus behavior
Editor usability
responsive composition
```

Manual validation complements automated tests.

---

## 133. Manual Checklist

Before major releases, manually inspect representative:

```text
Runtime sample
Editor sample
AudioLib UI
```

under supported themes/environments.

---

## 134. Dogfood Bug Priority

A bug discovered in real AudioLib usage that violates a Core invariant is treated as a framework bug, not patched locally in AudioLib.

Fix the reusable infrastructure.

---

## 135. No Dogfood-Specific Hacks

Avoid:

```text
if (AudioLibWindow)
```

or application-specific branches inside Core.

Dogfooding should reveal missing abstractions, not contaminate them.

---

## 136. Performance Dogfood

AudioLib large lists and editor workflows should be used to observe:

```text
mount allocations
list scroll performance
theme update cost
window reopening/closing
```

---

## 137. Memory Dogfood

Repeatedly open and close an AudioLib window.

Memory/subscription counts should not continuously grow due to LumaFlow lifecycle leaks.

---

## 138. Runtime Sample Memory Test

Similarly, repeatedly enable/disable Runtime hosts to validate MountHandle teardown.

---

## 139. Build-Time Warnings

New LumaFlow-related compile warnings should be investigated.

Do not allow warnings to accumulate until important compatibility warnings become invisible.

---

## 140. Warnings-as-Errors

Core library compilation may eventually enable strict warning policies where practical.

Do not enable a policy that produces excessive noise from Unity-generated/external code.

---

## 141. Static Analysis

Optional analyzers may later help detect:

```text
BuildContext stored in fields
Runtime → Editor reference
invalid custom Widget patterns
```

Do not require custom analyzers for basic correctness initially.

---

## 142. Source Generator Testing

If optional source generation is ever introduced, it requires independent compile tests and generated API snapshot tests.

Core must still work without it.

---

## 143. Test Data

Use simple deterministic fake models.

Avoid requiring large Unity asset projects for ordinary component tests.

---

## 144. Asset-Based Tests

Where real assets are necessary:

```text
VisualTreeAsset
StyleSheet
VectorImage
```

keep minimal test fixtures under controlled test resources.

---

## 145. Clean Repository Tests

Tests should not depend on the developer's local:

```text
AssetDatabase content
Editor preferences
scene state
cached assets
```

unless the test explicitly creates/configures them.

---

## 146. Parallel Tests

Do not assume tests can safely share global LumaFlow mutable state.

Ideally there should be little or no such state.

If test parallelism exposes interference, treat that as possible architecture smell.

---

## 147. Multiple Panel Testing

Where practical, create multiple independent root trees/panels in tests.

This is valuable for catching accidental global state.

---

## 148. Editor Theme Testing

Editor theme adapters should be tested separately from ThemeData Core.

Core Theme tests remain environment-neutral.

---

## 149. List Performance Gate

Before claiming ListView production readiness, verify with a large collection that:

```text
native host count remains bounded
no stale binding defects occur
scrolling does not allocate catastrophically
```

Exact numeric thresholds should be based on measurements.

---

## 150. Navigation Readiness Gate

Before navigation becomes stable public API:

```text
nested navigator tests
lifecycle tests
retention policy tests
focus/scroll implications documented
```

must exist.

---

## 151. Overlay Readiness Gate

Before overlay becomes stable:

```text
scope resolution
host teardown
modal input blocking
reentrant close
anchor removal
```

must be proven.

---

## 152. StatefulView Readiness Gate

Do not stabilize StatefulView API until tests prove:

```text
per-mount state isolation
cleanup
rebuild semantics
multiple mounts
```

---

## 153. Public Extension API Readiness Gate

Before exposing low-level custom Widget authoring:

```text
test third-party-like custom native widget
test lifecycle helpers
test context usage
test cleanup
```

The extension API must be proven outside built-in components.

---

## 154. Release Candidate Validation

A release candidate should ideally pass:

```text
Core tests
Editor tests
player build
package install test
minimum Unity version
primary Unity version
Runtime smoke sample
Editor smoke sample
```

Additional matrix targets may be layered according to release maturity.

---

## 155. Pre-1.0 Policy

Before 1.0, API changes are expected.

Tests should make changes explicit rather than preventing all breakage.

Architectural invariants, however, should not regress casually.

---

## 156. 1.0 Quality Bar

1.0 should require:

```text
stable lifecycle model
stable ownership model
reliable bindings
tested component core
documented compatibility
clean Runtime/Editor boundary
dogfooded Editor UI
working Runtime sample
release CI gates
```

not merely a large Widget count.

---

## 157. Rejected Alternative: Manual Testing Only

Rejected.

Lifecycle and recycling defects are too subtle to rely on visual inspection.

---

## 158. Rejected Alternative: Unit Tests Only

Rejected.

Many important contracts exist only when mounted into real UI Toolkit elements.

---

## 159. Rejected Alternative: Screenshot Tests as Primary Validation

Rejected.

They are useful later for visual regressions but do not prove lifecycle, ownership, state, or dependency correctness.

---

## 160. Rejected Alternative: AudioLib Is the Test Suite

Rejected.

Dogfooding provides valuable integration pressure but cannot systematically exercise edge cases.

---

## 161. Rejected Alternative: Every Unity Version Gets Full Matrix Every Commit

Rejected initially due to CI cost.

Use sensible fast and release matrices.

---

## 162. Rejected Alternative: Raw Coverage Percentage as Quality Gate

Rejected as the primary success criterion.

Architectural behavior matters more than raw executed-line percentage.

---

## 163. Architectural Invariants Created by This ADR

Unless superseded:

### Invariant 1

Core lifecycle behavior has automated tests.

### Invariant 2

Bindings are tested for cleanup.

### Invariant 3

Context scoping is tested with nested scopes.

### Invariant 4

Native ownership is tested.

### Invariant 5

List recycling tests explicitly detect stale subscriptions/callbacks.

### Invariant 6

Runtime/Editor boundaries are validated automatically.

### Invariant 7

At least one real player build is part of serious release validation.

### Invariant 8

Compatibility claims are based on testing rather than assumption.

### Invariant 9

Important framework bugs receive regression tests where practical.

### Invariant 10

Dogfooding complements automated testing but does not replace it.

---

## 164. Codex Rules

### Rule 1

Before changing architecture-critical runtime code, inspect its existing tests.

### Rule 2

Add tests for new lifecycle/state/ownership semantics.

### Rule 3

When fixing a reproducible framework bug, add a regression test.

### Rule 4

Do not remove an architecture-protecting test simply because new implementation fails it.

### Rule 5

Use real VisualElements in native integration tests where practical.

### Rule 6

Do not rely exclusively on screenshots for correctness.

### Rule 7

Validate cleanup after failed mounts, not only successful paths.

### Rule 8

If Runtime assembly dependencies change, run the Runtime/Editor boundary checks.

### Rule 9

If public package structure changes, perform a clean-package installation/build validation.

### Rule 10

Do not claim a feature complete when only the happy path has been tested.

---

## 165. Decision Test

When deciding how to test a feature:

```text
Is this pure value/configuration logic?
        ↓ yes
Unit test.

Does it involve WidgetNode lifecycle?
        ↓ yes
Mounted runtime test.

Does it depend on VisualElement behavior?
        ↓ yes
Use real UI Toolkit integration.

Does it depend on UnityEditor?
        ↓ yes
Editor test assembly.

Could it fail only in a player/build?
        ↓ yes
Add build/compatibility validation.

Did a bug escape existing tests?
        ↓ yes
Add a regression test at the lowest useful layer.
```

---

## 166. Initial Phase 1 Test Target

Before expanding Core significantly, Phase 1 should have tests for:

```text
State<T>
BindingScope
WidgetNode mount/unmount
double mount
MountHandle disposal
parent/child ownership
mount rollback
Text native mapping
Button native mapping
reactive Text update
BuildContext Theme scope
Native borrowed element lifecycle
Runtime assembly boundary
```

This is the minimum foundation.

---

## 167. Phase 2+ Expansion

As components are added, extend tests alongside:

```text
layout
styling
inputs
ListView
Navigator
Overlay
Editor adapters
```

Do not postpone all testing until the framework is feature-complete.

---

## 168. Final Decision

LumaFlow validation is based on:

```text
small deterministic unit tests
+
real mounted UI Toolkit integration tests
+
explicit lifecycle and leak tests
+
Editor-specific tests
+
player/build compatibility gates
+
real-world dogfooding
```

The guiding rule is:

**If an architectural invariant matters enough to document, it should be tested wherever technically practical.**