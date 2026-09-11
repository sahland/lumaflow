# LumaFlow Threading and Async Policy

This document defines the threading and asynchronous execution contract of LumaFlow.

It applies to:

```text
WidgetNode
BuildContext
BindingScope
MountHandle
Navigator
Overlay
State<T>
native UI Toolkit interaction
framework-owned async operations
```

The initial policy intentionally favors simple, explicit semantics over hidden thread dispatch.

---

# 1. Core Decision

LumaFlow UI runtime is main-thread-affine.

Operations that interact with mounted LumaFlow UI or Unity UI Toolkit must occur on Unity's main thread.

This includes:

```text
mount
unmount
native VisualElement mutation
WidgetNode lifecycle
binding native properties
navigation mutation
overlay mutation
context-tree mutation
```

---

# 2. No Implicit Global Dispatcher

LumaFlow Core will not silently marshal arbitrary API calls from worker threads to Unity's main thread.

Forbidden implicit behavior:

```text
worker thread
↓
State.Value changes
↓
LumaFlow secretly schedules UI mutation sometime later
```

unless a future explicitly designed scheduling API is introduced.

Hidden dispatch creates:

```text
ordering ambiguity
race conditions
surprising latency
lifecycle races
harder testing
```

---

# 3. Widget

Widget descriptions are primarily immutable configuration.

Constructing a Widget off the main thread may be technically safe when it only stores plain immutable data.

However LumaFlow does not guarantee that every future Widget constructor is thread-safe.

Consumers should not assume arbitrary Widget construction is a concurrent API contract unless documented.

---

# 4. WidgetNode

`WidgetNode` is strictly main-thread-affine.

All node lifecycle operations occur on the Unity main thread.

This includes:

```text
Create native elements
Mount
Update
Unmount
Dispose
Child mutation
Context assignment
Binding setup
```

---

# 5. VisualElement

All `VisualElement` access performed by LumaFlow follows Unity UI Toolkit threading requirements.

LumaFlow must not attempt to make native UI Toolkit objects thread-safe.

---

# 6. BuildContext

`BuildContext` is a mount-tree runtime object.

It is main-thread-affine.

It must not be treated as:

```text
thread-safe service container
global immutable application context
background-task dependency container
```

---

# 7. BuildContext Lifetime

BuildContext is valid only while its corresponding mounted scope remains valid.

Do not retain BuildContext for long-lived asynchronous operations without explicit lifecycle reasoning.

Bad:

```csharp
var context = thisContext;

await SomeLongOperation();

context.Navigator.Push(...);
```

because the originating subtree may have been unmounted during the await.

---

# 8. Prefer Extracting Durable Dependencies

Before awaiting, extract only the durable application data or explicit runtime handle that is safe to retain.

Conceptually:

```csharp
var request = repository.LoadAsync(...);

var result = await request;

// verify owner is still active before touching mounted UI
```

Exact patterns depend on component lifecycle APIs.

---

# 9. State<T> Initial Threading Contract

`State<T>` does not guarantee thread safety.

The initial implementation may assume serialized access.

This applies to:

```text
Value get/set
Subscribe
Unsubscribe
notification delivery
```

Do not use `State<T>` as a concurrent synchronization primitive.

---

# 10. UI-Bound State<T>

A `State<T>` currently bound to mounted UI should be mutated from Unity's main thread.

Example:

```text
main thread
State.Value = newValue
↓
listener
↓
Label.text update
```

is the normal supported path.

---

# 11. Worker Thread State Mutation

Initial LumaFlow behavior does not promise safe worker-thread mutation of UI-bound State.

Consumers must marshal the result to the main thread through their application/runtime scheduling mechanism before updating UI state.

---

# 12. No Locking by Default

`State<T>` should not introduce internal locking merely to appear thread-safe.

Thread safety would require precise semantics around:

```text
notification ordering
reentrant mutation
unsubscription during notification
main-thread dispatch
lifecycle
```

and would add cost to every normal UI update.

Do not introduce this complexity without a separate decision.

---

# 13. Notification Thread

`State<T>` listeners execute synchronously on the thread that performs the mutation unless a future API explicitly states otherwise.

Because UI-bound State should be mutated on the main thread, UI listeners therefore execute on the main thread.

---

# 14. Synchronous Notification

Initial `State<T>` notification is synchronous.

Conceptually:

```csharp
state.Value = value;
```

completes listener notification before returning.

Do not silently defer notifications to a future frame.

---

# 15. Reentrant State Mutation

A State listener may potentially cause another state mutation.

Implementation must define behavior consistently and protect internal subscriber collection integrity.

Do not introduce arbitrary locks.

Tests should cover:

```text
listener unsubscribes itself
listener causes another State update
listener causes owner unmount
```

as relevant.

---

# 16. Navigator

Navigator mutations are main-thread-only.

Examples:

```text
Push
Pop
Replace
```

must not manipulate route stacks from worker threads.

---

# 17. Overlay

Overlay operations are main-thread-only.

Examples:

```text
Show
Close
ShowDialog
ShowPopover
```

must execute on the UI thread.

---

# 18. MountHandle

`MountHandle.Dispose()` is main-thread-affine because disposal unmounts native UI.

Do not use MountHandle as a cross-thread disposal primitive.

---

# 19. BindingScope

BindingScope lifecycle belongs to WidgetNode lifecycle and is therefore main-thread-affine unless an owned resource explicitly supports independent cleanup.

Framework-owned node cleanup still begins on the main thread.

---

# 20. Async Application Callbacks

Application callbacks are free to start asynchronous work.

Example:

```csharp
Button(
    "Refresh",
    onPressed: RefreshAsync
)
```

Exact callback signatures may differ.

Framework APIs must not assume async work completes before the Widget remains mounted.

---

# 21. Async Completion and Lifetime

The key rule is:

```text
async operation lifetime
≠
Widget lifetime automatically
```

If an operation can complete after the owner unmounts, the application/framework code must account for that.

---

# 22. Cancellation

Framework-owned async operations that are tied to a mounted lifetime should use cancellation where practical.

Conceptually:

```text
WidgetNode
↓ owns
CancellationTokenSource
↓ cancelled on unmount
```

Do not introduce this mechanism for components that have no async behavior.

---

# 23. Cancellation Ownership

The creator of a framework-owned cancellation source owns its disposal.

Cancellation should occur before resources referenced by the continuation are destroyed where practical.

---

# 24. Fire-and-Forget

Avoid unmanaged fire-and-forget tasks.

Bad:

```csharp
_ = LoadSomethingAsync();
```

when:

```text
exceptions are unobserved
lifecycle is unowned
completion may mutate dead UI
```

If fire-and-forget behavior is necessary, ownership and error reporting must be explicit.

---

# 25. Framework Async Exceptions

Framework-owned asynchronous exceptions must not disappear silently.

The implementation should either:

```text
propagate through returned Task
observe/report through framework diagnostics
or cancel as part of defined lifecycle
```

depending on the API.

---

# 26. User Async Exceptions

LumaFlow should not globally swallow application exceptions from asynchronous callbacks.

Application errors remain application errors.

---

# 27. No Async Build

`Build(BuildContext context)` is synchronous.

Do not introduce:

```csharp
Task<Widget> BuildAsync(...)
```

as foundational component composition.

Asynchronous data should flow through application state.

Example:

```text
load data asynchronously
↓
update State
↓
UI reacts
```

---

# 28. No Async Mount by Default

Core mount should remain synchronous unless a real native requirement appears.

A mounted tree should either:

```text
mount successfully
or
fail/rollback
```

within the mount operation.

---

# 29. No Per-Frame Polling for Async Completion

Do not implement generic async integration by polling Tasks every frame.

Use normal continuations/state mutation and explicit main-thread scheduling where required.

---

# 30. Main-Thread Verification

Development builds may perform optional main-thread assertions around architecture-critical UI operations.

Examples:

```text
Mount
Unmount
Navigator.Push
Overlay.Show
```

if an efficient reliable Unity main-thread identity mechanism is available.

---

# 31. Release Behavior

Correctness must not depend entirely on development-only thread assertions.

Unsupported cross-thread usage remains unsupported even if release builds omit expensive validation.

---

# 32. Future Dispatcher

A future convenience API may provide explicit main-thread scheduling if real consumer demand exists.

Conceptually:

```csharp
await LumaDispatcher.SwitchToMainThread();
```

or:

```csharp
LumaDispatcher.Post(...);
```

Such an API requires its own semantics around:

```text
lifecycle
ordering
player/editor behavior
domain reload
tests
```

and is not part of initial Core.

---

# 33. Application Dispatcher Interop

Applications may use their own established scheduling systems.

LumaFlow must not require one specific third-party async library.

---

# 34. Third-Party Async Packages

Core must not require:

```text
UniTask
UniRx
R3
custom scheduler package
```

Optional adapters may be added later.

---

# 35. Editor Threading

Editor UI remains main-thread-affine.

EditorWindow integration does not change the Core threading model.

Do not access mounted LumaFlow Editor UI from arbitrary background tasks.

---

# 36. Domain Reload

Async Editor work must not assume mounted UI survives domain reload.

Editor-specific long-running operations require additional lifecycle care in `LumaFlow.Editor` or application code.

---

# 37. Tests

Required threading/lifetime tests should eventually include:

```text
State listener runs synchronously
State equality suppression remains deterministic
listener can unsubscribe safely
UI binding cleanup prevents later updates
async completion after unmount cannot mutate disposed node through framework-owned path
Navigator/Overlay thread guard diagnostics if implemented
```

---

# 38. Documentation Contract

Public APIs involving async work must document:

```text
whether they return Task
who owns cancellation
what unmount does
what thread continuation-based UI mutation requires
```

Do not expose ambiguous asynchronous APIs.

---

# 39. Codex Rules

Codex must:

1. treat WidgetNode and native UI mutation as main-thread-only;
2. not make State<T> implicitly thread-safe;
3. not add locks without explicit concurrency requirements;
4. not add hidden worker→main-thread dispatch;
5. not retain BuildContext casually across `await`;
6. attach framework-owned async work to explicit lifecycle;
7. avoid unobserved fire-and-forget Tasks;
8. preserve synchronous State notification unless the architecture changes;
9. keep Core free from mandatory third-party async libraries;
10. add lifecycle tests for any framework-owned asynchronous feature.

---

# 40. Initial Contract Summary

For the initial implementation:

```text
WidgetNode              main thread only
VisualElement           main thread only
BuildContext            main thread only
MountHandle             main thread only
BindingScope            main thread-affine
Navigator               main thread only
Overlay                 main thread only

State<T>                not thread-safe
State<T> notifications  synchronous
UI-bound State<T>       mutate on main thread

implicit dispatch       none
mandatory async package none
```

---

# 41. Final Principle

LumaFlow does not hide concurrency behind reactive APIs.

The guiding rule is:

**Background work may produce data.  
Mounted UI is mutated explicitly on Unity's main thread.**