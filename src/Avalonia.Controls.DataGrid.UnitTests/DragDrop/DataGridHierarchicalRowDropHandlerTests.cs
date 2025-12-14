// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.DataGridDragDrop;
using Avalonia.Controls.DataGridHierarchical;
using Avalonia.Data;
using Avalonia.Input;
using Xunit;
using AvaloniaDragDrop = Avalonia.Input.DragDrop;

namespace Avalonia.Controls.DataGridTests.DragDrop;

public class DataGridHierarchicalRowDropHandlerTests
{
    [Fact]
    public void Execute_Moves_Node_Within_Same_Parent()
    {
        var (grid, model, root, _, handler) = CreateGrid();
        var nodeA = root.MutableChildren[0];
        var target = root.MutableChildren[2];

        var args = CreateArgs(grid, model, new[] { nodeA }, target, DataGridRowDropPosition.After);

        Assert.True(handler.Execute(args));
        Assert.Equal(new[] { "B", "C", "A" }, GetChildNames(root));
    }

    [Fact]
    public void Execute_Moves_Node_To_Different_Parent()
    {
        var (grid, model, root, secondary, handler) = CreateGrid();
        var nodeA1 = root.MutableChildren[0].MutableChildren[0];
        var target = secondary.MutableChildren[0];

        var args = CreateArgs(grid, model, new[] { nodeA1 }, target, DataGridRowDropPosition.Before);

        Assert.True(handler.Execute(args));
        Assert.Equal(new[] { "A2" }, GetChildNames(root.MutableChildren[0]));
        Assert.Equal(new[] { "A1", "B1" }, GetChildNames(secondary));
    }

    [Fact]
    public void Validate_Fails_When_Target_Is_Descendant()
    {
        var (grid, model, root, _, handler) = CreateGrid();
        var parent = root.MutableChildren[0];
        var child = parent.MutableChildren[0];

        var args = CreateArgs(grid, model, new[] { parent }, child, DataGridRowDropPosition.Before);

        Assert.False(handler.Validate(args));
        Assert.False(handler.Execute(args));
    }

    [Fact]
    public void Execute_Drops_Node_Inside_Target()
    {
        var (grid, model, root, _, handler) = CreateGrid();
        var nodeB = root.MutableChildren[1];
        var target = root.MutableChildren[0];

        var args = CreateArgs(grid, model, new[] { nodeB }, target, DataGridRowDropPosition.Inside);

        Assert.True(handler.Execute(args));
        Assert.Equal(new[] { "A", "C" }, GetChildNames(root));
        Assert.Equal(new[] { "A1", "A2", "B" }, GetChildNames(target));
    }

    private static (DataGrid Grid, HierarchicalModel Model, HierarchicalNode Root, HierarchicalNode Secondary, DataGridHierarchicalRowReorderHandler Handler) CreateGrid()
    {
        var rootItem = new TreeNode("Root", new ObservableCollection<TreeNode>
        {
            new("A", new ObservableCollection<TreeNode>
            {
                new("A1"),
                new("A2"),
            }),
            new("B", new ObservableCollection<TreeNode>
            {
                new("B1"),
            }),
            new("C")
        });

        var options = new HierarchicalOptions<TreeNode>
        {
            ChildrenSelector = x => x.Children,
            AutoExpandRoot = true
        };

        var model = new HierarchicalModel<TreeNode>(options);
        model.SetRoot(rootItem);
        HierarchicalModel untyped = model;

        var grid = new DataGrid
        {
            HierarchicalModel = model,
            HierarchicalRowsEnabled = true,
            CanUserReorderRows = true,
            AutoGenerateColumns = false
        };

        grid.ColumnsInternal.Add(new DataGridTextColumn
        {
            Header = "Name",
            Binding = new Binding("Item.Name")
        });

        var handler = new DataGridHierarchicalRowReorderHandler();

        return (grid, untyped, untyped.Root!, untyped.Root!.MutableChildren[1], handler);
    }

    private static DataGridRowDropEventArgs CreateArgs(
        DataGrid grid,
        HierarchicalModel model,
        IReadOnlyList<HierarchicalNode> dragNodes,
        HierarchicalNode target,
        DataGridRowDropPosition position)
    {
        IList? list = grid.ItemsSource as IList;
        var items = dragNodes.Cast<object>().ToList();
        var indices = dragNodes.Select(model.IndexOf).ToList();
        var targetIndex = model.IndexOf(target);
        var insertIndex = position switch
        {
            DataGridRowDropPosition.After => targetIndex + 1,
            DataGridRowDropPosition.Inside => target.MutableChildren.Count,
            _ => targetIndex
        };

        var dragEvent = new DragEventArgs(
            AvaloniaDragDrop.DropEvent,
            new DataTransfer(),
            grid,
            new Avalonia.Point(),
            KeyModifiers.None)
        {
            RoutedEvent = AvaloniaDragDrop.DropEvent,
            Source = grid
        };

        return new DataGridRowDropEventArgs(
            grid,
            list,
            items,
            indices,
            target,
            targetIndex,
            insertIndex,
            null,
            position,
            isSameGrid: true,
            DragDropEffects.Move,
            dragEvent);
    }

    private static IReadOnlyList<string> GetChildNames(HierarchicalNode node)
    {
        return node.MutableChildren.Select(x => x.Item).OfType<TreeNode>().Select(x => x.Name).ToList();
    }

    private class TreeNode
    {
        public TreeNode(string name, ObservableCollection<TreeNode>? children = null)
        {
            Name = name;
            Children = children ?? new ObservableCollection<TreeNode>();
        }

        public string Name { get; }

        public ObservableCollection<TreeNode> Children { get; }
    }
}
