# ADR-005: Use a First-Class Theme and Design Token System

- **Status:** Accepted
- **Decision date:** 2026-08-11
- **Scope:** Theming, styling, design tokens, component defaults
- **Affects:** Runtime, Widgets, Styling, BuildContext, Components, Documentation
- **Related documents:** `ARCHITECTURE.md`, `API_DESIGN.md`, `ADR-002-code-first.md`, `ADR-003-reactive-state.md`

---

## 1. Context

LumaFlow is intended to make UI development more consistent and less dependent on repetitive literals.

Without a first-class theme system, component code tends to accumulate values such as:

```text
padding = 12
radius = 8
fontSize = 14
color = ...
borderWidth = 1
```

across many widgets.

This creates:

- inconsistent UI;
- difficult redesigns;
- duplicated values;
- weak design-system semantics;
- poor maintainability;
- component-specific styling hacks.

LumaFlow therefore requires a centralized theme architecture.

---

## 2. Decision

LumaFlow will provide a first-class theme system centered around:

```text
ThemeData
Design Tokens
Component Themes
Scoped Theme Overrides
```

Theme data will flow through `BuildContext`.

The architecture is:

```text
ThemeData
    ↓
BuildContext
    ↓
Widget
    ↓
Component Style Resolver
    ↓
UI Toolkit styles
```

---

## 3. ThemeData

`ThemeData` represents the current design system.

Conceptually:

```csharp
public sealed class ThemeData
{
    public ColorScheme Colors { get; init; }
    public TypographyScheme Typography { get; init; }
    public SpacingScheme Spacing { get; init; }
    public RadiusScheme Radius { get; init; }
    public BorderScheme Borders { get; init; }
    public ShadowScheme Shadows { get; init; }
    public MotionScheme Motion { get; init; }

    public ButtonTheme Button { get; init; }
    public TextFieldTheme TextField { get; init; }
    public CardTheme Card { get; init; }
}
```

Exact structure may evolve.

---

## 4. Design Tokens

Theme values should be semantic.

Prefer:

```text
Primary
Surface
SurfaceContainer
TextPrimary
TextSecondary
Border
Success
Warning
Error
```

over arbitrary names like:

```text
Blue500
Gray17
PanelDark2
```

when the value has design-system meaning.

Raw palette values may still exist internally or in user themes.

---

## 5. Color Scheme

Conceptual API:

```csharp
public sealed class ColorScheme
{
    public Color Primary { get; init; }
    public Color OnPrimary { get; init; }

    public Color Surface { get; init; }
    public Color SurfaceContainer { get; init; }

    public Color TextPrimary { get; init; }
    public Color TextSecondary { get; init; }

    public Color Border { get; init; }

    public Color Success { get; init; }
    public Color Warning { get; init; }
    public Color Error { get; init; }
}
```

Components should prefer semantic colors over hardcoded values.

---

## 6. Typography

Typography must be centralized.

Conceptual access:

```csharp
context.Theme.Typography.TitleLarge
context.Theme.Typography.TitleMedium
context.Theme.Typography.BodyMedium
context.Theme.Typography.BodySmall
context.Theme.Typography.Caption
```

Each entry may describe:

```text
font
font size
font weight
line height
letter spacing
color defaults where appropriate
```

---

## 7. Spacing

Spacing should use shared tokens.

Possible API:

```csharp
context.Theme.Spacing.XS
context.Theme.Spacing.S
context.Theme.Spacing.M
context.Theme.Spacing.L
context.Theme.Spacing.XL
```

The exact naming convention must remain consistent.

Components should not invent their own arbitrary spacing values unless behavior genuinely requires it.

---

## 8. Radius

Central radius tokens:

```csharp
context.Theme.Radius.Small
context.Theme.Radius.Medium
context.Theme.Radius.Large
context.Theme.Radius.XLarge
```

This keeps the visual language coherent.

---

## 9. Borders

Border tokens may include:

```text
default width
subtle width
focus width
default color
strong color
```

Do not scatter border literals through component implementations.

---

## 10. Shadows

If UI Toolkit supports the required effect directly, standard shadows should be theme-driven.

Potential:

```csharp
context.Theme.Shadows.Small
context.Theme.Shadows.Medium
context.Theme.Shadows.Large
```

Advanced render-pipeline-specific shadows must remain outside Core if necessary.

---

## 11. Motion

Theme may eventually expose motion tokens:

```text
DurationFast
DurationNormal
DurationSlow
CurveStandard
CurveEmphasized
```

These should be reusable across components.

Motion remains optional during MVP.

---

## 12. Component Themes

Global tokens are not sufficient for every component.

Components may have dedicated theme objects.

Examples:

```text
ButtonTheme
TextFieldTheme
CardTheme
ToggleTheme
SliderTheme
```

These define semantic defaults for that component.

---

## 13. ButtonTheme

Conceptual example:

```csharp
public sealed class ButtonTheme
{
    public float Height { get; init; }
    public EdgeInsets Padding { get; init; }
    public float Radius { get; init; }

    public ButtonVariantStyle Primary { get; init; }
    public ButtonVariantStyle Secondary { get; init; }
    public ButtonVariantStyle Danger { get; init; }
}
```

Application code should not need to specify these repeatedly.

---

## 14. Theme Resolution Order

LumaFlow will use the following precedence:

```text
1. Explicit widget value
2. Component theme value
3. Global semantic token
4. Framework default
```

Example:

```csharp
Button(
    "Save",
    radius: 20,
    onPressed: Save
)
```

uses `20` even if `ButtonTheme` defines another radius.

---

## 15. Explicit Overrides

Explicit overrides are allowed.

The theme provides defaults, not restrictions.

Example:

```csharp
Card(
    decoration: BoxDecoration(
        color: customColor
    )
)
```

should work even if the theme normally defines a different surface color.

---

## 16. No Literal Explosion

Framework component implementation should avoid literals such as:

```csharp
padding = 12;
radius = 8;
height = 36;
```

unless the value is a documented internal invariant.

Prefer:

```text
theme.Button.Padding
theme.Button.Radius
theme.Button.Height
```

---

## 17. Framework Defaults

LumaFlow must still work if users do not provide a custom theme.

Therefore the framework must ship with a default theme.

Conceptually:

```csharp
ThemeData.Default
```

or equivalent.

This theme should be visually coherent and neutral.

---

## 18. Theme Is Not Global Mutable State

Forbidden:

```csharp
Theme.Current = darkTheme;
```

as the primary architecture.

Theme flows through the mounted UI tree.

This supports:

```text
multiple windows
multiple panels
nested themes
tests
independent UI trees
```

---

## 19. BuildContext Access

Desired usage:

```csharp
context.Theme
```

not:

```csharp
ThemeManager.Instance.CurrentTheme
```

This is a strict architectural preference.

---

## 20. Scoped Theme Overrides

A subtree should be able to override theme data.

Conceptual:

```csharp
Theme(
    data: darkTheme,
    child: dialog
)
```

or:

```csharp
ThemeOverride(
    colors: customColors,
    child: content
)
```

The exact public API may evolve.

---

## 21. Nested Themes

Nearest theme wins.

Conceptually:

```text
Root Theme
    ↓
App
    ↓
Dark Theme Override
    ↓
Dialog
```

The Dialog sees the dark override.

Sibling trees remain unaffected.

---

## 22. Partial Overrides

Users should not necessarily need to recreate the entire ThemeData for one change.

Potential pattern:

```csharp
theme.CopyWith(
    colors: theme.Colors.CopyWith(
        primary: customPrimary
    )
)
```

or a dedicated override type.

Exact API requires later design.

---

## 23. Immutability

Theme data should preferably be immutable.

Benefits:

- predictable behavior;
- safe sharing;
- easy scoped replacement;
- cacheability;
- simpler debugging.

Prefer init-only or readonly data structures.

---

## 24. Theme Mutation

Avoid mutating ThemeData in place.

Bad:

```csharp
context.Theme.Colors.Primary = newColor;
```

Preferred:

```text
replace theme or scoped override
```

This keeps contextual semantics predictable.

---

## 25. Reactive Theme Switching

Theme replacement may eventually be reactive.

Example:

```text
Light Theme
    ↓
theme state changes
    ↓
Dark Theme
```

The implementation must update affected components predictably.

Exact propagation strategy may depend on the broader reactive architecture.

---

## 26. Theme Change Scope

A theme change may affect many descendants.

This is a legitimate broad invalidation case.

LumaFlow may use a scoped theme dependency mechanism rather than ordinary single-property bindings for every token.

This requires deliberate implementation.

---

## 27. Theme Dependency Tracking

Initial implementation may allow themed components to subscribe to the nearest theme provider.

When the theme changes:

```text
Theme provider
    ↓
dependent descendant nodes update style
```

Do not rebuild the entire application if style updates can occur in place.

---

## 28. Component Style Resolution

Components should resolve a final style configuration.

Conceptually:

```text
Widget explicit values
        +
ComponentTheme
        +
Global tokens
        ↓
ResolvedButtonStyle
        ↓
UI Toolkit style mapping
```

This isolates styling logic from native application.

---

## 29. Resolved Styles

Internal resolved style objects may be useful.

Example:

```text
ResolvedButtonStyle
ResolvedTextFieldStyle
ResolvedCardStyle
```

These are implementation details.

Do not expose them publicly unless real user value exists.

---

## 30. State Variants

Component theme must account for interaction states when appropriate.

Examples:

```text
normal
hover
pressed
focused
disabled
selected
```

Prefer semantic state styling over hardcoded callback logic.

---

## 31. UI Toolkit Pseudo States

Where UI Toolkit's style/pseudo-state mechanisms work well, LumaFlow should use them.

Do not manually simulate every visual state if USS/native styling already solves it cleanly.

The theme system may generate or apply suitable styles around those mechanisms.

---

## 32. Typed Styling and USS

Theme and typed style APIs coexist with USS.

A user may still add a class:

```csharp
classes:
[
    "audio-card"
]
```

The exact precedence between USS and inline LumaFlow styles must be documented.

---

## 33. Theme vs USS

LumaFlow should not generate a giant dynamic USS stylesheet as the only theme implementation unless there is a proven technical reason.

Direct typed style mapping may remain the primary mechanism.

USS may be used where it provides better support for:

```text
hover
focus
transitions
selectors
shared state styling
```

---

## 34. No Mandatory Generated USS

Changing ThemeData must not require users to manually regenerate theme files.

If generation is ever introduced, it must be handled internally or remain optional.

---

## 35. Theme Serialization

ThemeData does not need to be a `ScriptableObject` at the core architectural level.

A future optional asset representation may exist.

Example:

```text
LumaThemeAsset : ScriptableObject
```

could provide Inspector authoring.

But Core theme semantics should remain ordinary managed data.

---

## 36. ScriptableObject Theme Assets

Potential future workflow:

```text
Theme Asset
    ↓
convert/load into ThemeData
    ↓
Theme provider
```

This can be useful for designers.

It must not make ScriptableObjects mandatory for code-first usage.

---

## 37. Design Tokens as Assets

Likewise, design tokens may eventually be imported/exported from assets or JSON.

This is optional tooling.

Runtime semantics remain typed.

---

## 38. Dark and Light Themes

LumaFlow should support distinct theme instances.

Example:

```csharp
AppThemes.Light
AppThemes.Dark
```

Do not bake dark/light assumptions into every component.

They are just ThemeData variants.

---

## 39. Brand Themes

Applications should be able to provide their own complete theme.

Example:

```csharp
LumaFlow.Mount(
    App(),
    root,
    theme: MyGameTheme
);
```

Conceptual syntax only.

---

## 40. Per-Component Overrides

Applications may want to customize all Buttons globally without replacing other tokens.

That should be possible through:

```text
ThemeData.Button
```

rather than creating a subclass of Button.

---

## 41. Semantic Variants

Theme should define appearance for variants such as:

```text
Primary
Secondary
Outline
Ghost
Danger
```

The widget selects semantics.

The theme decides visuals.

---

## 42. Avoid Color-Based API Semantics

Bad:

```csharp
Button(
    color: Color.red
)
```

as the primary way to express destructive action.

Better:

```csharp
Button(
    variant: ButtonVariant.Danger
)
```

The theme then controls actual color.

Explicit color override remains possible.

---

## 43. Accessibility Considerations

Theme defaults should leave room for:

```text
contrast
focus visibility
disabled readability
text scaling
```

Do not optimize purely for aesthetics.

---

## 44. Density

Future themes may support density:

```text
Compact
Comfortable
Touch
```

This may affect:

```text
control height
padding
spacing
hit area
```

Do not hardcode desktop-only dimensions into every component.

---

## 45. Platform Variation

Theme may eventually adapt to platform or environment.

Example:

```text
Editor
Desktop Runtime
Mobile Runtime
```

This should happen through theme/context rather than platform checks scattered inside components.

---

## 46. Editor Theme

LumaFlow.Editor may provide an Editor-friendly default theme.

However, editor styling should still use the same theme architecture.

Avoid building a separate unrelated theme system.

---

## 47. Runtime Theme

Runtime applications may use fully custom themes independent from Unity Editor appearance.

---

## 48. Theme Extension

Users should be able to extend the theme with application-specific values.

Potential future mechanisms:

```text
ThemeExtension<T>
custom token groups
typed theme extensions
```

This requires separate API design.

Do not turn ThemeData into a generic dictionary of strings.

---

## 49. No String Token Lookup

Avoid:

```csharp
theme.GetColor("primary")
```

as the normal API.

Prefer:

```csharp
theme.Colors.Primary
```

Strong typing improves autocomplete and refactoring.

---

## 50. Custom Component Themes

Third-party/user-defined components should be able to define their own theme data.

Example concept:

```text
AudioCardTheme
```

LumaFlow should eventually provide a clean way to resolve custom component theme data from context.

---

## 51. Component Theme Ownership

Core should not know every future component type.

Therefore extension mechanisms must eventually support external theme types without modifying `ThemeData` endlessly.

Possible future design:

```text
ThemeExtension<T>
```

or typed registry.

This is deferred until needed.

---

## 52. Theme Versioning

Public theme types are public API.

Adding required properties to them can be breaking.

Prefer defaults and optional construction patterns that allow future extension.

---

## 53. Theme Constructors

Avoid constructors with dozens of mandatory parameters.

Prefer sensible defaults and override/copy patterns.

Example:

```csharp
ThemeData.Default.CopyWith(
    colors: ...
)
```

or a builder if needed.

---

## 54. No Giant Theme Constructor

Bad:

```csharp
new ThemeData(
    primary,
    secondary,
    surface,
    text,
    paddingXs,
    paddingSm,
    paddingMd,
    ...
)
```

This is difficult to maintain.

Use structured token groups.

---

## 55. CopyWith Pattern

A `CopyWith` style API may be appropriate.

Example:

```csharp
var theme = ThemeData.Default.CopyWith(
    colors: ThemeData.Default.Colors.CopyWith(
        primary: brandBlue
    )
);
```

Exact naming may follow C# conventions rather than Flutter literally.

---

## 56. Theme Equality

Theme value objects may benefit from structural equality.

This can reduce unnecessary updates.

Do not implement expensive deep equality blindly.

Profile and design appropriately.

---

## 57. Theme Caching

Resolved component styles may eventually be cached.

Cache keys could include:

```text
theme identity/version
component variant
explicit overrides
state
```

This is an optimization, not MVP requirement.

---

## 58. Theme Hot Path

Ordinary state changes should not repeatedly reconstruct entire theme objects.

Theme resolution should be efficient.

---

## 59. Framework Theme Defaults

Framework defaults must live in clearly identifiable code.

Example:

```text
LumaFlowDefaultTheme
```

not scattered across widget classes.

---

## 60. No Secret Defaults

Public component defaults should be traceable.

A developer should be able to determine why a Button is 36 px high or has a specific radius.

---

## 61. Documentation

Theme docs should explain:

```text
global tokens
component themes
explicit overrides
nested themes
USS interaction
```

Examples should show both minimal and advanced customization.

---

## 62. Sample Theme

LumaFlow should eventually include a polished sample theme demonstrating:

```text
colors
typography
spacing
radius
buttons
inputs
cards
```

This can be used in the component gallery.

---

## 63. Dogfooding

AudioLib should eventually use a custom LumaFlow theme.

This will validate:

- component theme ergonomics;
- token structure;
- nested overrides;
- real-world customization.

---

## 64. Rejected Alternative: Hardcoded Component Styling

Rejected:

```text
Button.cs contains all final visual values
```

Reason:

- difficult customization;
- duplicated design logic;
- no coherent application design system.

---

## 65. Rejected Alternative: USS-Only Theme System

Rejected as the only architecture:

```text
Theme
    ↓
USS stylesheet
```

Reasons:

- weaker typed API;
- dynamic theme changes are less direct;
- component semantic variants become harder to model;
- code-first experience suffers.

USS remains supported.

---

## 66. Rejected Alternative: Global Singleton Theme

Rejected:

```csharp
ThemeManager.Instance.Current
```

Reasons:

- multiple windows;
- multiple panels;
- nested themes;
- tests;
- Editor workflows;
- hidden global state.

---

## 67. Rejected Alternative: Theme as Dictionary

Rejected:

```csharp
Dictionary<string, object>
```

for normal token access.

Reasons:

- no type safety;
- no autocomplete;
- runtime errors;
- difficult refactoring.

---

## 68. Rejected Alternative: Per-Widget Manual Styling

Rejected as the intended common workflow:

```csharp
Button(
    backgroundColor: ...,
    textColor: ...,
    radius: ...,
    height: ...,
    padding: ...
)
```

for every Button.

Explicit overrides exist, but the theme should cover normal design.

---

## 69. Architectural Invariants Created by This ADR

Unless superseded:

### Invariant 1

Theme flows through BuildContext.

### Invariant 2

Theme is not a global mutable singleton.

### Invariant 3

Design tokens are centralized.

### Invariant 4

Components use semantic theme defaults.

### Invariant 5

Explicit widget values override theme defaults.

### Invariant 6

Nested theme overrides are supported.

### Invariant 7

Theme data should preferably be immutable.

### Invariant 8

USS remains interoperable but is not the sole theme mechanism.

### Invariant 9

Framework visual literals are minimized.

### Invariant 10

Core theme semantics remain render-pipeline independent.

---

## 70. Codex Rules

### Rule 1

Before adding a visual literal to a framework component, determine whether it belongs in ThemeData or a component theme.

### Rule 2

Do not access theme through global singletons.

### Rule 3

Resolve theme through BuildContext.

### Rule 4

Do not require USS for ordinary theme values.

### Rule 5

Do not remove USS interoperability.

### Rule 6

Prefer semantic variants over direct colors for standard component intent.

### Rule 7

Do not mutate shared ThemeData in place.

### Rule 8

Keep component theme resolution centralized.

### Rule 9

Do not introduce pipeline-specific types into Core theme objects.

### Rule 10

If external custom component themes become necessary, design an extensible typed mechanism rather than a string dictionary.

---

## 71. Decision Test

When adding a style value:

```text
Is this a one-off user override?
        ↓ yes
Expose explicit widget styling.

Is this a normal default for one component?
        ↓ yes
Put it in component theme.

Is this shared across the whole design system?
        ↓ yes
Put it in semantic design tokens.

Is it render-pipeline specific?
        ↓ yes
It does not belong in Core theme.
```

---

## 72. Example Target Theme

```csharp
var theme = new ThemeData
{
    Colors = new ColorScheme
    {
        Primary = BrandColors.Blue,
        Surface = BrandColors.Surface,
        SurfaceContainer = BrandColors.SurfaceRaised,
        TextPrimary = BrandColors.TextPrimary,
        TextSecondary = BrandColors.TextSecondary,
        Border = BrandColors.Border,
        Error = BrandColors.Error
    },

    Spacing = new SpacingScheme
    {
        XS = 4,
        S = 8,
        M = 12,
        L = 16,
        XL = 24
    },

    Radius = new RadiusScheme
    {
        Small = 4,
        Medium = 8,
        Large = 12,
        XLarge = 20
    }
};
```

Conceptual API only.

---

## 73. Example Consumer Usage

```csharp
return Card(
    child: Column(
        gap: context.Theme.Spacing.M,
        children:
        [
            Text(
                "Audio Settings",
                style: context.Theme.Typography.TitleLarge
            ),

            Button(
                "Save",
                variant: ButtonVariant.Primary,
                onPressed: Save
            )
        ]
    )
);
```

The developer expresses intent.

The theme provides visual consistency.

---

## 74. Reconsideration Conditions

Revisit this ADR if:

1. ThemeData becomes too rigid for third-party components;
2. theme propagation becomes a major performance issue;
3. UI Toolkit styling semantics require a different architecture;
4. USS proves significantly better for certain classes of theme behavior;
5. runtime theme switching exposes unresolved lifecycle problems.

Any major replacement requires a new ADR.

---

## 75. Final Decision

LumaFlow will treat theming as a first-class architectural layer.

The system is based on:

```text
ThemeData
+
semantic design tokens
+
component themes
+
scoped overrides
+
explicit widget overrides
```

The goal is that application code expresses UI intent while visual consistency is controlled centrally.

A LumaFlow application should be able to substantially change its appearance by changing its theme rather than rewriting component code.