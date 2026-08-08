// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using Avalonia.Controls;
using Avalonia.Controls.DataGridFiltering;
using Avalonia.Controls.DataGridSearching;
using Avalonia.Controls.DataGridSorting;
using BenchmarkDotNet.Attributes;

namespace ProDataGrid.SourceGeneration.Benchmarks;

[MemoryDiagnoser]
[BenchmarkCategory("Runtime", "Integration")]
public class ColumnDefinitionCreationBenchmarks
{
    [Benchmark(Baseline = true)]
    public DataGridColumnDefinitionList HandwrittenCompiledFastPath() =>
        HandwrittenBenchmarkTradeSchema.CreateCompiledColumns();

    [Benchmark]
    public DataGridColumnDefinitionList GeneratedStrictPath() =>
        BenchmarkTradeSchema.Instance.CreateColumnDefinitions();

    [Benchmark]
    public DataGridColumnDefinitionList ExpressionCompatibilityPath() =>
        HandwrittenBenchmarkTradeSchema.CreateExpressionColumns();
}

[MemoryDiagnoser]
[BenchmarkCategory("Runtime", "Accessor")]
public class AccessorReadBenchmarks
{
    private BenchmarkTradeRow[] _rows = null!;

    [Params(32, 4096)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup() => _rows = BenchmarkData.CreateRows(RowCount);

    [Benchmark(Baseline = true)]
    public decimal HandwrittenCompiledFastPath()
    {
        decimal sum = 0;
        DataGridColumnValueAccessor<BenchmarkTradeRow, decimal> accessor = HandwrittenBenchmarkTradeSchema.PriceAccessor;
        BenchmarkTradeRow[] rows = _rows;
        for (int index = 0; index < rows.Length; index++)
        {
            sum += accessor.GetValue(rows[index]);
        }
        return sum;
    }

    [Benchmark]
    public decimal GeneratedTypedAccessor()
    {
        decimal sum = 0;
        DataGridColumnValueAccessor<BenchmarkTradeRow, decimal> accessor = BenchmarkTradeSchema.Price.TypedAccessor;
        BenchmarkTradeRow[] rows = _rows;
        for (int index = 0; index < rows.Length; index++)
        {
            sum += accessor.GetValue(rows[index]);
        }
        return sum;
    }

    [Benchmark]
    public decimal GeneratedObjectBoundary()
    {
        decimal sum = 0;
        IDataGridColumnValueAccessor accessor = BenchmarkTradeSchema.Price.Accessor;
        BenchmarkTradeRow[] rows = _rows;
        for (int index = 0; index < rows.Length; index++)
        {
            sum += (decimal)accessor.GetValue(rows[index]);
        }
        return sum;
    }
}

[MemoryDiagnoser]
[BenchmarkCategory("Runtime", "Sorting")]
public class SortingExecutionBenchmarks
{
    private BenchmarkTradeRow[] _rows = null!;
    private IComparer<BenchmarkTradeRow> _handwritten = null!;
    private IComparer<BenchmarkTradeRow> _generated = null!;

    [GlobalSetup]
    public void Setup()
    {
        _rows = BenchmarkData.CreateRows(4096);
        SortingDescriptor[] descriptors =
        [
            BenchmarkTradeSchema.Price.Descending(),
            BenchmarkTradeSchema.Symbol.Ascending()
        ];
        _handwritten = HandwrittenBenchmarkTradeSchema.Operations.CreateSortComparer(descriptors);
        _generated = BenchmarkTradeSchema.Instance.CreateSortComparer(descriptors);
    }

    [Benchmark(Baseline = true)]
    public int HandwrittenCompiledFastPath() => CompareAdjacent(_handwritten, _rows);

    [Benchmark]
    public int GeneratedStrictPath() => CompareAdjacent(_generated, _rows);

    private static int CompareAdjacent(IComparer<BenchmarkTradeRow> comparer, BenchmarkTradeRow[] rows)
    {
        int result = 0;
        for (int index = 1; index < rows.Length; index++)
        {
            result += comparer.Compare(rows[index - 1], rows[index]);
        }
        return result;
    }
}

[MemoryDiagnoser]
[BenchmarkCategory("Runtime", "Filtering")]
public class FilteringExecutionBenchmarks
{
    private BenchmarkTradeRow[] _rows = null!;
    private Func<BenchmarkTradeRow, bool> _handwritten = null!;
    private Func<BenchmarkTradeRow, bool> _generated = null!;

    [GlobalSetup]
    public void Setup()
    {
        _rows = BenchmarkData.CreateRows(4096);
        FilteringDescriptor[] descriptors =
        [
            BenchmarkTradeSchema.Price.GreaterThanOrEqual(100m),
            BenchmarkTradeSchema.Quantity.GreaterThan(1000L)
        ];
        _handwritten = HandwrittenBenchmarkTradeSchema.Operations.CreateFilterPredicate(descriptors);
        _generated = BenchmarkTradeSchema.Instance.CreateFilterPredicate(descriptors);
    }

    [Benchmark(Baseline = true)]
    public int HandwrittenCompiledFastPath() => CountMatches(_handwritten, _rows);

    [Benchmark]
    public int GeneratedStrictPath() => CountMatches(_generated, _rows);

    private static int CountMatches(Func<BenchmarkTradeRow, bool> predicate, BenchmarkTradeRow[] rows)
    {
        int count = 0;
        for (int index = 0; index < rows.Length; index++)
        {
            if (predicate(rows[index]))
            {
                count++;
            }
        }
        return count;
    }
}

[MemoryDiagnoser]
[BenchmarkCategory("Runtime", "Searching")]
public class SearchingExecutionBenchmarks
{
    private BenchmarkTradeRow[] _rows = null!;
    private Func<BenchmarkTradeRow, bool> _handwritten = null!;
    private Func<BenchmarkTradeRow, bool> _generated = null!;

    [GlobalSetup]
    public void Setup()
    {
        _rows = BenchmarkData.CreateRows(4096);
        SearchDescriptor[] descriptors = [BenchmarkTradeSchema.Symbol.Search("AV")];
        _handwritten = HandwrittenBenchmarkTradeSchema.Operations.CreateSearchPredicate(descriptors);
        _generated = BenchmarkTradeSchema.Instance.CreateSearchPredicate(descriptors);
    }

    [Benchmark(Baseline = true)]
    public int HandwrittenCompiledFastPath() => CountMatches(_handwritten, _rows);

    [Benchmark]
    public int GeneratedStrictPath() => CountMatches(_generated, _rows);

    private static int CountMatches(Func<BenchmarkTradeRow, bool> predicate, BenchmarkTradeRow[] rows)
    {
        int count = 0;
        for (int index = 0; index < rows.Length; index++)
        {
            if (predicate(rows[index]))
            {
                count++;
            }
        }
        return count;
    }
}

internal static class BenchmarkData
{
    private static readonly string[] s_symbols = ["AVLN", "GRID", "RXUI", "AOT", "DATA", "FAST", "CACHE", "VIEW"];
    private static readonly string[] s_desks = ["Warsaw", "London", "New York", "Tokyo"];

    internal static BenchmarkTradeRow[] CreateRows(int count)
    {
        var rows = new BenchmarkTradeRow[count];
        for (int index = 0; index < count; index++)
        {
            rows[index] = new BenchmarkTradeRow
            {
                Id = index + 1,
                Symbol = s_symbols[index % s_symbols.Length],
                Desk = s_desks[index % s_desks.Length],
                Price = 75m + (index % 175) * 1.25m,
                Quantity = 250 + (index % 40) * 125L
            };
        }
        return rows;
    }
}
