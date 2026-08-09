// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable enable

using Avalonia.Controls.DataGridFormulas;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Avalonia.Controls
{
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    sealed class DataGridFormulaTextColumn : DataGridTextColumn
    {
        internal DataGridFormulaColumnDefinition? FormulaDefinition { get; set; }

        internal override bool CanReuseCellContentOnDataContextChange => true;

        protected override object PrepareCellForEdit(Control editingElement, RoutedEventArgs editingEventArgs)
        {
            var unedited = base.PrepareCellForEdit(editingElement, editingEventArgs);
            if (editingElement is TextBox textBox && FormulaDefinition != null)
            {
                if (OwningGrid?.FormulaModel is IDataGridFormulaModel model && FormulaDefinition.AllowCellFormulas)
                {
                    var item = textBox.DataContext;
                    var formula = item != null ? model.GetCellFormula(item, FormulaDefinition) : null;
                    if (!string.IsNullOrWhiteSpace(formula))
                    {
                        textBox.Text = formula;
                        var length = textBox.Text?.Length ?? 0;
                        if (editingEventArgs is KeyEventArgs keyEventArgs && keyEventArgs.Key == Key.F2)
                        {
                            textBox.SelectionStart = length;
                            textBox.SelectionEnd = length;
                        }
                        else
                        {
                            textBox.SelectionStart = 0;
                            textBox.SelectionEnd = length;
                            textBox.CaretIndex = length;
                        }
                    }
                }
            }

            return unedited;
        }
    }
}
