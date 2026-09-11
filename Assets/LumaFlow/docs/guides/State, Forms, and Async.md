# State, Forms, and Async

LumaFlow uses explicit owner-held state. Inputs do not become the authority for
their values; they bind to application-owned `State<T>` instances.

Every active input also accepts an optional `onChanged:` callback. It runs only
for a user-originated edit, after the external state has been committed. Writing
to the state from application code updates the native control silently:

```csharp
var volume = new State<float>(0.5f);

new Slider(
    volume,
    min: 0f,
    max: 1f,
    onChanged: value => Analytics.RecordVolumeEdit(value));

volume.Value = 0.75f; // updates the slider; does not call onChanged
```

`onChanged: null` does not disable a control. Use `enabled: false` explicitly.
TextField, Checkbox, Radio, Switch, Slider, and Dropdown follow this same rule.

Text completion and Slider interaction boundaries are observational callbacks;
they do not replace `onChanged` as the value-commit path:

```csharp
new TextField(
    query,
    onChanged: value => UpdateSuggestions(value),
    onSubmitted: value => OpenSearch(value));

new Slider(
    volume,
    0f,
    1f,
    onChanged: value => PreviewVolume(value),
    onChangeStart: previousValue => BeginVolumeEdit(previousValue),
    onChangeEnd: finalValue => SaveVolume(finalValue));
```

`TextField.onSubmitted` runs for Return and keypad Enter only on enabled,
single-line fields, before an enclosing Form handles the same submission.
Slider calls `onChangeStart` once with the pre-change value and `onChangeEnd`
once with the final committed value. This covers pointer/tap interactions and
native non-pointer changes such as keyboard adjustment.

## Reactive state

```csharp
private readonly State<bool> _isPlaying = new(false);

new ReactiveBuilder<bool>(
    _isPlaying,
    isPlaying => new Button(
        isPlaying ? "Pause" : "Play",
        () => _isPlaying.Value = !isPlaying));
```

Keep a State for as long as its value is meaningful. A `ReactiveBuilder<T>`
borrows the State and unsubscribes automatically when its mounted subtree is
removed.

`State<T>` is not thread-safe. When it drives mounted UI, mutate it on Unity's
main thread.

## Controlled fields and validation

`FormField<T>` combines an external value with optional validation; `FormState`
owns only validation lifecycle for the fields mounted under its `Form`.

```csharp
private readonly State<string> _email = new("");
private readonly FormState _form = new(FormValidationMode.OnChange);
private readonly FormField<string> _emailField;

public AccountView()
{
    _emailField = new FormField<string>(
        _email,
        value => value.Contains("@") ? null : "Enter a valid email address.");
}

public override Widget Build(BuildContext context) => new Form(
    _form,
    new Column(
        gap: 12f,
        children: new Widget[]
        {
            new TextField(_emailField, label: "Email"),
            new ReactiveBuilder<bool>(
                _form.IsValid,
                isValid => new Button("Continue", Submit, enabled: isValid))
        }),
    onSubmit: Submit);

private void Submit()
{
    // Submit the external _email.Value.
}
```

When `onSubmit` is supplied, Return and keypad Enter from a descendant input
call `FormState.Submit`. LumaFlow validates first, focuses the first invalid
field when needed, and invokes the callback only for a valid form.

Use `new TextField(notes, multiline: true)` for text that accepts line breaks.
Return remains a line break in that field and does not submit its ancestor form.

For programmatic focus, keep a `FocusNode` next to the field state rather than
searching the native hierarchy:

```csharp
private readonly FocusNode _emailFocus = new();
private readonly FocusNode _saveFocus = new();

new TextField(_emailField, label: "Email", focusNode: _emailFocus);

new Button("Save", Save, focusNode: _saveFocus);

// Later, while the field is mounted:
_emailFocus.RequestFocus();
```

`FocusNode.IsFocused` is reactive and becomes `false` when its control unmounts.

Wrap a related set of explicitly controlled fields and actions in
`FocusTraversalGroup` to keep Tab / Shift+Tab navigation local to that set:

```csharp
new FocusTraversalGroup(
    new Column(new Widget[]
    {
        new TextField(_emailField, focusNode: _emailFocus),
        new Button("Save", Save, focusNode: _saveFocus)
    }));
```

`TextField`, `Button`, `IconButton`, `Checkbox`, `Switch`, `Slider`,
`Dropdown<T>`, `Radio<T>`, and `AsyncButton` accept the same `focusNode:` argument. Controls
without one retain normal UI Toolkit behavior and are not included in this
traversal contract.

For `Checkbox`, `Switch`, `Slider`, `Dropdown<T>`, and `Radio<T>`, wrap the typed
control in `FormFieldMessage<T>` when its validation message must be rendered
below the control.

`Toggle` is a deprecated checkbox-shaped compatibility alias. Replace it with
`Checkbox` when the value is an independent option, or `Switch` when changing
the value immediately enables or disables a setting. The alias will be removed
before 1.0.

## Async actions

`AsyncAction` owns one operation at a time. It exposes status and error state,
prevents duplicate runs, and accepts a cancellation token.

```csharp
private readonly AsyncAction _save;

public EditorView(OverlayController overlay)
{
    _save = new AsyncAction(
        token => repository.SaveAsync(token),
        onSucceeded: () => overlay.ShowToast(
            new Toast(new Text("Saved")),
            TimeSpan.FromSeconds(2)),
        onFailed: error => logger.LogError(error));
}

public override Widget Build(BuildContext context) =>
    new AsyncButton("Save", _save, loadingText: "Saving…");
```

`AsyncButton` disables itself while running, exposes a retry label and inline
error after failure, and cancels its action when its mount scope is removed.
Set `cancelOnUnmount: false` only when the action has a broader, explicitly
owned lifetime.

LumaFlow does not install a global worker-to-main-thread dispatcher. Preserve
Unity's synchronization context, or marshal back to the main thread before
touching State that is bound to UI.

## Confirmation flow

Use `OverlayController.ShowConfirm` for an explicit async decision:

```csharp
var accepted = await overlay.ShowConfirm(
    new Text("Deleting a project cannot be undone."),
    new Text("Delete project?"),
    confirmText: "Delete",
    confirmVariant: ButtonVariant.Destructive,
    onConfirm: token => repository.DeleteAsync(token));

if (accepted)
{
    // The async confirmation completed successfully.
}
```

The returned task resolves `false` for Cancel, Escape/back, barrier click, or
unmount of the owning overlay. A failed confirm stays open, renders its inline
error, and can be retried.

## Ownership checklist

- Application code owns `State<T>`, `FormState`, `FormField<T>`, `Navigator`,
  `OverlayController`, and long-lived `AsyncAction` instances.
- A mounted WidgetNode owns native controls, event subscriptions, and its local
  bindings.
- Do not retain `BuildContext` across `await` boundaries.
- Never mutate a `VisualElement` from a worker thread.
