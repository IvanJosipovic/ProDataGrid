// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System.Linq.Expressions;
using Avalonia.Controls;
using Avalonia.Data.Core;

namespace ProDataGrid.SourceGeneration.Benchmarks;

internal static class HandwrittenBenchmarkTradeSchema
{
    private static readonly IPropertyInfo s_idProperty = CreateProperty<int>(
        nameof(BenchmarkTradeRow.Id),
        static row => row.Id,
        static (row, value) => row.Id = value);

    private static readonly IPropertyInfo s_symbolProperty = CreateProperty<string>(
        nameof(BenchmarkTradeRow.Symbol),
        static row => row.Symbol,
        static (row, value) => row.Symbol = value);

    private static readonly IPropertyInfo s_deskProperty = CreateProperty<string>(
        nameof(BenchmarkTradeRow.Desk),
        static row => row.Desk,
        static (row, value) => row.Desk = value);

    private static readonly IPropertyInfo s_priceProperty = CreateProperty<decimal>(
        nameof(BenchmarkTradeRow.Price),
        static row => row.Price,
        static (row, value) => row.Price = value);

    private static readonly IPropertyInfo s_quantityProperty = CreateProperty<long>(
        nameof(BenchmarkTradeRow.Quantity),
        static row => row.Quantity,
        static (row, value) => row.Quantity = value);

    internal static readonly DataGridColumnValueAccessor<BenchmarkTradeRow, int> IdAccessor =
        new(static row => row.Id, static (row, value) => row.Id = value);

    internal static readonly DataGridColumnValueAccessor<BenchmarkTradeRow, string> SymbolAccessor =
        new(static row => row.Symbol, static (row, value) => row.Symbol = value);

    internal static readonly DataGridColumnValueAccessor<BenchmarkTradeRow, string> DeskAccessor =
        new(static row => row.Desk, static (row, value) => row.Desk = value);

    internal static readonly DataGridColumnValueAccessor<BenchmarkTradeRow, decimal> PriceAccessor =
        new(static row => row.Price, static (row, value) => row.Price = value);

    internal static readonly DataGridColumnValueAccessor<BenchmarkTradeRow, long> QuantityAccessor =
        new(static row => row.Quantity, static (row, value) => row.Quantity = value);

    internal static readonly DataGridGeneratedDataOperations<BenchmarkTradeRow> Operations = new(
    [
        new DataGridColumnAccessorRegistration("id", nameof(BenchmarkTradeRow.Id), IdAccessor),
        new DataGridColumnAccessorRegistration("symbol", nameof(BenchmarkTradeRow.Symbol), SymbolAccessor),
        new DataGridColumnAccessorRegistration("desk", nameof(BenchmarkTradeRow.Desk), DeskAccessor),
        new DataGridColumnAccessorRegistration("price", nameof(BenchmarkTradeRow.Price), PriceAccessor),
        new DataGridColumnAccessorRegistration("quantity", nameof(BenchmarkTradeRow.Quantity), QuantityAccessor)
    ]);

    internal static DataGridColumnDefinitionList CreateCompiledColumns()
    {
        DataGridColumnDefinitionBuilder<BenchmarkTradeRow> builder = DataGridColumnDefinitionBuilder.For<BenchmarkTradeRow>();
        var columns = new DataGridColumnDefinitionList
        {
            Configure(builder.Numeric("ID", s_idProperty, static row => row.Id, static (row, value) => row.Id = value), "id", nameof(BenchmarkTradeRow.Id), IdAccessor, typeof(int)),
            Configure(builder.Text("Symbol", s_symbolProperty, static row => row.Symbol, static (row, value) => row.Symbol = value), "symbol", nameof(BenchmarkTradeRow.Symbol), SymbolAccessor, typeof(string)),
            Configure(builder.Text("Desk", s_deskProperty, static row => row.Desk, static (row, value) => row.Desk = value), "desk", nameof(BenchmarkTradeRow.Desk), DeskAccessor, typeof(string)),
            Configure(builder.Numeric("Price", s_priceProperty, static row => row.Price, static (row, value) => row.Price = value), "price", nameof(BenchmarkTradeRow.Price), PriceAccessor, typeof(decimal)),
            Configure(builder.Numeric("Quantity", s_quantityProperty, static row => row.Quantity, static (row, value) => row.Quantity = value), "quantity", nameof(BenchmarkTradeRow.Quantity), QuantityAccessor, typeof(long))
        };
        return columns;
    }

    internal static DataGridColumnDefinitionList CreateExpressionColumns()
    {
        var columns = new DataGridColumnDefinitionList
        {
            Configure(CreateExpressionColumn<DataGridNumericColumnDefinition, int>("ID", static row => row.Id), "id", nameof(BenchmarkTradeRow.Id), IdAccessor, typeof(int)),
            Configure(CreateExpressionColumn<DataGridTextColumnDefinition, string>("Symbol", static row => row.Symbol), "symbol", nameof(BenchmarkTradeRow.Symbol), SymbolAccessor, typeof(string)),
            Configure(CreateExpressionColumn<DataGridTextColumnDefinition, string>("Desk", static row => row.Desk), "desk", nameof(BenchmarkTradeRow.Desk), DeskAccessor, typeof(string)),
            Configure(CreateExpressionColumn<DataGridNumericColumnDefinition, decimal>("Price", static row => row.Price), "price", nameof(BenchmarkTradeRow.Price), PriceAccessor, typeof(decimal)),
            Configure(CreateExpressionColumn<DataGridNumericColumnDefinition, long>("Quantity", static row => row.Quantity), "quantity", nameof(BenchmarkTradeRow.Quantity), QuantityAccessor, typeof(long))
        };
        return columns;
    }

    private static IPropertyInfo CreateProperty<TValue>(
        string name,
        Func<BenchmarkTradeRow, TValue> getter,
        Action<BenchmarkTradeRow, TValue> setter) =>
        new ClrPropertyInfo(
            name,
            target => target is BenchmarkTradeRow row ? getter(row) : default,
            (target, value) =>
            {
                if (target is BenchmarkTradeRow row)
                {
                    setter(row, value is null ? default! : (TValue)value);
                }
            },
            typeof(TValue));

    private static TDefinition CreateExpressionColumn<TDefinition, TValue>(
        object header,
        Expression<Func<BenchmarkTradeRow, TValue>> expression)
        where TDefinition : DataGridBoundColumnDefinition, new() =>
        new()
        {
            Header = header,
            Binding = DataGridBindingDefinition.Create(expression)
        };

    private static TDefinition Configure<TDefinition>(
        TDefinition column,
        string key,
        string propertyName,
        IDataGridColumnValueAccessor accessor,
        Type valueType)
        where TDefinition : DataGridColumnDefinition
    {
        column.ColumnKey = key;
        column.SortMemberPath = propertyName;
        column.ValueAccessor = accessor;
        column.ValueType = valueType;
        return column;
    }
}
