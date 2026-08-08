// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using ProDataGrid.SourceGeneration;

namespace ProDataGrid.SourceGeneration.AotSmoke;

[GenerateDataGridColumns(
    ProviderName = "AotTradeRowSchema",
    SchemaId = "smoke/aot-trade-row/v1",
    Discovery = DataGridColumnDiscovery.AttributedOnly,
    Strict = true,
    Streaming = true,
    MutationHandlerType = typeof(AotTradeMutationHandler),
    NewRowFactoryType = typeof(AotTradeNewRowFactory))]
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

internal sealed class AotTradeMutationHandler : IDataGridGeneratedCollectionMutationHandler<AotTradeRow>
{
    public ValueTask AddAsync(
        int index,
        ReadOnlyMemory<AotTradeRow> items,
        CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public ValueTask RemoveAsync(
        int index,
        ReadOnlyMemory<AotTradeRow> items,
        CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public ValueTask ReplaceAsync(
        int index,
        ReadOnlyMemory<AotTradeRow> oldItems,
        ReadOnlyMemory<AotTradeRow> newItems,
        CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public ValueTask MoveAsync(
        int oldIndex,
        int newIndex,
        int count,
        CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public ValueTask ResetAsync(
        ReadOnlyMemory<AotTradeRow> items,
        CancellationToken cancellationToken) => ValueTask.CompletedTask;
}

internal sealed class AotTradeNewRowFactory : IDataGridGeneratedNewRowFactory<AotTradeRow>
{
    public ValueTask<AotTradeRow> CreateAsync(CancellationToken cancellationToken) =>
        ValueTask.FromResult(new AotTradeRow { Id = 42, Symbol = "NEW" });
}
