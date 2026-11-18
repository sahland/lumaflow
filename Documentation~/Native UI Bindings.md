# Native UI access and bindings

Widgets remain declarative descriptions; queries return real Unity UI Toolkit
elements. Add a public ID before layout wrappers:

```csharp
using Framework = global::LumaFlow.LumaFlow;

Widget content = new global::LumaFlow.Button("Parking", OpenParking).WithId("parking");
var mount = Framework.Mount(content, document.rootVisualElement);
var view = new NativeView(document.rootVisualElement);
var button = view.Require<UnityEngine.UIElements.Button>("parking");
```

`document.rootVisualElement.Q<UnityEngine.UIElements.Button>("parking")` also
works. Standard UI Toolkit Q keeps its own semantics and is a one-time lookup;
it does not automatically renew a clicked subscription after a control is replaced.
Never query a declarative AppButton type: it is not a VisualElement.

## Lifecycle-safe subscriptions

Value bindings observe events originating from the queried control itself.
Bubbled events from child controls or labels are ignored; updating a caption
must not be interpreted as a user edit of the containing field.

```csharp
IDisposable click = view.OnClick("parking", OnParkingClicked);
IDisposable change = view.OnValueChanged<string>("library-path", OnPathChanged);

IDisposable custom = view.Bind<MyTreeView>("library-tree",
    tree => tree.selectedIndicesChanged += OnSelectionChanged,
    tree => tree.selectedIndicesChanged -= OnSelectionChanged);
```

Bind and the convenience methods can be registered before an element exists.
They detach from removed/replaced elements and attach to the new matching one.
An unchanged native instance is not subscribed twice. Dispose each token with
its controller; disposing the view disposes all its bindings but not the borrowed
root or widgets. Dispose the view before disposing the mount that owns the UI.
The Disposed event runs after binding cleanup, including when a cleanup fails;
controllers can use it to release their non-UI subscriptions as well.
Do not treat an ID as a reconciliation key: WithId preserves an existing key,
and WithKey still controls keyed state identity independently.

Refreshes are coalesced onto the root's UI Toolkit scheduler after identified
subtrees change. There is no per-frame polling. For detached trees in tests,
or native children added manually without WithId, call `view.Refresh()` after
the mutation finishes. Bind/Unbind must not call Refresh recursively. Unbind
also runs when Bind throws midway; write cleanup that tolerates partial setup.
Binding failures remain visible rather than being swallowed. All native queries,
binding operations and UI mutations belong on Unity's main thread.

## Lookup and IDs

`view.Q<T>` returns null for missing elements or a different native type.
`view.Require<T>` throws an explanatory error for either case. Both reject
duplicate IDs within the view root. IDs contain 1–128 letters/digits and
`_ - . : /`; matching is case-sensitive. Do not overwrite element.name manually
after assigning an ID.

For repeated component instances, use unique IDs or a NativeView rooted at each
instance container. That narrower view must be disposed/recreated if its own
container is replaced. A single native root cannot own two public IDs; wrap a
composite in a separate Container when it needs an additional scope ID.

For a custom StatelessWidget/StatefulWidget, WithId names its actual native root.
If that root is a Container, query VisualElement; if it is a Button, query the
native Button. Native-backed custom classes are supported directly:

```csharp
Widget tree = new Native(() => new MyTreeView()).WithId("library-tree");
```

For an existing Initialize(VisualElement root) controller, bind the screen root
and clean up its subscriptions when that screen disappears:

```csharp
var binding = view.Bind<VisualElement>("library-screen",
    root => controller.Initialize(root),
    root => controller.Unbind());
```

## State ownership

Native references and bindings are not a data store. Keep application state in
State/ViewModel/store. Mounted compatible keyed widgets retain their local
state; removing a widget ends that local lifetime. Restoring a screen from
long-lived application state and saving data to disk are separate responsibilities.

Setting a native value emits normal native events and can update an existing
controlled State binding. SetValueWithoutNotify changes only the displayed native
value; it does not update State and emits no change callback. Declarative rebuilds
may overwrite direct native text/value/style edits. Avoid a second independent
source of truth. Added clicked handlers supplement existing declarative actions;
they do not replace or suppress them automatically.
