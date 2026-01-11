// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable disable

using System;
using System.Collections;
using Avalonia.Controls.Templates;

namespace Avalonia.Controls
{
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    sealed class DataGridAutoCompleteColumnDefinition : DataGridBoundColumnDefinition
    {
        private IEnumerable _itemsSource;
        private string _itemTemplateKey;
        private AutoCompleteFilterMode? _filterMode;
        private int? _minimumPrefixLength;
        private TimeSpan? _minimumPopulateDelay;
        private double? _maxDropDownHeight;
        private bool? _isTextCompletionEnabled;
        private string _watermark;

        public IEnumerable ItemsSource
        {
            get => _itemsSource;
            set => SetProperty(ref _itemsSource, value);
        }

        public string ItemTemplateKey
        {
            get => _itemTemplateKey;
            set => SetProperty(ref _itemTemplateKey, value);
        }

        public AutoCompleteFilterMode? FilterMode
        {
            get => _filterMode;
            set => SetProperty(ref _filterMode, value);
        }

        public int? MinimumPrefixLength
        {
            get => _minimumPrefixLength;
            set => SetProperty(ref _minimumPrefixLength, value);
        }

        public TimeSpan? MinimumPopulateDelay
        {
            get => _minimumPopulateDelay;
            set => SetProperty(ref _minimumPopulateDelay, value);
        }

        public double? MaxDropDownHeight
        {
            get => _maxDropDownHeight;
            set => SetProperty(ref _maxDropDownHeight, value);
        }

        public bool? IsTextCompletionEnabled
        {
            get => _isTextCompletionEnabled;
            set => SetProperty(ref _isTextCompletionEnabled, value);
        }

        public string Watermark
        {
            get => _watermark;
            set => SetProperty(ref _watermark, value);
        }

        protected override DataGridColumn CreateColumnCore()
        {
            return new DataGridAutoCompleteColumn();
        }

        protected override void ApplyColumnProperties(DataGridColumn column, DataGridColumnDefinitionContext context)
        {
            base.ApplyColumnProperties(column, context);

            if (column is DataGridAutoCompleteColumn autoColumn)
            {
                if (ItemsSource != null)
                {
                    autoColumn.ItemsSource = ItemsSource;
                }
                else
                {
                    autoColumn.ClearValue(DataGridAutoCompleteColumn.ItemsSourceProperty);
                }

                if (ItemTemplateKey != null)
                {
                    autoColumn.ItemTemplate = context?.ResolveResource<IDataTemplate>(ItemTemplateKey);
                }
                else
                {
                    autoColumn.ClearValue(DataGridAutoCompleteColumn.ItemTemplateProperty);
                }

                if (Watermark != null)
                {
                    autoColumn.Watermark = Watermark;
                }
                else
                {
                    autoColumn.ClearValue(DataGridAutoCompleteColumn.WatermarkProperty);
                }

                if (FilterMode.HasValue)
                {
                    autoColumn.FilterMode = FilterMode.Value;
                }
                else
                {
                    autoColumn.ClearValue(DataGridAutoCompleteColumn.FilterModeProperty);
                }

                if (MinimumPrefixLength.HasValue)
                {
                    autoColumn.MinimumPrefixLength = MinimumPrefixLength.Value;
                }
                else
                {
                    autoColumn.ClearValue(DataGridAutoCompleteColumn.MinimumPrefixLengthProperty);
                }

                if (MinimumPopulateDelay.HasValue)
                {
                    autoColumn.MinimumPopulateDelay = MinimumPopulateDelay.Value;
                }
                else
                {
                    autoColumn.ClearValue(DataGridAutoCompleteColumn.MinimumPopulateDelayProperty);
                }

                if (MaxDropDownHeight.HasValue)
                {
                    autoColumn.MaxDropDownHeight = MaxDropDownHeight.Value;
                }
                else
                {
                    autoColumn.ClearValue(DataGridAutoCompleteColumn.MaxDropDownHeightProperty);
                }

                if (IsTextCompletionEnabled.HasValue)
                {
                    autoColumn.IsTextCompletionEnabled = IsTextCompletionEnabled.Value;
                }
                else
                {
                    autoColumn.ClearValue(DataGridAutoCompleteColumn.IsTextCompletionEnabledProperty);
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

            if (column is not DataGridAutoCompleteColumn autoColumn)
            {
                return false;
            }

            switch (propertyName)
            {
                case nameof(ItemsSource):
                    if (ItemsSource != null)
                    {
                        autoColumn.ItemsSource = ItemsSource;
                    }
                    else
                    {
                        autoColumn.ClearValue(DataGridAutoCompleteColumn.ItemsSourceProperty);
                    }
                    return true;
                case nameof(ItemTemplateKey):
                    if (ItemTemplateKey != null)
                    {
                        autoColumn.ItemTemplate = context?.ResolveResource<IDataTemplate>(ItemTemplateKey);
                    }
                    else
                    {
                        autoColumn.ClearValue(DataGridAutoCompleteColumn.ItemTemplateProperty);
                    }
                    return true;
                case nameof(Watermark):
                    if (Watermark != null)
                    {
                        autoColumn.Watermark = Watermark;
                    }
                    else
                    {
                        autoColumn.ClearValue(DataGridAutoCompleteColumn.WatermarkProperty);
                    }
                    return true;
                case nameof(FilterMode):
                    if (FilterMode.HasValue)
                    {
                        autoColumn.FilterMode = FilterMode.Value;
                    }
                    else
                    {
                        autoColumn.ClearValue(DataGridAutoCompleteColumn.FilterModeProperty);
                    }
                    return true;
                case nameof(MinimumPrefixLength):
                    if (MinimumPrefixLength.HasValue)
                    {
                        autoColumn.MinimumPrefixLength = MinimumPrefixLength.Value;
                    }
                    else
                    {
                        autoColumn.ClearValue(DataGridAutoCompleteColumn.MinimumPrefixLengthProperty);
                    }
                    return true;
                case nameof(MinimumPopulateDelay):
                    if (MinimumPopulateDelay.HasValue)
                    {
                        autoColumn.MinimumPopulateDelay = MinimumPopulateDelay.Value;
                    }
                    else
                    {
                        autoColumn.ClearValue(DataGridAutoCompleteColumn.MinimumPopulateDelayProperty);
                    }
                    return true;
                case nameof(MaxDropDownHeight):
                    if (MaxDropDownHeight.HasValue)
                    {
                        autoColumn.MaxDropDownHeight = MaxDropDownHeight.Value;
                    }
                    else
                    {
                        autoColumn.ClearValue(DataGridAutoCompleteColumn.MaxDropDownHeightProperty);
                    }
                    return true;
                case nameof(IsTextCompletionEnabled):
                    if (IsTextCompletionEnabled.HasValue)
                    {
                        autoColumn.IsTextCompletionEnabled = IsTextCompletionEnabled.Value;
                    }
                    else
                    {
                        autoColumn.ClearValue(DataGridAutoCompleteColumn.IsTextCompletionEnabledProperty);
                    }
                    return true;
            }

            return false;
        }
    }
}
