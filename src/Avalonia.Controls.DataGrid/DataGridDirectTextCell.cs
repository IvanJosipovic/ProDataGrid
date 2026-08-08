// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable disable

using System;
using System.ComponentModel;

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
        private DataGridTextColumn _column;
        private INotifyPropertyChanged _notifier;
        private bool _usesValueAccessor;

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

        private void UpdateValueSubscription()
        {
            if (_notifier != null)
            {
                _notifier.PropertyChanged -= OnItemPropertyChanged;
                _notifier = null;
            }

            if (!_usesValueAccessor)
            {
                return;
            }

            UpdateValue();
            if (DataContext is INotifyPropertyChanged notifier)
            {
                _notifier = notifier;
                _notifier.PropertyChanged += OnItemPropertyChanged;
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
    }
}
