// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using ProDataGrid.SourceGeneration;

namespace ProDataGrid.SourceGeneration.AotSmoke;

[GenerateDataGridColumns(
    ProviderName = "AotTradeRowSchema",
    SchemaId = "smoke/aot-trade-row/v1",
    Discovery = DataGridColumnDiscovery.AttributedOnly,
    Strict = true,
    Streaming = true)]
internal sealed class AotTradeRow
{
    [DataGridKey]
    [DataGridColumn(DataGridColumnKind.Numeric, Header = "ID", ColumnKey = "id", Order = 0, IsReadOnly = true)]
    public int Id { get; init; }

    [DataGridColumn(DataGridColumnKind.Text, Header = "Symbol", ColumnKey = "symbol", Order = 1, Width = "*")]
    public string Symbol { get; init; } = string.Empty;

    [DataGridColumn(DataGridColumnKind.Text, Header = "Desk", ColumnKey = "desk", Order = 2, Width = "*")]
    public string Desk { get; init; } = string.Empty;

    [DataGridColumn(DataGridColumnKind.Numeric, Header = "Price", ColumnKey = "price", Order = 3, FormatString = "N2")]
    public decimal Price { get; init; }

    [DataGridColumn(DataGridColumnKind.DatePicker, Header = "Timestamp", ColumnKey = "timestamp", Order = 4, FormatString = "HH:mm:ss")]
    public DateTimeOffset Timestamp { get; init; }
}
