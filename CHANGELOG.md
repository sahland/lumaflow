# Changelog

All notable changes to LumaFlow are documented in this file.

## [Unreleased]

### Added

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
