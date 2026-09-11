# ADR-023: Retain Mounted Navigator Route Entries Within a Host Lifetime

- **Status:** Accepted
- **Date:** 2026-08-20
- **Scope:** Runtime navigation lifecycle and reconciliation

## Context

The first `NavigatorHost` implementation kept only the active route mounted.
`Push` unmounted the previous route and `Pop` rebuilt its widget description.
That kept the native hierarchy small, but discarded mount-local
`StatefulWidget` state, native control identity, focus attachments, scroll
position, and other UI Toolkit state whenever the user returned to a screen.

Flutter's Navigator retains route entries in an overlay while a route remains
in history. LumaFlow needs the same developer-facing state continuity, without
copying Flutter's rendering or animation implementation.

## Decision

1. A mounted `NavigatorHostNode` owns one internal route entry per description
   in the Navigator stack. Entry identity is the stack entry itself, not widget
   type, widget instance, position alone, or public `WidgetKey`.
2. Every entry has a host-owned native `VisualElement` layer. The route widget
   mounts inside that layer using the Navigator-scoped `BuildContext`.
3. The active entry is displayed, enabled, and pickable. Inactive history
   entries remain mounted but are `display: none`, disabled in the native
   hierarchy, and non-pickable. Hidden controls therefore cannot receive input
   or programmatic focus through a LumaFlow `FocusNode`.
4. `Push` mounts the incoming entry before it mutates visible history. A mount
   failure leaves the previous route and Navigator stack unchanged.
5. `Pop` reveals the retained previous entry and releases only the outgoing
   entry. `Replace` mounts a new active entry and releases the replaced entry.
   `PopToRoot` releases every entry above the retained root.
6. Route cleanup uses ordinary `WidgetNode` ownership and `BindingScope`
   disposal. Navigator does not introduce a parallel cleanup system.
7. The host records the last focused native element of a route when a real
   panel is available and attempts to restore it when that retained route is
   revealed. Focus restoration is best-effort; an element that was removed,
   disabled, or moved outside the entry is not focused.
8. Retention lasts only for one mounted `NavigatorHost`. Unmounting the host
   releases every retained entry, removes the host-owned route layers, and
   clears entry/focus references even when the detached host element or mount
   handle remains referenced. If an externally supplied Navigator is later
   mounted again, its route descriptions are rebuilt; disposed native nodes are
   not resurrected.
9. Application-critical data still belongs outside screen-local state when it
   must survive host disposal, domain reload, scene reload, or process restart.

## Failure semantics

- A route build failure is transactional: the temporary native layer is
  removed and no stack state is committed.
- Stack descriptions and mounted entry order are committed together before
  observer failures are reported.
- `DepthState` and `CanPopState` are both updated even if a subscriber throws;
  notification failures are propagated after the Navigator reaches a coherent
  state.
- Cleanup failures are propagated, but cannot keep a removed route entry in the
  logical stack.

## Consequences

- Returning with `Pop` preserves mount-local state, native element identity,
  focus attachments, and native scroll/control state.
- Inactive routes keep their subscriptions and native hierarchy alive. This is
  an intentional memory/update cost and must be characterized before beta.
- Route transitions can later animate the existing entry layers without
  redefining route ownership.
- Public `Push`, `Pop`, `Replace`, and `PopToRoot` APIs remain source-compatible.
- Contract tests must cover duplicate route types, state retention, inactive
  focus blocking, build failure rollback, per-operation cleanup, and host
  disposal.
- Performance tests record named batched mount, retained push/pop, and
  32-entry host-disposal samples together with `GC.Alloc`. These establish
  comparable data; alpha results do not become release budgets until Player,
  IL2CPP, CI history, and target hardware are represented.
