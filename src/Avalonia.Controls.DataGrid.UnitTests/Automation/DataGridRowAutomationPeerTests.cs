// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System.Collections.Generic;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.DataGridHierarchical;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Avalonia.Controls.DataGridTests.Automation;

public sealed class DataGridRowAutomationPeerTests
{
    [AvaloniaFact]
    public void HierarchicalRow_ExposesExpandCollapseProvider()
    {
        var rootItem = new TreeItem("Root", new TreeItem("Child"));
        var model = CreateModel(rootItem);
        var grid = new DataGrid { HierarchicalModel = model };
        var row = new DataGridRow
        {
            OwningGrid = grid,
            DataContext = model.Root
        };
        var provider = Assert.IsAssignableFrom<IExpandCollapseProvider>(new DataGridRowAutomationPeer(row));

        Assert.Equal(ExpandCollapseState.Collapsed, provider.ExpandCollapseState);

        provider.Expand();

        Assert.Equal(ExpandCollapseState.Expanded, provider.ExpandCollapseState);
        Assert.True(model.Root!.IsExpanded);

        provider.Collapse();

        Assert.Equal(ExpandCollapseState.Collapsed, provider.ExpandCollapseState);
        Assert.False(model.Root.IsExpanded);
    }

    [AvaloniaFact]
    public void LeafAndRecycledRows_ReportCurrentHierarchyState()
    {
        var model = CreateModel(new TreeItem("Root", new TreeItem("Child")));
        model.Expand(model.Root!);
        var row = new DataGridRow { DataContext = model.GetNode(1) };
        var provider = Assert.IsAssignableFrom<IExpandCollapseProvider>(new DataGridRowAutomationPeer(row));

        Assert.Equal(ExpandCollapseState.LeafNode, provider.ExpandCollapseState);

        row.DataContext = model.Root;

        Assert.Equal(ExpandCollapseState.Expanded, provider.ExpandCollapseState);
    }

    private static HierarchicalModel CreateModel(TreeItem root)
    {
        var model = new HierarchicalModel(new HierarchicalOptions
        {
            ChildrenSelector = item => ((TreeItem)item).Children,
            IsLeafSelector = item => ((TreeItem)item).Children.Count == 0
        });
        model.SetRoot(root);
        return model;
    }

    private sealed class TreeItem
    {
        public TreeItem(string name, params TreeItem[] children)
        {
            Name = name;
            Children = children;
        }

        public string Name { get; }

        public IReadOnlyList<TreeItem> Children { get; }
    }
}
