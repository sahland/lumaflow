#nullable enable

using System;
using NUnit.Framework;
using Unity.PerformanceTesting;
using Unity.Profiling;
using UnityEngine.UIElements;

namespace LumaFlow.Performance.Tests {

    public sealed class ReconciliationPerformanceTests {
        private const int WarmupCount = 5;
        private const int MeasurementCount = 20;

        [Test, Performance, Version("1")]
        public void ReactiveBuilder_ReconcileOneHundredKeyedRows() {
            var revision = new State<int>(0);
            var root = new VisualElement();
            using var mount = global::LumaFlow.LumaFlow.Mount(
                new ReactiveBuilder<int>(revision, value => BuildRows(value)),
                root);
            Action reconcile = () => revision.Value++;

            Measure.Method(reconcile)
                .SampleGroup("Reconcile.KeyedColumn.100Rows")
                .WarmupCount(WarmupCount)
                .MeasurementCount(MeasurementCount)
                .IterationsPerMeasurement(1)
                .Run();
            AllocationEventMeasurement.Run(
                "Reconcile.KeyedColumn.100Rows",
                reconcile,
                WarmupCount,
                MeasurementCount);

            Assert.That(root.childCount, Is.EqualTo(1));
        }

        [Test, Performance, Version("1")]
        public void TabBar_SelectAmongEightItems() {
            var selected = new State<int>(0);
            var items = new TabItem<int>[8];
            for (var index = 0; index < items.Length; index++) {
                items[index] = new TabItem<int>(index, $"Tab {index}");
            }
            var root = new VisualElement();
            using var mount = global::LumaFlow.LumaFlow.Mount(new TabBar<int>(selected, items), root);
            var next = 0;
            Action selectNext = () => {
                next = (next + 1) % items.Length;
                selected.Value = next;
            };

            Measure.Method(selectNext)
                .SampleGroup("TabBar.Select.8Items")
                .WarmupCount(WarmupCount)
                .MeasurementCount(MeasurementCount)
                .IterationsPerMeasurement(1)
                .Run();
            AllocationEventMeasurement.Run(
                "TabBar.Select.8Items",
                selectNext,
                WarmupCount,
                MeasurementCount);

            Assert.That(selected.Value, Is.EqualTo(next));
            Assert.That(
                root.Query<UnityEngine.UIElements.Button>().ToList().Count,
                Is.EqualTo(items.Length));
        }

        private static Widget BuildRows(int revision) {
            var rows = new Widget[100];
            for (var index = 0; index < rows.Length; index++) {
                rows[index] = new Row(new Widget[] {
                    new Text($"Row {index}"),
                    new Text($"Revision {revision}")
                }).WithKey(new WidgetKey($"row-{index}"));
            }
            return new Column(rows);
        }
    }

    internal static class AllocationEventMeasurement {
        private const int RecorderCapacity = 16384;

        public static void Run(
            string name,
            Action action,
            int warmupCount,
            int measurementCount) {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A metric name is required.", nameof(name));
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (warmupCount < 0) throw new ArgumentOutOfRangeException(nameof(warmupCount));
            if (measurementCount <= 0) throw new ArgumentOutOfRangeException(nameof(measurementCount));

            for (var index = 0; index < warmupCount; index++) action();

            var samples = new SampleGroup($"{name}.ManagedAllocationCount", SampleUnit.Undefined, false);
            for (var index = 0; index < measurementCount; index++) {
                using var recorder = ProfilerRecorder.StartNew(
                    ProfilerCategory.Internal,
                    "GC.Alloc",
                    RecorderCapacity,
                    ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
                if (!recorder.Valid) {
                    throw new InvalidOperationException("The GC.Alloc profiler recorder is unavailable in this Player.");
                }

                action();
                recorder.Stop();
                var allocationCount = recorder.Count;
                if (allocationCount >= RecorderCapacity) {
                    throw new InvalidOperationException(
                        $"The GC.Alloc recorder saturated at {RecorderCapacity} samples while measuring '{name}'.");
                }
                Measure.Custom(samples, allocationCount);
            }
        }
    }
}
