// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls;
using Xunit;

namespace Avalonia.Controls.DataGridTests.ColumnDefinitions;

public sealed class DataGridGeneratedTransferTests
{
    [Fact]
    public void Export_uses_stable_keys_typed_formatters_and_supported_representations()
    {
        Row[] rows = [new(1, "A, B", 12.5m), new(2, "C", 3m)];
        using DataGridGeneratedEditController<Row, int> edits = CreateEdits();
        var clipboard = new DataGridGeneratedClipboardController<Row, int>(new RowKey(), edits);

        string csv = clipboard.Export(rows, ["name", "amount"], DataGridGeneratedExportFormat.Csv, formatProvider: CultureInfo.InvariantCulture);
        string json = clipboard.Export(rows, ["name"], DataGridGeneratedExportFormat.Json);
        string html = clipboard.Export(rows, ["name"], DataGridGeneratedExportFormat.Html);

        Assert.Equal("name,amount\n\"A, B\",12.50\nC,3.00\n", csv.Replace("\r\n", "\n", StringComparison.Ordinal));
        Assert.Equal("[{\"name\":\"A, B\"},{\"name\":\"C\"}]", json);
        Assert.Contains("<td>A, B</td>", html);
    }

    [Fact]
    public void Paste_is_quoted_typed_structured_and_one_undo_batch()
    {
        Row[] rows = [new(1, "old", 1m), new(2, "old", 2m)];
        using DataGridGeneratedEditController<Row, int> edits = CreateEdits();
        var clipboard = new DataGridGeneratedClipboardController<Row, int>(new RowKey(), edits);

        DataGridGeneratedTransferResult<int> result = clipboard.PasteDelimited(
            rows,
            ["name", "amount"],
            "\"new, one\",10.5\nsecond,invalid".AsSpan(),
            ',',
            CultureInfo.InvariantCulture);

        Assert.Equal(3, result.AppliedCells);
        Assert.Single(result.Errors);
        Assert.Equal(DataGridGeneratedEditStatus.ParseFailed, result.Errors[0].Result.Status);
        Assert.Equal(("new, one", 10.5m), (rows[0].Name, rows[0].Amount));
        Assert.Equal(("second", 2m), (rows[1].Name, rows[1].Amount));
        Assert.True(edits.Undo());
        Assert.Equal(("old", 1m), (rows[0].Name, rows[0].Amount));
        Assert.Equal(("old", 2m), (rows[1].Name, rows[1].Amount));
    }

    [Fact]
    public void Fill_supports_copy_custom_series_limits_and_undo()
    {
        Row[] rows = [new(1, "A", 1m), new(2, "B", 2m), new(3, "C", 3m)];
        using DataGridGeneratedEditController<Row, int> edits = CreateEdits();
        var fill = new DataGridGeneratedFillController<Row, int>(new RowKey(), edits);

        DataGridGeneratedTransferResult<int> copied = fill.CopyDown(rows, "name");
        DataGridGeneratedTransferResult<int> series = fill.Fill(rows, "amount", 0, index => 10m + index, maximumCells: 2);

        Assert.Equal(2, copied.AppliedCells);
        Assert.Equal(("A", "A", "A"), (rows[0].Name, rows[1].Name, rows[2].Name));
        Assert.True(series.Truncated);
        Assert.Equal((10m, 11m, 3m), (rows[0].Amount, rows[1].Amount, rows[2].Amount));
        Assert.True(edits.Undo());
        Assert.Equal((1m, 2m, 3m), (rows[0].Amount, rows[1].Amount, rows[2].Amount));
    }

    [Fact]
    public void Export_enforces_cell_and_character_limits()
    {
        Row[] rows = [new(1, "A", 1m)];
        using DataGridGeneratedEditController<Row, int> edits = CreateEdits();
        var clipboard = new DataGridGeneratedClipboardController<Row, int>(new RowKey(), edits);

        Assert.Throws<InvalidOperationException>(() => clipboard.Export(
            rows, ["name", "amount"], limits: new DataGridGeneratedTransferLimits(1, 100)));
        Assert.Throws<InvalidOperationException>(() => clipboard.Export(
            rows, ["name"], limits: new DataGridGeneratedTransferLimits(10, 1)));
    }

    [Fact]
    public void Paste_ignores_a_terminal_line_break()
    {
        Row[] rows = [new(1, "old", 1m), new(2, "unchanged", 2m)];
        using DataGridGeneratedEditController<Row, int> edits = CreateEdits();
        var clipboard = new DataGridGeneratedClipboardController<Row, int>(new RowKey(), edits);

        DataGridGeneratedTransferResult<int> result = clipboard.PasteDelimited(
            rows,
            ["name"],
            "new\r\n".AsSpan());

        Assert.Equal(1, result.AppliedCells);
        Assert.Equal("new", rows[0].Name);
        Assert.Equal("unchanged", rows[1].Name);
    }

    private static DataGridGeneratedEditController<Row, int> CreateEdits() =>
        new(
            new RowKey(),
            new IDataGridGeneratedEditField<Row>[]
            {
                new DataGridGeneratedEditField<Row, string>(
                    "name",
                    static row => row.Name,
                    static (row, value) => row.Name = value,
                    static (ReadOnlySpan<char> text, IFormatProvider _, out string value) => { value = text.ToString(); return true; },
                    static (value, _) => value),
                new DataGridGeneratedEditField<Row, decimal>(
                    "amount",
                    static row => row.Amount,
                    static (row, value) => row.Amount = value,
                    static (ReadOnlySpan<char> text, IFormatProvider provider, out decimal value) => decimal.TryParse(text, provider, out value),
                    static (value, provider) => value.ToString("0.00", provider))
            });

    private sealed class Row
    {
        public Row(int id, string name, decimal amount) { Id = id; Name = name; Amount = amount; }
        public int Id { get; }
        public string Name { get; set; }
        public decimal Amount { get; set; }
    }

    private sealed class RowKey : IDataGridItemKey<Row, int>
    {
        public int GetKey(Row item) => item.Id;
    }
}
