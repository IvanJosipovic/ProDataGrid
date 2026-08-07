// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.DataGridTests;

public class DataGridRowGroupExpandCollapseAllTests
{
    [AvaloniaFact]
    public void CollapseAllGroups_Collapses_All_Groups_And_Hides_SubGroups()
    {
        var (grid, view, root) = CreateNestedGroupedGrid();

        try
        {
            var topGroups = GetTopLevelGroups(view);
            var totalGroups = CountAllGroups(topGroups);

            var rowGroupInfos = GetRowGroupInfos(grid);
            Assert.Equal(totalGroups, rowGroupInfos.Count);
            Assert.True(rowGroupInfos.Any(info => info.Level > 0));

            grid.CollapseAllGroups();
            PumpLayout(grid);

            rowGroupInfos = GetRowGroupInfos(grid);
            Assert.All(rowGroupInfos, info => Assert.False(info.IsVisible));

            var visibleHeaders = GetGroupHeaders(grid).Where(header => header.IsVisible).ToList();
            Assert.All(visibleHeaders, header => Assert.Equal(0, header.RowGroupInfo!.Level));

            var subGroupHeaders = GetGroupHeaders(grid).Where(header => header.RowGroupInfo!.Level > 0).ToList();
            Assert.All(subGroupHeaders, header => Assert.False(header.IsVisible));
        }
        finally
        {
            root.Close();
        }
    }

    [AvaloniaFact]
    public void CollapseAllGroups_Hides_All_Realized_Rows_For_Dominant_Nested_Group()
    {
        var items = new List<Item>();
        for (var index = 0; index < 640; index++)
        {
            items.Add(new Item("Dominant", $"Segment {index / 4}", $"Item {index}"));
        }
        for (var index = 640; index < 680; index++)
        {
            items.Add(new Item($"Group {index - 640}", "Segment", $"Item {index}"));
        }

        var (grid, _, root) = CreateNestedGroupedGrid(items, height: 320);

        try
        {
            grid.CollapseAllGroups();
            PumpLayout(grid);
            grid.ExpandAllGroups();
            PumpLayout(grid);
            Assert.NotEmpty(GetVisibleRows(grid));

            DataGridRowGroupInfo dominantGroup = GetRowGroupInfos(grid)
                .First(info => info.Level == 0);
            grid.ScrollIntoView(items[500], grid.ColumnsInternal[0]);
            PumpLayout(grid);

            Assert.True(dominantGroup.Slot < grid.DisplayData.FirstScrollingSlot);
            Assert.Contains(GetVisibleRows(grid), row => ReferenceEquals(row.DataContext, items[500]));

            grid.CollapseAllGroups();

            Assert.Empty(GetVisibleRows(grid));
            Assert.InRange(grid.GetVerticalOffset(), 0, 0.01);
            Assert.Equal(0, grid.DisplayData.FirstScrollingSlot);
            Assert.IsType<DataGridRowGroupHeader>(grid.DisplayData.GetDisplayedElement(0));
        }
        finally
        {
            root.Close();
        }
    }

    [AvaloniaFact]
    public void CollapseAllGroups_Rebuilds_From_Positive_Adjusted_Legacy_Offset()
    {
        const int prefixGroupCount = 80;
        var items = new List<Item>();
        for (var index = 0; index < prefixGroupCount; index++)
        {
            items.Add(new Item($"Prefix {index:D2}", "Segment", $"Prefix item {index}"));
        }
        for (var index = 0; index < 640; index++)
        {
            items.Add(new Item("Dominant", $"Segment {index / 4}", $"Dominant item {index}"));
        }
        for (var index = 0; index < 20; index++)
        {
            items.Add(new Item($"Suffix {index:D2}", "Segment", $"Suffix item {index}"));
        }

        var (grid, _, root) = CreateNestedGroupedGrid(items, height: 320);

        try
        {
            DataGridRowGroupInfo dominantGroup = GetRowGroupInfos(grid)
                .Where(info => info.Level == 0)
                .Skip(prefixGroupCount)
                .First();
            Item targetItem = items[prefixGroupCount + 630];
            grid.ScrollIntoView(targetItem, grid.ColumnsInternal[0]);
            PumpLayout(grid);

            Assert.True(dominantGroup.Slot < grid.DisplayData.FirstScrollingSlot);

            grid.CollapseAllGroups();

            Assert.Empty(GetVisibleRows(grid));
            Assert.True(grid.GetVerticalOffset() > 0);
            Assert.True(grid.DisplayData.FirstScrollingSlot < dominantGroup.Slot);
            Assert.IsType<DataGridRowGroupHeader>(
                grid.DisplayData.GetDisplayedElement(grid.DisplayData.FirstScrollingSlot));
        }
        finally
        {
            root.Close();
        }
    }

    [AvaloniaFact]
    public void CollapseAllGroups_DoesNotBuildLargeDirtyScrollHeightIndex()
    {
        var item = new Item("Dominant", "Segment", "Item");
        var items = Enumerable.Repeat(item, 100_000).ToArray();
        var (grid, _, root) = CreateNestedGroupedGrid(items, height: 320);

        try
        {
            Assert.Equal(0, grid.ScrollHeightIndexCount);

            grid.CollapseAllGroups();

            Assert.Equal(0, grid.ScrollHeightIndexCount);
            Assert.IsType<DataGridRowGroupHeader>(
                grid.DisplayData.GetDisplayedElement(grid.DisplayData.FirstScrollingSlot));
        }
        finally
        {
            root.Close();
        }
    }

    [AvaloniaFact]
    public void ExpandAllGroups_Expands_All_Groups_After_Collapse()
    {
        var (grid, view, root) = CreateNestedGroupedGrid();

        try
        {
            var totalGroups = CountAllGroups(GetTopLevelGroups(view));

            grid.CollapseAllGroups();
            PumpLayout(grid);
            grid.ExpandAllGroups();
            PumpLayout(grid);

            var rowGroupInfos = GetRowGroupInfos(grid);
            Assert.Equal(totalGroups, rowGroupInfos.Count);
            Assert.All(rowGroupInfos, info => Assert.True(info.IsVisible));

            var subgroupInfo = rowGroupInfos.First(info => info.Level > 0);
            var subgroupHeader = GetHeaderForGroupInfo(grid, subgroupInfo);
            Assert.True(subgroupHeader.IsVisible);
        }
        finally
        {
            root.Close();
        }
    }

    [AvaloniaFact]
    public void ExpandCollapseAllGroups_With_No_Grouping_Is_NoOp()
    {
        var items = new List<Item>
        {
            new("A", "X", "One"),
            new("B", "Y", "Two")
        };

        var root = new Window
        {
            Width = 400,
            Height = 300,
        };

        root.SetThemeStyles();

        var grid = new DataGrid
        {
            ItemsSource = items,
            HeadersVisibility = DataGridHeadersVisibility.Column,
        };

        grid.ColumnsInternal.Add(new DataGridTextColumn
        {
            Header = "Name",
            Binding = new Binding(nameof(Item.Name))
        });

        root.Content = grid;
        root.Show();
        grid.UpdateLayout();

        try
        {
            Assert.Empty(GetGroupHeaders(grid));

            grid.CollapseAllGroups();
            grid.ExpandAllGroups();
            grid.UpdateLayout();

            Assert.Empty(GetGroupHeaders(grid));
        }
        finally
        {
            root.Close();
        }
    }

    [AvaloniaFact]
    public void GroupHeaderIndentation_Updates_After_GroupDescriptions_Change()
    {
        var items = new List<Item>
        {
            new("A", "X", "One"),
            new("A", "Y", "Two"),
            new("B", "X", "Three"),
            new("B", "Y", "Four"),
        };

        var view = new DataGridCollectionView(items);
        view.GroupDescriptions.Add(new DataGridPathGroupDescription(nameof(Item.Category)));
        view.Refresh();

        var root = new Window
        {
            Width = 600,
            Height = 400,
        };

        root.SetThemeStyles();

        var grid = new DataGrid
        {
            ItemsSource = view,
            HeadersVisibility = DataGridHeadersVisibility.Column,
        };

        grid.ColumnsInternal.Add(new DataGridTextColumn
        {
            Header = "Category",
            Binding = new Binding(nameof(Item.Category))
        });
        grid.ColumnsInternal.Add(new DataGridTextColumn
        {
            Header = "SubCategory",
            Binding = new Binding(nameof(Item.SubCategory))
        });
        grid.ColumnsInternal.Add(new DataGridTextColumn
        {
            Header = "Name",
            Binding = new Binding(nameof(Item.Name))
        });

        root.Content = grid;
        root.Show();
        grid.UpdateLayout();
        Dispatcher.UIThread.RunJobs();

        try
        {
            var initialInfo = GetRowGroupInfos(grid).First(info => info.Level == 0);
            var initialHeader = GetHeaderForGroupInfo(grid, initialInfo);
            Assert.Equal(0d, GetIndentSpacerWidth(initialHeader));

            view.GroupDescriptions.Clear();
            view.GroupDescriptions.Add(new DataGridPathGroupDescription(nameof(Item.Category)));
            view.GroupDescriptions.Add(new DataGridPathGroupDescription(nameof(Item.SubCategory)));
            view.Refresh();
            PumpLayout(grid);

            var subInfo = GetRowGroupInfos(grid).First(info => info.Level == 1);
            var subHeader = GetHeaderForGroupInfo(grid, subInfo);

            Assert.NotNull(grid.RowGroupSublevelIndents);
            var expectedIndent = grid.RowGroupSublevelIndents![0];
            var actualIndent = GetIndentSpacerWidth(subHeader);

            Assert.Equal(expectedIndent, actualIndent, precision: 3);
        }
        finally
        {
            root.Close();
        }
    }

    private static (DataGrid grid, DataGridCollectionView view, Window root) CreateNestedGroupedGrid(
        IReadOnlyList<Item>? source = null,
        double height = 400)
    {
        var items = source ?? new List<Item>
        {
            new("A", "X", "One"),
            new("A", "Y", "Two"),
            new("A", "Y", "Three"),
            new("B", "X", "Four"),
            new("B", "Z", "Five"),
        };

        var view = new DataGridCollectionView(items);
        view.GroupDescriptions.Add(new DataGridPathGroupDescription(nameof(Item.Category)));
        view.GroupDescriptions.Add(new DataGridPathGroupDescription(nameof(Item.SubCategory)));
        view.Refresh();

        var root = new Window
        {
            Width = 600,
            Height = height,
        };

        root.SetThemeStyles();

        var grid = new DataGrid
        {
            ItemsSource = view,
            HeadersVisibility = DataGridHeadersVisibility.Column,
        };

        grid.ColumnsInternal.Add(new DataGridTextColumn
        {
            Header = "Category",
            Binding = new Binding(nameof(Item.Category))
        });
        grid.ColumnsInternal.Add(new DataGridTextColumn
        {
            Header = "SubCategory",
            Binding = new Binding(nameof(Item.SubCategory))
        });
        grid.ColumnsInternal.Add(new DataGridTextColumn
        {
            Header = "Name",
            Binding = new Binding(nameof(Item.Name))
        });

        root.Content = grid;
        root.Show();
        PumpLayout(grid);

        return (grid, view, root);
    }

    private static IReadOnlyList<DataGridRowGroupHeader> GetGroupHeaders(DataGrid grid)
    {
        return grid.GetVisualDescendants()
            .OfType<DataGridRowGroupHeader>()
            .Where(header => !header.IsRecycled)
            .ToList();
    }

    private static IReadOnlyList<DataGridRowGroupInfo> GetRowGroupInfos(DataGrid grid)
    {
        return grid.RowGroupHeadersTable.GetIndexes()
            .Select(slot => grid.RowGroupHeadersTable.GetValueAt(slot))
            .Where(info => info != null)
            .ToList();
    }

    private static IReadOnlyList<DataGridRow> GetVisibleRows(DataGrid grid)
    {
        return grid.GetVisualDescendants()
            .OfType<DataGridRow>()
            .Where(row => row.IsVisible)
            .ToList();
    }

    private static DataGridRowGroupHeader GetHeaderForGroupInfo(DataGrid grid, DataGridRowGroupInfo info)
    {
        var columnIndex = grid.ColumnsInternal.FirstVisibleNonFillerColumn?.Index ?? 0;
        if (grid.ColumnDefinitions.Count > 0)
        {
            grid.ScrollSlotIntoView(columnIndex, info.Slot, forCurrentCellChange: false, forceHorizontalScroll: false);
        }

        PumpLayout(grid);

        if (grid.DisplayData.GetDisplayedElement(info.Slot) is DataGridRowGroupHeader header)
        {
            return header;
        }

        throw new InvalidOperationException("Group header was not realized.");
    }

    private static double GetIndentSpacerWidth(DataGridRowGroupHeader header)
    {
        var spacer = header.GetVisualDescendants()
            .OfType<Rectangle>()
            .FirstOrDefault(rect => rect.Name == "PART_IndentSpacer");

        Assert.NotNull(spacer);
        return spacer!.Width;
    }

    private static IReadOnlyList<DataGridCollectionViewGroup> GetTopLevelGroups(DataGridCollectionView view)
    {
        return view.Groups?.Cast<DataGridCollectionViewGroup>().ToList()
               ?? new List<DataGridCollectionViewGroup>();
    }

    private static int CountAllGroups(IEnumerable<DataGridCollectionViewGroup> groups)
    {
        var count = 0;
        foreach (var group in groups)
        {
            count++;
            count += CountAllGroups(group.Items.OfType<DataGridCollectionViewGroup>());
        }
        return count;
    }

    private static void PumpLayout(Control control)
    {
        Dispatcher.UIThread.RunJobs();
        if (control.GetVisualRoot() is Window window)
        {
            window.ApplyTemplate();
            window.UpdateLayout();
        }
        control.ApplyTemplate();
        control.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
        control.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
    }

    private record Item(string Category, string SubCategory, string Name);
}
