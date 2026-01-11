// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable disable

using Avalonia.Controls.Primitives;

namespace Avalonia.Controls
{
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    sealed class DataGridSliderColumnDefinition : DataGridBoundColumnDefinition
    {
        private double? _minimum;
        private double? _maximum;
        private double? _smallChange;
        private double? _largeChange;
        private double? _tickFrequency;
        private bool? _isSnapToTickEnabled;
        private TickPlacement? _tickPlacement;
        private bool? _showValueText;
        private string _valueTextFormat;

        public double? Minimum
        {
            get => _minimum;
            set => SetProperty(ref _minimum, value);
        }

        public double? Maximum
        {
            get => _maximum;
            set => SetProperty(ref _maximum, value);
        }

        public double? SmallChange
        {
            get => _smallChange;
            set => SetProperty(ref _smallChange, value);
        }

        public double? LargeChange
        {
            get => _largeChange;
            set => SetProperty(ref _largeChange, value);
        }

        public double? TickFrequency
        {
            get => _tickFrequency;
            set => SetProperty(ref _tickFrequency, value);
        }

        public bool? IsSnapToTickEnabled
        {
            get => _isSnapToTickEnabled;
            set => SetProperty(ref _isSnapToTickEnabled, value);
        }

        public TickPlacement? TickPlacement
        {
            get => _tickPlacement;
            set => SetProperty(ref _tickPlacement, value);
        }

        public bool? ShowValueText
        {
            get => _showValueText;
            set => SetProperty(ref _showValueText, value);
        }

        public string ValueTextFormat
        {
            get => _valueTextFormat;
            set => SetProperty(ref _valueTextFormat, value);
        }

        protected override DataGridColumn CreateColumnCore()
        {
            return new DataGridSliderColumn();
        }

        protected override void ApplyColumnProperties(DataGridColumn column, DataGridColumnDefinitionContext context)
        {
            base.ApplyColumnProperties(column, context);

            if (column is DataGridSliderColumn sliderColumn)
            {
                if (ValueTextFormat != null)
                {
                    sliderColumn.ValueTextFormat = ValueTextFormat;
                }
                else
                {
                    sliderColumn.ClearValue(DataGridSliderColumn.ValueTextFormatProperty);
                }

                if (Minimum.HasValue)
                {
                    sliderColumn.Minimum = Minimum.Value;
                }
                else
                {
                    sliderColumn.ClearValue(DataGridSliderColumn.MinimumProperty);
                }

                if (Maximum.HasValue)
                {
                    sliderColumn.Maximum = Maximum.Value;
                }
                else
                {
                    sliderColumn.ClearValue(DataGridSliderColumn.MaximumProperty);
                }

                if (SmallChange.HasValue)
                {
                    sliderColumn.SmallChange = SmallChange.Value;
                }
                else
                {
                    sliderColumn.ClearValue(DataGridSliderColumn.SmallChangeProperty);
                }

                if (LargeChange.HasValue)
                {
                    sliderColumn.LargeChange = LargeChange.Value;
                }
                else
                {
                    sliderColumn.ClearValue(DataGridSliderColumn.LargeChangeProperty);
                }

                if (TickFrequency.HasValue)
                {
                    sliderColumn.TickFrequency = TickFrequency.Value;
                }
                else
                {
                    sliderColumn.ClearValue(DataGridSliderColumn.TickFrequencyProperty);
                }

                if (IsSnapToTickEnabled.HasValue)
                {
                    sliderColumn.IsSnapToTickEnabled = IsSnapToTickEnabled.Value;
                }
                else
                {
                    sliderColumn.ClearValue(DataGridSliderColumn.IsSnapToTickEnabledProperty);
                }

                if (TickPlacement.HasValue)
                {
                    sliderColumn.TickPlacement = TickPlacement.Value;
                }
                else
                {
                    sliderColumn.ClearValue(DataGridSliderColumn.TickPlacementProperty);
                }

                if (ShowValueText.HasValue)
                {
                    sliderColumn.ShowValueText = ShowValueText.Value;
                }
                else
                {
                    sliderColumn.ClearValue(DataGridSliderColumn.ShowValueTextProperty);
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

            if (column is not DataGridSliderColumn sliderColumn)
            {
                return false;
            }

            switch (propertyName)
            {
                case nameof(ValueTextFormat):
                    if (ValueTextFormat != null)
                    {
                        sliderColumn.ValueTextFormat = ValueTextFormat;
                    }
                    else
                    {
                        sliderColumn.ClearValue(DataGridSliderColumn.ValueTextFormatProperty);
                    }
                    return true;
                case nameof(Minimum):
                    if (Minimum.HasValue)
                    {
                        sliderColumn.Minimum = Minimum.Value;
                    }
                    else
                    {
                        sliderColumn.ClearValue(DataGridSliderColumn.MinimumProperty);
                    }
                    return true;
                case nameof(Maximum):
                    if (Maximum.HasValue)
                    {
                        sliderColumn.Maximum = Maximum.Value;
                    }
                    else
                    {
                        sliderColumn.ClearValue(DataGridSliderColumn.MaximumProperty);
                    }
                    return true;
                case nameof(SmallChange):
                    if (SmallChange.HasValue)
                    {
                        sliderColumn.SmallChange = SmallChange.Value;
                    }
                    else
                    {
                        sliderColumn.ClearValue(DataGridSliderColumn.SmallChangeProperty);
                    }
                    return true;
                case nameof(LargeChange):
                    if (LargeChange.HasValue)
                    {
                        sliderColumn.LargeChange = LargeChange.Value;
                    }
                    else
                    {
                        sliderColumn.ClearValue(DataGridSliderColumn.LargeChangeProperty);
                    }
                    return true;
                case nameof(TickFrequency):
                    if (TickFrequency.HasValue)
                    {
                        sliderColumn.TickFrequency = TickFrequency.Value;
                    }
                    else
                    {
                        sliderColumn.ClearValue(DataGridSliderColumn.TickFrequencyProperty);
                    }
                    return true;
                case nameof(IsSnapToTickEnabled):
                    if (IsSnapToTickEnabled.HasValue)
                    {
                        sliderColumn.IsSnapToTickEnabled = IsSnapToTickEnabled.Value;
                    }
                    else
                    {
                        sliderColumn.ClearValue(DataGridSliderColumn.IsSnapToTickEnabledProperty);
                    }
                    return true;
                case nameof(TickPlacement):
                    if (TickPlacement.HasValue)
                    {
                        sliderColumn.TickPlacement = TickPlacement.Value;
                    }
                    else
                    {
                        sliderColumn.ClearValue(DataGridSliderColumn.TickPlacementProperty);
                    }
                    return true;
                case nameof(ShowValueText):
                    if (ShowValueText.HasValue)
                    {
                        sliderColumn.ShowValueText = ShowValueText.Value;
                    }
                    else
                    {
                        sliderColumn.ClearValue(DataGridSliderColumn.ShowValueTextProperty);
                    }
                    return true;
            }

            return false;
        }
    }
}
