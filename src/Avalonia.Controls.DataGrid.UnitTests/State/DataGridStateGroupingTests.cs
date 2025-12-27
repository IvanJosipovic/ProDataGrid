// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.DataGridTests;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Xunit;

namespace Avalonia.Controls.DataGridTests.State;

public class DataGridStateGroupingTests
{
    [AvaloniaFact]
    public void CaptureAndRestoreGroupingState_RestoresDescriptionsAndExpansion()
    {
        var items = StateTestHelper.CreateItems(12);
        var view = new DataGridCollectionView(items);
        view.GroupDescriptions.Add(new DataGridPathGroupDescription(nameof(StateTestItem.Category)));
        view.GroupDescriptions.Add(new DataGridPathGroupDescription(nameof(StateTestItem.Group)));
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
            Binding = new Binding(nameof(StateTestItem.Category)),
        });
        grid.ColumnsInternal.Add(new DataGridTextColumn
        {
            Header = "Group",
            Binding = new Binding(nameof(StateTestItem.Group)),
        });
        grid.ColumnsInternal.Add(new DataGridTextColumn
        {
            Header = "Name",
            Binding = new Binding(nameof(StateTestItem.Name)),
        });

        root.Content = grid;
        root.Show();
        grid.UpdateLayout();

        try
        {
            var targetGroup = FindGroup(view, "A");
            Assert.NotNull(targetGroup);

            grid.CollapseRowGroup(targetGroup, collapseAllSubgroups: false);
            grid.UpdateLayout();

            var state = grid.CaptureGroupingState();
            Assert.NotNull(state);

            view.GroupDescriptions.Clear();
            view.Refresh();
            grid.UpdateLayout();

            grid.RestoreGroupingState(state);
            grid.UpdateLayout();

            Assert.Equal(2, view.GroupDescriptions.Count);

            var restoredGroup = FindGroup(view, "A");
            Assert.NotNull(restoredGroup);

            var info = grid.RowGroupInfoFromCollectionViewGroup(restoredGroup);
            Assert.NotNull(info);
            Assert.False(info.IsVisible);
        }
        finally
        {
            root.Close();
        }
    }

    [AvaloniaFact]
    public void RestoreGroupingState_RefreshesIndentationForDisplayedHeaders()
    {
        var items = StateTestHelper.CreateItems(18);
        var view = new DataGridCollectionView(items);
        view.GroupDescriptions.Add(new DataGridPathGroupDescription(nameof(StateTestItem.Category)));
        view.GroupDescriptions.Add(new DataGridPathGroupDescription(nameof(StateTestItem.Group)));
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
            Binding = new Binding(nameof(StateTestItem.Category)),
        });
        grid.ColumnsInternal.Add(new DataGridTextColumn
        {
            Header = "Group",
            Binding = new Binding(nameof(StateTestItem.Group)),
        });
        grid.ColumnsInternal.Add(new DataGridTextColumn
        {
            Header = "Name",
            Binding = new Binding(nameof(StateTestItem.Name)),
        });

        root.Content = grid;
        root.Show();
        grid.UpdateLayout();
        Dispatcher.UIThread.RunJobs();

        try
        {
            grid.ExpandAllGroups();
            grid.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var state = grid.CaptureGroupingState();
            Assert.NotNull(state);

            view.GroupDescriptions.Clear();
            view.Refresh();
            grid.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            grid.RestoreGroupingState(state);
            grid.ExpandAllGroups();
            grid.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.NotNull(grid.RowGroupSublevelIndents);

            var index = grid.RowGroupSublevelIndents.Length - 1;
            var expectedIndent = grid.RowGroupSublevelIndents[index];

            Assert.True(grid.ColumnsInternal.RowGroupSpacerColumn.IsRepresented);
            Assert.Equal(DataGridLengthUnitType.Pixel, grid.ColumnsInternal.RowGroupSpacerColumn.Width.UnitType);
            Assert.Equal(expectedIndent, grid.ColumnsInternal.RowGroupSpacerColumn.Width.Value);
        }
        finally
        {
            root.Close();
        }
    }

    private static DataGridCollectionViewGroup? FindGroup(DataGridCollectionView view, params object[] pathKeys)
    {
        IEnumerable<DataGridCollectionViewGroup> current = view.Groups?.Cast<DataGridCollectionViewGroup>();

        DataGridCollectionViewGroup? matched = null;
        foreach (var key in pathKeys)
        {
            matched = current?.FirstOrDefault(group => Equals(group.Key, key));
            if (matched == null)
            {
                return null;
            }

            current = matched.Items.OfType<DataGridCollectionViewGroup>();
        }

        return matched;
    }
}
