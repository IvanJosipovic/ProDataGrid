// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable disable

using System.Collections;
using Avalonia.Controls.Templates;
using Avalonia.Layout;

namespace Avalonia.Controls
{
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    sealed class DataGridComboBoxColumnDefinition : DataGridColumnDefinition
    {
        private IEnumerable _itemsSource;
        private string _itemTemplateKey;
        private bool? _isEditable;
        private HorizontalAlignment? _horizontalContentAlignment;
        private VerticalAlignment? _verticalContentAlignment;
        private string _displayMemberPath;
        private string _selectedValuePath;
        private DataGridBindingDefinition _selectedItemBinding;
        private DataGridBindingDefinition _selectedValueBinding;
        private DataGridBindingDefinition _textBinding;
        private IDataGridColumnValueAccessor _bindingAccessor;
        private System.Type _bindingValueType;

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

        public bool? IsEditable
        {
            get => _isEditable;
            set => SetProperty(ref _isEditable, value);
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

        public string DisplayMemberPath
        {
            get => _displayMemberPath;
            set => SetProperty(ref _displayMemberPath, value);
        }

        public string SelectedValuePath
        {
            get => _selectedValuePath;
            set => SetProperty(ref _selectedValuePath, value);
        }

        public DataGridBindingDefinition SelectedItemBinding
        {
            get => _selectedItemBinding;
            set
            {
                var previous = _selectedItemBinding;
                if (SetProperty(ref _selectedItemBinding, value))
                {
                    UpdateBindingMetadata(previous);
                }
            }
        }

        public DataGridBindingDefinition SelectedValueBinding
        {
            get => _selectedValueBinding;
            set
            {
                var previous = _selectedValueBinding;
                if (SetProperty(ref _selectedValueBinding, value))
                {
                    UpdateBindingMetadata(previous);
                }
            }
        }

        public DataGridBindingDefinition TextBinding
        {
            get => _textBinding;
            set
            {
                var previous = _textBinding;
                if (SetProperty(ref _textBinding, value))
                {
                    UpdateBindingMetadata(previous);
                }
            }
        }

        private void UpdateBindingMetadata(DataGridBindingDefinition previousBinding)
        {
            var accessor = SelectedItemBinding?.ValueAccessor
                ?? SelectedValueBinding?.ValueAccessor
                ?? TextBinding?.ValueAccessor;

            var valueType = SelectedItemBinding?.ValueType
                ?? SelectedValueBinding?.ValueType
                ?? TextBinding?.ValueType;

            if (ValueAccessor == null || ReferenceEquals(ValueAccessor, _bindingAccessor) || ReferenceEquals(ValueAccessor, previousBinding?.ValueAccessor))
            {
                ValueAccessor = accessor;
            }

            if (ValueType == null || ReferenceEquals(ValueType, _bindingValueType) || ReferenceEquals(ValueType, previousBinding?.ValueType))
            {
                ValueType = valueType;
            }

            _bindingAccessor = accessor;
            _bindingValueType = valueType;
        }

        protected override DataGridColumn CreateColumnCore()
        {
            return new DataGridComboBoxColumn();
        }

        protected override void ApplyColumnProperties(DataGridColumn column, DataGridColumnDefinitionContext context)
        {
            if (column is DataGridComboBoxColumn comboColumn)
            {
                if (ItemsSource != null)
                {
                    comboColumn.ItemsSource = ItemsSource;
                }
                else
                {
                    comboColumn.ClearValue(DataGridComboBoxColumn.ItemsSourceProperty);
                }

                if (DisplayMemberPath != null)
                {
                    comboColumn.DisplayMemberPath = DisplayMemberPath;
                }
                else
                {
                    comboColumn.ClearValue(DataGridComboBoxColumn.DisplayMemberPathProperty);
                }

                if (SelectedValuePath != null)
                {
                    comboColumn.SelectedValuePath = SelectedValuePath;
                }
                else
                {
                    comboColumn.ClearValue(DataGridComboBoxColumn.SelectedValuePathProperty);
                }

                if (ItemTemplateKey != null)
                {
                    comboColumn.ItemTemplate = context?.ResolveResource<IDataTemplate>(ItemTemplateKey);
                }
                else
                {
                    comboColumn.ClearValue(DataGridComboBoxColumn.ItemTemplateProperty);
                }

                if (IsEditable.HasValue)
                {
                    comboColumn.IsEditable = IsEditable.Value;
                }
                else
                {
                    comboColumn.ClearValue(DataGridComboBoxColumn.IsEditableProperty);
                }

                if (HorizontalContentAlignment.HasValue)
                {
                    comboColumn.HorizontalContentAlignment = HorizontalContentAlignment.Value;
                }
                else
                {
                    comboColumn.ClearValue(DataGridComboBoxColumn.HorizontalContentAlignmentProperty);
                }

                if (VerticalContentAlignment.HasValue)
                {
                    comboColumn.VerticalContentAlignment = VerticalContentAlignment.Value;
                }
                else
                {
                    comboColumn.ClearValue(DataGridComboBoxColumn.VerticalContentAlignmentProperty);
                }

                comboColumn.SelectedItemBinding = SelectedItemBinding?.CreateBinding();
                comboColumn.SelectedValueBinding = SelectedValueBinding?.CreateBinding();
                comboColumn.TextBinding = TextBinding?.CreateBinding();
            }

            if (ValueAccessor == null)
            {
                var accessor = SelectedItemBinding?.ValueAccessor
                    ?? SelectedValueBinding?.ValueAccessor
                    ?? TextBinding?.ValueAccessor;

                var valueType = SelectedItemBinding?.ValueType
                    ?? SelectedValueBinding?.ValueType
                    ?? TextBinding?.ValueType;

                if (accessor != null && (ValueType == null || ValueType == valueType))
                {
                    DataGridColumnMetadata.SetValueAccessor(column, accessor);
                }
            }
        }

        protected override bool ApplyColumnPropertyChange(
            DataGridColumn column,
            DataGridColumnDefinitionContext context,
            string propertyName)
        {
            if (column is not DataGridComboBoxColumn comboColumn)
            {
                return false;
            }

            switch (propertyName)
            {
                case nameof(ItemsSource):
                    if (ItemsSource != null)
                    {
                        comboColumn.ItemsSource = ItemsSource;
                    }
                    else
                    {
                        comboColumn.ClearValue(DataGridComboBoxColumn.ItemsSourceProperty);
                    }
                    return true;
                case nameof(DisplayMemberPath):
                    if (DisplayMemberPath != null)
                    {
                        comboColumn.DisplayMemberPath = DisplayMemberPath;
                    }
                    else
                    {
                        comboColumn.ClearValue(DataGridComboBoxColumn.DisplayMemberPathProperty);
                    }
                    return true;
                case nameof(SelectedValuePath):
                    if (SelectedValuePath != null)
                    {
                        comboColumn.SelectedValuePath = SelectedValuePath;
                    }
                    else
                    {
                        comboColumn.ClearValue(DataGridComboBoxColumn.SelectedValuePathProperty);
                    }
                    return true;
                case nameof(ItemTemplateKey):
                    if (ItemTemplateKey != null)
                    {
                        comboColumn.ItemTemplate = context?.ResolveResource<IDataTemplate>(ItemTemplateKey);
                    }
                    else
                    {
                        comboColumn.ClearValue(DataGridComboBoxColumn.ItemTemplateProperty);
                    }
                    return true;
                case nameof(IsEditable):
                    if (IsEditable.HasValue)
                    {
                        comboColumn.IsEditable = IsEditable.Value;
                    }
                    else
                    {
                        comboColumn.ClearValue(DataGridComboBoxColumn.IsEditableProperty);
                    }
                    return true;
                case nameof(HorizontalContentAlignment):
                    if (HorizontalContentAlignment.HasValue)
                    {
                        comboColumn.HorizontalContentAlignment = HorizontalContentAlignment.Value;
                    }
                    else
                    {
                        comboColumn.ClearValue(DataGridComboBoxColumn.HorizontalContentAlignmentProperty);
                    }
                    return true;
                case nameof(VerticalContentAlignment):
                    if (VerticalContentAlignment.HasValue)
                    {
                        comboColumn.VerticalContentAlignment = VerticalContentAlignment.Value;
                    }
                    else
                    {
                        comboColumn.ClearValue(DataGridComboBoxColumn.VerticalContentAlignmentProperty);
                    }
                    return true;
                case nameof(SelectedItemBinding):
                    comboColumn.SelectedItemBinding = SelectedItemBinding?.CreateBinding();
                    ApplyBindingMetadata(column);
                    return true;
                case nameof(SelectedValueBinding):
                    comboColumn.SelectedValueBinding = SelectedValueBinding?.CreateBinding();
                    ApplyBindingMetadata(column);
                    return true;
                case nameof(TextBinding):
                    comboColumn.TextBinding = TextBinding?.CreateBinding();
                    ApplyBindingMetadata(column);
                    return true;
                case nameof(ValueAccessor):
                case nameof(ValueType):
                    ApplyBindingMetadata(column);
                    return true;
            }

            return false;
        }

        private void ApplyBindingMetadata(DataGridColumn column)
        {
            if (ValueAccessor != null)
            {
                return;
            }

            var accessor = SelectedItemBinding?.ValueAccessor
                ?? SelectedValueBinding?.ValueAccessor
                ?? TextBinding?.ValueAccessor;

            var valueType = SelectedItemBinding?.ValueType
                ?? SelectedValueBinding?.ValueType
                ?? TextBinding?.ValueType;

            if (accessor != null && (ValueType == null || ValueType == valueType))
            {
                DataGridColumnMetadata.SetValueAccessor(column, accessor);
            }
        }
    }
}
