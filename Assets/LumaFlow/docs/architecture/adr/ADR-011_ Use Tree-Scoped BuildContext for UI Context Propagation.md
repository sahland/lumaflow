# ADR-011: Use Tree-Scoped BuildContext for UI Context Propagation

- **Status:** Accepted
- **Decision date:** 2026-08-11
- **Scope:** BuildContext, scoped values, theme propagation, navigation, environment data
- **Affects:** Runtime, WidgetNode, Theme, Navigation, Responsive UI, Localization, Testing
- **Related documents:** `ARCHITECTURE.md`, `ADR-003-reactive-state.md`, `ADR-005-theme-system.md`, `ADR-007-widget-runtime-model.md`, `ADR-008-lifecycle-and-ownership.md`

> **Implementation update (2026-08-20):** `Theme` and `MediaQuery` now use
> specialized dependency-aware scopes. Reads register the mounted `WidgetNode`;
> compatible provider updates notify only readers of that aspect; nested scopes
> are isolated; and unmount removes registrations. This deliberately follows
> sections 34–37 without introducing a generic service/provider registry.

---

## 1. Context

LumaFlow components often need access to information that depends on where they are mounted.

Examples include:

```text
Theme
Navigator
MediaQuery
Localization
Focus scope
UI density
platform/environment information
```

A common but problematic solution would be global singleton access:

```csharp
ThemeManager.Instance.CurrentTheme
Navigator.Instance
Localization.Instance
```

This creates several issues:

- multiple Editor windows become difficult;
- multiple runtime panels become difficult;
- nested themes are difficult;
- testing becomes harder;
- scoped overrides become difficult;
- hidden global dependencies appear;
- components become less reusable.

LumaFlow therefore needs a tree-scoped context mechanism.

---

## 2. Decision

LumaFlow will provide a `BuildContext` associated with each mounted location in the LumaFlow runtime tree.

Conceptually:

```text
Root BuildContext
        ↓
Parent WidgetNode
        ↓
Child BuildContext
        ↓
Child WidgetNode
```

Contextual values flow downward through the mounted tree.

A descendant resolves the nearest applicable contextual value.

---

## 3. Core Principle

`BuildContext` represents:

```text
the environment of a mounted Widget
```

It does not represent:

```text
global application state
```

and it is not intended to become:

```text
a universal dependency injection container
```

---

## 4. Mounted Context

`BuildContext` belongs to a mounted `WidgetNode`.

This follows ADR-007.

A Widget description does not permanently own its context.

Conceptually:

```text
Widget
    ↓ mounted at location A
Context A

Widget
    ↓ mounted at location B
Context B
```

The same Widget description can therefore resolve different contextual values in different locations.

---

## 5. Initial Context Values

The initial BuildContext should focus on UI-specific concerns.

Potential core properties:

```csharp
public sealed class BuildContext
{
    public ThemeData Theme { get; }

    public MediaQueryData MediaQuery { get; }

    public Navigator Navigator { get; }
}
```

Not all properties need to exist in MVP.

Theme is the first required scoped concern.

---

## 6. Contextual Values

Good candidates for BuildContext include:

```text
Theme
MediaQuery
Navigator
Localization
Focus scope
UI density
text scale
platform UI information
overlay host
```

These share one important property:

```text
their meaning depends on position in the UI tree
```

---

## 7. Poor Context Candidates

BuildContext should not automatically contain arbitrary application services.

Avoid:

```csharp
context.Database
context.NetworkClient
context.SaveSystem
context.PlayerInventoryService
context.Analytics
context.AudioManager
```

unless a deliberate extensibility/provider mechanism is introduced later.

These concerns do not inherently belong to UI tree context.

---

## 8. No Global Singleton Requirement

Components should prefer:

```csharp
context.Theme
```

over:

```csharp
ThemeManager.Instance.CurrentTheme
```

Likewise future navigation should prefer:

```csharp
context.Navigator
```

over:

```csharp
Navigator.Global
```

This enables independent trees.

---

## 9. Multiple Mount Trees

Each root mount may have its own root context.

Example:

```text
Editor Window A
└── LumaFlow Mount
    └── Theme A

Editor Window B
└── LumaFlow Mount
    └── Theme B
```

There must be no requirement that both trees share one global theme.

---

## 10. Multiple Runtime Panels

Likewise:

```text
UIDocument A
└── LumaFlow context A

UIDocument B
└── LumaFlow context B
```

Each may have independent:

```text
Theme
Navigator
MediaQuery
```

where appropriate.

---

## 11. Context Inheritance

Child contexts inherit values from their parent unless overridden.

Conceptually:

```text
Root Context
├── Theme = Default
├── Navigator = RootNavigator
└── MediaQuery = RootPanelMetrics
```

A child may reuse the same values without copying every object.

---

## 12. Scoped Override

A subtree may replace one contextual value.

Example:

```text
Root
Theme = Light
    ↓
Theme Override
Theme = Dark
    ↓
Dialog
```

The Dialog resolves Dark.

Sibling content still resolves Light.

---

## 13. Theme Example

Desired:

```csharp
Theme(
    data: darkTheme,
    child: SettingsDialog()
)
```

Conceptually:

```text
Parent BuildContext
        ↓
Create child context
        ↓
Theme replaced
        ↓
mount SettingsDialog
```

---

## 14. Context Derivation

A child context should conceptually derive from its parent.

Potential internal API:

```csharp
var childContext = context.WithTheme(darkTheme);
```

This does not imply the public API must expose arbitrary context mutation.

---

## 15. Context Immutability

BuildContext should preferably behave as immutable scoped data.

Once a context is created for a mounted location, code should not mutate:

```csharp
context.Theme = ...
```

Instead:

```text
new derived context
```

or:

```text
provider/context update mechanism
```

should be used.

This improves predictability.

---

## 16. Parent Relationship

Internally, context may be implemented through:

```text
parent-linked lookup
```

or:

```text
resolved/copy-on-write context values
```

The exact strategy is an implementation decision.

The public semantics are:

```text
nearest scoped value wins
```

---

## 17. Lookup Strategy

Two broad implementation strategies are acceptable.

### Strategy A — Parent-linked lookup

```text
Context
    ↓
parent
    ↓
parent
    ↓
find value
```

Advantages:

- cheap child creation;
- natural inheritance;
- easy sparse overrides.

Potential disadvantage:

- lookup depth.

### Strategy B — Resolved context snapshot

```text
child context contains resolved references
```

Advantages:

- fast lookup.

Potential disadvantage:

- more data copying/updating.

Implementation should be chosen through simplicity and profiling.

---

## 18. Avoid Premature Generic Context Registry

Do not immediately implement:

```csharp
Dictionary<Type, object>
```

inside every BuildContext.

The initial required context values are known and should remain strongly typed.

---

## 19. Strongly Typed Core Properties

Core values should use explicit properties.

Preferred:

```csharp
context.Theme
context.Navigator
context.MediaQuery
```

over:

```csharp
context.Get<ThemeData>()
context.Get<Navigator>()
context.Get<MediaQueryData>()
```

for foundational framework concepts.

This improves:

- discoverability;
- IntelliSense;
- documentation;
- performance;
- clarity.

---

## 20. Extensible Context

Third-party and application components may eventually need custom scoped data.

LumaFlow may later introduce a provider mechanism.

Potential concept:

```csharp
Provider<T>(
    value: value,
    child: content
)
```

and:

```csharp
context.Read<T>()
```

or an equivalent typed API.

This is deferred.

---

## 21. Provider Is Not DI by Default

If generic Provider support is introduced, it should remain:

```text
UI tree-scoped contextual data
```

not:

```text
full application dependency injection container
```

The framework should not automatically handle:

- service construction;
- service lifetimes;
- constructor injection;
- module registration;
- application startup.

Those concerns belong to application architecture or dedicated DI systems.

---

## 22. Why Provider May Still Be Useful

Custom reusable components may need scoped data.

Example:

```text
Form validation context
Selection context
Editor tool context
Custom design system extension
```

A typed Provider could support these without global state.

---

## 23. BuildContext Lifetime

A BuildContext is valid while its owning mounted location is active.

Application code should not assume:

```text
BuildContext can be stored forever
```

After the node unmounts, the context should be considered stale.

---

## 24. Do Not Store BuildContext Globally

Forbidden pattern:

```csharp
public static BuildContext CurrentContext;
```

This breaks:

- multiple windows;
- multiple trees;
- nested contexts;
- lifecycle safety.

---

## 25. Avoid Long-Lived Context Capture

This is potentially dangerous:

```csharp
_savedContext = context;
```

followed by usage long after the Widget unmounts.

Documentation should discourage this.

Use durable external references such as:

```text
State
application services
Navigator handle where explicitly designed
```

instead of retaining BuildContext casually.

---

## 26. Callback Context Usage

Using context synchronously inside callbacks is valid.

Example:

```csharp
Button(
    "Settings",
    onPressed: () =>
        context.Navigator.Push(
            new SettingsScreen()
        )
)
```

provided the callback belongs to the mounted lifecycle.

Once the widget unmounts, its callback should also be detached under ADR-008.

---

## 27. Context and StatelessView

A StatelessView receives the context for its mounted location:

```csharp
public override Widget Build(BuildContext context)
{
    return Text(
        "Settings",
        style: context.Theme.Typography.TitleLarge
    );
}
```

This is one of BuildContext's primary use cases.

---

## 28. Context and StatefulView

Stateful mounted views also receive their current context.

Any mounted-local state is separate from context.

Do not use BuildContext as a storage location for mutable local view state.

---

## 29. Context Is Not State<T>

Important distinction:

```text
BuildContext
=
scoped environment
```

while:

```text
State<T>
=
reactive mutable value
```

Do not merge the two abstractions.

---

## 30. Context Values May Be Reactive

A contextual value may internally change.

Example:

```text
Theme
MediaQuery dimensions
Localization locale
```

LumaFlow must define how descendants respond to those changes.

This does not mean BuildContext itself becomes mutable application state.

---

## 31. Theme Change

Example:

```text
ThemeProvider
Theme A
    ↓
switch
Theme B
```

Theme-dependent descendants should update their resolved styles.

This follows ADR-005 and ADR-010.

---

## 32. MediaQuery Change

Panel size may change.

Example:

```text
EditorWindow resized
```

MediaQuery-dependent responsive builders may need reevaluation.

The framework should update only consumers that depend on MediaQuery where practical.

---

## 33. Navigator Stability

A Navigator is generally a stable scoped service for a navigation subtree.

It should not be recreated simply because ordinary state changes occur.

---

## 34. Dependency Awareness

Some context values may need subscription behavior.

Conceptually:

```text
WidgetNode
    ↓ reads Theme
    ↓
register dependency on ThemeProvider
```

When theme changes:

```text
dependent node updates
```

This requires deliberate implementation.

---

## 35. No Automatic Universal Dependency Tracking Yet

Do not introduce a generic hidden context dependency graph for every possible `context.X` access during MVP unless required.

Core values may use explicit specialized mechanisms.

For example:

```text
Theme-aware node
```

may subscribe through theme infrastructure.

Avoid generalized magic before real need.

---

## 36. Explicit Context Dependencies

Framework nodes may declare which context values they depend on.

Potential internal model:

```text
TextNode
→ Theme dependency

ResponsiveBuilderNode
→ MediaQuery dependency
```

This can produce localized updates.

---

## 37. Context Update Scope

Changing one contextual value should not automatically rebuild unrelated UI.

Example:

```text
MediaQuery changed
```

should not necessarily cause static themed Text values to rebuild unless they depend on MediaQuery.

Likewise:

```text
Navigator stack changed
```

should not invalidate unrelated style nodes.

---

## 38. Context Providers

Potential provider categories:

```text
ThemeProvider
MediaQueryProvider
NavigatorProvider
LocalizationProvider
OverlayProvider
```

These may be internal or exposed through semantic widgets.

---

## 39. Semantic Provider Widgets

Prefer:

```csharp
Theme(
    data: theme,
    child: content
)
```

over generic:

```csharp
Provider<ThemeData>(
    value: theme,
    child: content
)
```

for foundational framework concepts.

Semantic APIs improve discoverability.

---

## 40. Navigator Context

Navigation should be resolved from the nearest relevant navigation scope.

Example:

```text
Root Navigator
    ↓
Nested Navigator
        ↓
Dialog flow
```

A descendant inside the nested flow should resolve the nested Navigator.

This enables independent navigation stacks.

---

## 41. Nested Navigation

Potential future:

```csharp
NavigatorScope(
    navigator: localNavigator,
    child: tabContent
)
```

This is why global navigation singleton architecture is rejected.

---

## 42. Overlay Context

Overlays may need access to the nearest OverlayHost.

Potential:

```csharp
context.Overlay
```

or:

```csharp
context.Navigator.Overlay
```

Exact API is deferred.

The nearest scope principle should remain consistent.

---

## 43. MediaQuery

Potential `MediaQueryData` values:

```text
Width
Height
PixelScale
SafeArea
Orientation
```

depending on what Unity reliably provides.

Do not expose device information that cannot be accurately determined.

---

## 44. Panel-Scoped Metrics

MediaQuery should primarily describe:

```text
available UI panel environment
```

not:

```text
physical monitor assumptions
```

This matters for:

- Editor windows;
- windowed desktop;
- split-screen;
- embedded UI.

---

## 45. Safe Area

Future mobile SafeArea support should derive from contextual environment data.

Potential:

```csharp
SafeArea(
    child: content
)
```

which reads MediaQuery.

Do not hardcode Screen.safeArea logic inside random components.

---

## 46. Localization

Localization may later be represented through a scoped interface.

Potential:

```csharp
context.Localization
```

This allows nested locale overrides where useful.

---

## 47. Unity Localization Integration

LumaFlow may adapt Unity Localization without making it mandatory.

A localization context abstraction should remain optional if the package is absent.

This follows `COMPATIBILITY.md`.

---

## 48. Text Direction

If localization eventually requires text direction:

```text
LTR
RTL
```

that may become contextual.

Layout widgets may need to resolve directional concepts through context.

Do not add directional semantics before required.

---

## 49. UI Density

Future themes or environments may expose:

```text
Compact
Comfortable
Touch
```

through context/theme.

This allows Editor and mobile UI to use different control density without component-specific platform checks.

---

## 50. Platform Information

Some UI behavior may legitimately differ by platform.

If platform UI information is introduced, it should be a narrow typed contextual value.

Avoid:

```csharp
if (Application.platform == ...)
```

scattered throughout widgets.

---

## 51. Core Context Should Stay Small

BuildContext must not accumulate dozens of direct properties.

If the class starts to look like:

```text
Theme
Navigator
MediaQuery
Localization
Audio
Network
Database
Player
Analytics
SaveSystem
Input
Physics
Camera
...
```

the architecture has failed.

Core context should remain UI-specific.

---

## 52. No God Context

BuildContext must not become the main dependency object passed to the entire application.

Its role is intentionally narrow.

---

## 53. Context Extension Strategy

If extension becomes necessary, prefer a separate typed extension/provider API rather than adding every user service directly to BuildContext.

Conceptually:

```csharp
context.Read<MyUiContext>()
```

might be acceptable in the future.

This requires separate API review.

---

## 54. Type-Keyed Provider Risks

A generic type-keyed provider introduces concerns:

- duplicate provider types;
- missing-provider exceptions;
- lifetime ownership;
- performance;
- hidden dependencies.

Therefore it should not be introduced casually.

---

## 55. Missing Context Values

Core required values should always have sensible root defaults where practical.

Example:

```text
Theme
→ ThemeData.Default
```

A component should not crash merely because no custom Theme was provided.

---

## 56. Optional Context Values

Some values may legitimately be absent.

Example:

```text
Navigator
```

for an isolated component preview.

The API should clearly define whether:

```csharp
context.Navigator
```

throws, returns null, or returns a no-op implementation.

Avoid ambiguous behavior.

---

## 57. Required vs Optional Context

Potential internal/public distinction:

```text
RequireNavigator()
TryGetNavigator()
```

may be useful.

Do not expose nullable core values casually if misuse would produce delayed NullReferenceExceptions.

---

## 58. Theme Must Always Exist

Theme should have a framework default.

Therefore:

```csharp
context.Theme
```

should normally be non-null.

This improves every component's assumptions.

---

## 59. MediaQuery Default

Every mounted UI exists in some panel environment.

A basic MediaQuery may be derivable from the current panel/root.

If not yet available at initial mount, the framework may use a transitional representation.

Exact semantics must be implemented carefully.

---

## 60. Context Creation at Root Mount

Root mount may conceptually create:

```text
RootBuildContext
├── ThemeData.Default or supplied theme
├── root environment
└── optional navigation scope
```

Example conceptual API:

```csharp
LumaFlow.Mount(
    widget,
    root,
    theme: appTheme
);
```

Exact overloads are deferred.

---

## 61. Root Context Options

As root configuration grows, avoid a constructor/overload explosion.

Potential future:

```csharp
new MountOptions
{
    Theme = appTheme,
    Navigator = navigator
}
```

may be appropriate.

Do not introduce it before multiple options genuinely exist.

---

## 62. Context Propagation and WidgetNode

WidgetNode owns or references the context appropriate for its mounted position.

When mounting children:

```text
parent node
    ↓
child context
    ↓
child node
```

This relationship must remain explicit in runtime code.

---

## 63. Structural Nodes

A structural WidgetNode may provide a different child context without owning a native VisualElement.

Example:

```text
ThemeNode
    ↓
derived BuildContext
    ↓
child node
```

This is a key reason the LumaFlow runtime tree may differ from the VisualElement tree.

---

## 64. Theme Node Example

Framework tree:

```text
ThemeNode
└── CardNode
```

Native tree could be:

```text
Card VisualElement
```

if ThemeNode requires no native wrapper.

The context scope still exists.

---

## 65. Provider Nodes Can Be Structural

Context providers should not introduce native hierarchy elements unless native behavior requires them.

This avoids unnecessary wrappers.

---

## 66. Context Scope and Native Tree

Do not determine contextual ownership purely by traversing `VisualElement.parent`.

LumaFlow semantic context follows the `WidgetNode` tree.

Native and framework trees may differ under ADR-007.

---

## 67. Context Caching

Frequently accessed values such as Theme may be stored directly on derived contexts for O(1) access.

This is an implementation optimization.

Do not sacrifice update correctness for cache simplicity.

---

## 68. Parent Lookup Performance

If generic parent-chain lookup is used later, deeply nested UI trees could make repeated reads expensive.

Profile before introducing complex caching.

Common core values should likely have efficient direct access.

---

## 69. Context Equality

BuildContext itself does not need broad structural equality.

Individual contextual values may use identity/equality to determine whether dependents need updating.

---

## 70. Context Versioning

Providers may eventually maintain revisions.

Example:

```text
ThemeProvider revision 7
```

Dependent nodes can detect changes.

This is an implementation option, not MVP requirement.

---

## 71. Context Updates

A provider updating one scoped value should preserve unrelated values.

Example:

```text
old:
Theme A
Navigator X
MediaQuery M

new:
Theme B
Navigator X
MediaQuery M
```

Only theme semantics changed.

---

## 72. No Context Object Recreation Everywhere

Even if immutable contexts are used, a root theme change should not require naively allocating a fresh BuildContext object for every node if a more efficient scoped model is available.

Correctness first, then profile.

---

## 73. Testing Context

BuildContext architecture should be easy to test.

Example:

```text
mount Widget with Theme A
↓
nested Theme B
↓
verify inner widget resolves B
↓
verify sibling resolves A
```

---

## 74. Component Testing

Tests should be able to mount a Widget under a deliberately constructed root context without bootstrapping an entire application.

---

## 75. No Editor Singleton Dependency

Editor components must not obtain their theme/navigation context through static EditorWindow globals.

Each window remains independently scoped.

---

## 76. Domain Reload

Because context is mount-scoped rather than static-global, domain reload behavior becomes simpler.

Any root mounts destroyed by reload can be recreated through normal Editor lifecycle.

---

## 77. Threading

BuildContext itself may contain immutable references that can technically be read from other threads.

However, mounted UI use remains main-thread oriented.

Do not use BuildContext to imply thread-safe UI operations.

---

## 78. Async Context Capture

Async callbacks should avoid relying on BuildContext still being mounted after an `await`.

Example risk:

```csharp
var context = currentContext;

await LoadAsync();

context.Navigator.Push(...);
```

The owning view may have been unmounted.

Future APIs may provide lifecycle-aware patterns.

MVP documentation should caution against long-lived context capture across asynchronous boundaries.

---

## 79. Navigator Lifetime

A Navigator may outlive one child view.

If application logic needs navigation after async work, holding a deliberately scoped Navigator reference may be safer than retaining BuildContext.

This should be documented once navigation exists.

---

## 80. Context Debugging

Future LumaFlow developer tooling should allow inspection of:

```text
current Theme
nearest Theme provider
Navigator
MediaQuery
custom providers
```

for a selected WidgetNode.

This makes scoped behavior understandable.

---

## 81. Context Source

A useful debug view should answer:

```text
Where did this Theme come from?
```

Example:

```text
RootTheme
    ↓
SettingsThemeOverride
    ↓
selected ButtonNode
```

---

## 82. No Invisible Global Fallbacks

If a scoped value is missing, do not silently retrieve some unrelated global singleton unless explicitly part of the contract.

Fallback behavior must be deterministic.

---

## 83. Theme Fallback

Theme is special because:

```text
ThemeData.Default
```

is an explicit framework fallback.

This is acceptable.

---

## 84. Navigation Missing Scope

If a Widget calls navigation without a Navigator scope, the framework should eventually produce a clear error.

Example:

```text
LumaFlow: No Navigator is available in this BuildContext.
```

not:

```text
NullReferenceException
```

---

## 85. Provider Override Semantics

Nearest provider wins.

Conceptually:

```text
Provider A
    ↓
Provider B
        ↓
Child
```

Child resolves B.

Removing B causes Child to see A again if lifecycle/rebuild semantics allow it.

---

## 86. Provider Lifetime

A provider node owns:

```text
the scope
dependency registrations
child node
```

It does not necessarily own the supplied contextual value itself.

Example:

```text
Theme widget
```

does not destroy externally provided ThemeData.

---

## 87. Provider Update

If a provider receives a new compatible contextual value:

```text
Theme A
→
Theme B
```

it should notify relevant dependents and keep the child subtree mounted where possible.

Do not automatically replace the whole subtree.

---

## 88. Context and Reconciliation

BuildContext does not require full reconciliation.

Context-dependent native properties can update directly.

Structural consumers such as responsive builders may use explicit local rebuild boundaries under ADR-004.

---

## 89. Example: Theme-Dependent Text

```csharp
Text(
    "Settings",
    style: context.Theme.Typography.TitleLarge
)
```

If the Theme changes:

```text
TextNode remains mounted
↓
style resolves again
↓
changed style properties applied
```

No whole-tree rebuild required.

---

## 90. Example: Responsive Layout

```csharp
ResponsiveBuilder(
    builder: context =>
        context.MediaQuery.Width < 800
            ? MobileLayout()
            : DesktopLayout()
)
```

MediaQuery change may rebuild only that explicit responsive boundary.

---

## 91. Example: Nested Theme

```csharp
Column(
    children:
    [
        StandardCard(),

        Theme(
            data: warningTheme,
            child: WarningCard()
        )
    ]
)
```

`StandardCard` uses parent theme.

`WarningCard` uses `warningTheme`.

---

## 92. Example: Nested Navigator

Conceptually:

```text
App Navigator
├── Main Screen
└── Modal Flow
    └── Local Navigator
        ├── Step 1
        └── Step 2
```

Components inside the modal flow resolve the local Navigator.

---

## 93. Rejected Alternative: Global Singletons

Rejected:

```csharp
Theme.Current
Navigator.Current
MediaQuery.Current
```

Reasons:

- multiple trees;
- nested scopes;
- tests;
- Editor windows;
- hidden dependencies;
- lifecycle problems.

---

## 94. Rejected Alternative: Pass Everything as Widget Parameters

Rejected as the general solution:

```csharp
SettingsPanel(
    theme: theme,
    navigator: navigator,
    mediaQuery: mediaQuery,
    ...
)
```

for every component.

Reasons:

- parameter plumbing;
- poor composability;
- noisy APIs;
- difficult nested overrides.

Tree context exists to solve these cross-cutting UI dependencies.

---

## 95. Rejected Alternative: Service Locator Context

Rejected:

```csharp
context.GetService<T>()
```

as the primary BuildContext API.

Reasons:

- hidden dependencies;
- weak discoverability;
- encourages unrelated application services;
- turns UI context into a god container.

A narrow optional provider system may be added later.

---

## 96. Rejected Alternative: Dictionary<string, object>

Rejected:

```csharp
context["theme"]
```

Reasons:

- no type safety;
- runtime errors;
- no autocomplete;
- difficult refactoring.

---

## 97. Rejected Alternative: Store Context on Widget

Rejected under ADR-007.

Context belongs to mounted location, not declarative description.

---

## 98. Rejected Alternative: Read Context From VisualElement Hierarchy

Rejected as the sole implementation model.

Reasons:

- WidgetNode tree may contain structural nodes;
- one semantic provider may have no native element;
- framework/native trees may differ.

Context belongs to LumaFlow runtime structure.

---

## 99. Architectural Invariants Created by This ADR

Unless superseded:

### Invariant 1

BuildContext is mount-scoped.

### Invariant 2

Context belongs to WidgetNode location, not Widget description.

### Invariant 3

Nearest scoped value wins.

### Invariant 4

Theme is accessed through BuildContext.

### Invariant 5

BuildContext is not a global service locator.

### Invariant 6

Core context values are strongly typed.

### Invariant 7

Multiple mount trees may have independent contexts.

### Invariant 8

Provider scopes may exist without native VisualElements.

### Invariant 9

Context updates should invalidate only relevant consumers where practical.

### Invariant 10

Application code should not retain BuildContext beyond mounted lifetime.

---

## 100. Codex Rules

### Rule 1

Do not introduce global singleton access for Theme, Navigator, MediaQuery, or other tree-scoped UI concerns.

### Rule 2

Do not store BuildContext permanently in Widget descriptions.

### Rule 3

Resolve context through mounted WidgetNode scope.

### Rule 4

Do not add arbitrary application services directly to BuildContext.

### Rule 5

Prefer strongly typed core context properties.

### Rule 6

Context providers should be structural-only when no native element is required.

### Rule 7

Do not invalidate the whole UI tree when one contextual value changes unless technically unavoidable.

### Rule 8

Do not use `VisualElement.parent` as the sole source of framework context hierarchy.

### Rule 9

When introducing a new core contextual value, document why it is inherently UI-tree scoped.

### Rule 10

Do not introduce a generic DI/provider framework without demonstrated need and API review.

---

## 101. Decision Test

When considering adding something to BuildContext:

```text
Does its meaning depend on UI tree position?
        ↓ no
It probably does not belong in BuildContext.

Is it needed by many descendants?
        ↓ yes
Context may be appropriate.

Does nested override make sense?
        ↓ yes
Strong candidate.

Is it an application backend/service?
        ↓ yes
Do not add directly.

Is it one of LumaFlow's foundational UI concepts?
        ↓ yes
Prefer strongly typed property.
```

---

## 102. Initial MVP Context

MVP should initially focus on:

```text
BuildContext
└── Theme
```

Then add environment values only when required.

Likely progression:

```text
Theme
↓
MediaQuery
↓
Navigator
↓
Overlay
↓
Localization
```

Do not build the final giant context API before those systems exist.

---

## 103. Initial Internal API

Conceptually:

```csharp
internal sealed class BuildContext
{
    public ThemeData Theme { get; }

    internal BuildContext WithTheme(
        ThemeData theme
    )
    {
        ...
    }
}
```

This is illustrative.

Public/internal visibility should follow actual extensibility needs.

---

## 104. BuildContext Public Visibility

`BuildContext` must be public enough for:

```csharp
Widget Build(BuildContext context)
```

but its construction and mutation may remain internal.

Users consume context.

The framework controls scope construction.

---

## 105. Consumer API Target

```csharp
public sealed class SettingsScreen : StatelessView
{
    public override Widget Build(BuildContext context)
    {
        return Column(
            gap: context.Theme.Spacing.L,
            children:
            [
                Text(
                    "Settings",
                    style: context.Theme.Typography.TitleLarge
                ),

                Button(
                    "Back",
                    onPressed: () =>
                        context.Navigator.Pop()
                )
            ]
        );
    }
}
```

No global theme manager.

No global navigator.

No service locator.

---

## 106. Long-Term Direction

BuildContext may eventually support:

```text
Theme
MediaQuery
Navigator
Overlay
Localization
Focus scope
UI density
typed custom UI providers
```

The context should remain intentionally small and coherent.

---

## 107. Reconsideration Conditions

Revisit this ADR if:

1. context lookup becomes a measurable performance bottleneck;
2. contextual update propagation becomes overly complex;
3. a provider extension system is required by real third-party components;
4. BuildContext grows into too many unrelated responsibilities;
5. navigation or responsive UI reveal missing scoping semantics.

Implementation strategy may change.

Tree-scoped semantics should remain unless strong evidence suggests otherwise.

---

## 108. Final Decision

LumaFlow uses `BuildContext` as a **tree-scoped mounted UI environment**.

Its purpose is to allow components to access contextual values such as:

```text
Theme
Navigator
MediaQuery
```

without:

```text
global singletons
parameter plumbing
hidden application service location
```

The guiding rule is:

**If a value changes meaning based on where a Widget lives in the UI tree, it may belong in BuildContext.**

**If it is simply an application dependency, it probably does not.**
