// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using Avalonia.Controls;
using Xunit;

namespace Avalonia.Controls.DataGridTests.ColumnDefinitions;

public sealed class DataGridGeneratedIndexedColumnTests
{
    [Fact]
    public void Factory_creates_typed_method_backed_definition()
    {
        var row = new Row(["old"]);
        var options = new DataGridGeneratedIndexedColumnOptions<string>
        {
            Header = "A",
            ColumnKey = "cell-a",
            PropertyName = "A",
            Kind = DataGridGeneratedIndexedColumnKind.Text,
            FormatString = "value:{0}"
        };

        DataGridColumnDefinition definition = DataGridGeneratedIndexedColumnFactory.Create<Row, string>(
            0,
            item => (string)item.Get(0)!,
            (item, value) => item.Set(0, value),
            in options);

        DataGridTextColumnDefinition text = Assert.IsType<DataGridTextColumnDefinition>(definition);
        Assert.Equal("cell-a", text.ColumnKey);
        Assert.Equal("A", text.SortMemberPath);
        Assert.Equal("value:{0}", text.Binding.StringFormat);
        Assert.Equal("old", text.Binding.ValueAccessor.GetValue(row));
        text.Binding.ValueAccessor.SetValue(row, "new");
        Assert.Equal("new", row.Get(0));
    }

    private sealed class Row
    {
        private readonly object?[] _values;
        public Row(object?[] values) => _values = values;
        public object? Get(int index) => _values[index];
        public void Set(int index, object? value) => _values[index] = value;
    }
}
