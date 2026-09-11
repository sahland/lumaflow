# LumaFlow Performance Baselines

**Status:** alpha measurement protocol; release budgets are not locked yet.  
**Last reviewed:** 2026-08-20

LumaFlow performance changes must be measured with the Unity Performance Testing
package instead of inferred from code shape or one stopwatch result. Contract
tests remain separate from performance tests: correctness failures block the
change, while performance samples establish comparable baselines until beta
budgets are approved.

## Test assembly

Performance tests live in:

```text
Assets/LumaFlow.Tests/Performance
```

`LumaFlow.Performance.Tests` is enabled only when
`com.unity.test-framework.performance` version 3.x is installed. This is a test
dependency, not a runtime dependency of the LumaFlow package.

The first retained-Navigator baseline records:

| Sample group | Measured operation |
| --- | --- |
| `Navigator.Mount.16RoutesBatch` | Mount 16 independent one-route hosts. |
| `Navigator.Push.16RetainedRoutesBatch` | Push 16 prepared descriptions into one retained history. |
| `Navigator.Pop.16RetainedRoutesBatch` | Pop and release 16 retained entries. |
| `Navigator.Dispose.32RetainedRoutes` | Dispose a host owning 32 retained route entries. |

Every sample also records Unity `GC.Alloc` events. Route descriptions and test
fixtures are prepared outside the measured region. Cleanup assertions run after
the sample and prove that retained layers are present at the expected depth and
fully detached at the host lifetime boundary.

## Running a local baseline

1. Run the ordinary Runtime and Editor contract suites first. Do not profile a
   build with correctness failures.
2. Open **Window > General > Test Runner** and run
   `LumaFlow.Performance.Tests` in Edit Mode.
3. Inspect **Window > General > Performance Test Report**.
4. Repeat the complete performance suite at least three times with the same
   Unity version, scripting backend, build target, hardware power mode, and
   background workload.
5. Accept a local baseline only when repeated samples are reasonably stable;
   target a data-set deviation below 5% before comparing changes.
6. Preserve the generated `PerformanceTestResults.json` as a CI artifact when
   the performance job is introduced.

Record environment metadata with every published result:

```text
Unity version
LumaFlow revision/package version
OS and CPU
Editor or Player
Mono or IL2CPP
Build target and configuration
Profiler/development-build state
```

## Interpreting results

- Compare the same named sample group under the same environment.
- Use medians and distribution stability, not only the fastest sample.
- A lower time or allocation count is not accepted as an optimization until all
  contract tests and structural cleanup assertions remain green.
- EditMode results characterize managed framework operations and UI Toolkit
  hierarchy ownership. They do not represent rendered frame cost, layout/style
  pass duration, device memory, or Player/IL2CPP behavior.
- Do not copy a threshold from one developer machine into a release gate.

## Beta gate still pending

Before beta, LumaFlow still needs representative Player measurements, IL2CPP
coverage, large-history memory characterization, inherited-update and list
virtualization baselines, CI history, and reviewed regression budgets. Stable API
or production-performance claims are not justified by this initial suite.
