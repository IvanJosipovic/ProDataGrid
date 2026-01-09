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
