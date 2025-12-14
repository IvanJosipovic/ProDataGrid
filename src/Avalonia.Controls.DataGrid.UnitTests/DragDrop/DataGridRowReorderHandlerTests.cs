// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.DataGridDragDrop;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Xunit;
using AvaloniaDragDrop = Avalonia.Input.DragDrop;

namespace Avalonia.Controls.DataGridTests.DragDrop;

public class DataGridRowReorderHandlerTests
{
    [AvaloniaFact]
    public void Execute_Moves_Single_Row_Down()
    {
        var items = new ObservableCollection<string> { "A", "B", "C" };
        var (grid, list) = CreateGrid(items);

        var args = CreateArgs(
            grid,
            list,
            new[] { items[0] },
            new[] { 0 },
            items[2],
            targetIndex: 2,
            position: DataGridRowDropPosition.After);

        var handler = new DataGridRowReorderHandler();

        Assert.True(handler.Validate(args));
        Assert.True(handler.Execute(args));
        Assert.Equal(new[] { "B", "C", "A" }, items);
    }

    [AvaloniaFact]
    public void Execute_Moves_Multiple_Rows_Preserving_Order()
    {
        var items = new ObservableCollection<string> { "A", "B", "C", "D", "E" };
        var (grid, list) = CreateGrid(items);

        var args = CreateArgs(
            grid,
            list,
            new object[] { items[1], items[3] },
            new[] { 1, 3 },
            items[2],
            targetIndex: 2,
            position: DataGridRowDropPosition.After);

        var handler = new DataGridRowReorderHandler();

        Assert.True(handler.Execute(args));
        Assert.Equal(new[] { "A", "C", "B", "D", "E" }, items);
    }

    [AvaloniaFact]
    public void Execute_Noops_When_Drop_Inside_Selection_Block()
    {
        var items = new ObservableCollection<string> { "A", "B", "C", "D" };
        var (grid, list) = CreateGrid(items);
        var args = CreateArgs(
            grid,
            list,
            new object[] { items[1], items[2] },
            new[] { 1, 2 },
            items[1],
            targetIndex: 1,
            position: DataGridRowDropPosition.Before);

        var handler = new DataGridRowReorderHandler();

        Assert.False(handler.Execute(args));
        Assert.Equal(new[] { "A", "B", "C", "D" }, items);
    }

    [AvaloniaFact]
    public void Validate_Fails_For_Sorted_View()
    {
        var items = new ObservableCollection<TestItem>
        {
            new("A", 1),
            new("B", 2)
        };
        var (grid, list) = CreateGrid(items);
        var view = (DataGridCollectionView)grid.ItemsSource!;
        view.SortDescriptions.Add(DataGridSortDescription.FromPath(nameof(TestItem.Value)));

        var args = CreateArgs(
            grid,
            list,
            new object[] { items[0] },
            new[] { 0 },
            items[1],
            targetIndex: 1,
            position: DataGridRowDropPosition.After);

        var handler = new DataGridRowReorderHandler();

        Assert.False(handler.Validate(args));
    }

    [AvaloniaFact]
    public void Validate_Fails_For_Readonly_List()
    {
        var source = new ObservableCollection<string> { "A", "B" };
        var ro = new ReadOnlyObservableCollection<string>(source);
        var (grid, list) = CreateGrid(ro);

        var args = CreateArgs(
            grid,
            list,
            new object[] { ro[0] },
            new[] { 0 },
            ro[1],
            targetIndex: 1,
            position: DataGridRowDropPosition.After);

        var handler = new DataGridRowReorderHandler();

        Assert.False(handler.Validate(args));
    }

    private static (DataGrid Grid, IList List) CreateGrid(IEnumerable items)
    {
        var view = new DataGridCollectionView(items);
        var grid = new DataGrid
        {
            ItemsSource = view,
            CanUserReorderRows = true,
            IsReadOnly = false
        };

        var root = new Window
        {
            Width = 300,
            Height = 300,
            Content = grid
        };

        root.Show();
        grid.UpdateLayout();

        return (grid, view);
    }

    private static DataGridRowDropEventArgs CreateArgs(
        DataGrid grid,
        IList list,
        IReadOnlyList<object> draggedItems,
        IReadOnlyList<int> sourceIndices,
        object? targetItem,
        int targetIndex,
        DataGridRowDropPosition position)
    {
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
            draggedItems,
            sourceIndices,
            targetItem,
            targetIndex,
            targetIndex,
            null,
            position,
            isSameGrid: true,
            requestedEffect: DragDropEffects.Move,
            dragEventArgs: dragEvent);
    }

    private record TestItem(string Name, int Value);
}
