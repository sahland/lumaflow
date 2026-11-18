# Tables

LumaFlow follows Flutter's separation of table layout, data presentation and
pagination. These widgets use LumaFlow reconciliation, state and themes; no
additional package or scene component is needed.

## Table: layout

```csharp
new Table(
    new[] {
        new TableRow(new Widget[] {new Text("Name"), new Text("Area")}),
        new TableRow(new Widget[] {new Text("Apartment 123"), new Text("37.11 m²")},
            key: new WidgetKey("apartment-123"))
    },
    columnWidths: new Dictionary<int, TableColumnWidth> {
        [0] = new FlexColumnWidth(2),
        [1] = new FixedColumnWidth(120)
    },
    border: TableBorder.All(Color.gray),
    cellPadding: EdgeInsets.Symmetric(horizontal: 12, vertical: 8));
```

Every row must have the same number of cells. Lists and width dictionaries are
copied at construction. Give reorderable rows unique, stable `WidgetKey`s to
retain compatible input and stateful widgets. Keys identify the data, not its
current row index. Unkeyed rows reconcile by position.

`FixedColumnWidth` sets the cell's outer width in UI units, including padding and
its interior right border. `FlexColumnWidth` distributes remaining space by
weight, after fixed widths, cell padding and borders. The default is flex 1.
Explicit widths keep column boundaries consistent without measuring every cell
for intrinsic width. Widths must be finite and positive.

Rows grow with their contents. `minRowHeight` is a minimum, not a clipping height;
individual `TableRow.minHeight` overrides it. A `TableCell` can override vertical
alignment (`Top`, `Middle`, `Bottom`, `Fill`) or attach a native tooltip. Plain
widgets use the table's `defaultVerticalAlignment`.

`TableBorder` configures the outside, horizontal-inside and vertical-inside
strokes independently. There are no doubled inner border strokes.

For horizontal scrolling, give the table a finite width and wrap it with the
existing `ScrollView` in horizontal mode. Flex columns need available width;
they are not intrinsic-size columns. A table does not install its own scroll
container or pin its header.

## DataTable: data and actions

`DataColumn` describes the label, optional tooltip, numeric alignment, width and
sort callback. `DataCell` holds any widget and an optional tap action. `DataRow`
holds cells, a stable key, a selected flag, an optional selection callback and
an optional background color.

```csharp
new DataTable(
    columns: new[] {
        new DataColumn(new Text("Name"), onSort: (column, ascending) => {
            // Sort your collection and rebuild with the new sort metadata.
        }),
        new DataColumn(new Text("Area"), numeric: true,
            columnWidth: new FixedColumnWidth(120))
    },
    rows: new[] {
        new DataRow(new[] {
            new DataCell(new Text("Apartment 123")),
            new DataCell(new Text("37.11 m²"))
        }, key: new WidgetKey("123"), selected: isSelected,
            onSelectChanged: value => {
                // Update application state and rebuild the row description.
            })
    },
    sortColumnIndex: 0,
    sortAscending: true);
```

As in Flutter, the widget does not sort or mutate your collection. Clicking the
currently sorted heading requests the opposite direction; clicking a different
heading requests ascending order. Update your data and rebuild with the new
`sortColumnIndex` and `sortAscending`. Sortable headings and tappable cells are
keyboard-activatable `Pressable`s.

A selection column appears only when `showCheckboxColumn` is true and at least
one visible row has `onSelectChanged`. Rows without this callback have disabled
selection controls. Rebuild after selection callbacks to publish the new
`DataRow.selected` values and row colors. The header checkbox is checked when
all selectable visible rows are selected; mixed selection is shown unchecked.
It selects all on the next click. `onSelectAll` overrides default dispatch;
otherwise each changed selectable row receives its callback. Cell tap actions
are independent of selection.

Set `ThemeData.dataTableTheme` to a `DataTableTheme` for tree-scoped defaults.
`DataTableStyle` on a widget overrides individual values. Missing values inherit
the component theme and then the semantic color scheme. The table does not
hardcode application branding. Arbitrary cell widgets keep their own typography
and inherited context.

## PaginatedDataTable: bounded reads

Keep one `DataTableSource` for the lifetime of the dataset. Implement `RowCount`
and `GetRow(index)`. Return null for an unavailable row to render placeholder
cells, and call `NotifyListeners()` on Unity's main thread when data becomes
available or selection changes. The source owns loading, errors, filtering,
sorting and any network requests. It must not notify listeners while `GetRow`
is being read.

```csharp
private readonly State<int> firstRow = new(0);

// Inside Build; source and columns are application-owned.
new PaginatedDataTable(columns, source, firstRow,
    rowsPerPage: 10,
    onPageChanged: first => Debug.Log($"Page starts at {first}"));
```

The widget reads at most `rowsPerPage` rows per rebuild, only from the current
page. It does not fetch all rows to count or select them. Selection in the
header applies to the current page; implement `onSelectAll` when the application
needs dataset-wide selection. `DataTableSource.Subscribe` returns a disposable
subscription. The widget subscribes while mounted, switches subscriptions when
source or controlled page changes, and releases them on unmount. It never owns
or disposes the borrowed source.

Previous/next actions commit `firstRow` and then invoke `onPageChanged`, following
the existing controlled-input contract. Programmatic state updates, source
notifications, rebuilds and data shrinking do not invoke that callback. A
negative, out-of-range or non-page-aligned index is normalized for display
without mutating borrowed state. After shrinking, the last valid page is shown;
an empty source reads no rows and shows `0–0 / 0`. `RowCount` cannot be negative.

Pass `DataTableLabels` to translate previous/next captions and the page-range
formatter, and `header` to supply arbitrary content above the table. To change
page size, rebuild with a different positive `rowsPerPage`.

## Scope and performance

`DataTable` combines cell sizing, padding and alignment into one native host.
Unchanged immutable `DataRow` instances bypass row reconciliation when effective
column layout and row styling are unchanged. Keep row instances for unchanged
data and create a new `DataRow` for changed selection, callbacks or cell content.
The cache belongs to the mounted table, not a global registry; changes to column
widths, numeric alignment, padding, row height and colors invalidate it. Controlled
checkbox state is resynchronized on parent rebuilds. Interactive cells retain
their pressable/keyboard activation boundary. Pagination uses this same renderer.

`Table` and `DataTable` mount every supplied row. They are for bounded datasets;
use `PaginatedDataTable` for a larger dataset. Pagination limits mounted rows
and source reads but is not two-dimensional virtualization. Intrinsic column
sizing, cell merging, pinned headers/columns, resizing/reordering columns by
dragging, editable spreadsheet behavior and Flutter's complete Material API are
not implemented. Normal input widgets can be placed in cells today.

The `Tables` Package Manager sample includes a working sortable, selectable,
paginated table. Runtime code compiles against Unity 6.2 and 6.4.

References: [Flutter Table](https://api.flutter.dev/flutter/widgets/Table-class.html),
[DataTable](https://api.flutter.dev/flutter/material/DataTable-class.html),
[PaginatedDataTable](https://api.flutter.dev/flutter/material/PaginatedDataTable-class.html).
