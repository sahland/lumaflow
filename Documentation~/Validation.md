# Release validation

This document describes the maintainer release checks. It is not included in
the published package.

## Required checks

1. Compile the Runtime, Editor, test and sample assemblies in the supported
   Unity versions.
2. Run the complete EditMode and PlayMode suites.
3. Run performance scenarios in a standalone Development Player.
4. Build Windows Mono, Windows IL2CPP and Android IL2CPP players.
5. Exercise keyboard navigation, focus restoration, overlays, scrolling,
   reduced motion and screen-reader behavior on representative devices.
6. Import the packed archive into a clean Unity project and compile the Getting
   Started sample.
7. Compare the Runtime public API with `PublicApiBaseline.txt`.
8. Inspect the result of `npm pack --dry-run --json`.
9. Confirm that the archive contains no repository tooling, tests, build
   artifacts or previously created package archives.

## Performance checks

Record medians and worst samples for:

- selection among eight tabs;
- reconciliation of one hundred keyed rows;
- virtualized-list mounting with 100, 1,000 and 10,000 source items;
- recycling of visible list rows;
- route push, pop and snapshot restoration;
- modal open and close;
- implicit-animation workloads.

Performance budgets are platform-specific. Capture allocation bytes with the
Unity Profiler in the target Player; event-count recorders are useful for
regression detection but are not byte measurements.

## Result handling

Keep the Unity version, scripting backend, operating system, hardware, test
result XML and profiler captures with each release candidate. A skipped,
inconclusive or missing suite does not count as a pass. Record environmental
failures separately when Unity fails before the LumaFlow package is imported.

