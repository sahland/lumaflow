#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using NativeButton = UnityEngine.UIElements.Button;
using Framework = LumaFlow.LumaFlow;

namespace LumaFlow.Editor.Tests {
    public sealed class TableTests {
        [Test]
        public void PlainDataTableStaysWithinNativeElementBudget() {
            var columns = Enumerable.Range(0, 8).Select(i => new DataColumn(new Text("column-" + i))).ToArray();
            var rows = Enumerable.Range(0, 47).Select(i => new DataRow(
                Enumerable.Range(0, 8).Select(j => new DataCell(new Text(i + ":" + j))).ToArray(), new WidgetKey("row-" + i))).ToArray();
            var root = new VisualElement();
            using var mount = Framework.Mount(new DataTable(columns, rows), root);
            Assert.That(root.Query<Label>().ToList().Count, Is.EqualTo(48 * 8));
            Assert.That(CountElements(root), Is.LessThan(900));
            var exported = UxmlPreviewExporter.Export(new DataTable(columns, rows.Take(1).ToArray()), "table.uss");
            Assert.That(exported.Uxml, Does.Contain("0:0"));
        }

        [Test]
        public void CachedRowsInvalidateLayoutAndKeepInputsAcrossReorder() {
            var root = new VisualElement();
            var text = new State<string>("typed");
            var first = new DataRow(new[] {new DataCell(new TextField(text))}, new WidgetKey("first"));
            var second = new DataRow(new[] {new DataCell(new Text("second"))}, new WidgetKey("second"));
            DataTable Make(bool reverse, float width, bool numeric = false) => new(
                new[] {new DataColumn(new Text("Name"), numeric: numeric, columnWidth: new FixedColumnWidth(width))},
                reverse ? new[] {second, first} : new[] {first, second},
                style: new DataTableStyle(background: Color.magenta, dataRowMinHeight: width));
            var state = new State<Widget>(Make(false, 100));
            using var mount = Framework.Mount(new ReactiveBuilder<Widget>(state, value => value), root);
            var input = root.Q<UnityEngine.UIElements.TextField>();
            state.Value = Make(true, 100);
            Assert.That(root.Q<UnityEngine.UIElements.TextField>(), Is.SameAs(input));
            state.Value = Make(false, 160, true);
            Assert.That(root.Q<UnityEngine.UIElements.TextField>(), Is.SameAs(input));
            Assert.That(input.value, Is.EqualTo("typed"));
            var row = root.Query("lumaflow-table-row").ToList()[1];
            Assert.That(row.style.minHeight.value.value, Is.EqualTo(160));
            Assert.That(row.style.backgroundColor.value, Is.EqualTo(Color.magenta));
            Assert.That(row[0].style.width.value.value, Is.EqualTo(160));
            Assert.That(row[0].style.alignItems.value, Is.EqualTo(UnityEngine.UIElements.Align.FlexEnd));
        }

        [UnityTest]
        public IEnumerator DataTableLayoutKeepsNumericAlignmentAndMinimumRowHeight() {
            var window = ScriptableObject.CreateInstance<TableTestWindow>();
            MountHandle? mount = null;
            try {
                window.position = new Rect(50, 50, 600, 400); window.Show();
                mount = Framework.Mount(new SizedBox(new DataTable(new[] {
                    new DataColumn(new Text("Name"), columnWidth: new FixedColumnWidth(200)),
                    new DataColumn(new Text("Amount"), numeric: true, columnWidth: new FixedColumnWidth(200))
                }, new[] {new DataRow(new[] {new DataCell(new Text("item")), new DataCell(new Text("42"))})},
                    style: new DataTableStyle(cellPadding: EdgeInsets.All(8), dataRowMinHeight: 64)), width: 400), window.rootVisualElement);
                yield return null; yield return null;
                var rows = window.rootVisualElement.Query("lumaflow-table-row").ToList();
                Assert.That(rows[1].resolvedStyle.height, Is.GreaterThanOrEqualTo(64));
                Assert.That(rows[1][0].resolvedStyle.width, Is.EqualTo(200).Within(.5));
                Assert.That(rows[1][1].resolvedStyle.width, Is.EqualTo(200).Within(.5));
                Assert.That(rows[1][0][0].worldBound.xMin, Is.EqualTo(rows[1][0].worldBound.xMin + 8).Within(.5));
                Assert.That(rows[1][1][0].worldBound.xMax, Is.EqualTo(rows[1][1].worldBound.xMax - 8).Within(.5));
            } finally { mount?.Dispose(); window.Close(); }
        }

        private static int CountElements(VisualElement element) {
            var count = 1;
            foreach (var child in element.Children()) count += CountElements(child);
            return count;
        }

        [UnityTest]
        public IEnumerator CachedRowsRestoreControlledSelectionOnParentRebuild() {
            var window = ScriptableObject.CreateInstance<TableTestWindow>();
            MountHandle? mount = null;
            try {
                window.Show();
                var callbacks = 0;
                var row = new DataRow(new[] {new DataCell(new Text("item"))}, onSelectChanged: _ => callbacks++);
                var columns = Columns();
                var state = new State<Widget>(new DataTable(columns, new[] {row}));
                mount = Framework.Mount(new ReactiveBuilder<Widget>(state, value => value), window.rootVisualElement);
                yield return null;
                var toggle = window.rootVisualElement.Query<UnityEngine.UIElements.Toggle>().ToList()[1];
                toggle.value = true;
                Assert.That(callbacks, Is.EqualTo(1));
                state.Value = new DataTable(columns, new[] {row});
                Assert.That(toggle.value, Is.False);
                Assert.That(callbacks, Is.EqualTo(1));
            } finally { mount?.Dispose(); window.Close(); }
        }

        [Test]
        public void RejectsRaggedRowsDuplicateKeysAndInvalidDimensions() {
            Assert.Throws<ArgumentOutOfRangeException>(() => new FixedColumnWidth(float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FlexColumnWidth(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DataTableStyle(dataRowMinHeight: -1));
            Assert.Throws<ArgumentException>(() => new Table(new[] {
                new TableRow(new Widget[] {new Text("a")}), new TableRow(Array.Empty<Widget>())
            }));
            var row = new DataRow(new[] {new DataCell(new Text("cell"))}, new WidgetKey("same"));
            Assert.Throws<ArgumentException>(() => new DataTable(Columns(), new[] {row, row}));
            Assert.Throws<ArgumentException>(() => new DataTable(Columns(), new[] {new DataRow(Array.Empty<DataCell>())}));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PaginatedDataTable(Columns(), new Source(3), new State<int>(0), rowsPerPage: 0));
        }

        [Test]
        public void SnapshotsCollectionsAndKeepsCallerDataUnchanged() {
            var children = new List<Widget> {new Text("first")};
            var widths = new Dictionary<int, TableColumnWidth> {{0, new FixedColumnWidth(90)}};
            var row = new TableRow(children);
            var table = new Table(new[] {row}, widths);
            children.Clear(); widths.Clear();
            Assert.That(row.Children.Count, Is.EqualTo(1));
            Assert.That(table.ColumnWidths[0].Value, Is.EqualTo(90));
        }

        [Test]
        public void KeyedRowsRetainNativeInputsAcrossReorderAndWidthUpdates() {
            var root = new VisualElement();
            var state = new State<string>("typed");
            Table Build(bool reverse, float width) {
                var a = new TableRow(new Widget[] {new TextField(state)}, new WidgetKey("a"));
                var b = new TableRow(new Widget[] {new Text("b")}, new WidgetKey("b"));
                return new Table(reverse ? new[] {b, a} : new[] {a, b},
                    new Dictionary<int, TableColumnWidth> {{0, new FixedColumnWidth(width)}});
            }
            var configuration = new State<Widget>(Build(false, 100));
            using var mount = Framework.Mount(new ReactiveBuilder<Widget>(configuration, widget => widget), root);
            var field = root.Q<UnityEngine.UIElements.TextField>();
            configuration.Value = Build(true, 160);
            Assert.That(root.Q<UnityEngine.UIElements.TextField>(), Is.SameAs(field));
            Assert.That(field.value, Is.EqualTo("typed"));
            Assert.That(root.Q("lumaflow-table-slot").style.width.value.value, Is.EqualTo(160));
        }

        [Test]
        public void PaginationReadsOnlyVisibleRowsClampsAfterShrinkAndReleasesSubscriptions() {
            var root = new VisualElement();
            var source = new Source(23);
            var page = new State<int>(10);
            var changes = 0;
            var mount = Framework.Mount(new PaginatedDataTable(Columns(), source, page, 10, onPageChanged: _ => changes++), root);
            Assert.That(source.Requests, Is.EqualTo(Enumerable.Range(10, 10)));
            source.Requests.Clear();
            page.Value = 20;
            Assert.That(source.Requests, Is.EqualTo(new[] {20, 21, 22}));
            Assert.That(changes, Is.Zero);
            source.Count = 4; source.Requests.Clear(); source.NotifyListeners();
            Assert.That(source.Requests, Is.EqualTo(new[] {0, 1, 2, 3}));
            Assert.That(page.Value, Is.EqualTo(20));
            source.Count = 0; source.Requests.Clear(); source.NotifyListeners();
            Assert.That(source.Requests, Is.Empty);
            mount.Dispose();
            source.Count = 30; page.Value = 0; source.NotifyListeners();
            Assert.That(source.Requests, Is.Empty);
            Assert.That(root.childCount, Is.Zero);
        }

        [Test]
        public void PaginationRebindsSourceAndPageAndHandlesUnavailableRows() {
            var root = new VisualElement();
            var first = new Source(4);
            var second = new Source(5) {Loading = true};
            var oldPage = new State<int>(0);
            var configuration = new State<Widget>(new PaginatedDataTable(Columns(), first, oldPage, 2));
            using var mount = Framework.Mount(new ReactiveBuilder<Widget>(configuration, widget => widget), root);
            configuration.Value = new PaginatedDataTable(Columns(), second, new State<int>(2), 2);
            first.Requests.Clear(); second.Requests.Clear();
            first.NotifyListeners(); oldPage.Value = 2;
            Assert.That(first.Requests, Is.Empty);
            Assert.That(second.Requests, Is.Empty);
            second.Loading = false; second.NotifyListeners();
            Assert.That(second.Requests, Is.EqualTo(new[] {2, 3}));
            Assert.That(root.Query<Label>().ToList().Any(label => label.text == "row-2"), Is.True);
        }

        [Test]
        public void ThemeCopyWithPreservesAndOverridesTableTheme() {
            var theme = EditorToolWidgets.Theme;
            var tableTheme = new DataTableTheme(new DataTableStyle(headingBackground: Color.magenta));
            var updated = theme.WithDataTableTheme(tableTheme);
            Assert.That(updated.CopyWith().DataTableTheme, Is.SameAs(tableTheme));
            var root = new VisualElement();
            using var mount = Framework.Mount(new Theme(updated, new DataTable(Columns(), Array.Empty<DataRow>())), root);
            Assert.That(root.Q("lumaflow-table-row").style.backgroundColor.value, Is.EqualTo(Color.magenta));
        }

        [UnityTest]
        public IEnumerator LayoutKeepsColumnEdgesAlignedAndContentAdapts() {
            var window = ScriptableObject.CreateInstance<TableTestWindow>();
            MountHandle? mount = null;
            try {
                window.position = new Rect(50, 50, 600, 400); window.Show();
                var rows = new[] {
                    new TableRow(new Widget[] {new Text("ID"), new Text("Name"), new Text("Value")}),
                    new TableRow(new Widget[] {new Text("1"), new Text(new string('x', 180)), new SizedBox(new Text("tall"), height: 70)})
                };
                mount = Framework.Mount(new SizedBox(new Table(rows,
                    new Dictionary<int, TableColumnWidth> {{0, new FixedColumnWidth(80)}, {1, new FlexColumnWidth(2)}, {2, new FlexColumnWidth(1)}},
                    border: TableBorder.All(Color.gray), cellPadding: EdgeInsets.All(4)), width: 480), window.rootVisualElement);
                yield return null; yield return null;
                var nativeRows = window.rootVisualElement.Query("lumaflow-table-row").ToList();
                Assert.That(nativeRows.Count, Is.EqualTo(2));
                for (var i = 0; i < 3; i++) {
                    Assert.That(nativeRows[0][i].worldBound.x, Is.EqualTo(nativeRows[1][i].worldBound.x).Within(.5));
                    Assert.That(nativeRows[0][i].resolvedStyle.width, Is.EqualTo(nativeRows[1][i].resolvedStyle.width).Within(.5));
                }
                Assert.That(nativeRows[0][0].resolvedStyle.width, Is.EqualTo(80).Within(.5));
                Assert.That(nativeRows[1].resolvedStyle.height, Is.GreaterThanOrEqualTo(78));
                Assert.That(nativeRows[0][1].resolvedStyle.width, Is.GreaterThan(nativeRows[0][2].resolvedStyle.width));
            } finally { mount?.Dispose(); window.Close(); }
        }

        [UnityTest]
        public IEnumerator SortSelectionCellActionsAndPagingUseCurrentCallbacks() {
            var window = ScriptableObject.CreateInstance<TableTestWindow>();
            MountHandle? mount = null;
            try {
                window.Show();
                var sorts = new List<(int, bool)>();
                var selected = new List<bool>();
                var tapped = 0;
                var column = new DataColumn(new Text("Name"), onSort: (i, asc) => sorts.Add((i, asc)));
                var row = new DataRow(new[] {new DataCell(new Text("edit"), () => tapped++)}, onSelectChanged: selected.Add);
                mount = Framework.Mount(new DataTable(new[] {column}, new[] {row}, sortColumnIndex: 0), window.rootVisualElement);
                yield return null;
                var buttons = window.rootVisualElement.Query<NativeButton>().ToList();
                Submit(buttons[0]); Submit(buttons[1]);
                Assert.That(sorts, Is.EqualTo(new[] {(0, false)}));
                Assert.That(tapped, Is.EqualTo(1));
                var toggles = window.rootVisualElement.Query<UnityEngine.UIElements.Toggle>().ToList();
                toggles[1].value = true;
                Assert.That(selected, Is.EqualTo(new[] {true}));
                Assert.That(row.Selected, Is.False);
                mount.Dispose();
                var source = new Source(7);
                var page = new State<int>(0);
                var changed = new List<int>();
                mount = Framework.Mount(new PaginatedDataTable(Columns(), source, page, 3, onPageChanged: changed.Add), window.rootVisualElement);
                yield return null;
                Submit(window.rootVisualElement.Query<NativeButton>().ToList().Last());
                Assert.That(page.Value, Is.EqualTo(3));
                Assert.That(changed, Is.EqualTo(new[] {3}));
                page.Value = 6;
                Assert.That(window.rootVisualElement.Query<NativeButton>().ToList().Last().enabledSelf, Is.False);
                var before = changed.Count;
                Submit(window.rootVisualElement.Query<NativeButton>().ToList().Last());
                Assert.That(changed.Count, Is.EqualTo(before));
            } finally { mount?.Dispose(); window.Close(); }
        }

        private static void Submit(NativeButton button) {
            using var evt = NavigationSubmitEvent.GetPooled(); evt.target = button; button.SendEvent(evt);
        }
        private static DataColumn[] Columns() => new[] {new DataColumn(new Text("Name"))};
        private sealed class TableTestWindow : EditorWindow { }
        private sealed class Source : DataTableSource {
            internal int Count;
            internal bool Loading;
            internal readonly List<int> Requests = new();
            internal Source(int count) => Count = count;
            public override int RowCount => Count;
            public override DataRow? GetRow(int index) {
                Requests.Add(index);
                if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index));
                return Loading ? null : new DataRow(new[] {new DataCell(new Text("row-" + index))}, new WidgetKey("row-" + index));
            }
        }
    }
}
