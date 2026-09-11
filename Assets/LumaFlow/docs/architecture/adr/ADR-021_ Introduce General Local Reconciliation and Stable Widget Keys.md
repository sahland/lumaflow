# ADR-021: Introduce General Local Reconciliation and Stable Widget Keys

- **Status:** Accepted
- **Decision date:** 2026-08-20
- **Scope:** Widget updates, sibling identity, state retention, structural rebuilds
- **Affects:** Runtime, WidgetNode, StatefulWidget, StatelessWidget, ReactiveBuilder, layout parents, tests, diagnostics
- **Supersedes:** ADR-004 sections that defer general reconciliation and widget keys

---

## Context

ADR-004 deliberately deferred reconciliation until dogfooding demonstrated a
real need. The Dashboard dogfood and framework audit now provide that evidence:

- compatible `ReactiveBuilder` and `StatefulWidget` output remounts native UI;
- nested local state and focus are lost across ordinary structural rebuilds;
- identity preservation exists only in the special `KeyedRow`/`KeyedColumn` APIs;
- responsive `LayoutBuilder` branches create avoidable native hierarchy churn;
- specialized rebuild paths are starting to duplicate the same replacement logic.

The original decision gate is therefore satisfied. Continuing to add components
without a shared update contract would make later API stabilization more costly.

## Decision

LumaFlow will introduce **general local reconciliation** at explicit rebuild
boundaries. This is not a global frame-driven Flutter rebuild loop and it does
not replace UI Toolkit/Yoga rendering or layout.

The update model remains localized:

```text
State or inherited dependency changes
    -> affected builder/stateful boundary builds a new description
    -> compatible mounted nodes update in place
    -> incompatible nodes mount before the previous nodes are released
```

## Identity and compatibility

Mounted identity belongs to `WidgetNode`, never to a Widget object reference.

A mounted node is eligible for update when:

```text
same runtime widget type
+ same optional WidgetKey
```

For direct multi-child parents:

- an unkeyed child matches only the existing child at the same position;
- a keyed child matches the sibling with the same key and may move;
- sibling keys must be unique;
- a type mismatch replaces the node even when the key matches;
- object reference equality and Widget value equality are not identity rules.

`Widget.WithKey(WidgetKey)` and `KeyedSubtree` provide general identity without
requiring a key parameter on every alpha constructor immediately. Constructor-
level key ergonomics may be added before API stabilization.

## Lifecycle

Compatible `StatefulWidget` updates retain their `WidgetState` and invoke
`DidUpdateWidget`. Compatible `StatelessWidget` nodes rebuild their local child
description while retaining compatible descendants. Removed or incompatible
nodes unmount normally and dispose their bindings exactly once.

Future inherited dependency work will add `DidChangeDependencies`; this ADR
does not treat dependency changes as ordinary widget configuration changes.

## Failure behavior

Builders are evaluated before the current child is released. Incompatible
replacement nodes mount before the previous subtree is unmounted, preserving
the existing visible subtree when building or mounting the replacement fails.

Multi-child reconciliation validates null children and duplicate keys before
mutation. Updates are applied locally in sibling order. Full transaction rollback
of already-successful compatible child updates is not promised; every mounted
node must nevertheless remain lifecycle-valid and owned after an exception.

If a replacement mounts successfully but releasing the previous child fails,
the replacement is committed as the current owned child before the cleanup
failure is propagated. Multi-child and specialized action collections attempt
every stale-child cleanup, commit the resolved hierarchy, and aggregate failures
afterward. Failed inherited evaluations remove dependencies read by the failed
build before restoring the previous dependency set. These rules keep later
update and unmount operations valid after an exception.

## Performance model

LumaFlow continues to prefer direct property bindings for simple changes.
Reconciliation runs only inside a boundary that explicitly rebuilds and its cost
is proportional to that local child set/subtree, not to the whole application.

No per-frame tree scan, WidgetNode pooling, VisualElement diff, or custom layout
engine is introduced.

## Initial rollout

1. Core single-child reconciliation for stateful, stateless, keyed, and reactive boundaries.
2. Multi-child reconciliation for ordinary `Row`, `Column`, and `Stack`.
3. In-place updates for leaf and layout nodes, beginning with `Text` and the
   common single-child layout wrappers.
4. Expansion to remaining layout, controls, navigation, overlay, and list nodes.
5. Reactive inherited context and dependency lifecycle. The initial specialized
   `Theme`/`MediaQuery` dependency scopes are implemented; expansion and
   diagnostics remain part of the rollout.
6. Diagnostics, benchmarks, visual tests, and compatibility gates.

Until rollout is complete, a node whose implementation does not opt into update
is replaced safely. This is an implementation stage, not the final beta contract.

## Non-goals

- copying Flutter's RenderObject or scheduling pipeline;
- rebuilding the application root for every `State<T>` change;
- implementing `GlobalKey` reparenting;
- reconciling native `VisualElement` trees directly;
- changing native `ListView` virtualization into ordinary child reconciliation.

## Required validation

- compatible `Text` updates preserve its native `Label`;
- compatible stateless and stateful chains preserve mounted descendants;
- keyed children reorder in ordinary multi-child parents without state loss;
- duplicate sibling keys fail before mounting ambiguous children;
- removed nodes dispose state and subscriptions exactly once;
- incompatible replacements preserve mount-before-release behavior;
- existing Runtime and Editor layout contracts remain green.

The first implementation tranche also covers compatible `Button` and
structurally compatible `TextField` updates. A text-field update is currently
structurally compatible only when it does not add or remove the supporting/error
slot wrapper; changing that native shape remains an explicit remount boundary.

The second tranche extends compatible updates to the controlled input family,
`ListView<T>`, `ReactiveBuilder<T>` configuration changes, `LayoutBuilder`
responsive branches with one persistent `MediaQuery` scope, forms and focus
groups, app/scaffold/navigation chrome, keyed flex collections, common overlay
hosts/content, dialogs, async scopes, and animated opacity. A different
`FormState`, `Navigator`, or `OverlayController` remains an explicit scope-owner
remount boundary. `NavigatorHost` now retains its internal history entries under
ADR-023; that route-entry ownership is a navigation lifecycle contract layered
on top of, rather than implied by, ordinary child reconciliation.

The final built-in-node tranche adds `ConfirmDialog` and `Native`. A compatible
confirmation update retains its mounted dialog and content state, declines and
cancels the replaced result owner, and transfers unmount completion to the latest
configuration. `Native` is compatible only when the next description supplies
the same borrowed `VisualElement` or the same factory delegate; a different
native source remains an explicit remount boundary. All built-in nodes now have
an explicit or inherited update contract, while the narrower compatibility rules
above remain intentional.

`KeyedRow` and `KeyedColumn` now delegate identity and failure semantics to the
same `KeyedSubtree`/general multi-child reconciler as ordinary flex layouts;
they no longer release an incompatible keyed child before its replacement has
mounted successfully.

## Consequences

Positive:

- state, focus, scroll, and native identity can survive compatible rebuilds;
- specialized keyed layout APIs are no longer the only identity mechanism;
- component authors can use declarative rebuilds without automatic native churn;
- the framework gains a coherent basis for inherited context and animations.

Costs:

- every reusable node needs an explicit update contract;
- style removal/reset semantics must be tested, not only style application;
- lifecycle and failure-path tests become release-critical;
- diagnostics must eventually explain why a node updated, moved, or remounted.

## Final rule

Use direct native mutation when only a property changes. When a local Widget
description is rebuilt, reconcile compatible nodes by type, position, and key;
remount only when compatibility fails.
