// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Avalonia.Controls.DataGridTests.Columns;

public class DataGridColumnHeaderCacheTests
{
    [AvaloniaFact]
    public void HeaderCell_Binding_Does_Not_Update_After_ClearElementCache()
    {
        var column = new DataGridTextColumn
        {
            Header = "Initial"
        };

        var header = column.HeaderCell;

        Assert.Equal("Initial", header.Content);

        column.ClearElementCache();
        column.Header = "Updated";

        Assert.Null(header.OwningColumn);
        Assert.Null(header.Content);
    }

    [AvaloniaFact]
    public void HeaderCell_Binding_Does_Not_Update_After_Column_Removed_From_Grid()
    {
        var grid = new DataGrid();
        var column = new DataGridTextColumn
        {
            Header = "Initial"
        };

        grid.ColumnsInternal.Add(column);

        var header = column.HeaderCell;

        Assert.Equal("Initial", header.Content);

        grid.ColumnsInternal.Remove(column);
        column.Header = "Updated";

        Assert.Null(header.OwningColumn);
        Assert.Null(header.Content);
    }
}
