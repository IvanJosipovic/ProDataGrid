// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Controls.DataGridHierarchical;
using Xunit;

namespace Avalonia.Controls.DataGridTests.Hierarchical;

public class HierarchicalAdapterTests
{
    private class Item
    {
        public Item(string name)
        {
            Name = name;
            Children = new ObservableCollection<Item>();
        }

        public string Name { get; }

        public ObservableCollection<Item> Children { get; }
    }

    private static HierarchicalModel CreateModel()
    {
        return new HierarchicalModel(new HierarchicalOptions
        {
            ChildrenSelector = item => ((Item)item).Children
        });
    }

    [Fact]
    public void CountAndIndex_UseModel()
    {
        var root = new Item("root");
        var child = new Item("child");
        root.Children.Add(child);

        var model = CreateModel();
        var adapter = new DataGridHierarchicalAdapter(model);
        adapter.SetRoot(root);
        adapter.Expand(0);

        Assert.Equal(2, adapter.Count);
        Assert.Same(child, adapter.ItemAt(1));
        Assert.Equal(1, adapter.LevelAt(1));
        Assert.Equal(1, adapter.IndexOfItem(child));
        Assert.Equal(1, adapter.IndexOfNode(adapter.NodeAt(1)));
    }

    [Fact]
    public void ToggleAndCollapse_DelegateToModel()
    {
        var root = new Item("root");
        var child = new Item("child");
        child.Children.Add(new Item("grand"));
        root.Children.Add(child);

        var model = CreateModel();
        var adapter = new DataGridHierarchicalAdapter(model);
        adapter.SetRoot(root);
        adapter.Expand(0);

        Assert.False(adapter.NodeAt(1).IsExpanded);
        adapter.Toggle(1);
        Assert.True(adapter.NodeAt(1).IsExpanded);
        adapter.Collapse(1);
        Assert.False(adapter.NodeAt(1).IsExpanded);
    }

    [Fact]
    public void ExpandAll_And_CollapseAll_Work()
    {
        var root = new Item("root");
        var child = new Item("child");
        var grand = new Item("grand");
        child.Children.Add(grand);
        root.Children.Add(child);

        var model = CreateModel();
        var adapter = new DataGridHierarchicalAdapter(model);
        adapter.SetRoot(root);

        adapter.ExpandAll();
        Assert.True(model.Root!.IsExpanded);
        Assert.True(model.GetNode(1).IsExpanded);
        Assert.Equal(3, adapter.Count);

        adapter.CollapseAll(minDepth: 1);
        Assert.True(model.Root!.IsExpanded);
        Assert.False(model.GetNode(1).IsExpanded);
        Assert.Equal(2, adapter.Count);
    }

    [Fact]
    public void FlattenedChanged_IsRaised()
    {
        var root = new Item("root");
        root.Children.Add(new Item("child"));

        var model = CreateModel();
        var adapter = new DataGridHierarchicalAdapter(model);
        adapter.SetRoot(root);

        int calls = 0;
        adapter.FlattenedChanged += (_, __) => calls++;

        adapter.Expand(0);

        Assert.Equal(1, calls);
    }
}
