# FILE: docs/development/ACCESSIBILITY.md

# LumaFlow Accessibility Policy

This document defines accessibility constraints that must influence LumaFlow component design from the beginning.

LumaFlow does not attempt to implement a separate accessibility engine.

Its responsibility is to preserve and improve the semantic behavior available through native Unity UI Toolkit and to avoid architectural decisions that make accessible UI unnecessarily difficult.

---

# 1. Core Principle

LumaFlow must never intentionally make accessibility worse than equivalent native UI Toolkit usage.

When native UI Toolkit provides:

```text
semantic control behavior
focus behavior
keyboard behavior
accessibility metadata
```

LumaFlow should preserve it.

---

# 2. Native Semantic Controls First

Prefer native semantic controls over generic `VisualElement` replacements.

Correct foundation:

```text
Button
→ UI Toolkit Button

TextField
→ UI Toolkit TextField

Toggle
→ UI Toolkit Toggle
```

Avoid rebuilding standard controls from generic VisualElements solely for visual customization.

Native controls provide behavior that custom containers would otherwise need to reproduce.

---

# 3. Keyboard Is First-Class Input

LumaFlow components must not assume pointer-only interaction.

Interactive components should support native keyboard interaction where the underlying UI Toolkit control supports it.

Examples include:

```text
Tab focus
Enter/Space activation
arrow navigation where appropriate
Escape/back semantics
```

---

# 4. Focus Semantics

LumaFlow must preserve native focus behavior.

Do not arbitrarily make:

```text
layout containers
decorative wrappers
theme providers
structural nodes
```

focusable.

Focusability should correspond to actual interaction semantics.

---

# 5. Focus Visibility

Themes must be able to provide a visible focused state for interactive controls.

Do not design components where focus indication is impossible to style.

A keyboard user must be able to determine which control currently owns focus.

---

# 6. Modal Focus

Modal overlays must prevent focus from remaining meaningfully active in inaccessible background UI.

A future Dialog implementation should preserve native focus behavior and, where reliable, support focus containment/restoration.

---

# 7. Inactive Navigation Routes

Inactive routes must not remain keyboard-focusable.

Navigation retention must not expose hidden controls through focus traversal.

---

# 8. Disabled Semantics

Disabled is a semantic interaction state.

Do not represent disabled controls only through visual styling while leaving them interactive.

Likewise, do not represent ordinary disabled state by removing the element unless removal is semantically intended.

---

# 9. Color Is Not the Only Signal

Framework component APIs/themes should not force color to be the sole representation of important state.

Examples:

```text
error
warning
selection
disabled
success
```

should be capable of using:

```text
text
icons
shape/border
native semantics
```

where appropriate.

---

# 10. Contrast

LumaFlow should not hard-code low-contrast visual assumptions into component behavior.

Actual contrast values belong to Theme/application design.

Framework defaults should aim for readable foreground/background relationships.

---

# 11. High-Contrast Themes

Theme architecture must permit applications to provide high-contrast variants.

Components should consume semantic Theme tokens rather than embedded literal colors.

---

# 12. Text Scaling

Components must avoid unnecessary assumptions that text has one fixed physical size.

Layouts should tolerate larger text where UI Toolkit layout allows it.

Avoid fixed heights that clip labels merely because they fit the default font size.

---

# 13. Long Text

Components should be tested with longer labels than typical English UI strings.

Example:

```text
Save
```

must not be the only test case.

Use representative long strings when testing:

```text
buttons
settings labels
dialogs
menus
forms
```

---

# 14. Localization-Friendly Layout

Layout APIs should avoid permanently encoding left/right semantics where logical start/end semantics are more appropriate.

Future RTL/localization support must remain architecturally possible.

---

# 15. RTL

Initial LumaFlow does not promise complete right-to-left support unless UI Toolkit capabilities are sufficient and tested.

However Core APIs must avoid unnecessary architectural barriers to future RTL support.

---

# 16. Icon-Only Controls

Icon-only interactive components should support a semantic label/tooltip mechanism where native accessibility capabilities permit it.

Visual icon recognition alone should not be assumed.

---

# 17. Tooltip Is Not Primary Information

Information required to understand or operate the interface should not exist exclusively in a hover-only Tooltip.

Tooltips are supplementary.

This is especially important for:

```text
touch
keyboard
accessibility technology
```

---

# 18. Pointer Target Size

LumaFlow should not impose excessively small default interactive targets.

Exact target sizing belongs to design-system/theme policy and platform context.

Core components must allow applications to provide suitable sizing.

---

# 19. Touch Support

Interactive APIs should not require mouse-hover behavior for basic functionality.

Hover may enhance UI, but core interaction must remain possible on touch-oriented platforms where the component is intended to work.

---

# 20. Motion

Animation must not become necessary to understand state changes.

A future motion system should allow:

```text
reduced motion
disabled motion
shorter motion
```

through Theme/context policy.

Core component correctness must not depend on animation completing visually.

---

# 21. Reduced Motion

When motion infrastructure is implemented, reduced-motion preference should be representable as tree-scoped UI context or Theme configuration.

Exact API is deferred.

Do not hard-code transitions that cannot be disabled.

---

# 22. Screen Readers and Native Accessibility

Unity/UI Toolkit accessibility capabilities vary by Unity version and platform.

LumaFlow must not claim screen-reader functionality beyond what is actually supported and tested.

Where native semantic APIs exist, wrappers should preserve/expose them.

---

# 23. Accessibility Capability Policy

Documentation must distinguish:

```text
supported by LumaFlow
supported through native UI Toolkit
platform-limited
not currently available
```

Do not advertise unsupported accessibility features.

---

# 24. Custom Native Widgets

A custom native-backed Widget should preserve:

```text
focus
keyboard interaction
semantic role
enabled state
```

where applicable.

Custom visual rendering is not sufficient for interactive component quality.

---

# 25. Layout Widgets

Pure layout Widgets such as:

```text
Row
Column
Padding
Spacer
Container
```

should remain non-interactive unless explicitly configured otherwise.

They should not alter focus order unnecessarily.

---

# 26. ListView

Virtualized lists must preserve native selection/focus behavior where possible.

Recycling must not cause stale focus semantics or callbacks to refer to the wrong logical item.

---

# 27. Error UI

Validation and error components should support more than color-only representation.

For forms, error text should be possible.

Exact form semantics are deferred until input components mature.

---

# 28. Editor UI

Accessibility principles apply to LumaFlow Editor UI as well.

Editor integrations should preserve Unity Editor keyboard/focus behavior instead of replacing it with pointer-only custom interactions.

---

# 29. Testing Strategy

Accessibility-sensitive tests should eventually include:

```text
focusability of standard controls
layout wrappers are not focusable
disabled controls are not interactive
inactive route focus isolation
modal focus isolation
keyboard activation
long text
larger typography
```

Exact automation depends on Unity test capabilities.

---

# 30. Manual Accessibility Review

Some behavior requires manual review.

Representative components should periodically be tested using:

```text
keyboard only
large text/theme
high-contrast theme
touch-oriented interaction where applicable
```

---

# 31. Component Review Checklist

Before stabilizing an interactive component:

```text
[ ] Does it use an appropriate native semantic control?
[ ] Can it be reached by keyboard where appropriate?
[ ] Is focused state styleable/visible?
[ ] Does disabled state disable interaction?
[ ] Does it work without hover-only behavior?
[ ] Does long text break it?
[ ] Can Theme provide sufficient contrast?
[ ] Is important meaning conveyed by more than color where appropriate?
```

---

# 32. Codex Rules

Codex must:

1. prefer semantic native controls;
2. avoid making decorative/layout elements focusable;
3. preserve native keyboard behavior;
4. avoid pointer-only foundational interactions;
5. not encode important meaning exclusively through color;
6. avoid fixed component geometry that unnecessarily clips larger text;
7. preserve future RTL/localization flexibility;
8. keep motion optional;
9. not claim accessibility capability that is not tested;
10. add accessibility-sensitive tests as corresponding components mature.

---

# 33. Final Principle

LumaFlow accessibility begins with architectural restraint.

The framework should preserve native semantics, keep keyboard/focus behavior intact, avoid visual-only assumptions, and leave applications enough flexibility to build accessible design systems.

The guiding rule is:

**Declarative convenience must not come at the cost of interaction semantics.**


---

# FILE: docs/development/PERFORMANCE_BUDGETS.md

# LumaFlow Performance Budgets and Benchmark Policy

This document defines how LumaFlow performance is measured and protected.

At the beginning of implementation, concrete numeric budgets are intentionally not invented.

Initial measurements establish baselines.

Budgets are introduced after representative implementation and hardware/environment measurements exist.

---

# 1. Core Principle

Performance decisions must be based on measurements.

LumaFlow should optimize:

```text
hot paths
common operations
large collections
frequent state updates
mount/unmount
```

without sacrificing lifecycle correctness or API clarity for hypothetical gains.

---

# 2. Initial Budget Status

Before the first working vertical slice:

```text
numeric performance budgets = TBD
```

This is intentional.

Do not invent target milliseconds without implementation data.

---

# 3. What Must Be Measured

The benchmark suite should eventually cover:

```text
State<T> mutation
State<T> notification
reactive Text update
mount simple Widget trees
unmount Widget trees
structural subtree replacement
Theme switch
controlled TextField synchronization
ListView bind/rebind/unbind
large ListView scrolling
Navigator Push/Pop
Overlay Show/Close
EditorWindow repeated mount/unmount
```

---

# 4. Primary Metrics

Prefer direct metrics such as:

```text
elapsed operation time
GC allocations
native VisualElement count
WidgetNode count
active subscription count
mounted list row count
```

Frame rate alone is not sufficient.

---

# 5. State<T> Benchmark

Measure:

```text
one subscriber
multiple subscribers
unchanged assignment
changed assignment
subscription disposal
```

Important dimensions:

```text
latency
allocations
notification count
```

---

# 6. Reactive Property Update

A simple reactive update such as:

```text
State<string>
↓
Text
↓
Label.text
```

should not allocate new WidgetNodes.

It should not remount the subtree.

Future target:

```text
0 Widget allocations per simple property update
```

GC byte targets are set after baseline profiling.

---

# 7. Mount Benchmark

Representative benchmark:

```text
mount 1 Widget
mount 100 Widgets
mount 1,000 simple Widgets
```

Measure:

```text
elapsed time
WidgetNode allocations
VisualElement allocations
temporary GC allocations
```

---

# 8. Unmount Benchmark

Measure deterministic cleanup cost for equivalent trees.

Also verify:

```text
active bindings after unmount = 0
framework-owned active nodes after unmount = 0
```

Performance results are invalid if lifecycle cleanup is incorrect.

---

# 9. Structural Rebuild Benchmark

Measure replacement of:

```text
small subtree
medium subtree
large localized subtree
```

through ReactiveBuilder or equivalent.

Verify unrelated surrounding nodes are not recreated.

---

# 10. Theme Switch Benchmark

Once dynamic theme updates exist, measure:

```text
small tree
representative form
large component tree
```

Metrics:

```text
elapsed time
style writes
allocations
number of affected nodes
```

Theme updates should not remount the entire application tree.

---

# 11. TextField Benchmark

Controlled TextField synchronization should measure:

```text
native → State
State → native
```

and verify no recursive change feedback.

Performance optimization must not weaken correctness.

---

# 12. ListView Benchmark

ListView requires dedicated benchmarking.

Use datasets such as:

```text
100 items
1,000 items
10,000 items
```

Metrics include:

```text
mounted row host count
bind calls
unbind calls
scroll allocations
frame-time spikes during representative scroll
stale subscription count
```

---

# 13. ListView Scaling Contract

The key architectural performance contract is:

```text
mounted row UI scales primarily with visible range + native recycle buffer
```

not:

```text
mounted row UI scales linearly with total item count
```

This is more important than any initial millisecond target.

---

# 14. ListView Memory Contract

Do not cache one detached WidgetNode subtree per item as a substitute for virtualization.

Memory usage should remain proportional to active/recycled native hosts plus application data.

---

# 15. Navigation Benchmark

Navigation stack depth is normally small.

Measure:

```text
Push
Pop
Replace
```

with representative screen sizes.

Push/Pop must not rebuild unrelated UI outside NavigatorHost.

---

# 16. Route Retention

If inactive routes remain mounted, measure:

```text
memory per retained route
reactive update work while inactive
focus/input isolation
```

Retention policy must be informed by real data.

---

# 17. Overlay Benchmark

Overlay operations should measure:

```text
Dialog Show/Close
Popover placement
Toast lifecycle
```

No per-frame full overlay-tree scanning should exist.

---

# 18. EditorWindow Stress Benchmark

A critical leak/performance scenario:

```text
repeat N times:
    mount Editor UI
    interact
    unmount/close
```

Measure:

```text
active WidgetNodes
active State subscriptions
registered Editor callbacks
memory trend
```

No monotonic growth caused by LumaFlow is acceptable.

---

# 19. Stress Iterations

Initial stress counts may be:

```text
100
1,000
```

depending on operation cost.

Exact counts should be chosen to expose lifecycle/performance regressions without making local test runs unreasonable.

---

# 20. No Per-Frame Global Work

Core must avoid architecture that performs work every frame merely because LumaFlow is mounted.

Examples to avoid:

```text
scan entire Widget tree
poll all State<T>
recalculate all styles
inspect all contexts
rebuild diagnostics
```

without an actual change.

---

# 21. Event-Driven Updates

Preferred performance architecture:

```text
State change
→ affected binding

Theme change
→ theme dependents

Navigation operation
→ Navigator subtree

Overlay operation
→ OverlayHost

List recycle
→ recycled host only
```

Work should be localized to the cause.

---

# 22. Allocation Policy

Do not require zero allocations everywhere.

Allocations are acceptable during:

```text
initial mount
Widget description construction
route creation
overlay creation
structural rebuild
```

where they are semantically expected.

High-frequency property updates should be more allocation-sensitive.

---

# 23. Hot Paths

Likely hot paths include:

```text
State notification
native property binding
TextField value synchronization
ListView recycling
style diff/update
```

These deserve profiling before complex optimization.

---

# 24. Cold Paths

Cold operations such as:

```text
package setup
initial diagnostics formatting on error
rare navigation reset
```

should prioritize clarity unless profiling proves otherwise.

---

# 25. Diagnostic Performance

Normal diagnostics must not introduce:

```text
per-frame allocations
continuous stack trace capture
constant tree serialization
```

Expensive inspection belongs behind explicit development tooling.

---

# 26. Style Performance

Style update infrastructure should eventually avoid reassigning every property when only one value changes.

Style diffing is local property diffing, not Widget reconciliation.

Benchmark before introducing complex caches.

---

# 27. Caching

Every cache must answer:

```text
what is expensive?
what is the cache key?
who owns the cache?
when is it invalidated?
what memory does it retain?
```

Do not introduce global caches without measured benefit.

---

# 28. Pooling

Do not pool WidgetNodes or ordinary components initially.

Pooling is justified only by profiling.

Pooling must not compromise:

```text
ownership
context correctness
subscription cleanup
debuggability
```

---

# 29. Benchmark Environment

Performance numbers must record relevant environment information:

```text
Unity version
LumaFlow version/commit
Editor or Player
Mono or IL2CPP
platform
development/release mode
hardware class
```

Without context, absolute numbers are misleading.

---

# 30. Editor vs Player

Editor measurements and player measurements are not directly interchangeable.

Use Editor benchmarks for development trends.

Use player builds for serious runtime performance claims.

---

# 31. Warmup

Benchmarks should include warmup where runtime/JIT/editor behavior makes it necessary.

Do not compare first-run initialization cost to warmed repeated operation without labeling the difference.

---

# 32. Repetition

Microbenchmarks should execute enough iterations to reduce noise.

Use median/percentiles where practical rather than trusting one measurement.

---

# 33. Regression Thresholds

Initial regression thresholds remain:

```text
TBD after baseline implementation
```

After baselines exist, define acceptable changes for high-value benchmarks.

Example categories:

```text
informational
warning
release-blocking
```

---

# 34. Performance Gate Philosophy

Not every small benchmark variation should fail CI.

Release-blocking gates should protect major regressions in important paths.

Avoid flaky performance CI.

---

# 35. Performance and Correctness

Never accept a performance optimization that causes:

```text
stale bindings
lifecycle leaks
incorrect context
lost callbacks
inconsistent native hierarchy
```

Performance tests supplement correctness tests.

They do not replace them.

---

# 36. Baseline Milestone

The first real baseline should be recorded after the first vertical slice supports:

```text
Widget
WidgetNode
MountHandle
BindingScope
State<T>
Text
Button
Column
```

with lifecycle tests passing.

---

# 37. First Baseline Set

Record at least:

```text
mount 1 Text
mount 100 Text
mount 1,000 Text

State<int> update with 1 listener
State<int> update with 100 listeners

reactive Text update

mount/unmount counter vertical slice repeatedly
```

---

# 38. Second Baseline Set

After Styling/Theme/TextField:

```text
theme switch
style update
style clearing
TextField controlled input
representative settings form mount
```

---

# 39. Third Baseline Set

After ListView:

```text
10,000 item list
scroll/recycle
rebind
selection
list teardown
```

This is a major production-readiness checkpoint.

---

# 40. Public Performance Claims

Do not publish marketing claims such as:

```text
zero allocation
blazing fast
faster than X
```

without reproducible benchmarks.

Prefer concrete architecture claims:

```text
simple State property changes update native properties without subtree rebuild
ListView uses native UI Toolkit virtualization
```

---

# 41. Performance Documentation

When numeric budgets are established, document:

```text
benchmark scenario
target
test environment
reason for target
```

Do not store unexplained numbers.

---

# 42. Codex Rules

Codex must:

1. not optimize before profiling unless removing an obvious pathological operation;
2. not introduce node/component pooling during initial Core;
3. not introduce global caches without measured benefit;
4. avoid per-frame global scans;
5. preserve local reactive updates;
6. preserve native ListView virtualization;
7. measure allocations in high-frequency paths when optimizing;
8. accompany complex optimizations with correctness regression tests;
9. update benchmark baselines when architecture intentionally changes;
10. never trade deterministic lifecycle for speculative speed.

---

# 43. Initial Budget Table

| Scenario | Metric | Initial Budget |
|---|---|---|
| `State<T>` changed value | latency / allocations | TBD after baseline |
| `State<T>` unchanged value | notifications | exactly 0 |
| Reactive `Text` update | WidgetNode allocations | 0 |
| Reactive `Text` update | subtree remount | forbidden |
| Mount 1,000 simple Widgets | time / allocations | TBD after baseline |
| Unmount mounted tree | remaining subscriptions | exactly 0 |
| Unmount mounted tree | remaining owned nodes | exactly 0 |
| Structural local rebuild | unrelated node remount | forbidden |
| 10k-item `ListView` | mounted UI count | proportional to visible/recycle range |
| List row rebind | stale subscriptions | exactly 0 |
| List row rebind | stale callbacks | exactly 0 |
| Runtime idle UI | full-tree per-frame work | forbidden |
| Repeated Editor mount/unmount | leaked framework subscriptions | exactly 0 |

---

# 44. Final Principle

LumaFlow performance policy is not:

```text
optimize everything
```

It is:

```text
measure
identify hot paths
protect architectural performance contracts
optimize proven bottlenecks
```

The guiding rule is:

**Correct localized work first.  
Measured optimization second.  
No hidden global cost.**