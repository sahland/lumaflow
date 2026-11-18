#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine.UIElements;

namespace LumaFlow {
    public abstract class DataTableSource {
        private readonly State<int> _revision = new(0);
        public abstract int RowCount { get; }
        public abstract DataRow? GetRow(int index);
        public void NotifyListeners() => _revision.Value = unchecked(_revision.Value + 1);
        public IDisposable Subscribe(Action listener) {
            if (listener is null) throw new ArgumentNullException(nameof(listener));
            return _revision.Subscribe(_ => listener());
        }
    }

    public sealed class DataTableLabels {
        public DataTableLabels(string previousPage = "Previous page", string nextPage = "Next page",
            Func<int, int, int, string>? pageRange = null) {
            PreviousPage = previousPage ?? throw new ArgumentNullException(nameof(previousPage));
            NextPage = nextPage ?? throw new ArgumentNullException(nameof(nextPage));
            PageRange = pageRange ?? ((first, last, count) => string.Format(CultureInfo.InvariantCulture, "{0}–{1} / {2}", first, last, count));
        }
        public string PreviousPage { get; }
        public string NextPage { get; }
        public Func<int, int, int, string> PageRange { get; }
    }

    public sealed class PaginatedDataTable : Widget {
        public PaginatedDataTable(IReadOnlyList<DataColumn> columns, DataTableSource source,
            State<int> firstRowIndex, int rowsPerPage = 10,
            int? sortColumnIndex = null, bool sortAscending = true,
            bool showCheckboxColumn = true, Action<bool>? onSelectAll = null,
            Action<int>? onPageChanged = null, DataTableStyle? style = null,
            Widget? header = null, DataTableLabels? labels = null) {
            Columns = TableValidation.Copy(columns, nameof(columns));
            if (Columns.Count == 0) throw new ArgumentException("At least one column is required.", nameof(columns));
            Source = source ?? throw new ArgumentNullException(nameof(source));
            FirstRowIndex = firstRowIndex ?? throw new ArgumentNullException(nameof(firstRowIndex));
            if (rowsPerPage <= 0) throw new ArgumentOutOfRangeException(nameof(rowsPerPage));
            if (sortColumnIndex is { } index && (index < 0 || index >= Columns.Count)) throw new ArgumentOutOfRangeException(nameof(sortColumnIndex));
            RowsPerPage = rowsPerPage;
            SortColumnIndex = sortColumnIndex;
            SortAscending = sortAscending;
            ShowCheckboxColumn = showCheckboxColumn;
            OnSelectAll = onSelectAll;
            OnPageChanged = onPageChanged;
            Style = style;
            Header = header;
            Labels = labels ?? new DataTableLabels();
        }
        public IReadOnlyList<DataColumn> Columns { get; }
        public DataTableSource Source { get; }
        public State<int> FirstRowIndex { get; }
        public int RowsPerPage { get; }
        public int? SortColumnIndex { get; }
        public bool SortAscending { get; }
        public bool ShowCheckboxColumn { get; }
        public Action<bool>? OnSelectAll { get; }
        public Action<int>? OnPageChanged { get; }
        public DataTableStyle? Style { get; }
        public Widget? Header { get; }
        public DataTableLabels Labels { get; }
        internal override WidgetNode CreateNode() => new PaginatedDataTableNode(this);
    }

    internal sealed class PaginatedDataTableNode : WidgetNode {
        private IDisposable? _sourceSubscription;
        private IDisposable? _pageSubscription;
        internal PaginatedDataTableNode(PaginatedDataTable widget) : base(widget) { }
        protected override VisualElement CreateElement(BuildContext context) => new VisualElement { name = "lumaflow-paginated-table" };
        protected override void OnMounted() {
            Bindings.Add(Release);
            Rebuild();
            Subscribe();
        }
        internal override bool TryUpdate(Widget nextWidget) {
            if (nextWidget is not PaginatedDataTable table || !CanUpdateWith(table)) return false;
            Release();
            UpdateWidget(table);
            Rebuild();
            Subscribe();
            return true;
        }
        protected override void OnInheritedChanged(InheritedAspect aspect) => Rebuild();
        private void Subscribe() {
            var table = (PaginatedDataTable)Widget;
            _sourceSubscription = table.Source.Subscribe(Rebuild);
            _pageSubscription = table.FirstRowIndex.Subscribe(_ => Rebuild());
        }
        private void Release() {
            _sourceSubscription?.Dispose(); _sourceSubscription = null;
            _pageSubscription?.Dispose(); _pageSubscription = null;
        }
        private static int Start(PaginatedDataTable table, int count) {
            if (count == 0) return 0;
            var last = (count - 1) / table.RowsPerPage * table.RowsPerPage;
            return Math.Min(Math.Max(0, table.FirstRowIndex.Value) / table.RowsPerPage * table.RowsPerPage, last);
        }
        private void Rebuild() {
            var table = (PaginatedDataTable)Widget;
            var count = table.Source.RowCount;
            if (count < 0) throw new InvalidOperationException("DataTableSource.RowCount cannot be negative.");
            var first = Start(table, count);
            var length = Math.Min(table.RowsPerPage, count - first);
            var rows = new DataRow[length];
            for (var i = 0; i < length; i++) {
                var row = table.Source.GetRow(first + i);
                if (row is null) {
                    var cells = new DataCell[table.Columns.Count];
                    for (var j = 0; j < cells.Length; j++) cells[j] = new DataCell(new SizedBox(new Text(string.Empty), height: 20));
                    row = new DataRow(cells);
                }
                rows[i] = row;
            }
            var children = new List<Widget>();
            if (table.Header is not null) children.Add(table.Header.WithKey(new WidgetKey("header")));
            children.Add(new DataTable(table.Columns, rows, table.SortColumnIndex, table.SortAscending,
                table.ShowCheckboxColumn, table.OnSelectAll, table.Style).WithKey(new WidgetKey("table")));
            children.Add(new Row(new Widget[] {
                new Expanded(new Text(table.Labels.PageRange(count == 0 ? 0 : first + 1, first + length, count), Context.Theme?.Typography.Label)),
                new Button(table.Labels.PreviousPage, () => ChangePage(false), ButtonVariant.Secondary, enabled: first > 0),
                new Button(table.Labels.NextPage, () => ChangePage(true), ButtonVariant.Secondary, enabled: length < count - first)
            }, gap: 8, crossAxisAlignment: CrossAxisAlignment.Center).WithKey(new WidgetKey("footer")));
            ReconcileChildren(children, Element);
        }
        private void ChangePage(bool next) {
            if (!IsMounted) return;
            var table = (PaginatedDataTable)Widget;
            var count = table.Source.RowCount;
            if (count < 0) throw new InvalidOperationException("DataTableSource.RowCount cannot be negative.");
            var first = Start(table, count);
            var target = next ? Math.Min((long)first + table.RowsPerPage, count == 0 ? 0 : (long)(count - 1) / table.RowsPerPage * table.RowsPerPage)
                : Math.Max(0L, (long)first - table.RowsPerPage);
            if (target == first) return;
            ControlledInputChange.Commit(table.FirstRowIndex, (int)target, table.OnPageChanged);
        }
    }
}
