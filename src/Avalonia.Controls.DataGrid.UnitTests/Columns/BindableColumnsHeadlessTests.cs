using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.DataGridTests.Columns;

public class BindableColumnsHeadlessTests
{
    [AvaloniaFact]
    public void Columns_Binding_Populates_Headers_When_Templated()
    {
        var vm = new ColumnsViewModel();
        var (window, grid) = CreateWindow(vm);

        window.Show();
        grid.ApplyTemplate();
        grid.UpdateLayout();

        var headers = grid
            .GetVisualDescendants()
            .OfType<DataGridColumnHeader>()
            .Where(h => h.OwningColumn != null && h.OwningColumn is not DataGridFillerColumn)
            .ToList();

        Assert.Equal(vm.Columns.Count, headers.Count);
        Assert.Equal(vm.Columns, headers.Select(h => h.OwningColumn));
        Assert.Equal(vm.Columns.Select(c => c.Header), headers.Select(h => h.Content));
    }

    [AvaloniaFact]
    public void Columns_TwoWay_DisplayIndex_Change_Reorders_Bound_Source()
    {
        var vm = new ColumnsViewModel();
        var (window, grid) = CreateWindow(vm, twoWay: true);

        window.Show();
        grid.ApplyTemplate();
        grid.UpdateLayout();

        var first = vm.Columns[0];
        var second = vm.Columns[1];

        second.DisplayIndex = 0;
        grid.UpdateLayout();

        Assert.Same(second, vm.Columns[0]);
        Assert.Same(first, vm.Columns[1]);
    }

    private static (Window window, DataGrid grid) CreateWindow(ColumnsViewModel vm, bool twoWay = false)
    {
        var root = new Window
        {
            Width = 400,
            Height = 200,
            Styles =
            {
                new StyleInclude((Uri?)null)
                {
                    Source = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Simple.xaml")
                },
            },
            DataContext = vm
        };

        var grid = new DataGrid
        {
            AutoGenerateColumns = false,
            ColumnsSynchronizationMode = twoWay ? ColumnsSynchronizationMode.TwoWay : ColumnsSynchronizationMode.OneWayToGrid
        };

        grid.Bind(DataGrid.ColumnsProperty, new Binding("Columns")
        {
            Mode = twoWay ? BindingMode.TwoWay : BindingMode.OneWay
        });
        grid.Bind(DataGrid.ItemsSourceProperty, new Binding("Items"));

        root.Content = grid;
        return (root, grid);
    }

    private sealed class ColumnsViewModel
    {
        public ColumnsViewModel()
        {
            Items = new ObservableCollection<Person>
            {
                new() { Name = "First" },
                new() { Name = "Second" }
            };

            Columns = new ObservableCollection<DataGridColumn>
            {
                new DataGridTextColumn { Header = "Name", Binding = new Binding("Name") },
                new DataGridTextColumn { Header = "Length", Binding = new Binding("Name.Length") },
            };
        }

        public ObservableCollection<Person> Items { get; }

        public ObservableCollection<DataGridColumn> Columns { get; }
    }

    private sealed class Person
    {
        public string Name { get; set; } = string.Empty;
    }
}
