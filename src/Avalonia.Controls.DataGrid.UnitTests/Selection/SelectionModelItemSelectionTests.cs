// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Controls.DataGridHierarchical;
using Avalonia.Controls.Selection;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Avalonia.Controls.DataGridTests.Selection;

public class SelectionModelItemSelectionTests
{
    private sealed class NodeItem
    {
        public NodeItem(string name)
        {
            Name = name;
            Children = new ObservableCollection<NodeItem>();
        }

        public string Name { get; }

        public ObservableCollection<NodeItem> Children { get; }
    }

    [Fact]
    public void SelectionModel_Select_By_Item_Uses_Source_Index()
    {
        var items = new List<string> { "A", "B", "C" };
        var selection = new SelectionModel<string> { Source = items };

        selection.Select("B");

        Assert.Equal(1, selection.SelectedIndex);
        Assert.Equal("B", selection.SelectedItem);
        Assert.Contains(1, selection.SelectedIndexes);
    }

    [Fact]
    public void SelectionModel_Select_By_Item_Resolves_Hierarchical_Nodes()
    {
        var root = new NodeItem("root");
        var child = new NodeItem("child");
        root.Children.Add(child);

        var model = CreateModel(root);
        var selection = new SelectionModel<object> { Source = model.Flattened };

        selection.Select(child);

        Assert.Contains(1, selection.SelectedIndexes);
        var selectedNode = Assert.IsAssignableFrom<IHierarchicalNodeItem>(selection.SelectedItem);
        Assert.Same(child, selectedNode.Item);
    }

    [Fact]
    public void SelectionModel_Select_By_Item_Throws_When_Not_Found()
    {
        var items = new List<string> { "A", "B" };
        var selection = new SelectionModel<string> { Source = items };

        var exception = Assert.Throws<ArgumentException>(() => selection.Select("C"));
        Assert.Contains("Item not found", exception.Message);
    }

    [Fact]
    public void SelectionModel_Select_By_Item_With_Null_Source_Sets_SelectedItem()
    {
        var selection = new SelectionModel<string>();

        selection.Select("B");

        Assert.Equal("B", selection.SelectedItem);
        Assert.Single(selection.SelectedItems);
        Assert.Equal("B", selection.SelectedItems[0]);
    }

    [Fact]
    public void SelectionModel_Select_By_Item_With_Null_Source_Allows_MultiSelect()
    {
        var selection = new SelectionModel<string> { SingleSelect = false };

        selection.Select("B");

        Assert.Equal("B", selection.SelectedItem);
        Assert.Single(selection.SelectedItems);
        Assert.Equal("B", selection.SelectedItems[0]);
    }

    [Fact]
    public void SelectionModel_SelectRange_By_Item_Uses_Source_Index()
    {
        var items = new List<string> { "A", "B", "C", "D" };
        var selection = new SelectionModel<string> { Source = items, SingleSelect = false };

        selection.SelectRange("B", "D");

        Assert.Equal(3, selection.SelectedIndexes.Count);
        Assert.Contains(1, selection.SelectedIndexes);
        Assert.Contains(2, selection.SelectedIndexes);
        Assert.Contains(3, selection.SelectedIndexes);
    }

    [Fact]
    public void SelectionModel_SelectRange_By_Item_Resolves_Hierarchical_Nodes()
    {
        var root = new NodeItem("root");
        var childA = new NodeItem("childA");
        var childB = new NodeItem("childB");
        root.Children.Add(childA);
        root.Children.Add(childB);

        var model = CreateModel(root);
        var selection = new SelectionModel<object> { Source = model.Flattened, SingleSelect = false };

        selection.SelectRange(childA, childB);

        Assert.Equal(2, selection.SelectedIndexes.Count);
        Assert.Contains(1, selection.SelectedIndexes);
        Assert.Contains(2, selection.SelectedIndexes);
    }

    [Fact]
    public void SelectionModel_SelectRange_By_Item_Throws_When_Not_Found()
    {
        var items = new List<string> { "A", "B" };
        var selection = new SelectionModel<string> { Source = items, SingleSelect = false };

        var exception = Assert.Throws<ArgumentException>(() => selection.SelectRange("A", "C"));
        Assert.Contains("Item not found", exception.Message);
    }

    [Fact]
    public void SelectionModel_SelectRange_By_Item_With_Null_Source_Throws_For_MultiSelect()
    {
        var selection = new SelectionModel<string> { SingleSelect = false };

        Assert.Throws<InvalidOperationException>(() => selection.SelectRange("A", "B"));
    }

    [Fact]
    public void SelectionModel_SelectRange_By_Item_With_Null_Source_Sets_SelectedItem_When_SingleSelect()
    {
        var selection = new SelectionModel<string> { SingleSelect = true };

        selection.SelectRange("A", "B");

        Assert.Equal("B", selection.SelectedItem);
        Assert.Single(selection.SelectedItems);
        Assert.Equal("B", selection.SelectedItems[0]);
    }

    [Fact]
    public void SelectionModel_SelectRange_By_Items_Uses_Source_Index()
    {
        var items = new List<string> { "A", "B", "C", "D" };
        var selection = new SelectionModel<string> { Source = items, SingleSelect = false };

        selection.SelectRange(new[] { "B", "D" });

        Assert.Equal(2, selection.SelectedIndexes.Count);
        Assert.Contains(1, selection.SelectedIndexes);
        Assert.Contains(3, selection.SelectedIndexes);
    }

    [Fact]
    public void SelectionModel_SelectRange_By_Items_Resolves_Hierarchical_Nodes()
    {
        var root = new NodeItem("root");
        var childA = new NodeItem("childA");
        var childB = new NodeItem("childB");
        root.Children.Add(childA);
        root.Children.Add(childB);

        var model = CreateModel(root);
        var selection = new SelectionModel<object> { Source = model.Flattened, SingleSelect = false };

        selection.SelectRange(new object[] { childA, childB });

        Assert.Equal(2, selection.SelectedIndexes.Count);
        Assert.Contains(1, selection.SelectedIndexes);
        Assert.Contains(2, selection.SelectedIndexes);
    }

    [Fact]
    public void SelectionModel_SelectRange_By_Items_Throws_When_Not_Found()
    {
        var items = new List<string> { "A", "B" };
        var selection = new SelectionModel<string> { Source = items, SingleSelect = false };

        var exception = Assert.Throws<ArgumentException>(() => selection.SelectRange(new[] { "A", "C" }));
        Assert.Contains("Item not found", exception.Message);
    }

    [Fact]
    public void SelectionModel_SelectRange_By_Items_With_Null_Source_Throws_For_MultiSelect()
    {
        var selection = new SelectionModel<string> { SingleSelect = false };

        Assert.Throws<InvalidOperationException>(() => selection.SelectRange(new[] { "A", "B" }));
    }

    [Fact]
    public void SelectionModel_SelectRange_By_Items_With_Null_Source_Allows_Single_Item()
    {
        var selection = new SelectionModel<string> { SingleSelect = false };

        selection.SelectRange(new[] { "A" });

        Assert.Equal("A", selection.SelectedItem);
        Assert.Single(selection.SelectedItems);
        Assert.Equal("A", selection.SelectedItems[0]);
    }

    [Fact]
    public void SelectionModel_SelectRange_By_Items_With_Null_Source_Sets_SelectedItem_When_SingleSelect()
    {
        var selection = new SelectionModel<string> { SingleSelect = true };

        selection.SelectRange(new[] { "A", "B" });

        Assert.Equal("B", selection.SelectedItem);
        Assert.Single(selection.SelectedItems);
        Assert.Equal("B", selection.SelectedItems[0]);
    }

    [AvaloniaFact]
    public void DataGrid_Selection_Select_By_Item_Selects_Hierarchical_Item()
    {
        var root = new NodeItem("root");
        var child = new NodeItem("child");
        root.Children.Add(child);

        var model = CreateModel(root);
        var grid = new DataGrid
        {
            AutoGenerateColumns = false,
            HierarchicalModel = model,
            HierarchicalRowsEnabled = true,
            ItemsSource = model.Flattened
        };

        grid.Columns.Add(new DataGridHierarchicalColumn
        {
            Header = "Name",
            Binding = new Binding("Item.Name")
        });

        grid.ApplyTemplate();
        grid.UpdateLayout();

        grid.Selection.Select(child);

        Assert.Equal(1, grid.Selection.SelectedIndex);
        Assert.Same(child, grid.Selection.SelectedItem);
    }

    private static HierarchicalModel<NodeItem> CreateModel(NodeItem root)
    {
        var model = new HierarchicalModel<NodeItem>(new HierarchicalOptions<NodeItem>
        {
            ChildrenSelector = item => item.Children,
            AutoExpandRoot = true
        });

        model.SetRoot(root);
        model.Expand(model.Root ?? throw new InvalidOperationException("Root node not created."));
        return model;
    }
}
