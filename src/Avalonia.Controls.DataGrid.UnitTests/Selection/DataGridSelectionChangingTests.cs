// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.DataGridHierarchical;
using Avalonia.Controls.Selection;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.Styling;
using Xunit;

namespace Avalonia.Controls.DataGridTests.Selection;

public class DataGridSelectionChangingTests
{
    private sealed class HierarchyItem
    {
        public HierarchyItem(string name) => Name = name;

        public string Name { get; }

        public ObservableCollection<HierarchyItem> Children { get; } = new();
    }

    [AvaloniaFact]
    public void Programmatic_Row_Proposal_Is_Raised_Before_Commit()
    {
        ObservableCollection<string> items = new() { "A", "B", "C" };
        SelectionModel<string> model = new() { SingleSelect = false };
        DataGrid grid = CreateGrid(items, model);
        model.Select(0);
        grid.UpdateLayout();
        DataGridSelectionChangingEventArgs? observed = null;
        bool selectionChangedDuringPreview = true;

        grid.SelectionChanging += (_, e) =>
        {
            observed = e;
            selectionChangedDuringPreview = Equals(grid.SelectedItem, items[1]);
        };

        grid.SelectedItem = items[1];

        Assert.NotNull(observed);
        Assert.False(selectionChangedDuringPreview);
        Assert.True(observed!.Source.HasFlag(DataGridSelectionChangeSource.Programmatic));
        Assert.Equal(new object[] { "B" }, observed.AddedItems);
        Assert.Equal(new object[] { "A" }, observed.RemovedItems);
        Assert.Equal(1, Assert.Single(observed.AddedRows).RowIndex);
        Assert.Equal(0, Assert.Single(observed.RemovedRows).RowIndex);
        Assert.Equal("B", observed.ProposedCurrentItem);
        Assert.Equal(1, observed.ProposedCurrentCell.RowIndex);
        Assert.Equal(1, observed.ProposedAnchor.RowIndex);
        Assert.Equal("B", grid.SelectedItem);
        Assert.Equal(1, grid.SelectedIndex);
    }

    [AvaloniaFact]
    public void Cancellation_Leaves_Row_Current_Anchor_Currency_And_Scroll_Unchanged()
    {
        ObservableCollection<string> items = new(Enumerable.Range(0, 40).Select(i => $"Item {i}"));
        DataGrid grid = CreateGrid(items);
        grid.SelectedItem = items[2];
        grid.UpdateLayout();
        double offset = grid.GetVerticalOffset();
        DataGridCellInfo currentCell = grid.CurrentCell;
        int anchorSlot = grid.AnchorSlot;
        object? currency = grid.DataConnection.CollectionView?.CurrentItem;
        int changedCount = 0;
        grid.SelectionChanged += (_, _) => changedCount++;
        grid.SelectionChanging += (_, e) => e.Cancel = true;

        grid.SelectedItem = items[30];
        grid.UpdateLayout();

        Assert.Equal(items[2], grid.SelectedItem);
        Assert.Equal(2, grid.SelectedIndex);
        Assert.Equal(currentCell, grid.CurrentCell);
        Assert.Equal(anchorSlot, grid.AnchorSlot);
        Assert.Same(currency, grid.DataConnection.CollectionView?.CurrentItem);
        Assert.Equal(offset, grid.GetVerticalOffset());
        Assert.Equal(0, changedCount);
        Assert.Equal(new object[] { items[2] }, grid.SelectedItems.Cast<object>().ToArray());
    }

    [AvaloniaFact]
    public void SelectAll_Cancellation_Commits_Nothing()
    {
        ObservableCollection<string> items = new() { "A", "B", "C" };
        DataGrid grid = CreateGrid(items);
        grid.SelectedItem = items[1];
        DataGridSelectionChangingEventArgs? observed = null;
        grid.SelectionChanging += (_, e) =>
        {
            observed = e;
            e.Cancel = true;
        };

        grid.SelectAll();

        Assert.NotNull(observed);
        Assert.True(observed!.Source.HasFlag(DataGridSelectionChangeSource.Command));
        Assert.Equal(new object[] { "A", "C" }, observed.AddedItems);
        Assert.Empty(observed.RemovedItems);
        Assert.Equal(new object[] { "B" }, grid.SelectedItems.Cast<object>().ToArray());
        Assert.Equal("B", grid.SelectedItem);
    }

    [AvaloniaFact]
    public void SelectAllCells_Cancellation_Preserves_Cell_Column_And_Anchor_State()
    {
        ObservableCollection<string> items = new() { "A", "B", "C" };
        DataGrid grid = CreateGrid(items);
        grid.SelectionUnit = DataGridSelectionUnit.CellOrRowOrColumnHeader;
        DataGridCellInfo current = grid.CurrentCell;
        int anchorSlot = grid.AnchorSlot;
        DataGridSelectionChangingEventArgs? observed = null;
        grid.SelectionChanging += (_, e) =>
        {
            observed = e;
            e.Cancel = true;
        };

        grid.SelectAllCells();

        Assert.NotNull(observed);
        Assert.True(observed!.Source.HasFlag(DataGridSelectionChangeSource.Command));
        Assert.Equal(3, observed.AddedCells.Count);
        Assert.Single(observed.AddedColumns);
        Assert.Empty(grid.SelectedCells);
        Assert.Empty(grid.SelectedColumns);
        Assert.Equal(current, grid.CurrentCell);
        Assert.Equal(anchorSlot, grid.AnchorSlot);
    }

    [AvaloniaFact]
    public void Bound_Cell_Proposal_Cancellation_Restores_Bound_Collection()
    {
        ObservableCollection<string> items = new() { "A", "B" };
        DataGrid grid = CreateGrid(items);
        grid.SelectionUnit = DataGridSelectionUnit.Cell;
        DataGridColumn column = grid.ColumnsInternal[0];
        ObservableCollection<DataGridCellInfo> bound = new()
        {
            new DataGridCellInfo(items[1], column, 1, column.Index),
        };
        grid.SelectionChanging += (_, e) => e.Cancel = true;

        grid.SelectedCells = bound;

        Assert.Empty(grid.SelectedCells);
        Assert.Empty(bound);
    }

    [AvaloniaFact]
    public void Bound_Row_Proposal_Cancellation_Restores_Bound_Collection()
    {
        ObservableCollection<string> items = new() { "A", "B", "C" };
        DataGrid grid = CreateGrid(items);
        grid.SelectedItem = items[0];
        ObservableCollection<object> bound = new() { items[1], items[2] };
        grid.SelectionChanging += (_, e) => e.Cancel = true;

        grid.SelectedItems = bound;

        Assert.Equal(new object[] { "A" }, grid.SelectedItems.Cast<object>().ToArray());
        Assert.Equal(new object[] { "A" }, bound);
    }

    [AvaloniaFact]
    public void SelectedItems_Add_Cancellation_Commits_Nothing()
    {
        ObservableCollection<string> items = new() { "A", "B", "C" };
        DataGrid grid = CreateGrid(items);
        grid.SelectedItem = items[0];
        int proposals = 0;
        grid.SelectionChanging += (_, e) =>
        {
            proposals++;
            e.Cancel = true;
        };

        int result = grid.SelectedItems.Add(items[1]);

        Assert.Equal(1, proposals);
        Assert.Equal(new object[] { "A" }, grid.SelectedItems.Cast<object>().ToArray());
        Assert.Equal(-1, result);
    }

    [AvaloniaFact]
    public void SelectionModel_Cancellation_Restores_Model_And_Grid_Atomically()
    {
        ObservableCollection<string> items = new() { "A", "B", "C" };
        SelectionModel<string> model = new() { SingleSelect = false };
        DataGrid grid = CreateGrid(items, model);
        model.Select(0);
        grid.UpdateLayout();
        DataGridCellInfo current = grid.CurrentCell;
        int proposals = 0;
        grid.SelectionChanging += (_, e) =>
        {
            proposals++;
            e.Cancel = true;
        };

        model.Select(1);

        Assert.True(proposals > 0);
        Assert.Equal(new object[] { "A" }, grid.SelectedItems.Cast<object>().ToArray());
        Assert.Equal(new[] { 0 }, model.SelectedIndexes);
        Assert.Equal(current, grid.CurrentCell);
    }

    [AvaloniaFact]
    public void Reentrant_Selection_Is_Rejected_Without_Corrupting_Outer_Proposal()
    {
        ObservableCollection<string> items = new() { "A", "B", "C" };
        DataGrid grid = CreateGrid(items);
        grid.SelectedItem = items[0];
        Exception? reentrancyError = null;
        grid.SelectionChanging += (_, _) =>
        {
            reentrancyError = Record.Exception(() => grid.SelectedItem = items[2]);
        };

        grid.SelectedItem = items[1];

        Assert.IsType<InvalidOperationException>(reentrancyError);
        Assert.Equal("B", grid.SelectedItem);
        Assert.Equal(new object[] { "B" }, grid.SelectedItems.Cast<object>().ToArray());
    }

    [AvaloniaFact]
    public void CurrentCell_Reset_Cancellation_Preserves_Current_Cell()
    {
        ObservableCollection<string> items = new() { "A", "B" };
        DataGrid grid = CreateGrid(items);
        DataGridCellInfo current = grid.CurrentCell;
        Assert.True(current.IsValid);
        grid.SelectionChanging += (_, e) => e.Cancel = true;

        grid.CurrentCell = DataGridCellInfo.Unset;

        Assert.Equal(current, grid.CurrentCell);
    }

    [AvaloniaFact]
    public void Selection_State_Restore_Is_One_Atomic_Proposal()
    {
        ObservableCollection<string> items = new() { "A", "B", "C" };
        DataGrid grid = CreateGrid(items);
        grid.SelectedItem = items[0];
        DataGridCellInfo current = grid.CurrentCell;
        DataGridSelectionMode mode = grid.SelectionMode;
        DataGridSelectionUnit unit = grid.SelectionUnit;
        int proposals = 0;
        grid.SelectionChanging += (_, e) =>
        {
            proposals++;
            e.Cancel = true;
        };
        DataGridColumn column = grid.ColumnsInternal[0];
        DataGridSelectionState state = new()
        {
            SelectionMode = DataGridSelectionMode.Single,
            SelectionUnit = DataGridSelectionUnit.Cell,
            SelectedItemKeys = new object[] { items[2] },
            SelectedIndexes = new[] { 2 },
            SelectedCells = new[]
            {
                new DataGridCellState
                {
                    ItemKey = items[2],
                    ColumnKey = column,
                    RowIndex = 2,
                    ColumnIndex = column.Index,
                },
            },
            CurrentCell = new DataGridCellState
            {
                ItemKey = items[2],
                ColumnKey = column,
                RowIndex = 2,
                ColumnIndex = column.Index,
            },
        };

        grid.RestoreSelectionState(state);

        Assert.Equal(1, proposals);
        Assert.Equal(new object[] { "A" }, grid.SelectedItems.Cast<object>().ToArray());
        Assert.Equal(current, grid.CurrentCell);
        Assert.Equal(mode, grid.SelectionMode);
        Assert.Equal(unit, grid.SelectionUnit);
    }

    [AvaloniaFact]
    public void Collapsed_Hierarchy_Selection_Is_Vetoed_Before_Auto_Expansion()
    {
        HierarchyItem rootItem = new("root");
        HierarchyItem childItem = new("child");
        rootItem.Children.Add(childItem);
        HierarchicalModel<HierarchyItem> model = new(new HierarchicalOptions<HierarchyItem>
        {
            ChildrenSelector = item => item.Children,
            VirtualizeChildren = false,
        });
        model.SetRoot(rootItem);
        HierarchicalNode<HierarchyItem> rootNode = model.Root ?? throw new InvalidOperationException();
        model.Expand(rootNode);
        HierarchicalNode<HierarchyItem> childNode = model.FindNode(childItem) ?? throw new InvalidOperationException();

        Window window = new() { Width = 400, Height = 240 };
        window.SetThemeStyles();
        DataGrid grid = new()
        {
            AutoExpandSelectedItem = true,
            AutoGenerateColumns = false,
            HierarchicalModel = model,
            HierarchicalRowsEnabled = true,
            ItemsSource = model.Flattened,
        };
        grid.ColumnsInternal.Add(new DataGridHierarchicalColumn
        {
            Header = "Name",
            Binding = new Binding("Item.Name"),
        });
        window.Content = grid;
        window.Show();
        grid.UpdateLayout();
        model.Collapse(rootNode);
        grid.UpdateLayout();
        Assert.Equal(1, model.Count);
        object? originalSelection = grid.SelectedItem;
        DataGridSelectionChangingEventArgs? observed = null;
        grid.SelectionChanging += (_, e) =>
        {
            observed = e;
            e.Cancel = true;
        };

        grid.SelectedItem = childItem;

        Assert.NotNull(observed);
        Assert.Same(childItem, Assert.Single(observed!.AddedRows).Item);
        Assert.Same(childItem, observed!.ProposedCurrentItem);
        Assert.Same(childNode.Inner, observed.HierarchyNode);
        Assert.Same(childNode.Inner, Assert.Single(observed.HierarchyPath.Skip(1)));
        Assert.Equal(-1, Assert.Single(observed.AddedRows).RowIndex);
        Assert.False(rootNode.IsExpanded);
        Assert.Same(originalSelection, grid.SelectedItem);
    }

    private static DataGrid CreateGrid(IEnumerable<string> items, SelectionModel<string>? selection = null)
    {
        Window root = new()
        {
            Width = 400,
            Height = 240,
        };
        root.SetThemeStyles();

        DataGrid grid = new()
        {
            ItemsSource = items,
            Selection = selection ?? new SelectionModel<string> { SingleSelect = false },
            SelectionMode = DataGridSelectionMode.Extended,
            AutoGenerateColumns = false,
        };
        grid.ColumnsInternal.Add(new DataGridTextColumn
        {
            Header = "Value",
            Binding = new Binding("."),
        });
        root.Content = grid;
        root.Show();
        grid.UpdateLayout();
        return grid;
    }

}
