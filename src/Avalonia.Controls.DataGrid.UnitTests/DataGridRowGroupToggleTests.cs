// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.DataGridTests;

public class DataGridRowGroupToggleTests
{
    [AvaloniaFact]
    public void Toggling_Subgroup_Header_Does_Not_Toggle_Siblings()
    {
        var items = new List<Item>
        {
            new("A", "X", 2),
            new("A", "Y", 1),
            new("B", "X", 3),
        };
        RunToggleScenario(items, modify: null);
    }

    [AvaloniaFact]
    public void Toggling_After_Inserting_Row_In_Earlier_Group_Targets_Correct_Group()
    {
        var items = new List<Item>
        {
            new("A", "X", 2),
            new("A", "Y", 1),
            new("B", "X", 3),
        };

        RunToggleScenario(items, modify: view =>
        {
            items.Insert(0, new Item("A", "X", 0)); // insert into first group to shift later slots without adding new subgroup
            view.Refresh();
        });
    }

    [AvaloniaFact]
    public void Toggle_Uses_Current_RowGroupInfo_From_Table()
    {
        var items = new List<Item>
        {
            new("A", "X", 2),
            new("A", "Y", 1),
            new("B", "X", 3),
        };

        RunToggleScenario(items, modify: view => { }, corruptHeader: true);
    }

    [AvaloniaFact]
    public void Programmatic_GroupDescriptions_Mutations_Do_Not_Desync_RowGroupHeaders()
    {
        var items = new List<Item>
        {
            new("A", "X", 2),
            new("A", "Y", 1),
            new("B", "X", 3),
        };

        RunToggleScenario(items, modify: view =>
        {
            view.GroupDescriptions.Clear();
            view.GroupDescriptions.Add(new DataGridPathGroupDescription(nameof(Item.Category)));
            view.GroupDescriptions.Add(new DataGridPathGroupDescription(nameof(Item.SubCategory)));
            view.GroupDescriptions.Insert(1, new DataGridPathGroupDescription(nameof(Item.SortKey)));
            view.GroupDescriptions.RemoveAt(1);
            view.GroupDescriptions[0] = new DataGridPathGroupDescription(nameof(Item.Category));
        });
    }

    [AvaloniaFact]
    public void Programmatic_GroupDescriptions_Changed_After_Rendering_Uses_Current_Headers()
    {
        var items = new List<Item>
        {
            new("A", "X", 2),
            new("A", "Y", 1),
            new("B", "X", 3),
        };

        RunToggleScenario(items, modify: view =>
        {
            view.GroupDescriptions.Clear();
            view.GroupDescriptions.Add(new DataGridPathGroupDescription(nameof(Item.Category)));
            view.GroupDescriptions.Add(new DataGridPathGroupDescription(nameof(Item.SubCategory)));
            view.Refresh();
        }, modifyAfterShow: true);
    }

    private static void RunToggleScenario(IList<Item> items, Action<DataGridCollectionView>? modify, bool corruptHeader = false, bool modifyAfterShow = false)
    {
        var view = new DataGridCollectionView(items);
        view.GroupDescriptions.Add(new DataGridPathGroupDescription(nameof(Item.Category)));
        view.GroupDescriptions.Add(new DataGridPathGroupDescription(nameof(Item.SubCategory)));
        view.SortDescriptions.Add(DataGridSortDescription.FromPath(nameof(Item.SortKey), ListSortDirection.Ascending));

        if (!modifyAfterShow)
        {
            modify?.Invoke(view);
        }

        var root = new Window
        {
            Width = 400,
            Height = 300,
        };

        root.SetThemeStyles();

        var grid = new DataGrid
        {
            ItemsSource = view,
            HeadersVisibility = DataGridHeadersVisibility.Column,
        };

        grid.ColumnsInternal.Add(new DataGridTextColumn
        {
            Header = "Name",
            Binding = new Binding(nameof(Item.SubCategory)),
            SortMemberPath = nameof(Item.SortKey),
        });

        root.Content = grid;
        root.Show();

        if (modifyAfterShow)
        {
            modify?.Invoke(view);
        }

        var headers = GetGroupHeaders(grid)
            .OrderBy(h => h.RowGroupInfo?.Slot ?? int.MaxValue)
            .ToList();

        // first top-level group header (Category = A)
        var topGroups = headers.Where(h => h.RowGroupInfo?.Level == 0)
            .OrderBy(h => h.RowGroupInfo!.Slot)
            .ToList();
        Assert.True(topGroups.Count >= 2);
        var firstGroupSlot = topGroups[0].RowGroupInfo!.Slot;
        var nextGroupSlot = topGroups[1].RowGroupInfo!.Slot;

        // Grab the two sub-group headers under the first category (A)
        var subGroupHeaders = headers
            .Where(h => h.RowGroupInfo?.Level == 1)
            .Where(h => h.RowGroupInfo!.Slot > firstGroupSlot && h.RowGroupInfo!.Slot < nextGroupSlot)
            .OrderBy(h => h.RowGroupInfo!.Slot)
            .ToList();

        Assert.Equal(2, subGroupHeaders.Count);

        var targetHeader = subGroupHeaders.Single(h => Equals(h.RowGroupInfo!.CollectionViewGroup.Key, "Y"));
        var siblingHeader = subGroupHeaders.Single(h => Equals(h.RowGroupInfo!.CollectionViewGroup.Key, "X"));

        if (corruptHeader)
        {
            // Corrupt the RowGroupInfo reference to simulate stale recycled header state
            targetHeader.RowGroupInfo = siblingHeader.RowGroupInfo;
        }

        // Act - collapse the "Y" subgroup
        targetHeader.ToggleExpandCollapse(false, setCurrent: true);

        // Assert - only the target subgroup should collapse
        Assert.False(targetHeader.RowGroupInfo!.IsVisible);
        Assert.True(siblingHeader.RowGroupInfo!.IsVisible);
    }

    private static IReadOnlyList<DataGridRowGroupHeader> GetGroupHeaders(DataGrid target)
    {
        return target.GetSelfAndVisualDescendants()
            .OfType<DataGridRowGroupHeader>()
            .ToList();
    }

    private record Item(string Category, string SubCategory, int SortKey);
}
