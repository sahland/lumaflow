#nullable enable

using System;
using System.Collections.Generic;
using Assets.Application.UIKit.Tokens;
using Assets.Application.UIKit.Theme;
using LumaFlow;

namespace Assets.Application.UIKit.Components {
    /// <summary>Application-styled composition of the runtime tab strip and reactive tab content.</summary>
    public sealed class AppTabs<T> : StatelessWidget {
        private readonly State<T> _selected;
        private readonly IReadOnlyList<TabItem<T>> _items;
        private readonly Func<T, Widget> _contentBuilder;
        private readonly Action<T>? _onChanged;

        public AppTabs(
            State<T> selected,
            IReadOnlyList<TabItem<T>> items,
            Func<T, Widget> contentBuilder,
            Action<T>? onChanged = null) {
            _selected = selected ?? throw new ArgumentNullException(nameof(selected));
            _items = items ?? throw new ArgumentNullException(nameof(items));
            _contentBuilder = contentBuilder ?? throw new ArgumentNullException(nameof(contentBuilder));
            _onChanged = onChanged;
        }

        public override Widget Build(BuildContext context) {
            return new Column(gap: AppSpacing.Sm, children: new Widget[] {
                new TabBar<T>(_selected, _items, style: AppControlStyles.Tabs, onChanged: _onChanged),
                new TabView<T>(_selected, _contentBuilder)
            });
        }
    }
}
