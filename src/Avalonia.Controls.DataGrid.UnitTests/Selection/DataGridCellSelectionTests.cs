// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Avalonia.Collections;
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

    [AvaloniaFact]
    public void CellSelection_Follows_Item_After_Sorting()
    {
        var items = new ObservableCollection<Item>
        {
            new() { Name = "Beta", Value = 2 },
            new() { Name = "Alpha", Value = 1 },
            new() { Name = "Gamma", Value = 3 },
        };

        var view = new DataGridCollectionView(items);
        var grid = CreateGrid(view);
        grid.SelectionUnit = DataGridSelectionUnit.Cell;
        grid.SelectionMode = DataGridSelectionMode.Extended;
        grid.UpdateLayout();

        var firstColumn = grid.Columns.ToList().First();
        var selectedItem = items[0];
        grid.SelectedCells = new ObservableCollection<DataGridCellInfo>
        {
            new(selectedItem, firstColumn, 0, firstColumn.Index, isValid: true)
        };
        grid.UpdateLayout();

        Assert.Equal(0, Assert.Single(grid.SelectedCells).RowIndex);

        view.SortDescriptions.Clear();
        view.SortDescriptions.Add(DataGridSortDescription.FromPath(nameof(Item.Name), ListSortDirection.Ascending));
        grid.UpdateLayout();

        Assert.Equal(1, grid.DataConnection.IndexOf(selectedItem));
        var selectedCell = Assert.Single(grid.SelectedCells);
        Assert.Same(selectedItem, selectedCell.Item);
        Assert.Equal(1, selectedCell.RowIndex);
    }

    [AvaloniaFact]
    public void CellSelection_Follows_Item_After_Move()
    {
        var items = new ObservableCollection<Item>
        {
            new() { Name = "A", Value = 1 },
            new() { Name = "B", Value = 2 },
            new() { Name = "C", Value = 3 },
        };

        var grid = CreateGrid(items);
        grid.SelectionUnit = DataGridSelectionUnit.Cell;
        grid.SelectionMode = DataGridSelectionMode.Extended;
        grid.UpdateLayout();

        var firstColumn = grid.Columns.ToList().First();
        var selectedItem = items[0];
        grid.SelectedCells = new ObservableCollection<DataGridCellInfo>
        {
            new(selectedItem, firstColumn, 0, firstColumn.Index, isValid: true)
        };
        grid.UpdateLayout();

        Assert.Equal(0, Assert.Single(grid.SelectedCells).RowIndex);

        items.Move(0, 2);
        grid.UpdateLayout();

        Assert.Equal(2, grid.DataConnection.IndexOf(selectedItem));
        var selectedCell = Assert.Single(grid.SelectedCells);
        Assert.Same(selectedItem, selectedCell.Item);
        Assert.Equal(2, selectedCell.RowIndex);
    }

    [AvaloniaFact]
    public void CellSelection_Follows_Item_After_Filtering_And_When_Filter_Is_Cleared()
    {
        var items = new ObservableCollection<Item>
        {
            new() { Name = "A", Value = 1 },
            new() { Name = "B", Value = 2 },
            new() { Name = "C", Value = 3 },
        };

        var view = new DataGridCollectionView(items);
        var grid = CreateGrid(view);
        grid.SelectionUnit = DataGridSelectionUnit.Cell;
        grid.SelectionMode = DataGridSelectionMode.Extended;
        grid.UpdateLayout();

        var firstColumn = grid.Columns.ToList().First();
        var selectedItem = items[1];
        grid.SelectedCells = new ObservableCollection<DataGridCellInfo>
        {
            new(selectedItem, firstColumn, 1, firstColumn.Index, isValid: true)
        };
        grid.UpdateLayout();

        view.Filter = item => !ReferenceEquals(item, items[0]);
        grid.UpdateLayout();

        Assert.Equal(0, grid.DataConnection.IndexOf(selectedItem));
        Assert.Equal(0, Assert.Single(grid.SelectedCells).RowIndex);

        view.Filter = null;
        grid.UpdateLayout();

        Assert.Equal(1, grid.DataConnection.IndexOf(selectedItem));
        Assert.Equal(1, Assert.Single(grid.SelectedCells).RowIndex);
    }

    [AvaloniaFact]
    public void CellSelection_Removes_Filtered_Out_Items_And_Remaps_Remaining_Selection()
    {
        var items = new ObservableCollection<Item>
        {
            new() { Name = "A", Value = 1 },
            new() { Name = "B", Value = 2 },
            new() { Name = "C", Value = 3 },
        };

        var view = new DataGridCollectionView(items);
        var grid = CreateGrid(view);
        grid.SelectionUnit = DataGridSelectionUnit.Cell;
        grid.SelectionMode = DataGridSelectionMode.Extended;
        grid.UpdateLayout();

        var firstColumn = grid.Columns.ToList().First();
        grid.SelectedCells = new ObservableCollection<DataGridCellInfo>
        {
            new(items[0], firstColumn, 0, firstColumn.Index, isValid: true),
            new(items[1], firstColumn, 1, firstColumn.Index, isValid: true),
        };
        grid.UpdateLayout();

        view.Filter = item => !ReferenceEquals(item, items[0]);
        grid.UpdateLayout();

        var selectedCell = Assert.Single(grid.SelectedCells);
        Assert.Same(items[1], selectedCell.Item);
        Assert.Equal(0, selectedCell.RowIndex);
    }

    [AvaloniaFact]
    public void MultiCellSelection_Follows_Items_After_Sorting()
    {
        var items = new ObservableCollection<Item>
        {
            new() { Name = "Beta", Value = 2 },
            new() { Name = "Alpha", Value = 1 },
            new() { Name = "Gamma", Value = 3 },
        };

        var view = new DataGridCollectionView(items);
        var grid = CreateGrid(view);
        grid.SelectionUnit = DataGridSelectionUnit.Cell;
        grid.SelectionMode = DataGridSelectionMode.Extended;
        grid.UpdateLayout();

        var nameColumn = grid.Columns.First(c => Equals(c.Header, nameof(Item.Name)));
        var valueColumn = grid.Columns.First(c => Equals(c.Header, nameof(Item.Value)));
        var selectedBeta = items[0];
        var selectedAlpha = items[1];

        grid.SelectedCells = new ObservableCollection<DataGridCellInfo>
        {
            new(selectedBeta, nameColumn, 0, nameColumn.Index, isValid: true),
            new(selectedBeta, valueColumn, 0, valueColumn.Index, isValid: true),
            new(selectedAlpha, nameColumn, 1, nameColumn.Index, isValid: true),
            new(selectedAlpha, valueColumn, 1, valueColumn.Index, isValid: true),
        };
        grid.UpdateLayout();

        view.SortDescriptions.Clear();
        view.SortDescriptions.Add(DataGridSortDescription.FromPath(nameof(Item.Name), ListSortDirection.Ascending));
        grid.UpdateLayout();

        var alphaRowIndex = grid.DataConnection.IndexOf(selectedAlpha);
        var betaRowIndex = grid.DataConnection.IndexOf(selectedBeta);
        Assert.Equal(4, grid.SelectedCells.Count);
        Assert.Contains(grid.SelectedCells, cell => ReferenceEquals(cell.Item, selectedAlpha) && cell.RowIndex == alphaRowIndex && cell.ColumnIndex == nameColumn.Index);
        Assert.Contains(grid.SelectedCells, cell => ReferenceEquals(cell.Item, selectedAlpha) && cell.RowIndex == alphaRowIndex && cell.ColumnIndex == valueColumn.Index);
        Assert.Contains(grid.SelectedCells, cell => ReferenceEquals(cell.Item, selectedBeta) && cell.RowIndex == betaRowIndex && cell.ColumnIndex == nameColumn.Index);
        Assert.Contains(grid.SelectedCells, cell => ReferenceEquals(cell.Item, selectedBeta) && cell.RowIndex == betaRowIndex && cell.ColumnIndex == valueColumn.Index);
    }

    [AvaloniaFact]
    public void MultiCellSelection_Follows_Items_After_Move()
    {
        var items = new ObservableCollection<Item>
        {
            new() { Name = "A", Value = 1 },
            new() { Name = "B", Value = 2 },
            new() { Name = "C", Value = 3 },
        };

        var grid = CreateGrid(items);
        grid.SelectionUnit = DataGridSelectionUnit.Cell;
        grid.SelectionMode = DataGridSelectionMode.Extended;
        grid.UpdateLayout();

        var nameColumn = grid.Columns.First(c => Equals(c.Header, nameof(Item.Name)));
        var valueColumn = grid.Columns.First(c => Equals(c.Header, nameof(Item.Value)));
        var selectedA = items[0];
        var selectedB = items[1];

        grid.SelectedCells = new ObservableCollection<DataGridCellInfo>
        {
            new(selectedA, nameColumn, 0, nameColumn.Index, isValid: true),
            new(selectedA, valueColumn, 0, valueColumn.Index, isValid: true),
            new(selectedB, nameColumn, 1, nameColumn.Index, isValid: true),
            new(selectedB, valueColumn, 1, valueColumn.Index, isValid: true),
        };
        grid.UpdateLayout();

        items.Move(0, 2);
        grid.UpdateLayout();

        var rowIndexA = grid.DataConnection.IndexOf(selectedA);
        var rowIndexB = grid.DataConnection.IndexOf(selectedB);
        Assert.Equal(4, grid.SelectedCells.Count);
        Assert.Contains(grid.SelectedCells, cell => ReferenceEquals(cell.Item, selectedA) && cell.RowIndex == rowIndexA && cell.ColumnIndex == nameColumn.Index);
        Assert.Contains(grid.SelectedCells, cell => ReferenceEquals(cell.Item, selectedA) && cell.RowIndex == rowIndexA && cell.ColumnIndex == valueColumn.Index);
        Assert.Contains(grid.SelectedCells, cell => ReferenceEquals(cell.Item, selectedB) && cell.RowIndex == rowIndexB && cell.ColumnIndex == nameColumn.Index);
        Assert.Contains(grid.SelectedCells, cell => ReferenceEquals(cell.Item, selectedB) && cell.RowIndex == rowIndexB && cell.ColumnIndex == valueColumn.Index);
    }

    [AvaloniaFact]
    public void MultiCellSelection_Removes_Filtered_Out_Items_And_Retains_Remaining_Rectangle_Cells()
    {
        var items = new ObservableCollection<Item>
        {
            new() { Name = "A", Value = 1 },
            new() { Name = "B", Value = 2 },
            new() { Name = "C", Value = 3 },
        };

        var view = new DataGridCollectionView(items);
        var grid = CreateGrid(view);
        grid.SelectionUnit = DataGridSelectionUnit.Cell;
        grid.SelectionMode = DataGridSelectionMode.Extended;
        grid.UpdateLayout();

        var nameColumn = grid.Columns.First(c => Equals(c.Header, nameof(Item.Name)));
        var valueColumn = grid.Columns.First(c => Equals(c.Header, nameof(Item.Value)));

        grid.SelectedCells = new ObservableCollection<DataGridCellInfo>
        {
            new(items[0], nameColumn, 0, nameColumn.Index, isValid: true),
            new(items[0], valueColumn, 0, valueColumn.Index, isValid: true),
            new(items[1], nameColumn, 1, nameColumn.Index, isValid: true),
            new(items[1], valueColumn, 1, valueColumn.Index, isValid: true),
        };
        grid.UpdateLayout();

        view.Filter = item => !ReferenceEquals(item, items[0]);
        grid.UpdateLayout();

        Assert.Equal(2, grid.SelectedCells.Count);
        Assert.Contains(grid.SelectedCells, cell => ReferenceEquals(cell.Item, items[1]) && cell.RowIndex == 0 && cell.ColumnIndex == nameColumn.Index);
        Assert.Contains(grid.SelectedCells, cell => ReferenceEquals(cell.Item, items[1]) && cell.RowIndex == 0 && cell.ColumnIndex == valueColumn.Index);
    }

    private static DataGrid CreateGrid(IEnumerable items)
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
        public int Value { get; set; }
    }
}
