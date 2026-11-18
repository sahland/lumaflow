#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

namespace LumaFlow {
    public abstract class TableColumnWidth {
        private protected TableColumnWidth(float value) {
            if (!float.IsFinite(value) || value <= 0f) throw new ArgumentOutOfRangeException(nameof(value));
            Value = value;
        }
        public float Value { get; }
    }

    public sealed class FixedColumnWidth : TableColumnWidth {
        public FixedColumnWidth(float value) : base(value) { }
    }

    public sealed class FlexColumnWidth : TableColumnWidth {
        public FlexColumnWidth(float value = 1f) : base(value) { }
    }

    public enum TableCellVerticalAlignment { Top, Middle, Bottom, Fill }

    public sealed class TableCell : Widget {
        public TableCell(Widget child, TableCellVerticalAlignment verticalAlignment = TableCellVerticalAlignment.Middle, string? tooltip = null) {
            Child = child ?? throw new ArgumentNullException(nameof(child));
            if (!Enum.IsDefined(typeof(TableCellVerticalAlignment), verticalAlignment))
                throw new ArgumentOutOfRangeException(nameof(verticalAlignment));
            VerticalAlignment = verticalAlignment;
            Tooltip = tooltip;
        }
        public Widget Child { get; }
        public TableCellVerticalAlignment VerticalAlignment { get; }
        public string? Tooltip { get; }
        internal override WidgetNode CreateNode() => new TableCellNode(this);
    }

    public sealed class TableRow {
        public TableRow(IReadOnlyList<Widget> children, WidgetKey? key = null, Color? background = null, float? minHeight = null) {
            Children = TableValidation.Copy(children, nameof(children));
            if (key is { IsValid: false }) throw new ArgumentException("A row key must be valid.", nameof(key));
            Key = key;
            Background = background;
            if (minHeight is { } height) TableValidation.NonNegative(height, nameof(minHeight));
            MinHeight = minHeight;
        }
        public IReadOnlyList<Widget> Children { get; }
        public WidgetKey? Key { get; }
        public Color? Background { get; }
        public float? MinHeight { get; }
    }

    public sealed class TableBorder {
        public TableBorder(BorderSide outside = default, BorderSide horizontalInside = default, BorderSide verticalInside = default) {
            Outside = outside;
            HorizontalInside = horizontalInside;
            VerticalInside = verticalInside;
        }
        public BorderSide Outside { get; }
        public BorderSide HorizontalInside { get; }
        public BorderSide VerticalInside { get; }
        public static TableBorder All(Color color, float width = 1f) {
            var side = new BorderSide(color, width);
            return new TableBorder(side, side, side);
        }
    }

    public sealed class Table : Widget {
        public Table(
            IReadOnlyList<TableRow> children,
            IReadOnlyDictionary<int, TableColumnWidth>? columnWidths = null,
            TableColumnWidth? defaultColumnWidth = null,
            TableBorder? border = null,
            TableCellVerticalAlignment defaultVerticalAlignment = TableCellVerticalAlignment.Top,
            EdgeInsets? cellPadding = null,
            float minRowHeight = 0f) {
            Children = TableValidation.Copy(children, nameof(children));
            if (!Enum.IsDefined(typeof(TableCellVerticalAlignment), defaultVerticalAlignment))
                throw new ArgumentOutOfRangeException(nameof(defaultVerticalAlignment));
            TableValidation.NonNegative(minRowHeight, nameof(minRowHeight));
            ColumnCount = Children.Count == 0 ? 0 : Children[0].Children.Count;
            var keys = new HashSet<WidgetKey>();
            foreach (var row in Children) {
                if (row.Children.Count != ColumnCount) throw new ArgumentException("All table rows must have the same number of cells.", nameof(children));
                if (row.Key is { } key && !keys.Add(key)) throw new ArgumentException("Table row keys must be unique.", nameof(children));
            }
            var widths = new Dictionary<int, TableColumnWidth>();
            if (columnWidths is not null) foreach (var pair in columnWidths) {
                if (pair.Key < 0 || (ColumnCount > 0 && pair.Key >= ColumnCount)) throw new ArgumentOutOfRangeException(nameof(columnWidths));
                widths.Add(pair.Key, pair.Value ?? throw new ArgumentException("Column widths cannot be null.", nameof(columnWidths)));
            }
            ColumnWidths = new System.Collections.ObjectModel.ReadOnlyDictionary<int, TableColumnWidth>(widths);
            DefaultColumnWidth = defaultColumnWidth ?? new FlexColumnWidth();
            Border = border ?? new TableBorder();
            DefaultVerticalAlignment = defaultVerticalAlignment;
            CellPadding = cellPadding ?? EdgeInsets.Zero;
            MinRowHeight = minRowHeight;
        }
        public IReadOnlyList<TableRow> Children { get; }
        public int ColumnCount { get; }
        public IReadOnlyDictionary<int, TableColumnWidth> ColumnWidths { get; }
        public TableColumnWidth DefaultColumnWidth { get; }
        public TableBorder Border { get; }
        public TableCellVerticalAlignment DefaultVerticalAlignment { get; }
        public EdgeInsets CellPadding { get; }
        public float MinRowHeight { get; }
        internal TableColumnWidth WidthAt(int index) => ColumnWidths.TryGetValue(index, out var width) ? width : DefaultColumnWidth;
        internal override WidgetNode CreateNode() => new TableNode(this);
    }

    internal static class TableValidation {
        internal static IReadOnlyList<T> Copy<T>(IReadOnlyList<T> source, string name) where T : class {
            if (source is null) throw new ArgumentNullException(name);
            var copy = new T[source.Count];
            for (var i = 0; i < copy.Length; i++) copy[i] = source[i] ?? throw new ArgumentException("Items cannot be null.", name);
            return Array.AsReadOnly(copy);
        }
        internal static void NonNegative(float value, string name) {
            if (!float.IsFinite(value) || value < 0f) throw new ArgumentOutOfRangeException(name);
        }
    }
}
