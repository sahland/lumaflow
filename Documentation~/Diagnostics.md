# Diagnostics and Widget Inspector

Diagnostics provide a non-owning projection of mounted LumaFlow trees for
development tooling, bug reports and lifecycle verification. A diagnostic
snapshot is not application state.

## Capture one mount

Every live `MountHandle` exposes a process-local `DiagnosticId` and can capture
an immutable snapshot:

```csharp
using var mount = LumaFlow.Mount(application, root);

WidgetTreeDiagnostics snapshot = mount.CaptureDiagnostics();
Debug.Log(snapshot.ToStringDeep());
```

The snapshot contains:

- widget and internal node type;
- `WidgetKey` and state type, when present;
- node lifecycle state;
- native element type, name, classes, enabled/focused state, and resolved layout;
- inherited aspects actually read by the node;
- immutable child snapshots and actionable findings.

Snapshots contain strings, value types, and child snapshots. They do not retain
`WidgetNode`, `WidgetState`, `VisualElement`, or `MountHandle` instances.
Capturing a disposed handle throws `ObjectDisposedException`.

## Active mounts

```csharp
IReadOnlyList<WidgetTreeDiagnostics> active =
    LumaFlowDiagnostics.CaptureActiveTrees();
```

The registry stores weak references and removes a mount during disposal. It does
not turn forgotten `MountHandle.Dispose()` calls into permanent framework roots.
Capture these UI Toolkit values on Unity's main thread.

## Editor window

Open **Window > LumaFlow > Widget Inspector**. Press **Refresh** to capture active
trees. The window shows:

- mount and node counts;
- keys and state types;
- current findings;
- focused and disabled nodes;
- tooltips with native type, layout, lifecycle, and inherited dependencies;
- **Copy tree** output suitable for issue reports.

Refresh is explicit so the Inspector does not create a permanent Editor update
subscription or continuously allocate snapshots.

## Finding codes

| Code | Severity | Meaning | Action |
| --- | --- | --- | --- |
| `LF1001` | Warning | A stateful widget is one of several unkeyed siblings and therefore has positional identity. | Assign a stable `WidgetKey` before the collection can insert, remove, or reorder entries. |
| `LF1002` | Info | The tree crosses a `Native` UI Toolkit ownership boundary. | Keep borrowed elements detached before mount and never reparent them externally while mounted. |

Findings do not log warnings automatically, mutate the tree or assume that a
static sibling list will later become dynamic.
