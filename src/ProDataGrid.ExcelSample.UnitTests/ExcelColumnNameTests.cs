using ProDataGrid.ExcelSample.Helpers;
using Xunit;

namespace ProDataGrid.ExcelSample.Tests;

public sealed class ExcelColumnNameTests
{
    [Theory]
    [InlineData(0, "A")]
    [InlineData(1, "B")]
    [InlineData(25, "Z")]
    [InlineData(26, "AA")]
    [InlineData(27, "AB")]
    [InlineData(51, "AZ")]
    [InlineData(52, "BA")]
    [InlineData(701, "ZZ")]
    [InlineData(702, "AAA")]
    public void FromIndex_ReturnsExpectedName(int index, string expected)
    {
        var actual = ExcelColumnName.FromIndex(index);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("A", 0)]
    [InlineData("B", 1)]
    [InlineData("Z", 25)]
    [InlineData("AA", 26)]
    [InlineData("AZ", 51)]
    [InlineData("ZZ", 701)]
    [InlineData("$C$D", 81)]
    [InlineData("ab", 27)]
    public void TryParseIndex_ReadsColumnIndex(string text, int expected)
    {
        var success = ExcelColumnName.TryParseIndex(text, out var index);

        Assert.True(success);
        Assert.Equal(expected, index);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("A1")]
    [InlineData("A-")]
    public void TryParseIndex_RejectsInvalidInput(string text)
    {
        var success = ExcelColumnName.TryParseIndex(text, out _);

        Assert.False(success);
    }
}
