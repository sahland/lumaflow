# Migration guide

LumaFlow `0.2.0` is an unreleased package with a frozen Runtime
API v1 contract. Version changes do not permit silently breaking that contract.
Historical migrations below predate the freeze unless stated otherwise.

## Moving to 0.2.0

Package metadata now declares Unity 6000.2.2f1 as the minimum instead of 6000.0.
Use a qualified Editor before updating; no support for 6000.0/6000.1 is claimed.
Existing namespaces, assembly identity and frozen Runtime signatures are unchanged.
This local version is not an uploaded archive, Git tag or registry publication.

## Package identity

Before the first public stable release, the UPM package identity changed from
`com.lumaflow.ui` to `com.sahland.lumaflow`. Update package manifest and local
path references to use the new identity. C# namespaces and assembly names are
unchanged.

## Toggle to Checkbox or Switch

The old checkbox-shaped `Toggle` API has been removed.

```csharp
// Checkbox semantics
new Checkbox(accepted, label: "Accept terms");

// Track-and-thumb semantics
new Switch(enabled);
```

Do not select a widget from appearance alone: these controls expose different
semantics, keyboard behavior, and theme types.

## Positional children to stable keys

Unkeyed children are reconciled by position. Before a collection can reorder,
insert, or remove stateful entries, assign stable identity:

```csharp
new Column(items.Select(item =>
    new ProjectRow(item).WithKey(new WidgetKey(item.Id))).ToArray());
```

For controlled dynamic collections, `KeyedColumn`, `KeyedRow`, and
`ListView<T>.itemKey` remain convenient adapters. The Widget Inspector reports
`LF1001` for ambiguous stateful sibling identity.

## Callback ownership

Text fields, checkbox, radio, switch, slider, and dropdown values are controlled
by `State<T>` or `FormField<T>`. Programmatic state updates synchronize native UI
without invoking `onChanged`; callbacks report user-originated commits only.

Move side effects that must run for every state change into a `State<T>`
subscription. Keep `onChanged` for user intent.

## Mount ownership

Keep the `MountHandle` at the native integration boundary and dispose it in the
matching lifecycle callback:

```csharp
private void OnEnable() =>
    _mount = LumaFlow.Mount(application, document.rootVisualElement);

private void OnDisable() {
    _mount?.Dispose();
    _mount = null;
}
```

Application widgets should not call `Mount`, retain `VisualElement`, or reparent
borrowed `Native` elements.

## Navigator routes

Use explicitly keyed `Route` instances when history must be inspected or
restored. `NavigationSnapshot` is an in-memory route description; it is not a
process-persistence format and does not serialize mounted `WidgetState`.

## Implicit animations

Use `TweenAnimationBuilder<T>` for local implicit transitions and
`AnimatedOpacity` for state-bound opacity. Compatible updates retarget from the
currently displayed value. Supply reduced motion through
`MediaQueryData.DisableAnimations`; choose `AnimationBehavior.Preserve` only
when motion conveys required meaning.
