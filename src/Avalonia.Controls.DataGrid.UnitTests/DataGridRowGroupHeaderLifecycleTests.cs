// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.DataGridTests;

public class DataGridRowGroupHeaderLifecycleTests
{
    [AvaloniaFact]
    public void UpdatePseudoClasses_Does_Not_Throw_After_Detach()
    {
        var (grid, root) = CreateGroupedGrid();

        try
        {
            var header = grid.GetVisualDescendants()
                .OfType<DataGridRowGroupHeader>()
                .First();

            Assert.NotNull(header.RowGroupInfo);
            Assert.Same(grid, header.OwningGrid);

            root.Content = null;
            root.UpdateLayout();

            Assert.Null(header.OwningGrid);

            var exception = Record.Exception(() => header.UpdatePseudoClasses());

            Assert.Null(exception);
        }
        finally
        {
            root.Close();
        }
    }

    [AvaloniaFact]
    public void RowGroupHeaders_Rebuild_On_Reattach()
    {
        var (grid, root) = CreateGroupedGrid();

        try
        {
            Dispatcher.UIThread.RunJobs();
            grid.UpdateLayout();

            Assert.NotEmpty(GetGroupHeaders(grid));

            root.Content = null;
            Dispatcher.UIThread.RunJobs();

            root.Content = grid;
            Dispatcher.UIThread.RunJobs();
            grid.UpdateLayout();

            Assert.NotEmpty(GetGroupHeaders(grid));
        }
        finally
        {
            root.Close();
        }
    }

    [AvaloniaFact]
    public void RowGroupFooters_Rebuild_On_Reattach()
    {
        var (grid, root) = CreateGroupedGrid(showGroupSummary: true, groupSummaryPosition: DataGridGroupSummaryPosition.Footer);

        try
        {
            Dispatcher.UIThread.RunJobs();
            grid.UpdateLayout();

            Assert.NotEmpty(GetGroupFooters(grid));

            root.Content = null;
            Dispatcher.UIThread.RunJobs();

            root.Content = grid;
            Dispatcher.UIThread.RunJobs();
            grid.UpdateLayout();

            Assert.NotEmpty(GetGroupFooters(grid));
        }
        finally
        {
            root.Close();
        }
    }

    [AvaloniaFact]
    public void RowGroupCollapse_State_Persists_On_Reattach()
    {
        var (grid, root) = CreateGroupedGrid();

        try
        {
            Dispatcher.UIThread.RunJobs();
            grid.UpdateLayout();

            var header = GetGroupHeaders(grid).First();
            var key = header.RowGroupInfo.CollectionViewGroup.Key;

            var toggle = typeof(DataGridRowGroupHeader).GetMethod(
                "ToggleExpandCollapse",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(toggle);
            toggle.Invoke(header, new object[] { false, true });

            Assert.False(header.RowGroupInfo.IsVisible);

            root.Content = null;
            Dispatcher.UIThread.RunJobs();

            root.Content = grid;
            Dispatcher.UIThread.RunJobs();
            grid.UpdateLayout();

            var restoredInfo = GetGroupInfos(grid)
                .FirstOrDefault(info => Equals(info.CollectionViewGroup.Key, key));

            Assert.NotNull(restoredInfo);
            Assert.False(restoredInfo.IsVisible);
        }
        finally
        {
            root.Close();
        }
    }

    private static (DataGrid grid, Window root) CreateGroupedGrid(
        bool showGroupSummary = false,
        DataGridGroupSummaryPosition groupSummaryPosition = DataGridGroupSummaryPosition.Header)
    {
        var items = new ObservableCollection<Item>
        {
            new("Alpha", "G1"),
            new("Beta", "G1"),
            new("Gamma", "G2"),
        };

        var view = new DataGridCollectionView(items);
        view.GroupDescriptions.Add(new DataGridPathGroupDescription(nameof(Item.Group)));

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
        if (showGroupSummary)
        {
            grid.ShowGroupSummary = true;
            grid.GroupSummaryPosition = groupSummaryPosition;
        }

        grid.ColumnsInternal.Add(new DataGridTextColumn
        {
            Header = "Name",
            Binding = new Binding(nameof(Item.Name))
        });

        root.Content = grid;
        root.Show();
        grid.UpdateLayout();

        return (grid, root);
    }

    private static IReadOnlyList<DataGridRowGroupHeader> GetGroupHeaders(DataGrid grid)
    {
        return grid.GetVisualDescendants()
            .OfType<DataGridRowGroupHeader>()
            .ToList();
    }

    private static IReadOnlyList<DataGridRowGroupFooter> GetGroupFooters(DataGrid grid)
    {
        return grid.GetVisualDescendants()
            .OfType<DataGridRowGroupFooter>()
            .ToList();
    }

    private static IReadOnlyList<DataGridRowGroupInfo> GetGroupInfos(DataGrid grid)
    {
        return grid.RowGroupHeadersTable
            .GetIndexes()
            .Select(slot => grid.RowGroupHeadersTable.GetValueAt(slot))
            .Where(info => info != null)
            .ToList();
    }

    private record Item(string Name, string Group);
}
