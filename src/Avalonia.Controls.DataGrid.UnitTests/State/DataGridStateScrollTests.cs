// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Avalonia.Controls.DataGridTests.State;

public class DataGridStateScrollTests
{
    [AvaloniaFact]
    public void CaptureAndRestoreScrollState_RestoresOffsets()
    {
        var items = StateTestHelper.CreateItems(60);
        var (grid, root) = StateTestHelper.CreateGrid(items, g =>
        {
            foreach (var column in g.ColumnsInternal)
            {
                column.Width = new DataGridLength(200);
            }
        }, width: 320, height: 140);

        try
        {
            grid.ScrollIntoView(items[30], grid.ColumnsInternal[0]);
            grid.UpdateLayout();
            grid.UpdateHorizontalOffset(120);
            grid.UpdateLayout();

            var expectedSlot = grid.DisplayData.FirstScrollingSlot;
            var expectedHorizontal = grid.HorizontalOffset;

            var state = grid.CaptureScrollState();
            Assert.NotNull(state);

            grid.ScrollIntoView(items[0], grid.ColumnsInternal[0]);
            grid.UpdateHorizontalOffset(0);
            grid.UpdateLayout();

            Assert.True(grid.TryRestoreScrollState(state));

            Assert.Equal(expectedSlot, grid.DisplayData.FirstScrollingSlot);
            Assert.Equal(expectedHorizontal, grid.HorizontalOffset);
        }
        finally
        {
            root.Close();
        }
    }

    [AvaloniaFact]
    public void CaptureAndRestoreScrollState_UsesItemKeysWhenItemsReplaced()
    {
        var items = new ObservableCollection<StateTestItem>(StateTestHelper.CreateItems(30));
        var (grid, root) = StateTestHelper.CreateGrid(items, width: 320, height: 140);

        try
        {
            grid.ScrollIntoView(items[15], grid.ColumnsInternal[0]);
            grid.UpdateLayout();

            var options = StateTestHelper.CreateKeyedOptions(grid, items);
            var state = grid.CaptureScrollState(options);
            Assert.NotNull(state);

            items[0] = new StateTestItem(0, "Item 0*", "A", "G1");
            items[items.Count / 2] = new StateTestItem(items.Count / 2, $"Item {items.Count / 2}*", "B", "G2");
            items[items.Count - 1] = new StateTestItem(items.Count - 1, $"Item {items.Count - 1}*", "A", "G1");
            grid.UpdateLayout();

            Assert.True(grid.TryRestoreScrollState(state, options));
        }
        finally
        {
            root.Close();
        }
    }

    [AvaloniaFact]
    public void CaptureAndRestoreScrollState_RestoresAfterItemsSourceRebind()
    {
        var items = StateTestHelper.CreateItems(50);
        var (grid, root) = StateTestHelper.CreateGrid(items, width: 320, height: 140);

        try
        {
            grid.ScrollIntoView(items[20], grid.ColumnsInternal[0]);
            grid.UpdateLayout();

            var expectedSlot = grid.DisplayData.FirstScrollingSlot;
            var state = grid.CaptureScrollState(StateTestHelper.CreateKeyedOptions(grid, items));
            Assert.NotNull(state);

            var newItems = StateTestHelper.CreateItems(50);
            grid.ItemsSource = newItems;
            grid.UpdateLayout();

            Assert.True(grid.TryRestoreScrollState(state, StateTestHelper.CreateKeyedOptions(grid, newItems)));
            Assert.Equal(expectedSlot, grid.DisplayData.FirstScrollingSlot);
        }
        finally
        {
            root.Close();
        }
    }

    [AvaloniaFact]
    public void RestoreScrollState_AllowsNullDataSourceWhenUsingItemKeys()
    {
        var items = StateTestHelper.CreateItems(40);
        var (grid, root) = StateTestHelper.CreateGrid(items, width: 320, height: 140);

        try
        {
            grid.ScrollIntoView(items[20], grid.ColumnsInternal[0]);
            grid.UpdateLayout();

            var options = StateTestHelper.CreateKeyedOptions(grid, items);
            var captured = grid.CaptureScrollState(options);
            Assert.NotNull(captured);

            var serialized = new DataGridScrollState
            {
                DataSource = null,
                DataSourceCount = captured.DataSourceCount,
                Samples = captured.Samples,
                FirstScrollingSlot = captured.FirstScrollingSlot,
                NegVerticalOffset = captured.NegVerticalOffset,
                VerticalOffset = captured.VerticalOffset,
                HorizontalOffset = captured.HorizontalOffset,
                RowHeightEstimatorState = captured.RowHeightEstimatorState
            };

            grid.ScrollIntoView(items[0], grid.ColumnsInternal[0]);
            grid.UpdateLayout();

            Assert.True(grid.TryRestoreScrollState(serialized, options));
            Assert.Equal(captured.FirstScrollingSlot, grid.DisplayData.FirstScrollingSlot);
        }
        finally
        {
            root.Close();
        }
    }

    [AvaloniaFact]
    public void RestoreScrollState_ReturnsFalseWhenItemsSourceCleared()
    {
        var items = StateTestHelper.CreateItems(20);
        var (grid, root) = StateTestHelper.CreateGrid(items, width: 320, height: 140);

        try
        {
            grid.ScrollIntoView(items[10], grid.ColumnsInternal[0]);
            grid.UpdateLayout();

            var state = grid.CaptureScrollState(StateTestHelper.CreateKeyedOptions(grid, items));
            Assert.NotNull(state);

            grid.ItemsSource = null;
            grid.UpdateLayout();

            Assert.False(grid.TryRestoreScrollState(state, StateTestHelper.CreateKeyedOptions(grid, items)));
        }
        finally
        {
            root.Close();
        }
    }
}
