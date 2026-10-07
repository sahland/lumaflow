#nullable enable

using System;
using System.Collections.Generic;

namespace LumaFlow {

    /// <summary>
    /// Builds a full-width button row with optional leading and trailing content.
    /// The label owns the remaining width and truncates naturally when it is text.
    /// </summary>
    public sealed class ButtonContent : StatelessWidget {
        public ButtonContent(
            string label,
            Widget? leading = null,
            Widget? trailing = null,
            float gap = 8f,
            TextStyle? labelStyle = null)
            : this(
                new Text(
                    label ?? throw new ArgumentNullException(nameof(label)),
                    labelStyle,
                    maxLines: 1,
                    overflow: TextOverflow.Ellipsis),
                leading,
                trailing,
                gap) {
            Label = label;
        }

        public ButtonContent(
            Widget label,
            Widget? leading = null,
            Widget? trailing = null,
            float gap = 8f) {
            LabelWidget = label ?? throw new ArgumentNullException(nameof(label));
            if (!float.IsFinite(gap) || gap < 0f) {
                throw new ArgumentOutOfRangeException(nameof(gap), "Gap must be finite and non-negative.");
            }
            Leading = leading;
            Trailing = trailing;
            Gap = gap;
        }

        public string? Label { get; }
        public Widget LabelWidget { get; }
        public Widget? Leading { get; }
        public Widget? Trailing { get; }
        public float Gap { get; }

        public override Widget Build(BuildContext context) {
            var children = new List<Widget>(3);
            if (Leading is not null) children.Add(Leading);
            children.Add(new Expanded(Align.CenterLeft(LabelWidget)));
            if (Trailing is not null) children.Add(Trailing);
            return new FractionallySizedBox(
                new Row(children, Gap, crossAxisAlignment: CrossAxisAlignment.Center),
                widthFactor: 1f);
        }
    }
}
