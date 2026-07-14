// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Avalonia.Controls.DataGridTests.Columns;

public class DataGridTemplateColumnReuseTests
{
    [AvaloniaFact]
    public void ReuseCellContent_true_reuses_existing_content()
    {
        var template = new CountingTemplate();
        var column = new TestTemplateColumn
        {
            CellTemplate = template,
            ReuseCellContent = true
        };
        var cell = new DataGridCell();

        var first = column.GenerateElementPublic(cell, new object());
        cell.Content = first;

        var second = column.GenerateElementPublic(cell, new object());

        Assert.Same(first, second);
        Assert.Equal(1, template.BuildCount);
    }

    [AvaloniaFact]
    public void ReuseCellContent_false_rebuilds_content()
    {
        var template = new CountingTemplate();
        var column = new TestTemplateColumn
        {
            CellTemplate = template,
            ReuseCellContent = false
        };
        var cell = new DataGridCell();

        var first = column.GenerateElementPublic(cell, new object());
        cell.Content = first;

        var second = column.GenerateElementPublic(cell, new object());

        Assert.NotSame(first, second);
        Assert.Equal(2, template.BuildCount);
    }

    [AvaloniaFact]
    public void Recycled_row_reuses_existing_content_when_enabled()
    {
        var template = new CountingTemplate();
        var column = new TestTemplateColumn
        {
            CellTemplate = template,
            ReuseCellContent = true
        };
        var grid = new DataGrid();
        grid.ColumnsInternal.Add(column);

        var row = new DataGridRow
        {
            OwningGrid = grid,
            DataContext = new object(),
            Index = 0,
            Slot = 0
        };
        var cell = new DataGridCell
        {
            OwningColumn = column
        };
        var initialContent = column.GenerateElementPublic(cell, row.DataContext);
        cell.Content = initialContent;
        row.Cells.Insert(column.Index, cell);

        var root = new Window
        {
            Content = grid
        };
        root.Show();

        try
        {
            grid.DisplayData.RecycleRow(row);
            row.DataContext = new object();

            Assert.Same(initialContent, cell.Content);
            Assert.Equal(1, template.BuildCount);
        }
        finally
        {
            root.Close();
        }
    }

    private sealed class TestTemplateColumn : DataGridTemplateColumn
    {
        public Control GenerateElementPublic(DataGridCell cell, object dataItem)
        {
            return base.GenerateElement(cell, dataItem);
        }
    }

    private sealed class CountingTemplate : IDataTemplate
    {
        public int BuildCount { get; private set; }

        public Control? Build(object? data)
        {
            BuildCount++;
            return new Border();
        }

        public bool Match(object? data)
        {
            return true;
        }
    }
}
