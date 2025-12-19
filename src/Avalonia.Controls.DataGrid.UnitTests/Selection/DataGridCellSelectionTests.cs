// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.DataGridTests.Selection;

public class DataGridCellSelectionTests
{
    [AvaloniaFact]
    public void SelectedCells_Binding_Selects_Row_And_Raises_Event()
    {
        var items = new ObservableCollection<Item>
        {
            new() { Name = "A" },
            new() { Name = "B" },
        };

        var grid = CreateGrid(items);
        grid.SelectionUnit = DataGridSelectionUnit.Cell;
        grid.SelectionMode = DataGridSelectionMode.Extended;
        grid.UpdateLayout();

        List<DataGridSelectedCellsChangedEventArgs> events = new();
        grid.SelectedCellsChanged += (_, e) => events.Add(e);

        var firstColumn = grid.Columns.ToList().First();
        var cell = new DataGridCellInfo(items[1], firstColumn, 1, 0, isValid: true);
        grid.SelectedCells = new ObservableCollection<DataGridCellInfo> { cell };
        grid.UpdateLayout();

        Assert.Single(grid.SelectedCells);
        Assert.Equal(items[1], grid.SelectedItem);
        Assert.Contains(items[1], grid.SelectedItems.Cast<object>());

        var row = GetRows(grid).First(r => Equals(r.DataContext, items[1]));
        Assert.True(row.IsSelected);

        Assert.Single(events);
        Assert.Single(events[0].AddedCells);
        Assert.Empty(events[0].RemovedCells);
    }

    [AvaloniaFact]
    public void Switching_To_FullRow_Clears_Cell_Selection_But_Keeps_Rows_Selected()
    {
        var items = new ObservableCollection<Item>
        {
            new() { Name = "A" },
            new() { Name = "B" },
        };

        var grid = CreateGrid(items);
        grid.SelectionUnit = DataGridSelectionUnit.Cell;
        grid.UpdateLayout();

        var firstColumn = grid.Columns.ToList().First();
        var cell = new DataGridCellInfo(items[0], firstColumn, 0, 0, isValid: true);
        grid.SelectedCells = new ObservableCollection<DataGridCellInfo> { cell };
        grid.UpdateLayout();

        Assert.NotEmpty(grid.SelectedCells);
        Assert.Contains(items[0], grid.SelectedItems.Cast<object>());

        grid.SelectionUnit = DataGridSelectionUnit.FullRow;
        grid.UpdateLayout();

        Assert.Empty(grid.SelectedCells);
        Assert.Contains(items[0], grid.SelectedItems.Cast<object>());
        Assert.True(GetRows(grid).First(r => Equals(r.DataContext, items[0])).IsSelected);
    }

    [AvaloniaFact]
    public void SelectAllCells_Selects_All_Visible_Cells()
    {
        var items = new ObservableCollection<Item>
        {
            new() { Name = "A" },
            new() { Name = "B" },
            new() { Name = "C" },
        };

        var grid = CreateGrid(items);
        grid.SelectionUnit = DataGridSelectionUnit.Cell;
        grid.UpdateLayout();

        grid.SelectAllCells();
        grid.UpdateLayout();

        var columns = grid.Columns.ToList();
        var visibleColumns = columns.Count;
        Assert.Equal(items.Count * visibleColumns, grid.SelectedCells.Count);
        Assert.Equal(items.Count, grid.SelectedItems.Count);
        Assert.All(GetRows(grid), r => Assert.True(r.IsSelected));
    }

    private static DataGrid CreateGrid(IEnumerable<Item> items)
    {
        var root = new Window
        {
            Width = 320,
            Height = 240,
        };

        root.SetThemeStyles();

        var grid = new DataGrid
        {
            ItemsSource = items,
            AutoGenerateColumns = true,
            SelectionMode = DataGridSelectionMode.Extended,
            CanUserAddRows = false,
        };

        root.Content = grid;
        root.Show();
        grid.UpdateLayout();
        return grid;
    }

    private static IReadOnlyList<DataGridRow> GetRows(DataGrid grid)
    {
        return grid.GetSelfAndVisualDescendants().OfType<DataGridRow>().ToList();
    }

    private class Item
    {
        public string Name { get; set; } = string.Empty;
    }
}
