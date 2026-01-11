// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable disable

using System;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

namespace Avalonia.Controls
{
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    sealed class DataGridDatePickerColumnDefinition : DataGridBoundColumnDefinition
    {
        private DateTime? _displayDateStart;
        private DateTime? _displayDateEnd;
        private DayOfWeek? _firstDayOfWeek;
        private bool? _isTodayHighlighted;
        private CalendarDatePickerFormat? _selectedDateFormat;
        private string _customDateFormatString;
        private string _watermark;
        private HorizontalAlignment? _horizontalContentAlignment;
        private VerticalAlignment? _verticalContentAlignment;

        public DateTime? DisplayDateStart
        {
            get => _displayDateStart;
            set => SetProperty(ref _displayDateStart, value);
        }

        public DateTime? DisplayDateEnd
        {
            get => _displayDateEnd;
            set => SetProperty(ref _displayDateEnd, value);
        }

        public DayOfWeek? FirstDayOfWeek
        {
            get => _firstDayOfWeek;
            set => SetProperty(ref _firstDayOfWeek, value);
        }

        public bool? IsTodayHighlighted
        {
            get => _isTodayHighlighted;
            set => SetProperty(ref _isTodayHighlighted, value);
        }

        public CalendarDatePickerFormat? SelectedDateFormat
        {
            get => _selectedDateFormat;
            set => SetProperty(ref _selectedDateFormat, value);
        }

        public string CustomDateFormatString
        {
            get => _customDateFormatString;
            set => SetProperty(ref _customDateFormatString, value);
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
            return new DataGridDatePickerColumn();
        }

        protected override void ApplyColumnProperties(DataGridColumn column, DataGridColumnDefinitionContext context)
        {
            base.ApplyColumnProperties(column, context);

            if (column is DataGridDatePickerColumn dateColumn)
            {
                if (!string.IsNullOrEmpty(CustomDateFormatString))
                {
                    dateColumn.CustomDateFormatString = CustomDateFormatString;
                }
                else
                {
                    dateColumn.ClearValue(DataGridDatePickerColumn.CustomDateFormatStringProperty);
                }

                if (Watermark != null)
                {
                    dateColumn.Watermark = Watermark;
                }
                else
                {
                    dateColumn.ClearValue(DataGridDatePickerColumn.WatermarkProperty);
                }

                if (DisplayDateStart.HasValue)
                {
                    dateColumn.DisplayDateStart = DisplayDateStart.Value;
                }
                else
                {
                    dateColumn.ClearValue(DataGridDatePickerColumn.DisplayDateStartProperty);
                }

                if (DisplayDateEnd.HasValue)
                {
                    dateColumn.DisplayDateEnd = DisplayDateEnd.Value;
                }
                else
                {
                    dateColumn.ClearValue(DataGridDatePickerColumn.DisplayDateEndProperty);
                }

                if (FirstDayOfWeek.HasValue)
                {
                    dateColumn.FirstDayOfWeek = FirstDayOfWeek.Value;
                }
                else
                {
                    dateColumn.ClearValue(DataGridDatePickerColumn.FirstDayOfWeekProperty);
                }

                if (IsTodayHighlighted.HasValue)
                {
                    dateColumn.IsTodayHighlighted = IsTodayHighlighted.Value;
                }
                else
                {
                    dateColumn.ClearValue(DataGridDatePickerColumn.IsTodayHighlightedProperty);
                }

                if (SelectedDateFormat.HasValue)
                {
                    dateColumn.SelectedDateFormat = SelectedDateFormat.Value;
                }
                else
                {
                    dateColumn.ClearValue(DataGridDatePickerColumn.SelectedDateFormatProperty);
                }

                if (HorizontalContentAlignment.HasValue)
                {
                    dateColumn.HorizontalContentAlignment = HorizontalContentAlignment.Value;
                }
                else
                {
                    dateColumn.ClearValue(DataGridDatePickerColumn.HorizontalContentAlignmentProperty);
                }

                if (VerticalContentAlignment.HasValue)
                {
                    dateColumn.VerticalContentAlignment = VerticalContentAlignment.Value;
                }
                else
                {
                    dateColumn.ClearValue(DataGridDatePickerColumn.VerticalContentAlignmentProperty);
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

            if (column is not DataGridDatePickerColumn dateColumn)
            {
                return false;
            }

            switch (propertyName)
            {
                case nameof(CustomDateFormatString):
                    if (!string.IsNullOrEmpty(CustomDateFormatString))
                    {
                        dateColumn.CustomDateFormatString = CustomDateFormatString;
                    }
                    else
                    {
                        dateColumn.ClearValue(DataGridDatePickerColumn.CustomDateFormatStringProperty);
                    }
                    return true;
                case nameof(Watermark):
                    if (Watermark != null)
                    {
                        dateColumn.Watermark = Watermark;
                    }
                    else
                    {
                        dateColumn.ClearValue(DataGridDatePickerColumn.WatermarkProperty);
                    }
                    return true;
                case nameof(DisplayDateStart):
                    if (DisplayDateStart.HasValue)
                    {
                        dateColumn.DisplayDateStart = DisplayDateStart.Value;
                    }
                    else
                    {
                        dateColumn.ClearValue(DataGridDatePickerColumn.DisplayDateStartProperty);
                    }
                    return true;
                case nameof(DisplayDateEnd):
                    if (DisplayDateEnd.HasValue)
                    {
                        dateColumn.DisplayDateEnd = DisplayDateEnd.Value;
                    }
                    else
                    {
                        dateColumn.ClearValue(DataGridDatePickerColumn.DisplayDateEndProperty);
                    }
                    return true;
                case nameof(FirstDayOfWeek):
                    if (FirstDayOfWeek.HasValue)
                    {
                        dateColumn.FirstDayOfWeek = FirstDayOfWeek.Value;
                    }
                    else
                    {
                        dateColumn.ClearValue(DataGridDatePickerColumn.FirstDayOfWeekProperty);
                    }
                    return true;
                case nameof(IsTodayHighlighted):
                    if (IsTodayHighlighted.HasValue)
                    {
                        dateColumn.IsTodayHighlighted = IsTodayHighlighted.Value;
                    }
                    else
                    {
                        dateColumn.ClearValue(DataGridDatePickerColumn.IsTodayHighlightedProperty);
                    }
                    return true;
                case nameof(SelectedDateFormat):
                    if (SelectedDateFormat.HasValue)
                    {
                        dateColumn.SelectedDateFormat = SelectedDateFormat.Value;
                    }
                    else
                    {
                        dateColumn.ClearValue(DataGridDatePickerColumn.SelectedDateFormatProperty);
                    }
                    return true;
                case nameof(HorizontalContentAlignment):
                    if (HorizontalContentAlignment.HasValue)
                    {
                        dateColumn.HorizontalContentAlignment = HorizontalContentAlignment.Value;
                    }
                    else
                    {
                        dateColumn.ClearValue(DataGridDatePickerColumn.HorizontalContentAlignmentProperty);
                    }
                    return true;
                case nameof(VerticalContentAlignment):
                    if (VerticalContentAlignment.HasValue)
                    {
                        dateColumn.VerticalContentAlignment = VerticalContentAlignment.Value;
                    }
                    else
                    {
                        dateColumn.ClearValue(DataGridDatePickerColumn.VerticalContentAlignmentProperty);
                    }
                    return true;
            }

            return false;
        }
    }
}
