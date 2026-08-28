#nullable enable

using System;

namespace LumaFlow {

    /// <summary>Defines how a text widget handles glyphs outside its layout bounds.</summary>
    public enum TextOverflow {
        Clip,
        Ellipsis,
        Visible
    }

    /// <summary>
    /// Displays static text using a native UI Toolkit label.
    /// </summary>
    public sealed class Text : Widget {
        /// <summary>
        /// Creates a text widget.
        /// </summary>
        public Text(
            string value,
            TextStyle? style = null,
            bool softWrap = true,
            TextOverflow overflow = TextOverflow.Clip,
            int? maxLines = null) {
            Value = value ?? throw new ArgumentNullException(nameof(value));
            Style = style;
            ValidateTextLayout(overflow, maxLines);
            SoftWrap = softWrap;
            Overflow = overflow;
            MaxLines = maxLines;
        }

        /// <summary>
        /// Creates text bound to an externally owned state value.
        /// </summary>
        public Text(
            State<string> value,
            TextStyle? style = null,
            bool softWrap = true,
            TextOverflow overflow = TextOverflow.Clip,
            int? maxLines = null) {
            ValueState = value ?? throw new ArgumentNullException(nameof(value));
            Value = value.Value;
            Style = style;
            ValidateTextLayout(overflow, maxLines);
            SoftWrap = softWrap;
            Overflow = overflow;
            MaxLines = maxLines;
        }

        /// <summary>
        /// Gets the text displayed by this widget.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Gets the optional explicit typography style.
        /// </summary>
        public TextStyle? Style { get; }

        /// <summary>Whether text may wrap at soft line breaks.</summary>
        public bool SoftWrap { get; }

        /// <summary>How visual overflow is rendered.</summary>
        public TextOverflow Overflow { get; }

        /// <summary>Maximum number of rendered lines, or null for no limit.</summary>
        public int? MaxLines { get; }

        internal State<string>? ValueState { get; }

        internal override WidgetNode CreateNode() {
            return new TextNode(this);
        }

        private static void ValidateTextLayout(TextOverflow overflow, int? maxLines) {
            if (!Enum.IsDefined(typeof(TextOverflow), overflow)) throw new ArgumentOutOfRangeException(nameof(overflow));
            if (maxLines is <= 0) throw new ArgumentOutOfRangeException(nameof(maxLines), "Max lines must be greater than zero.");
        }
    }

}
