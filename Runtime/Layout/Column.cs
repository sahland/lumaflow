#nullable enable

using System;
using System.Collections.Generic;

namespace LumaFlow {

    /// <summary>
    /// Arranges child widgets vertically using native UI Toolkit flex layout.
    /// </summary>
    public sealed class Column : Widget {
        private readonly Widget[] _children;

        /// <summary>
        /// Creates a vertical layout.
        /// </summary>
        public Column(
            IReadOnlyList<Widget> children,
            float gap = 0f,
            MainAxisAlignment mainAxisAlignment = MainAxisAlignment.Start,
            CrossAxisAlignment crossAxisAlignment = CrossAxisAlignment.Stretch) {
            if (children is null) {
                throw new ArgumentNullException(nameof(children));
            }

            if (float.IsNaN(gap) || float.IsInfinity(gap) || gap < 0f) {
                throw new ArgumentOutOfRangeException(nameof(gap), "Gap must be finite and non-negative.");
            }

            ValidateAlignment(mainAxisAlignment, nameof(mainAxisAlignment));
            ValidateAlignment(crossAxisAlignment, nameof(crossAxisAlignment));

            _children = new Widget[children.Count];
            for (var index = 0; index < children.Count; index++) {
                _children[index] = children[index] ?? throw new ArgumentException(
                    "Column children cannot contain null.",
                    nameof(children));
            }

            Gap = gap;
            MainAxisAlignment = mainAxisAlignment;
            CrossAxisAlignment = crossAxisAlignment;
        }

        /// <summary>
        /// Gets the widgets arranged by this column.
        /// </summary>
        public IReadOnlyList<Widget> Children => _children;

        /// <summary>
        /// Gets the native flex gap between adjacent children.
        /// </summary>
        public float Gap { get; }

        public MainAxisAlignment MainAxisAlignment { get; }

        public CrossAxisAlignment CrossAxisAlignment { get; }

        internal override WidgetNode CreateNode() {
            return new ColumnNode(this);
        }

        private static void ValidateAlignment<TAlignment>(TAlignment alignment, string parameterName)
            where TAlignment : struct, Enum {
            if (!Enum.IsDefined(typeof(TAlignment), alignment)) {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }
    }

}
