# Getting Started sample

1. Import this sample from Package Manager.
2. Create a `UIDocument` with a full-screen `PanelSettings` asset.
3. Add `LumaFlowCounterSample` to the **same GameObject** as the `UIDocument`.
4. Enter Play Mode.
5. Open **Window > LumaFlow > Widget Inspector** to inspect the mounted tree.

The component resolves the `UIDocument` on its own GameObject. All presentation below
`BuildApplication` uses LumaFlow widgets, and `OnDisable` disposes the owned
`MountHandle`. The screen also demonstrates tree-scoped theming, the bundled
LumaFlow brand icon, reactive state, semantic buttons, cards and flex layout.
