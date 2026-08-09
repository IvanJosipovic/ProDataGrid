// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable disable

using System;
using System.ComponentModel;
using Avalonia.Controls.DataGridHierarchical;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Utils;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
#if !DATAGRID_INTERNAL
    /// <summary>
    /// A retained, template-driven text cell that binds its display value directly to the cell.
    /// </summary>
    public
#else
    internal
#endif
    sealed class DataGridDirectTextCell : DataGridCell
    {
        private const string PartText = "PART_Text";
        private DataGridTextColumn _column;
        private INotifyPropertyChanged _notifier;
        private INotifyPropertyChanged _itemNotifier;
        private bool _usesValueAccessor;
        private bool _valueAccessorConfigurationInitialized;
        private TextBlock _textElement;
        private readonly WeakPropertyChangedListener<DataGridDirectTextCell> _itemPropertyChangedListener;

        static DataGridDirectTextCell()
        {
            UseDirectChromeProperty.OverrideDefaultValue<DataGridDirectTextCell>(true);
            AffectsRender<DataGridDirectTextCell>(
                BackgroundProperty,
                BorderBrushProperty,
                BorderThicknessProperty,
                CornerRadiusProperty);
        }

        public DataGridDirectTextCell()
        {
            _itemPropertyChangedListener = new WeakPropertyChangedListener<DataGridDirectTextCell>(
                this,
                static (cell, sender, e) => cell.OnItemPropertyChanged(sender, e));
        }

        /// <summary>
        /// Defines the <see cref="Value"/> property.
        /// </summary>
        public static readonly DirectProperty<DataGridDirectTextCell, string> ValueProperty =
            AvaloniaProperty.RegisterDirect<DataGridDirectTextCell, string>(
                nameof(Value),
                cell => cell.Value,
                (cell, value) => cell.Value = value);

        private string _value;

        /// <summary>
        /// Gets or sets the formatted display value rendered by the cell theme.
        /// </summary>
        public string Value
        {
            get => _value;
            set => SetAndRaise(ValueProperty, ref _value, value);
        }

        internal bool ConfigureValueAccessor(DataGridTextColumn column, object dataItem)
        {
            _column = column;
            _usesValueAccessor = column?.CanUseDirectValueAccessorFor(dataItem) == true;
            _valueAccessorConfigurationInitialized = true;
            UpdateValueSubscription();
            return _usesValueAccessor;
        }

        internal bool ConfigureValueAccessor(DataGridTextColumn column) =>
            ConfigureValueAccessor(column, DataContext);

        internal bool UsesValueAccessor => _usesValueAccessor;

        internal bool ValueAccessorConfigurationInitialized => _valueAccessorConfigurationInitialized;

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            if (_column != null)
            {
                _column.ConfigureDirectTextCell(this, DataContext, preserveCompatibleMode: true);
            }
            else
            {
                UpdateValueSubscription();
            }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            UpdateValueSubscription();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            DetachValueSubscriptions();
            base.OnDetachedFromVisualTree(e);
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            _textElement = e.NameScope.Find<TextBlock>(PartText);
            UpdateTextElement();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == ValueProperty)
            {
                UpdateTextElement();
            }
        }

        private void UpdateValueSubscription()
        {
            DetachValueSubscriptions();

            if (!_usesValueAccessor)
            {
                return;
            }

            UpdateValue();
            if (_column?.TrackDirectTextValueChanges != true)
            {
                return;
            }

            // DataContext can change after recycling has detached the cell. Keep the
            // displayed value current, but let the attach hook establish subscriptions.
            if (VisualRoot == null)
            {
                return;
            }

            if (DataContext is INotifyPropertyChanged notifier)
            {
                _notifier = notifier;
                _notifier.PropertyChanged += _itemPropertyChangedListener.Handler;
            }

            if (DataContext is IHierarchicalNodeItem node &&
                node.Item is INotifyPropertyChanged itemNotifier &&
                !ReferenceEquals(itemNotifier, _notifier))
            {
                _itemNotifier = itemNotifier;
                _itemNotifier.PropertyChanged += _itemPropertyChangedListener.Handler;
            }
        }

        private void DetachValueSubscriptions()
        {
            if (_notifier != null)
            {
                _notifier.PropertyChanged -= _itemPropertyChangedListener.Handler;
                _notifier = null;
            }

            if (_itemNotifier != null)
            {
                _itemNotifier.PropertyChanged -= _itemPropertyChangedListener.Handler;
                _itemNotifier = null;
            }
        }

        private void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (ReferenceEquals(sender, _notifier) || ReferenceEquals(sender, _itemNotifier))
            {
                UpdateValue();
            }
        }

        private void UpdateValue()
        {
            Value = _column?.GetDirectCellText(DataContext);
        }

        private void UpdateTextElement()
        {
            if (_textElement != null && !string.Equals(_textElement.Text, Value, StringComparison.Ordinal))
            {
                _textElement.Text = Value;
            }
        }
    }
}
