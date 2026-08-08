// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System.Collections;
using Avalonia.Controls;
using BenchmarkDotNet.Attributes;
using ProDataGrid.Charting;

namespace ProDataGrid.SourceGeneration.Benchmarks;

[MemoryDiagnoser]
[BenchmarkCategory("Runtime", "Analytics")]
public class GeneratedChartRangeProjectionBenchmarks
{
    private BenchmarkTradeRow[] _rows = null!;
    private DataGridColumnDefinitionList _columns = null!;

    [Params(256, 4096)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _rows = BenchmarkData.CreateRows(RowCount);
        _columns = BenchmarkTradeSchema.Instance.CreateColumnDefinitions();
    }

    [Benchmark]
    public int CreateBoundedGeneratedRange()
    {
        using DataGridGeneratedChartRangeProjection projection = DataGridGeneratedChartAdapter.CreateRangeProjection(
            _rows,
            BenchmarkTradeSchema.AnalyticsFields,
            _columns,
            new DataGridCellRange(0, RowCount - 1, 1, 4),
            maximumRows: 8192);
        return projection.Model.Snapshot.Categories.Count + projection.Model.Snapshot.Series.Count;
    }

    [Benchmark]
    public int BuildGeneratedLongFormSeries()
    {
        using DataGridGeneratedLongFormChartDataSource source = DataGridGeneratedChartAdapter.CreateLongFormSource(
            _rows,
            BenchmarkTradeSchema.AnalyticsFields,
            maximumItems: 8192,
            maximumSeries: 32);
        ProCharts.ChartDataSnapshot snapshot = source.BuildSnapshot(new ProCharts.ChartDataRequest());
        return snapshot.Categories.Count + snapshot.Series.Count;
    }

    [Benchmark]
    public int BuildGeneratedOutline()
    {
        using Avalonia.Controls.DataGridReporting.OutlineReportModel outline =
            BenchmarkTradeSchema.CreateOutlineReportModel(_rows);
        return outline.GroupFields.Count + outline.ValueFields.Count;
    }
}

[MemoryDiagnoser]
[BenchmarkCategory("Runtime", "Analytics", "Identity")]
public class GeneratedChartKeyLookupBenchmarks
{
    private BenchmarkTradeRow[] _rows = null!;
    private DataGridGeneratedListChartKeyMap<BenchmarkTradeRow, int> _keyMap = null!;
    private int[] _keys = null!;

    [Params(256, 4096)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _rows = BenchmarkData.CreateRows(RowCount);
        _keyMap = new DataGridGeneratedListChartKeyMap<BenchmarkTradeRow, int>(
            (IList)_rows,
            BenchmarkTradeSchema.Instance);
        _keys = new int[32];
        for (int index = 0; index < _keys.Length; index++)
        {
            _keys[index] = 1 + index * Math.Max(1, (RowCount - 1) / (_keys.Length - 1));
        }
    }

    [GlobalCleanup]
    public void Cleanup() => _keyMap.Dispose();

    [Benchmark(Baseline = true)]
    public int LinearSourceScan()
    {
        int checksum = 0;
        for (int keyIndex = 0; keyIndex < _keys.Length; keyIndex++)
        {
            int key = _keys[keyIndex];
            for (int rowIndex = 0; rowIndex < _rows.Length; rowIndex++)
            {
                if (_rows[rowIndex].Id == key)
                {
                    checksum += rowIndex;
                    break;
                }
            }
        }
        return checksum;
    }

    [Benchmark]
    public int GeneratedStableKeyIndex()
    {
        int checksum = 0;
        for (int keyIndex = 0; keyIndex < _keys.Length; keyIndex++)
        {
            if (_keyMap.TryGetIndex(_keys[keyIndex], out int rowIndex))
            {
                checksum += rowIndex;
            }
        }
        return checksum;
    }
}
