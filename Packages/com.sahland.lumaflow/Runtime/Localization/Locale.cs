#nullable enable

using System;

namespace LumaFlow {

    /// <summary>Identifies a language and optional script and region.</summary>
    public readonly struct Locale : IEquatable<Locale> {
        public Locale(string languageCode, string? countryCode = null, string? scriptCode = null) {
            if (string.IsNullOrWhiteSpace(languageCode)) throw new ArgumentException("A language code is required.", nameof(languageCode));
            LanguageCode = languageCode.Trim().ToLowerInvariant();
            CountryCode = NormalizeOptional(countryCode, upper: true);
            ScriptCode = NormalizeScript(scriptCode);
        }

        public string LanguageCode { get; }
        public string? CountryCode { get; }
        public string? ScriptCode { get; }

        public bool Equals(Locale other) =>
            string.Equals(LanguageCode, other.LanguageCode, StringComparison.Ordinal)
            && string.Equals(CountryCode, other.CountryCode, StringComparison.Ordinal)
            && string.Equals(ScriptCode, other.ScriptCode, StringComparison.Ordinal);

        public override bool Equals(object? obj) => obj is Locale other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(LanguageCode, CountryCode, ScriptCode);
        public override string ToString() {
            var value = LanguageCode;
            if (ScriptCode is not null) value += $"-{ScriptCode}";
            if (CountryCode is not null) value += $"-{CountryCode}";
            return value;
        }

        public static bool operator ==(Locale left, Locale right) => left.Equals(right);
        public static bool operator !=(Locale left, Locale right) => !left.Equals(right);

        private static string? NormalizeOptional(string? value, bool upper) {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var normalized = value.Trim();
            return upper ? normalized.ToUpperInvariant() : normalized.ToLowerInvariant();
        }

        private static string? NormalizeScript(string? value) {
            var normalized = NormalizeOptional(value, upper: false);
            if (normalized is null) return null;
            return char.ToUpperInvariant(normalized[0]) + normalized.Substring(1);
        }
    }
}
