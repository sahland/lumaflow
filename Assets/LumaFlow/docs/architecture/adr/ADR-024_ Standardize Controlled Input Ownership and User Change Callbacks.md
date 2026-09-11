# ADR-024: Standardize Controlled Input Ownership and User Change Callbacks

- **Status:** Accepted
- **Date:** 2026-08-20
- **Scope:** Runtime controlled inputs, forms, and UI Toolkit adapters

## Context

LumaFlow inputs were already controlled by externally owned `State<T>`, but
applications had to subscribe to that state even when they only needed to react
to a user edit. The active controls also differed in form integration: TextField,
Checkbox, Radio, Switch, and Dropdown accepted `FormField<T>`, while Slider did
not. Several native callback adapters accepted synthetic changes after disable or
unmount, and there was no shared ordering contract for state observers and a
future component callback.

Flutter distinguishes a user-originated `onChanged` notification from a
programmatic controller update. That distinction is useful for LumaFlow, while
LumaFlow's explicit `State<T>` ownership remains preferable to adding a hidden
uncontrolled value copy inside each adapter.

## Decision

1. Every active value input remains controlled. TextField, Checkbox, Radio,
   Switch, Slider, and Dropdown require an externally owned `State<T>` directly
   or through `FormField<T>`.
2. These controls expose an optional `OnChanged` callback through the additive
   `onChanged:` constructor argument. It reports only a user-originated native
   change. Programmatic writes to `State<T>` update the native control silently
   and do not invoke `OnChanged`.
3. `Enabled` remains explicit and independent. A null callback does not disable
   a LumaFlow control; disabled or unmounted nodes ignore native/synthetic user
   changes.
4. A user change first commits the externally owned `State<T>`, including its
   synchronous observers, and then invokes the callback belonging to the widget
   configuration that received the event. The callback therefore observes the
   committed value.
5. If a state observer and `OnChanged` both fail, LumaFlow attempts both stages
   and throws an `AggregateException` containing both failures. A single failure
   preserves its original stack through `ExceptionDispatchInfo`.
6. Compatible reconciliation does not cache callbacks in native event handlers.
   The next native event reads the latest widget configuration. If state
   observers reconcile or unmount during an event, that already-started event
   still completes the callback captured from its originating configuration.
7. Slider gains the same `FormField<float>` overload, focus attachment, reactive
   error binding, and `WidgetStates.Error` resolution as the other active inputs.
8. Native-to-state events use the shared `ControlledInputChange` adapter.
   State-to-native synchronization continues to use UI Toolkit's silent value
   assignment APIs to prevent feedback loops.
9. The deprecated checkbox-shaped `Toggle` alias is not extended. New code uses
   Checkbox or Switch and receives this contract there.
10. A single-line TextField may expose `OnSubmitted`. Return and keypad Enter
    invoke it with the current controlled value before an enclosing Form handles
    the same submit event. Disabled, multi-line, and unmounted fields ignore the
    callback; multi-line fields retain Enter for line breaks.
11. Slider may expose `OnChangeStart` and `OnChangeEnd` as observational
    interaction boundaries. Start receives the value before a pointer, tap, or
    non-pointer native change; End receives the final committed value. Only
    `OnChanged` performs value ownership work. Pointer capture loss completes an
    active interaction, while unmount cleanup releases it without emitting a
    post-unmount callback.
12. Specialized callbacks, like `OnChanged`, are read from the current compatible
    widget configuration when their native event occurs.

## Consequences

- Application code can react to user edits declaratively without confusing them
  with programmatic state changes.
- Forms and focus ownership are consistent across the active input family.
- Callback ordering, reentrant unmount, disabled behavior, and failure handling
  are testable framework contracts rather than per-control accidents.
- `OnChanged` is observational: `State<T>` remains authoritative. Applications
  should not write the same value back merely to make the control update.
- Input formatting, IME action variants, and an optional uncontrolled
  convenience layer remain separate future API decisions.
