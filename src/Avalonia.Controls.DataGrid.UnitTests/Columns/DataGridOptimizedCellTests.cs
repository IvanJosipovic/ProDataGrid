// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.DataGridHierarchical;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.DataGridTests.Columns;

public sealed class DataGridOptimizedCellTests
{
    [AvaloniaFact]
    public void DirectTextCell_UsesTypedAccessor_AndTracksItemChanges()
    {
        var item = new NotifyItem("First");
        var column = new DataGridTextColumn
        {
            Binding = new Binding(nameof(NotifyItem.Name)),
            UseDirectTextCell = true
        };
        DataGridColumnMetadata.SetValueAccessor(
            column,
            new DataGridColumnValueAccessor<NotifyItem, string>(value => value.Name));

        var cell = Assert.IsType<DataGridDirectTextCell>(column.CreateCell());
        cell.DataContext = item;

        Assert.True(cell.ConfigureValueAccessor(column));
        Assert.Equal("First", cell.Value);

        item.Name = "Second";

        Assert.Equal("Second", cell.Value);
    }

    [AvaloniaFact]
    public void DirectTextCell_FallsBackToBinding_ForExplicitSource()
    {
        var item = new NotifyItem("First");
        var column = new DataGridTextColumn
        {
            Binding = new Binding(nameof(NotifyItem.Name)) { Source = item },
            UseDirectTextCell = true
        };
        DataGridColumnMetadata.SetValueAccessor(
            column,
            new DataGridColumnValueAccessor<NotifyItem, string>(value => value.Name));

        var cell = Assert.IsType<DataGridDirectTextCell>(column.CreateCell());

        Assert.False(cell.ConfigureValueAccessor(column));
    }

    [AvaloniaFact]
    public void DirectHierarchicalCell_TracksNodeState()
    {
        var node = new HierarchicalNode("Node", level: 2, isLeaf: false);
        var cell = new DataGridDirectHierarchicalCell
        {
            Indent = 10,
            DataContext = node
        };

        Assert.Equal(2, cell.Level);
        Assert.Equal(new Thickness(20, 0, 0, 0), cell.Padding);
        Assert.True(cell.IsExpandable);
        Assert.False(cell.IsExpanded);

        node.IsExpanded = true;

        Assert.True(cell.IsExpanded);
    }

    [AvaloniaFact]
    public void OptimizedColumns_CreateCoalescedCellContainers()
    {
        var drawingColumn = new DataGridCustomDrawingColumn();
        var hierarchyColumn = new DataGridHierarchicalColumn { UseDirectCell = true };
        var textColumn = new DataGridTextColumn { UseDirectTextCell = true };

        Assert.IsType<DataGridCustomDrawingCell>(drawingColumn.CreateCell());
        Assert.IsType<DataGridDirectHierarchicalCell>(hierarchyColumn.CreateCell());
        Assert.IsType<DataGridDirectTextCell>(textColumn.CreateCell());
    }

    [AvaloniaTheory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void LightweightFiller_AvoidsFillerCellCreation(bool useLightweightFiller, bool expectsFiller)
    {
        var grid = new DataGrid
        {
            Width = 500,
            Height = 160,
            ItemsSource = new[] { new NotifyItem("First") },
            UseLightweightFiller = useLightweightFiller,
            AutoGenerateColumns = false,
            HeadersVisibility = DataGridHeadersVisibility.Column
        };
        grid.ColumnsInternal.Add(new DataGridTextColumn
        {
            Width = new DataGridLength(100),
            Binding = new Binding(nameof(NotifyItem.Name))
        });

        var window = new Window { Width = 500, Height = 200 };
        window.SetThemeStyles(DataGridTheme.FluentV2);
        window.Content = grid;
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            grid.UpdateLayout();

            var row = Assert.Single(grid.GetVisualDescendants().OfType<DataGridRow>());
            Assert.Equal(expectsFiller, row.ExistingFillerCell != null);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void RecycledRetainedCells_UpdateIndexerBindingWithoutRegeneration()
    {
        var items = Enumerable.Range(0, 200)
            .Select(index => new IndexedItem($"Value {index}"))
            .ToList();
        var column = new DataGridTextColumn
        {
            Header = "Value",
            Width = new DataGridLength(180),
            Binding = new Binding("Fields[0]")
        };
        var grid = new DataGrid
        {
            Width = 320,
            Height = 160,
            RowHeight = 24,
            ItemsSource = items,
            UseLogicalScrollable = true,
            AutoGenerateColumns = false,
            HeadersVisibility = DataGridHeadersVisibility.Column
        };
        grid.ColumnsInternal.Add(column);

        var window = new Window { Width = 320, Height = 200 };
        window.SetThemeStyles(DataGridTheme.FluentV2);
        window.Content = grid;
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            grid.UpdateLayout();
            var originalCells = grid.GetVisualDescendants().OfType<DataGridCell>().ToHashSet();

            grid.ScrollIntoView(items[^1], column);
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            grid.UpdateLayout();

            var displayedRows = grid.GetVisualDescendants()
                .OfType<DataGridRow>()
                .Where(row => row.DataContext is IndexedItem)
                .ToList();
            Assert.NotEmpty(displayedRows);
            Assert.Contains(
                displayedRows.SelectMany(row => row.GetVisualDescendants().OfType<DataGridCell>()),
                originalCells.Contains);
            foreach (var row in displayedRows)
            {
                var item = Assert.IsType<IndexedItem>(row.DataContext);
                var text = Assert.Single(row.GetVisualDescendants().OfType<TextBlock>());
                Assert.Equal(item.Fields[0], text.Text);
            }
        }
        finally
        {
            window.Close();
        }
    }

    private sealed class NotifyItem : INotifyPropertyChanged
    {
        private string _name;

        public NotifyItem(string name) => _name = name;

        public string Name
        {
            get => _name;
            set
            {
                if (_name == value)
                {
                    return;
                }

                _name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    private sealed class IndexedItem
    {
        public IndexedItem(string value) => Fields = new List<string> { value };

        public List<string> Fields { get; }
    }
}
