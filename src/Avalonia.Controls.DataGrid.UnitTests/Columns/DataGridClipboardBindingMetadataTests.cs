using System;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Metadata;
using Xunit;

namespace Avalonia.Controls.DataGridTests.Columns;

public class DataGridClipboardBindingMetadataTests
{
    [Theory]
    [InlineData(typeof(DataGridColumn))]
    [InlineData(typeof(DataGridBoundColumn))]
    [InlineData(typeof(DataGridComboBoxColumn))]
    [InlineData(typeof(DataGridHyperlinkColumn))]
    public void ClipboardContentBinding_Inherits_Row_Item_DataType(Type columnType)
    {
        PropertyInfo? property = columnType.GetProperty(
            nameof(DataGridColumn.ClipboardContentBinding),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

        Assert.NotNull(property);
        Assert.NotNull(property.GetCustomAttribute<AssignBindingAttribute>(inherit: false));

        InheritDataTypeFromItemsAttribute? attribute =
            property.GetCustomAttribute<InheritDataTypeFromItemsAttribute>(inherit: false);

        Assert.NotNull(attribute);
        Assert.Equal(nameof(DataGrid.ItemsSource), attribute.AncestorItemsProperty);
        Assert.Equal(typeof(DataGrid), attribute.AncestorType);
    }
}
