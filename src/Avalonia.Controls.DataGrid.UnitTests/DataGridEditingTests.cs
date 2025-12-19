// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.DataGridTests;

public class DataGridEditingTests
{
    [AvaloniaFact]
    public void EditingColumnIndex_Tracks_Edit_State()
    {
        var (grid, root, items) = CreateGrid();
        try
        {
            var slot = grid.SlotFromRowIndex(0);
            var column = grid.ColumnsInternal[1];

            Assert.Equal(-1, grid.EditingColumnIndex);

            Assert.True(grid.UpdateSelectionAndCurrency(column.Index, slot, DataGridSelectionAction.SelectCurrent, scrollIntoView: false));
            grid.UpdateLayout();

            Assert.True(grid.BeginEdit());
            grid.UpdateLayout();

            Assert.Equal(column.Index, grid.EditingColumnIndex);

            var cell = FindCell(grid, items[0], column.Index);
            Assert.True(((IPseudoClasses)cell.Classes).Contains(":edited"));

            Assert.True(grid.CommitEdit());
            grid.UpdateLayout();

            Assert.Equal(-1, grid.EditingColumnIndex);
            Assert.False(((IPseudoClasses)cell.Classes).Contains(":edited"));
        }
        finally
        {
            root.Close();
        }
    }

    [AvaloniaFact]
    public void Edited_PseudoClass_Follows_Editing_Column_For_All_Types()
    {
        var (grid, root, items) = CreateGrid();
        try
        {
            var slot = grid.SlotFromRowIndex(0);
            var item = items[0];

            var expectations = new (int ColumnIndex, Type ExpectedContent)[]
            {
                (1, typeof(TextBox)),
                (2, typeof(CheckBox)),
                (3, typeof(ComboBox)),
                (4, typeof(TextBox)),
                (5, typeof(TextBox))
            };

            foreach (var (columnIndex, expectedContent) in expectations)
            {
                var column = grid.ColumnsInternal[columnIndex];
                Assert.True(grid.UpdateSelectionAndCurrency(column.Index, slot, DataGridSelectionAction.SelectCurrent, scrollIntoView: false));
                grid.UpdateLayout();

                Assert.True(grid.BeginEdit());
                grid.UpdateLayout();

                Assert.Equal(column.Index, grid.EditingColumnIndex);

                var row = FindRow(item, grid);
                var editedCell = FindCell(grid, item, column.Index);
                Assert.IsType(expectedContent, editedCell.Content);

                AssertOnlyEditedCellHasPseudo(row, editedCell);

                Assert.True(grid.CommitEdit());
                grid.UpdateLayout();
            }
        }
        finally
        {
            root.Close();
        }
    }

    [AvaloniaFact]
    public void ReadOnly_Cell_Does_Not_Get_Edited_Pseudo()
    {
        var (grid, root, items) = CreateGrid();
        try
        {
            var slot = grid.SlotFromRowIndex(0);
            var editableColumn = grid.ColumnsInternal[1];

            Assert.True(grid.UpdateSelectionAndCurrency(editableColumn.Index, slot, DataGridSelectionAction.SelectCurrent, scrollIntoView: false));
            grid.UpdateLayout();

            Assert.True(grid.BeginEdit());
            grid.UpdateLayout();

            var readOnlyCell = FindCell(grid, items[0], grid.ColumnsInternal[0].Index);
            Assert.False(((IPseudoClasses)readOnlyCell.Classes).Contains(":edited"));

            var editedCell = FindCell(grid, items[0], editableColumn.Index);
            Assert.True(((IPseudoClasses)editedCell.Classes).Contains(":edited"));
        }
        finally
        {
            root.Close();
        }
    }

    [AvaloniaFact]
    public void Template_Column_Without_EditingTemplate_Is_Not_Editable()
    {
        var (grid, root, items) = CreateTemplateDisplayGrid();
        try
        {
            var slot = grid.SlotFromRowIndex(0);
            var templateColumn = grid.ColumnsInternal[1];

            Assert.True(grid.UpdateSelectionAndCurrency(templateColumn.Index, slot, DataGridSelectionAction.SelectCurrent, scrollIntoView: false));
            grid.UpdateLayout();

            Assert.False(grid.BeginEdit());
            grid.UpdateLayout();

            Assert.Equal(-1, grid.EditingColumnIndex);

            var templateCell = FindCell(grid, items[0], templateColumn.Index);
            Assert.False(((IPseudoClasses)templateCell.Classes).Contains(":edited"));
        }
        finally
        {
            root.Close();
        }
    }

    [AvaloniaFact]
    public void Double_Click_On_ReadOnly_Cell_Does_Not_Begin_Edit()
    {
        var (grid, root, items) = CreateGrid();
        try
        {
            var slot = grid.SlotFromRowIndex(0);
            var readOnlyColumn = grid.ColumnsInternal[0];

            InvokePrivateUpdateStateOnMouseLeftButtonDown(grid, CreatePointerArgs(grid), readOnlyColumn.Index, slot, allowEdit: true);
            grid.UpdateLayout();

            InvokePrivateUpdateStateOnMouseLeftButtonDown(grid, CreatePointerArgs(grid), readOnlyColumn.Index, slot, allowEdit: true);
            grid.UpdateLayout();

            Assert.Equal(-1, grid.EditingColumnIndex);

            var readOnlyCell = FindCell(grid, items[0], readOnlyColumn.Index);
            Assert.False(((IPseudoClasses)readOnlyCell.Classes).Contains(":edited"));
        }
        finally
        {
            root.Close();
        }
    }

    [AvaloniaFact]
    public void Double_Click_On_DisplayOnly_Template_Cell_Does_Not_Begin_Edit()
    {
        var (grid, root, items) = CreateTemplateDisplayGrid();
        try
        {
            var slot = grid.SlotFromRowIndex(0);
            var templateColumn = grid.ColumnsInternal[1];

            InvokePrivateUpdateStateOnMouseLeftButtonDown(grid, CreatePointerArgs(grid), templateColumn.Index, slot, allowEdit: true);
            grid.UpdateLayout();

            InvokePrivateUpdateStateOnMouseLeftButtonDown(grid, CreatePointerArgs(grid), templateColumn.Index, slot, allowEdit: true);
            grid.UpdateLayout();

            Assert.Equal(-1, grid.EditingColumnIndex);

            var templateCell = FindCell(grid, items[0], templateColumn.Index);
            Assert.False(((IPseudoClasses)templateCell.Classes).Contains(":edited"));
        }
        finally
        {
            root.Close();
        }
    }

    private static (DataGrid Grid, Window Root, ObservableCollection<EditItem> Items) CreateGrid()
    {
        var items = new ObservableCollection<EditItem>
        {
            new("Alpha", true, "One", "https://example.com", "TemplateA")
        };

        var root = new Window
        {
            Width = 640,
            Height = 480
        };

        root.SetThemeStyles();

        var grid = new DataGrid
        {
            AutoGenerateColumns = false,
            ItemsSource = items,
            SelectionMode = DataGridSelectionMode.Extended,
            SelectionUnit = DataGridSelectionUnit.Cell
        };

        grid.ColumnsInternal.Add(new DataGridTextColumn
        {
            Header = "ReadOnly Text",
            Binding = new Binding(nameof(EditItem.Text)),
            IsReadOnly = true
        });

        grid.ColumnsInternal.Add(new DataGridTextColumn
        {
            Header = "Text",
            Binding = new Binding(nameof(EditItem.Text))
        });

        grid.ColumnsInternal.Add(new DataGridCheckBoxColumn
        {
            Header = "Flag",
            Binding = new Binding(nameof(EditItem.Flag))
        });

        grid.ColumnsInternal.Add(new DataGridComboBoxColumn
        {
            Header = "Choice",
            ItemsSource = new[] { "One", "Two", "Three" },
            SelectedItemBinding = new Binding(nameof(EditItem.Choice))
        });

        grid.ColumnsInternal.Add(new DataGridHyperlinkColumn
        {
            Header = "Link",
            Binding = new Binding(nameof(EditItem.Link))
        });

        grid.ColumnsInternal.Add(new DataGridTemplateColumn
        {
            Header = "Template",
            CellTemplate = new FuncDataTemplate<EditItem>((item, _) => new TextBlock { Text = item.TemplateValue }),
            CellEditingTemplate = new FuncDataTemplate<EditItem>((_, _) =>
            {
                var editor = new TextBox();
                editor.Bind(TextBox.TextProperty, new Binding(nameof(EditItem.TemplateValue)) { Mode = BindingMode.TwoWay });
                return editor;
            })
        });

        root.Content = grid;
        root.Show();
        grid.UpdateLayout();
        return (grid, root, items);
    }

    private static (DataGrid Grid, Window Root, ObservableCollection<EditItem> Items) CreateTemplateDisplayGrid()
    {
        var items = new ObservableCollection<EditItem>
        {
            new("Alpha", true, "One", "https://example.com", "TemplateA")
        };

        var root = new Window
        {
            Width = 320,
            Height = 240
        };

        root.SetThemeStyles();

        var grid = new DataGrid
        {
            AutoGenerateColumns = false,
            ItemsSource = items,
            SelectionMode = DataGridSelectionMode.Single,
            SelectionUnit = DataGridSelectionUnit.Cell
        };

        grid.ColumnsInternal.Add(new DataGridTextColumn
        {
            Header = "Text",
            Binding = new Binding(nameof(EditItem.Text))
        });

        grid.ColumnsInternal.Add(new DataGridTemplateColumn
        {
            Header = "Template",
            CellTemplate = new FuncDataTemplate<EditItem>((item, _) => new TextBlock { Text = item.TemplateValue })
        });

        root.Content = grid;
        root.Show();
        grid.UpdateLayout();
        return (grid, root, items);
    }

    private static DataGridRow FindRow(EditItem item, DataGrid grid)
    {
        return grid
            .GetSelfAndVisualDescendants()
            .OfType<DataGridRow>()
            .First(r => ReferenceEquals(r.DataContext, item));
    }

    private static DataGridCell FindCell(DataGrid grid, EditItem item, int columnIndex)
    {
        var row = FindRow(item, grid);
        for (var i = 0; i < row.Cells.Count; i++)
        {
            var cell = row.Cells[i];
            if (cell.OwningColumn?.Index == columnIndex)
            {
                return cell;
            }
        }

        throw new InvalidOperationException($"Could not find cell for column {columnIndex}.");
    }

    private static void AssertOnlyEditedCellHasPseudo(DataGridRow row, DataGridCell editedCell)
    {
        for (var i = 0; i < row.Cells.Count; i++)
        {
            var cell = row.Cells[i];
            if (cell?.OwningColumn == null)
            {
                continue;
            }

            var classes = (IPseudoClasses)cell.Classes;
            if (ReferenceEquals(cell, editedCell))
            {
                Assert.True(classes.Contains(":edited"));
            }
            else
            {
                Assert.False(classes.Contains(":edited"));
            }
        }
    }

    private static PointerPressedEventArgs CreatePointerArgs(Control target)
    {
        var pointer = new Avalonia.Input.Pointer(Avalonia.Input.Pointer.GetNextFreeId(), PointerType.Mouse, isPrimary: true);
        var properties = new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed);
        return new PointerPressedEventArgs(target, pointer, target, new Point(0, 0), 0, properties, KeyModifiers.None);
    }

    private static void InvokePrivateUpdateStateOnMouseLeftButtonDown(DataGrid grid, PointerPressedEventArgs args, int columnIndex, int slot, bool allowEdit)
    {
        var method = typeof(DataGrid).GetMethod(
            "UpdateStateOnMouseLeftButtonDown",
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(PointerPressedEventArgs), typeof(int), typeof(int), typeof(bool) },
            modifiers: null);

        Assert.NotNull(method);
        method!.Invoke(grid, new object[] { args, columnIndex, slot, allowEdit });
    }

    private class EditItem
    {
        public EditItem(string text, bool flag, string choice, string link, string templateValue)
        {
            Text = text;
            Flag = flag;
            Choice = choice;
            Link = link;
            TemplateValue = templateValue;
        }

        public string Text { get; set; }

        public bool Flag { get; set; }

        public string Choice { get; set; }

        public string Link { get; set; }

        public string TemplateValue { get; set; }
    }
}
