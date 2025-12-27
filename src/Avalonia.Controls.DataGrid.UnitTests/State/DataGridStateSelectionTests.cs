// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Avalonia.Controls.DataGridTests.State;

public class DataGridStateSelectionTests
{
    [AvaloniaFact]
    public void CaptureAndRestoreSelectionState_RestoresRowsCellsAndCurrent()
    {
        var items = StateTestHelper.CreateItems(8);
        var (grid, root) = StateTestHelper.CreateGrid(items, g =>
        {
            g.SelectionMode = DataGridSelectionMode.Extended;
            g.SelectionUnit = DataGridSelectionUnit.CellOrRowHeader;
        });

        try
        {
            var nameColumn = grid.ColumnsInternal[1];

            grid.Selection.Select(1);
            grid.Selection.Select(4);
            grid.SelectedCells.Clear();
            grid.SelectedCells.Add(new DataGridCellInfo(items[2], nameColumn, 2, nameColumn.Index, true));
            var currentSlot = grid.SlotFromRowIndex(1);
            grid.UpdateSelectionAndCurrency(nameColumn.Index, currentSlot, DataGridSelectionAction.None, scrollIntoView: false);

            var options = StateTestHelper.CreateKeyedOptions(grid, items);
            var state = grid.CaptureSelectionState(options);

            grid.SelectionMode = DataGridSelectionMode.Single;
            grid.SelectionUnit = DataGridSelectionUnit.FullRow;
            grid.SelectedItems.Clear();
            grid.SelectedCells.Clear();
            grid.CurrentCell = DataGridCellInfo.Unset;

            grid.RestoreSelectionState(state, options);

            var selectedIds = grid.SelectedItems.Cast<StateTestItem>()
                .Select(item => item.Id)
                .OrderBy(id => id)
                .ToArray();

            Assert.Equal(new[] { 1, 4 }, selectedIds);
            Assert.Equal(DataGridSelectionMode.Extended, grid.SelectionMode);
            Assert.Equal(DataGridSelectionUnit.CellOrRowHeader, grid.SelectionUnit);
            Assert.Contains(grid.SelectedCells, cell => cell.RowIndex == 2 && cell.Column == nameColumn);
            Assert.Equal(1, grid.CurrentCell.RowIndex);
            Assert.Same(nameColumn, grid.CurrentCell.Column);
        }
        finally
        {
            root.Close();
        }
    }

    [AvaloniaFact]
    public void RestoreSelectionState_UsesItemKeysAfterItemsSourceSwap()
    {
        var items = StateTestHelper.CreateItems(6);
        var (grid, root) = StateTestHelper.CreateGrid(items);

        try
        {
            grid.Selection.Select(2);

            var state = grid.CaptureSelectionState(StateTestHelper.CreateKeyedOptions(grid, items));

            var newItems = StateTestHelper.CreateItems(6);
            grid.ItemsSource = newItems;
            grid.UpdateLayout();

            grid.SelectedItems.Clear();

            grid.RestoreSelectionState(state, StateTestHelper.CreateKeyedOptions(grid, newItems));

            var selected = Assert.Single(grid.SelectedItems.Cast<StateTestItem>());
            Assert.Equal(2, selected.Id);
            Assert.Contains(newItems, item => ReferenceEquals(item, selected));
        }
        finally
        {
            root.Close();
        }
    }

    [AvaloniaFact]
    public void RestoreSelectionState_IgnoresHiddenCurrentCell()
    {
        var items = StateTestHelper.CreateItems(6);
        var (grid, root) = StateTestHelper.CreateGrid(items, g =>
        {
            g.SelectionMode = DataGridSelectionMode.Extended;
            g.SelectionUnit = DataGridSelectionUnit.FullRow;
        });

        try
        {
            var nameColumn = grid.ColumnsInternal[1];

            grid.Selection.Select(1);
            grid.Selection.Select(3);

            var currentSlot = grid.SlotFromRowIndex(1);
            grid.UpdateSelectionAndCurrency(nameColumn.Index, currentSlot, DataGridSelectionAction.None, scrollIntoView: false);

            var options = StateTestHelper.CreateKeyedOptions(grid, items);
            var state = grid.CaptureSelectionState(options);

            nameColumn.IsVisible = false;
            grid.SelectedItems.Clear();
            grid.CurrentCell = DataGridCellInfo.Unset;

            grid.RestoreSelectionState(state, options);

            var selectedIds = grid.SelectedItems.Cast<StateTestItem>()
                .Select(item => item.Id)
                .OrderBy(id => id)
                .ToArray();

            Assert.Equal(new[] { 1, 3 }, selectedIds);
            Assert.NotSame(nameColumn, grid.CurrentCell.Column);
        }
        finally
        {
            root.Close();
        }
    }
}
