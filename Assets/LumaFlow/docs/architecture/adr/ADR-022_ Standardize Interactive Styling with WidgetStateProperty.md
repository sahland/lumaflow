# ADR-022: Standardize Interactive Styling with WidgetStateProperty

- **Status:** Accepted
- **Date:** 2026-08-20
- **Scope:** Runtime styling and interactive controls

## Context

LumaFlow originally represented button interaction overrides with separate
`Hovered`, `Pressed`, `Focused`, and `Disabled` objects. `Checkbox` and `Radio`
accepted only static colors, while `Switch` and `Slider` exposed no component
style contract. This made branded controls verbose and prevented a value from
depending on combined conditions such as selected-and-disabled or
focused-and-hovered.

Flutter resolves style properties against a set of interactive widget states.
That model is useful independently of Material Design and fits LumaFlow's
declarative configuration model.

## Decision

1. LumaFlow exposes `[Flags] WidgetStates` with `Hovered`, `Focused`, `Pressed`,
   `Dragged`, `Selected`, `ScrolledUnder`, `Disabled`, and `Error` conditions.
   A flags value avoids allocating a collection for every pointer/focus event.
2. `WidgetStateProperty<T>` is the common public resolver contract. `All(value)`
   creates a constant property and `ResolveWith(resolver)` creates a custom
   property. `WidgetStatePropertyAll<T>` has value equality for theme/config
   comparisons.
3. The plural name `WidgetStates` is intentional: the existing public
   `WidgetState` class owns `StatefulWidget` lifecycle and cannot be renamed
   without an API break.
4. Controls pass all active conditions to each resolver. A resolver returning
   null falls back to the control's legacy override, base style, theme, or
   component default in that order.
5. Existing `ButtonStyle`, `CheckboxStyle`, `RadioStyle`, `TextFieldStyle`, and
   `Dropdown<T>` constructor parameters remain source-compatible. State-aware
   properties are additive; old button and text-field state objects remain
   compatibility layers.
6. `Button`, `IconButton`, `Checkbox`, `Radio`, `Switch`, `Slider`, and
   `TextField` use the shared state contract. `Dropdown` uses it for the native
   anchor field and inherits defaults from `DropdownTheme`. Their mounted nodes
   support compatible local updates so style and external state changes do not
   inherently remount the native control.
7. Text fields report combined hovered, focused, pressed, disabled, and error
   conditions. If a state-aware value resolves to null, legacy text-field
   states use `disabled > error > focused > normal` priority.
8. Dropdown styling currently owns the anchor `PopupField` surface only. The
   separate native popup menu is not part of the stable component-style contract
   until UI Toolkit exposes a lifecycle and styling boundary LumaFlow can own
   consistently.
9. The old checkbox-shaped `Toggle` alias is not extended with a second styling
   contract. It is deprecated in favour of state-aware `Checkbox` or `Switch`,
   remains compatibility-tested during the migration window, and is scheduled
   for removal before 1.0.

## Consequences

- Branded component themes can express state-dependent colors, geometry, and
  sizing without subclassing a native UI Toolkit control.
- Custom resolvers must be pure and fast because they run on input transitions.
- Resolver-backed properties use delegate equality; applications that require
  stable structural theme equality should reuse resolver instances or use
  `WidgetStatePropertyAll<T>`.
- The legacy button priority (`disabled`, then `pressed`, `hovered`, `focused`)
  applies only when the corresponding state-aware property resolves to null.
- The legacy text-field priority (`disabled`, then `error`, `focused`) applies
  under the same fallback rule.
- Animation between resolved values is a separate concern and is not implied by
  this ADR.
- Controlled value ownership and user-change callback ordering are defined by
  ADR-024 rather than by individual style adapters.
