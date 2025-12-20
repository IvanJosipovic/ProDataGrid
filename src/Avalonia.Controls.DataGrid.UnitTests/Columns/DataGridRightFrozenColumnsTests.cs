// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.DataGridTests.Columns;

public class DataGridRightFrozenColumnsTests
{
    [AvaloniaFact]
    public void FrozenColumnCountRight_Assigns_Right_Frozen_Positions()
    {
        var grid = new DataGrid
        {
            AutoGenerateColumns = false
        };

        grid.Columns.Add(new DataGridTextColumn { Header = "A", Binding = new Binding("Id") });
        grid.Columns.Add(new DataGridTextColumn { Header = "B", Binding = new Binding("Name") });
        grid.Columns.Add(new DataGridTextColumn { Header = "C", Binding = new Binding("Category") });
        grid.Columns.Add(new DataGridTextColumn { Header = "D", Binding = new Binding("Description") });
        grid.Columns.Add(new DataGridTextColumn { Header = "E", Binding = new Binding("Notes") });

        grid.FrozenColumnCount = 1;
        grid.FrozenColumnCountRight = 2;

        var columns = grid.ColumnsInternal.GetDisplayedColumns().ToList();

        Assert.Equal(DataGridFrozenColumnPosition.Left, columns[0].FrozenPosition);
        Assert.Equal(DataGridFrozenColumnPosition.None, columns[1].FrozenPosition);
        Assert.Equal(DataGridFrozenColumnPosition.None, columns[2].FrozenPosition);
        Assert.Equal(DataGridFrozenColumnPosition.Right, columns[3].FrozenPosition);
        Assert.Equal(DataGridFrozenColumnPosition.Right, columns[4].FrozenPosition);
    }

    [AvaloniaFact]
    public void RightFrozenHeaders_StayPinned_WhenScrolling()
    {
        var window = new Window
        {
            Width = 400,
            Height = 200
        };

        window.SetThemeStyles();

        var grid = new DataGrid
        {
            AutoGenerateColumns = false,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            Width = 400,
            Height = 200,
            ItemsSource = new[]
            {
                new TestItem { Id = 1, Name = "One", Category = "Alpha", Description = "Desc", Notes = "Note" }
            }
        };

        grid.Columns.Add(new DataGridTextColumn { Header = "ID", Binding = new Binding("Id"), Width = new DataGridLength(80) });
        grid.Columns.Add(new DataGridTextColumn { Header = "Name", Binding = new Binding("Name"), Width = new DataGridLength(120) });
        grid.Columns.Add(new DataGridTextColumn { Header = "Category", Binding = new Binding("Category"), Width = new DataGridLength(120) });
        grid.Columns.Add(new DataGridTextColumn { Header = "Description", Binding = new Binding("Description"), Width = new DataGridLength(120) });
        grid.Columns.Add(new DataGridTextColumn { Header = "Notes", Binding = new Binding("Notes"), Width = new DataGridLength(90) });

        grid.FrozenColumnCount = 1;
        grid.FrozenColumnCountRight = 1;

        window.Content = grid;
        window.Show();

        grid.ApplyTemplate();
        ApplyLayout(grid, window);

        var rightHeader = GetHeaderCell(grid, "Notes");
        var scrollingHeader = GetHeaderCell(grid, "Name");

        double initialRightX = rightHeader.Bounds.X;
        double initialScrollingX = scrollingHeader.Bounds.X;

        grid.UpdateHorizontalOffset(50);
        ApplyLayout(grid, window);

        double scrolledRightX = rightHeader.Bounds.X;
        double scrolledScrollingX = scrollingHeader.Bounds.X;

        double expectedRightX = grid.CellsWidth - rightHeader.Bounds.Width;

        Assert.True(Math.Abs(initialRightX - expectedRightX) < 0.1);
        Assert.True(Math.Abs(scrolledRightX - expectedRightX) < 0.1);
        Assert.True(scrolledScrollingX < initialScrollingX);
    }

    private static void ApplyLayout(DataGrid grid, Window window)
    {
        grid.Measure(new Size(window.Width, window.Height));
        grid.Arrange(new Rect(0, 0, window.Width, window.Height));
        grid.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
    }

    private static DataGridColumnHeader GetHeaderCell(DataGrid grid, string header)
    {
        var headerCell = grid.GetVisualDescendants()
            .OfType<DataGridColumnHeader>()
            .FirstOrDefault(h => Equals(h.Content, header));

        if (headerCell == null)
        {
            throw new InvalidOperationException($"Header '{header}' not found.");
        }

        return headerCell;
    }

    private sealed class TestItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string Notes { get; set; }
    }
}
