// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Avalonia.Controls.DataGridTests.State;

public class DataGridStateColumnsTests
{
    [AvaloniaFact]
    public void CaptureAndRestoreColumnLayoutState_RestoresLayout()
    {
        var items = StateTestHelper.CreateItems(5);
        var (grid, root) = StateTestHelper.CreateGrid(items);

        try
        {
            var idColumn = grid.ColumnsInternal[0];
            var nameColumn = grid.ColumnsInternal[1];
            var categoryColumn = grid.ColumnsInternal[2];

            idColumn.DisplayIndex = 2;
            nameColumn.DisplayIndex = 0;
            categoryColumn.DisplayIndex = 1;
            idColumn.Width = new DataGridLength(120);
            nameColumn.Width = new DataGridLength(180);
            categoryColumn.IsVisible = false;
            grid.FrozenColumnCount = 1;
            grid.FrozenColumnCountRight = 1;

            var options = StateTestHelper.CreateKeyedOptions(grid, items);
            var state = grid.CaptureColumnLayoutState(options);
            Assert.NotNull(state);

            idColumn.DisplayIndex = 0;
            nameColumn.DisplayIndex = 1;
            categoryColumn.DisplayIndex = 2;
            idColumn.Width = new DataGridLength(50);
            nameColumn.Width = new DataGridLength(60);
            categoryColumn.IsVisible = true;
            grid.FrozenColumnCount = 0;
            grid.FrozenColumnCountRight = 0;

            grid.RestoreColumnLayoutState(state, options);

            Assert.Equal(2, idColumn.DisplayIndex);
            Assert.Equal(0, nameColumn.DisplayIndex);
            Assert.Equal(1, categoryColumn.DisplayIndex);
            Assert.Equal(new DataGridLength(120), idColumn.Width);
            Assert.Equal(new DataGridLength(180), nameColumn.Width);
            Assert.False(categoryColumn.IsVisible);
            Assert.Equal(1, grid.FrozenColumnCount);
            Assert.Equal(1, grid.FrozenColumnCountRight);
        }
        finally
        {
            root.Close();
        }
    }
}
