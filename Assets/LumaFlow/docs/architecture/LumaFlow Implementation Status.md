# LumaFlow Implementation Status

**Last reviewed:** 2026-08-24  
**Package version:** `0.1.0`  
**Status:** working alpha; API is actively evolving.

This is the factual complement to the development roadmap. It documents what is
implemented and covered by automated tests today, rather than what is merely
planned.

## Scope and supported backend

LumaFlow is a declarative C# layer over Unity UI Toolkit. It does not provide a
renderer or layout engine of its own.

```text
Widget description → WidgetNode lifecycle → native VisualElement hierarchy
```

Runtime code lives in `Packages/com.sahland.lumaflow/Runtime` and has no
`UnityEditor` dependency. The current minimum supported Unity version is Unity
6.

## Implemented capabilities

| Area | Current contract |
| --- | --- |
| Core | `Widget`, `StatelessWidget`, `StatefulWidget`, `WidgetState`, `WidgetNode`, deterministic mount/unmount, binding cleanup, general `WidgetKey`/`KeyedSubtree` identity, and `Native` UI Toolkit interop. |
| Context | Tree-scoped `BuildContext` carries theme, navigator, form, media-query, localization, and text-scaler data. `Theme`, `MediaQuery`, `Localizations`, and `TextScale` track mounted readers, invalidate only their dependents, isolate nested scopes, and release registrations on unmount. |
| Layout | `Row`, `Column`, `KeyedRow`, `KeyedColumn`, `Padding`, `SizedBox`, `ConstrainedBox`, `Align`, `Center`, `Expanded`, `Flexible`, `Spacer`, `Container`, `Card`, `ScrollView`, `ListView<T>`, `Stack`, and adaptive scaffold primitives. `gap` is applied without technical wrapper elements. S2 verifies explicit constraints, flex allocation, alignment, scroll viewport composition, and width-driven `LayoutBuilder` rebuilds. `ListView<T>` adds bounded keyed realized-row identity, controlled key selection, and retained scroll restoration over native virtualization. |
| Styling | Typed text, box, border, spacing, and radius values. `ThemeData` scopes colors, typography, spacing, radii, icon, text-field, and button themes. |
| Controls | `Text`, `Button`, `Icon`, `IconButton`, `TextField`, `FocusNode`, scoped `FocusTraversalGroup`, `Slider`, `Switch`, `Checkbox`, `Radio<T>`, `Dropdown<T>`, `Form`, and validation messages. The checkbox-shaped `Toggle` alias is deprecated and retained only for pre-1.0 migration. All built-in interactive controls accept an optional `FocusNode`. |
| State | Explicit `State<T>` and localized `ReactiveBuilder<T>` updates. |
| Animation | `Curve`/`Curves`, cubic Bézier easing, typed float/color/insets/radius tweens, `AnimationSpec`, `TweenAnimationBuilder<T>`, state-bound `AnimatedOpacity`, and route fades on one deterministic implicit driver. Compatible updates retarget from the displayed value; unmount cancels scheduling; `MediaQueryData.DisableAnimations` and `AnimationBehavior.Preserve` define reduced-motion behavior. |
| Navigation | Scoped `Navigator`, unique keyed `Route` entries, immutable in-memory stack snapshots, observable current route/depth/pop state, retained mounted history, `Push`/`Pop`/`Replace`/`PopToRoot`/`ClearAndPush`, route fade activation, back handling, inactive-route input isolation, and best-effort focus restoration. |
| Overlay | Scoped `OverlayHost`, toast, dialog, left/right modal drawer, popover, context menu, tooltip, and `ConfirmDialog`; top-modal input ownership, configurable barrier/back dismissal, ordered cleanup, and focus restoration. |
| Async | `AsyncAction`, `AsyncButton`, cancellation, duplicate-run protection, retry/error UI, and unmount-scoped cancellation. |
| Icons | Typed built-in Lucide-based `IconData` catalog and SVG-backed `Icon`. |
| Accessibility | One Unity `AccessibilityHierarchy` per mount; explicit `Semantics`/`ExcludeSemantics`; built-in roles, labels, values, state and actions; native focus/keyboard preservation; scoped Tab traversal; platform-aware announcements. Unity's native screen-reader bridge is enabled only on supported Android/iOS Players. |
| Localization | Zero-dependency `Locale` and typed `Localizations.Of<T>` scopes with nested isolation and reader-only locale invalidation. `TextScaler`/`TextScale` scale widget-owned typography without remounting compatible descendants. |
| Native interop | Existing detached `VisualElement` instances or mount-local factories can be embedded with `Native`. |
| Diagnostics | `MountHandle.CaptureDiagnostics()` returns an immutable, non-owning widget/state/native/layout/environment snapshot. `LumaFlowDiagnostics` exposes a weak active-mount registry, formatted tree output, and conservative key/ownership findings without exposing internal nodes. |
| Tooling | Mobile preview and Component Gallery editor windows serve as integration benchmarks. The explicit-refresh Widget Inspector renders active diagnostic snapshots and copies issue-ready trees. `MountHandle.Rebuild()` and `Restart(widget)` provide explicit PlayMode preview lifecycle boundaries; on Unity 6000.4+, two LumaFlow controls are docked immediately after the main Play/Pause/Step group. An importable Getting Started sample demonstrates declarative bootstrap and mount ownership. The optional Unity Performance Testing assembly records retained-Navigator, virtualized-list, and animation baselines. |

The runtime Dashboard is the S2 layout dogfood surface. Its shell, responsive
header, one/two/four-column metric composition, and wide/compact project rows
are built exclusively from LumaFlow widgets. Direct UI Toolkit access is
limited to `AppBootstrap`, which owns the external `UIDocument` root and
`MountHandle` lifecycle.

The completed S2 gate on Unity `6000.4.5f1` passed 264 Runtime tests and four
Performance tests in a Windows Player, plus all 29 Editor tests. The generated
Runtime, Runtime Tests, Editor Tests, and dogfood application projects also
compile with zero warnings and zero errors.

The S3 Runtime gate on the same Unity version passed 273 tests in a Windows
Player with zero failed, skipped, or inconclusive results. Coverage includes
semantic identity/state, exclusion, navigation selection, nested localization,
locale changes without state loss, and scalable-text updates without state
loss. Platform screen-reader speech remains a manual Android/iOS device gate.

The S4 Windows Player gate on the same Unity version passed 280 Runtime tests
and eight Performance scenarios (`288/288` combined). Coverage adds bounded
keyed realized-row state, mutation-safe controlled key selection, missing-key
restoration, subscription replacement, invalid-key rollback, scroll-controller
lifecycle, and 100/1,000/10,000-item native virtualization workloads. Generated
Runtime, Runtime Tests, Performance Tests, Editor Tests, and dogfood application
assemblies compile with zero warnings and zero errors.

The S5 development gate compiles Runtime, Runtime Tests, and Performance Tests
with zero warnings and errors. An isolated Unity `6000.4.5f1` batch pass executed
288 Runtime regressions with zero failures. Coverage adds route-key validation,
stack snapshots, clear cleanup, fade activation, nested modal input ownership,
non-dismissible back, drawers, and overlay reconciliation/cleanup. This is not
a final Player/IL2CPP release matrix.

The S6 isolated Unity `6000.4.5f1` development gate passed 300/300 Runtime
tests. Coverage adds curve/tween validation, exact completion, current-value
retargeting, latest callbacks, retained builder-child state, inherited reduced
motion, preserve behavior, unmount cancellation, and shared route-fade policy.
The animation performance scenarios are named allocation/scheduling baselines;
the isolated stand recorded `10.9231 ms / 0 B GC.Alloc` for 10,000 driver
start/sample operations and `29.2041 ms / 22,664 B GC.Alloc` for sixteen
builders including mount, scheduler creation, sixty frames, and unmount.
Portable thresholds and the final Player/IL2CPP matrix remain release work.

The S7 isolated Unity `6000.4.5f1` development gate passed 307/307 checks: 304
Runtime contracts, two performance smoke cases, and one Widget Inspector
integration test. Coverage adds immutable snapshot projection, weak live-mount
registration, disposal races, state/theme/media/layout/locale/text-scale data,
key and native-ownership findings, actionable duplicate-key errors, and Editor
rendering. Runtime, Runtime Tests, Editor, Editor Tests, Performance Tests, and
the consuming application projects compile sequentially with zero warnings and
errors. This remains a development gate rather than a Player/IL2CPP release
matrix.

## Consumer API examples

### Scoped application shell

```csharp
new Theme(
    MobileTheme,
    new OverlayHost(
        new Scaffold(
            appBar: new AppBar(title: new Text("Library")),
            body: new ScrollView(new LibraryContent()),
            navigationBar: new NavigationBar(selectedTab, destinations, SelectTab)),
        overlay));
```

### Controlled form

```csharp
var name = new State<string>("Alex");
var nameField = new FormField<string>(
    name,
    value => string.IsNullOrWhiteSpace(value) ? "Name is required." : null);

new Form(
    new FormState(FormValidationMode.OnChange),
    new TextField(nameField, label: "Display name"));
```

### Async action and confirmation

```csharp
var save = new AsyncAction(
    token => repository.SaveAsync(token),
    onSucceeded: () => overlay.ShowToast(new Toast(new Text("Saved")), TimeSpan.FromSeconds(2)));

new AsyncButton("Save", save, loadingText: "Saving…");

var deleteAccepted = await overlay.ShowConfirm(
    new Text("This cannot be undone."),
    new Text("Delete project?"),
    confirmText: "Delete",
    confirmVariant: ButtonVariant.Destructive,
    onConfirm: token => repository.DeleteAsync(token));
```

`ShowConfirm` resolves to `false` when the user cancels, presses Escape/back,
clicks the modal barrier, or the owning overlay is unmounted.

## Verified invariants

The Runtime and Editor test suites cover, among other things:

- direct native hierarchy for `Row`/`Column(gap)` with no gap wrappers;
- key-based reordering and compatible `StatefulWidget` state retention in
  `KeyedColumn`/`KeyedRow`;
- resolved layout behavior for `SizedBox`, `Expanded`, flex parents, and
  scroll composition;
- button semantic styles and normal/hovered/pressed/focused/disabled states;
- theme resolution through `BuildContext` and explicit style override order;
- reactive theme/media-query dependency invalidation, nested-scope isolation,
  native identity retention, failed-evaluation dependency rollback, and
  unmount cleanup;
- controlled input binding, multi-line fields, external `FocusNode`, form
  validation, Return/Enter submission, focus, and cleanup;
- local navigation and overlay cleanup/back priority;
- implicit animation retargeting, exact completion, reduced-motion snapping,
  preserve behavior, retained child state, and deterministic unmount cleanup;
- retained Navigator state/native identity, inactive focus blocking, failed-push
  rollback, and complete native-layer/reference cleanup of replaced, popped,
  and host-owned route entries;
- mount/reconciliation failure ownership, continued cleanup after multiple
  disposal failures, and aggregated error reporting;
- native `ListView` virtualization, keyed realized-row state across mutations,
  controlled key selection, scroll restoration, binding cleanup, and `Native`
  interop;
- async success, failure, duplicate invocation prevention, cancellation,
  unmount cleanup, retry UI, confirm-dialog decisions, and result-owner changes
  across compatible dialog updates;
- compatible `Native` updates retain the mounted element for the same borrowed
  element or factory delegate and reject a different source as incompatible;
- diagnostic snapshots contain values only, do not register inherited
  dependencies, do not retain internal nodes/native elements, and are rejected
  after their owning `MountHandle` is disposed;
- duplicate sibling, keyed-layout, dialog-action, list-item, and navigation
  snapshot errors include the conflicting key and indexes; capture-time findings
  flag ambiguous unkeyed stateful siblings and explicit native ownership
  boundaries.

## Current limitations

The framework is deliberately not production-stable yet.

- General local reconciliation is being rolled out under ADR-021. Compatible
  stateful/stateless/reactive boundaries, common single-child layout wrappers,
  reactive/layout builders, form and focus scopes, AppBar/Scaffold/adaptive
  navigation chrome, common overlay hosts/content, `Text`, `Button`,
  `IconButton`, structurally compatible `TextField`, controlled
  `Toggle`/`Checkbox`/`Radio`/`Switch`/`Slider`/`Dropdown` inputs, native
  `ListView<T>`, `ConfirmDialog`, compatible `Native` sources, and ordinary
  `Row`/`Column`/`Stack` children update in place; keyed arbitrary subtrees can
  move through `Widget.WithKey`. Every built-in node now has an explicit or
  inherited update contract. Narrow compatibility boundaries remain deliberate:
  structurally changed text fields, different scope-owner controllers, and a
  different `Native` element/factory remount.
- Interactive component styling uses `WidgetStateProperty<T>` and combined,
  allocation-free `WidgetStates` flags. Button/IconButton, Checkbox, Radio,
  Switch, Slider, TextField, and Dropdown have adopted the contract. TextField
  resolves hovered/focused/pressed/disabled/error combinations with legacy state
  fallback; Dropdown applies the same states to its themeable native anchor.
  Popup-menu-surface styling, broader theme composition, and animated state
  transitions remain future work.
- Composable interaction now has two explicit levels: `Button` accepts a string
  or arbitrary widget child and provides resolved foreground/typography defaults
  to nested `Text`/`Icon`; `Pressable` supplies native activation, focus,
  disabled, and button semantics without owning the child surface. `ButtonStyle`
  and legacy `ButtonStateStyle` support typed, state-aware borders.
- `Text` now standardizes soft wrapping, maximum lines, and clip/ellipsis/visible
  overflow. UI Toolkit has no public multi-line ellipsis or line-height control,
  so bounded multiline rendering uses a scalable 1.2-em cap and clipping; this
  limitation is documented rather than exposed as a false precision contract.
- `Container` and `Card` share `BoxDecoration` background/radius/border mapping.
  Unity 6.0 UI Toolkit has no public box-shadow style; shadow/elevation remains a
  renderer-backed future primitive and no no-op API is exposed.
- `LinearProgressIndicator` replaces hand-built two-segment progress rows with a
  controlled `0..1` widget. It supports reactive state, themed or explicit
  track/value colors, height/radius geometry, and progress semantics.
- `ListTile` supports arbitrary title/subtitle/leading/trailing widget slots and
  preserves compatible nested state while optional edge slots change. Its
  string convenience path retains text styles and leading content correctly.
- Checkbox, radio, switch, and slider expose tree-scoped component themes.
  Resolution is field-by-field: explicit control style, component theme, then
  semantic color/geometry defaults.
- Common fixed, fill, shrink, min/max, and edge-alignment compositions have
  named `SizedBox`, `ConstrainedBox`, and `Align` factories; they map directly
  to tested UI Toolkit constraints rather than introducing implicit measurement.
- `Image` and `CircleAvatar` cover loaded application media and avatar
  composition. The application retains asset loading, caching, and destruction
  ownership; the widget layer owns only presentation and semantics.
- Arbitrary interactive surfaces use `Pressable` state builders for combined
  hover/focus/press/disabled visuals and preserve compatible nested state.
  Custom cursor textures are supported; portable system cursor IDs are not
  claimed because UI Toolkit does not expose them publicly.
- Runtime diagnostics and the Widget Inspector expose deterministic layout
  property maps for flex, sizing, constraints, alignment, scrolling, and the
  most recent `LayoutBuilder` constraints.
- TextField, Checkbox, Radio, Switch, Slider, and Dropdown share one controlled
  ownership contract: external `State<T>`/`FormField<T>` remains authoritative,
  optional `OnChanged` observes only user-originated commits, disabled/unmounted
  adapters ignore events, and programmatic synchronization is callback-silent.
  Slider now has the same form/focus/error integration as the other active
  inputs. Single-line TextField submission and Slider interaction start/end are
  standardized, including compatible-update, disabled, unmount, keyboard, and
  pointer-capture boundaries. Input formatting and richer IME action semantics
  are not yet standardized.
- The old checkbox-shaped `Toggle` is not part of the intended stable API. Its
  constructors warn with an explicit `Checkbox`/`Switch` migration path; package
  dogfood no longer consumes it, while narrow compatibility tests cover the
  temporary bridge until pre-1.0 removal.
- `StatefulWidget` preserves mount-local state and supports `InitState`,
  `SetState`, and `Dispose`. Within `KeyedColumn`/`KeyedRow`, compatible
  stateful children retain that state as they reorder. Replacing a child with
  a different widget type remounts it.
- `State<T>` and all mounted UI are main-thread-affine. LumaFlow does not
  silently dispatch worker-thread changes to Unity's main thread.
- Async callbacks rely on the caller preserving the appropriate Unity
  synchronization context; there is no framework task scheduler.
- Accessibility semantics, typed localization, scalable text, focus order, and
  core keyboard operation are implemented. Scoped Tab / Shift+Tab traversal is
  intentionally explicit through controls connected with `FocusNode`. Unity's
  native screen-reader bridge is platform-limited to supported Android/iOS
  Players; desktop/Editor screen-reader support and gamepad spatial navigation
  are not claimed. Unity Localization table loading, plural/fallback policy,
  and async resource loading remain application or optional-adapter concerns.
- `ListView<T>` uses native virtualization with stable realized-row keys,
  controlled key selection, and `ListViewController` restoration. Incremental
  loading, asynchronous paging, animated insert/remove, and advanced
  dynamic-height correction are not standardized.
- `NavigatorHost` retains mounted history entries for its mounted lifetime.
  Inactive routes remain subscribed and consume native hierarchy memory while
  hidden. `NavigationSnapshot` restores keyed route descriptions and order in
  memory, not serialized process state or mount-local `StatefulWidget` state.
  Route fades now share S6 easing, reduced-motion, and cancellation behavior.
  Serialized process restoration, lifecycle suspension hooks, richer transition
  composition, and portable performance budgets remain future work.
- Retained-Navigator performance scenarios are reproducible and named, but the
  first environment baselines, Player/IL2CPP comparisons, CI history, and
  reviewed regression thresholds are still pending. EditMode samples alone are
  not a beta performance gate.
- Diagnostics are explicit main-thread snapshots. The Widget Inspector refreshes
  only on request, and S7 findings are conservative capture-time tooling rather
  than automatic Roslyn analyzers. Historical snapshots are not retained by the
  framework.
- PlayMode `Rebuild()` preserves compatible state only inside the currently
  loaded managed domain. Standard Unity C# recompilation may refresh script
  state and causes the application bootstrap to create a fresh mount; LumaFlow
  does not claim Dart VM-style method patching across that boundary.
- Sliver APIs are intentionally frozen. Add them only after a scenario cannot
  be represented with native `ScrollView` or `ListView<T>`.
- Public API compatibility is not guaranteed before a declared beta policy.

## Next architectural priorities

1. Execute the full Unity regression matrix for the completed dogfood priority
   batch and PlayMode preview lifecycle, then continue the remaining S8 beta
   gates.
2. Capture environment history for navigation, lists, and animation performance
   samples before setting portable budgets.
3. Run beta compatibility, package-import, Player/IL2CPP, CI matrix, public API
   review, and stable-release gates (S8).

## Documentation ownership

- [Architecture](LumaFlow%20Architecture.md) explains normative design rules.
- [Development Roadmap](LumaFlow%20Development%20Roadmap.md) defines intended
  sequencing.
- This document records implemented behavior and current gaps.
- Runtime tests in `Assets/LumaFlow.Tests` are the executable
  specification for concrete contracts.

Any feature change must update this document when it changes a supported
capability, a guarantee, or a known limitation.
