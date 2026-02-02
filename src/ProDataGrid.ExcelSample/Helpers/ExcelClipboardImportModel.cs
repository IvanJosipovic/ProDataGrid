using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Controls.DataGridClipboard;
using Avalonia.Controls.DataGridFormulas;
using ProDataGrid.ExcelSample.Models;

namespace ProDataGrid.ExcelSample.Helpers;

/// <summary>
/// Clipboard import model that supports structured header paste and range repetition.
/// </summary>
public sealed class ExcelClipboardImportModel : DataGridClipboardImportModel
{
    private readonly SpreadsheetClipboardState? _clipboardState;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExcelClipboardImportModel"/> class.
    /// </summary>
    /// <param name="clipboardState">Optional clipboard state to update during paste.</param>
    public ExcelClipboardImportModel(SpreadsheetClipboardState? clipboardState = null)
    {
        _clipboardState = clipboardState;
    }

    /// <inheritdoc />
    public override bool Paste(DataGridClipboardImportContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (context.IsReadOnly || context.RowCount == 0 || context.ColumnCount == 0)
        {
            return false;
        }

        if (!context.Grid.CommitEdit(DataGridEditingUnit.Row, exitEditingMode: true))
        {
            return false;
        }

        var rows = ParseClipboardText(context.Text);
        if (rows.Count == 0)
        {
            return false;
        }

        var maxColumns = GetMaxColumnCount(rows);
        _clipboardState?.SetClipboardSize(rows.Count, maxColumns);

        var selectionRange = GetSelectionRange(context);
        if (TryPasteStructured(context, rows, selectionRange))
        {
            return true;
        }

        if (TryPasteSingleValueToSelection(context, rows))
        {
            return true;
        }

        if (selectionRange.HasValue && ShouldRepeat(selectionRange.Value, rows, maxColumns))
        {
            return TryPasteRepeating(context, rows, selectionRange.Value, maxColumns);
        }

        if (!TryGetPasteAnchor(context, out var startRow, out var startColumn))
        {
            return false;
        }

        return TryPasteTable(context, rows, startRow, startColumn);
    }

    /// <inheritdoc />
    protected override bool TryPasteSingleValueToSelection(
        DataGridClipboardImportContext context,
        IReadOnlyList<List<string>> rows)
    {
        if (rows.Count != 1 || rows[0].Count != 1)
        {
            return false;
        }

        var selectedCells = context.SelectedCells;
        if (selectedCells.Count <= 1)
        {
            return false;
        }

        var value = rows[0][0];
        var applied = false;
        var rowGroups = new Dictionary<int, List<DataGridCellInfo>>();

        for (var i = 0; i < selectedCells.Count; i++)
        {
            var cell = selectedCells[i];
            if (!cell.IsValid)
            {
                continue;
            }

            if (!rowGroups.TryGetValue(cell.RowIndex, out var group))
            {
                group = new List<DataGridCellInfo>();
                rowGroups[cell.RowIndex] = group;
            }

            group.Add(cell);
        }

        foreach (var entry in rowGroups)
        {
            using var editScope = context.BeginRowEdit(entry.Key, out var item);
            if (item == null)
            {
                continue;
            }

            var cells = entry.Value;
            for (var i = 0; i < cells.Count; i++)
            {
                var cell = cells[i];
                if (TrySetCellText(context, item, cell.ColumnIndex, value))
                {
                    applied = true;
                }
            }
        }

        return applied;
    }

    /// <inheritdoc />
    protected override bool TryPasteTable(
        DataGridClipboardImportContext context,
        IReadOnlyList<List<string>> rows,
        int startRow,
        int startColumn)
    {
        var applied = false;
        for (var rowOffset = 0; rowOffset < rows.Count; rowOffset++)
        {
            var rowIndex = startRow + rowOffset;
            if (rowIndex < 0 || rowIndex >= context.RowCount)
            {
                break;
            }

            using var editScope = context.BeginRowEdit(rowIndex, out var item);
            if (item == null)
            {
                continue;
            }

            var rowValues = rows[rowOffset];
            for (var colOffset = 0; colOffset < rowValues.Count; colOffset++)
            {
                var columnIndex = startColumn + colOffset;
                if (columnIndex < 0 || columnIndex >= context.ColumnCount)
                {
                    break;
                }

                if (TrySetCellText(context, item, columnIndex, rowValues[colOffset]))
                {
                    applied = true;
                }
            }
        }

        return applied;
    }

    private static SpreadsheetCellRange? GetSelectionRange(DataGridClipboardImportContext context)
    {
        var selectedCells = context.SelectedCells;
        if (selectedCells.Count == 0)
        {
            var row = context.CurrentRowIndex;
            var column = context.CurrentColumnIndex;
            if (row < 0 || column < 0)
            {
                return null;
            }

            var cell = new SpreadsheetCellReference(row, column);
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

    private static int GetMaxColumnCount(IReadOnlyList<List<string>> rows)
    {
        var max = 0;
        for (var i = 0; i < rows.Count; i++)
        {
            var count = rows[i].Count;
            if (count > max)
            {
                max = count;
            }
        }

        return max;
    }

    private bool TryPasteStructured(
        DataGridClipboardImportContext context,
        IReadOnlyList<List<string>> rows,
        SpreadsheetCellRange? selectionRange)
    {
        if (rows.Count < 2)
        {
            return false;
        }

        var headerRow = rows[0];
        if (headerRow.Count == 0)
        {
            return false;
        }

        var columnMap = new int[headerRow.Count];
        var lookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < context.ColumnCount; i++)
        {
            if (!context.TryGetColumn(i, out var column))
            {
                continue;
            }

            var headerText = column?.Header?.ToString();
            if (string.IsNullOrWhiteSpace(headerText))
            {
                continue;
            }

            if (!lookup.ContainsKey(headerText))
            {
                lookup[headerText] = i;
            }
        }

        var matchCount = 0;
        var seenHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var hasDuplicate = false;

        for (var i = 0; i < headerRow.Count; i++)
        {
            var header = headerRow[i]?.Trim();
            if (string.IsNullOrEmpty(header))
            {
                columnMap[i] = -1;
                continue;
            }

            if (!seenHeaders.Add(header))
            {
                hasDuplicate = true;
            }

            if (lookup.TryGetValue(header, out var columnIndex))
            {
                columnMap[i] = columnIndex;
                matchCount++;
            }
            else
            {
                columnMap[i] = -1;
            }
        }

        if (hasDuplicate)
        {
            return false;
        }

        var requiredMatches = Math.Max(2, (headerRow.Count + 1) / 2);
        if (matchCount < requiredMatches)
        {
            return false;
        }

        var startRow = selectionRange?.Start.RowIndex ?? context.CurrentRowIndex;
        if (startRow < 0)
        {
            return false;
        }

        _clipboardState?.SetClipboardSize(rows.Count - 1, headerRow.Count);

        var applied = false;
        for (var rowOffset = 1; rowOffset < rows.Count; rowOffset++)
        {
            var rowIndex = startRow + rowOffset - 1;
            if (rowIndex < 0 || rowIndex >= context.RowCount)
            {
                break;
            }

            using var editScope = context.BeginRowEdit(rowIndex, out var item);
            if (item == null)
            {
                continue;
            }

            var rowValues = rows[rowOffset];
            for (var colOffset = 0; colOffset < columnMap.Length; colOffset++)
            {
                var columnIndex = columnMap[colOffset];
                if (columnIndex < 0 || columnIndex >= context.ColumnCount)
                {
                    continue;
                }

                var value = colOffset < rowValues.Count ? rowValues[colOffset] : string.Empty;
                if (TrySetCellText(context, item, columnIndex, value))
                {
                    applied = true;
                }
            }
        }

        return applied;
    }

    private static bool ShouldRepeat(
        SpreadsheetCellRange selectionRange,
        IReadOnlyList<List<string>> rows,
        int columnCount)
    {
        if (rows.Count == 0 || columnCount == 0)
        {
            return false;
        }

        if (selectionRange.RowCount <= 1 && selectionRange.ColumnCount <= 1)
        {
            return false;
        }

        if (selectionRange.RowCount == rows.Count && selectionRange.ColumnCount == columnCount)
        {
            return false;
        }

        return selectionRange.RowCount % rows.Count == 0 && selectionRange.ColumnCount % columnCount == 0;
    }

    private bool TryPasteRepeating(
        DataGridClipboardImportContext context,
        IReadOnlyList<List<string>> rows,
        SpreadsheetCellRange selectionRange,
        int columnCount)
    {
        var applied = false;
        var maxRow = Math.Min(selectionRange.End.RowIndex, context.RowCount - 1);
        var maxColumn = Math.Min(selectionRange.End.ColumnIndex, context.ColumnCount - 1);

        for (var rowIndex = selectionRange.Start.RowIndex; rowIndex <= maxRow; rowIndex++)
        {
            using var editScope = context.BeginRowEdit(rowIndex, out var item);
            if (item == null)
            {
                continue;
            }

            var sourceRow = rows[(rowIndex - selectionRange.Start.RowIndex) % rows.Count];
            for (var columnIndex = selectionRange.Start.ColumnIndex; columnIndex <= maxColumn; columnIndex++)
            {
                var sourceColumn = (columnIndex - selectionRange.Start.ColumnIndex) % columnCount;
                var value = sourceColumn < sourceRow.Count ? sourceRow[sourceColumn] : string.Empty;
                if (TrySetCellText(context, item, columnIndex, value))
                {
                    applied = true;
                }
            }
        }

        return applied;
    }

    private bool TrySetCellText(DataGridClipboardImportContext context, object item, int columnIndex, string text)
    {
        if (!context.TryGetColumn(columnIndex, out var column))
        {
            return false;
        }

        if (context.Grid.FormulaModel is IDataGridFormulaModel formulaModel &&
            TryResolveFormulaDefinition(context.Grid, column, columnIndex, out var formulaDefinition) &&
            formulaDefinition?.AllowCellFormulas == true)
        {
            return formulaModel.TrySetCellFormula(item, formulaDefinition, text, out _);
        }

        var accessor = DataGridColumnMetadata.GetValueAccessor(column);
        if (accessor != null && accessor.CanWrite)
        {
            if (!TryConvertText(text, accessor.ValueType, out var converted))
            {
                return false;
            }

            accessor.SetValue(item, converted!);
            return true;
        }

        return context.TrySetCellText(item, columnIndex, text);
    }

    private static bool TryResolveFormulaDefinition(
        DataGrid grid,
        DataGridColumn column,
        int columnIndex,
        out DataGridFormulaColumnDefinition? definition)
    {
        definition = null;

        if (grid.ColumnDefinitionsSource is not IList<DataGridColumnDefinition> definitions)
        {
            return false;
        }

        if ((uint)columnIndex < (uint)definitions.Count &&
            definitions[columnIndex] is DataGridFormulaColumnDefinition indexedDefinition)
        {
            definition = indexedDefinition;
            return true;
        }

        var headerText = column.Header?.ToString();
        for (var i = 0; i < definitions.Count; i++)
        {
            if (definitions[i] is not DataGridFormulaColumnDefinition formulaDefinition)
            {
                continue;
            }

            if (formulaDefinition.ColumnKey is string columnKey &&
                !string.IsNullOrWhiteSpace(columnKey) &&
                string.Equals(columnKey, headerText, StringComparison.Ordinal))
            {
                definition = formulaDefinition;
                return true;
            }

            if (formulaDefinition.Header is string header &&
                !string.IsNullOrWhiteSpace(header) &&
                string.Equals(header, headerText, StringComparison.Ordinal))
            {
                definition = formulaDefinition;
                return true;
            }
        }

        return false;
    }

    private static bool TryConvertText(string text, Type valueType, out object? converted)
    {
        converted = null;
        var targetType = Nullable.GetUnderlyingType(valueType) ?? valueType;

        if (string.IsNullOrWhiteSpace(text))
        {
            if (!targetType.IsValueType || Nullable.GetUnderlyingType(valueType) != null)
            {
                converted = null;
                return true;
            }

            converted = Activator.CreateInstance(targetType);
            return true;
        }

        if (targetType == typeof(string) || targetType == typeof(object))
        {
            converted = text;
            return true;
        }

        if (targetType == typeof(double))
        {
            if (double.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out var value))
            {
                converted = value;
                return true;
            }
        }
        else if (targetType == typeof(float))
        {
            if (float.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out var value))
            {
                converted = value;
                return true;
            }
        }
        else if (targetType == typeof(decimal))
        {
            if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out var value))
            {
                converted = value;
                return true;
            }
        }
        else if (targetType == typeof(int))
        {
            if (int.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out var value))
            {
                converted = value;
                return true;
            }
        }
        else if (targetType == typeof(long))
        {
            if (long.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out var value))
            {
                converted = value;
                return true;
            }
        }
        else if (targetType == typeof(short))
        {
            if (short.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out var value))
            {
                converted = value;
                return true;
            }
        }
        else if (targetType == typeof(bool))
        {
            if (bool.TryParse(text, out var value))
            {
                converted = value;
                return true;
            }

            if (text == "1")
            {
                converted = true;
                return true;
            }

            if (text == "0")
            {
                converted = false;
                return true;
            }
        }
        else if (targetType == typeof(DateTime))
        {
            if (DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out var value))
            {
                converted = value;
                return true;
            }
        }

        return false;
    }
}
