// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.DataGridHierarchical;
using Avalonia.Controls.DataGridSorting;
using Avalonia.Input;
using Avalonia.Controls.DataGridFiltering;
using Avalonia.Controls.DataGridSelection;
using Avalonia.Controls.Selection;
using Xunit;

namespace Avalonia.Controls.DataGridTests.Hierarchical;

public class HierarchicalIntegrationTests
{
    private class Item
    {
        public Item(string name)
        {
            Name = name;
            Children = new ObservableCollection<Item>();
        }

        public string Name { get; set; }

        public ObservableCollection<Item> Children { get; set; }

        public long Size { get; set; }
    }

    private static HierarchicalModel CreateModel()
    {
        return new HierarchicalModel(new HierarchicalOptions
        {
            ChildrenSelector = o => ((Item)o).Children
        });
    }

    private static IComparer<object> BuildComparer(IReadOnlyList<SortingDescriptor> descriptors)
    {
        return Comparer<object>.Create((x, y) =>
        {
            var left = x as Item;
            var right = y as Item;

            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left == null)
            {
                return -1;
            }

            if (right == null)
            {
                return 1;
            }

            foreach (var descriptor in descriptors)
            {
                var path = descriptor.PropertyPath;
                var leftValue = GetPropertyValue(left, path);
                var rightValue = GetPropertyValue(right, path);
                var result = StringComparer.OrdinalIgnoreCase.Compare(leftValue, rightValue);

                if (result != 0)
                {
                    return descriptor.Direction == ListSortDirection.Descending ? -result : result;
                }
            }

            return 0;
        });
    }

    private static string? GetPropertyValue(Item item, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var type = typeof(Item);
        PropertyInfo? property = type.GetProperty(path) ?? type.GetProperty(path.Replace("Item.", string.Empty));
        var value = property?.GetValue(item);
        return value?.ToString();
    }

    [Fact]
    public void HeaderClick_SortsHierarchyAscending()
    {
        var root = new Item("root");
        root.Children.Add(new Item("b"));
        root.Children.Add(new Item("a"));
        var model = CreateModel();
        model.SetRoot(root);
        model.Expand(model.Root!);

        var sorting = new SortingModel();
        sorting.SortingChanged += (_, e) =>
        {
            var comparer = BuildComparer(e.NewDescriptors);
            model.ApplySiblingComparer(comparer, recursive: true);
        };

        var column = new DataGridTextColumn { SortMemberPath = "Name" };
        var adapter = new LocalHierarchicalSortingAdapter(
            sorting,
            () => new[] { column });

        adapter.HandleHeaderClick(column, KeyModifiers.None);

        Assert.Equal("a", ((Item)model.GetItem(1)!).Name);
        Assert.Equal("b", ((Item)model.GetItem(2)!).Name);
    }

    [Fact]
    public void HeaderClick_TogglesDescendingOnSecondClick()
    {
        var root = new Item("root");
        root.Children.Add(new Item("a"));
        root.Children.Add(new Item("b"));
        var model = CreateModel();
        model.SetRoot(root);
        model.Expand(model.Root!);

        var sorting = new SortingModel();
        sorting.SortingChanged += (_, e) =>
        {
            var comparer = BuildComparer(e.NewDescriptors);
            model.ApplySiblingComparer(comparer, recursive: true);
        };

        var column = new DataGridTextColumn { SortMemberPath = "Name" };
        var adapter = new LocalHierarchicalSortingAdapter(
            sorting,
            () => new[] { column });

        adapter.HandleHeaderClick(column, KeyModifiers.None); // ascending
        adapter.HandleHeaderClick(column, KeyModifiers.None); // descending

        Assert.Equal("b", ((Item)model.GetItem(1)!).Name);
        Assert.Equal("a", ((Item)model.GetItem(2)!).Name);
    }

    [Fact]
    public void Selection_CanBeRemappedAfterSort()
    {
        var root = new Item("root");
        var childA = new Item("b");
        var childB = new Item("a");
        root.Children.Add(childA);
        root.Children.Add(childB);

        var model = CreateModel();
        model.SetRoot(root);
        model.Expand(model.Root!);

        var selection = new SelectionModel<object>();
        selection.Select(1); // selects childA in initial order

        var sorting = new SortingModel();
        sorting.SortingChanged += (_, e) =>
        {
            var comparer = BuildComparer(e.NewDescriptors);
            model.ApplySiblingComparer(comparer, recursive: true);
        };

        var column = new DataGridTextColumn { SortMemberPath = "Name" };
        var adapter = new LocalHierarchicalSortingAdapter(
            sorting,
            () => new[] { column });

        adapter.HandleHeaderClick(column, KeyModifiers.None); // ascending

        var newIndex = model.IndexOf(childA);
        Assert.Equal(2, newIndex); // childA moved after sorting
        selection.Clear();
        selection.Select(newIndex);
        Assert.True(selection.IsSelected(newIndex));
    }

    [Fact]
    public void FilteringModel_Filters_By_Name_Contains()
    {
        var items = new[]
        {
            new Item("alpha"),
            new Item("beta"),
            new Item("alphabet")
        };

        var filtering = new FilteringModel();
        filtering.SetOrUpdate(new FilteringDescriptor(
            columnId: "col",
            @operator: FilteringOperator.Contains,
            propertyPath: "Name",
            value: "alpha",
            values: null,
            predicate: null,
            culture: null,
            stringComparison: StringComparison.OrdinalIgnoreCase));

        var descriptor = Assert.Single(filtering.Descriptors);
        bool Predicate(Item item)
        {
            var name = item.Name ?? string.Empty;
            return name.IndexOf(descriptor.Value?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        var filtered = items.Where(Predicate).ToArray();
        Assert.Equal(2, filtered.Length);
        Assert.Contains(filtered, x => x.Name == "alpha");
        Assert.Contains(filtered, x => x.Name == "alphabet");
    }

    private sealed class LocalHierarchicalSortingAdapter : DataGridSortingAdapter
    {
        public LocalHierarchicalSortingAdapter(
            ISortingModel model,
            Func<IEnumerable<DataGridColumn>> columnProvider)
            : base(model, columnProvider, null, null)
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
