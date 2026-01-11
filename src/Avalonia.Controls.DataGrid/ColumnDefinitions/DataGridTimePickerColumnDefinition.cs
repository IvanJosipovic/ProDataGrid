// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable disable

namespace Avalonia.Controls
{
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    sealed class DataGridTimePickerColumnDefinition : DataGridBoundColumnDefinition
    {
        private int? _minuteIncrement;
        private int? _secondIncrement;
        private string _clockIdentifier;
        private bool? _useSeconds;
        private string _formatString;

        public int? MinuteIncrement
        {
            get => _minuteIncrement;
            set => SetProperty(ref _minuteIncrement, value);
        }

        public int? SecondIncrement
        {
            get => _secondIncrement;
            set => SetProperty(ref _secondIncrement, value);
        }

        public string ClockIdentifier
        {
            get => _clockIdentifier;
            set => SetProperty(ref _clockIdentifier, value);
        }

        public bool? UseSeconds
        {
            get => _useSeconds;
            set => SetProperty(ref _useSeconds, value);
        }

        public string FormatString
        {
            get => _formatString;
            set => SetProperty(ref _formatString, value);
        }

        protected override DataGridColumn CreateColumnCore()
        {
            return new DataGridTimePickerColumn();
        }

        protected override void ApplyColumnProperties(DataGridColumn column, DataGridColumnDefinitionContext context)
        {
            base.ApplyColumnProperties(column, context);

            if (column is DataGridTimePickerColumn timeColumn)
            {
                if (!string.IsNullOrEmpty(ClockIdentifier))
                {
                    timeColumn.ClockIdentifier = ClockIdentifier;
                }
                else
                {
                    timeColumn.ClearValue(DataGridTimePickerColumn.ClockIdentifierProperty);
                }

                if (!string.IsNullOrEmpty(FormatString))
                {
                    timeColumn.FormatString = FormatString;
                }
                else
                {
                    timeColumn.ClearValue(DataGridTimePickerColumn.FormatStringProperty);
                }

                if (MinuteIncrement.HasValue)
                {
                    timeColumn.MinuteIncrement = MinuteIncrement.Value;
                }
                else
                {
                    timeColumn.ClearValue(DataGridTimePickerColumn.MinuteIncrementProperty);
                }

                if (SecondIncrement.HasValue)
                {
                    timeColumn.SecondIncrement = SecondIncrement.Value;
                }
                else
                {
                    timeColumn.ClearValue(DataGridTimePickerColumn.SecondIncrementProperty);
                }

                if (UseSeconds.HasValue)
                {
                    timeColumn.UseSeconds = UseSeconds.Value;
                }
                else
                {
                    timeColumn.ClearValue(DataGridTimePickerColumn.UseSecondsProperty);
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

            if (column is not DataGridTimePickerColumn timeColumn)
            {
                return false;
            }

            switch (propertyName)
            {
                case nameof(ClockIdentifier):
                    if (!string.IsNullOrEmpty(ClockIdentifier))
                    {
                        timeColumn.ClockIdentifier = ClockIdentifier;
                    }
                    else
                    {
                        timeColumn.ClearValue(DataGridTimePickerColumn.ClockIdentifierProperty);
                    }
                    return true;
                case nameof(FormatString):
                    if (!string.IsNullOrEmpty(FormatString))
                    {
                        timeColumn.FormatString = FormatString;
                    }
                    else
                    {
                        timeColumn.ClearValue(DataGridTimePickerColumn.FormatStringProperty);
                    }
                    return true;
                case nameof(MinuteIncrement):
                    if (MinuteIncrement.HasValue)
                    {
                        timeColumn.MinuteIncrement = MinuteIncrement.Value;
                    }
                    else
                    {
                        timeColumn.ClearValue(DataGridTimePickerColumn.MinuteIncrementProperty);
                    }
                    return true;
                case nameof(SecondIncrement):
                    if (SecondIncrement.HasValue)
                    {
                        timeColumn.SecondIncrement = SecondIncrement.Value;
                    }
                    else
                    {
                        timeColumn.ClearValue(DataGridTimePickerColumn.SecondIncrementProperty);
                    }
                    return true;
                case nameof(UseSeconds):
                    if (UseSeconds.HasValue)
                    {
                        timeColumn.UseSeconds = UseSeconds.Value;
                    }
                    else
                    {
                        timeColumn.ClearValue(DataGridTimePickerColumn.UseSecondsProperty);
                    }
                    return true;
            }

            return false;
        }
    }
}
