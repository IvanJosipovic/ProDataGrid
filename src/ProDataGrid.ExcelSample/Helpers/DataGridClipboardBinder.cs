using System;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.DataGridClipboard;
using Avalonia.Input;
using Avalonia.Interactivity;
using ProDataGrid.ExcelSample.Models;

namespace ProDataGrid.ExcelSample.Helpers;

public sealed class DataGridClipboardBinder
{
    public static readonly AttachedProperty<SpreadsheetClipboardState?> ClipboardStateProperty =
        AvaloniaProperty.RegisterAttached<DataGridClipboardBinder, DataGrid, SpreadsheetClipboardState?>(
            "ClipboardState");

    private static readonly ConditionalWeakTable<DataGrid, ClipboardSubscription> Subscriptions = new();

    static DataGridClipboardBinder()
    {
        ClipboardStateProperty.Changed.AddClassHandler<DataGrid>(OnClipboardStateChanged);
    }

    public static void SetClipboardState(AvaloniaObject element, SpreadsheetClipboardState? value)
    {
        element.SetValue(ClipboardStateProperty, value);
    }

    public static SpreadsheetClipboardState? GetClipboardState(AvaloniaObject element)
    {
        return element.GetValue(ClipboardStateProperty);
    }

    private static void OnClipboardStateChanged(DataGrid grid, AvaloniaPropertyChangedEventArgs args)
    {
        var subscription = Subscriptions.GetOrCreateValue(grid);
        subscription.Detach();
        subscription.Attach(grid, args.NewValue as SpreadsheetClipboardState);
    }

    private sealed class ClipboardSubscription
    {
        private DataGrid? _grid;
        private SpreadsheetClipboardState? _state;
        private IDataGridClipboardImportModel? _previousModel;

        public void Attach(DataGrid grid, SpreadsheetClipboardState? state)
        {
            _grid = grid;
            _state = state;
            if (_grid == null || _state == null)
            {
                return;
            }

            _previousModel = _grid.ClipboardImportModel;
            _grid.ClipboardImportModel = new ExcelClipboardImportModel(_state);

            _grid.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        }

        public void Detach()
        {
            if (_grid != null)
            {
                _grid.RemoveHandler(InputElement.KeyDownEvent, OnKeyDown);
                if (_previousModel != null)
                {
                    _grid.ClipboardImportModel = _previousModel;
                }
            }

            _grid = null;
            _state = null;
            _previousModel = null;
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (_grid == null || _state == null)
            {
                return;
            }

            if (e.Key == Key.Escape)
            {
                _state.Clear();
                return;
            }

            if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                return;
            }

            if (e.Key == Key.C || e.Key == Key.X)
            {
                var range = GetSelectedRange(_grid);
                _state.SetCopiedRange(range);
            }
        }
    }

    private static SpreadsheetCellRange? GetSelectedRange(DataGrid grid)
    {
        var selectedCells = grid.SelectedCells;
        if (selectedCells == null || selectedCells.Count == 0)
        {
            var current = grid.CurrentCell;
            if (!current.IsValid)
            {
                return null;
            }

            var cell = new SpreadsheetCellReference(current.RowIndex, current.ColumnIndex);
            return new SpreadsheetCellRange(cell, cell);
        }

        var minRow = int.MaxValue;
        var maxRow = int.MinValue;
        var minColumn = int.MaxValue;
        var maxColumn = int.MinValue;

        for (var i = 0; i < selectedCells.Count; i++)
        {
            var cell = selectedCells[i];
            if (!cell.IsValid)
            {
                continue;
            }

            minRow = Math.Min(minRow, cell.RowIndex);
            maxRow = Math.Max(maxRow, cell.RowIndex);
            minColumn = Math.Min(minColumn, cell.ColumnIndex);
            maxColumn = Math.Max(maxColumn, cell.ColumnIndex);
        }

        if (minRow == int.MaxValue)
        {
            return null;
        }

        return new SpreadsheetCellRange(
            new SpreadsheetCellReference(minRow, minColumn),
            new SpreadsheetCellReference(maxRow, maxColumn));
    }
}
