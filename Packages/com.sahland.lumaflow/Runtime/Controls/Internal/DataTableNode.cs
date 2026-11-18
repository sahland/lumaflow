#nullable enable

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumaFlow {
    internal sealed class DataTableBody : Widget {
        internal readonly DataTable Table;
        internal readonly ThemeData? Theme;
        internal readonly Color Background, HeadingBackground, SelectedBackground, Divider;
        internal readonly EdgeInsets Padding;
        internal readonly float RowHeight, HeadingHeight, DividerThickness;
        internal readonly bool Checkbox, AllSelected;
        internal DataTableBody(DataTable table, ThemeData? theme) {
            Table = table; Theme = theme;
            var style = table.Style;
            var themed = theme?.DataTableTheme.Style;
            Background = style?.Background ?? themed?.Background ?? theme?.Colors.Surface ?? Color.white;
            HeadingBackground = style?.HeadingBackground ?? themed?.HeadingBackground ?? theme?.Colors.SurfaceVariant ?? new Color(.94f, .95f, .97f);
            SelectedBackground = style?.SelectedBackground ?? themed?.SelectedBackground ?? theme?.Colors.PrimaryContainer ?? new Color(.85f, .9f, 1f);
            Divider = style?.DividerColor ?? themed?.DividerColor ?? theme?.Colors.Outline ?? new Color(.8f, .82f, .85f);
            Padding = style?.CellPadding ?? themed?.CellPadding ?? EdgeInsets.Symmetric(12, 8);
            RowHeight = style?.DataRowMinHeight ?? themed?.DataRowMinHeight ?? 44;
            HeadingHeight = style?.HeadingRowHeight ?? themed?.HeadingRowHeight ?? 48;
            DividerThickness = style?.DividerThickness ?? themed?.DividerThickness ?? 1;
            var selectable = 0; var selected = 0;
            foreach (var row in table.Rows) if (row.OnSelectChanged is not null) { selectable++; if (row.Selected) selected++; }
            Checkbox = table.ShowCheckboxColumn && selectable > 0;
            AllSelected = selectable > 0 && selected == selectable;
        }
        internal bool SameRowAppearance(DataTableBody other) {
            if (Checkbox != other.Checkbox || !Background.Equals(other.Background)
                || !SelectedBackground.Equals(other.SelectedBackground) || !Divider.Equals(other.Divider)
                || !Padding.Equals(other.Padding) || RowHeight != other.RowHeight || DividerThickness != other.DividerThickness
                || Table.Columns.Count != other.Table.Columns.Count) return false;
            for (var i = 0; i < Table.Columns.Count; i++) {
                var a = Table.Columns[i]; var b = other.Table.Columns[i];
                if (a.Numeric != b.Numeric || a.ColumnWidth.GetType() != b.ColumnWidth.GetType()
                    || a.ColumnWidth.Value != b.ColumnWidth.Value) return false;
            }
            return true;
        }
        internal override WidgetNode CreateNode() => new DataTableBodyNode(this);
    }

    internal sealed class DataTableBodyNode : WidgetNode {
        internal DataTableBodyNode(DataTableBody widget) : base(widget) { }
        protected override VisualElement CreateElement(BuildContext context) {
            var element = new VisualElement {name = "lumaflow-table"};
            element.style.flexShrink = 0;
            return element;
        }
        protected override void OnMounted() => Apply((DataTableBody)Widget, false);
        internal override bool TryUpdate(Widget nextWidget) {
            if (nextWidget is not DataTableBody next || !CanUpdateWith(next)) return false;
            Apply(next, next.SameRowAppearance((DataTableBody)Widget));
            UpdateWidget(next);
            return true;
        }
        private void Apply(DataTableBody body, bool sameAppearance) {
            var rows = new Widget[body.Table.Rows.Count + 1];
            rows[0] = new DataTableRowLayout(body, null, rows.Length == 1, false);
            for (var i = 1; i < rows.Length; i++)
                rows[i] = new DataTableRowLayout(body, body.Table.Rows[i - 1], i == rows.Length - 1, sameAppearance);
            ReconcileChildren(rows, Element);
        }
    }

    internal sealed class DataTableRowLayout : Widget {
        internal readonly DataTableBody Body;
        internal readonly DataRow? Row;
        internal readonly bool Last, SameAppearance;
        internal DataTableRowLayout(DataTableBody body, DataRow? row, bool last, bool sameAppearance)
            : base(row?.Key is { } key ? new WidgetKey("data:" + key.Value) : null) {
            Body = body; Row = row; Last = last; SameAppearance = sameAppearance;
        }
        internal override WidgetNode CreateNode() => new DataTableRowNode(this);
    }

    internal sealed class DataTableRowNode : WidgetNode {
        private State<bool>? _selectionState;
        internal DataTableRowNode(DataTableRowLayout widget) : base(widget) { }
        protected override VisualElement CreateElement(BuildContext context) {
            var element = new VisualElement {name = "lumaflow-table-row"};
            element.style.flexDirection = FlexDirection.Row;
            element.style.alignItems = UnityEngine.UIElements.Align.Stretch;
            element.style.flexShrink = 0;
            return element;
        }
        protected override void OnMounted() => Apply((DataTableRowLayout)Widget);
        internal override bool TryUpdate(Widget nextWidget) {
            if (nextWidget is not DataTableRowLayout next || !CanUpdateWith(next)) return false;
            var previous = (DataTableRowLayout)Widget;
            if (next.Row is null || !ReferenceEquals(previous.Row, next.Row) || !next.SameAppearance || previous.Last != next.Last
                || (next.Body.Checkbox && _selectionState?.Value != next.Row.Selected)) Apply(next);
            UpdateWidget(next);
            return true;
        }
        private void Apply(DataTableRowLayout layout) {
            var body = layout.Body; var row = layout.Row; var table = body.Table;
            Element.style.backgroundColor = row is null ? body.HeadingBackground : row.Color ?? (row.Selected ? body.SelectedBackground : body.Background);
            Element.style.minHeight = row is null ? body.HeadingHeight : body.RowHeight;
            Element.style.borderBottomWidth = layout.Last ? 0 : body.DividerThickness;
            Element.style.borderBottomColor = body.Divider;
            var cells = new Widget[table.Columns.Count + (body.Checkbox ? 1 : 0)];
            _selectionState = null;
            if (body.Checkbox) {
                var selected = row?.Selected ?? body.AllSelected;
                _selectionState = new State<bool>(selected);
                var toggle = new Checkbox(_selectionState, enabled: row is null || row.OnSelectChanged is not null,
                    onChanged: value => { if (row is null) table.SelectAll(value); else row.OnSelectChanged?.Invoke(value); });
                cells[0] = new DataTableCellLayout(toggle, new FixedColumnWidth(48), body.Padding, null, false, true);
            }
            for (var i = 0; i < table.Columns.Count; i++) {
                var column = table.Columns[i];
                Widget content;
                if (row is null) {
                    var index = i;
                    content = column.Label;
                    if (table.SortColumnIndex == i) content = new Row(new Widget[] {
                        new Expanded(content), new Text(table.SortAscending ? "↑" : "↓", body.Theme?.Typography.Label)
                    }, gap: 4, crossAxisAlignment: CrossAxisAlignment.Center);
                    if (column.OnSort is not null) content = new Pressable(new Align(content, column.Numeric ? Alignment.CenterRight : Alignment.CenterLeft),
                        () => column.OnSort(index, table.SortColumnIndex == index ? !table.SortAscending : true));
                } else {
                    var cell = row.Cells[i]; content = cell.Child;
                    if (cell.OnTap is not null) content = new Pressable(new Align(content, column.Numeric ? Alignment.CenterRight : Alignment.CenterLeft), cell.OnTap);
                }
                cells[i + (body.Checkbox ? 1 : 0)] = new DataTableCellLayout(content, column.ColumnWidth,
                    body.Padding, row is null ? column.Tooltip : null, column.Numeric, content is Pressable || (row is null && table.SortColumnIndex == i));
            }
            ReconcileChildren(cells, Element);
        }
    }

    internal sealed class DataTableCellLayout : Widget {
        internal readonly Widget Child;
        internal readonly TableColumnWidth Width;
        internal readonly EdgeInsets Padding;
        internal readonly string? Tooltip;
        internal readonly bool Numeric, Stretch;
        internal DataTableCellLayout(Widget child, TableColumnWidth width, EdgeInsets padding, string? tooltip, bool numeric, bool stretch) {
            Child = child; Width = width; Padding = padding; Tooltip = tooltip; Numeric = numeric; Stretch = stretch;
        }
        internal override WidgetNode CreateNode() => new DataTableCellNode(this);
    }

    internal sealed class DataTableCellNode : SingleChildWidgetNode<DataTableCellLayout> {
        internal DataTableCellNode(DataTableCellLayout widget) : base(widget) { }
        internal override bool TryUpdate(Widget nextWidget) {
            if (nextWidget is DataTableCellLayout next && CanUpdateWith(next)) {
                var previous = CurrentWidget;
                if (ReferenceEquals(previous.Child, next.Child) && previous.Width.GetType() == next.Width.GetType()
                    && previous.Width.Value == next.Width.Value && previous.Padding.Equals(next.Padding)
                    && previous.Tooltip == next.Tooltip && previous.Numeric == next.Numeric && previous.Stretch == next.Stretch) {
                    UpdateWidget(next);
                    return true;
                }
            }
            return base.TryUpdate(nextWidget);
        }
        protected override VisualElement CreateElement(BuildContext context) {
            var element = new VisualElement {name = "lumaflow-table-slot"};
            element.style.minWidth = 0;
            element.style.flexShrink = 0;
            element.style.justifyContent = Justify.Center;
            return element;
        }
        protected override void OnMounted() { base.OnMounted(); ApplyConfiguration(CurrentWidget); }
        protected override Widget GetChild(DataTableCellLayout widget) => widget.Child;
        protected override void ApplyConfiguration(DataTableCellLayout widget) {
            Element.style.flexGrow = widget.Width is FlexColumnWidth ? widget.Width.Value : 0;
            Element.style.flexBasis = widget.Width is FixedColumnWidth ? widget.Width.Value : 0;
            Element.style.width = widget.Width is FixedColumnWidth ? new StyleLength(widget.Width.Value) : StyleKeyword.Auto;
            Element.style.alignItems = widget.Stretch ? UnityEngine.UIElements.Align.Stretch
                : widget.Numeric ? UnityEngine.UIElements.Align.FlexEnd : UnityEngine.UIElements.Align.FlexStart;
            Element.tooltip = widget.Tooltip ?? string.Empty;
            PaddingStyleMapper.Apply(Element, widget.Padding);
        }
    }
}
