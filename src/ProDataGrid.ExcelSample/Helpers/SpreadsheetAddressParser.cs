using System;
using ProDataGrid.ExcelSample.Models;

namespace ProDataGrid.ExcelSample.Helpers;

public static class SpreadsheetAddressParser
{
    public static bool TryParseCellReference(string? text, out SpreadsheetCellReference cell)
    {
        cell = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var span = text.AsSpan().Trim();
        if (span.Length == 0)
        {
            return false;
        }

        var index = 0;
        while (index < span.Length && (char.IsLetter(span[index]) || span[index] == '$'))
        {
            index++;
        }

        if (index == 0 || index == span.Length)
        {
            return false;
        }

        var columnSpan = span.Slice(0, index).ToString();
        var rowSpan = span.Slice(index).ToString().Replace("$", string.Empty, StringComparison.Ordinal);

        if (!ExcelColumnName.TryParseIndex(columnSpan, out var columnIndex))
        {
            return false;
        }

        if (!int.TryParse(rowSpan, out var rowIndex))
        {
            return false;
        }

        rowIndex -= 1;
        if (rowIndex < 0)
        {
            return false;
        }

        cell = new SpreadsheetCellReference(rowIndex, columnIndex);
        return true;
    }

    public static bool TryParseRange(string? text, out SpreadsheetCellRange range)
    {
        range = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var span = text.AsSpan().Trim();
        if (span.Length == 0)
        {
            return false;
        }

        var separatorIndex = span.IndexOf(':');
        if (separatorIndex < 0)
        {
            if (!TryParseCellReference(span.ToString(), out var single))
            {
                return false;
            }

            range = new SpreadsheetCellRange(single, single);
            return true;
        }

        var startText = span.Slice(0, separatorIndex).ToString();
        var endText = span.Slice(separatorIndex + 1).ToString();

        if (!TryParseCellReference(startText, out var start) || !TryParseCellReference(endText, out var end))
        {
            return false;
        }

        range = new SpreadsheetCellRange(start, end);
        return true;
    }
}
