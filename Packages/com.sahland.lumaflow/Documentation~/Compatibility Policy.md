# Compatibility and breaking changes

LumaFlow `0.x` is a preview API. Public changes are controlled, documented and
reviewed, but source compatibility is not guaranteed until `1.0`.

## Public API baseline

The source repository records exported Runtime types and their public or
protected members in `PublicApiBaseline.txt`. The compatibility test compares
the compiled `LumaFlow.Runtime` assembly with that snapshot.

Update the baseline through **Tools > LumaFlow > Validation > Write Public API
Baseline** only after reviewing the source change, migration impact and
changelog entry.

## Preview releases

- Additive APIs require documented lifecycle, ownership and update behavior.
- Source-breaking corrections require a migration note and changelog entry.
- Deprecated APIs must name their replacement.
- Namespace changes, assembly moves, signature changes, removed enum values and
  reduced member visibility are breaking changes.

## Stable releases

Starting with `1.0`, semantic versioning applies to the supported Runtime API:

- patch releases contain compatible fixes;
- minor releases may add compatible APIs;
- major releases may introduce documented breaking changes.

Behavior covered by contract tests is part of compatibility even when public
signatures do not change. This includes reconciliation identity, ownership,
controlled input ordering, focus behavior, cleanup and inherited-scope
invalidation.

The Editor assembly is tooling and is not currently a supported extension API.
