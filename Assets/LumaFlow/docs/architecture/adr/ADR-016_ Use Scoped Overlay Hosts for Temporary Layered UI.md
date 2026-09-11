# ADR-016: Use Scoped Overlay Hosts for Temporary Layered UI

- **Status:** Accepted
- **Decision date:** 2026-08-11
- **Scope:** Dialogs, modals, popovers, context menus, tooltips, toasts, layered UI
- **Affects:** Runtime, BuildContext, Lifecycle, Focus, Input, Navigation, Theme
- **Related documents:** `ADR-004-reconciliation-strategy.md`, `ADR-008-lifecycle-and-ownership.md`, `ADR-011-build-context-and-scoping.md`, `ADR-015-navigation-model.md`

---

## 1. Context

Modern interfaces frequently need temporary content rendered above the normal UI hierarchy.

Examples include:

```text
Dialog
Modal
Popover
ContextMenu
Tooltip
Toast
Notification
Dropdown menu
Command palette
```

These elements differ from normal layout content because they may require:

```text
z-order above surrounding content
focus capture
pointer blocking
screen-relative positioning
anchor-relative positioning
automatic dismissal
independent lifetime
```

They also differ from normal navigation.

Opening a confirmation dialog should not necessarily create another entry in the main navigation stack.

LumaFlow therefore needs a dedicated overlay architecture.

---

## 2. Decision

LumaFlow will provide a scoped overlay system centered around:

```text
OverlayHost
OverlayController
OverlayEntry
```

or equivalent concepts.

Conceptually:

```text
Application UI
    ↓
OverlayHost
├── normal content
└── overlay layer
    ├── Dialog
    ├── Popover
    └── Tooltip
```

Temporary layered UI is mounted into the nearest applicable OverlayHost.

---

## 3. Core Principle

Use:

```text
Navigator
```

for:

```text
destination history
screen transitions
Push / Pop
```

Use:

```text
Overlay
```

for:

```text
temporary UI above the current destination
```

These systems may cooperate but remain architecturally distinct.

---

## 4. OverlayHost

`OverlayHost` defines a scope where temporary overlay entries can be mounted.

Conceptually:

```text
OverlayHostNode
├── content subtree
└── overlay container
```

The overlay container should appear above normal host content.

---

## 5. Scoped Overlay Access

Overlay access should be resolved through `BuildContext`.

Conceptually:

```csharp
context.Overlay.Show(...);
```

or:

```csharp
context.Overlay.ShowDialog(...);
```

Exact naming is deferred.

Do not require:

```csharp
OverlayManager.Instance
```

as the core API.

---

## 6. Nearest OverlayHost Wins

Following ADR-011:

```text
Root OverlayHost
    ↓
Nested OverlayHost
        ↓
Child
```

The child resolves the nested host.

This allows isolated UI regions and nested flows.

---

## 7. Root OverlayHost

A normal LumaFlow application will likely establish one root OverlayHost automatically or through the application shell.

Conceptually:

```text
LumaFlow Root
└── OverlayHost
    ├── NavigatorHost
    └── Overlay Layer
```

Exact bootstrap composition is deferred.

---

## 8. OverlayEntry

Every active temporary overlay is represented by a runtime entry.

Conceptually:

```text
OverlayEntry
├── Widget
├── mounted WidgetNode subtree
├── overlay type
├── dismissal policy
└── positioning metadata
```

The entry is owned by its OverlayHost.

---

## 9. Overlay Lifecycle

Opening:

```text
Create OverlayEntry
↓
mount overlay subtree
↓
attach to overlay layer
↓
activate behavior
```

Closing:

```text
deactivate
↓
unmount overlay subtree
↓
dispose bindings/events
↓
remove OverlayEntry
```

This must reuse ADR-008 lifecycle infrastructure.

---

## 10. No Separate Cleanup Model

Overlays must not invent their own:

```text
State subscription system
event ownership
Widget lifecycle
resource cleanup
```

An overlay contains an ordinary LumaFlow mounted subtree.

---

## 11. Overlay Ordering

OverlayHost owns the logical ordering of entries.

Conceptually:

```text
Entry A
Entry B
Entry C
```

where C is visually above B and B above A.

Do not allow unrelated application code to manipulate native z-order behind the OverlayHost.

---

## 12. Entry Order

Initial implementation may use insertion order:

```text
latest overlay
=
topmost overlay
```

Specialized categories may later have separate priorities.

Do not introduce a complex arbitrary z-index system prematurely.

---

## 13. Overlay Categories

Conceptually, overlays may belong to categories such as:

```text
Modal
Popover
Tooltip
Toast
Menu
```

Different categories may require different interaction policies.

They should still use the same core host/entry infrastructure.

---

## 14. Dialog

A Dialog is a temporary focused surface.

Example target API:

```csharp
context.Overlay.ShowDialog(
    Dialog(
        title: Text("Delete clip?"),
        content: Text("This action cannot be undone."),
        actions:
        [
            Button("Cancel"),
            Button(
                "Delete",
                variant: ButtonVariant.Danger
            )
        ]
    )
);
```

Exact API remains subject to design.

---

## 15. Modal Dialog

A modal entry prevents interaction with underlying UI while active.

Conceptually:

```text
OverlayHost
├── normal UI
├── modal barrier
└── modal content
```

The barrier and content are owned by one modal entry.

---

## 16. Modal Barrier

The barrier may provide:

```text
pointer blocking
background dimming
click-outside dismissal
```

depending on configuration.

The barrier should remain themeable.

---

## 17. Barrier Is Semantic

Applications should request:

```text
modal
```

rather than manually creating a full-screen black VisualElement and wiring pointer events.

The overlay system owns modal interaction semantics.

---

## 18. Modal Dismissal

Possible policies:

```text
explicit only
tap/click outside
Escape/back
programmatic
```

These should be typed options.

Avoid several overlapping booleans if a clear dismissal policy type is more expressive.

---

## 19. Programmatic Close

Opening an overlay should return some form of handle when useful.

Conceptually:

```csharp
var handle = context.Overlay.Show(...);

handle.Close();
```

The exact type may be:

```text
OverlayHandle
OverlayEntryHandle
```

and should follow deterministic ownership.

---

## 20. OverlayHandle

Conceptual contract:

```csharp
public interface IOverlayHandle : IDisposable
{
    bool IsOpen { get; }

    void Close();
}
```

Exact shape is deferred.

`Dispose()` should normally close the entry safely.

---

## 21. Handle Idempotency

Calling:

```text
Close
Close
```

or:

```text
Dispose
Dispose
```

should not corrupt the host.

Idempotent close semantics are preferred.

---

## 22. External Host Removal

If the owning OverlayHost unmounts:

```text
all active entries
↓
must close/unmount automatically
```

No overlay may outlive its host.

---

## 23. Context Propagation

An overlay subtree should receive context derived from the OverlayHost location.

Therefore it naturally sees:

```text
Theme
Navigator
MediaQuery
Overlay scope
Localization
```

according to normal scoping rules.

---

## 24. Nested Context

An overlay opened inside a scoped theme should normally inherit that scoped theme when the resolved OverlayHost belongs to the same scope.

This must remain predictable.

---

## 25. Root Overlay Escape

There may eventually be use cases requiring an explicitly root-level overlay.

Example:

```text
global application notification
```

Do not expose root-host access by default.

If needed, introduce an explicit API later.

Nearest scope remains the standard behavior.

---

## 26. Popover

A Popover is an overlay positioned relative to an anchor.

Examples:

```text
dropdown menu
color picker
small inspector
profile menu
```

It requires geometry/positioning behavior beyond ordinary flex layout.

---

## 27. Anchor

A Popover may be anchored to a mounted native element.

Conceptually:

```text
anchor VisualElement
        ↓
resolved geometry
        ↓
popover placement
```

This is a legitimate use of UI Toolkit geometry APIs.

---

## 28. No General Custom Layout Engine

Popover placement may perform specialized positioning calculations.

This does not violate ADR-009 because:

```text
specialized overlay placement
≠
general UI layout engine
```

Normal Widget layout remains UI Toolkit-owned.

---

## 29. Anchor Lifetime

If the anchor unmounts while a Popover is active, the Popover should normally close automatically.

Do not allow an overlay to continue positioning relative to a dead element.

---

## 30. Anchor Reference

A future safe API may use a native reference abstraction instead of application code directly retaining WidgetNode.

Example:

```text
ElementAnchor
NativeRef<T>
OverlayAnchor
```

Exact mechanism is deferred.

---

## 31. Popover Position Strategy

Potential placements:

```text
Above
Below
Left
Right
Auto
```

with alignment options.

`Auto` may select placement based on available panel space.

---

## 32. Viewport Clamping

Popovers should avoid rendering entirely outside their host bounds where practical.

The specialized positioning algorithm may clamp or flip placement.

This behavior must be deterministic.

---

## 33. Context Menu

Context menus are a specialized Popover/Menu behavior.

Example:

```text
right-click
↓
ContextMenu overlay
```

LumaFlow may use native UI Toolkit contextual menu APIs where they already provide a suitable implementation.

Do not recreate native behavior purely for architectural consistency.

---

## 34. Native Context Menu Integration

ADR-001 applies:

```text
native UI Toolkit solves it well
↓
adapt/use native capability
```

A LumaFlow semantic API may wrap native context menus while preserving common theme/component behavior.

---

## 35. Menu Items

A future typed menu model may expose:

```text
label
icon
enabled
checked
callback
separator
submenu
```

Do not implement an entire menu framework until needed.

---

## 36. Tooltip

Tooltip is a lightweight temporary overlay tied to an anchor.

Potential trigger:

```text
hover delay
focus
```

Native UI Toolkit tooltip support should be used where sufficient.

---

## 37. Native Tooltip Preference

If native `VisualElement.tooltip` provides the desired behavior:

```text
use it
```

rather than mounting a custom overlay.

A custom LumaFlow tooltip system is only justified for richer Widget content or advanced behavior.

---

## 38. Rich Tooltip

A richer future API may support:

```csharp
Tooltip(
    content: Text("..."),
    child: Button(...)
)
```

or equivalent.

This may use the OverlayHost.

---

## 39. Toast

Toast/notification entries are temporary non-modal overlays.

Conceptually:

```text
OverlayHost
└── Toast region
    ├── Toast A
    └── Toast B
```

They do not block underlying interaction.

---

## 40. Toast Lifetime

A toast may close:

```text
automatically after duration
explicitly
through action
```

Time-based dismissal should use lifecycle-owned scheduled work.

Unmounting the host cancels schedules.

---

## 41. Toast Timing

Default duration, motion, and visual appearance should come from semantic component/theme configuration where appropriate.

Do not scatter timeout literals.

---

## 42. Toast Queue

A future ToastHost may manage:

```text
maximum visible count
queue
stack direction
deduplication
```

This is higher-level behavior built on OverlayEntry.

It should not redefine the core overlay lifecycle.

---

## 43. Notification vs Toast

Do not introduce multiple nearly identical concepts without semantic difference.

Names should be settled based on actual product API.

---

## 44. Focus

Modal overlays frequently require focus management.

At minimum:

```text
underlying controls must not continue receiving focus/input
```

while modal interaction is active.

---

## 45. Focus Trap

A future modal implementation may trap keyboard focus inside itself.

This should build on UI Toolkit focus behavior rather than implementing a separate focus engine.

---

## 46. Initial Focus

Dialogs may eventually allow:

```text
initial focus target
```

or automatically choose the first suitable control.

Do not make elaborate focus heuristics an MVP requirement.

---

## 47. Focus Restoration

When a modal closes, restoring focus to the previously focused element is desirable where safe.

Potential flow:

```text
open modal
↓
remember previous focus
↓
modal focus
↓
close
↓
restore previous focus if still mounted
```

This should be tested against UI Toolkit behavior.

---

## 48. Dead Focus Target

If the previous focus target was unmounted while the overlay was active:

```text
do not restore it
```

Fall back to normal host focus behavior.

---

## 49. Pointer Blocking

Modal barriers should intercept pointer interaction before it reaches underlying content.

Use native UI Toolkit picking/event behavior.

Do not install a global application input lock for ordinary modal UI.

---

## 50. Keyboard Blocking

Keyboard shortcuts outside a modal may also require suppression depending on application architecture.

The overlay system should not globally intercept every application input source.

It controls the UI Toolkit focus/event scope it owns.

---

## 51. Escape Handling

Escape is a common dismiss action.

It should respect:

```text
topmost dismissible entry
```

rather than closing arbitrary overlays.

---

## 52. Topmost Policy

When multiple overlays exist:

```text
only the appropriate topmost overlay
```

should generally process generic dismissal commands.

This prevents one Escape press from closing several layers.

---

## 53. Stacked Modals

The system may permit:

```text
Modal A
↓
Modal B
```

though application UX should use this sparingly.

Ownership remains stack-like within the OverlayHost.

---

## 54. Overlay Is Not Navigator

Even though overlay entries may form a stack, this does not make OverlayHost another Navigator.

Difference:

```text
Navigator:
destination history

Overlay:
temporary layered presentation
```

Overlay entries normally disappear without changing the current destination.

---

## 55. Navigation While Overlay Is Open

Applications may navigate while overlays exist.

The policy depends on overlay scope.

If the overlay belongs to the current screen subtree and that screen unmounts:

```text
overlay host/subscope disappears
↓
overlay closes
```

This produces natural ownership.

---

## 56. Root Overlay and Navigation

If a root OverlayHost sits above Navigator:

```text
root-level overlays
```

may survive a route change.

This can be useful for global notifications.

The composition chosen by the application determines semantics.

---

## 57. Dialog Navigation

A dialog may invoke Navigator callbacks.

Example:

```text
Confirm
↓
close dialog
↓
navigate
```

The callback should explicitly define operation order.

LumaFlow should not automatically convert a dialog action into navigation.

---

## 58. Overlay Results

Dialogs often return results.

Potential future API:

```csharp
var confirmed =
    await context.Overlay.ShowDialog<bool>(...);
```

This is attractive but requires:

```text
Task cancellation
host unmount semantics
multiple close paths
result ownership
```

Therefore async result APIs are deferred.

---

## 59. Callback-Based Initial API

MVP may use explicit callbacks:

```csharp
ShowDialog(
    ConfirmDialog(
        onConfirm: ...,
        onCancel: ...
    )
);
```

plus OverlayHandle.

Simpler lifecycle first.

---

## 60. Async Result Gate

Typed async overlay results may be added after:

```text
OverlayHandle
host teardown
dismiss semantics
cancellation
```

are proven.

---

## 61. Modal Barrier Styling

Barrier appearance should be theme-driven.

Conceptual:

```text
Theme.Overlay.ModalBarrierColor
```

or a component theme.

The exact ThemeData extension should follow ADR-005 extensibility principles.

---

## 62. Overlay Theme

Potential component theme groups:

```text
DialogTheme
TooltipTheme
MenuTheme
ToastTheme
OverlayTheme
```

Do not create all of these before components exist.

---

## 63. Overlay Position and Layout

Overlay root typically occupies the host's available bounds.

Entries inside may use:

```text
flex centering
absolute positioning
anchor positioning
```

through normal/native UI Toolkit semantics.

---

## 64. Dialog Position

A normal modal Dialog can often use:

```text
Center
```

inside a full-host barrier.

No manual x/y calculation required.

---

## 65. Popover Position

Anchor-based Popover is the primary case where explicit geometry calculations are justified.

Keep this code isolated.

---

## 66. Overlay Host Native Structure

Potential native hierarchy:

```text
OverlayHostRoot
├── ContentContainer
└── OverlayContainer
```

where OverlayContainer sits above ContentContainer.

Exact UI Toolkit hierarchy must be validated.

---

## 67. Wrapper Cost

An extra host native structure is acceptable because it provides real behavior:

```text
layering
ownership
input management
positioning
```

This is not a meaningless wrapper.

---

## 68. Native Hierarchy Safety

External code must not manually clear or reorder OverlayHost-owned native children.

Overlay ownership must remain authoritative.

---

## 69. Picking Mode

Overlay containers and barriers must use appropriate native picking behavior.

A transparent top-level overlay container must not accidentally block all underlying input when no modal entry requires it.

---

## 70. Non-Modal Overlays

A Tooltip or Toast should generally allow interaction with underlying UI unless its own visible element receives input.

Overlay root implementation must support this distinction.

---

## 71. Clipping

OverlayHost must carefully choose clipping behavior.

Popovers and menus may need to render outside local content bounds.

If the host itself is clipped, a more suitable ancestor OverlayHost may be required.

---

## 72. Scope Choice Matters

A local OverlayHost inside a clipped panel provides local overlays.

A root OverlayHost can provide window-level overlays.

This is a useful architectural feature, not an error.

---

## 73. Editor Windows

Each EditorWindow can have its own OverlayHost.

Opening a menu/dialog in one window must not affect another.

No global editor overlay singleton is required.

---

## 74. Runtime Panels

Multiple UIDocuments/panels may have independent OverlayHosts.

This follows the general mount/context isolation architecture.

---

## 75. Native Editor APIs

Some Editor interactions already have high-quality Unity-native popup/menu APIs.

LumaFlow.Editor may use them where appropriate.

Not every Editor menu must be rendered through LumaFlow OverlayHost.

---

## 76. Platform Independence

Core overlays must remain usable across supported platforms.

Pointer-specific behavior should not make keyboard/touch use impossible.

---

## 77. Render Pipeline Independence

Ordinary overlay rendering remains UI Toolkit-based and pipeline-neutral.

Backdrop blur or other advanced modal effects belong in optional Effects under ADR-006.

---

## 78. Glass Dialogs

A Dialog may compose with:

```text
GlassSurface
```

from an optional Effects module.

Dialog semantics must not require that effect.

---

## 79. Animations

Initial overlays may open/close without animation.

Animation belongs to a later motion layer.

Correct lifecycle precedes transition polish.

---

## 80. Exit Animation

Future exit animations require a distinction between:

```text
Close requested
↓
entry still visually mounted during exit
↓
final unmount
```

This adds lifecycle complexity and must be introduced deliberately.

---

## 81. Input During Exit Animation

Once close is requested, an overlay should normally stop accepting semantic interaction even if it remains visually mounted for an exit animation.

This behavior belongs to the future transition implementation.

---

## 82. Overlay Entry State

Potential future lifecycle:

```text
Opening
Active
Closing
Closed
```

is only necessary once transitions exist.

MVP may remain:

```text
Mounted
Unmounted
```

---

## 83. Scheduled Dismissal

Toast timers or delayed Tooltip presentation must belong to entry/host lifetime.

No timer should call into a disposed entry.

---

## 84. Reentrant Close

An overlay callback may close itself.

Example:

```text
Delete button clicked
↓
handle.Close()
```

Framework event code must tolerate the overlay subtree unmounting during its own callback.

ADR-008 applies.

---

## 85. Close During Host Teardown

If both entry close and host unmount occur:

```text
cleanup must remain safe
```

Idempotent lifecycle is valuable here.

---

## 86. Close All

A host may eventually expose:

```text
CloseAll()
```

for application teardown.

This should close only entries belonging to that host.

---

## 87. Selective Close

Future APIs may close:

```text
topmost
specific handle
all of category
```

Do not introduce arbitrary querying/mutation of internal entry collections.

---

## 88. No Global Overlay Registry

Rejected:

```text
static List<OverlayEntry>
```

for normal runtime ownership.

Each OverlayHost owns its entries.

Diagnostic registries, if any, remain separate.

---

## 89. Overlay Debugging

Future developer tooling may display:

```text
OverlayHost
active entries
entry order
entry category
anchor
dismissal policy
focus state
```

This would be useful for complex UI.

---

## 90. Testing

Required tests should include:

```text
show overlay
close overlay
double close
host teardown
multiple overlay order
modal barrier
non-modal pointer behavior
nested OverlayHost
context inheritance
anchor removal
subscription cleanup
```

---

## 91. Modal Test

Open modal.

Verify:

```text
dialog visible
underlying pointer interaction blocked
dialog receives interaction
```

Close.

Verify underlying content becomes interactive again.

---

## 92. Nested Host Test

Structure:

```text
Root OverlayHost
└── Nested OverlayHost
    └── Button
```

Opening from Button should use Nested OverlayHost.

Root host remains unchanged.

---

## 93. Host Teardown Test

Open several overlays.

Unmount OverlayHost.

Verify:

```text
all entries unmounted
bindings disposed
timers stopped
handles become closed
```

---

## 94. Anchor Removal Test

Open anchored Popover.

Unmount anchor.

Expected:

```text
Popover closes safely
```

No stale geometry callbacks.

---

## 95. Reentrant Test

Dialog button closes its own OverlayHandle.

Verify no framework code touches disposed subtree afterward.

---

## 96. Multiple Mount Isolation

Two independent applications/windows with separate OverlayHosts must not share entries.

---

## 97. Dogfooding

AudioLib can validate overlays through:

```text
confirmation dialog
audio settings popover
context menu
tooltip
toast notification
```

This provides enough varied behavior to test the common infrastructure.

---

## 98. Rejected Alternative: Use Navigator for Everything

Rejected.

A tooltip or context menu should not become a navigation destination.

Navigation history and temporary presentation are different concerns.

---

## 99. Rejected Alternative: Every Widget Can Render Globally

Rejected architecture:

```text
Widget requests global screen layer
```

without an OverlayHost.

Reasons:

```text
global state
multiple window problems
unclear lifetime
unclear context
```

---

## 100. Rejected Alternative: Global Overlay Singleton

Rejected for the same reasons as global Navigator:

```text
multiple panels
multiple EditorWindows
nested scopes
testing
ownership
```

---

## 101. Rejected Alternative: Absolute Position Everything Manually

Rejected for normal overlays.

Dialogs should still use native layout.

Only specialized anchor placement should calculate geometry.

---

## 102. Rejected Alternative: Separate Overlay Widget Runtime

Rejected.

Overlays contain ordinary Widgets and WidgetNodes.

Only entry/host management is specialized.

---

## 103. Rejected Alternative: Modal Means Separate UIDocument

Rejected as default.

A modal is a UI subtree inside the relevant overlay scope.

No extra GameObject or UIDocument should be required.

---

## 104. Rejected Alternative: Scene-Based Dialogs

Rejected.

Dialogs must work in Editor windows and arbitrary UI panels.

---

## 105. Architectural Invariants Created by This ADR

Unless superseded:

### Invariant 1

Temporary layered UI is owned by OverlayHost.

### Invariant 2

Overlay access is scoped through BuildContext.

### Invariant 3

Nearest OverlayHost wins.

### Invariant 4

OverlayEntry owns an ordinary LumaFlow mounted subtree.

### Invariant 5

OverlayHost owns entry lifecycle.

### Invariant 6

Modal and non-modal interaction behavior are distinct.

### Invariant 7

Navigator and Overlay are separate responsibilities.

### Invariant 8

Anchored positioning may use specialized geometry without creating a general layout engine.

### Invariant 9

No global Overlay singleton is required.

### Invariant 10

Host unmount closes all owned overlays.

---

## 106. Codex Rules

### Rule 1

Do not implement Dialog, Tooltip, Popover, Toast, and ContextMenu using unrelated lifetime systems.

### Rule 2

Reuse OverlayHost/OverlayEntry infrastructure.

### Rule 3

Do not use Navigator as the default container for temporary overlays.

### Rule 4

Do not introduce a global overlay manager.

### Rule 5

Resolve overlay scope through BuildContext.

### Rule 6

Use normal WidgetNode lifecycle for overlay content.

### Rule 7

Stop entry-owned timers/events/subscriptions when the entry closes.

### Rule 8

If an anchor disappears, close or safely detach anchored overlays.

### Rule 9

Use UI Toolkit native layout/input/focus capabilities wherever practical.

### Rule 10

Do not add transition lifecycle complexity until basic overlay semantics are correct.

---

## 107. Decision Test

When presenting temporary UI:

```text
Is this a new destination in user history?
        ↓ yes
Navigator.

Is it temporary content above current UI?
        ↓ yes
Overlay.

Does it block interaction below?
        ↓ yes
Modal overlay.

Is it attached to another element?
        ↓ yes
Anchored Popover/Tooltip.

Does native UI Toolkit already provide the exact behavior well?
        ↓ yes
Prefer native adapter.
```

---

## 108. Initial MVP Overlay Set

The first implementation should focus on:

```text
OverlayHost
OverlayEntry
OverlayHandle
Modal barrier
Dialog
basic Popover
```

Then:

```text
ContextMenu
Tooltip
Toast
```

after the foundation is proven.

---

## 109. Example: Confirm Dialog

Conceptually:

```csharp
var handle = context.Overlay.Show(
    Dialog(
        title: Text("Delete sound?"),

        content: Text(
            "This action cannot be undone."
        ),

        actions:
        [
            Button(
                "Cancel",
                onPressed: () => handle.Close()
            ),

            Button(
                "Delete",
                variant: ButtonVariant.Danger,
                onPressed: Delete
            )
        ]
    )
);
```

Exact closure API must avoid initialization/capture awkwardness; this example only illustrates behavior.

---

## 110. Example: Popover

```csharp
context.Overlay.ShowPopover(
    anchor: buttonAnchor,
    placement: PopoverPlacement.BottomEnd,
    content: AudioOptionsMenu()
);
```

Conceptual behavior:

```text
resolve anchor geometry
↓
mount menu in OverlayHost
↓
position relative to anchor
↓
reposition if relevant geometry changes
↓
close when dismissed/anchor disappears
```

---

## 111. Example: Toast

Conceptually:

```csharp
context.Overlay.ShowToast(
    Toast(
        message: "Audio library refreshed"
    )
);
```

The toast:

```text
does not block normal UI
↓
uses host-owned lifetime
↓
closes automatically
```

---

## 112. Initial Implementation Target

Before advanced overlay components are added, prove:

```text
scoped OverlayHost
entry mount/unmount
entry ordering
OverlayHandle
modal barrier
context propagation
host teardown
reentrant close
```

This is enough foundation for subsequent components.

---

## 113. Long-Term Direction

Overlay infrastructure may later support:

```text
Dialog
Popover
ContextMenu
Tooltip
Toast
Notification
CommandPalette
Dropdown surfaces
animations
focus trapping
typed dialog results
```

These should remain variations of one scoped overlay architecture.

---

## 114. Reconsideration Conditions

Revisit this ADR if:

1. UI Toolkit introduces a superior universal overlay system;
2. native Editor popup behavior requires a separate adapter layer;
3. multi-panel overlays require cross-panel presentation;
4. focus trapping proves incompatible with the current host model;
5. overlay animations require lifecycle changes.

Any revision should preserve explicit scope and deterministic ownership.

---

## 115. Final Decision

LumaFlow overlay architecture is:

```text
BuildContext
    ↓
OverlayHost
    ↓
OverlayEntry
    ↓
ordinary Widget subtree
```

It supports temporary layered presentation without turning every transient surface into a navigation destination or global UI object.

The guiding rule is:

**Navigator changes the current destination.  
Overlay temporarily presents something above it.**