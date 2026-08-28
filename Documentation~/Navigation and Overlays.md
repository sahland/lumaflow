# Navigation and overlays

Navigation uses an explicitly owned, retained stack. Overlays use a separate
scoped stack above application content. `Navigator` and
`OverlayController` are controllers: create them outside `Build`, retain them,
and mount each below exactly one matching host.

## Keyed routes

The compatibility API still accepts a widget directly:

```csharp
navigator.Push(new ProjectDetails(project));
```

Use an explicit `Route` when identity, restoration, or a transition matters:

```csharp
var navigator = new Navigator(new Route(
    new WidgetKey("dashboard"),
    new DashboardPage()));

navigator.Push(new Route(
    new WidgetKey($"project:{project.Id}"),
    new ProjectDetails(project),
    RouteTransition.Fade(TimeSpan.FromMilliseconds(180))));
```

Keys must be valid and unique within one stack. A failed mount or duplicate-key
push leaves the active route and stack unchanged. `CurrentRouteState`,
`DepthState`, and `CanPopState` notify only after a navigation operation has
committed. Observer failures are reported after the stack and mounted hierarchy
are coherent.

`Push` retains the previous mounted subtree. `Pop` reveals it again and attempts
to restore its last focused native element. `Replace` removes only the active
entry. `PopToRoot` retains the original root; `ClearAndPush` removes every old
entry and establishes a new root.

## In-memory restoration

```csharp
NavigationSnapshot snapshot = navigator.CaptureSnapshot();

// A later application/session scope:
var restoredNavigator = new Navigator(snapshot);
```

`NavigationSnapshot` is immutable and validates unique keys. It retains route
widget descriptions by reference and restores their order and active route.
It is not a disk format. Applications that need process-restart
restoration must serialize route keys/arguments and rebuild equivalent `Route`
objects through their own feature boundary. Mount-local `StatefulWidget` state
is not serialized by a navigation snapshot; domain state should remain in
externally owned application state.

`RouteTransition` applies only to route activation and currently supports
`None` and `Fade`. The common animation API supplies easing, cancellation and
reduced-motion behavior.

## Back and Escape

Mount `BackNavigation` around the matching navigator and overlay scopes. It
consumes Escape/back in this order:

1. the top overlay entry;
2. the current navigator route;
3. no action at the root with no overlay.

A modal with `dismissOnBack: false` still consumes back, so input cannot leak
through and pop the route behind a required decision.

## Modal ownership and drawers

```csharp
var handle = overlay.ShowModal(
    new Dialog(new Text("Delete this project?")),
    new ModalOptions(
        dismissOnBarrier: false,
        dismissOnBack: false,
        requestFocus: true,
        restoreFocus: true,
        barrierColor: new Color(0f, 0f, 0f, 0.32f)));

var drawer = overlay.ShowDrawer(
    new ProjectFilters(),
    DrawerPlacement.Right);
```

The top modal owns input. Normal content and overlay entries below it are
disabled and non-pickable until the modal closes. Barriers and hosts remain in
strict insertion order. Closing an entry removes its node, barrier, scheduled
work, anchor callbacks, and native hierarchy even when content cleanup reports
an exception. A modal requests its first focusable descendant by default. Focus
restoration is best-effort and occurs only when the previous native element is
still attached and enabled.

`Show`, `ShowPopover`, and `ShowToast` remain non-modal. Popovers close when
their anchor detaches. Toast timers are cancelled during explicit close or host
teardown. `TryCloseTop` is an explicit programmatic close and therefore ignores
back-dismiss policy; `BackNavigation` respects it.

Use `TooltipAnchor` when a visual tooltip should appear on hover or focus. It
owns its popover handle and removes it when its child loses hover/focus or the
anchor unmounts; `Tooltip.AttachTo` remains available as a native fallback for
existing UI Toolkit elements.

## Retention and performance

Inactive routes remain mounted, subscribed, and present as hidden native route
layers. This preserves local UI state but consumes memory proportional to stack
depth. Use `Replace`, `PopToRoot`, or `ClearAndPush` for flows that do not need
the full history. Performance tests record mount, push, pop, 32-route disposal,
32-route snapshot restoration, and a 16-entry modal open/close workload.
Performance budgets must be established for each target platform.
