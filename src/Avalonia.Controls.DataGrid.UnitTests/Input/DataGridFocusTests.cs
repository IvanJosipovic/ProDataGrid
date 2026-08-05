// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.DataGridTests;

public class DataGridFocusTests
{
    [AvaloniaFact]
    public void ContextMenu_Focus_Does_Not_Commit_Active_Text_Edit()
    {
        var item = new FocusItem { Text = "Original" };
        var grid = new DataGrid
        {
            AutoGenerateColumns = false,
            ItemsSource = new ObservableCollection<FocusItem> { item },
            SelectionMode = DataGridSelectionMode.Extended,
            SelectionUnit = DataGridSelectionUnit.Cell
        };
        grid.ColumnsInternal.Add(new DataGridTextColumn
        {
            Binding = new Binding(nameof(FocusItem.Text)),
            Width = new DataGridLength(160)
        });

        var root = new Window
        {
            Width = 320,
            Height = 180
        };
        root.SetThemeStyles();
        root.Content = grid;

        var menuItem = new MenuItem { Header = "Copy" };
        var contextMenu = new ContextMenu();
        contextMenu.Items.Add(menuItem);

        try
        {
            root.Show();
            Dispatcher.UIThread.RunJobs();
            grid.ApplyTemplate();
            grid.UpdateLayout();

            Assert.True(grid.UpdateSelectionAndCurrency(
                columnIndex: 0,
                slot: grid.SlotFromRowIndex(0),
                DataGridSelectionAction.SelectCurrent,
                scrollIntoView: false));
            grid.UpdateLayout();
            Assert.True(grid.BeginEdit());
            grid.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var row = grid.GetSelfAndVisualDescendants()
                .OfType<DataGridRow>()
                .Single(candidate => ReferenceEquals(candidate.DataContext, item));
            var cell = Enumerable.Range(0, row.Cells.Count)
                .Select(index => row.Cells[index])
                .Single(candidate => candidate.OwningColumn is DataGridTextColumn);
            var editor = Assert.IsType<TextBox>(cell.Content);
            editor.ContextMenu = contextMenu;
            Assert.True(editor.Focus());

            contextMenu.Open(editor);
            Dispatcher.UIThread.RunJobs();
            Assert.True(menuItem.Focus());
            Dispatcher.UIThread.RunJobs();

            Assert.True(contextMenu.IsOpen);
            Assert.Equal(0, grid.EditingColumnIndex);
            Assert.True(grid.IsFocusWithinDataGrid(menuItem, out var dataGridWillReceiveRoutedEvent));
            Assert.False(dataGridWillReceiveRoutedEvent);
        }
        finally
        {
            contextMenu.Close();
            root.Close();
        }
    }

    [AvaloniaFact]
    public void IsFocusWithinDataGrid_Does_Not_Treat_Another_Window_As_InternalFocus()
    {
        var grid = new DataGrid();
        var externalControl = new Button();
        var gridWindow = new Window { Content = grid };
        var externalWindow = new Window { Content = externalControl };

        try
        {
            gridWindow.Show();
            externalWindow.Show();

            var containsFocus = grid.IsFocusWithinDataGrid(
                externalControl,
                out var dataGridWillReceiveRoutedEvent);

            Assert.False(containsFocus);
            Assert.True(dataGridWillReceiveRoutedEvent);
        }
        finally
        {
            externalWindow.Close();
            gridWindow.Close();
        }
    }

    [Fact]
    public void IsFocusWithinDataGrid_Treats_LogicalDescendant_As_InternalFocus()
    {
        var grid = new DataGrid();
        var logicalHost = new Border();
        var popupEditor = new TextBox();

        SetLogicalParent(logicalHost, grid);
        SetLogicalParent(popupEditor, logicalHost);

        var containsFocus = grid.IsFocusWithinDataGrid(popupEditor, out var dataGridWillReceiveRoutedEvent);

        Assert.True(containsFocus);
        Assert.False(dataGridWillReceiveRoutedEvent);
    }

    [Fact]
    public void IsFocusWithinDataGrid_Breaks_LogicalParent_Cycles()
    {
        var grid = new DataGrid();
        var first = new Border();
        var second = new Border();

        SetLogicalParent(first, second);
        SetLogicalParent(second, first);

        var containsFocus = grid.IsFocusWithinDataGrid(first, out var dataGridWillReceiveRoutedEvent);

        Assert.False(containsFocus);
        Assert.True(dataGridWillReceiveRoutedEvent);
    }

    private static void SetLogicalParent(StyledElement element, StyledElement? parent)
    {
        var property = typeof(StyledElement).GetProperty("Parent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(property);
        property!.SetValue(element, parent);
    }

    private sealed class FocusItem
    {
        public string Text { get; set; } = string.Empty;
    }
}
