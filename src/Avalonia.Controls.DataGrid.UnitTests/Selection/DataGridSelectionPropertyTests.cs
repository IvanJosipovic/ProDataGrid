using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Selection;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.DataGridTests.Selection;

public class DataGridSelectionPropertyTests
{
    [AvaloniaFact]
    public void Custom_SelectionModel_Applies_Selection_To_Grid()
    {
        var items = new ObservableCollection<string> { "A", "B", "C" };
        var selectionModel = new SelectionModel<string> { SingleSelect = false };
        selectionModel.Select(1); // preselect before wiring

        var grid = CreateGrid(items);
        grid.Selection = selectionModel;
        grid.UpdateLayout();

        Assert.Equal("B", grid.SelectedItem);
        Assert.Equal(new[] { "B" }, grid.SelectedItems.Cast<string>());

        var rows = GetRows(grid);
        Assert.True(rows.First(r => Equals(r.DataContext, "B")).IsSelected);
        Assert.All(rows.Where(r => !Equals(r.DataContext, "B")), r => Assert.False(r.IsSelected));

        Assert.Same(grid.Selection, selectionModel);
        Assert.Same(grid.Selection.Source, grid.CollectionView);
    }

    [AvaloniaFact]
    public void Selection_Model_With_Mismatched_Source_Throws()
    {
        var items = new ObservableCollection<string> { "A", "B", "C" };
        var grid = CreateGrid(items);

        // Source is the raw collection, not the view wrapped by the grid.
        var selectionModel = new SelectionModel<object> { Source = items };

        Assert.Throws<InvalidOperationException>(() => grid.Selection = selectionModel);
    }

    [AvaloniaFact]
    public void Replacing_Selection_Raises_Removed_SelectionChanged()
    {
        var items = new ObservableCollection<string> { "A", "B" };
        var grid = CreateGrid(items);
        grid.SelectedItem = items[0];
        grid.UpdateLayout();

        SelectionChangedEventArgs? args = null;
        grid.SelectionChanged += (_, e) => args = e;

        grid.Selection = new SelectionModel<object>();
        grid.UpdateLayout();

        Assert.NotNull(args);
        var removed = args!.RemovedItems.Cast<object>().ToArray();
        Assert.Equal(new[] { items[0] }, removed);
        Assert.Empty(args.AddedItems);
    }

    [AvaloniaFact]
    public void SelectionModel_Shifts_On_Insert_Before_Selected_Item()
    {
        var items = new ObservableCollection<string> { "A", "B", "C" };
        var selectionModel = new SelectionModel<string> { SingleSelect = false };
        selectionModel.Select(1); // select "B" before source to verify deferred selection

        var grid = CreateGrid(items);
        grid.Selection = selectionModel;
        grid.UpdateLayout();

        var selected = items[1];
        Assert.Equal(selected, grid.SelectedItem);
        Assert.Equal(1, selectionModel.SelectedIndex);

        items.Insert(0, "Z");
        grid.UpdateLayout();

        Assert.Equal(selected, grid.SelectedItem);
        Assert.Contains(selected, grid.SelectedItems.Cast<object>());
        Assert.Equal(items.IndexOf(selected), selectionModel.SelectedIndex);
    }

    [AvaloniaFact]
    public void SelectionModel_Reflects_Grid_Selection_Changes()
    {
        var items = new ObservableCollection<string> { "A", "B", "C" };
        var selectionModel = new SelectionModel<string> { SingleSelect = false };

        var grid = CreateGrid(items);
        grid.Selection = selectionModel;
        grid.UpdateLayout();

        grid.SelectedItem = items[2];
        grid.UpdateLayout();
        Assert.Equal(items[2], selectionModel.SelectedItem);
        Assert.Equal(2, selectionModel.SelectedIndex);

        selectionModel.Clear();
        selectionModel.Select(0);
        grid.UpdateLayout();

        Assert.Equal(items[0], grid.SelectedItem);
        Assert.Equal(0, grid.SelectedIndex);
    }

    private static DataGrid CreateGrid(IEnumerable items)
    {
        var root = new Window
        {
            Width = 250,
            Height = 150,
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
            ItemsSource = items,
            SelectionMode = DataGridSelectionMode.Extended,
        };

        grid.Columns.Add(new DataGridTextColumn
        {
            Header = "Value",
            Binding = new Binding(".")
        });

        root.Content = grid;
        root.Show();
        return grid;
    }

    private static IReadOnlyList<DataGridRow> GetRows(DataGrid grid)
    {
        return grid.GetSelfAndVisualDescendants().OfType<DataGridRow>().ToList();
    }
}
