# Self-hosted GUI Performance

Open **Tools > LumaFlow > GUI Performance**, or use the button in LumaFlow UXML.
The window is part of the package, not an application-specific IMGUI tool. Its
layout, controls, theme and report are authored with LumaFlow.

If a project contains an older `Assets/Editor/GuiPerformanceWindow.cs`, its
`Tools/GUI/Performance` menu is still project-owned. Use the packaged menu above;
installing LumaFlow does not delete or rewrite consumer source files.

## Almost automatic capture

1. Leave **Capture automatically on entering Play Mode** enabled (the default).
2. Choose an optional scenario label and 60, 300 or 1000 frame samples.
3. Enter Play Mode. The window waits one second, captures, then displays a report.
   Opening it during Play Mode starts the same workflow.
4. Use **Capture now** for another measurement, **Cancel** to stop, or **Copy
   report** to copy a completed result.

The automatic preference is stored per user/project. A pause or mode exit
cancels incomplete work. Closing the window or reloading assemblies releases
all native recorders; interrupted data is not presented as a successful capture.
Automatic mode captures once per play session while the window is open, not
continuously. No profiler is installed in the scene or included in a Player.

## What the numbers mean

- **Main Thread**: average, p95 and maximum, converted from nanoseconds to ms.
  It includes waits; it is not CPU time exclusively spent in LumaFlow.
- **GC/frame**: allocation counter converted from bytes to KiB.
- **Draw calls**: the Render profiler's draw count.
- **n**: available samples in that counter's fixed-capacity buffer. After the
  30-second timeout the available sample count may be below the requested size.

Missing/empty counters are reported as **unavailable**, not zero. These are
whole-Editor/application counters, including Editor UI and unrelated rendering.
Compare the same scenario, Editor version, graphics backend and hardware. Final
performance budgets require a development Player on the target device and Unity
Profiler, not a claim derived solely from this window.

## Overhead and ownership

Sampling uses fixed native ProfilerRecorder buffers. The report is aggregated
once, after capture; there is no per-frame managed sample list or UI progress
rebuild. The Editor update callback exists only during warm-up/capture. UI changes
at the start/end still have a cost, so this is a diagnostic aid, not an isolated
zero-overhead microbenchmark. Runtime assemblies do not reference UnityEditor.

## Reproducible validation

From the development repository, run the isolated checks with a new output path:

```powershell
./Tools/LumaFlow/Validation/Invoke-EditorToolsSmoke.ps1 -UnityPath 'C:/Program Files/Unity/Hub/Editor/6000.4.5f1/Editor/Unity.exe' -OutputDirectory 'E:/LumaFlow/Artifacts/editor-tools-check'
```

This compiles current sources and checks startup policy/preferences, discovery,
viewport and comparison, mount cleanup, automatic/manual captures, pause/cancel
and recorder cleanup across mode changes. It uses a separate Unity process/project
and does not stop Play Mode in your working Editor. Interactive project-launch
focus behavior and target-device profiling remain separate acceptance checks.
