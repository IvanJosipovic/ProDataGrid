// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable disable

using System;
using System.ComponentModel;
using Avalonia.Controls.DataGridHierarchical;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

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
        private IBrush _cachedBorderBrush;
        private double _cachedBorderThickness;
        private Pen _cachedBorderPen;
        private TextBlock _textElement;

        static DataGridDirectTextCell()
        {
            AffectsRender<DataGridDirectTextCell>(
                BackgroundProperty,
                BorderBrushProperty,
                BorderThicknessProperty,
                CornerRadiusProperty);
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

        internal bool ConfigureValueAccessor(DataGridTextColumn column)
        {
            _column = column;
            _usesValueAccessor = column?.CanUseDirectValueAccessor == true;
            UpdateValueSubscription();
            return _usesValueAccessor;
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            UpdateValueSubscription();
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            _textElement = e.NameScope.Find<TextBlock>(PartText);
            UpdateTextElement();
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            var bounds = new Rect(Bounds.Size);
            var thickness = Math.Max(
                Math.Max(BorderThickness.Left, BorderThickness.Top),
                Math.Max(BorderThickness.Right, BorderThickness.Bottom));
            Pen borderPen = null;
            if (BorderBrush != null && thickness > 0d)
            {
                if (!ReferenceEquals(_cachedBorderBrush, BorderBrush) ||
                    !_cachedBorderThickness.Equals(thickness))
                {
                    _cachedBorderBrush = BorderBrush;
                    _cachedBorderThickness = thickness;
                    _cachedBorderPen = new Pen(BorderBrush, thickness);
                }

                borderPen = _cachedBorderPen;
            }

            if (Background == null && borderPen == null)
            {
                return;
            }

            var inset = borderPen == null ? 0d : thickness * 0.5d;
            var chromeBounds = new Rect(
                inset,
                inset,
                Math.Max(0d, bounds.Width - (inset * 2d)),
                Math.Max(0d, bounds.Height - (inset * 2d)));
            context.DrawRectangle(
                Background,
                borderPen,
                new RoundedRect(chromeBounds, CornerRadius));
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == BorderBrushProperty || change.Property == BorderThicknessProperty)
            {
                _cachedBorderBrush = null;
                _cachedBorderPen = null;
                _cachedBorderThickness = 0d;
            }
            else if (change.Property == ValueProperty)
            {
                UpdateTextElement();
            }
        }

        private void UpdateValueSubscription()
        {
            if (_notifier != null)
            {
                _notifier.PropertyChanged -= OnItemPropertyChanged;
                _notifier = null;
            }

            if (_itemNotifier != null)
            {
                _itemNotifier.PropertyChanged -= OnItemPropertyChanged;
                _itemNotifier = null;
            }

            if (!_usesValueAccessor)
            {
                return;
            }

            UpdateValue();
            if (_column?.TrackDirectTextValueChanges != true)
            {
                return;
            }

            if (DataContext is INotifyPropertyChanged notifier)
            {
                _notifier = notifier;
                _notifier.PropertyChanged += OnItemPropertyChanged;
            }

            if (DataContext is IHierarchicalNodeItem node &&
                node.Item is INotifyPropertyChanged itemNotifier &&
                !ReferenceEquals(itemNotifier, _notifier))
            {
                _itemNotifier = itemNotifier;
                _itemNotifier.PropertyChanged += OnItemPropertyChanged;
            }
        }

        private void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateValue();
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
