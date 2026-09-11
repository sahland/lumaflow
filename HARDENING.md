# LumaFlow hardening

Work in this development project. Complete and verify changes in the order below.
An unchecked item is planned work, not a claim that its implementation is absent
in every form.

- [x] Establish a versioned development baseline including package, tests and CI.
- [x] Replace stale Lucide tooling with checks against the CC0 provenance manifest.
- [x] Separate Git-UPM and Asset Store payloads and validate generated archives.
- [ ] Expand CI to minimum/current Unity, clean package installation and builds.
- [ ] Add C# edit-mode preview refreshed after assembly reload.
- [ ] Add gradient backgrounds, clipping and other required styling primitives.
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

Do not mark runtime or release items complete based only on static inspection.
