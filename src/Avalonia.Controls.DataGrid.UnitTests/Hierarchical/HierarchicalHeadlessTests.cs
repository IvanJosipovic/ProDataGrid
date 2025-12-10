// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.DataGridHierarchical;
using Avalonia.Controls.DataGridSorting;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.DataGridTests.Hierarchical;

public class HierarchicalHeadlessTests
{
    private class Item
    {
        public Item(string name)
        {
            Name = name;
            Children = new ObservableCollection<Item>();
        }

        public string Name { get; set; }

        public ObservableCollection<Item> Children { get; }
    }

    [AvaloniaFact]
    public void Header_Click_Toggles_Sort_And_Indicators()
    {
        RunSortScenario("Name");
    }

    [AvaloniaFact]
    public void Header_Click_Supports_Nested_SortMemberPath()
    {
        RunSortScenario("Item.Name");
    }

    private static void RunSortScenario(string sortMemberPath)
    {
        var root = new Item("root");
        root.Children.Add(new Item("b"));
        root.Children.Add(new Item("a"));

        var grid = CreateGrid(root, sortMemberPath);

        ClickHeader(grid, "Name");
        grid.UpdateLayout();
        var sorting = Assert.IsAssignableFrom<ISortingModel>(grid.SortingModel);
        Assert.NotEmpty(sorting.Descriptors);
        Assert.Equal(new[] { "a", "b" }, GetRowOrder(grid));
        AssertHeaderSort(grid, "Name", asc: true, desc: false);

        ClickHeader(grid, "Name");
        grid.UpdateLayout();
        Assert.Equal(new[] { "b", "a" }, GetRowOrder(grid));
        AssertHeaderSort(grid, "Name", asc: false, desc: true);
    }

    private static DataGrid CreateGrid(Item root, string sortMemberPath)
    {
        var model = new HierarchicalModel(new HierarchicalOptions
        {
            ChildrenSelector = o => ((Item)o).Children,
            AutoExpandRoot = true,
            MaxAutoExpandDepth = 1,
        });
        model.SetRoot(root);
        model.Expand(model.Root!);

        var view = new DataGridCollectionView(model.Flattened);
        var sortingModel = new SortingModel();
        sortingModel.SortingChanged += (_, e) =>
        {
            if (e.NewDescriptors.Count == 0)
            {
                return;
            }

            var descriptor = e.NewDescriptors[0];
            var comparer = Comparer<object>.Create((x, y) =>
            {
                var a = x as Item;
                var b = y as Item;
                var result = string.Compare(a?.Name, b?.Name, StringComparison.OrdinalIgnoreCase);
                return descriptor.Direction == ListSortDirection.Descending ? -result : result;
            });

            model.ApplySiblingComparer(comparer, recursive: true);
            view.Refresh();
        };

        var grid = new DataGrid
        {
            HierarchicalModel = model,
            HierarchicalRowsEnabled = true,
            CanUserSortColumns = true,
            AutoGenerateColumns = false
        };

        grid.SortingAdapterFactory = new HierarchicalSortingAdapterFactory();
        grid.ItemsSource = view;
        grid.SortingModel = sortingModel;
        EnsureSortingAdapter(grid);
        grid.Styles.Add(new StyleInclude((Uri?)null)
        {
            Source = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Simple.xaml")
        });

        grid.Columns.Add(new DataGridHierarchicalColumn
        {
            Header = "Name",
            Binding = new Avalonia.Data.Binding("Item.Name"),
            SortMemberPath = sortMemberPath,
            Width = new DataGridLength(1, DataGridLengthUnitType.Star)
        });

        var window = new Window
        {
            Width = 400,
            Height = 300,
            Content = grid,
            Styles =
            {
                new StyleInclude((Uri?)null)
                {
                    Source = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Simple.xaml")
                }
            }
        };
        window.Show();
        grid.UpdateLayout();
        return grid;
    }

    private static void ClickHeader(DataGrid grid, string header, KeyModifiers modifiers = KeyModifiers.None)
    {
        var headerCell = GetHeaderCell(grid, header);
        var sortingModel = Assert.IsAssignableFrom<ISortingModel>(grid.SortingModel);
        var before = sortingModel.Descriptors.ToList();

        var method = typeof(DataGridColumnHeader).GetMethod(
            "ProcessSort",
            BindingFlags.Instance | BindingFlags.NonPublic);

        method!.Invoke(headerCell, new object[] { modifiers, null });

        if (before.SequenceEqual(sortingModel.Descriptors))
        {
            var adapterField = typeof(DataGrid).GetField("_sortingAdapter", BindingFlags.Instance | BindingFlags.NonPublic);
            var adapter = adapterField?.GetValue(grid) as DataGridSortingAdapter;
            var column = grid.Columns.First(c => Equals(c.Header, header));
            adapter?.HandleHeaderClick(column, modifiers);
        }
    }

    private static string[] GetRowOrder(DataGrid grid)
    {
        var source = (IEnumerable?)grid.ItemsSource ?? Array.Empty<object>();

        return source.OfType<HierarchicalNode>()
            .Select(n => ((Item)n.Item).Name)
            .Skip(1) // skip root
            .ToArray();
    }

    private static void AssertHeaderSort(DataGrid grid, string header, bool asc, bool desc)
    {
        var headerCell = GetHeaderCell(grid, header);

        Assert.Equal(asc, HasPseudo(headerCell, ":sortascending"));
        Assert.Equal(desc, HasPseudo(headerCell, ":sortdescending"));
    }

    private static DataGridColumnHeader GetHeaderCell(DataGrid grid, string header)
    {
        grid.ApplyTemplate();
        grid.Measure(new Size(400, 300));
        grid.Arrange(new Rect(0, 0, 400, 300));
        grid.UpdateLayout();
        Dispatcher.UIThread.RunJobs();

        if (grid.GetVisualRoot() is null)
        {
            throw new InvalidOperationException("DataGrid is not attached to a visual root.");
        }

        var descendants = grid.GetVisualDescendants().ToList();
        var headerCell = descendants
            .OfType<DataGridColumnHeader>()
            .FirstOrDefault(h => Equals(h.Content, header));

        if (headerCell == null)
        {
            var presenterProp = typeof(DataGrid).GetProperty(
                "ColumnHeaders",
                BindingFlags.Instance | BindingFlags.NonPublic);

            var presenter = presenterProp?.GetValue(grid) as Visual;
            headerCell = presenter?
                .GetVisualDescendants()
                .OfType<DataGridColumnHeader>()
                .FirstOrDefault(h => header == null || Equals(h.Content, header));
        }

        if (headerCell == null)
        {
            var names = descendants.Select(d => d.GetType().Name).Distinct().OrderBy(n => n);
            throw new InvalidOperationException($"No DataGridColumnHeader found. Descendants: {string.Join(", ", names)}");
        }

        return headerCell!;
    }

    private static bool HasPseudo(StyledElement element, string name)
    {
        var prop = typeof(StyledElement).GetProperty("PseudoClasses", BindingFlags.Instance | BindingFlags.NonPublic);
        var pseudo = prop!.GetValue(element);
        var contains = pseudo!.GetType().GetMethod("Contains", new[] { typeof(string) });
        return (bool)contains!.Invoke(pseudo, new object[] { name });
    }

    private static void EnsureSortingAdapter(DataGrid grid)
    {
        var adapterField = typeof(DataGrid).GetField("_sortingAdapter", BindingFlags.Instance | BindingFlags.NonPublic);
        var adapter = adapterField?.GetValue(grid);
        if (adapter != null)
        {
            return;
        }

        var createMethod = typeof(DataGrid).GetMethod("CreateSortingAdapter", BindingFlags.Instance | BindingFlags.NonPublic);
        var updateMethod = typeof(DataGrid).GetMethod("UpdateSortingAdapterView", BindingFlags.Instance | BindingFlags.NonPublic);
        var sortingModel = Assert.IsAssignableFrom<ISortingModel>(grid.SortingModel);

        var created = (DataGridSortingAdapter)createMethod!.Invoke(grid, new object[] { sortingModel })!;
        adapterField!.SetValue(grid, created);
        updateMethod?.Invoke(grid, null);
    }

    private sealed class HierarchicalSortingAdapterFactory : IDataGridSortingAdapterFactory
    {
        public DataGridSortingAdapter Create(DataGrid grid, ISortingModel model)
        {
            return new HierarchicalSortingAdapter(model, () => grid.Columns, null, null);
        }
    }

    private sealed class HierarchicalSortingAdapter : DataGridSortingAdapter
    {
        public HierarchicalSortingAdapter(
            ISortingModel model,
            Func<IEnumerable<DataGridColumn>> columnProvider,
            Action beforeViewRefresh,
            Action afterViewRefresh)
            : base(model, columnProvider, beforeViewRefresh, afterViewRefresh)
        {
        }

        protected override bool TryApplyModelToView(
            IReadOnlyList<SortingDescriptor> descriptors,
            IReadOnlyList<SortingDescriptor> previousDescriptors,
            out bool changed)
        {
            changed = true;
            return true;
        }
    }
}
