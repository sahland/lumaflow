#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Framework = global::LumaFlow.LumaFlow;

namespace LumaFlow.Samples.Tables {
    [RequireComponent(typeof(UIDocument))]
    public sealed class TableSample : MonoBehaviour {
        private static readonly ThemeData SampleTheme = new(
            new ColorScheme(new Color(.95f, .95f, .96f), Color.white, new Color(.92f, .94f, .96f),
                new Color(.2f, .35f, .65f), new Color(.84f, .89f, .97f), Color.white,
                new Color(.16f, .19f, .24f), new Color(.35f, .4f, .46f), new Color(.75f, .78f, .82f)),
            new TypographyTheme(new TextStyle(Color.black, 22), new TextStyle(Color.black, 18),
                new TextStyle(new Color(.16f, .19f, .24f), 14), new TextStyle(new Color(.16f, .19f, .24f), 12)),
            new SpacingTheme(4, 8, 12, 16, 24),
            new RadiusTheme(BorderRadius.All(4), BorderRadius.All(8), BorderRadius.All(12)),
            new ButtonTheme(new ButtonStyle(background: new Color(.2f, .35f, .65f), foreground: Color.white),
                new ButtonStyle(background: new Color(.92f, .94f, .96f), foreground: new Color(.16f, .19f, .24f))));
        private MountHandle? _mount;
        private Rows _rows = null!;
        private State<int> _first = null!;
        private State<Widget> _configuration = null!;
        private int _sortColumn;
        private bool _ascending = true;

        private void OnEnable() {
            _rows = new Rows();
            _first = new State<int>(0);
            _configuration = new State<Widget>(BuildTable());
            _mount = Framework.Mount(new Theme(SampleTheme, new Padding(new ReactiveBuilder<Widget>(_configuration, widget => widget), EdgeInsets.All(20))),
                GetComponent<UIDocument>().rootVisualElement);
        }
        private Widget BuildTable() => new PaginatedDataTable(new[] {
            new DataColumn(new Text("Name"), onSort: Sort),
            new DataColumn(new Text("Area, m²"), numeric: true, onSort: Sort, columnWidth: new FixedColumnWidth(140))
        }, _rows, _first, rowsPerPage: 8, sortColumnIndex: _sortColumn, sortAscending: _ascending,
            header: new Text("Apartments", new TextStyle(fontSize: 22, fontStyle: FontStyle.Bold)));

        private void Sort(int column, bool ascending) {
            _sortColumn = column; _ascending = ascending;
            _rows.Items.Sort((a, b) => {
                var order = column == 0 ? string.CompareOrdinal(a.Name, b.Name) : a.Area.CompareTo(b.Area);
                return ascending ? order : -order;
            });
            _first.Value = 0;
            _configuration.Value = BuildTable();
        }
        private void OnDisable() { _mount?.Dispose(); _mount = null; }

        private sealed class Entry {
            internal readonly int Id;
            internal readonly string Name;
            internal readonly float Area;
            internal bool Selected;
            internal Entry(int id) { Id = id; Name = "Apartment " + id.ToString("D3"); Area = 30 + id * 1.25f; }
        }
        private sealed class Rows : DataTableSource {
            internal readonly List<Entry> Items = new();
            internal Rows() { for (var i = 1; i <= 25; i++) Items.Add(new Entry(i)); }
            public override int RowCount => Items.Count;
            public override DataRow GetRow(int index) {
                var entry = Items[index];
                return new DataRow(new[] {
                    new DataCell(new Text(entry.Name)),
                    new DataCell(new Text(entry.Area.ToString("F2")))
                }, new WidgetKey(entry.Id.ToString()), entry.Selected, selected => {
                    entry.Selected = selected;
                    NotifyListeners();
                });
            }
        }
    }
}
