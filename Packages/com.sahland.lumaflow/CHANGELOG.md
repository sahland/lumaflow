# Changelog

All notable changes to LumaFlow are documented in this file.

## [0.2.0-preview.1] - Unreleased

- Ignore bubbled child/label value events when binding a native value control
  or editing a TextField, so caption updates cannot overwrite its value.

- Added native element IDs and NativeView typed queries/lifecycle-safe bindings.
  Identified composite roots follow reactive replacements without changing
  reconciliation keys or application state ownership.

- Centralized exact Unity 6000.2.2f1/6000.4.5f1 CI pins; required isolated
  functional suites and both archive channels per Editor, strict NUnit evidence,
  unique artifacts and an aggregate validation gate.

- Aligned package metadata with preview guarantees and the frozen Runtime API v1
  contract. Minimum declared Editor is now 6000.2.2f1; 6000.0 is not qualified.
  This working-tree version has not been published as a release or registry tag.

- Reduced DataTable native cell wrappers and skipped unchanged immutable rows
  during updates, including paginated tables. Preserved input identity,
  controlled selection, styling invalidation and the frozen Runtime API.

- Frozen Runtime API contract v1 with compatibility tests, baseline-write
  protection and an immutable-contract CI gate. Restored pre-table ThemeData
  constructor/CopyWith binary signatures; added WithDataTableTheme for styling.

- Added Table layout with shared fixed/flex column widths, keyed rows, cell
  alignment and borders; DataTable with controlled sort/selection callbacks;
  PaginatedDataTable with bounded source reads, controlled paging and localized
  captions; DataTableTheme, documentation and the Tables sample.

- Fixed Unity 6.2 accessibility compilation: use the supported activation event
  and fallback roles when the newer native accessibility API is unavailable.

- Added one-button scene setup in LumaFlow UXML: reuse/create UIDocument,
  AppBootstrap and Camera, preserve existing PanelSettings, wire document
  references, and persist edit-mode preview connections with Undo support.
- Empty projects receive an editable starter in Assets/LumaFlowApp; setup resumes
  after compilation. Editor snapshots are restored after reload and removed
  before runtime mounting, including Player startup.

- Fixed Checkbox styling targeting the full native input row instead of its
  square indicator; check color now tints the checkmark image correctly.
- Fixed clipped UXML dimension inputs and Performance settings in narrow Editor
  windows; captions stack above inputs, and Performance content scrolls vertically.

### Added

- Self-hosted LumaFlow UXML and GUI Performance Editor windows. UXML opens once
  per interactive project session by default, remembers its source, discovers
  previews and provides a built-in welcome factory. Startup can be disabled
  with a persistent project-scoped checkbox.
- Undoable one-click UXML binding that configures a missing UIDocument, default
  runtime theme and Panel Settings without overwriting existing panel settings.
- Automatic Play Mode performance captures with bounded native recorder buffers,
  explicit unavailable counters and cancellation/disposal on lifecycle changes.

- Balanced mount/update/unmount hooks for borrowed and factory-backed `Native`
  elements.
- `PointerRegion` with pooled-event-safe pointer details, pointer capture, drag
  thresholds, deltas, and lifecycle-safe cancellation.
- `ButtonContent` for consistent leading-label-trailing button composition.
- Typed `DiscreteSlider<T>` with declarative marker, track, and thumb content,
  continuous dragging, snapping, focus, keyboard, and semantics support.
- `AnimatedStyle<T>` for direct native style transitions without per-frame
  child reconciliation, plus common style appliers and `Vector2Tween`.
- Cached two-color linear gradients for `BoxDecoration`.
- Explicit rounded-content clipping through `Container` and `ClipBehavior`.
- Root `MediaQuery` propagation after UI Toolkit panel resize.
- Percentage sizing and alignment through `FractionallySizedBox`.

## [0.1.1] - 2026-09-07

### Added

- Declarative widget mounting with stateful, stateless and reactive widgets.
- Keyed reconciliation for ordinary layouts and dynamic collections.
- Typed flex, constraint, alignment, stack, scrolling and responsive layout
  widgets.
- Controlled text field, checkbox, radio, switch, slider and dropdown widgets.
- Buttons with text or arbitrary widget content, icon buttons and `Pressable`.
- Component themes and state-dependent styling through
  `WidgetStateProperty<T>`.
- Forms, focus nodes, keyboard traversal and asynchronous actions.
- Retained navigation, in-memory route snapshots and route transitions.
- Dialog, confirmation dialog, drawer, popover, tooltip and toast overlays.
- Virtualized lists with stable item identity, controlled selection and scroll
  restoration.
- Implicit animations, typed tweens, curves and reduced-motion handling.
- Accessibility semantics, scalable text and typed localization scopes.
- Text wrapping, line limits and overflow handling.
- Cards, decoration, borders, progress indicators, images and avatars.
- Typed built-in icon catalog.
- LumaFlow brand icon for widget layouts and framework logo for package
  documentation.
- Mounted-tree diagnostics and the Editor Widget Inspector.
- Play Mode Reassemble and Compile & Restart toolbar actions.
- Getting Started sample and package documentation.

### Changed

- UPM package identity changed from `com.lumaflow.ui` to
  `com.sahland.lumaflow` before public stable release.
- Selection controls update only the previously selected and newly selected
  items.
- `ThemeData.CopyWith` supports local theme variants without reconstructing
  unchanged values.
- `ListTile`, tabs and segmented controls accept arbitrary widget slots while
  retaining their string convenience constructors.
- Reconciliation commits successful replacements before reporting cleanup
  failures and aggregates multiple lifecycle failures.
- Component style fields override theme defaults independently.

### Fixed

- Corrected loose and tight `Flexible` allocation and validation outside flex
  parents.
- Corrected slider disabled-state styling, active-track rendering and custom
  track/thumb geometry on Unity 6.
- Preserved focus and resolved icon colors across button interaction states.
- Preserved inherited-scope subscriptions after failed updates.
- Ensured navigator and overlay teardown releases all owned native layers even
  when cleanup reports an exception.
- Prevented invalid gaps and duplicate controlled selection values.
- Corrected TextField submission ordering and controlled input failure
  aggregation.
- Corrected TabView content replacement and TooltipAnchor native fallback.
- Declared the UI Toolkit module dependency and exposed the Getting Started
  sample through Package Manager.

### Removed

- Removed the pre-release `Toggle` alias. Use `Checkbox` for checkbox semantics
  or `Switch` for track-and-thumb presentation.
