# LumaFlow hardening

Work in this development project. Complete and verify changes in the order below.
An unchecked item is planned work, not a claim that its implementation is absent
in every form.

- [x] Establish a versioned development baseline including package, tests and CI.
- [x] Replace stale Lucide tooling with checks against the CC0 provenance manifest.
- [x] Separate Git-UPM and Asset Store payloads and validate generated archives.
- [ ] Expand CI to minimum/current Unity, clean package installation and builds.
- [x] Add C# edit-mode preview refreshed after assembly reload.
- [x] Add gradient backgrounds, clipping and other required styling primitives.
- [ ] Improve relative layout and root size propagation.
- [ ] Provide native-widget lifecycle hooks and pointer/drag interaction primitives.
- [ ] Improve composition of buttons and discrete/custom sliders.
- [ ] Add direct style animations and measure their cost.
- [ ] Split subsystem tests and cover consumer regressions.
- [ ] Add a realistic responsive sample and document the authoring workflow.

## Baseline audit

The local development folder originally had no Git history. The public package
repository did not contain the development tests, CI or validation tooling.
The two package source trees matched at audit time.

Icon maintenance now uses a stable catalog mapping and the CC0 provenance
manifest. The offline validator, generator check and negative regression tests
pass locally and have a dedicated CI job. The obsolete Lucide license requirement
was removed from tarball validation. Channel-specific archives now have deterministic
packaging, content/metadata/icon checks and negative regression tests. Unity import
and runtime smoke tests for the resulting archives remain open in the CI stage.
CI now defines clean archive smoke/build jobs on Unity 6000.0.0f1 and 6000.4.5f1
in addition to the full current-version suite. This item stays open until green
Unity evidence exists. Local 6000.4.5f1 and 6000.0.34f1 attempts fail in UPM with
`path argument ... undefined`; an empty 6000.4.5f1 project without LumaFlow fails
the same way. This establishes an environment failure, not successful package
validation. Logs are retained under `Artifacts/consumer-*.log`.

Hosted CI run 34585332523 was diagnosed on 2026-09-14: Unity activation inputs
are absent (repository Actions secrets list is empty). Image download succeeds,
then GameCI reports `Licensing method: <none>`. An activation preflight now blocks
the expensive Unity matrix early with setup instructions. Package and icon jobs
passed; the Unity validation milestone remains blocked on activation.

## UXML prototype

An editor-only factory, snapshot exporter and comparison window now generate
UXML/USS from the existing mount/style mappers. Initial scope covers ordinary
containers, labels, buttons and asset-backed images. A Unity 6000.4.5f1 isolated
`-noUpm` smoke run imported the assets and passed recursive layout/color/text
parity. This bypasses the local UPM issue; it is not package installation evidence.
Saved preview definitions now regenerate stable UXML/USS assets after assembly
reload without requiring the authoring window. A selected scene UIDocument can
be bound once and then displays that stable asset in Edit Mode. Common form
controls, responsive scenarios and direct UIDocument Inspector binding are
covered by isolated-Editor regression tests. Game View refresh after reload
remains a manual Editor check.

## Styling primitives

`BoxDecoration` now supports cached two-color linear gradients at arbitrary
angles. `Container` exposes explicit hard-edge clipping, including rounded-corner
clipping inherited from its decoration. Gradient textures are immutable, shared
by value and created once per distinct gradient; widget rebuilds reuse them.

Do not mark runtime or release items complete based only on static inspection.
