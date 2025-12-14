// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.DataGridDragDrop;
using Avalonia.Controls.DataGridHierarchical;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.VisualTree;
using Xunit;
using AvaloniaDragDrop = Avalonia.Input.DragDrop;

namespace Avalonia.Controls.DataGridTests.DragDrop;

    public class DataGridRowDragDropControllerTests
    {
    [AvaloniaFact]
    public void Row_DragHandle_Allows_Header_Start()
    {
        var items = new ObservableCollection<RowItem>
        {
            new("A"),
            new("B")
        };
        var (grid, window) = CreateGrid(items);
        grid.CanUserReorderRows = true;
        grid.RowDragHandle = DataGridRowDragHandle.Row;
        grid.UpdateLayout();

        var handler = new DataGridRowReorderHandler();
        using var controller = new DataGridRowDragDropController(grid, handler, new DataGridRowDragDropOptions());

        var header = grid.GetVisualDescendants().OfType<DataGridRowHeader>().First();
        var point = header.TranslatePoint(new Point(1, 1), grid) ?? new Point(1, 1);

        var dragInfo = new DataGridRowDragInfo(grid, new List<object> { items[0] }, new List<int> { 0 }, fromSelection: false);
        var data = new DataObject();
        var dragEvent = new DragEventArgs(
            AvaloniaDragDrop.DragOverEvent,
            data,
            grid,
            point,
            KeyModifiers.None)
        {
            RoutedEvent = AvaloniaDragDrop.DragOverEvent,
            Source = grid
        };

        var dropArgs = InvokeCreateDropArgs(controller, dragInfo, dragEvent, DragDropEffects.Move);
        Assert.NotNull(dropArgs);
        window.Close();
    }

    [AvaloniaFact]
    public void Drop_Reorders_Items()
    {
        var items = new ObservableCollection<RowItem>
        {
            new("A"),
            new("B"),
            new("C")
        };
        var (grid, window) = CreateGrid(items);
        grid.CanUserReorderRows = true;
        grid.CanUserAddRows = false;

        var handler = new DataGridRowReorderHandler();
        using var controller = new DataGridRowDragDropController(grid, handler, new DataGridRowDragDropOptions());

        var dropArgs = CreateDropArgs(
            grid,
            items,
            new List<object> { items[0] },
            new List<int> { 0 },
            targetItem: items[2],
            position: DataGridRowDropPosition.After);

        Assert.True(handler.Execute(dropArgs));
        InvokeUpdateSelection(controller, dropArgs);

        Assert.Equal(new[] { "B", "C", "A" }, items.Select(x => x.Value));
        window.Close();
    }

    [AvaloniaFact]
    public void Drop_Multiple_Selected_Rows_Reorders_And_Keeps_Selection()
    {
        var items = new ObservableCollection<RowItem>
        {
            new("A"),
            new("B"),
            new("C"),
            new("D")
        };
        var (grid, window) = CreateGrid(items);
        grid.CanUserReorderRows = true;
        grid.CanUserAddRows = false;

        var b = items[1];
        var c = items[2];
        grid.SelectedItems.Add(b);
        grid.SelectedItems.Add(c);

        var handler = new DataGridRowReorderHandler();
        using var controller = new DataGridRowDragDropController(grid, handler, new DataGridRowDragDropOptions());

        var dropArgs = CreateDropArgs(
            grid,
            items,
            new List<object> { b, c },
            new List<int> { 1, 2 },
            targetItem: items[0],
            position: DataGridRowDropPosition.Before);

        Assert.True(handler.Execute(dropArgs));
        InvokeUpdateSelection(controller, dropArgs);

        Assert.Equal(new[] { "B", "C", "A", "D" }, items.Select(x => x.Value));
        Assert.Collection(grid.SelectedItems.Cast<object>(),
            i => Assert.Same(b, i),
            i => Assert.Same(c, i));
        window.Close();
    }

    [AvaloniaFact]
    public void Drop_Onto_Placeholder_Is_Ignored()
    {
        var items = new DataGridCollectionView(new ObservableCollection<PlaceholderItem>
        {
            new() { Value = "A" },
            new() { Value = "B" }
        });
        var (grid, window) = CreateGrid(items);
        grid.CanUserReorderRows = true;
        grid.CanUserAddRows = true;

        var handler = new DataGridRowReorderHandler();
        using var controller = new DataGridRowDragDropController(grid, handler, new DataGridRowDragDropOptions());

        var dropArgs = CreateDropArgs(
            grid,
            (IList)items,
            new List<object> { items[0] },
            new List<int> { 0 },
            targetItem: DataGridCollectionView.NewItemPlaceholder,
            position: DataGridRowDropPosition.Before);

        Assert.False(handler.Validate(dropArgs));
        Assert.False(handler.Execute(dropArgs));
        InvokeUpdateSelection(controller, dropArgs);

        Assert.Equal(new[] { "A", "B" }, items.Cast<PlaceholderItem>().Select(x => x.Value));
        window.Close();
    }

    [AvaloniaFact]
    public void DragOver_On_Dragged_Row_Produces_DropArgs()
    {
        var items = new ObservableCollection<RowItem>
        {
            new("A"),
            new("B"),
            new("C")
        };
        var (grid, window) = CreateGrid(items);
        grid.CanUserReorderRows = true;
        grid.UpdateLayout();

        var handler = new DataGridRowReorderHandler();
        using var controller = new DataGridRowDragDropController(grid, handler, new DataGridRowDragDropOptions());

        var firstRow = grid.GetVisualDescendants().OfType<DataGridRow>().First();
        var rowPoint = firstRow.TranslatePoint(new Point(1, 1), grid) ?? new Point(1, 1);

        var dragInfo = new DataGridRowDragInfo(grid, new List<object> { items[0] }, new List<int> { 0 }, fromSelection: false);
        var data = new DataObject();
        var dragEvent = new DragEventArgs(
            AvaloniaDragDrop.DragOverEvent,
            data,
            grid,
            rowPoint,
            KeyModifiers.None)
        {
            RoutedEvent = AvaloniaDragDrop.DragOverEvent,
            Source = grid
        };

        var dropArgs = InvokeCreateDropArgs(controller, dragInfo, dragEvent, DragDropEffects.Move);

        Assert.NotNull(dropArgs);
        Assert.Equal(0, dropArgs!.InsertIndex);
        Assert.Equal(firstRow, dropArgs.TargetRow);
        Assert.True(handler.Validate(dropArgs));
        window.Close();
    }

    [AvaloniaFact]
    public void DragOver_Above_First_Row_Produces_Before_Target()
    {
        var items = new ObservableCollection<RowItem>
        {
            new("A"),
            new("B"),
            new("C")
        };
        var (grid, window) = CreateGrid(items);
        grid.CanUserReorderRows = true;
        grid.UpdateLayout();

        var handler = new DataGridRowReorderHandler();
        using var controller = new DataGridRowDragDropController(grid, handler, new DataGridRowDragDropOptions());

        var firstRow = grid.GetVisualDescendants().OfType<DataGridRow>().First();
        var top = firstRow.TranslatePoint(new Point(0, 0), grid) ?? new Point(0, 0);
        var abovePoint = new Point(top.X + 1, Math.Max(0, top.Y - 2));

        var dragInfo = new DataGridRowDragInfo(grid, new List<object> { items[1] }, new List<int> { 1 }, fromSelection: false);
        var data = new DataObject();
        var dragEvent = new DragEventArgs(
            AvaloniaDragDrop.DragOverEvent,
            data,
            grid,
            abovePoint,
            KeyModifiers.None)
        {
            RoutedEvent = AvaloniaDragDrop.DragOverEvent,
            Source = grid
        };

        var dropArgs = InvokeCreateDropArgs(controller, dragInfo, dragEvent, DragDropEffects.Move);

        Assert.NotNull(dropArgs);
        Assert.Equal(DataGridRowDropPosition.Before, dropArgs!.Position);
        Assert.Equal(0, dropArgs.InsertIndex);
        Assert.Equal(firstRow, dropArgs.TargetRow);
        Assert.True(handler.Validate(dropArgs));
        window.Close();
    }

    [AvaloniaFact]
    public void RowHeader_Does_Not_Toggle_Hierarchical_Node_When_Reorder_Is_Enabled()
    {
        var (grid, window, model, root) = CreateHierarchicalGrid();
        grid.UpdateLayout();

        Assert.False(root.IsExpanded);

        var header = grid.GetVisualDescendants().OfType<DataGridRowHeader>().First();
        var pointer = new Avalonia.Input.Pointer(Avalonia.Input.Pointer.GetNextFreeId(), PointerType.Mouse, isPrimary: true);
        var properties = new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed);
        var args = new PointerPressedEventArgs(
            header,
            pointer,
            window,
            new Point(1, 1),
            0,
            properties,
            KeyModifiers.None);

        header.RaiseEvent(args);

        Assert.False(root.IsExpanded);
        window.Close();
    }

    [AvaloniaFact]
    public void Hierarchical_DragOver_Middle_Uses_Inside_Position()
    {
        var (grid, window, model, root) = CreateHierarchicalGrid();
        grid.CanUserAddRows = false;
        root.IsExpanded = true;
        grid.UpdateLayout();

        var handler = new DataGridHierarchicalRowReorderHandler();
        using var controller = new DataGridRowDragDropController(grid, handler, new DataGridRowDragDropOptions());

        var firstRow = grid.GetVisualDescendants().OfType<DataGridRow>().First();
        var point = firstRow.TranslatePoint(new Point(firstRow.Bounds.Width / 2, firstRow.Bounds.Height * 0.5), grid) ?? new Point(1, 1);

        var dragged = firstRow.DataContext!;
        var dragInfo = new DataGridRowDragInfo(grid, new List<object> { dragged }, new List<int> { firstRow.Index }, fromSelection: false);
        var data = new DataObject();
        var dragEvent = new DragEventArgs(
            AvaloniaDragDrop.DragOverEvent,
            data,
            grid,
            point,
            KeyModifiers.None)
        {
            RoutedEvent = AvaloniaDragDrop.DragOverEvent,
            Source = grid
        };

        var dropArgs = InvokeCreateDropArgs(controller, dragInfo, dragEvent, DragDropEffects.Move);

        Assert.NotNull(dropArgs);
        Assert.Equal(DataGridRowDropPosition.Inside, dropArgs!.Position);
        window.Close();
    }

    private static DataGridRowDropEventArgs CreateDropArgs(
        DataGrid grid,
        IList list,
        IReadOnlyList<object> items,
        IReadOnlyList<int> indices,
        object? targetItem,
        DataGridRowDropPosition position)
    {
        var data = new DataTransfer();
        var dragEvent = new DragEventArgs(
            AvaloniaDragDrop.DropEvent,
            data,
            grid,
            new Avalonia.Point(),
            KeyModifiers.None)
        {
            RoutedEvent = AvaloniaDragDrop.DropEvent,
            Source = grid
        };

        var targetIndex = targetItem != null ? list.IndexOf(targetItem) : list.Count;
        if (targetIndex < 0)
        {
            targetIndex = list.Count;
        }

        var insertIndex = position switch
        {
            DataGridRowDropPosition.After => Math.Clamp(targetIndex + 1, 0, list.Count),
            DataGridRowDropPosition.Inside => list.Count,
            _ => Math.Clamp(targetIndex, 0, list.Count)
        };

        return new DataGridRowDropEventArgs(
            grid,
            list,
            items,
            indices,
            targetItem,
            targetIndex,
            insertIndex,
            targetRow: null,
            position,
            isSameGrid: true,
            DragDropEffects.Move,
            dragEvent);
    }

    private static void InvokeUpdateSelection(DataGridRowDragDropController controller, DataGridRowDropEventArgs args)
    {
        var method = typeof(DataGridRowDragDropController).GetMethod("UpdateSelectionAfterDrop", BindingFlags.NonPublic | BindingFlags.Instance);
        method?.Invoke(controller, new object[] { args });
    }

    private static DataGridRowDropEventArgs? InvokeCreateDropArgs(
        DataGridRowDragDropController controller,
        DataGridRowDragInfo info,
        DragEventArgs e,
        DragDropEffects effects)
    {
        var method = typeof(DataGridRowDragDropController).GetMethod("CreateDropArgs", BindingFlags.NonPublic | BindingFlags.Instance);
        return method?.Invoke(controller, new object[] { info, e, effects }) as DataGridRowDropEventArgs;
    }

    private static (DataGrid Grid, Window Window) CreateGrid(IEnumerable items)
    {
        var grid = new DataGrid
        {
            ItemsSource = items,
            IsReadOnly = false,
            SelectionMode = DataGridSelectionMode.Extended,
            AutoGenerateColumns = false
        };

        grid.Styles.Add(new StyleInclude((Uri?)null)
        {
            Source = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Simple.xaml")
        });

        grid.ColumnsInternal.Add(new DataGridTextColumn
        {
            Header = "Value",
            Binding = new Binding("Value")
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
        grid.ApplyTemplate();
        window.Measure(new Size(window.Width, window.Height));
        window.Arrange(new Rect(0, 0, window.Width, window.Height));
        grid.UpdateLayout();
        return (grid, window);
    }

    private static (DataGrid Grid, Window Window, HierarchicalModel Model, HierarchicalNode Root) CreateHierarchicalGrid()
    {
        var rootItem = new TreeNode("Root", new ObservableCollection<TreeNode>
        {
            new("Child 1", new ObservableCollection<TreeNode>
            {
                new("Leaf")
            })
        });

        var options = new HierarchicalOptions<TreeNode>
        {
            ChildrenSelector = x => x.Children,
            AutoExpandRoot = false
        };

        var model = new HierarchicalModel<TreeNode>(options);
        model.SetRoot(rootItem);
        HierarchicalModel untyped = model;

        var grid = new DataGrid
        {
            HierarchicalModel = model,
            HierarchicalRowsEnabled = true,
            CanUserReorderRows = true,
            AutoGenerateColumns = false,
            RowHeaderWidth = 28
        };

        grid.Styles.Add(new StyleInclude((Uri?)null)
        {
            Source = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Simple.xaml")
        });

        grid.ColumnsInternal.Add(new DataGridTextColumn
        {
            Header = "Name",
            Binding = new Binding("Item.Name")
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
        grid.ApplyTemplate();
        window.Measure(new Size(window.Width, window.Height));
        window.Arrange(new Rect(0, 0, window.Width, window.Height));
        grid.UpdateLayout();

        return (grid, window, untyped, untyped.Root!);
    }

    private class PlaceholderItem
    {
        public string Value { get; set; } = string.Empty;

        public override string ToString() => Value;
    }

    private record RowItem(string Value)
    {
        public override string ToString() => Value;
    }

    private record TreeNode(string Name, ObservableCollection<TreeNode>? Children = null)
    {
        public ObservableCollection<TreeNode> Children { get; } = Children ?? new ObservableCollection<TreeNode>();

        public override string ToString() => Name;
    }
}
