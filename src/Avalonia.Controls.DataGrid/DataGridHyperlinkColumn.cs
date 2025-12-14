// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable disable

using System;
using System.Collections.Generic;
using Avalonia.Collections;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents a <see cref="DataGridBoundColumn" /> that displays hyperlinks.
    /// </summary>
#if !DATAGRID_INTERNAL
    public
#endif
    class DataGridHyperlinkColumn : DataGridBoundColumn
    {
        private IBinding _contentBinding;
        private readonly Lazy<ControlTheme> _cellHyperlinkButtonTheme;
        private readonly Lazy<ControlTheme> _cellTextBoxTheme;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataGridHyperlinkColumn"/> class.
        /// </summary>
        public DataGridHyperlinkColumn()
        {
            BindingTarget = TextBox.TextProperty;
            _cellHyperlinkButtonTheme = new Lazy<ControlTheme>(() => GetThemeSafe("DataGridCellHyperlinkButtonTheme"));
            _cellTextBoxTheme = new Lazy<ControlTheme>(() => GetThemeSafe("DataGridCellTextBoxTheme"));
        }

        /// <summary>
        /// Defines the <see cref="TargetName"/> property.
        /// </summary>
        public static readonly StyledProperty<string> TargetNameProperty =
            AvaloniaProperty.Register<DataGridHyperlinkColumn, string>(nameof(TargetName));

        /// <summary>
        /// Gets or sets the target name annotation for the hyperlink.
        /// </summary>
        public string TargetName
        {
            get => GetValue(TargetNameProperty);
            set => SetValue(TargetNameProperty, value);
        }

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

        /// <summary>
        /// Gets or sets the binding for the displayed hyperlink content.
        /// </summary>
        [AssignBinding]
        public IBinding ContentBinding
        {
            get => _contentBinding;
            set
            {
                if (_contentBinding != value)
                {
                    _contentBinding = PrepareBinding(value);
                    NotifyPropertyChanged(nameof(ContentBinding));
                }
            }
        }

        /// <summary>
        /// The binding that will be used to get or set cell content for the clipboard.
        /// </summary>
        public override IBinding ClipboardContentBinding
        {
            get
            {
                var baseBinding = base.ClipboardContentBinding;
                if (baseBinding != null && !ReferenceEquals(baseBinding, Binding))
                {
                    return baseBinding;
                }

                return ContentBinding ?? baseBinding ?? Binding;
            }
            set => base.ClipboardContentBinding = value;
        }

        /// <summary>
        /// Called by the DataGrid control when this column asks for its elements to be updated.
        /// </summary>
        protected internal override void RefreshCellContent(Control element, string propertyName)
        {
            if (element is HyperlinkButton hyperlink)
            {
                switch (propertyName)
                {
                    case nameof(ContentBinding):
                        ApplyContentBinding(hyperlink);
                        break;
                    case nameof(TargetName):
                        ApplyTargetName(hyperlink);
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

        /// <summary>
        /// Causes the column cell being edited to revert to the specified value.
        /// </summary>
        /// <param name="editingElement">The element that the column displays for a cell in editing mode.</param>
        /// <param name="uneditedValue">The previous, unedited value in the cell being edited.</param>
        protected override void CancelCellEdit(Control editingElement, object uneditedValue)
        {
            if (editingElement is TextBox textBox)
            {
                textBox.Text = uneditedValue as string ?? string.Empty;
            }
        }

        /// <summary>
        /// Creates the visual tree for read-only cells.
        /// </summary>
        protected override Control GenerateElement(DataGridCell cell, object dataItem)
        {
            var hyperlink = new HyperlinkButton
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(0),
                Background = Brushes.Transparent,
                BorderBrush = null,
                IsEnabled = dataItem != DataGridCollectionView.NewItemPlaceholder
            };

            if (_cellHyperlinkButtonTheme.Value is { } theme)
            {
                hyperlink.Theme = theme;
            }

            ApplyTargetName(hyperlink);

            if (Binding != null && dataItem != DataGridCollectionView.NewItemPlaceholder)
            {
                ApplyBinding(hyperlink, HyperlinkButton.NavigateUriProperty, Binding);
            }

            if (dataItem != DataGridCollectionView.NewItemPlaceholder)
            {
                ApplyContentBinding(hyperlink);
            }

            return hyperlink;
        }

        /// <summary>
        /// Creates the visual tree for editing cells.
        /// </summary>
        protected override Control GenerateEditingElementDirect(DataGridCell cell, object dataItem)
        {
            var textBox = new TextBox
            {
                Name = "CellHyperlinkTextBox"
            };

            if (_cellTextBoxTheme.Value is { } theme)
            {
                textBox.Theme = theme;
            }

            return textBox;
        }

        /// <summary>
        /// Called when a cell in the column enters editing mode.
        /// </summary>
        /// <param name="editingElement">The element that the column displays for a cell in editing mode.</param>
        /// <param name="editingEventArgs">Information about the user gesture that is causing a cell to enter editing mode.</param>
        /// <returns>The unedited value.</returns>
        protected override object PrepareCellForEdit(Control editingElement, RoutedEventArgs editingEventArgs)
        {
            if (editingElement is TextBox textBox)
            {
                var uneditedText = textBox.Text ?? string.Empty;
                int len = uneditedText.Length;
                if (editingEventArgs is KeyEventArgs keyEventArgs && keyEventArgs.Key == Key.F2)
                {
                    textBox.SelectionStart = len;
                    textBox.SelectionEnd = len;
                }
                else
                {
                    textBox.SelectionStart = 0;
                    textBox.SelectionEnd = len;
                    textBox.CaretIndex = len;
                }

                return uneditedText;
            }

            return string.Empty;
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == TargetNameProperty)
            {
                NotifyPropertyChanged(change.Property.Name);
            }
        }

        private void ApplyContentBinding(ContentControl contentTarget)
        {
            if (ContentBinding != null)
            {
                ApplyBinding(contentTarget, ContentControl.ContentProperty, ContentBinding);
            }
            else if (Binding != null)
            {
                ApplyBinding(contentTarget, ContentControl.ContentProperty, Binding);
            }
            else
            {
                contentTarget.ClearValue(ContentControl.ContentProperty);
            }
        }

        private void ApplyTargetName(HyperlinkButton hyperlink)
        {
            if (TargetName != null)
            {
                hyperlink.Name = TargetName;
            }
            else
            {
                hyperlink.ClearValue(Control.NameProperty);
            }
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

        private static IBinding PrepareBinding(IBinding binding)
        {
            if (binding is BindingBase bindingBase && bindingBase.Mode == BindingMode.OneWayToSource)
            {
                throw new InvalidOperationException("DataGridHyperlinkColumn does not support BindingMode.OneWayToSource. Use BindingMode.TwoWay instead.");
            }

            return binding;
        }

    }
}
