#nullable enable

using System.Collections.Generic;
using System.Globalization;
using UnityEngine.UIElements;

namespace LumaFlow {

    internal static class LayoutDiagnosticProperties {
        public static void Append(Widget widget, VisualElement element, IDictionary<string, string> values) {
            values["native.flexGrow"] = Format(element.resolvedStyle.flexGrow);
            values["native.flexShrink"] = Format(element.resolvedStyle.flexShrink);
            switch (widget) {
                case Row row:
                    values["layout.axis"] = "horizontal";
                    values["layout.gap"] = Format(row.Gap);
                    values["layout.mainAxisAlignment"] = row.MainAxisAlignment.ToString();
                    values["layout.crossAxisAlignment"] = row.CrossAxisAlignment.ToString();
                    break;
                case Column column:
                    values["layout.axis"] = "vertical";
                    values["layout.gap"] = Format(column.Gap);
                    values["layout.mainAxisAlignment"] = column.MainAxisAlignment.ToString();
                    values["layout.crossAxisAlignment"] = column.CrossAxisAlignment.ToString();
                    break;
                case Expanded expanded:
                    values["layout.flex"] = expanded.Flex.ToString(CultureInfo.InvariantCulture);
                    values["layout.fit"] = FlexFit.Tight.ToString();
                    break;
                case Flexible flexible:
                    values["layout.flex"] = flexible.Flex.ToString(CultureInfo.InvariantCulture);
                    values["layout.fit"] = flexible.Fit.ToString();
                    break;
                case SizedBox sized:
                    values["constraints.width"] = FormatNullable(sized.Width, sized.ExpandsWidth);
                    values["constraints.height"] = FormatNullable(sized.Height, sized.ExpandsHeight);
                    break;
                case ConstrainedBox constrained:
                    values["constraints.minWidth"] = FormatNullable(constrained.Constraints.MinWidth);
                    values["constraints.maxWidth"] = FormatNullable(constrained.Constraints.MaxWidth);
                    values["constraints.minHeight"] = FormatNullable(constrained.Constraints.MinHeight);
                    values["constraints.maxHeight"] = FormatNullable(constrained.Constraints.MaxHeight);
                    break;
                case Align align:
                    values["layout.alignment"] = align.Alignment.ToString();
                    break;
                case Center:
                    values["layout.alignment"] = Alignment.Center.ToString();
                    break;
                case Stack:
                    values["layout.mode"] = "stack";
                    break;
                case ScrollView scrollView:
                    values["layout.scrollAxis"] = scrollView.Direction.ToString();
                    break;
                case LayoutBuilder builder:
                    values["layout.observedAxis"] = builder.Axis.ToString();
                    break;
            }
        }

        public static string Format(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);

        private static string FormatNullable(float? value, bool expands = false) =>
            expands ? "expand" : value is { } resolved ? Format(resolved) : "auto";
    }
}
