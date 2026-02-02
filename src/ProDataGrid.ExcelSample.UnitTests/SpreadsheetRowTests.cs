using System;
using ProDataGrid.ExcelSample.Models;
using Xunit;

namespace ProDataGrid.ExcelSample.Tests;

public sealed class SpreadsheetRowTests
{
    [Fact]
    public void SetCell_RaisesPropertyChangedWithColumnName()
    {
        var row = new SpreadsheetRow(columnCount: 3, rowIndex: 1);
        string? propertyName = null;
        row.PropertyChanged += (_, args) => propertyName = args.PropertyName;

        row.SetCell(1, "Hello");

        Assert.Equal("B", propertyName);
        Assert.Equal("Hello", row.GetCell<string>(1));
    }

    [Fact]
    public void GetCell_InvalidIndex_Throws()
    {
        var row = new SpreadsheetRow(columnCount: 2, rowIndex: 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => row.GetCell(2));
        Assert.Throws<ArgumentOutOfRangeException>(() => row.SetCell(3, 5));
    }
}
