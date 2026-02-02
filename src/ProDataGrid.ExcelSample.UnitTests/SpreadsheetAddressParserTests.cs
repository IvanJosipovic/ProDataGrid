using ProDataGrid.ExcelSample.Helpers;
using ProDataGrid.ExcelSample.Models;
using Xunit;

namespace ProDataGrid.ExcelSample.Tests;

public sealed class SpreadsheetAddressParserTests
{
    [Theory]
    [InlineData("A1", 0, 0)]
    [InlineData("B2", 1, 1)]
    [InlineData("$C$7", 6, 2)]
    [InlineData("AA10", 9, 26)]
    public void TryParseCellReference_ReadsRowAndColumn(string text, int expectedRow, int expectedColumn)
    {
        var success = SpreadsheetAddressParser.TryParseCellReference(text, out var cell);

        Assert.True(success);
        Assert.Equal(expectedRow, cell.RowIndex);
        Assert.Equal(expectedColumn, cell.ColumnIndex);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("A")]
    [InlineData("A0")]
    public void TryParseCellReference_RejectsInvalidInput(string text)
    {
        var success = SpreadsheetAddressParser.TryParseCellReference(text, out _);

        Assert.False(success);
    }

    [Fact]
    public void TryParseRange_ReadsStartAndEnd()
    {
        var success = SpreadsheetAddressParser.TryParseRange("B2:D4", out var range);

        Assert.True(success);
        Assert.Equal(new SpreadsheetCellReference(1, 1), range.Start);
        Assert.Equal(new SpreadsheetCellReference(3, 3), range.End);
    }

    [Fact]
    public void TryParseRange_AllowsSingleCell()
    {
        var success = SpreadsheetAddressParser.TryParseRange("C3", out var range);

        Assert.True(success);
        Assert.True(range.IsSingleCell);
        Assert.Equal(new SpreadsheetCellReference(2, 2), range.Start);
    }
}
