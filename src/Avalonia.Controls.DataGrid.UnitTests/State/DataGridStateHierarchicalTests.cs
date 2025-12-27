// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Controls.DataGridHierarchical;
using Avalonia.Controls.DataGridTests;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Xunit;

namespace Avalonia.Controls.DataGridTests.State;

public class DataGridStateHierarchicalTests
{
    private sealed class TreeItem
    {
        public TreeItem(int id, string name)
        {
            Id = id;
            Name = name;
            Children = new ObservableCollection<TreeItem>();
        }

        public int Id { get; }

        public string Name { get; }

        public ObservableCollection<TreeItem> Children { get; }
    }

    [AvaloniaFact]
    public void CaptureAndRestoreHierarchicalState_RestoresExpandedNodes()
    {
        var rootItem = new TreeItem(0, "Root");
        var childItem = new TreeItem(1, "Child");
        var grandChildItem = new TreeItem(2, "GrandChild");
        childItem.Children.Add(grandChildItem);
        rootItem.Children.Add(childItem);

        var model = new HierarchicalModel<TreeItem>(new HierarchicalOptions<TreeItem>
        {
            ChildrenSelector = item => item.Children,
        });
        model.SetRoot(rootItem);

        var grid = new DataGrid
        {
            HierarchicalModel = model,
            HierarchicalRowsEnabled = true,
            AutoGenerateColumns = false,
            ItemsSource = model.Flattened,
        };

        grid.ColumnsInternal.Add(new DataGridHierarchicalColumn
        {
            Header = "Name",
            Binding = new Binding("Item.Name"),
        });

        var root = new Window
        {
            Width = 500,
            Height = 300,
            Content = grid,
        };

        root.SetThemeStyles();
        root.Show();
        grid.UpdateLayout();
        Dispatcher.UIThread.RunJobs();

        try
        {
            var rootNode = model.Root ?? throw new InvalidOperationException("Root node missing.");
            model.Expand(rootNode);
            var childNode = model.FindNode(childItem) ?? throw new InvalidOperationException("Child node missing.");
            model.Expand(childNode);

            var state = grid.CaptureHierarchicalState();
            Assert.NotNull(state);

            model.CollapseAll();
            Assert.False((model.Root ?? rootNode).IsExpanded);

            grid.RestoreHierarchicalState(state);
            Dispatcher.UIThread.RunJobs();

            Assert.True((model.Root ?? rootNode).IsExpanded);
            var restoredChild = model.FindNode(childItem) ?? throw new InvalidOperationException("Restored child missing.");
            Assert.True(restoredChild.IsExpanded);
        }
        finally
        {
            root.Close();
        }
    }

    [AvaloniaFact]
    public void CaptureAndRestoreHierarchicalState_UsesPathKeys()
    {
        var rootItem = new TreeItem(0, "Root");
        var childA = new TreeItem(1, "Child A");
        var childB = new TreeItem(2, "Child B");
        rootItem.Children.Add(childA);
        rootItem.Children.Add(childB);

        var model = new HierarchicalModel<TreeItem>(new HierarchicalOptions<TreeItem>
        {
            ChildrenSelector = item => item.Children,
            ExpandedStateKeyMode = ExpandedStateKeyMode.Path,
        });
        model.SetRoot(rootItem);

        var grid = new DataGrid
        {
            HierarchicalModel = model,
            HierarchicalRowsEnabled = true,
            AutoGenerateColumns = false,
            ItemsSource = model.Flattened,
        };

        grid.ColumnsInternal.Add(new DataGridHierarchicalColumn
        {
            Header = "Name",
            Binding = new Binding("Item.Name"),
        });

        var root = new Window
        {
            Width = 500,
            Height = 300,
            Content = grid,
        };

        root.SetThemeStyles();
        root.Show();
        grid.UpdateLayout();
        Dispatcher.UIThread.RunJobs();

        try
        {
            var rootNode = model.Root ?? throw new InvalidOperationException("Root node missing.");
            model.Expand(rootNode);
            var childNode = model.FindNode(childB) ?? throw new InvalidOperationException("Child node missing.");
            model.Expand(childNode);

            var state = grid.CaptureHierarchicalState();
            Assert.NotNull(state);
            Assert.Equal(ExpandedStateKeyMode.Path, state.KeyMode);

            model.CollapseAll();
            Assert.False((model.Root ?? rootNode).IsExpanded);

            grid.RestoreHierarchicalState(state);
            Dispatcher.UIThread.RunJobs();

            Assert.True((model.Root ?? rootNode).IsExpanded);
            var restoredChild = model.FindNode(childB) ?? throw new InvalidOperationException("Restored child missing.");
            Assert.True(restoredChild.IsExpanded);
        }
        finally
        {
            root.Close();
        }
    }

    [AvaloniaFact]
    public void RestoreHierarchicalState_InfersPathModeForLegacyState()
    {
        var rootItem = new TreeItem(0, "Root");
        var childA = new TreeItem(1, "Child A");
        var childB = new TreeItem(2, "Child B");
        rootItem.Children.Add(childA);
        rootItem.Children.Add(childB);

        var model = new HierarchicalModel<TreeItem>(new HierarchicalOptions<TreeItem>
        {
            ChildrenSelector = item => item.Children,
            ExpandedStateKeyMode = ExpandedStateKeyMode.Path,
        });
        model.SetRoot(rootItem);

        var grid = new DataGrid
        {
            HierarchicalModel = model,
            HierarchicalRowsEnabled = true,
            AutoGenerateColumns = false,
            ItemsSource = model.Flattened,
        };

        grid.ColumnsInternal.Add(new DataGridHierarchicalColumn
        {
            Header = "Name",
            Binding = new Binding("Item.Name"),
        });

        var root = new Window
        {
            Width = 500,
            Height = 300,
            Content = grid,
        };

        root.SetThemeStyles();
        root.Show();
        grid.UpdateLayout();
        Dispatcher.UIThread.RunJobs();

        try
        {
            var rootNode = model.Root ?? throw new InvalidOperationException("Root node missing.");
            model.Expand(rootNode);
            var childNode = model.FindNode(childB) ?? throw new InvalidOperationException("Child node missing.");
            model.Expand(childNode);

            var state = grid.CaptureHierarchicalState();
            Assert.NotNull(state);

            state.KeyMode = ExpandedStateKeyMode.Item;

            model.CollapseAll();
            Assert.False((model.Root ?? rootNode).IsExpanded);

            grid.RestoreHierarchicalState(state);
            Dispatcher.UIThread.RunJobs();

            Assert.True((model.Root ?? rootNode).IsExpanded);
            var restoredChild = model.FindNode(childB) ?? throw new InvalidOperationException("Restored child missing.");
            Assert.True(restoredChild.IsExpanded);
        }
        finally
        {
            root.Close();
        }
    }

    [AvaloniaFact]
    public void RestoreHierarchicalState_UsesStoredKeyModeWhenModelChanges()
    {
        var rootItem = new TreeItem(0, "Root");
        var childA = new TreeItem(1, "Child A");
        var childB = new TreeItem(2, "Child B");
        rootItem.Children.Add(childA);
        rootItem.Children.Add(childB);

        var modelPath = new HierarchicalModel<TreeItem>(new HierarchicalOptions<TreeItem>
        {
            ChildrenSelector = item => item.Children,
            ExpandedStateKeyMode = ExpandedStateKeyMode.Path,
        });
        modelPath.SetRoot(rootItem);

        var grid = new DataGrid
        {
            HierarchicalModel = modelPath,
            HierarchicalRowsEnabled = true,
            AutoGenerateColumns = false,
            ItemsSource = modelPath.Flattened,
        };

        grid.ColumnsInternal.Add(new DataGridHierarchicalColumn
        {
            Header = "Name",
            Binding = new Binding("Item.Name"),
        });

        var root = new Window
        {
            Width = 500,
            Height = 300,
            Content = grid,
        };

        root.SetThemeStyles();
        root.Show();
        grid.UpdateLayout();
        Dispatcher.UIThread.RunJobs();

        try
        {
            var rootNode = modelPath.Root ?? throw new InvalidOperationException("Root node missing.");
            modelPath.Expand(rootNode);
            var childNode = modelPath.FindNode(childB) ?? throw new InvalidOperationException("Child node missing.");
            modelPath.Expand(childNode);

            var state = grid.CaptureHierarchicalState();
            Assert.NotNull(state);
            Assert.Equal(ExpandedStateKeyMode.Path, state.KeyMode);

            var modelItem = new HierarchicalModel<TreeItem>(new HierarchicalOptions<TreeItem>
            {
                ChildrenSelector = item => item.Children,
                ExpandedStateKeyMode = ExpandedStateKeyMode.Item,
            });
            modelItem.SetRoot(rootItem);

            grid.HierarchicalModel = modelItem;
            grid.ItemsSource = modelItem.Flattened;
            grid.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            modelItem.CollapseAll();

            grid.RestoreHierarchicalState(state);
            Dispatcher.UIThread.RunJobs();

            Assert.True((modelItem.Root ?? rootNode).IsExpanded);
            var restoredChild = modelItem.FindNode(childB) ?? throw new InvalidOperationException("Restored child missing.");
            Assert.True(restoredChild.IsExpanded);
        }
        finally
        {
            root.Close();
        }
    }
}
