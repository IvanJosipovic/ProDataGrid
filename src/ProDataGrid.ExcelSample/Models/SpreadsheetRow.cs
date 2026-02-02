using System;
using ProDataGrid.ExcelSample.Helpers;
using ReactiveUI;

namespace ProDataGrid.ExcelSample.Models;

public sealed class SpreadsheetRow : ReactiveObject
{
    private readonly object?[] _cells;

    public SpreadsheetRow(int columnCount, int rowIndex)
    {
        if (columnCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(columnCount));
        }

        _cells = new object?[columnCount];
        RowIndex = rowIndex;
    }

    public int RowIndex { get; }

    public int ColumnCount => _cells.Length;

    public object? GetCell(int columnIndex)
    {
        if ((uint)columnIndex >= (uint)_cells.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(columnIndex));
        }

        return _cells[columnIndex];
    }

    public T? GetCell<T>(int columnIndex)
    {
        if ((uint)columnIndex >= (uint)_cells.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(columnIndex));
        }

        var value = _cells[columnIndex];
        if (value is T typed)
        {
            return typed;
        }

        if (value is null)
        {
            return default;
        }

        return (T)Convert.ChangeType(value, typeof(T));
    }

    public void SetCell<T>(int columnIndex, T value)
    {
        if ((uint)columnIndex >= (uint)_cells.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(columnIndex));
        }

        if (Equals(_cells[columnIndex], value))
        {
            return;
        }

        _cells[columnIndex] = value;
        this.RaisePropertyChanged(ExcelColumnName.FromIndex(columnIndex));
    }
}
