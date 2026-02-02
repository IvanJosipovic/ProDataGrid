using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.VisualTree;
using ProDataGrid.ExcelSample.Helpers;
using Xunit;

namespace ProDataGrid.ExcelSample.Tests;

public sealed class DataGridRowDragHeaderOnlyBinderTests
{
    [AvaloniaFact]
    public void RowDragStarting_IsCanceled_WhenPointerNotOnHeader()
    {
        var items = new ObservableCollection<TextRow>
        {
            new TextRow { A = "A1" }
        };

        var (window, grid) = CreateGrid(items);
        DataGridRowDragHeaderOnlyBinder.SetIsEnabled(grid, true);
        try
        {
            var cell = FindVisual<DataGridCell>(grid);
            Assert.NotNull(cell);

            RaisePointerPressed(cell!, grid);

            var args = new DataGridRowDragStartingEventArgs(
                Array.Empty<object>(),
                Array.Empty<int>(),
                new DataTransfer(),
                DragDropEffects.Move,
                DataGrid.RowDragStartingEvent,
                grid);

            grid.RaiseEvent(args);

            Assert.True(args.Cancel);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void RowDragStarting_Allows_WhenPointerOnHeader()
    {
        var items = new ObservableCollection<TextRow>
        {
            new TextRow { A = "A1" }
        };

        var (window, grid) = CreateGrid(items);
        DataGridRowDragHeaderOnlyBinder.SetIsEnabled(grid, true);
        try
        {
            var header = FindVisual<DataGridRowHeader>(grid);
            Assert.NotNull(header);

            RaisePointerPressed(header!, grid);

            var args = new DataGridRowDragStartingEventArgs(
                Array.Empty<object>(),
                Array.Empty<int>(),
                new DataTransfer(),
                DragDropEffects.Move,
                DataGrid.RowDragStartingEvent,
                grid);

            grid.RaiseEvent(args);

            Assert.False(args.Cancel);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void RowDragStarting_Allows_WhenPointerOnHeader_WithTouch()
    {
        var items = new ObservableCollection<TextRow>
        {
            new TextRow { A = "A1" }
        };

        var (window, grid) = CreateGrid(items);
        DataGridRowDragHeaderOnlyBinder.SetIsEnabled(grid, true);
        try
        {
            var header = FindVisual<DataGridRowHeader>(grid);
            Assert.NotNull(header);

            RaisePointerPressed(header!, grid, PointerType.Touch, leftPressed: false);

            var args = new DataGridRowDragStartingEventArgs(
                Array.Empty<object>(),
                Array.Empty<int>(),
                new DataTransfer(),
                DragDropEffects.Move,
                DataGrid.RowDragStartingEvent,
                grid);

            grid.RaiseEvent(args);

            Assert.False(args.Cancel);
        }
        finally
        {
            window.Close();
        }
    }

    private static (Window Window, DataGrid Grid) CreateGrid(ObservableCollection<TextRow> items)
    {
        var window = CreateWindow();

        var grid = new DataGrid
        {
            ItemsSource = items,
            AutoGenerateColumns = false,
            CanUserAddRows = false,
            CanUserDeleteRows = false,
            ShowRowNumbers = true,
            RowHeaderWidth = 50
        };

        grid.Columns.Add(new DataGridTextColumn
        {
            Header = "A",
            Binding = new Avalonia.Data.Binding(nameof(TextRow.A))
        });

        window.Content = grid;
        window.Show();
        grid.UpdateLayout();

        return (window, grid);
    }

    private static Window CreateWindow()
    {
        var window = new Window
        {
            Width = 640,
            Height = 480
        };

        window.Styles.Add(new FluentTheme());
        window.Styles.Add(new StyleInclude(new Uri("avares://Avalonia.Controls.DataGrid/Themes/"))
        {
            Source = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Fluent.v2.xaml")
        });

        return window;
    }

    private static T? FindVisual<T>(Visual root) where T : Visual
    {
        return root.GetVisualDescendants()
            .OfType<T>()
            .FirstOrDefault();
    }

    private static void RaisePointerPressed(Control target, Visual root, PointerType pointerType = PointerType.Mouse, bool leftPressed = true)
    {
        var pointer = new Pointer(Pointer.GetNextFreeId(), pointerType, true);
        var modifiers = leftPressed ? RawInputModifiers.LeftMouseButton : RawInputModifiers.None;
        var updateKind = leftPressed ? PointerUpdateKind.LeftButtonPressed : PointerUpdateKind.Other;
        var properties = new PointerPointProperties(modifiers, updateKind);
        var args = new PointerPressedEventArgs(target, pointer, root, new Point(1, 1), 0, properties, KeyModifiers.None);
        target.RaiseEvent(args);
    }

    private sealed class TextRow
    {
        public string? A { get; set; }
    }
}
