using System;
using ProDataGrid.ExcelSample.Helpers;
using ReactiveUI;

namespace ProDataGrid.ExcelSample.Models;

public readonly struct SpreadsheetCellReference : IEquatable<SpreadsheetCellReference>
{
    public SpreadsheetCellReference(int rowIndex, int columnIndex)
    {
        if (rowIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rowIndex));
        }

        if (columnIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(columnIndex));
        }

        RowIndex = rowIndex;
        ColumnIndex = columnIndex;
    }

    public int RowIndex { get; }

    public int ColumnIndex { get; }

    public string ToA1()
    {
        return $"{ExcelColumnName.FromIndex(ColumnIndex)}{RowIndex + 1}";
    }

    public bool Equals(SpreadsheetCellReference other)
    {
        return RowIndex == other.RowIndex && ColumnIndex == other.ColumnIndex;
    }

    public override bool Equals(object? obj)
    {
        return obj is SpreadsheetCellReference other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (RowIndex * 397) ^ ColumnIndex;
        }
    }

    public static bool operator ==(SpreadsheetCellReference left, SpreadsheetCellReference right) => left.Equals(right);

    public static bool operator !=(SpreadsheetCellReference left, SpreadsheetCellReference right) => !left.Equals(right);
}

public readonly struct SpreadsheetCellRange : IEquatable<SpreadsheetCellRange>
{
    public SpreadsheetCellRange(SpreadsheetCellReference start, SpreadsheetCellReference end)
    {
        var startRow = Math.Min(start.RowIndex, end.RowIndex);
        var endRow = Math.Max(start.RowIndex, end.RowIndex);
        var startColumn = Math.Min(start.ColumnIndex, end.ColumnIndex);
        var endColumn = Math.Max(start.ColumnIndex, end.ColumnIndex);

        Start = new SpreadsheetCellReference(startRow, startColumn);
        End = new SpreadsheetCellReference(endRow, endColumn);
    }

    public SpreadsheetCellReference Start { get; }

    public SpreadsheetCellReference End { get; }

    public bool IsSingleCell => Start.RowIndex == End.RowIndex && Start.ColumnIndex == End.ColumnIndex;

    public int RowCount => End.RowIndex - Start.RowIndex + 1;

    public int ColumnCount => End.ColumnIndex - Start.ColumnIndex + 1;

    public string ToA1Range()
    {
        return IsSingleCell ? Start.ToA1() : $"{Start.ToA1()}:{End.ToA1()}";
    }

    public bool Equals(SpreadsheetCellRange other)
    {
        return Start.Equals(other.Start) && End.Equals(other.End);
    }

    public override bool Equals(object? obj)
    {
        return obj is SpreadsheetCellRange other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (Start.GetHashCode() * 397) ^ End.GetHashCode();
        }
    }

    public static bool operator ==(SpreadsheetCellRange left, SpreadsheetCellRange right) => left.Equals(right);

    public static bool operator !=(SpreadsheetCellRange left, SpreadsheetCellRange right) => !left.Equals(right);
}

public sealed class SpreadsheetSelectionState : ReactiveObject
{
    private SpreadsheetCellReference? _currentCell;
    private SpreadsheetCellRange? _selectedRange;

    public SpreadsheetCellReference? CurrentCell
    {
        get => _currentCell;
        set => this.RaiseAndSetIfChanged(ref _currentCell, value);
    }

    public SpreadsheetCellRange? SelectedRange
    {
        get => _selectedRange;
        set => this.RaiseAndSetIfChanged(ref _selectedRange, value);
    }
}
