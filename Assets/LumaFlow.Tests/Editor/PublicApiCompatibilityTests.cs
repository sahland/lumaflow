#nullable enable

using System.IO;
using NUnit.Framework;

namespace LumaFlow.Editor.Tests {

    public sealed class PublicApiCompatibilityTests {
        [Test]
        public void RuntimePublicApiMatchesReviewedBaseline() {
            var path = PublicApiSnapshot.ResolveBaselinePath();
            Assert.That(File.Exists(path), Is.True,
                $"Missing public API baseline at '{path}'. Generate and review it from the LumaFlow validation menu.");

            var expected = Normalize(File.ReadAllText(path));
            var actual = Normalize(PublicApiSnapshot.Capture(typeof(Widget).Assembly));
            Assert.That(actual, Is.EqualTo(expected),
                "LumaFlow.Runtime public API changed. Preserve compatibility or regenerate PublicApiBaseline.txt after reviewing the change.");
        }

        private static string Normalize(string value) => value.Replace("\r\n", "\n").TrimEnd() + "\n";
    }
}
