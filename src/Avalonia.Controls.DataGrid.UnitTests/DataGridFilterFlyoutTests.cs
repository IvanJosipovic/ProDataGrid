// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.DataGridFiltering;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.DataGridTests;

public class DataGridFilterFlyoutTests
{
    [AvaloniaFact]
    public void Column_TryShowFilterFlyout_Returns_False_When_No_Flyout()
    {
        var (grid, root, column) = CreateGrid(null);

        try
        {
            var result = column.TryShowFilterFlyout();

            Assert.False(result);
        }
        finally
        {
            root.Close();
        }
    }

    [AvaloniaFact]
    public void Column_TryShowFilterFlyout_Shows_Flyout_When_Configured()
    {
        var flyout = new Flyout { Content = new TextBlock { Text = "Filter" } };
        var (grid, root, column) = CreateGrid(flyout);

        try
        {
            var result = column.TryShowFilterFlyout();
            Dispatcher.UIThread.RunJobs();

            Assert.True(result);
            Assert.True(flyout.IsOpen);

            flyout.Hide();
        }
        finally
        {
            root.Close();
        }
    }

    [AvaloniaFact]
    public void Grid_TryShowFilterFlyout_By_ColumnId_Shows_Flyout()
    {
        var flyout = new Flyout { Content = new TextBlock { Text = "Filter" } };
        var (grid, root, column) = CreateGrid(flyout);

        try
        {
            column.ColumnKey = "Name";

            var result = grid.TryShowFilterFlyout("Name");
            Dispatcher.UIThread.RunJobs();

            Assert.True(result);
            Assert.True(flyout.IsOpen);

            flyout.Hide();
        }
        finally
        {
            root.Close();
        }
    }

    [AvaloniaFact]
    public void Grid_ClearFilterByColumnId_Removes_Descriptor()
    {
        var (grid, root, column) = CreateGrid(null);

        try
        {
            column.ColumnKey = "Name";
            grid.FilteringModel.SetOrUpdate(new FilteringDescriptor(
                columnId: "Name",
                @operator: FilteringOperator.Contains,
                propertyPath: nameof(Item.Name),
                value: "A",
                stringComparison: System.StringComparison.OrdinalIgnoreCase));

            var result = grid.ClearFilterByColumnId("Name");

            Assert.True(result);
            Assert.Empty(grid.FilteringModel.Descriptors);
        }
        finally
        {
            root.Close();
        }
    }

    [AvaloniaFact]
    public void FilteringModel_RequestShowFilterFlyout_Opens_Flyout()
    {
        var flyout = new Flyout { Content = new TextBlock { Text = "Filter" } };
        var (grid, root, column) = CreateGrid(flyout);

        try
        {
            column.ColumnKey = "Name";
            var interaction = grid.FilteringModel as Avalonia.Controls.DataGridFiltering.IFilteringModelInteraction;

            Assert.NotNull(interaction);

            interaction.RequestShowFilterFlyout("Name");
            Dispatcher.UIThread.RunJobs();

            Assert.True(flyout.IsOpen);

            flyout.Hide();
        }
        finally
        {
            root.Close();
        }
    }

    [AvaloniaFact]
    public void Header_TryShowFilterFlyout_Closes_ContextMenu_And_Opens_On_Next_Tick()
    {
        var flyout = new Flyout { Content = new TextBlock { Text = "Filter" } };
        var (grid, root, column) = CreateGrid(flyout);

        try
        {
            var menu = new ContextMenu();
            menu.Items.Add(new MenuItem { Header = "Show filter" });
            grid.ColumnHeaderContextMenu = menu;
            Dispatcher.UIThread.RunJobs();

            var header = GetHeaderForColumn(grid, column);
            menu.Open(header);
            Dispatcher.UIThread.RunJobs();

            var result = header.TryShowFilterFlyout();

            Assert.True(result);
            Assert.False(flyout.IsOpen);

            Dispatcher.UIThread.RunJobs();

            Assert.False(menu.IsOpen);
            Assert.True(flyout.IsOpen);

            flyout.Hide();
            Dispatcher.UIThread.RunJobs();
            Assert.False(flyout.IsOpen);
        }
        finally
        {
            root.Close();
        }
    }

    [AvaloniaFact]
    public void Header_Shows_Filter_Status_Icon_When_Filtered_And_Button_Hidden()
    {
        var (grid, root, column) = CreateGrid(null);

        try
        {
            grid.FilteringModel.SetOrUpdate(new FilteringDescriptor(
                columnId: column,
                @operator: FilteringOperator.Contains,
                propertyPath: nameof(Item.Name),
                value: "A",
                stringComparison: System.StringComparison.OrdinalIgnoreCase));

            Dispatcher.UIThread.RunJobs();

            var header = GetHeaderForColumn(grid, column);

            Assert.True(header.ShowFilterStatusIcon);
        }
        finally
        {
            root.Close();
        }
    }

    [AvaloniaFact]
    public void Distinct_Value_Flyout_Counts_Searches_And_Filters_Through_Central_Model()
    {
        var flyout = new DataGridDistinctValueFilterFlyout();
        var items = new ObservableCollection<Item>
        {
            new("Ada"),
            new("Ada"),
            new("Grace")
        };
        var (grid, root, column) = CreateGrid(flyout, items);

        try
        {
            column.ColumnKey = "Name";
            DataGridColumnFilter.SetValueAccessor(
                column,
                new DataGridColumnValueAccessor<Item, string>(item => item.Name));

            Assert.True(column.TryShowFilterFlyout());
            Dispatcher.UIThread.RunJobs();

            Assert.True(flyout.IsOpen);
            Assert.Null(flyout.LastError);
            Assert.NotNull(flyout.ContentTemplate);
            DataGridDistinctValueFilterContext context = Assert.IsType<DataGridDistinctValueFilterContext>(flyout.Content);
            Assert.Same(context, flyout.Context);
            Assert.Collection(
                context.Options,
                option =>
                {
                    Assert.Equal("Ada", option.Display);
                    Assert.Equal(2, option.Count);
                    Assert.False(option.IsSelected);
                },
                option =>
                {
                    Assert.Equal("Grace", option.Display);
                    Assert.Equal(1, option.Count);
                    Assert.False(option.IsSelected);
                });

            IFilterDistinctValueOption ada = context.Options.Single(option => option.Display == "Ada");
            ada.IsSelected = true;
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(2, grid.DataConnection.Count);
            FilteringDescriptor descriptor = Assert.Single(grid.FilteringModel.Descriptors);
            Assert.Equal(FilteringOperator.In, descriptor.Operator);
            Assert.Equal("Ada", Assert.Single(descriptor.Values));

            context.SearchText = "rac";
            IFilterDistinctValueOption visible = Assert.Single(context.Options);
            Assert.Equal("Grace", visible.Display);

            ada.IsSelected = false;
            Dispatcher.UIThread.RunJobs();

            Assert.Empty(grid.FilteringModel.Descriptors);
            Assert.Equal(3, grid.DataConnection.Count);
        }
        finally
        {
            flyout.Hide();
            root.Close();
        }
    }

    [AvaloniaFact]
    public void Distinct_Value_Flyout_Overrides_Accessor_Comparer_And_Formatter()
    {
        var flyout = new DataGridDistinctValueFilterFlyout
        {
            ValueAccessor = new DataGridColumnValueAccessor<Item, string>(item => item.Name),
            ValueComparer = System.StringComparer.OrdinalIgnoreCase,
            DisplayFormatter = value => value?.ToString()?.ToUpperInvariant() ?? "EMPTY"
        };
        var items = new ObservableCollection<Item>
        {
            new("Ada"),
            new("ADA"),
            new("Grace")
        };
        var (grid, root, column) = CreateGrid(flyout, items);

        try
        {
            column.ColumnKey = "Name";

            Assert.True(column.TryShowFilterFlyout());
            Dispatcher.UIThread.RunJobs();

            DataGridDistinctValueFilterContext context = Assert.IsType<DataGridDistinctValueFilterContext>(flyout.Content);
            IFilterDistinctValueOption ada = context.Options.Single(option => option.Display == "ADA");
            Assert.Equal(2, ada.Count);
            Assert.Equal("GRACE", context.Options.Single(option => option.Display == "GRACE").Display);

            ada.IsSelected = true;
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(2, grid.DataConnection.Count);
        }
        finally
        {
            flyout.Hide();
            root.Close();
        }
    }

    [AvaloniaFact]
    public void Distinct_Value_Flyout_Uses_Unfiltered_Source_When_Reopened()
    {
        var flyout = new DataGridDistinctValueFilterFlyout();
        var items = new ObservableCollection<Item>
        {
            new("Ada"),
            new("Ada"),
            new("Grace")
        };
        var (grid, root, column) = CreateGrid(flyout, items);

        try
        {
            column.ColumnKey = "Name";
            DataGridColumnFilter.SetValueAccessor(
                column,
                new DataGridColumnValueAccessor<Item, string>(item => item.Name));

            Assert.True(column.TryShowFilterFlyout());
            Dispatcher.UIThread.RunJobs();
            DataGridDistinctValueFilterContext firstContext = Assert.IsType<DataGridDistinctValueFilterContext>(flyout.Content);
            firstContext.Options.Single(option => option.Display == "Grace").IsSelected = true;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(1, grid.DataConnection.Count);

            flyout.Hide();
            Dispatcher.UIThread.RunJobs();
            Assert.True(column.TryShowFilterFlyout());
            Dispatcher.UIThread.RunJobs();

            DataGridDistinctValueFilterContext reopened = Assert.IsType<DataGridDistinctValueFilterContext>(flyout.Content);
            Assert.Equal(2, reopened.Options.Count);
            Assert.Equal(2, reopened.Options.Single(option => option.Display == "Ada").Count);
            Assert.True(reopened.Options.Single(option => option.Display == "Grace").IsSelected);
        }
        finally
        {
            flyout.Hide();
            root.Close();
        }
    }

    [AvaloniaFact]
    public void Distinct_Value_Flyout_Cancels_Open_When_Accessor_Is_Missing()
    {
        var flyout = new DataGridDistinctValueFilterFlyout();
        var (grid, root, column) = CreateGrid(flyout);

        try
        {
            Assert.True(column.TryShowFilterFlyout());
            Dispatcher.UIThread.RunJobs();

            Assert.False(flyout.IsOpen);
            Assert.Contains("IDataGridColumnValueAccessor", flyout.LastError);
        }
        finally
        {
            root.Close();
        }
    }

    private static (DataGrid grid, Window root, DataGridTextColumn column) CreateGrid(Flyout? flyout)
    {
        var items = new ObservableCollection<Item>
        {
            new("Ada"),
            new("Grace")
        };

        return CreateGrid(flyout, items);
    }

    private static (DataGrid grid, Window root, DataGridTextColumn column) CreateGrid(
        Flyout? flyout,
        ObservableCollection<Item> items)
    {
        var root = new Window
        {
            Width = 400,
            Height = 200,
        };

        root.SetThemeStyles();

        var grid = new DataGrid
        {
            ItemsSource = items
        };

        var column = new DataGridTextColumn
        {
            Header = "Name",
            Binding = new Binding(nameof(Item.Name)),
            FilterFlyout = flyout
        };

        grid.ColumnsInternal.Add(column);

        root.Content = grid;
        root.Show();
        grid.UpdateLayout();

        return (grid, root, column);
    }

    private static DataGridColumnHeader GetHeaderForColumn(DataGrid grid, DataGridColumn column)
    {
        return grid.GetVisualDescendants()
            .OfType<DataGridColumnHeader>()
            .First(h => ReferenceEquals(h.OwningColumn, column));
    }

    private sealed class Item
    {
        public Item(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
