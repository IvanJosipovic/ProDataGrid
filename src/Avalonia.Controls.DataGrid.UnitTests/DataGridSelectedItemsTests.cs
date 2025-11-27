using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.DataGridTests;

public class DataGridSelectedItemsTests
{
    [AvaloniaFact]
    public void SelectedItems_Raises_CollectionChanged_On_Selection_Change()
    {
        var items = new ObservableCollection<string> { "A", "B", "C" };
        var grid = CreateGrid(items);
        var changes = new List<NotifyCollectionChangedEventArgs>();

        ((INotifyCollectionChanged)grid.SelectedItems).CollectionChanged += (_, e) => changes.Add(e);

        grid.SelectedItem = items[1];
        grid.SelectedItem = items[2];

        Assert.Collection(
            changes,
            e =>
            {
                Assert.Equal(NotifyCollectionChangedAction.Add, e.Action);
                var newItems = Assert.IsAssignableFrom<IList>(e.NewItems);
                Assert.Equal("B", Assert.Single(newItems.Cast<string>()));
            },
            e =>
            {
                Assert.Equal(NotifyCollectionChangedAction.Remove, e.Action);
                var oldItems = Assert.IsAssignableFrom<IList>(e.OldItems);
                Assert.Equal("B", Assert.Single(oldItems.Cast<string>()));
            },
            e =>
            {
                Assert.Equal(NotifyCollectionChangedAction.Add, e.Action);
                var newItems = Assert.IsAssignableFrom<IList>(e.NewItems);
                Assert.Equal("C", Assert.Single(newItems.Cast<string>()));
            });
    }

    [AvaloniaFact]
    public void SelectedItems_Binding_Applies_ViewModel_Selection()
    {
        var vm = new SelectionViewModel();
        vm.SelectedItems.Add(vm.Items[1]);
        vm.SelectedItems.Add(vm.Items[3]);

        var grid = CreateGrid(vm.Items);
        grid.Bind(DataGrid.SelectedItemsProperty, new Binding(nameof(SelectionViewModel.SelectedItems))
        {
            Mode = BindingMode.TwoWay,
            Source = vm
        });

        grid.UpdateLayout();

        var rows = GetRows(grid);
        Assert.True(rows.First(x => x.Index == 1).IsSelected);
        Assert.True(rows.First(x => x.Index == 3).IsSelected);
        Assert.All(rows.Where(x => x.Index != 1 && x.Index != 3), r => Assert.False(r.IsSelected));
    }

    [AvaloniaFact]
    public void SelectedItems_Binding_Updates_ViewModel_When_Selection_Changes()
    {
        var vm = new SelectionViewModel();
        var grid = CreateGrid(vm.Items);

        grid.Bind(DataGrid.SelectedItemsProperty, new Binding(nameof(SelectionViewModel.SelectedItems))
        {
            Mode = BindingMode.TwoWay,
            Source = vm
        });

        grid.SelectAll();
        grid.UpdateLayout();

        Assert.Equal(vm.Items.Count, vm.SelectedItems.Count);

        grid.SelectedItem = null;
        grid.UpdateLayout();

        Assert.Empty(vm.SelectedItems);
    }

    [AvaloniaFact]
    public void Modifying_Bound_SelectedItems_Updates_DataGrid()
    {
        var vm = new SelectionViewModel();
        var grid = CreateGrid(vm.Items);

        grid.Bind(DataGrid.SelectedItemsProperty, new Binding(nameof(SelectionViewModel.SelectedItems))
        {
            Mode = BindingMode.TwoWay,
            Source = vm
        });

        vm.SelectedItems.Add(vm.Items[2]);
        vm.SelectedItems.Add(vm.Items[4]);

        grid.UpdateLayout();

        var rows = GetRows(grid);
        Assert.True(rows.First(x => x.Index == 2).IsSelected);
        Assert.True(rows.First(x => x.Index == 4).IsSelected);
        Assert.All(rows.Where(x => x.Index != 2 && x.Index != 4), r => Assert.False(r.IsSelected));
    }

    private static DataGrid CreateGrid(IList items)
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

    private class SelectionViewModel
    {
        public ObservableCollection<string> Items { get; } =
            new(Enumerable.Range(0, 6).Select(x => $"Item {x}").ToList());

        public ObservableCollection<object> SelectedItems { get; } = new();
    }
}
