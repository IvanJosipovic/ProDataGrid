// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia.Collections;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Utils;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Reactive;
using Avalonia.Styling;

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents a <see cref="DataGrid" /> column that hosts <see cref="ComboBox" /> controls in its cells.
    /// </summary>
#if !DATAGRID_INTERNAL
    public
#endif
    class DataGridComboBoxColumn : DataGridColumn
    {
        private IBinding _selectedItemBinding;
        private IBinding _selectedValueBinding;
        private IBinding _textBinding;
        private readonly Lazy<ControlTheme> _cellComboBoxTheme;
        private readonly Lazy<ControlTheme> _cellComboBoxDisplayTheme;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataGridComboBoxColumn"/> class.
        /// </summary>
        public DataGridComboBoxColumn()
        {
            _cellComboBoxTheme = new Lazy<ControlTheme>(() => GetThemeSafe("DataGridCellComboBoxTheme"));
            _cellComboBoxDisplayTheme = new Lazy<ControlTheme>(() => GetThemeSafe("DataGridCellComboBoxDisplayTheme"));
        }

        /// <summary>
        /// The ComboBox will attach to this ItemsSource.
        /// </summary>
        public static readonly StyledProperty<IEnumerable> ItemsSourceProperty =
            ItemsControl.ItemsSourceProperty.AddOwner<DataGridComboBoxColumn>();

        /// <summary>
        /// Gets or sets the collection used to generate the items for the combo box.
        /// </summary>
        public IEnumerable ItemsSource
        {
            get => GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        /// <summary>
        /// DisplayMemberPath is a simple way to define a default template
        /// that describes how to convert Items into UI elements by using the specified path.
        /// </summary>
        public static readonly StyledProperty<string> DisplayMemberPathProperty =
            AvaloniaProperty.Register<DataGridComboBoxColumn, string>(nameof(DisplayMemberPath), string.Empty);

        /// <summary>
        /// Gets or sets the path used to display the combo box items.
        /// </summary>
        public string DisplayMemberPath
        {
            get => GetValue(DisplayMemberPathProperty);
            set => SetValue(DisplayMemberPathProperty, value);
        }

        /// <summary>
        /// The path used to retrieve the SelectedValue from the SelectedItem.
        /// </summary>
        public static readonly StyledProperty<string> SelectedValuePathProperty =
            AvaloniaProperty.Register<DataGridComboBoxColumn, string>(nameof(SelectedValuePath), string.Empty);

        /// <summary>
        /// Gets or sets the path on the data item that is used to compute <see cref="ComboBox.SelectedValue"/>.
        /// </summary>
        public string SelectedValuePath
        {
            get => GetValue(SelectedValuePathProperty);
            set => SetValue(SelectedValuePathProperty, value);
        }

        /// <summary>
        /// The binding applied to <see cref="ComboBox.SelectedItem"/>.
        /// </summary>
        [AssignBinding]
        public IBinding SelectedItemBinding
        {
            get => _selectedItemBinding;
            set
            {
                if (_selectedItemBinding != value)
                {
                    _selectedItemBinding = PrepareBinding(value, applyValueConverter: false);
                    UpdateSortMemberPath(_selectedItemBinding);
                    NotifyPropertyChanged(nameof(SelectedItemBinding));
                }
            }
        }

        /// <summary>
        /// The binding applied to <see cref="ComboBox.SelectedValue"/>.
        /// </summary>
        [AssignBinding]
        public IBinding SelectedValueBinding
        {
            get => _selectedValueBinding;
            set
            {
                if (_selectedValueBinding != value)
                {
                    _selectedValueBinding = PrepareBinding(value, applyValueConverter: false);
                    UpdateSortMemberPath(_selectedValueBinding);
                    NotifyPropertyChanged(nameof(SelectedValueBinding));
                }
            }
        }

        /// <summary>
        /// The binding applied to <see cref="ComboBox.Text"/>.
        /// </summary>
        [AssignBinding]
        public IBinding TextBinding
        {
            get => _textBinding;
            set
            {
                if (_textBinding != value)
                {
                    _textBinding = PrepareBinding(value, applyValueConverter: true);
                    UpdateSortMemberPath(_textBinding);
                    NotifyPropertyChanged(nameof(TextBinding));
                }
            }
        }

        /// <summary>
        /// The binding that will be used to get or set cell content for the clipboard.
        /// </summary>
        public override IBinding ClipboardContentBinding
        {
            get => base.ClipboardContentBinding ?? EffectiveBinding;
            set => base.ClipboardContentBinding = value;
        }

        /// <summary>
        /// Gets the binding that should be used for edit/clipboard operations.
        /// </summary>
        private IBinding EffectiveBinding
        {
            get
            {
                if (SelectedItemBinding != null)
                {
                    return SelectedItemBinding;
                }
                else if (SelectedValueBinding != null)
                {
                    return SelectedValueBinding;
                }

                return TextBinding;
            }
        }

        private AvaloniaProperty GetEffectiveTargetProperty()
        {
            if (SelectedItemBinding != null)
            {
                return SelectingItemsControl.SelectedItemProperty;
            }

            if (SelectedValueBinding != null)
            {
                return SelectingItemsControl.SelectedValueProperty;
            }

            return ComboBox.TextProperty;
        }

        /// <inheritdoc />
        public override bool IsReadOnly
        {
            get
            {
                if (IsOneWayBinding(EffectiveBinding))
                {
                    return true;
                }

                return base.IsReadOnly;
            }
            set => base.IsReadOnly = value;
        }

        /// <summary>
        /// Causes the column cell being edited to revert to the specified value.
        /// </summary>
        /// <param name="editingElement">The element that the column displays for a cell in editing mode.</param>
        /// <param name="uneditedValue">The previous, unedited value in the cell being edited.</param>
        protected override void CancelCellEdit(Control editingElement, object uneditedValue)
        {
            if (editingElement is ComboBox comboBox)
            {
                var property = GetEffectiveTargetProperty();
                if (property == ComboBox.TextProperty)
                {
                    comboBox.Text = uneditedValue as string;
                }
                else if (property == SelectingItemsControl.SelectedItemProperty)
                {
                    comboBox.SelectedItem = uneditedValue;
                }
                else if (property == SelectingItemsControl.SelectedValueProperty)
                {
                    comboBox.SelectedValue = uneditedValue;
                }
            }
        }

        /// <summary>
        /// Gets a <see cref="ComboBox" /> control that is bound to the column's bindings.
        /// </summary>
        /// <param name="cell">The cell that will contain the generated element.</param>
        /// <param name="dataItem">The data item represented by the row that contains the intended cell.</param>
        /// <returns>A new <see cref="ComboBox" /> control that is bound to the column.</returns>
        protected override Control GenerateEditingElement(DataGridCell cell, object dataItem, out ICellEditBinding editBinding)
        {
            var comboBox = CreateComboBox(isEditing: true);

            ApplyColumnProperties(comboBox);

            editBinding = null;
            if (dataItem != DataGridCollectionView.NewItemPlaceholder)
            {
                var effectiveBinding = EffectiveBinding;
                if (effectiveBinding != null)
                {
                    editBinding = BindEditingElement(comboBox, GetEffectiveTargetProperty(), effectiveBinding);
                }

                ApplyBindings(comboBox, skipProperty: GetEffectiveTargetProperty());
            }

            return comboBox;
        }

        /// <summary>
        /// Gets a read-only <see cref="ComboBox" /> control that is bound to the column's bindings.
        /// </summary>
        /// <param name="cell">The cell that will contain the generated element.</param>
        /// <param name="dataItem">The data item represented by the row that contains the intended cell.</param>
        /// <returns>A new, read-only <see cref="ComboBox" /> control that is bound to the column.</returns>
        protected override Control GenerateElement(DataGridCell cell, object dataItem)
        {
            var comboBox = CreateComboBox(isEditing: false);

            ApplyColumnProperties(comboBox);

            if (dataItem != DataGridCollectionView.NewItemPlaceholder)
            {
                ApplyBindings(comboBox);
            }

            return comboBox;
        }

        /// <summary>
        /// Called when a cell in the column enters editing mode.
        /// </summary>
        /// <param name="editingElement">The element that the column displays for a cell in editing mode.</param>
        /// <param name="editingEventArgs">Information about the user gesture that is causing a cell to enter editing mode.</param>
        /// <returns>The unedited value. </returns>
        protected override object PrepareCellForEdit(Control editingElement, RoutedEventArgs editingEventArgs)
        {
            if (editingElement is ComboBox comboBox)
            {
                var originalValue = GetCurrentValue(comboBox);

                if (IsDropDownOpeningInput(editingEventArgs))
                {
                    comboBox.IsDropDownOpen = true;
                }

                return originalValue;
            }

            return null;
        }

        /// <summary>
        /// Called by the DataGrid control when this column asks for its elements to be updated.
        /// </summary>
        protected internal override void RefreshCellContent(Control element, string propertyName)
        {
            if (element is ComboBox comboBox)
            {
                switch (propertyName)
                {
                    case nameof(ItemsSource):
                        DataGridHelper.SyncColumnProperty(this, comboBox, ItemsSourceProperty);
                        break;
                    case nameof(DisplayMemberPath):
                        comboBox.DisplayMemberBinding = CreateDisplayMemberBinding();
                        break;
                    case nameof(SelectedValuePath):
                        comboBox.SelectedValueBinding = CreateSelectedValuePathBinding();
                        break;
                    case nameof(SelectedItemBinding):
                        ApplyBinding(comboBox, ComboBox.SelectedItemProperty, SelectedItemBinding);
                        break;
                    case nameof(SelectedValueBinding):
                        ApplyBinding(comboBox, ComboBox.SelectedValueProperty, SelectedValueBinding);
                        break;
                    case nameof(TextBinding):
                        ApplyBinding(comboBox, ComboBox.TextProperty, TextBinding);
                        break;
                    default:
                        base.RefreshCellContent(element, propertyName);
                        break;
                }
            }
            else
            {
                base.RefreshCellContent(element, propertyName);
            }
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ItemsSourceProperty ||
                change.Property == DisplayMemberPathProperty ||
                change.Property == SelectedValuePathProperty)
            {
                NotifyPropertyChanged(change.Property.Name);
            }
        }

        private void ApplyBindings(ComboBox comboBox, AvaloniaProperty skipProperty = null)
        {
            if (SelectedItemBinding != null && skipProperty != ComboBox.SelectedItemProperty)
            {
                ApplyBinding(comboBox, ComboBox.SelectedItemProperty, SelectedItemBinding);
            }

            if (SelectedValueBinding != null && skipProperty != ComboBox.SelectedValueProperty)
            {
                ApplyBinding(comboBox, ComboBox.SelectedValueProperty, SelectedValueBinding);
            }

            if (TextBinding != null && skipProperty != ComboBox.TextProperty)
            {
                ApplyBinding(comboBox, ComboBox.TextProperty, TextBinding);
            }
        }

        private void ApplyColumnProperties(ComboBox comboBox)
        {
            DataGridHelper.SyncColumnProperty(this, comboBox, ItemsSourceProperty);
            comboBox.DisplayMemberBinding = CreateDisplayMemberBinding();
            comboBox.SelectedValueBinding = CreateSelectedValuePathBinding();
        }

        private ControlTheme GetComboBoxTheme(bool isEditing) =>
            isEditing ? _cellComboBoxTheme.Value : _cellComboBoxDisplayTheme.Value;

        private ControlTheme GetThemeSafe(string key)
        {
            try
            {
                return OwningGrid.TryFindResource(key, out var theme) ? (ControlTheme)theme : null;
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }

        private ComboBox CreateComboBox(bool isEditing)
        {
            var comboBox = new ComboBox
            {
                Margin = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                IsHitTestVisible = isEditing,
                Focusable = isEditing
            };

            if (GetComboBoxTheme(isEditing) is { } theme)
            {
                comboBox.Theme = theme;
            }

            if (!isEditing)
            {
                comboBox.IsTabStop = false;
            }

            return comboBox;
        }

        private IBinding CreateDisplayMemberBinding()
        {
            if (!string.IsNullOrWhiteSpace(DisplayMemberPath))
            {
                return new Binding(DisplayMemberPath);
            }

            return null;
        }

        private IBinding CreateSelectedValuePathBinding()
        {
            if (!string.IsNullOrWhiteSpace(SelectedValuePath))
            {
                return new Binding(SelectedValuePath);
            }

            return null;
        }

        private object GetCurrentValue(ComboBox comboBox)
        {
            var property = GetEffectiveTargetProperty();
            if (property == ComboBox.TextProperty)
            {
                return comboBox.Text;
            }

            if (property == SelectingItemsControl.SelectedValueProperty)
            {
                return comboBox.SelectedValue;
            }

            return comboBox.SelectedItem;
        }

        private static bool IsDropDownOpeningInput(RoutedEventArgs editingEventArgs)
        {
            if (editingEventArgs is KeyEventArgs keyArgs)
            {
                var isAltDown = keyArgs.KeyModifiers.HasFlag(KeyModifiers.Alt);
                var key = keyArgs.Key;

                return (key == Key.F4 && !isAltDown) ||
                       ((key == Key.Up || key == Key.Down) && isAltDown);
            }

            if (editingEventArgs is PointerPressedEventArgs pointerArgs)
            {
                var properties = pointerArgs.GetCurrentPoint(null).Properties;
                return properties.IsLeftButtonPressed;
            }

            return false;
        }

        private static bool IsOneWayBinding(IBinding binding)
        {
            if (binding is BindingBase bindingBase)
            {
                return bindingBase.Mode == BindingMode.OneWay ||
                       bindingBase.Mode == BindingMode.OneTime ||
                       bindingBase.Mode == BindingMode.OneWayToSource;
            }

            return false;
        }

        private static IBinding PrepareBinding(IBinding binding, bool applyValueConverter)
        {
            if (binding is BindingBase bindingBase)
            {
                if (bindingBase.Mode == BindingMode.OneWayToSource)
                {
                    throw new InvalidOperationException("DataGridComboBoxColumn does not support BindingMode.OneWayToSource. Use BindingMode.TwoWay instead.");
                }

                if (bindingBase.Mode == BindingMode.Default)
                {
                    bindingBase.Mode = BindingMode.TwoWay;
                }

                if (applyValueConverter && bindingBase.Converter == null && string.IsNullOrEmpty(bindingBase.StringFormat))
                {
                    bindingBase.Converter = DataGridValueConverter.Instance;
                }
            }

            return binding;
        }

        private void UpdateSortMemberPath(IBinding binding)
        {
            if (!string.IsNullOrEmpty(SortMemberPath) || binding == null)
            {
                return;
            }

            var bindingPath = GetBindingPath(binding);
            if (!string.IsNullOrEmpty(bindingPath))
            {
                SortMemberPath = bindingPath;
            }
        }

        private static string GetBindingPath(IBinding binding)
        {
            return binding switch
            {
                Binding avaloniaBinding => avaloniaBinding.Path,
                CompiledBindingExtension compiled => compiled.Path?.ToString(),
                _ => null
            };
        }

        private static void ApplyBinding(AvaloniaObject target, AvaloniaProperty property, IBinding binding)
        {
            if (binding == null)
            {
                return;
            }

            var result = binding.Initiate(target, property, enableDataValidation: true);
            if (result != null)
            {
                BindingOperations.Apply(target, property, result, null);
            }
        }

        private static ICellEditBinding BindEditingElement(AvaloniaObject target, AvaloniaProperty property, IBinding binding)
        {
            var result = binding?.Initiate(target, property, enableDataValidation: true);

            if (result != null)
            {
                if (result.Source is IAvaloniaSubject<object> subject)
                {
                    var bindingHelper = new CellEditBinding(subject);
                    var instanceBinding = new InstancedBinding(bindingHelper.InternalSubject, result.Mode, result.Priority);

                    BindingOperations.Apply(target, property, instanceBinding, null);
                    return bindingHelper;
                }

                BindingOperations.Apply(target, property, result, null);
            }

            return null;
        }
    }
}
