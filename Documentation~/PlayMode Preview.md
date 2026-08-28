# PlayMode preview lifecycle

LumaFlow exposes two explicit lifecycle operations on an active `MountHandle`:

- `Rebuild()` re-evaluates mounted declarative builder boundaries and reconciles
  compatible descriptions in place. Compatible `StatefulWidget` nodes keep their
  mount-local `WidgetState`.
- `Restart(widget)` mounts a fresh root description and then releases the old
  tree. Mount-local state is reset. If the replacement cannot be
  mounted, the previous tree remains active.

Applications can expose these operations as explicit Reassemble and Restart
actions:

```csharp
void Reload() => _mount?.Rebuild();

void Restart() => _mount?.Restart(CreateApplication());
```

On Unity 6000.4+, `PlayModePreviewToolbar.Register(reload, restart)` connects
those application-owned callbacks to two LumaFlow icon buttons immediately
after Unity's Play/Pause/Step controls:

- **Reassemble** calls the registered reload callback in the currently loaded
  managed domain. It preserves compatible LumaFlow state but does not compile
  changed C# source.
- **Compile & Restart** exits Play Mode, refreshes the Asset Database, requests
  script compilation, and automatically re-enters Play Mode after a successful
  quiet compilation period. The operation survives assembly reload through a
  `SessionState` state machine. It resets application state.

The registration is disposed with the application bootstrap.

## Unity script compilation boundary

Unity's standard C# compiler always reloads managed assemblies. It cannot offer
C# recompilation cannot preserve state by patching methods in place. Continuing
the same LumaFlow mount through that boundary is unsupported because ownership
and UI Toolkit native objects cross different lifetimes.

True C# hot reload therefore requires a method-patching backend such as Hot
Reload for Unity or FastScriptReload. After such a backend applies changed
method bodies without a domain reload, the Reassemble button can re-run LumaFlow
builders while preserving compatible widget state. Changes to type shape,
fields, generic layouts, or other unsupported edits still require Compile &
Restart.

Unity references:

- [Preferences: Script Changes While Playing](https://docs.unity3d.com/6000.0/Documentation/Manual/Preferences.html)
- [Configurable Enter Play Mode and Domain Reloading](https://docs.unity3d.com/6000.0/Documentation/Manual/domain-reloading.html)

## Ownership

Keep the `MountHandle` at the native bootstrap boundary. In Editor builds,
register its Reassemble/Restart callbacks after mounting and dispose that toolbar
registration before disposing the handle in `OnDisable`. The toolbar content is
an ordinary LumaFlow `Row` containing two `IconButton` widgets; outside PlayMode
they remain visible but disabled.
