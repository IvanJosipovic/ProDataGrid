using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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

    [AvaloniaFact]
    public void SelectedItems_Preserved_When_Adding_Item_Before_Current_In_Sorted_View()
    {
        var items = new ObservableCollection<SortableItem>(Enumerable.Range(1, 5).Select(i => new SortableItem(i)));
        var view = new DataGridCollectionView(items);
        var grid = CreateSortableGrid(view);

        ApplyIdSort(view, ListSortDirection.Descending);
        grid.UpdateLayout();

        grid.SelectedItem = items[1];
        grid.SelectedItems.Add(items[2]);
        grid.UpdateLayout();

        var selectedIds = grid.SelectedItems.Cast<SortableItem>().Select(x => x.Id).OrderBy(x => x).ToList();
        Assert.Equal(new[] { 2, 3 }, selectedIds);

        items.Add(new SortableItem(6));
        grid.UpdateLayout();

        selectedIds = grid.SelectedItems.Cast<SortableItem>().Select(x => x.Id).OrderBy(x => x).ToList();
        Assert.Equal(new[] { 2, 3 }, selectedIds);
        Assert.Equal(view.IndexOf(items[1]), grid.SelectedIndex);
    }

    [AvaloniaFact]
    public void SelectedItems_Survive_Repeated_Adds_And_Sorts()
    {
        var items = new ObservableCollection<SortableItem>(Enumerable.Range(1, 6).Select(i => new SortableItem(i)));
        var view = new DataGridCollectionView(items);
        var grid = CreateSortableGrid(view);

        grid.UpdateLayout();

        grid.SelectedItem = items[0];
        grid.SelectedItems.Add(items[1]);
        grid.SelectedItems.Add(items[2]);
        grid.UpdateLayout();

        items.Add(new SortableItem(7));

        ApplyIdSort(view, ListSortDirection.Ascending);
        grid.UpdateLayout();

        items.Add(new SortableItem(8));

        ApplyIdSort(view, ListSortDirection.Descending);
        grid.UpdateLayout();

        items.Add(new SortableItem(9));
        grid.UpdateLayout();

        var selectedIds = grid.SelectedItems.Cast<SortableItem>().Select(x => x.Id).OrderBy(x => x).ToList();
        Assert.Equal(new[] { 1, 2, 3 }, selectedIds);
        var current = Assert.IsType<SortableItem>(grid.SelectedItem);
        Assert.Contains(current, grid.SelectedItems.Cast<SortableItem>());
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

    private static DataGrid CreateSortableGrid(IEnumerable items)
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
            Header = "Id",
            Binding = new Binding(nameof(SortableItem.Id)),
            SortMemberPath = nameof(SortableItem.Id)
        });

        grid.Columns.Add(new DataGridTextColumn
        {
            Header = "Name",
            Binding = new Binding(nameof(SortableItem.Name))
        });

        root.Content = grid;
        root.Show();
        return grid;
    }

    private static void ApplyIdSort(DataGridCollectionView view, ListSortDirection direction)
    {
        using (view.DeferRefresh())
        {
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(DataGridSortDescription.FromPath(nameof(SortableItem.Id), direction));
        }
    }

    private static IReadOnlyList<DataGridRow> GetRows(DataGrid grid)
    {
        return grid.GetSelfAndVisualDescendants().OfType<DataGridRow>().ToList();
    }

    private class SortableItem
    {
        public SortableItem(int id, string? name = null)
        {
            Id = id;
            Name = name ?? $"Item {id}";
        }

        public int Id { get; }

        public string Name { get; }
    }

    private class SelectionViewModel
    {
        public ObservableCollection<string> Items { get; } =
            new(Enumerable.Range(0, 6).Select(x => $"Item {x}").ToList());

        public ObservableCollection<object> SelectedItems { get; } = new();
    }
}
