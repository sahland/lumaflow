# Accessibility, scalable text, and localization

Accessibility and localization use three independent tree-scoped contracts:
semantic annotations, focus and keyboard behavior, and locale-specific
resources. None requires the optional Unity Localization package.

## Semantics

LumaFlow creates one Unity `AccessibilityHierarchy` per `MountHandle`. Built-in
text and interactive controls project labels, values, roles, enabled/selected
state, and supported actions into that hierarchy. Compatible widget and
controlled-state updates retain the corresponding `AccessibilityNode`.

Use `Semantics` for application-specific meaning and `ExcludeSemantics` for
decorative content:

```csharp
new Semantics(
    new ProjectCard(project),
    new SemanticsProperties(
        label: project.Name,
        hint: "Opens project details",
        role: SemanticsRole.Button,
        enabled: project.CanOpen,
        onTap: () => Open(project)),
    excludeDescendantSemantics: true);
```

`excludeDescendantSemantics` means that the parent annotation replaces the
child annotations. Without it, semantic descendants remain children of the
explicit node. `ExcludeSemantics` removes every descendant annotation and is
appropriate only for content that is genuinely decorative.

Built-in mappings include:

| LumaFlow widget | Semantic role/state |
| --- | --- |
| `Text` | static text |
| `Button`, `IconButton` | button, enabled/disabled |
| `Checkbox`, `Switch` | toggle, checked, enabled |
| `Radio<T>` | toggle, selected, enabled |
| `TextField` | text field, value, error/helper hint, direct interaction |
| `Slider` | slider, invariant value, increase/decrease actions |
| `Dropdown<T>` | dropdown, selected label, enabled |
| `NavigationBar`, `NavigationRail` | tab bar with selected tab children |
| `ScrollView`, `ListView<T>` | scroll view |

`SemanticsService.Announce` sends a native announcement only while a supported
platform screen reader is enabled and returns `false` otherwise.

### Platform boundary

The semantics hierarchy and its tests are platform-independent LumaFlow
behavior. Unity's native screen-reader bridge is currently activated by
LumaFlow only on Android and iOS, matching Unity's supported mobile
accessibility platforms. Editor and desktop Player builds retain normal native
UI Toolkit focus and keyboard behavior but are not advertised as having a
LumaFlow screen-reader backend.

## Focus and keyboard

Native UI Toolkit controls retain their native keyboard activation. Custom
`Switch` handles Space, Return, and keypad Enter. Single-line `TextField`
submits before its enclosing `Form`; multiline and disabled fields do not.
Escape/back remains scoped through `BackNavigation` and overlays.

Attach `FocusNode` to controls that require programmatic focus or explicit
traversal. `FocusTraversalGroup` visits its attached nodes in mount order for
Tab and reverse mount order for Shift+Tab, wraps at the ends, and skips disabled
or otherwise unavailable controls.

## Strongly typed localization

Application resource objects are ordinary C# types. This keeps Core independent
of any loading package and makes a Unity Localization adapter just another
resource object owned by the application or an optional integration assembly.

```csharp
public sealed class AppStrings {
    public AppStrings(string save) => Save = save;
    public string Save { get; }
}

public sealed class SaveButton : StatelessWidget {
    public override Widget Build(BuildContext context) =>
        new Button(Localizations.Of<AppStrings>(context).Save, Save);
}

var ui = new Localizations(
    new Locale("ru", "RU"),
    new SaveButton(),
    new AppStrings("Сохранить"));
```

`Localizations.Of<T>` reads the nearest exact resource type and registers an
inherited dependency. `MaybeOf<T>` returns `null` when absent, and `LocaleOf`
returns the nearest locale. Nested scopes isolate resources. Replacing a locale
invalidates only readers and preserves compatible mounted state below the
scope.

Loading, fallback selection, plural rules, and Unity Localization table access
belong to the application or an optional adapter; synchronous Core widgets do
not own asynchronous asset loading.

## Scalable text

`TextScale` applies a `TextScaler` to widget-owned font sizes without remounting
compatible descendants:

```csharp
new TextScale(
    new TextScaler(1.25f),
    new Theme(theme, new Dashboard()));
```

Configured `Text`, button, input, validation, app-bar, and themed field-label
font sizes are scaled. Layout remains responsible for wrapping and overflow;
do not combine large text with fixed heights that cannot contain it.

## Validation

The test suite verifies semantic hierarchy/state, exclusion boundaries,
controlled updates without node replacement, localization scope isolation,
locale changes without nested-state loss, scalable-text updates without state
loss, and navigation selection semantics. Screen reader speech, platform rotor
behavior, touch exploration, large-text visual overflow, and high-contrast
appearance still require representative device/manual review.
