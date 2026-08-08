// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using Avalonia.Controls;
using ProDataGrid.SourceGeneration;

namespace ProDataGrid.SourceGeneration.Benchmarks;

[GenerateDataGridColumns(
    ProviderName = "BenchmarkTradeSchema",
    SchemaId = "benchmarks/trade/v1",
    Discovery = DataGridColumnDiscovery.AttributedOnly,
    Strict = true,
    Streaming = true)]
public sealed class BenchmarkTradeRow
{
    [DataGridKey]
    [DataGridColumn(DataGridColumnKind.Numeric, Header = "ID", ColumnKey = "id", Order = 0)]
    public int Id { get; set; }

    [DataGridColumn(DataGridColumnKind.Text, Header = "Symbol", ColumnKey = "symbol", Order = 1)]
    [DataGridChartField(DataGridGeneratedAnalyticsRole.ChartCategory, Order = 0)]
    public string Symbol { get; set; } = string.Empty;

    [DataGridColumn(DataGridColumnKind.Text, Header = "Desk", ColumnKey = "desk", Order = 2)]
    [DataGridChartField(DataGridGeneratedAnalyticsRole.ChartSeries, Order = 0)]
    public string Desk { get; set; } = string.Empty;

    [DataGridColumn(DataGridColumnKind.Numeric, Header = "Price", ColumnKey = "price", Order = 3)]
    [DataGridChartField(DataGridGeneratedAnalyticsRole.ChartValue, Order = 0, Series = "Price")]
    public decimal Price { get; set; }

    [DataGridColumn(DataGridColumnKind.Numeric, Header = "Quantity", ColumnKey = "quantity", Order = 4)]
    [DataGridChartField(DataGridGeneratedAnalyticsRole.ChartValue, Order = 1, Series = "Quantity")]
    public long Quantity { get; set; }
}
