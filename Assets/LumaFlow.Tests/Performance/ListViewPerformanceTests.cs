#nullable enable

using System.Collections.Generic;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine.UIElements;

namespace LumaFlow.Performance.Tests {

    public sealed class ListViewPerformanceTests {
        private const int RealizedWindowSize = 24;
        private const int WarmupCount = 3;
        private const int MeasurementCount = 10;

        [TestCase(100)]
        [TestCase(1000)]
        [TestCase(10000)]
        [Performance, Version("1")]
        public void ListView_MountVirtualizedCollection(int itemCount) {
            var items = CreateItems(itemCount);
            VisualElement root = null!;
            MountHandle mount = null!;
            var buildCount = 0;

            Measure.Method(() => mount = global::LumaFlow.LumaFlow.Mount(
                    new ListView<int>(items, item => {
                        buildCount++;
                        return new Text(item.ToString());
                    }, itemHeight: 24f),
                    root))
                .SampleGroup($"ListView.Mount.{itemCount}")
                .WarmupCount(WarmupCount)
                .MeasurementCount(MeasurementCount)
                .IterationsPerMeasurement(1)
                .SetUp(() => {
                    root = new VisualElement();
                    buildCount = 0;
                })
                .CleanUp(() => {
                    Assert.That(buildCount, Is.LessThan(itemCount));
                    mount.Dispose();
                })
                .Run();
        }

        [Test, Performance, Version("1")]
        public void ListView_RecycleVisibleWindowInTenThousandItems() {
            var items = CreateItems(10000);
            var root = new VisualElement();
            var buildCount = 0;
            using var mount = global::LumaFlow.LumaFlow.Mount(
                new ListView<int>(items, item => {
                    buildCount++;
                    return new Text(item.ToString());
                }, itemHeight: 24f),
                root);
            var native = (UnityEngine.UIElements.ListView)root[0][0];
            var hosts = new VisualElement[RealizedWindowSize];
            for (var index = 0; index < hosts.Length; index++) {
                hosts[index] = native.makeItem();
                native.bindItem(hosts[index], index);
            }
            Assert.That(buildCount, Is.EqualTo(RealizedWindowSize));

            Measure.Method(() => {
                    for (var index = 0; index < hosts.Length; index++) {
                        native.bindItem(hosts[index], 5000 + index);
                    }
                })
                .SampleGroup("ListView.Rebind.24Of10000")
                .WarmupCount(WarmupCount)
                .MeasurementCount(MeasurementCount)
                .IterationsPerMeasurement(1)
                .Run();

            Assert.That(hosts.Length, Is.EqualTo(RealizedWindowSize));
            Assert.That(buildCount, Is.LessThan(items.Count));
            foreach (var host in hosts) native.destroyItem(host);
        }

        private static IReadOnlyList<int> CreateItems(int count) {
            var items = new List<int>(count);
            for (var index = 0; index < count; index++) items.Add(index);
            return items.AsReadOnly();
        }
    }
}
