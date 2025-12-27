// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.DataGridTests;
using Avalonia.Data;
using Avalonia.Threading;

namespace Avalonia.Controls.DataGridTests.State;

internal sealed class StateTestItem
{
    public StateTestItem(int id, string name, string category, string group)
    {
        Id = id;
        Name = name;
        Category = category;
        Group = group;
    }

    public int Id { get; }

    public string Name { get; }

    public string Category { get; }

    public string Group { get; }
}

internal static class StateTestHelper
{
    public static List<StateTestItem> CreateItems(int count)
    {
        var items = new List<StateTestItem>(count);
        for (int i = 0; i < count; i++)
        {
            var category = i % 2 == 0 ? "A" : "B";
            var group = i % 3 == 0 ? "G1" : "G2";
            items.Add(new StateTestItem(i, $"Item {i}", category, group));
        }

        return items;
    }

    public static (DataGrid grid, Window root) CreateGrid(
        IList<StateTestItem> items,
        Action<DataGrid> configure = null,
        int width = 600,
        int height = 400)
    {
        var root = new Window
        {
            Width = width,
            Height = height,
        };

        root.SetThemeStyles();

        var grid = new DataGrid
        {
            ItemsSource = items,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            AutoGenerateColumns = false,
            SelectionMode = DataGridSelectionMode.Extended,
            SelectionUnit = DataGridSelectionUnit.FullRow,
        };

        grid.ColumnsInternal.Add(new DataGridTextColumn
        {
            Header = "Id",
            Binding = new Binding(nameof(StateTestItem.Id)),
        });
        grid.ColumnsInternal.Add(new DataGridTextColumn
        {
            Header = "Name",
            Binding = new Binding(nameof(StateTestItem.Name)),
        });
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

        configure?.Invoke(grid);

        root.Content = grid;
        root.Show();

        grid.UpdateLayout();
        Dispatcher.UIThread.RunJobs();

        return (grid, root);
    }

    public static DataGridStateOptions CreateKeyedOptions(DataGrid grid, IList<StateTestItem> items)
    {
        return new DataGridStateOptions
        {
            ItemKeySelector = item => item is StateTestItem test ? test.Id : null,
            ItemKeyResolver = key => key is int id ? items.FirstOrDefault(item => item.Id == id) : null,
            ColumnKeySelector = column => column.Header?.ToString(),
            ColumnKeyResolver = key => grid.ColumnsInternal.FirstOrDefault(
                column => string.Equals(column.Header?.ToString(), key?.ToString(), StringComparison.Ordinal)),
        };
    }
}
