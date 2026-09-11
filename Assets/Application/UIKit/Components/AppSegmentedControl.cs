#nullable enable

using System;
using System.Collections.Generic;
using Assets.Application.UIKit.Theme;
using LumaFlow;

namespace Assets.Application.UIKit.Components {
    /// <summary>Application-branded composition of the typed runtime segmented control.</summary>
    public sealed class AppSegmentedControl<T> : StatelessWidget {
        private readonly State<T> _value;
        private readonly IReadOnlyList<SegmentedControlItem<T>> _items;
        private readonly Action<T>? _onChanged;

        public AppSegmentedControl(State<T> value, IReadOnlyList<SegmentedControlItem<T>> items, Action<T>? onChanged = null) {
            _value = value ?? throw new ArgumentNullException(nameof(value));
            _items = items ?? throw new ArgumentNullException(nameof(items));
            _onChanged = onChanged;
        }

        public override Widget Build(BuildContext context) =>
            new SegmentedControl<T>(_value, _items, style: AppControlStyles.SegmentedControl, onChanged: _onChanged);
    }
}
