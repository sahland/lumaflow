#nullable enable

using UnityEngine.UIElements;

namespace LumaFlow {
    internal sealed class TableNode : WidgetNode {
        internal TableNode(Table table) : base(table) { }
        protected override VisualElement CreateElement(BuildContext context) {
            var element = new VisualElement { name = "lumaflow-table" };
            element.style.flexShrink = 0;
            return element;
        }
        protected override void OnMounted() => Apply((Table)Widget);
        internal override bool TryUpdate(Widget nextWidget) {
            if (nextWidget is not Table table || !CanUpdateWith(table)) return false;
            Apply(table);
            UpdateWidget(table);
            return true;
        }
        private void Apply(Table table) {
            var side = table.Border.Outside;
            Element.style.borderLeftWidth = Element.style.borderRightWidth = Element.style.borderTopWidth = Element.style.borderBottomWidth = side.Width;
            Element.style.borderLeftColor = Element.style.borderRightColor = Element.style.borderTopColor = Element.style.borderBottomColor = side.Color;
            var rows = new Widget[table.Children.Count];
            for (var i = 0; i < rows.Length; i++) {
                Widget row = new TableRowLayout(table, table.Children[i], i == rows.Length - 1);
                if (table.Children[i].Key is { } key) row = row.WithKey(key);
                rows[i] = row;
            }
            ReconcileChildren(rows, Element);
        }
    }

    internal sealed class TableRowLayout : Widget {
        internal TableRowLayout(Table table, TableRow row, bool last) { Table = table; Row = row; Last = last; }
        internal Table Table { get; }
        internal TableRow Row { get; }
        internal bool Last { get; }
        internal override WidgetNode CreateNode() => new TableRowNode(this);
    }

    internal sealed class TableRowNode : WidgetNode {
        internal TableRowNode(TableRowLayout widget) : base(widget) { }
        protected override VisualElement CreateElement(BuildContext context) {
            var element = new VisualElement { name = "lumaflow-table-row" };
            element.style.flexDirection = FlexDirection.Row;
            element.style.alignItems = UnityEngine.UIElements.Align.Stretch;
            element.style.flexShrink = 0;
            return element;
        }
        protected override void OnMounted() => Apply((TableRowLayout)Widget);
        internal override bool TryUpdate(Widget nextWidget) {
            if (nextWidget is not TableRowLayout row || !CanUpdateWith(row)) return false;
            Apply(row);
            UpdateWidget(row);
            return true;
        }
        private void Apply(TableRowLayout row) {
            Element.style.backgroundColor = row.Row.Background ?? UnityEngine.Color.clear;
            Element.style.minHeight = row.Row.MinHeight ?? row.Table.MinRowHeight;
            var border = row.Last ? default : row.Table.Border.HorizontalInside;
            Element.style.borderBottomWidth = border.Width;
            Element.style.borderBottomColor = border.Color;
            var cells = new Widget[row.Row.Children.Count];
            for (var i = 0; i < cells.Length; i++) {
                var content = row.Row.Children[i];
                if (content is not TableCell) content = new TableCell(content, row.Table.DefaultVerticalAlignment);
                cells[i] = new TableSlot(content, row.Table.WidthAt(i), row.Table.CellPadding,
                    i == cells.Length - 1 ? default : row.Table.Border.VerticalInside);
            }
            ReconcileChildren(cells, Element);
        }
    }

    internal sealed class TableSlot : Widget {
        internal TableSlot(Widget child, TableColumnWidth width, EdgeInsets padding, BorderSide border) {
            Child = child; Width = width; Padding = padding; Border = border;
        }
        internal Widget Child { get; }
        internal TableColumnWidth Width { get; }
        internal EdgeInsets Padding { get; }
        internal BorderSide Border { get; }
        internal override WidgetNode CreateNode() => new TableSlotNode(this);
    }

    internal sealed class TableSlotNode : SingleChildWidgetNode<TableSlot> {
        internal TableSlotNode(TableSlot widget) : base(widget) { }
        protected override void OnMounted() { base.OnMounted(); ApplyConfiguration(CurrentWidget); }
        protected override VisualElement CreateElement(BuildContext context) {
            var element = new VisualElement { name = "lumaflow-table-slot" };
            element.style.minWidth = 0;
            return element;
        }
        protected override Widget GetChild(TableSlot widget) => widget.Child;
        protected override void ApplyConfiguration(TableSlot widget) {
            Element.style.flexShrink = 0;
            Element.style.flexGrow = widget.Width is FlexColumnWidth ? widget.Width.Value : 0;
            Element.style.flexBasis = widget.Width is FixedColumnWidth ? widget.Width.Value : 0;
            Element.style.width = widget.Width is FixedColumnWidth ? new StyleLength(widget.Width.Value) : StyleKeyword.Auto;
            PaddingStyleMapper.Apply(Element, widget.Padding);
            Element.style.borderRightWidth = widget.Border.Width;
            Element.style.borderRightColor = widget.Border.Color;
        }
    }

    internal sealed class TableCellNode : SingleChildWidgetNode<TableCell> {
        internal TableCellNode(TableCell widget) : base(widget) { }
        protected override void OnMounted() { base.OnMounted(); ApplyConfiguration(CurrentWidget); }
        protected override VisualElement CreateElement(BuildContext context) => new VisualElement { name = "lumaflow-table-cell" };
        protected override Widget GetChild(TableCell widget) => widget.Child;
        protected override void ApplyConfiguration(TableCell widget) {
            Element.tooltip = widget.Tooltip ?? string.Empty;
            Element.style.minWidth = 0;
            Element.style.flexGrow = 1;
            Element.style.justifyContent = widget.VerticalAlignment switch {
                TableCellVerticalAlignment.Middle => Justify.Center,
                TableCellVerticalAlignment.Bottom => Justify.FlexEnd,
                _ => Justify.FlexStart
            };
            if (Element.childCount > 0) Element[0].style.flexGrow = widget.VerticalAlignment == TableCellVerticalAlignment.Fill ? 1 : 0;
        }
    }
}
