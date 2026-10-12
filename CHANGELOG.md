# Changelog

All notable changes to LumaFlow are documented in this file.

## [Unreleased]

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
