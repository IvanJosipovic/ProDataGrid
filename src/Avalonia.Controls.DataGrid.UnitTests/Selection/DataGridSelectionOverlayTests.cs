// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.DataGridTests;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.DataGridTests.Selection;

public class DataGridSelectionOverlayTests
{
    [AvaloniaFact]
    public void FillHandle_Hidden_When_Selection_Extends_Beyond_Viewport()
    {
        var (window, grid, items) = CreateGrid(itemCount: 20, height: 120);
        try
        {
            SelectRange(grid, items, 0, items.Count - 1);
            grid.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var fillHandle = GetFillHandle(grid);
            Assert.False(fillHandle.IsVisible);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void FillHandle_Visible_When_Selection_Fully_Visible()
    {
        var (window, grid, items) = CreateGrid(itemCount: 3, height: 240);
        try
        {
            SelectRange(grid, items, 0, 1);
            grid.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var fillHandle = GetFillHandle(grid);
            Assert.True(fillHandle.IsVisible);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void FillHandle_Hidden_After_Column_Move_Invalidates_Display_Range()
    {
        var (window, grid, items) = CreateGrid(itemCount: 4, height: 260);
        try
        {
            SelectRectangle(grid, items, startRow: 0, endRow: 1, startColumn: 0, endColumn: 1);
            grid.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var fillHandle = GetFillHandle(grid);
            Assert.True(fillHandle.IsVisible);

            // Move the selected second column after an unselected column,
            // which breaks the contiguous display rectangle.
            grid.ColumnsInternal[1].DisplayIndex = 2;
            grid.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.False(fillHandle.IsVisible);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void FillHandle_Hidden_After_Row_Move_Invalidates_Row_Range()
    {
        var (window, grid, items) = CreateGrid(itemCount: 4, height: 260);
        try
        {
            SelectRectangle(grid, items, startRow: 0, endRow: 1, startColumn: 0, endColumn: 1);
            grid.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var fillHandle = GetFillHandle(grid);
            Assert.True(fillHandle.IsVisible);

            // Move one selected row outside the selected block, making row indexes non-contiguous.
            items.Move(0, 3);
            grid.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.False(fillHandle.IsVisible);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void FillHandle_Recovers_After_Hide_And_Show_Selected_Column()
    {
        var (window, grid, items) = CreateGrid(itemCount: 4, height: 260);
        try
        {
            SelectRectangle(grid, items, startRow: 0, endRow: 1, startColumn: 0, endColumn: 1);
            grid.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var fillHandle = GetFillHandle(grid);
            Assert.True(fillHandle.IsVisible);

            grid.ColumnsInternal[1].IsVisible = false;
            grid.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            Assert.False(fillHandle.IsVisible);

            grid.ColumnsInternal[1].IsVisible = true;
            grid.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            Assert.True(fillHandle.IsVisible);
        }
        finally
        {
            window.Close();
        }
    }

    private static (Window Window, DataGrid Grid, ObservableCollection<RowItem> Items) CreateGrid(int itemCount, double height)
    {
        var items = new ObservableCollection<RowItem>();
        for (var i = 0; i < itemCount; i++)
        {
            items.Add(new RowItem($"Item {i}", $"Code {i}", $"Group {i % 2}"));
        }

        var window = new Window
        {
            Width = 320,
            Height = height
        };

        window.SetThemeStyles(DataGridTheme.SimpleV2);

        var grid = new DataGrid
        {
            ItemsSource = items,
            SelectionUnit = DataGridSelectionUnit.Cell,
            SelectionMode = DataGridSelectionMode.Extended,
            AutoGenerateColumns = false,
            CanUserAddRows = false,
            HeadersVisibility = DataGridHeadersVisibility.All
        };

        grid.ColumnsInternal.Add(new DataGridTextColumn
        {
            Header = "Name",
            Binding = new Binding(nameof(RowItem.Name))
        });
        grid.ColumnsInternal.Add(new DataGridTextColumn
        {
            Header = "Code",
            Binding = new Binding(nameof(RowItem.Code))
        });
        grid.ColumnsInternal.Add(new DataGridTextColumn
        {
            Header = "Group",
            Binding = new Binding(nameof(RowItem.Group))
        });

        window.Content = grid;
        window.Show();
        grid.UpdateLayout();
        Dispatcher.UIThread.RunJobs();

        return (window, grid, items);
    }

    private static void SelectRange(DataGrid grid, ObservableCollection<RowItem> items, int startRow, int endRow)
    {
        SelectRectangle(grid, items, startRow, endRow, startColumn: 0, endColumn: 0);
    }

    private static void SelectRectangle(DataGrid grid, ObservableCollection<RowItem> items, int startRow, int endRow, int startColumn, int endColumn)
    {
        var selected = new ObservableCollection<DataGridCellInfo>();
        grid.SelectedCells = selected;

        for (var rowIndex = startRow; rowIndex <= endRow; rowIndex++)
        {
            for (var columnIndex = startColumn; columnIndex <= endColumn; columnIndex++)
            {
                var column = grid.ColumnsInternal[columnIndex];
                selected.Add(new DataGridCellInfo(items[rowIndex], column, rowIndex, column.Index, isValid: true));
            }
        }

        grid.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
    }

    private static Border GetFillHandle(DataGrid grid)
    {
        return grid.GetVisualDescendants()
            .OfType<Border>()
            .First(border => border.Name == "PART_FillHandle");
    }

    private sealed class RowItem
    {
        public RowItem(string name, string code, string group)
        {
            Name = name;
            Code = code;
            Group = group;
        }

        public string Name { get; }
        public string Code { get; }
        public string Group { get; }
    }
}
