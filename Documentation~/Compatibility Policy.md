# Compatibility and breaking changes

LumaFlow uses a standard package version; its existing Runtime API is frozen
as contract v1. This is a compatibility commitment, not a claim that every
widget, platform or Editor workflow is production-qualified.

## Current release status

Package version: `0.2.0` (unreleased working-tree version).
The package version, Runtime contract version and Unity Editor version describe
different things; Runtime contract v1 does not mean package release 1.0.

| Area | Current guarantee or evidence |
| --- | --- |
| Runtime signatures | Frozen contract v1; compatible extensions only. |
| Editor tools | Preview; not a supported extension API or fully qualified visual workflow. |
| Unity minimum | 6000.2.2f1; encoded with `unity: 6000.2` and `unityRelease: 2f1`. |
| Checked Editors | 6000.2.2f1 and 6000.4.5f1: affected assemblies compile; 56 targeted source-framework tests passed on each. This is not the full test suite. |
| Player/device qualification | No current full Windows Mono/IL2CPP and Android IL2CPP release evidence; touch, haptics, accessibility and performance still need representative device checks. |
| Table performance | Synthetic Editor CPU measurements only, without layout/rendering; no tablet FPS guarantee or table virtualization. |

Unity 6000.0 and 6000.1 are no longer declared supported. Newer Editors above
the declared minimum are not automatically certified by the manifest range.
CI jobs describe required checks, not proof of successful execution. Initial
qualification encountered an intermittent LayoutBuilder panel-metrics test;
an unchanged rerun passed, so repeatability remains a release-readiness concern.

CI now requires the complete Editor/Runtime functional suites on both exact
Editor pins, plus both clean archive consumers per Editor. These requirements
do not replace a successful hosted run or separate Player/device qualification.

The minimum version fields follow the official
[Unity package manifest format](https://docs.unity3d.com/6000.2/Documentation/Manual/upm-manifestPkg.html).

## Public API baseline

The source repository records exported Runtime types and their public or
protected members in `PublicApiBaseline.txt`. The compatibility test compares
the compiled `LumaFlow.Runtime` assembly with that snapshot. Separately,
`Editor/RuntimeApiContract.v1.txt` freezes the supported surface. The contract
test permits additive APIs but rejects removals and changes to existing
signatures, parameter names/defaults, enum values, inheritance and required
members. Public and protected members are included. Editor APIs, internal
implementation details and Unity APIs are outside this contract.

The exact baseline is reviewable; the frozen v1 file must not be regenerated.
The baseline writer checks v1 before writing. CI rejects changes or deletion
of existing versioned contracts relative to the pull request base. Adding a
new contract version does not waive the old one.

Framework maintainers can enable the `LUMAFLOW_DEVELOPMENT` scripting define to
update the baseline through **Tools > LumaFlow > Validation > Write Public API
Baseline** only after reviewing the source change, migration impact and
changelog entry.

## Compatible development

- Additive APIs require documented lifecycle, ownership and update behavior.
- Preserve old overloads when extending constructors or methods. Adding an
  optional parameter replaces a CLR signature and can break compiled clients.
- Deprecated APIs must name their replacement and remain callable. Warning-only
  deprecation is allowed; error-level deprecation is a breaking change.
- Namespace changes, assembly moves, signature changes, removed enum values and
  reduced member visibility are breaking changes.

## Stable releases

Use semantic versioning for the supported Runtime API even during preview:

- patch releases contain compatible fixes;
- minor releases may add compatible APIs;
- a deliberate breaking release requires an explicitly approved contract-policy
  change, a major version and migration instructions; simply rewriting v1 or
  adding v2 is not an acceptable bypass.

Increment `preview.N` for subsequent candidates on this track. Removing the
prerelease suffix, calling a build an RC, or promoting to `1.0.0` requires an
explicit release decision and retained evidence for the complete EditMode and
PlayMode suites, clean package imports/samples, declared Player build targets,
representative devices, performance budgets and supported Editor workflows.
The checklist is in `Validation.md`. Known failures must be resolved or explicitly
excluded from a narrowly documented support scope, not silently counted as passes.
Publishing tags, archives or registry versions is a separate authorized step.

For table styling, use `theme.WithDataTableTheme(tableTheme)`. The pre-table
15-parameter `ThemeData` constructor and `CopyWith` retain their defaults and
binary signatures. The full 16-parameter overloads remain available, with all
arguments required to avoid ambiguous source calls.

Maintainer checks: run `PublicApiCompatibilityTests`, review any exact-baseline
diff, and run `python Tools/LumaFlow/Validation/check_api_contract.py --base
origin/main`. Run the same compatibility checks for the independent distribution;
its namespace and assembly identity form its own contract. These gates check
the represented signatures; behavioral, serialization and platform regressions
still require dedicated tests.

Behavior covered by contract tests is part of compatibility even when public
signatures do not change. This includes reconciliation identity, ownership,
controlled input ordering, focus behavior, cleanup and inherited-scope
invalidation.

The Editor assembly is tooling and is not currently a supported extension API.
