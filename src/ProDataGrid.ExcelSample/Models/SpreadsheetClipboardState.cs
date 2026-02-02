using System;
using ReactiveUI;

namespace ProDataGrid.ExcelSample.Models;

/// <summary>
/// Tracks clipboard metadata for spreadsheet range copy and paste previews.
/// </summary>
public sealed class SpreadsheetClipboardState : ReactiveObject
{
    private SpreadsheetCellRange? _copiedRange;
    private int _clipboardRowCount;
    private int _clipboardColumnCount;

    /// <summary>
    /// Gets or sets the last copied range, if available.
    /// </summary>
    public SpreadsheetCellRange? CopiedRange
    {
        get => _copiedRange;
        set => this.RaiseAndSetIfChanged(ref _copiedRange, value);
    }

    /// <summary>
    /// Gets the number of rows represented by the clipboard data.
    /// </summary>
    public int ClipboardRowCount
    {
        get => _clipboardRowCount;
        private set => this.RaiseAndSetIfChanged(ref _clipboardRowCount, value);
    }

    /// <summary>
    /// Gets the number of columns represented by the clipboard data.
    /// </summary>
    public int ClipboardColumnCount
    {
        get => _clipboardColumnCount;
        private set => this.RaiseAndSetIfChanged(ref _clipboardColumnCount, value);
    }

    /// <summary>
    /// Gets a value indicating whether the clipboard currently represents a range.
    /// </summary>
    public bool HasClipboard => ClipboardRowCount > 0 && ClipboardColumnCount > 0;

    /// <summary>
    /// Updates the clipboard state based on a copied range.
    /// </summary>
    /// <param name="range">The copied range, or null to clear the clipboard state.</param>
    public void SetCopiedRange(SpreadsheetCellRange? range)
    {
        CopiedRange = range;
        if (!range.HasValue)
        {
            SetClipboardSize(0, 0);
            return;
        }

        SetClipboardSize(range.Value.RowCount, range.Value.ColumnCount);
    }

    /// <summary>
    /// Updates the clipboard size without changing the stored range.
    /// </summary>
    /// <param name="rowCount">The row count.</param>
    /// <param name="columnCount">The column count.</param>
    public void SetClipboardSize(int rowCount, int columnCount)
    {
        rowCount = Math.Max(0, rowCount);
        columnCount = Math.Max(0, columnCount);

        ClipboardRowCount = rowCount;
        ClipboardColumnCount = columnCount;
        this.RaisePropertyChanged(nameof(HasClipboard));
    }

    /// <summary>
    /// Clears all clipboard metadata.
    /// </summary>
    public void Clear()
    {
        CopiedRange = null;
        SetClipboardSize(0, 0);
    }
}
