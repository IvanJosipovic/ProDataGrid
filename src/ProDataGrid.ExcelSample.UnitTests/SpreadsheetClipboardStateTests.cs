using ProDataGrid.ExcelSample.Models;
using Xunit;

namespace ProDataGrid.ExcelSample.Tests;

public sealed class SpreadsheetClipboardStateTests
{
    [Fact]
    public void SetCopiedRange_UpdatesClipboardSize()
    {
        var state = new SpreadsheetClipboardState();
        var range = new SpreadsheetCellRange(
            new SpreadsheetCellReference(0, 0),
            new SpreadsheetCellReference(1, 2));

        state.SetCopiedRange(range);

        Assert.True(state.HasClipboard);
        Assert.Equal(2, state.ClipboardRowCount);
        Assert.Equal(3, state.ClipboardColumnCount);
        Assert.Equal(range, state.CopiedRange);
    }

    [Fact]
    public void Clear_ResetsClipboardMetadata()
    {
        var state = new SpreadsheetClipboardState();
        state.SetCopiedRange(new SpreadsheetCellRange(
            new SpreadsheetCellReference(0, 0),
            new SpreadsheetCellReference(0, 0)));

        state.Clear();

        Assert.False(state.HasClipboard);
        Assert.Equal(0, state.ClipboardRowCount);
        Assert.Equal(0, state.ClipboardColumnCount);
        Assert.Null(state.CopiedRange);
    }
}
