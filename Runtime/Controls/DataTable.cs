#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

namespace LumaFlow {
    public sealed class DataColumn {
        public DataColumn(Widget label, bool numeric = false, Action<int, bool>? onSort = null,
            TableColumnWidth? columnWidth = null, string? tooltip = null) {
            Label = label ?? throw new ArgumentNullException(nameof(label));
            Numeric = numeric;
            OnSort = onSort;
            ColumnWidth = columnWidth ?? new FlexColumnWidth();
            Tooltip = tooltip;
        }
        public Widget Label { get; }
        public bool Numeric { get; }
        public Action<int, bool>? OnSort { get; }
        public TableColumnWidth ColumnWidth { get; }
        public string? Tooltip { get; }
    }

    public sealed class DataCell {
        public DataCell(Widget child, Action? onTap = null) {
            Child = child ?? throw new ArgumentNullException(nameof(child));
            OnTap = onTap;
        }
        public Widget Child { get; }
        public Action? OnTap { get; }
    }

    public sealed class DataRow {
        public DataRow(IReadOnlyList<DataCell> cells, WidgetKey? key = null, bool selected = false,
            Action<bool>? onSelectChanged = null, Color? color = null) {
            Cells = TableValidation.Copy(cells, nameof(cells));
            if (key is { IsValid: false }) throw new ArgumentException("A row key must be valid.", nameof(key));
            Key = key;
            Selected = selected;
            OnSelectChanged = onSelectChanged;
            Color = color;
        }
        public IReadOnlyList<DataCell> Cells { get; }
        public WidgetKey? Key { get; }
        public bool Selected { get; }
        public Action<bool>? OnSelectChanged { get; }
        public Color? Color { get; }
    }

    public sealed class DataTableStyle {
        public DataTableStyle(Color? background = null, Color? headingBackground = null,
            Color? selectedBackground = null, Color? dividerColor = null,
            float? headingRowHeight = null, float? dataRowMinHeight = null,
            EdgeInsets? cellPadding = null, float? dividerThickness = null) {
            if (headingRowHeight is { } heading) TableValidation.NonNegative(heading, nameof(headingRowHeight));
            if (dataRowMinHeight is { } height) TableValidation.NonNegative(height, nameof(dataRowMinHeight));
            if (dividerThickness is { } thickness) TableValidation.NonNegative(thickness, nameof(dividerThickness));
            Background = background;
            HeadingBackground = headingBackground;
            SelectedBackground = selectedBackground;
            DividerColor = dividerColor;
            HeadingRowHeight = headingRowHeight;
            DataRowMinHeight = dataRowMinHeight;
            CellPadding = cellPadding;
            DividerThickness = dividerThickness;
        }
        public Color? Background { get; }
        public Color? HeadingBackground { get; }
        public Color? SelectedBackground { get; }
        public Color? DividerColor { get; }
        public float? HeadingRowHeight { get; }
        public float? DataRowMinHeight { get; }
        public EdgeInsets? CellPadding { get; }
        public float? DividerThickness { get; }
    }

    public sealed class DataTableTheme {
        public DataTableTheme(DataTableStyle style) => Style = style ?? throw new ArgumentNullException(nameof(style));
        public DataTableStyle Style { get; }
    }

    public sealed class DataTable : StatelessWidget {
        public DataTable(IReadOnlyList<DataColumn> columns, IReadOnlyList<DataRow> rows,
            int? sortColumnIndex = null, bool sortAscending = true,
            bool showCheckboxColumn = true, Action<bool>? onSelectAll = null,
            DataTableStyle? style = null) {
            Columns = TableValidation.Copy(columns, nameof(columns));
            if (Columns.Count == 0) throw new ArgumentException("At least one column is required.", nameof(columns));
            Rows = TableValidation.Copy(rows, nameof(rows));
            if (sortColumnIndex is { } index && (index < 0 || index >= Columns.Count)) throw new ArgumentOutOfRangeException(nameof(sortColumnIndex));
            var keys = new HashSet<WidgetKey>();
            foreach (var row in Rows) {
                if (row.Cells.Count != Columns.Count) throw new ArgumentException("Every data row must match the column count.", nameof(rows));
                if (row.Key is { } key && !keys.Add(key)) throw new ArgumentException("Data row keys must be unique.", nameof(rows));
            }
            SortColumnIndex = sortColumnIndex;
            SortAscending = sortAscending;
            ShowCheckboxColumn = showCheckboxColumn;
            OnSelectAll = onSelectAll;
            Style = style;
        }
        public IReadOnlyList<DataColumn> Columns { get; }
        public IReadOnlyList<DataRow> Rows { get; }
        public int? SortColumnIndex { get; }
        public bool SortAscending { get; }
        public bool ShowCheckboxColumn { get; }
        public Action<bool>? OnSelectAll { get; }
        public DataTableStyle? Style { get; }

        public override Widget Build(BuildContext context) {
            return new DataTableBody(this, context.Theme);
        }

        internal void SelectAll(bool selected) {
            if (OnSelectAll is not null) { OnSelectAll(selected); return; }
            var callbacks = new List<Action>();
            foreach (var row in Rows) if (row.OnSelectChanged is not null && row.Selected != selected)
                callbacks.Add(() => row.OnSelectChanged(selected));
            List<Exception>? failures = null;
            foreach (var callback in callbacks) try { callback(); }
                catch (Exception exception) { (failures ??= new List<Exception>()).Add(exception); }
            if (failures is { Count: > 0 }) throw new AggregateException("Table selection callbacks failed.", failures);
        }
    }
}
