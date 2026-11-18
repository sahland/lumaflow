#nullable enable

using System.IO;
using System;
using System.Linq;
using NUnit.Framework;

namespace LumaFlow.Editor.Tests {

    public sealed class PublicApiCompatibilityTests {
        [Test]
        public void RuntimePublicApiPreservesFrozenContract() {
            var frozen = File.ReadAllText(PublicApiContract.ResolvePath());
            var changes = PublicApiContract.FindBreakingChanges(frozen, PublicApiSnapshot.Capture(typeof(Widget).Assembly));
            Assert.That(changes, Is.Empty, string.Join("\n", changes));
        }

        [Test]
        public void AdditiveMembersDoNotBreakTheFrozenContract() {
            var frozen = Contract("public class Example.Widget", "public void Build()");
            var current = Contract("public class Example.Widget", "public void Build()", "public void Refresh()");
            Assert.That(PublicApiContract.FindBreakingChanges(frozen, current), Is.Empty);
        }

        [TestCase("public void Build(int count = 0)", "public void Build(int count = 1)")]
        [TestCase("public void Build(int count)", "public void Build(long count)")]
        [TestCase("public void Build(int count)", "public void Build(int size)")]
        [TestCase("public virtual void Build()", "public void Build()")]
        [TestCase("public string Name { get; set; }", "public string Name { get; }")]
        [TestCase("public const int Selected = 2", "public const int Selected = 3")]
        public void SignatureChangesAreRejected(string previous, string next) {
            Assert.That(PublicApiContract.FindBreakingChanges(Contract("public class Example.Widget", previous),
                Contract("public class Example.Widget", next)), Is.Not.Empty);
        }

        [Test]
        public void ChangedCurrentBaselineCannotHideRemovedFrozenMembers() {
            var frozen = Contract("public class Example.Widget", "public void Build()");
            var rewrittenBaseline = Contract("public class Example.Widget", "public void Render()");
            Assert.That(PublicApiContract.FindBreakingChanges(frozen, rewrittenBaseline), Is.Not.Empty);
        }

        [Test]
        public void NewRequiredMembersAndChangedInheritanceAreRejected() {
            var before = Contract("public interface Example.Source", "public abstract int Count { get; }");
            var after = Contract("public interface Example.Source", "public abstract int Count { get; }", "public abstract void Reset()");
            Assert.That(PublicApiContract.FindBreakingChanges(before, after), Is.Not.Empty);
            before = Contract("public abstract class Example.Source", "public void Reset()");
            after = Contract("public abstract class Example.Source", "public void Reset()", "public abstract void Build()");
            Assert.That(PublicApiContract.FindBreakingChanges(before, after), Is.Not.Empty);
            Assert.That(PublicApiContract.FindBreakingChanges(before,
                Contract("public abstract class Example.Source : Example.Base", "public void Reset()")), Is.Not.Empty);
        }

        [Test]
        public void WarningDeprecationIsAllowedButErrorDeprecationIsRejected() {
            var before = Contract("public class Example.Widget", "public void Build()");
            var warning = Contract("public class Example.Widget", "[Obsolete(\"Use Render\", error: false)] public void Build()");
            var error = Contract("public class Example.Widget", "[Obsolete(\"Use Render\", error: true)] public void Build()");
            Assert.That(PublicApiContract.FindBreakingChanges(before, warning), Is.Empty);
            Assert.That(PublicApiContract.FindBreakingChanges(before, error), Is.Not.Empty);
        }

        [Test]
        public void InvalidOrWrongAssemblyContractsFailClosed() {
            Assert.Throws<FormatException>(() => PublicApiContract.FindBreakingChanges(string.Empty, string.Empty));
            var contract = Contract("public class Example.Widget");
            Assert.That(PublicApiContract.FindBreakingChanges(contract, contract.Replace("Example.Runtime", "Other.Runtime")), Is.Not.Empty);
        }

        [Test]
        public void LegacyThemeSignaturesAndOptionalDefaultsRemainAvailable() {
            var signature = new[] {typeof(ColorScheme), typeof(TypographyTheme), typeof(SpacingTheme), typeof(RadiusTheme),
                typeof(ButtonTheme), typeof(IconThemeData), typeof(TextFieldTheme), typeof(DropdownTheme), typeof(ProgressIndicatorTheme),
                typeof(CheckboxTheme), typeof(RadioTheme), typeof(SwitchTheme), typeof(SliderTheme), typeof(TabBarTheme), typeof(SegmentedControlTheme)};
            var constructor = typeof(ThemeData).GetConstructor(signature);
            var copy = typeof(ThemeData).GetMethod("CopyWith", signature);
            Assert.That(constructor, Is.Not.Null);
            Assert.That(copy, Is.Not.Null);
            Assert.That(constructor!.GetParameters().Skip(5).All(parameter => parameter.IsOptional && parameter.DefaultValue == null), Is.True);
            Assert.That(copy!.GetParameters().All(parameter => parameter.IsOptional && parameter.DefaultValue == null), Is.True);
            var theme = EditorToolWidgets.Theme;
            Assert.That(theme.CopyWith(null).Colors, Is.SameAs(theme.Colors));
            var tableTheme = new DataTableTheme(new DataTableStyle());
            Assert.That(theme.WithDataTableTheme(tableTheme).CopyWith().DataTableTheme, Is.SameAs(tableTheme));
        }

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

        private static string Contract(string type, params string[] members) =>
            "# Assembly: Example.Runtime\n" + type + "\n" + string.Join("\n", members.Select(member => "  " + member)) + "\n";
    }
}
