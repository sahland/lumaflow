# Lists and virtualization

`ListView<T>` maps directly to UI Toolkit's native virtualized `ListView`. It
creates widget nodes only for native row hosts in the visible/recycle range; it
never expands a large source into thousands of ordinary mounted children.

## Basic list

```csharp
new ListView<Project>(
    projects,
    project => new ProjectTile(project),
    itemHeight: 56f);
```

Use a fixed `itemHeight` when rows have a common extent. Omitting it selects
native dynamic-height virtualization. The source is snapshotted when a widget
configuration is created. To replace the complete source reactively, pass a
`State<IReadOnlyList<T>>`. Mutating a collection in place does not notify the
list.

## Stable identity

Any list whose rows contain local state, focus, subscriptions, or other retained
resources should provide `itemKey`:

```csharp
var projects = new State<IReadOnlyList<Project>>(initialProjects);

new ListView<Project>(
    projects,
    (project, index) => new ProjectTile(project, index),
    itemHeight: 56f,
    itemKey: project => new WidgetKey(project.Id));
```

Keys must be valid and unique in every snapshot. A keyed, compatible row that
is still represented by a native realized/recycle host follows its item through
reordering and insertion. A row that leaves the native recycle window is
unmounted normally. This bounded ownership is intentional: LumaFlow does not
keep one hidden node per source item.

Key selectors must be deterministic for a source snapshot. Changing the
selector delegate is allowed; changing the keys it returns establishes new row
identity and therefore remounts unmatched rows.

## Controlled selection

Selection is key-based and externally owned. It cannot be enabled with indices
or without stable item keys:

```csharp
IReadOnlyList<WidgetKey> initialSelection = Array.Empty<WidgetKey>();
var selectedProjects = new State<IReadOnlyList<WidgetKey>>(initialSelection);

new ListView<Project>(
    projects,
    project => new ProjectTile(project),
    itemKey: project => new WidgetKey(project.Id),
    selectionMode: ListSelectionMode.Multiple,
    selectedKeys: selectedProjects,
    onSelectionChanged: keys => SaveSelection(keys));
```

`ListSelectionMode` is `None`, `Single`, or `Multiple`. A user-originated native
change commits `selectedKeys` before invoking `onSelectionChanged`, matching the
other controlled LumaFlow inputs. Programmatic changes synchronize native
selection without calling the callback.

Collection mutations map the same selected keys to their new indices. Missing
keys remain in the external state but are not selected natively; if an item with
that key returns later, its selection is restored. Selected-key values must be
valid and unique, and `Single` accepts at most one key.

## Scroll restoration

Retain a `ListViewController` above transient widget configurations or mounts:

```csharp
var listController = new ListViewController(initialOffset: 120f);

new ListView<Project>(
    projects,
    project => new ProjectTile(project),
    itemKey: project => new WidgetKey(project.Id),
    controller: listController);

listController.JumpTo(320f);
listController.ScrollTo(new WidgetKey(projectId));
```

`Offset` is read-only to consumers and is updated by native scrolling.
`JumpTo` changes it after validating a finite, non-negative value. Retaining the
controller across remounts restores that value when native geometry is ready.
`ScrollTo` returns `false` while detached or when the key is absent. One
controller can be attached to only one mounted list at a time.

## Recycling and cleanup contract

- The native list owns row hosts; `ListViewNode<T>` owns the widget node mounted
  in each host.
- Ordinary recycling to a different unkeyed index unmounts the previous row
  before mounting the next one.
- During keyed collection refresh, realized nodes may move between existing
  native hosts. Compatible nodes update in place; unmatched nodes are released
  after the native rebind pass.
- `unbindItem`, `destroyItem`, source replacement, incompatible configuration,
  and list unmount release row bindings deterministically.
- Item builders may run repeatedly. They must describe UI and must not use the
  number of invocations as application state.

## Performance validation

The performance suite records mount samples for 100, 1,000 and 10,000 items and
a 24-host recycling workload against a 10,000-item source. It verifies that
mounted and build counts stay below source size and that only the explicit
recycle window is materialized. Time and allocation budgets must be established
for each target platform.

Incremental loading, asynchronous paging, animated item insertion/removal and
advanced dynamic-height correction are not part of the current API. Extensions
should continue to use native virtualization rather than introduce an
unbounded node cache.
