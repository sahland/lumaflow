#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;

namespace LumaFlow.Editor {
    internal static class PublicApiContract {
        internal const string FileName = "RuntimeApiContract.v1.txt";

        internal static string ResolvePath() {
            var assembly = "# Assembly: " + typeof(Widget).Assembly.GetName().Name;
            var paths = AssetDatabase.FindAssets("RuntimeApiContract")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(FileName, StringComparison.Ordinal))
                .Where(path => File.ReadLines(path).Any(line => line == assembly))
                .ToArray();
            if (paths.Length != 1) throw new InvalidOperationException(
                $"Expected one frozen Runtime API contract for {assembly}; found {paths.Length}.");
            return Path.GetFullPath(paths[0]);
        }

        internal static IReadOnlyList<string> FindBreakingChanges(string frozen, string current) {
            var expected = Parse(frozen);
            var actual = Parse(current);
            var changes = new List<string>();
            if (expected.Assembly != actual.Assembly) changes.Add("Assembly identity changed.");
            foreach (var pair in expected.Types) {
                if (!actual.Types.TryGetValue(pair.Key, out var next)) {
                    changes.Add("Removed type: " + pair.Key);
                    continue;
                }
                if (pair.Value.Declaration != next.Declaration) changes.Add("Changed type contract: " + pair.Key);
                foreach (var member in pair.Value.Members)
                    if (!next.Members.Contains(member)) changes.Add(pair.Key + ": removed or changed " + member);
                foreach (var member in next.Members) {
                    if (pair.Value.Members.Contains(member)) continue;
                    if (next.IsInterface || member.Contains(" abstract ", StringComparison.Ordinal))
                        changes.Add(pair.Key + ": added required member " + member);
                }
            }
            return changes.AsReadOnly();
        }

        private static Contract Parse(string text) {
            if (text is null) throw new ArgumentNullException(nameof(text));
            var contract = new Contract();
            TypeContract? current = null;
            var errorObsolete = false;
            foreach (var original in text.Replace("\r\n", "\n").Split('\n')) {
                var line = original.Trim();
                if (line.StartsWith("# Assembly: ", StringComparison.Ordinal)) {
                    contract.Assembly = line.Substring(12);
                    continue;
                }
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) continue;
                line = Regex.Replace(line, @"^\[Obsolete\(.*error: false\)\]\s*", string.Empty);
                if (line.Length == 0) continue;
                if (!original.StartsWith("  ", StringComparison.Ordinal) && line.StartsWith("[Obsolete(", StringComparison.Ordinal)) {
                    errorObsolete = line.Contains("error: true", StringComparison.Ordinal);
                    continue;
                }
                if (original.StartsWith("  ", StringComparison.Ordinal)) {
                    if (current is null) throw new FormatException("API member has no declaring type.");
                    if (!current.Members.Add(line)) throw new FormatException("Duplicate API member: " + line);
                    continue;
                }
                var match = Regex.Match(line, @"^(?:public|protected)(?: \w+)* (class|struct|interface|enum|delegate) (.+)$");
                if (!match.Success) throw new FormatException("Invalid API type: " + line);
                var name = match.Groups[2].Value;
                var inheritance = name.IndexOf(" : ", StringComparison.Ordinal);
                if (inheritance >= 0) name = name.Substring(0, inheritance);
                var constraints = name.IndexOf(" where ", StringComparison.Ordinal);
                if (constraints >= 0) name = name.Substring(0, constraints);
                current = new TypeContract((errorObsolete ? "[Obsolete(error: true)] " : string.Empty) + line,
                    match.Groups[1].Value == "interface");
                errorObsolete = false;
                contract.Types.Add(name, current);
            }
            if (string.IsNullOrWhiteSpace(contract.Assembly) || contract.Types.Count == 0)
                throw new FormatException("A frozen API contract needs an assembly identity and at least one type.");
            return contract;
        }

        private sealed class Contract {
            internal string Assembly = string.Empty;
            internal readonly Dictionary<string, TypeContract> Types = new(StringComparer.Ordinal);
        }
        private sealed class TypeContract {
            internal readonly string Declaration;
            internal readonly bool IsInterface;
            internal readonly HashSet<string> Members = new(StringComparer.Ordinal);
            internal TypeContract(string declaration, bool isInterface) { Declaration = declaration; IsInterface = isInterface; }
        }
    }
}
