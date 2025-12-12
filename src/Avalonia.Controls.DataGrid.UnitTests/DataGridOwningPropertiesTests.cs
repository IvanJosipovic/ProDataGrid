// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.DataGridTests;

public class DataGridOwningPropertiesTests
{
    [AvaloniaFact]
    public void Row_And_Presenters_Expose_Owners()
    {
        var items = new ObservableCollection<Item>
        {
            new("A"),
            new("B")
        };

        var root = new Window
        {
            Width = 400,
            Height = 300,
            Styles =
            {
                new StyleInclude((Uri?)null)
                {
                    Source = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Simple.xaml")
                },
            }
        };

        var grid = new DataGrid
        {
            RowDetailsVisibilityMode = DataGridRowDetailsVisibilityMode.Visible,
            RowDetailsTemplate = new FuncDataTemplate<Item>((_, _) => new TextBlock { Text = "details" })
        };
        grid.ColumnsInternal.Add(new DataGridTextColumn
        {
            Header = "Name",
            Binding = new Binding(nameof(Item.Name))
        });
        grid.ItemsSource = items;

        root.Content = grid;
        root.Show();
        grid.UpdateLayout();

        try
        {
            var row = grid.GetVisualDescendants().OfType<DataGridRow>().First();
            var rowsPresenter = grid.GetVisualDescendants().OfType<DataGridRowsPresenter>().First();
            var cellsPresenter = row.GetVisualDescendants().OfType<DataGridCellsPresenter>().First();
            var detailsPresenter = row.GetVisualDescendants().OfType<DataGridDetailsPresenter>().FirstOrDefault();

            Assert.Same(grid, row.OwningGrid);
            Assert.Same(grid, rowsPresenter.OwningGrid);
            Assert.Same(row, cellsPresenter.OwningRow);
            Assert.NotNull(detailsPresenter);
            Assert.Same(row, detailsPresenter!.OwningRow);
        }
        finally
        {
            root.Close();
        }
    }

    private record Item(string Name);
}
