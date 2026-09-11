# ADR-002: Code-First Declarative C# as the Primary Authoring Model

- **Status:** Accepted
- **Decision date:** 2026-08-11
- **Scope:** Public API, authoring workflow, component model
- **Affects:** Runtime, Editor, Widgets, Styling, Documentation
- **Related documents:** `AGENTS.md`, `ARCHITECTURE.md`, `API_DESIGN.md`, `ADR-001-native-uitoolkit.md`

---

## 1. Context

Unity UI Toolkit traditionally encourages a combination of:

```text
UXML
USS
C#
```

For many interfaces this produces a workflow where structure, styling, behavior, state, and event wiring are distributed across several files.

A typical component may require:

```text
SettingsPanel.uxml
SettingsPanel.uss
SettingsPanel.cs
```

and sometimes additional controller, binding, or data files.

This workflow is valid and should remain supported by LumaFlow.

However, the primary product goal of LumaFlow is to provide a modern declarative programming model where UI structure, composition, state, and behavior can be expressed clearly in C#.

The intended developer experience is closer to:

```csharp
return Column(
    gap: 16,
    children:
    [
        Text("Settings"),

        TextField(
            label: "Username",
            value: username
        ),

        Button(
            "Save",
            onPressed: Save
        )
    ]
);
```

than to a multi-file UI definition workflow.

---

## 2. Decision

LumaFlow will use **code-first declarative C# as its primary UI authoring model**.

Application developers should be able to build complete production interfaces without requiring UXML or USS.

The preferred architecture is:

```text
C# declarative UI
        ↓
LumaFlow
        ↓
VisualElement hierarchy
        ↓
UI Toolkit
```

UXML and USS remain supported interoperability mechanisms.

They are not required dependencies of ordinary LumaFlow development.

---

## 3. Primary Authoring Experience

A normal LumaFlow component should be expressible in one C# type.

Example:

```csharp
public sealed class AudioCard : StatelessView
{
    public required AudioClip Clip { get; init; }

    public override Widget Build(BuildContext context)
    {
        return Card(
            child: Row(
                gap: 12,
                children:
                [
                    Icon(Icons.Audio),

                    Expanded(
                        child: Column(
                            children:
                            [
                                Text(Clip.name),
                                Text($"{Clip.length:F1}s")
                            ]
                        )
                    ),

                    IconButton(
                        icon: Icons.Play,
                        onPressed: Play
                    )
                ]
            )
        );
    }
}
```

Creating this component should not require:

```text
AudioCard.uxml
AudioCard.uss
AudioCardController.cs
```

unless the developer explicitly chooses those integration mechanisms.

---

## 4. Why Code-First

Code-first authoring provides several benefits for LumaFlow's target workflow.

### Locality

Structure and behavior exist close together.

### Refactoring

IDE refactoring works naturally across:

- widget types;
- properties;
- methods;
- callbacks;
- state;
- generic types.

### Type safety

Invalid configuration can often be detected by the compiler.

### Composition

Components can be ordinary reusable C# types.

### State integration

Reactive state can participate directly in construction and binding.

### Discoverability

Autocomplete can expose available components and options.

### Version control

UI changes remain ordinary source-code diffs rather than partially serialized assets.

### Generics

Strongly typed reusable UI patterns become possible.

---

## 5. Declarative, Not Imperative

Code-first does not mean returning to imperative `VisualElement` construction.

This is not the target:

```csharp
var root = new VisualElement();
root.style.flexDirection = FlexDirection.Column;

var title = new Label("Settings");
root.Add(title);

var button = new Button(Save)
{
    text = "Save"
};

root.Add(button);

return root;
```

The target is:

```csharp
return Column(
    children:
    [
        Text("Settings"),

        Button(
            "Save",
            onPressed: Save
        )
    ]
);
```

Therefore this ADR establishes:

```text
Code-first
+
Declarative
```

not merely:

```text
Code instead of UXML
```

---

## 6. UXML Remains Supported

LumaFlow must remain interoperable with existing UXML.

Developers may need to:

- migrate an existing project gradually;
- embed legacy UI;
- use UI Builder;
- use third-party UXML controls;
- preserve designer-created layouts;
- work with existing Unity tooling.

Possible integration mechanisms may include:

```csharp
Uxml(
    asset: settingsTemplate
)
```

or:

```csharp
Native(
    template.CloneTree()
)
```

The exact API may evolve.

The interoperability requirement does not.

---

## 7. UXML Is Secondary

LumaFlow should not shape its core architecture around UXML requirements.

For example, a Widget should not require:

```text
UxmlFactory
UxmlTraits
VisualTreeAsset
```

to exist simply because UXML support may be useful.

Code-first components must remain fully functional without UXML registration.

---

## 8. UI Builder Support

UI Builder compatibility may be added where practical.

Possible future uses:

- previewing custom native controls;
- placing LumaFlow-compatible components;
- consuming generated or wrapped controls;
- hybrid workflows.

However, UI Builder support is an optional integration concern.

It must not dictate the core component model.

---

## 9. USS Remains Supported

USS is a valid part of UI Toolkit and must remain available.

LumaFlow should allow:

- attaching stylesheets;
- assigning USS classes;
- using existing project USS;
- using pseudo-state styling;
- integrating third-party styles.

Example conceptual API:

```csharp
Container(
    classes:
    [
        "settings-panel",
        "surface"
    ],
    child: ...
)
```

or equivalent.

---

## 10. USS Is Not Required for Component-Level Styling

A normal LumaFlow developer should not need a USS file to:

- add padding;
- set a background color;
- apply border radius;
- set typography;
- apply standard component variants;
- set common alignment;
- configure common spacing.

These should be available through typed APIs and themes.

Example:

```csharp
Container(
    padding: EdgeInsets.All(16),
    decoration: BoxDecoration(
        color: context.Theme.Colors.Surface,
        borderRadius: BorderRadius.All(12)
    ),
    child: content
)
```

---

## 11. USS for Global and Advanced Styling

USS remains especially useful for:

```text
large shared style systems
pseudo states
complex selector-based rules
project-specific customization
existing UI Toolkit assets
transitions
specialized advanced styling
```

LumaFlow does not attempt to make USS obsolete.

It reduces mandatory dependence on it.

---

## 12. Styling Priority

When both typed LumaFlow styling and USS affect the same element, precedence must be deliberate and documented.

The exact implementation may depend on UI Toolkit style resolution.

LumaFlow must avoid silently creating confusing fights between:

```text
inline LumaFlow styles
USS selectors
theme defaults
```

The public documentation must clearly explain expected precedence.

---

## 13. Theme-First Styling

Standard component appearance should primarily come from `ThemeData`.

Example:

```csharp
Button(
    "Save",
    variant: ButtonVariant.Primary,
    onPressed: Save
)
```

should normally require no explicit USS class.

The component should resolve appearance through:

```text
Button
  ↓
Button theme
  ↓
global theme tokens
  ↓
framework defaults
```

---

## 14. Code-First Components

User-defined reusable components should normally use C# composition.

Preferred:

```csharp
public sealed class StatusBadge : StatelessView
{
    public required string Text { get; init; }

    public override Widget Build(BuildContext context)
    {
        return Container(
            padding: EdgeInsets.Symmetric(
                horizontal: 8,
                vertical: 4
            ),
            decoration: BoxDecoration(
                color: context.Theme.Colors.Success
            ),
            child: Text(Text)
        );
    }
}
```

Instead of requiring a template asset.

---

## 15. Assets Are Still Valid Inputs

Code-first UI may still reference Unity assets.

Examples:

```text
Texture2D
VectorImage
Font
StyleSheet
VisualTreeAsset
AudioClip
ScriptableObject
```

Code-first does not mean asset-free.

It means UI composition is controlled primarily from typed C#.

---

## 16. No Required MonoBehaviour UI Controllers

Ordinary LumaFlow components must not require a `MonoBehaviour`.

Bad default architecture:

```text
SettingsPanelView.uxml
SettingsPanelController : MonoBehaviour
SettingsPanelBinder
```

Preferred:

```text
SettingsView : StatelessView
```

or:

```text
SettingsView : StatefulView
```

MonoBehaviours may still host application-level mount points or Unity-specific lifecycle integration when needed.

---

## 17. No Required UIDocument Ownership Per Component

LumaFlow components should be mountable inside an existing UI Toolkit tree.

Do not require every component to own:

```text
UIDocument
PanelSettings
GameObject
```

A component is primarily a Widget.

Mount infrastructure owns attachment to a UI Toolkit root.

---

## 18. Hybrid Workflows

LumaFlow should support hybrid authoring.

Example:

```text
existing UXML application
        ↓
LumaFlow component inserted
```

and:

```text
LumaFlow application
        ↓
existing UXML subtree inserted
```

This enables gradual adoption.

---

## 19. Existing Project Migration

LumaFlow should not require a full rewrite of existing UI Toolkit applications.

Migration can happen incrementally.

Example progression:

```text
Raw UI Toolkit
    ↓
Use LumaFlow Button/Card
    ↓
Convert one panel
    ↓
Use ThemeData
    ↓
Use reactive state
    ↓
Convert remaining screens
```

Interop is therefore strategically important.

---

## 20. Public API Implication

Because C# is the primary authoring model, public APIs should optimize for:

```text
IntelliSense
strong typing
named parameters
generic composition
ordinary C# refactoring
collection expressions where supported
```

Do not design APIs primarily around serialization constraints.

---

## 21. Serialized Configuration

Some future components may need serialized configuration.

That does not mean core Widgets should become serialized objects.

Avoid making every Widget:

```text
ScriptableObject
MonoBehaviour
UnityEngine.Object
```

just to enable Inspector authoring.

UI descriptions should remain lightweight managed objects unless a real Unity asset use case requires otherwise.

---

## 22. UI Definition Files

LumaFlow does not introduce a new mandatory UI markup format.

Rejected idea:

```text
.luma
.lui
.json UI templates
custom XML
```

as the primary authoring workflow.

The project already has C# as a powerful declarative host language.

Creating another mandatory markup language would recreate many problems LumaFlow is intended to reduce.

---

## 23. No Code Generation Requirement

LumaFlow must not require code generation to translate UI definitions into usable runtime code.

This is intentionally valid:

```csharp
return Column(
    children:
    [
        Text("Hello")
    ]
);
```

without an additional compile/generate step owned by LumaFlow.

---

## 24. Optional Source Generation

Future source generators may provide optional tooling such as:

```text
compile-time validation
generated bindings
optimized theme access
strongly typed assets
developer tooling
```

These must remain optional unless a future ADR explicitly changes the policy.

Core Widget authoring must not depend on them.

---

## 25. Hot Reload Considerations

Code-first authoring may eventually benefit from improved iteration tooling.

Potential future features:

```text
view refresh
widget preview
component gallery
hot-reload-like development
```

However, iteration speed must not be solved by introducing mandatory markup purely because UXML can be edited independently.

Tooling should improve the code-first workflow instead.

---

## 26. Editor Windows

EditorWindow usage should feel natural.

Conceptually:

```csharp
public sealed class AudioLibWindow : EditorWindow
{
    private LumaMountHandle _mount;

    public void CreateGUI()
    {
        _mount = LumaFlow.Mount(
            new AudioLibView(),
            rootVisualElement
        );
    }
}
```

Higher-level helpers may later reduce this further.

The user should not need a UXML file to create an EditorWindow.

---

## 27. Runtime Mounting

Similarly, runtime applications may mount into an existing `UIDocument` root.

Conceptually:

```csharp
LumaFlow.Mount(
    new MainMenu(),
    document.rootVisualElement
);
```

The exact bootstrap API may evolve.

---

## 28. Code Organization

A code-first application may use a structure such as:

```text
UI/
├── Screens/
│   ├── HomeScreen.cs
│   └── SettingsScreen.cs
│
├── Components/
│   ├── AudioCard.cs
│   ├── StatusBadge.cs
│   └── SearchBar.cs
│
├── Theme/
│   └── AppTheme.cs
│
└── State/
    └── ...
```

LumaFlow should not force this exact structure.

It should make such organization natural.

---

## 29. Separation Still Matters

Code-first does not mean putting an entire application in one file.

Developers should still extract reusable:

```text
components
screens
themes
state
models
services
```

The framework supports composition.

It does not encourage monolithic source files.

---

## 30. Designer Collaboration

LumaFlow is primarily developer-oriented.

However, the architecture should not intentionally prevent designer workflows.

Designers may still work through:

- themes;
- design tokens;
- UXML integrations;
- UI Builder;
- component galleries;
- visual reference designs;
- future preview tooling.

Code-first is a primary authoring choice, not a rejection of visual design processes.

---

## 31. Documentation Examples

Official documentation should teach code-first usage first.

For example, Getting Started should begin with:

```csharp
return Column(
    children:
    [
        Text("Hello LumaFlow"),

        Button(
            "Continue",
            onPressed: Continue
        )
    ]
);
```

not with:

```xml
<ui:VisualElement>
...
</ui:VisualElement>
```

UXML integration should have its own dedicated documentation section.

---

## 32. Public Messaging

LumaFlow may describe itself using language such as:

```text
Declarative C# UI for Unity UI Toolkit
```

or:

```text
Build Unity UI like modern declarative frameworks,
with native UI Toolkit underneath.
```

Avoid messaging that suggests UXML is unsupported.

---

## 33. Rejected Alternative: UXML-First Framework

Rejected:

```text
LumaFlow components
        ↓
custom UXML tags
        ↓
C# controllers
```

as the primary workflow.

Reason:

This would improve component appearance but would not solve the core developer-experience problem LumaFlow targets.

Developers would still frequently switch between:

```text
markup
styles
code
```

---

## 34. Rejected Alternative: Mandatory USS Themes

Rejected:

```text
ThemeData
        ↓
generated USS only
```

as the sole styling architecture.

Reason:

Dynamic component styling, typed APIs, contextual themes, and reactive styling benefit from direct C# representation.

USS remains interoperable but not mandatory.

---

## 35. Rejected Alternative: Generated UXML

Rejected as the primary model:

```text
C# Widget tree
        ↓
generate UXML
        ↓
clone VisualTreeAsset
```

Reasons:

- unnecessary intermediate artifact;
- awkward dynamic UI;
- complicates state;
- complicates runtime composition;
- harder lifecycle;
- slower iteration;
- no clear benefit for core usage.

---

## 36. Rejected Alternative: Visual-Builder-First Custom Editor

LumaFlow may eventually provide visual tooling.

However, a proprietary visual builder will not be required to author LumaFlow UI.

Reason:

The framework's main value is its C# developer experience.

A visual builder would be an optional productivity layer.

---

## 37. Architectural Invariants Created by This ADR

Unless superseded:

### Invariant 1

Declarative C# is the primary authoring API.

### Invariant 2

UXML is optional.

### Invariant 3

USS is optional for ordinary component styling.

### Invariant 4

Existing UXML and USS remain interoperable.

### Invariant 5

No custom mandatory markup language is introduced.

### Invariant 6

No code-generation step is required for basic UI.

### Invariant 7

Ordinary reusable components can exist entirely in C#.

### Invariant 8

Core architecture is not designed around Unity serialization constraints.

---

## 38. Codex Rules

### Rule 1

When implementing a feature, design the C# consumer API first.

### Rule 2

Do not require creation of a UXML file for an ordinary new Widget.

### Rule 3

Do not require a USS class for ordinary styling that LumaFlow already models through typed APIs.

### Rule 4

Do not remove native UXML or USS interoperability.

### Rule 5

Do not create a custom markup language without a new ADR.

### Rule 6

Do not introduce code generation as a core requirement without a new ADR.

### Rule 7

Prefer reusable C# composition over template-specific controller classes.

### Rule 8

Do not require a MonoBehaviour for ordinary component logic.

---

## 39. Decision Test

When implementing a component, ask:

```text
Can it be authored entirely in declarative C#?
        ↓ no
Why?

Is the limitation caused by UI Toolkit?
        ↓ yes
Provide an integration path.

Is UXML merely easier for the implementation?
        ↓ yes
Do not make it mandatory.

Can USS still be used by advanced users?
        ↓ no
Restore interoperability.
```

---

## 40. Example Target

A complete settings card should be possible as:

```csharp
public sealed class AudioSettingsCard : StatelessView
{
    public required State<float> Volume { get; init; }

    public override Widget Build(BuildContext context)
    {
        return Card(
            child: Padding(
                padding: EdgeInsets.All(16),
                child: Column(
                    gap: 12,
                    children:
                    [
                        Text(
                            "Audio",
                            style: context.Theme.Typography.TitleMedium
                        ),

                        Slider(
                            value: Volume,
                            min: 0f,
                            max: 1f
                        )
                    ]
                )
            )
        );
    }
}
```

No UXML is required.

No USS is required.

No MonoBehaviour is required.

The resulting UI remains native UI Toolkit.

---

## 41. Example Hybrid Integration

An existing UXML subtree should still be embeddable:

```csharp
return Column(
    children:
    [
        Text("LumaFlow Header"),

        Native(
            existingTemplate.CloneTree()
        )
    ]
);
```

Conceptual syntax only.

This preserves migration and interoperability.

---

## 42. Reconsideration Conditions

This ADR may be reconsidered if:

1. code-first authoring proves significantly less productive in real LumaFlow projects;
2. Unity introduces a radically improved authoring model that changes the tradeoffs;
3. visual tooling becomes the dominant user requirement;
4. code-first prevents critical workflows;
5. a different primary model provides clearly superior DX while preserving framework goals.

Even then, code-first support should likely remain important.

---

## 43. Final Decision

LumaFlow's primary development experience is:

```text
C#
+
declarative composition
+
typed styling
+
reactive state
+
native UI Toolkit
```

UXML and USS remain first-class integration technologies.

They are not mandatory authoring layers.

The target experience is:

```csharp
return Column(
    gap: 16,
    children:
    [
        Text("Settings"),

        TextField(
            label: "Username",
            value: username
        ),

        Button(
            "Save",
            onPressed: Save
        )
    ]
);
```

rather than requiring the developer to coordinate multiple markup, style, and controller files for ordinary UI.

That code-first workflow is a foundational part of LumaFlow's identity.