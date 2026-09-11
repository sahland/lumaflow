# ADR-015: Use Scoped Navigator-Based UI Navigation Independent of Unity Scenes

- **Status:** Accepted
- **Decision date:** 2026-08-11
- **Scope:** Navigation, screens, routes, stack ownership, nested flows

> **Retention update (2026-08-20):** Sections that leave inactive-route
> retention undecided are superseded by ADR-023. A mounted `NavigatorHost` now
> retains one mounted, inert native layer per route entry for its host lifetime.
- **Affects:** Runtime, BuildContext, Lifecycle, Overlays, Stateful Views, Testing
- **Related documents:** `ARCHITECTURE.md`, `ADR-004-reconciliation-strategy.md`, `ADR-008-lifecycle-and-ownership.md`, `ADR-011-build-context-and-scoping.md`

---

## 1. Context

LumaFlow will eventually need navigation for interfaces such as:

```text
Main Menu
Settings
Profile
Inventory
Wizard flows
Editor tool pages
Modal flows
Tabbed sections
Nested settings
```

Unity already provides scene management through `SceneManager`.

However, scene loading and UI navigation solve different problems.

A screen transition such as:

```text
Settings
→ Audio
→ Advanced
```

should not require:

```text
loading Unity scenes
destroying GameObjects
changing scene ownership
```

Likewise, Editor UI navigation cannot reasonably depend on scene loading.

LumaFlow therefore requires its own UI navigation model.

---

## 2. Decision

LumaFlow will provide a scoped `Navigator` responsible for managing mounted UI screen entries.

Conceptually:

```text
Navigator
    ↓
navigation stack
    ↓
Screen entries
    ↓
Widget subtrees
```

Navigation is independent from Unity scene management.

---

## 3. Core Principle

LumaFlow navigation operates on:

```text
Widgets
Views
mounted UI subtrees
```

not:

```text
Unity Scenes
GameObjects
MonoBehaviours
```

Applications remain free to combine LumaFlow navigation with scene transitions where appropriate.

The systems are intentionally separate.

---

## 4. Navigator Is Scoped

Navigator is resolved through `BuildContext`.

Preferred:

```csharp
context.Navigator.Push(
    new SettingsScreen()
);
```

not:

```csharp
Navigator.Instance.Push(...);
```

This follows ADR-011.

---

## 5. No Global Navigator

LumaFlow must not require:

```text
Navigator.Current
Navigator.Instance
GlobalNavigationService
```

as its core architecture.

A single application may contain multiple independent navigation stacks.

---

## 6. Root Navigator

A root LumaFlow application may establish a root navigation scope.

Conceptually:

```text
LumaFlow Mount
    ↓
Root Navigator
    ↓
Current screen
```

The root Navigator may be created automatically by a navigation host or supplied explicitly.

Exact bootstrap API is deferred.

---

## 7. Navigation Stack

The initial navigation model is stack-based.

Example:

```text
Home
```

then:

```text
Home
Settings
```

then:

```text
Home
Settings
AudioSettings
```

`Pop()` removes the top entry.

---

## 8. Push

Conceptual API:

```csharp
context.Navigator.Push(
    new SettingsScreen()
);
```

Push should:

```text
create navigation entry
↓
mount destination subtree
↓
make destination active
↓
preserve previous entry according to stack policy
```

---

## 9. Pop

Conceptual:

```csharp
context.Navigator.Pop();
```

Pop should:

```text
remove current top entry
↓
unmount/dispose its owned subtree
↓
reactivate previous entry
```

Lifecycle follows ADR-008.

---

## 10. Replace

A Navigator may support:

```csharp
context.Navigator.Replace(
    new HomeScreen()
);
```

Conceptually:

```text
old top entry
↓
unmount
↓
new entry mounted
```

The previous top is not retained in history.

---

## 11. Pop to Root

A convenience operation may eventually support:

```text
PopToRoot
```

by disposing all entries above the root entry.

Do not add broad route manipulation APIs until real use cases exist.

---

## 12. Navigation Entry

Each pushed screen is represented by a runtime navigation entry.

Conceptually:

```text
NavigationEntry
├── screen Widget
├── mounted subtree
├── optional route metadata
└── navigation lifecycle state
```

This is an internal runtime concept.

---

## 13. Navigator Owns Entries

Navigator owns the lifecycle of stack entries it creates.

Conceptually:

```text
Navigator
├── Entry A
├── Entry B
└── Entry C
```

Popping C means Navigator owns cleanup of C.

Application code must not separately dispose internal navigation entries.

---

## 14. Screen Widgets

A screen can be an ordinary Widget/View.

Example:

```csharp
public sealed class SettingsScreen : StatelessView
{
    public override Widget Build(BuildContext context)
    {
        ...
    }
}
```

No special inheritance should be required solely to become navigable.

---

## 15. Optional Screen Abstraction

A dedicated abstraction such as:

```text
Screen
Page
RouteView
```

may be introduced later if it provides useful semantics.

It is not required for the fundamental navigation model.

---

## 16. Navigation Does Not Require Named Routes

Initial navigation may use typed Widget instances directly.

Example:

```csharp
navigator.Push(
    new UserScreen
    {
        UserId = user.Id
    }
);
```

This avoids premature route registries and string routing.

---

## 17. Typed Navigation

Strongly typed navigation is preferred over:

```csharp
navigator.Push("/users/42");
```

for ordinary in-process UI.

The compiler should help validate destination configuration.

---

## 18. Named Routes May Be Added Later

Named routes may become useful for:

```text
deep linking
external URLs
save/restore
analytics
debug tooling
web-like navigation
```

If introduced, they should map to typed destination factories rather than becoming the only navigation model.

---

## 19. No String-Only Core

Rejected as primary API:

```csharp
Navigator.Push("settings.audio.advanced");
```

because it introduces:

```text
runtime errors
weak refactoring
route registry boilerplate
string literals
```

without being necessary for basic Unity UI navigation.

---

## 20. BuildContext Propagation

Each screen entry receives a BuildContext derived from the Navigator's scope.

Conceptually:

```text
Navigator context
    ↓
Screen context
    ↓
screen subtree
```

The screen sees:

```text
Theme
Navigator
MediaQuery
other inherited context
```

normally.

---

## 21. Navigator Self-Reference

A screen mounted under Navigator resolves that same Navigator unless a nested navigation scope overrides it.

Thus:

```csharp
context.Navigator.Pop();
```

naturally acts on the nearest navigation stack.

---

## 22. Nested Navigators

Nested Navigators are explicitly supported.

Example:

```text
Root Navigator
├── Home
└── SettingsShell
    └── Settings Navigator
        ├── General
        ├── Audio
        └── Controls
```

A component under Audio resolves the Settings Navigator.

---

## 23. Why Nested Navigators Matter

Nested navigation supports:

```text
tabs with independent stacks
modal flows
settings subflows
multi-step wizards
split-pane interfaces
Editor tools
```

without global coordination.

---

## 24. Nearest Navigator Wins

Following BuildContext scoping rules:

```text
Root Navigator
    ↓
Nested Navigator
        ↓
Child
```

Child resolves Nested Navigator.

The root remains accessible only through an explicit future parent/root navigation API if needed.

---

## 25. No Hidden Root Navigation

Do not automatically expose:

```text
context.RootNavigator
```

unless real use cases justify it.

Nested scopes should be respected by default.

---

## 26. Navigator Host

A navigation stack requires a host in the mounted tree.

Conceptually:

```text
NavigatorHost
└── active screen subtree
```

Depending on transition strategy, previous screens may also remain mounted temporarily or persistently.

---

## 27. Initial Screen Retention Strategy

The initial implementation should prefer simple predictable semantics.

A likely strategy is:

```text
top screen
=
active and attached

previous stack entries
=
retained according to navigation implementation
```

The exact retention model must be validated before implementation.

---

## 28. Two Retention Models

Possible strategies:

### Model A — Keep previous mounted

```text
Push B
A remains mounted but inactive/hidden
B mounted
```

Benefits:

```text
instant Pop
local state preserved
scroll/focus state potentially preserved
```

Costs:

```text
more memory
more active subscriptions
hidden UI lifecycle complexity
```

### Model B — Unmount previous, preserve route description/state externally

```text
Push B
A unmounted
B mounted

Pop
A mounted again
```

Benefits:

```text
lower mounted UI cost
simpler active tree
```

Costs:

```text
local state lost unless externally owned
more remounting
```

---

## 29. Retention Is Not Finalized by This ADR

This question was resolved by ADR-023: inactive history entries remain mounted,
hidden, disabled, and non-pickable for the lifetime of their `NavigatorHost`.
Memory/update cost still requires profiling before beta, but the lifecycle
semantics are no longer provisional.

---

## 30. Preferred Initial Evaluation

The first implementation should test both approaches with:

```text
settings screens
forms
scrollable screens
Editor panels
stateful views
```

before stabilizing the public behavior.

---

## 31. Semantic State Should Not Depend on Hidden Screen Retention

Application-critical state should be owned outside screens when it must survive navigation.

Do not depend on:

```text
screen happened to stay mounted
```

as a persistence mechanism.

---

## 32. Navigation and State Hoisting

Example:

```text
App State
├── user settings
└── selected profile

Navigator
├── HomeScreen
└── SettingsScreen
```

Navigation controls visibility/lifecycle.

Application state remains externally owned.

---

## 33. Route Arguments

Typed screen properties may carry route arguments.

Example:

```csharp
navigator.Push(
    new ClipDetailsScreen
    {
        Clip = selectedClip
    }
);
```

This is preferable to requiring a generic key/value route-data dictionary.

---

## 34. Route Data Ownership

Navigator should not clone or own arbitrary application models passed to destinations.

They remain externally owned references unless the application decides otherwise.

---

## 35. Navigation Results

Some flows need to return a value.

Example:

```text
Select Audio Clip
↓
return selected clip
```

A future API may support typed results.

Conceptually:

```csharp
var result = await navigator.PushForResult<AudioClip>(
    new ClipPickerScreen()
);
```

This is deferred.

---

## 36. Async Navigation APIs

Do not introduce Task-based navigation solely because other frameworks use it.

If result-returning flows prove common, design explicit lifecycle/cancellation semantics first.

---

## 37. Pop Result

Potential future:

```csharp
context.Navigator.Pop(result);
```

requires typed route-entry ownership.

This is outside MVP.

---

## 38. Navigation Guards

Future use cases may require:

```text
unsaved changes
authentication
confirmation before leaving
```

Navigation guards are explicitly deferred.

Do not complicate core Push/Pop during initial implementation.

---

## 39. Back Behavior

Navigator should provide a semantic Back/Pop action.

Platform integration may map:

```text
Escape
Android Back
gamepad back
Editor shortcut
```

to Navigator where appropriate.

This integration belongs at host/input layers.

---

## 40. Pop Failure

Calling `Pop()` on a root-only stack must have deterministic behavior.

Possible:

```text
return false
```

or:

```text
no-op
```

or a clearly documented exception for misuse.

A boolean result is likely useful:

```csharp
if (!navigator.Pop())
{
    ...
}
```

Exact API deferred.

---

## 41. CanPop

A future/likely API:

```csharp
context.Navigator.CanPop
```

or:

```csharp
context.Navigator.CanPop()
```

allows controls to decide whether Back is available.

---

## 42. Navigation Bar Integration

A component may use:

```csharp
Button(
    "Back",
    onPressed: () => context.Navigator.Pop()
)
```

No scene knowledge required.

---

## 43. Scene Navigation Remains Application-Owned

An application may intentionally do:

```text
Main Menu UI
↓
load gameplay scene
```

That is valid.

The callback can call Unity `SceneManager`.

LumaFlow Navigator does not replace Unity scene management.

---

## 44. No SceneManager Dependency

Core navigation assemblies must not depend on:

```text
SceneManager
scene build indices
scene names
scene lifecycle
```

for normal UI routing.

This keeps navigation usable in Editor tools and scene-independent runtime UI.

---

## 45. Scene-Aware Adapter

A future optional helper may integrate scene transitions.

Example conceptual:

```text
SceneRoute
```

or:

```text
Navigator + application callback
```

This should remain outside the foundational navigation model.

---

## 46. Editor Navigation

Editor windows may use Navigator for internal tool pages.

Example:

```text
AudioLib
├── Library
├── Mixer
├── Settings
└── Diagnostics
```

No Play Mode or Scene is required.

This is a major design requirement.

---

## 47. Navigation vs Tabs

Tabs and navigation are related but not identical.

Tabs often represent parallel destinations.

A future `TabView` may:

```text
keep one current tab
preserve per-tab state
```

and may optionally contain nested Navigators.

Do not force every tab implementation through Push/Pop.

---

## 48. Navigation vs Conditional UI

For a simple binary switch:

```text
loading ? A : B
```

use structural reactive composition.

Do not use Navigator for every conditional subtree.

Navigator is for user-visible destination history/flow semantics.

---

## 49. Navigation vs Overlay

Dialogs, menus, tooltips, and popovers should generally use an Overlay system.

Do not push every dialog onto the main screen Navigator stack.

A modal full-screen flow may use nested Navigator where appropriate.

---

## 50. Overlay Relationship

Future architecture may be:

```text
NavigatorHost
├── current screen
└── OverlayHost
```

or OverlayHost may sit above Navigator.

This should be defined with overlay ADR later.

---

## 51. Navigation Entry Lifecycle

Push:

```text
create entry
↓
mount/activate destination
```

Pop:

```text
deactivate destination
↓
unmount/dispose entry according to retention semantics
```

Lifecycle must reuse ADR-008.

---

## 52. No Separate Navigation Cleanup System

Navigation entries must not invent parallel:

```text
subscription cleanup
event cleanup
resource cleanup
```

They own ordinary LumaFlow mounted subtrees and use the same lifecycle infrastructure.

---

## 53. Navigation Transition

Animations are not required for initial navigation correctness.

Initial Push/Pop may switch instantly.

Transition support should be layered later.

---

## 54. Transition Architecture

Future transitions may temporarily keep:

```text
outgoing screen
+
incoming screen
```

mounted simultaneously.

Example:

```text
A slides out
B slides in
```

Lifecycle must account for this without redefining Navigator ownership.

---

## 55. Transition Ownership

Navigator/transition host owns temporary transition nodes.

Once transition completes:

```text
outgoing screen
↓
detach/dispose according to stack retention policy
```

---

## 56. Transition Does Not Define Navigation Semantics

Navigation stack mutation should remain conceptually independent from how screens animate.

Avoid coupling route APIs to a specific animation system.

---

## 57. Route Transition Configuration

Future destinations may provide:

```text
default transition
duration
animation
```

through navigation options or Theme.

Do not put animation flags into every screen base class prematurely.

---

## 58. Navigator Theme

Navigation visuals should use Theme where applicable.

Navigator itself is primarily behavioral and should not contain arbitrary visual defaults.

---

## 59. Navigation Host Layout

NavigatorHost should normally fill the available space assigned by its parent.

It must still participate in ordinary UI Toolkit layout.

Do not use global screen dimensions.

---

## 60. Navigator Inside Layout

This should be possible:

```csharp
Row(
    children:
    [
        Sidebar(),

        Expanded(
            child: NavigatorHost(...)
        )
    ]
)
```

Navigation is not restricted to full-screen UI.

---

## 61. Split Views

A desktop/editor layout may have multiple navigation areas.

Example:

```text
Root
├── Asset Navigator
└── Inspector Navigator
```

Scoped navigators naturally support this.

---

## 62. Route Identity

Each navigation entry has runtime identity independent from screen Widget object identity.

This follows ADR-007.

Two instances of the same screen type may exist in one stack.

Example:

```text
UserScreen(User A)
UserScreen(User B)
```

---

## 63. Duplicate Destinations

Navigator must not assume one route type appears only once.

Stack semantics permit repeated destinations.

---

## 64. Route Keys

General Widget keys are still deferred.

Navigation entries may have internal IDs for stack identity without exposing global Widget keys.

---

## 65. Route Names

Optional route metadata may include:

```text
debug name
analytics name
deep-link identifier
```

later.

This metadata is separate from runtime entry identity.

---

## 66. Navigation History

Stack itself is navigation history.

Do not create a second duplicate history service in MVP.

---

## 67. Back Stack Limits

No arbitrary maximum stack depth should be imposed by Core.

Applications may enforce limits where needed.

---

## 68. Duplicate Push Protection

Core should not automatically prevent:

```text
Settings
↓
Settings
↓
Settings
```

because repeated destinations can be legitimate.

Higher-level APIs may provide `PushSingleTop`-style behavior later if real demand exists.

---

## 69. Advanced Stack Operations

Operations such as:

```text
PushAndRemoveUntil
ReplaceAll
PopUntil
Reset
```

may be added later.

Do not copy an entire mobile navigation API before LumaFlow needs it.

---

## 70. Minimal MVP Operations

Initial navigation should aim for:

```text
Push
Pop
Replace
CanPop
```

Possibly:

```text
SetRoot
```

if bootstrap requires it.

---

## 71. Navigator API Surface

Keep the Navigator API compact.

Avoid dozens of route operations before dogfooding.

---

## 72. Reactive Navigation State

Navigator stack itself is mutable state.

Consumers may eventually observe:

```text
current route
can pop
stack depth
```

through typed read-only reactive values.

Do not expose the mutable internal stack collection directly.

---

## 73. Read-Only Navigation State

Potential:

```csharp
navigator.Current
navigator.CanPop
```

with change notifications if required by UI.

This should not allow external code to mutate internal stack arbitrarily.

---

## 74. Threading

Navigation mutations happen on Unity's main thread.

Calling Push/Pop from background threads is unsupported unless future scheduling infrastructure explicitly marshals them.

---

## 75. Reentrant Navigation

A screen callback may call:

```text
Pop
Push
Replace
```

while handling an event.

This is normal.

The runtime must avoid continuing to mutate a screen after it was popped during its own callback.

This follows ADR-008 reentrant teardown rules.

---

## 76. Navigation During Transition

Once transitions exist, repeated navigation operations during an active transition require policy.

Possible future strategies:

```text
queue
cancel
complete immediately
reject
```

This is deferred.

Initial navigation without transitions avoids the issue.

---

## 77. Navigation During Build

Calling:

```csharp
context.Navigator.Push(...)
```

inside `Build()` is invalid design.

Build should describe UI, not cause navigation side effects.

---

## 78. Navigation in Callbacks

Correct:

```csharp
Button(
    "Open Settings",
    onPressed: () =>
        context.Navigator.Push(
            new SettingsScreen()
        )
)
```

---

## 79. Initial Destination

NavigatorHost requires an initial destination.

Conceptual:

```csharp
Navigator(
    initial: new HomeScreen()
)
```

or:

```csharp
NavigatorHost(
    navigator: navigator,
    child: ...
)
```

Exact API deferred.

---

## 80. Navigator Ownership

A Navigator may be:

```text
created and owned by NavigatorHost
```

or:

```text
supplied externally
```

if advanced application architectures require direct control.

Ownership must be explicit.

---

## 81. Externally Supplied Navigator

If external Navigator injection is supported, unmounting a host should not necessarily destroy externally owned Navigator state.

The API must clearly distinguish:

```text
owned navigator
borrowed navigator
```

This follows ADR-008.

---

## 82. Internal Navigator Preferred Initially

MVP may simplify ownership by having NavigatorHost own its Navigator.

Expose external ownership only when real use cases require it.

---

## 83. Navigator Lifetime

A Navigator's lifetime is tied to its navigation scope.

Unmounting the owning navigation host removes its entries.

Nested Navigator disappears with its subtree.

---

## 84. State Restoration

Persisting navigation state across:

```text
application restart
domain reload
scene reload
```

is outside the initial Navigator responsibility.

A future serialization/router layer may support it.

---

## 85. Deep Links

Deep-link routing is deferred.

If added, deep links should resolve into typed navigation operations.

Do not redesign core Push/Pop around URL strings prematurely.

---

## 86. URL-Like Routes

WebGL or desktop deep links may eventually justify URL-style route parsing.

That would be an optional routing layer above Navigator.

Conceptually:

```text
URI
↓
Router
↓
typed destination
↓
Navigator
```

Navigator itself remains stack runtime.

---

## 87. Router vs Navigator

Future distinction:

```text
Router
=
convert external route representation into destinations

Navigator
=
manage mounted destination stack
```

Do not merge these concepts prematurely.

---

## 88. Route Registry

A route registry belongs to future Router/deep-link infrastructure.

It is not required for direct typed Navigator usage.

---

## 89. Dependency Injection

Navigator must not construct application services for destinations.

A screen receives dependencies through normal application composition.

Example:

```csharp
navigator.Push(
    new ProfileScreen
    {
        User = user,
        Repository = repository
    }
);
```

or external app DI may create the screen.

---

## 90. Navigation Factories

If destination creation becomes complex, application code may use:

```csharp
navigator.Push(
    CreateProfileScreen(user)
);
```

No framework route container is required.

---

## 91. Lazy Destination Creation

A future overload may take a factory:

```csharp
navigator.Push(
    () => new ExpensiveScreen()
);
```

if this provides meaningful benefits.

Do not add until required.

---

## 92. Screen Context Isolation

Each stack entry may eventually have entry-specific contextual scopes.

Examples:

```text
route metadata
transition data
navigation result channel
```

Do not pollute global BuildContext with these unless they are genuinely tree-scoped.

---

## 93. Focus on Push

When pushing a screen, focus behavior should remain predictable.

Initial policy may allow native UI Toolkit focus to reset naturally.

Future destination focus policies may explicitly request first-focus behavior.

Do not implement fragile automatic focus restoration prematurely.

---

## 94. Focus on Pop

If previous screen stays mounted, restoring previous focus may be possible.

If it remounts, focus may reset.

Retention policy therefore affects focus semantics and must be documented.

---

## 95. Scroll Preservation

Same principle applies to scroll position.

Keeping previous screen mounted naturally preserves native scroll state.

Unmount/remount may not.

This is an important factor in retention-policy evaluation.

---

## 96. Input Blocking

Inactive screens must not receive pointer/focus input.

Whether they are:

```text
detached
display:none
otherwise made inactive
```

depends on retention implementation.

No hidden screen may accidentally remain interactive.

---

## 97. Hidden Screen Reactive Work

If inactive screens remain mounted, their State subscriptions may remain active.

This can create unnecessary work.

Retention policy must account for:

```text
memory
subscriptions
updates
animation
scheduled work
```

---

## 98. Suspended Screen State

A future lifecycle may distinguish:

```text
Mounted + Active
Mounted + Inactive/Suspended
```

if keeping screens mounted becomes the chosen strategy.

This would require explicit lifecycle semantics.

---

## 99. Do Not Invent Suspended Lifecycle Yet

ADR-008 currently defines normal mounted/unmounted lifecycle.

Do not introduce `OnPause`, `OnResume`, or suspension APIs until retention strategy requires them.

---

## 100. Navigation Testing

Required tests should eventually include:

```text
initial screen mount
Push
Pop
Replace
CanPop
entry cleanup
nested Navigator
multiple independent Navigators
context resolution
pop during callback
```

---

## 101. Push Lifecycle Test

Initial:

```text
A
```

Push B.

Verify:

```text
B becomes current
B receives correct BuildContext
Navigator stack depth updates
```

and A follows documented retention behavior.

---

## 102. Pop Lifecycle Test

Stack:

```text
A
B
```

Pop.

Verify:

```text
B resources cleaned according to lifecycle
A becomes current
CanPop changes appropriately
```

---

## 103. Replace Test

Stack:

```text
A
B
```

Replace C.

Expected:

```text
A
C
```

B is fully removed.

---

## 104. Nested Navigator Test

Structure:

```text
Root Navigator
└── Screen
    └── Nested Navigator
        └── Child
```

Verify Child resolves Nested Navigator.

A sibling outside nested scope resolves Root Navigator.

---

## 105. Independent Navigator Test

Mount:

```text
Navigator A
Navigator B
```

Push on A.

Verify B stack remains unchanged.

---

## 106. Pop During Callback Test

Button in screen B calls Pop.

After callback:

```text
B may already be unmounted
```

Framework handler must not touch disposed B afterward.

---

## 107. Navigation Without Scope Test

A component requiring Navigator is mounted without one.

Expected:

```text
clear LumaFlow diagnostic
```

not NullReferenceException.

---

## 108. Editor Dogfooding

A future AudioLib navigation structure could use:

```text
Library
Mixer
Settings
Diagnostics
```

to validate:

```text
screen switching
nested settings
state retention
navigation layout
Editor focus
```

before stabilizing APIs.

---

## 109. Runtime Dogfooding

A sample runtime application should test:

```text
Home
Settings
Profile/details
back navigation
nested flow
```

without loading scenes.

---

## 110. Performance

Push/Pop cost should scale primarily with affected destination subtree.

Navigator must not rebuild unrelated application UI.

This follows ADR-004.

---

## 111. Allocation

Navigation naturally allocates screen Widgets/Nodes when destinations mount.

This is acceptable.

Do not prematurely pool screens.

---

## 112. Screen Pooling

General screen pooling is deferred.

It introduces:

```text
stale state
stale context
lifecycle complexity
memory retention
```

Only add if profiling proves a real need.

---

## 113. Navigation Stack Memory

If screens stay mounted, memory scales with stack depth.

If screens unmount, remount cost increases.

This tradeoff must be measured, not guessed.

---

## 114. Rejected Alternative: SceneManager as Navigator

Rejected:

```text
Push(Settings)
=
SceneManager.LoadScene("Settings")
```

Reasons:

```text
Editor UI incompatibility
heavy runtime semantics
unnecessary scene coupling
poor nested-navigation support
```

---

## 115. Rejected Alternative: Global Navigation Singleton

Rejected due to:

```text
multiple windows
nested navigators
multiple panels
testing
hidden global state
```

---

## 116. Rejected Alternative: String Routes Only

Rejected as primary model due to weak typing and unnecessary route registry complexity.

---

## 117. Rejected Alternative: Full Web Router First

Rejected.

LumaFlow is not initially a browser framework.

Deep linking and URLs can be layered later.

---

## 118. Rejected Alternative: Navigation via Visibility State Everywhere

Possible:

```csharp
currentScreen.Value = Screen.Settings;
```

with one massive conditional builder.

This is acceptable for tiny flows but is not a scalable general navigation model because it lacks:

```text
history
nested stacks
destination ownership
Push/Pop semantics
```

---

## 119. Rejected Alternative: Navigation Owns Application State

Navigator should not become a general store for screen data.

Its job is destination stack/lifecycle.

---

## 120. Rejected Alternative: Navigator Requires MonoBehaviour

Navigation belongs to LumaFlow runtime and must work in EditorWindow contexts.

No MonoBehaviour requirement.

---

## 121. Architectural Invariants Created by This ADR

Unless superseded:

### Invariant 1

Navigator manages UI destinations, not Unity scenes.

### Invariant 2

Navigator is scoped through BuildContext.

### Invariant 3

Global Navigator singleton is not required.

### Invariant 4

Navigation entries own mounted screen lifecycle.

### Invariant 5

Nested Navigators are supported.

### Invariant 6

Nearest Navigator scope wins.

### Invariant 7

Screens are ordinary Widgets/Views unless additional abstraction proves necessary.

### Invariant 8

Core navigation is typed rather than string-route-only.

### Invariant 9

Navigation transitions are layered above stack semantics.

### Invariant 10

Navigation does not own general application state.

---

## 122. Codex Rules

### Rule 1

Do not couple Navigator Core to SceneManager.

### Rule 2

Do not create global static current Navigator state.

### Rule 3

Resolve Navigator through BuildContext.

### Rule 4

Use normal WidgetNode mount/unmount lifecycle for destinations.

### Rule 5

Do not rebuild unrelated application UI during navigation.

### Rule 6

Do not introduce route-string registries before a concrete deep-link/router need exists.

### Rule 7

Do not assume each screen type exists only once in the stack.

### Rule 8

Do not hide application state inside Navigator entries unless it is navigation-specific.

### Rule 9

Do not add animation complexity to initial Push/Pop semantics.

### Rule 10

Before finalizing inactive-screen retention, test memory, state preservation, focus, scroll, and subscription behavior.

---

## 123. Decision Test

When implementing a navigation feature:

```text
Is this changing the current UI destination/history?
        ↓ yes
Navigator may own it.

Is this loading a Unity world/scene?
        ↓ yes
Application/SceneManager concern.

Does the behavior need nested independent stacks?
        ↓ yes
Use scoped Navigator.

Is it a temporary dialog/popover?
        ↓ yes
Likely Overlay, not main Navigator.

Does it require URL parsing?
        ↓ yes
Future Router layer above Navigator.
```

---

## 124. Initial Target API

Conceptually:

```csharp
Button(
    "Settings",
    onPressed: () =>
        context.Navigator.Push(
            new SettingsScreen()
        )
)
```

Back:

```csharp
Button(
    "Back",
    onPressed: () =>
        context.Navigator.Pop()
)
```

Replace:

```csharp
context.Navigator.Replace(
    new HomeScreen()
);
```

The exact method signatures remain subject to API review.

---

## 125. Example Nested Flow

```text
Application Navigator
├── Home
└── SettingsShell
    └── Settings Navigator
        ├── General
        ├── Audio
        └── Advanced
```

Inside `Audio`:

```csharp
context.Navigator.Push(
    new AdvancedAudioScreen()
);
```

acts on the Settings Navigator.

The application root stack remains unchanged.

---

## 126. Example Scene Transition

A LumaFlow screen may still intentionally do:

```csharp
Button(
    "Start Game",
    onPressed: StartGame
)
```

where:

```csharp
void StartGame()
{
    SceneManager.LoadScene("Game");
}
```

This is application behavior.

Navigator does not need to know about it.

---

## 127. Initial Implementation Target

The first Navigator prototype should prove:

```text
Navigator
NavigatorHost
BuildContext scoping
Push
Pop
Replace
CanPop
screen lifecycle
nested Navigator
```

without:

```text
animations
named routes
deep links
navigation results
guards
state restoration
```

---

## 128. Decision Gate: Screen Retention

Before calling navigation stable, explicitly decide between:

```text
keep inactive screens mounted
```

and:

```text
unmount inactive screens
```

or a documented hybrid policy.

The decision must be based on:

```text
state preservation
memory
focus
scroll position
reactive workload
Editor behavior
runtime performance
```

If necessary, create a dedicated follow-up ADR.

---

## 129. Long-Term Direction

Navigation may later expand with:

```text
transitions
typed results
deep linking
Router
state restoration
navigation guards
tab navigation
route observers
analytics hooks
```

These features should build on the same scoped Navigator stack.

---

## 130. Reconsideration Conditions

Revisit this ADR if:

1. scene-based applications require tighter optional integration;
2. web/deep-link use becomes central;
3. UI Toolkit introduces a native navigation architecture worth adopting;
4. screen retention semantics require a fundamentally different runtime model;
5. nested Navigator behavior proves too complex for common Unity UI.

Any replacement must preserve independent scoped navigation unless evidence strongly argues otherwise.

---

## 131. Final Decision

LumaFlow navigation is:

```text
typed
+
Widget-based
+
stack-oriented
+
BuildContext-scoped
+
lifecycle-owned
+
independent from Unity scenes
```

The guiding rule is:

**Navigator changes where the user is inside the UI.  
SceneManager changes which Unity scene is running.**

These are different responsibilities and LumaFlow keeps them separate.
