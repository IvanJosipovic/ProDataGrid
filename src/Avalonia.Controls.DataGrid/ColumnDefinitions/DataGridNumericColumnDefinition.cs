// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable disable

using System.Globalization;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

namespace Avalonia.Controls
{
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    sealed class DataGridNumericColumnDefinition : DataGridBoundColumnDefinition
    {
        private string _formatString;
        private NumberFormatInfo _numberFormat;
        private decimal? _minimum;
        private decimal? _maximum;
        private decimal? _increment;
        private bool? _showButtonSpinner;
        private Location? _buttonSpinnerLocation;
        private bool? _allowSpin;
        private bool? _clipValueToMinMax;
        private string _watermark;
        private HorizontalAlignment? _horizontalContentAlignment;
        private VerticalAlignment? _verticalContentAlignment;

        public string FormatString
        {
            get => _formatString;
            set => SetProperty(ref _formatString, value);
        }

        public NumberFormatInfo NumberFormat
        {
            get => _numberFormat;
            set => SetProperty(ref _numberFormat, value);
        }

        public decimal? Minimum
        {
            get => _minimum;
            set => SetProperty(ref _minimum, value);
        }

        public decimal? Maximum
        {
            get => _maximum;
            set => SetProperty(ref _maximum, value);
        }

        public decimal? Increment
        {
            get => _increment;
            set => SetProperty(ref _increment, value);
        }

        public bool? ShowButtonSpinner
        {
            get => _showButtonSpinner;
            set => SetProperty(ref _showButtonSpinner, value);
        }

        public Location? ButtonSpinnerLocation
        {
            get => _buttonSpinnerLocation;
            set => SetProperty(ref _buttonSpinnerLocation, value);
        }

        public bool? AllowSpin
        {
            get => _allowSpin;
            set => SetProperty(ref _allowSpin, value);
        }

        public bool? ClipValueToMinMax
        {
            get => _clipValueToMinMax;
            set => SetProperty(ref _clipValueToMinMax, value);
        }

        public string Watermark
        {
            get => _watermark;
            set => SetProperty(ref _watermark, value);
        }

        public HorizontalAlignment? HorizontalContentAlignment
        {
            get => _horizontalContentAlignment;
            set => SetProperty(ref _horizontalContentAlignment, value);
        }

        public VerticalAlignment? VerticalContentAlignment
        {
            get => _verticalContentAlignment;
            set => SetProperty(ref _verticalContentAlignment, value);
        }

        protected override DataGridColumn CreateColumnCore()
        {
            return new DataGridNumericColumn();
        }

        protected override void ApplyColumnProperties(DataGridColumn column, DataGridColumnDefinitionContext context)
        {
            base.ApplyColumnProperties(column, context);

            if (column is DataGridNumericColumn numericColumn)
            {
                if (FormatString != null)
                {
                    numericColumn.FormatString = FormatString;
                }
                else
                {
                    numericColumn.ClearValue(DataGridNumericColumn.FormatStringProperty);
                }

                if (NumberFormat != null)
                {
                    numericColumn.NumberFormat = NumberFormat;
                }
                else
                {
                    numericColumn.ClearValue(DataGridNumericColumn.NumberFormatProperty);
                }

                if (Watermark != null)
                {
                    numericColumn.Watermark = Watermark;
                }
                else
                {
                    numericColumn.ClearValue(DataGridNumericColumn.WatermarkProperty);
                }

                if (Minimum.HasValue)
                {
                    numericColumn.Minimum = Minimum.Value;
                }
                else
                {
                    numericColumn.ClearValue(DataGridNumericColumn.MinimumProperty);
                }

                if (Maximum.HasValue)
                {
                    numericColumn.Maximum = Maximum.Value;
                }
                else
                {
                    numericColumn.ClearValue(DataGridNumericColumn.MaximumProperty);
                }

                if (Increment.HasValue)
                {
                    numericColumn.Increment = Increment.Value;
                }
                else
                {
                    numericColumn.ClearValue(DataGridNumericColumn.IncrementProperty);
                }

                if (ShowButtonSpinner.HasValue)
                {
                    numericColumn.ShowButtonSpinner = ShowButtonSpinner.Value;
                }
                else
                {
                    numericColumn.ClearValue(DataGridNumericColumn.ShowButtonSpinnerProperty);
                }

                if (ButtonSpinnerLocation.HasValue)
                {
                    numericColumn.ButtonSpinnerLocation = ButtonSpinnerLocation.Value;
                }
                else
                {
                    numericColumn.ClearValue(DataGridNumericColumn.ButtonSpinnerLocationProperty);
                }

                if (AllowSpin.HasValue)
                {
                    numericColumn.AllowSpin = AllowSpin.Value;
                }
                else
                {
                    numericColumn.ClearValue(DataGridNumericColumn.AllowSpinProperty);
                }

                if (ClipValueToMinMax.HasValue)
                {
                    numericColumn.ClipValueToMinMax = ClipValueToMinMax.Value;
                }
                else
                {
                    numericColumn.ClearValue(DataGridNumericColumn.ClipValueToMinMaxProperty);
                }

                if (HorizontalContentAlignment.HasValue)
                {
                    numericColumn.HorizontalContentAlignment = HorizontalContentAlignment.Value;
                }
                else
                {
                    numericColumn.ClearValue(DataGridNumericColumn.HorizontalContentAlignmentProperty);
                }

                if (VerticalContentAlignment.HasValue)
                {
                    numericColumn.VerticalContentAlignment = VerticalContentAlignment.Value;
                }
                else
                {
                    numericColumn.ClearValue(DataGridNumericColumn.VerticalContentAlignmentProperty);
                }
            }
        }

        protected override bool ApplyColumnPropertyChange(
            DataGridColumn column,
            DataGridColumnDefinitionContext context,
            string propertyName)
        {
            if (base.ApplyColumnPropertyChange(column, context, propertyName))
            {
                return true;
            }

            if (column is not DataGridNumericColumn numericColumn)
            {
                return false;
            }

            switch (propertyName)
            {
                case nameof(FormatString):
                    if (FormatString != null)
                    {
                        numericColumn.FormatString = FormatString;
                    }
                    else
                    {
                        numericColumn.ClearValue(DataGridNumericColumn.FormatStringProperty);
                    }
                    return true;
                case nameof(NumberFormat):
                    if (NumberFormat != null)
                    {
                        numericColumn.NumberFormat = NumberFormat;
                    }
                    else
                    {
                        numericColumn.ClearValue(DataGridNumericColumn.NumberFormatProperty);
                    }
                    return true;
                case nameof(Watermark):
                    if (Watermark != null)
                    {
                        numericColumn.Watermark = Watermark;
                    }
                    else
                    {
                        numericColumn.ClearValue(DataGridNumericColumn.WatermarkProperty);
                    }
                    return true;
                case nameof(Minimum):
                    if (Minimum.HasValue)
                    {
                        numericColumn.Minimum = Minimum.Value;
                    }
                    else
                    {
                        numericColumn.ClearValue(DataGridNumericColumn.MinimumProperty);
                    }
                    return true;
                case nameof(Maximum):
                    if (Maximum.HasValue)
                    {
                        numericColumn.Maximum = Maximum.Value;
                    }
                    else
                    {
                        numericColumn.ClearValue(DataGridNumericColumn.MaximumProperty);
                    }
                    return true;
                case nameof(Increment):
                    if (Increment.HasValue)
                    {
                        numericColumn.Increment = Increment.Value;
                    }
                    else
                    {
                        numericColumn.ClearValue(DataGridNumericColumn.IncrementProperty);
                    }
                    return true;
                case nameof(ShowButtonSpinner):
                    if (ShowButtonSpinner.HasValue)
                    {
                        numericColumn.ShowButtonSpinner = ShowButtonSpinner.Value;
                    }
                    else
                    {
                        numericColumn.ClearValue(DataGridNumericColumn.ShowButtonSpinnerProperty);
                    }
                    return true;
                case nameof(ButtonSpinnerLocation):
                    if (ButtonSpinnerLocation.HasValue)
                    {
                        numericColumn.ButtonSpinnerLocation = ButtonSpinnerLocation.Value;
                    }
                    else
                    {
                        numericColumn.ClearValue(DataGridNumericColumn.ButtonSpinnerLocationProperty);
                    }
                    return true;
                case nameof(AllowSpin):
                    if (AllowSpin.HasValue)
                    {
                        numericColumn.AllowSpin = AllowSpin.Value;
                    }
                    else
                    {
                        numericColumn.ClearValue(DataGridNumericColumn.AllowSpinProperty);
                    }
                    return true;
                case nameof(ClipValueToMinMax):
                    if (ClipValueToMinMax.HasValue)
                    {
                        numericColumn.ClipValueToMinMax = ClipValueToMinMax.Value;
                    }
                    else
                    {
                        numericColumn.ClearValue(DataGridNumericColumn.ClipValueToMinMaxProperty);
                    }
                    return true;
                case nameof(HorizontalContentAlignment):
                    if (HorizontalContentAlignment.HasValue)
                    {
                        numericColumn.HorizontalContentAlignment = HorizontalContentAlignment.Value;
                    }
                    else
                    {
                        numericColumn.ClearValue(DataGridNumericColumn.HorizontalContentAlignmentProperty);
                    }
                    return true;
                case nameof(VerticalContentAlignment):
                    if (VerticalContentAlignment.HasValue)
                    {
                        numericColumn.VerticalContentAlignment = VerticalContentAlignment.Value;
                    }
                    else
                    {
                        numericColumn.ClearValue(DataGridNumericColumn.VerticalContentAlignmentProperty);
                    }
                    return true;
            }

            return false;
        }
    }
}
