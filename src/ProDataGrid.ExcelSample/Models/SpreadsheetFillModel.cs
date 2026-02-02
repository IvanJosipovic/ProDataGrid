using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Controls.DataGridFilling;

namespace ProDataGrid.ExcelSample.Models;

public sealed class SpreadsheetFillModel : DataGridFillModel
{
    public override void ApplyFill(DataGridFillContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (!CanApplyFill(context))
        {
            return;
        }

        var source = context.SourceRange;
        var target = context.TargetRange;

        if (source == target)
        {
            return;
        }

        var rowCount = source.RowCount;
        var colCount = source.ColumnCount;

        if (rowCount <= 0 || colCount <= 0)
        {
            return;
        }

        var isVerticalFill = target.StartColumn == source.StartColumn
            && target.EndColumn == source.EndColumn
            && (target.StartRow != source.StartRow || target.EndRow != source.EndRow);
        var isHorizontalFill = target.StartRow == source.StartRow
            && target.EndRow == source.EndRow
            && (target.StartColumn != source.StartColumn || target.EndColumn != source.EndColumn);

        Dictionary<int, FillSeries>? seriesByColumn = null;
        Dictionary<int, FillSeries>? seriesByRow = null;
        Dictionary<int, TextFillSeries>? textSeriesByColumn = null;
        Dictionary<int, TextFillSeries>? textSeriesByRow = null;

        if (isVerticalFill)
        {
            seriesByColumn = BuildFillSeriesByColumn(context, source);
            textSeriesByColumn = BuildTextSeriesByColumn(context, source);
        }
        else if (isHorizontalFill)
        {
            seriesByRow = BuildFillSeriesByRow(context, source);
            textSeriesByRow = BuildTextSeriesByRow(context, source);
        }

        ApplyFillCore(context, source, target, rowCount, colCount, isVerticalFill, isHorizontalFill, seriesByColumn, seriesByRow, textSeriesByColumn, textSeriesByRow);
    }

    private void ApplyFillCore(
        DataGridFillContext context,
        DataGridCellRange source,
        DataGridCellRange target,
        int rowCount,
        int colCount,
        bool isVerticalFill,
        bool isHorizontalFill,
        Dictionary<int, FillSeries>? seriesByColumn,
        Dictionary<int, FillSeries>? seriesByRow,
        Dictionary<int, TextFillSeries>? textSeriesByColumn,
        Dictionary<int, TextFillSeries>? textSeriesByRow)
    {
        var grid = context.Grid;

        for (var rowIndex = target.StartRow; rowIndex <= target.EndRow; rowIndex++)
        {
            using var editScope = context.BeginRowEdit(rowIndex, out var item);
            if (item == null)
            {
                continue;
            }

            for (var columnIndex = target.StartColumn; columnIndex <= target.EndColumn; columnIndex++)
            {
                if (columnIndex < 0 || columnIndex >= grid.Columns.Count)
                {
                    continue;
                }

                if (source.Contains(rowIndex, columnIndex))
                {
                    continue;
                }

                var hasText = false;
                var text = string.Empty;

                if (isVerticalFill && textSeriesByColumn != null && textSeriesByColumn.TryGetValue(columnIndex, out var columnTextSeries))
                {
                    hasText = TryGetTextSeriesFillText(columnTextSeries, rowIndex - source.StartRow, out text);
                }
                else if (isHorizontalFill && textSeriesByRow != null && textSeriesByRow.TryGetValue(rowIndex, out var rowTextSeries))
                {
                    hasText = TryGetTextSeriesFillText(rowTextSeries, columnIndex - source.StartColumn, out text);
                }

                if (!hasText)
                {
                    if (isVerticalFill && seriesByColumn != null && seriesByColumn.TryGetValue(columnIndex, out var columnSeries))
                    {
                        hasText = TryGetSeriesFillText(columnSeries, rowIndex - source.StartRow, out text);
                    }
                    else if (isHorizontalFill && seriesByRow != null && seriesByRow.TryGetValue(rowIndex, out var rowSeries))
                    {
                        hasText = TryGetSeriesFillText(rowSeries, columnIndex - source.StartColumn, out text);
                    }
                }

                if (!hasText)
                {
                    var sourceRow = source.StartRow + Mod(rowIndex - source.StartRow, rowCount);
                    var sourceColumn = source.StartColumn + Mod(columnIndex - source.StartColumn, colCount);
                    hasText = TryGetFillText(context, sourceRow, sourceColumn, out text);
                }

                if (hasText)
                {
                    context.TrySetCellText(item, columnIndex, text);
                }
            }
        }
    }

    private static Dictionary<int, TextFillSeries> BuildTextSeriesByColumn(DataGridFillContext context, DataGridCellRange source)
    {
        var result = new Dictionary<int, TextFillSeries>();
        for (var columnIndex = source.StartColumn; columnIndex <= source.EndColumn; columnIndex++)
        {
            if (TryBuildTextSeriesForColumn(context, source, columnIndex, out var series))
            {
                result[columnIndex] = series;
            }
        }

        return result;
    }

    private static Dictionary<int, TextFillSeries> BuildTextSeriesByRow(DataGridFillContext context, DataGridCellRange source)
    {
        var result = new Dictionary<int, TextFillSeries>();
        for (var rowIndex = source.StartRow; rowIndex <= source.EndRow; rowIndex++)
        {
            if (TryBuildTextSeriesForRow(context, source, rowIndex, out var series))
            {
                result[rowIndex] = series;
            }
        }

        return result;
    }

    private static bool TryBuildTextSeriesForColumn(DataGridFillContext context, DataGridCellRange source, int columnIndex, out TextFillSeries series)
    {
        series = default;
        var values = new List<string>();
        for (var rowIndex = source.StartRow; rowIndex <= source.EndRow; rowIndex++)
        {
            if (!context.TryGetCellText(rowIndex, columnIndex, out var text) || string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            values.Add(text);
        }

        return TryBuildTextSeries(values, out series);
    }

    private static bool TryBuildTextSeriesForRow(DataGridFillContext context, DataGridCellRange source, int rowIndex, out TextFillSeries series)
    {
        series = default;
        var values = new List<string>();
        for (var columnIndex = source.StartColumn; columnIndex <= source.EndColumn; columnIndex++)
        {
            if (!context.TryGetCellText(rowIndex, columnIndex, out var text) || string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            values.Add(text);
        }

        return TryBuildTextSeries(values, out series);
    }

    private static bool TryBuildTextSeries(IReadOnlyList<string> values, out TextFillSeries series)
    {
        series = default;

        if (values.Count == 0)
        {
            return false;
        }

        if (!TryParseTextSeriesToken(values[0], out var first))
        {
            return false;
        }

        var step = 1;
        if (values.Count > 1)
        {
            if (!TryParseTextSeriesToken(values[1], out var second) || !first.MatchesFormat(second))
            {
                return false;
            }

            step = second.Number - first.Number;
            var expected = second.Number;
            for (var i = 2; i < values.Count; i++)
            {
                if (!TryParseTextSeriesToken(values[i], out var token) || !first.MatchesFormat(token))
                {
                    return false;
                }

                expected += step;
                if (token.Number != expected)
                {
                    return false;
                }
            }
        }

        series = new TextFillSeries(first.Prefix, first.Suffix, first.Number, step, first.Padding, first.HasLeadingZeros);
        return true;
    }

    private static bool TryGetTextSeriesFillText(TextFillSeries series, int offset, out string text)
    {
        text = string.Empty;
        try
        {
            var value = series.Start + (series.Step * offset);
            if (series.HasLeadingZeros)
            {
                var sign = value < 0 ? "-" : string.Empty;
                var digits = Math.Abs(value).ToString("D" + series.Padding, CultureInfo.InvariantCulture);
                text = string.Concat(series.Prefix, sign, digits, series.Suffix);
                return true;
            }

            text = string.Concat(series.Prefix, value.ToString(CultureInfo.CurrentCulture), series.Suffix);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryParseTextSeriesToken(string text, out TextSeriesToken token)
    {
        token = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var span = text.AsSpan();
        var digitEnd = span.Length - 1;
        while (digitEnd >= 0 && !char.IsDigit(span[digitEnd]))
        {
            digitEnd--;
        }

        if (digitEnd < 0)
        {
            return false;
        }

        var digitStart = digitEnd;
        while (digitStart >= 0 && char.IsDigit(span[digitStart]))
        {
            digitStart--;
        }

        digitStart++;
        var numberSpan = span.Slice(digitStart, digitEnd - digitStart + 1);
        if (!int.TryParse(numberSpan, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
        {
            return false;
        }

        var prefix = span.Slice(0, digitStart).ToString();
        var suffix = span.Slice(digitEnd + 1).ToString();
        var hasLeadingZeros = numberSpan.Length > 1 && numberSpan[0] == '0';
        var padding = hasLeadingZeros ? numberSpan.Length : 0;

        token = new TextSeriesToken(prefix, suffix, number, padding, hasLeadingZeros);
        return true;
    }

    private readonly struct TextFillSeries
    {
        public TextFillSeries(string prefix, string suffix, int start, int step, int padding, bool hasLeadingZeros)
        {
            Prefix = prefix;
            Suffix = suffix;
            Start = start;
            Step = step;
            Padding = padding;
            HasLeadingZeros = hasLeadingZeros;
        }

        public string Prefix { get; }

        public string Suffix { get; }

        public int Start { get; }

        public int Step { get; }

        public int Padding { get; }

        public bool HasLeadingZeros { get; }
    }

    private readonly struct TextSeriesToken
    {
        public TextSeriesToken(string prefix, string suffix, int number, int padding, bool hasLeadingZeros)
        {
            Prefix = prefix;
            Suffix = suffix;
            Number = number;
            Padding = padding;
            HasLeadingZeros = hasLeadingZeros;
        }

        public string Prefix { get; }

        public string Suffix { get; }

        public int Number { get; }

        public int Padding { get; }

        public bool HasLeadingZeros { get; }

        public bool MatchesFormat(TextSeriesToken other)
        {
            if (!string.Equals(Prefix, other.Prefix, StringComparison.Ordinal) ||
                !string.Equals(Suffix, other.Suffix, StringComparison.Ordinal))
            {
                return false;
            }

            if (HasLeadingZeros != other.HasLeadingZeros)
            {
                return false;
            }

            if (HasLeadingZeros && Padding != other.Padding)
            {
                return false;
            }

            return true;
        }
    }
}
