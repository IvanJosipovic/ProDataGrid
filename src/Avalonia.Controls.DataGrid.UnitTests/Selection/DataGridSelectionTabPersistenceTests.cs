// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.DataGridTests.Selection;

public class DataGridSelectionTabPersistenceTests
{
    private static readonly Size WindowSize = new(400, 300);

    [AvaloniaFact]
    public void Selection_Is_Restored_When_Switching_Tabs()
    {
        var items1 = new ObservableCollection<TabPersistenceItem>
        {
            new() { Name = "Alpha" },
            new() { Name = "Beta" },
            new() { Name = "Gamma" }
        };
        var items2 = new ObservableCollection<TabPersistenceItem>
        {
            new() { Name = "One" },
            new() { Name = "Two" },
            new() { Name = "Three" }
        };

        var grid1 = CreateGrid(items1);
        var grid2 = CreateGrid(items2);
        grid1.SelectedItem = items1[1]; // Beta

        var window = CreateWindow(grid1);

        try
        {
            ShowAndLayout(window, grid1);
            grid1.ApplyTemplate();
            grid1.UpdateLayout();

            var initialRow = RealizeRow(window, grid1, items1[1]);
            if (initialRow != null)
            {
                Assert.True(initialRow.IsSelected);
                Assert.True(((IPseudoClasses)initialRow.Classes).Contains(":selected"));
            }
            else
            {
                Assert.Equal(items1[1], grid1.SelectedItem);
            }

            SwitchContent(window, grid2);
            SwitchContent(window, grid1);

            var restoredRow = RealizeRow(window, grid1, items1[1]);
            if (restoredRow != null)
            {
                Assert.True(restoredRow.IsSelected);
                Assert.True(((IPseudoClasses)restoredRow.Classes).Contains(":selected"));
            }
            else
            {
                Assert.Equal(items1[1], grid1.SelectedItem);
            }
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Scroll_Offset_Is_Preserved_When_Switching_Tabs()
    {
        var items1 = new ObservableCollection<TabPersistenceItem>(
            Enumerable.Range(0, 200).Select(i => new TabPersistenceItem { Name = $"Item {i}" }));
        var items2 = new ObservableCollection<TabPersistenceItem>(
            Enumerable.Range(0, 5).Select(i => new TabPersistenceItem { Name = $"Other {i}" }));

        var grid1 = CreateGrid(items1);
        var grid2 = CreateGrid(items2);

        var window = CreateWindow(grid1);

        try
        {
            ShowAndLayout(window, grid1);
            grid1.ApplyTemplate();
            grid1.UpdateLayout();

            var targetItem = items1[150];
            var realized = RealizeRow(window, grid1, targetItem);
            Assert.True(realized != null, BuildGridDiagnostics(grid1));

            var offsetBefore = grid1.GetVerticalOffset();
            Assert.True(offsetBefore > 0, $"Expected scroll offset to be > 0, got {offsetBefore}.");

            SwitchContent(window, grid2);
            SwitchContent(window, grid1);

            var offsetAfter = grid1.GetVerticalOffset();
            Assert.InRange(offsetAfter, offsetBefore - 0.5, offsetBefore + 0.5);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Realized_Rows_Match_Viewport_After_Tab_Switch()
    {
        var items1 = new ObservableCollection<TabPersistenceItem>(
            Enumerable.Range(0, 300).Select(i => new TabPersistenceItem { Name = $"Item {i}" }));
        var items2 = new ObservableCollection<TabPersistenceItem>(
            Enumerable.Range(0, 5).Select(i => new TabPersistenceItem { Name = $"Other {i}" }));

        var grid1 = CreateGrid(items1);
        var grid2 = CreateGrid(items2);

        var window = CreateWindow(grid1);

        try
        {
            ShowAndLayout(window, grid1);
            grid1.ApplyTemplate();
            grid1.UpdateLayout();

            var targetItem = items1[180];
            var realized = RealizeRow(window, grid1, targetItem);
            Assert.True(realized != null, BuildGridDiagnostics(grid1));

            var beforeRows = GetRealizedRows(grid1).Select(r => r.DataContext).ToArray();
            Assert.NotEmpty(beforeRows);

            SwitchContent(window, grid2);
            SwitchContent(window, grid1);

            var afterRows = GetRealizedRows(grid1).Select(r => r.DataContext).ToArray();
            Assert.Equal(beforeRows.Length, afterRows.Length);
            Assert.True(beforeRows.SequenceEqual(afterRows));
        }
        finally
        {
            window.Close();
        }
    }

    private static DataGrid CreateGrid(System.Collections.IEnumerable items)
    {
        var grid = new DataGrid
        {
            AutoGenerateColumns = false,
            ItemsSource = items,
            SelectionMode = DataGridSelectionMode.Extended,
            HeadersVisibility = DataGridHeadersVisibility.Column
        };
        grid.ColumnsInternal.Add(new DataGridTextColumn
        {
            Header = "Name",
            Binding = new Binding(nameof(TabPersistenceItem.Name))
        });
        return grid;
    }

    private static Window CreateWindow(Control content)
    {
        var window = new Window
        {
            Width = WindowSize.Width,
            Height = WindowSize.Height
        };

        window.SetThemeStyles();
        window.Content = content;
        return window;
    }

    private static void ShowAndLayout(Window window, Control content)
    {
        window.Show();
        window.UpdateLayout();
        content.UpdateLayout();
    }

    private static void SwitchContent(Window window, Control content)
    {
        window.Content = content;
        window.UpdateLayout();
        content.UpdateLayout();
    }

    private static DataGridRow? FindRow(DataGrid grid, object item)
    {
        return grid
            .GetSelfAndVisualDescendants()
            .OfType<DataGridRow>()
            .FirstOrDefault(r => Equals(r.DataContext, item));
    }

    private static DataGridRow? RealizeRow(Window window, DataGrid grid, object item)
    {
        for (int i = 0; i < 5; i++)
        {
            grid.UpdateLayout();
            grid.ScrollIntoView(item, grid.ColumnDefinitions[0]);
            grid.UpdateLayout();

            var row = FindRow(grid, item);
            if (row != null)
            {
                return row;
            }
        }

        return null;
    }

    private static IReadOnlyList<DataGridRow> GetRealizedRows(DataGrid grid)
    {
        return grid
            .GetSelfAndVisualDescendants()
            .OfType<DataGridRow>()
            .OrderBy(r => r.Index)
            .ToArray();
    }

    private static string BuildGridDiagnostics(DataGrid grid)
    {
        var itemsCount = grid.ItemsSource is System.Collections.ICollection collection ? collection.Count : -1;
        var presenterCount = grid.GetSelfAndVisualDescendants().OfType<DataGridRowsPresenter>().Count();
        var rowCount = grid.GetSelfAndVisualDescendants().OfType<DataGridRow>().Count();
        return $"Row not realized. Bounds={grid.Bounds}, IsVisible={grid.IsVisible}, Root={grid.GetVisualRoot()}, Items={itemsCount}, Columns={grid.ColumnDefinitions.Count}, SlotCount={grid.SlotCount}, FirstSlot={grid.DisplayData.FirstScrollingSlot}, LastSlot={grid.DisplayData.LastScrollingSlot}, RowsPresenterSize={grid.RowsPresenterAvailableSize}, CellsWidth={grid.CellsWidth}, CellsEstimatedHeight={grid.CellsEstimatedHeight}, RowsPresenters={presenterCount}, Rows={rowCount}.";
    }

    private sealed class TabPersistenceItem
    {
        public string Name { get; set; } = string.Empty;
    }
}
