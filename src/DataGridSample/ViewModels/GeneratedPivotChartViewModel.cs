// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.ObjectModel;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.DataGridPivoting;
using Avalonia.Controls.DataGridSelection;
using Avalonia.Controls.Selection;
using DataGridSample.Models;
using ProCharts;
using ProCharts.Skia;
using ProDataGrid.Charting;
using ProDataGrid.SourceGeneration;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using RxVoid = ReactiveUI.Primitives.RxVoid;

namespace DataGridSample.ViewModels;

[GenerateDataGridViewModel(typeof(GeneratedPivotChartRow), ProviderName = "GeneratedPivotChartRowSchema")]
[GenerateDataGridView(
    typeof(GeneratedPivotChartRow),
    ViewName = "GeneratedPivotChartGrid",
    ViewNamespace = "DataGridSample.Pages",
    Framework = DataGridViewFramework.ReactiveUI,
    Recipe = DataGridViewRecipe.Analytics,
    Title = "Generated analytics source",
    AutomationId = "generated-pivot-chart-grid",
    SelectionModelPropertyName = nameof(SelectionModel))]
public sealed partial class GeneratedPivotChartViewModel : ReactiveObject, IDisposable
{
    private readonly ObservableCollection<GeneratedPivotChartRow> _source = [];
    private int _nextId = 9001;
    private int _nextPeriod = 5;
    private int _metricIndex;
    private bool _synchronizingSelection;
    private bool _disposed;

    [Reactive]
    private string _status = "Generated pivot and chart selectors share one canonical schema.";

    [Reactive]
    private string _selectedMetric = "Revenue";

    [Reactive]
    private int _sourceRowCount;

    [Reactive]
    private int _pivotRowCount;

    [Reactive]
    private int _pivotColumnCount;

    [Reactive]
    private int _directSeriesCount;

    public GeneratedPivotChartViewModel()
    {
        AddBaselineRows();
        Items = GeneratedPivotChartRowSchema.CreateCollectionView(_source, sourceIsInGroupOrder: false);

        Pivot = GeneratedPivotChartRowSchema.CreatePivotTableModel(Items);
        PivotChart = new PivotChartModel
        {
            Pivot = Pivot,
            SeriesSource = PivotChartSeriesSource.Columns,
            ValueField = Pivot.ValueFields[0],
            IncludeSubtotals = false,
            IncludeGrandTotals = false
        };
        PivotChartSource = new PivotChartDataSource
        {
            PivotChart = PivotChart,
            SeriesKind = ChartSeriesKind.Column
        };
        PivotChartModel = new ChartModel { DataSource = PivotChartSource };

        SelectionController = GeneratedPivotChartRowSchema.CreateSelectionController(
            new DataGridGeneratedSelectionProfile { Mode = DataGridSelectionMode.Single });
        SelectionController.ResetSource(_source);
        SelectionModel = SelectionController.CreateIdentitySelectionModel(Items);
        SelectionModel.SelectionChanged += SelectionModelOnSelectionChanged;
        SelectionController.SelectionChanged += SelectionControllerOnSelectionChanged;

        RangeChartProjection = DataGridGeneratedChartAdapter.CreateRangeProjection(
            Items,
            GeneratedPivotChartRowSchema.AnalyticsFields,
            ColumnDefinitions,
            new DataGridCellRange(0, 7, 1, 5),
            maximumRows: 64);
        DirectChartSource = RangeChartProjection.DataSource;
        DirectChartSource.Series[0].Kind = ChartSeriesKind.Column;
        DirectChartSource.Series[1].Kind = ChartSeriesKind.Line;
        DirectChartModel = RangeChartProjection.Model;
        ChartKeyMap = new DataGridGeneratedListChartKeyMap<GeneratedPivotChartRow, int>(
            Items,
            GeneratedPivotChartRowSchema.Instance);
        ChartSelection = new DataGridGeneratedChartSelectionSynchronizer<GeneratedPivotChartRow, int>(
            ChartKeyMap,
            SelectionController,
            DirectChartModel.Interaction,
            categoryToSourceIndex: index => RangeChartProjection.Range.StartRow + index,
            sourceToCategoryIndex: index => ToRangeCategoryIndex(index, RangeChartProjection.Range));
        LongFormChartSource = DataGridGeneratedChartAdapter.CreateLongFormSource(
            Items,
            GeneratedPivotChartRowSchema.AnalyticsFields,
            maximumItems: 256,
            maximumSeries: 16);
        LongFormChartSource.SeriesKind = ChartSeriesKind.Line;
        LongFormChartModel = new ChartModel { DataSource = LongFormChartSource };

        ChartStyle = new SkiaChartStyle
        {
            ShowGridlines = true,
            ShowCategoryGridlines = true,
            LegendFlow = SkiaLegendFlow.Row,
            LegendWrap = true
        };

        AddPeriodCommand = ReactiveCommand.Create(AddPeriod);
        RemovePeriodCommand = ReactiveCommand.Create(RemovePeriod);
        ToggleMetricCommand = ReactiveCommand.Create(ToggleMetric);
        ToggleSeriesSourceCommand = ReactiveCommand.Create(ToggleSeriesSource);
        RestoreCommand = ReactiveCommand.Create(Restore);

        Publish("Loaded deterministic grouped source, generated pivot fields, and two chart projections.");
    }

    public DataGridCollectionView Items { get; }

    public PivotTableModel Pivot { get; }

    public PivotChartModel PivotChart { get; }

    public PivotChartDataSource PivotChartSource { get; }

    public ChartModel PivotChartModel { get; }

    public DataGridChartModel DirectChartSource { get; }

    public ChartModel DirectChartModel { get; }

    public DataGridGeneratedChartRangeProjection RangeChartProjection { get; }

    public DataGridGeneratedListChartKeyMap<GeneratedPivotChartRow, int> ChartKeyMap { get; }

    public DataGridGeneratedChartSelectionSynchronizer<GeneratedPivotChartRow, int> ChartSelection { get; }

    public DataGridGeneratedLongFormChartDataSource LongFormChartSource { get; }

    public ChartModel LongFormChartModel { get; }

    public DataGridGeneratedSelectionController<GeneratedPivotChartRow, int> SelectionController { get; }

    public IdentitySelectionModel SelectionModel { get; }

    public SkiaChartStyle ChartStyle { get; }

    public ReactiveCommand<RxVoid, RxVoid> AddPeriodCommand { get; }

    public ReactiveCommand<RxVoid, RxVoid> RemovePeriodCommand { get; }

    public ReactiveCommand<RxVoid, RxVoid> ToggleMetricCommand { get; }

    public ReactiveCommand<RxVoid, RxVoid> ToggleSeriesSourceCommand { get; }

    public ReactiveCommand<RxVoid, RxVoid> RestoreCommand { get; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        SelectionModel.SelectionChanged -= SelectionModelOnSelectionChanged;
        SelectionController.SelectionChanged -= SelectionControllerOnSelectionChanged;
        ChartSelection.Dispose();
        ChartKeyMap.Dispose();
        RangeChartProjection.Dispose();
        SelectionModel.Source = Array.Empty<GeneratedPivotChartRow>();
        LongFormChartModel.Dispose();
        LongFormChartSource.Dispose();
        PivotChartModel.Dispose();
        PivotChartSource.Dispose();
        PivotChart.Dispose();
        Pivot.Dispose();
    }

    private void AddPeriod()
    {
        string period = $"P{_nextPeriod++}";
        AddPeriodRows(period, 1.08d + (_nextPeriod % 3) * 0.04d);
        RefreshCharts();
        Publish($"Added {period} as one observable batch of three regional rows.");
    }

    private void RemovePeriod()
    {
        if (_source.Count <= 3)
        {
            return;
        }

        string period = _source[^1].Period;
        while (_source.Count > 0 && string.Equals(_source[^1].Period, period, StringComparison.Ordinal))
        {
            _source.RemoveAt(_source.Count - 1);
        }

        RefreshCharts();
        Publish($"Removed {period}; pivot and chart projections observed the collection delta.");
    }

    private void ToggleMetric()
    {
        _metricIndex = (_metricIndex + 1) % Pivot.ValueFields.Count;
        PivotChart.ValueField = Pivot.ValueFields[_metricIndex];
        SelectedMetric = Pivot.ValueFields[_metricIndex].Header ?? Pivot.ValueFields[_metricIndex].Key?.ToString() ?? "Value";
        PivotChartModel.Refresh();
        Publish($"Pivot chart now uses the generated {SelectedMetric} value field.");
    }

    private void ToggleSeriesSource()
    {
        PivotChart.SeriesSource = PivotChart.SeriesSource == PivotChartSeriesSource.Columns
            ? PivotChartSeriesSource.Rows
            : PivotChartSeriesSource.Columns;
        PivotChartModel.Refresh();
        Publish($"Pivot chart series now come from {PivotChart.SeriesSource.ToString().ToLowerInvariant()}.");
    }

    private void Restore()
    {
        _source.Clear();
        _nextId = 9001;
        _nextPeriod = 5;
        _metricIndex = 0;
        AddBaselineRows();
        PivotChart.ValueField = Pivot.ValueFields[0];
        PivotChart.SeriesSource = PivotChartSeriesSource.Columns;
        SelectedMetric = Pivot.ValueFields[0].Header ?? "Revenue";
        RefreshCharts();
        Publish("Restored the deterministic four-period analytics source.");
    }

    private void AddBaselineRows()
    {
        AddPeriodRows("P1", 0.88d);
        AddPeriodRows("P2", 0.96d);
        AddPeriodRows("P3", 1.04d);
        AddPeriodRows("P4", 1.12d);
    }

    private void AddPeriodRows(string period, double factor)
    {
        AddRow(period, "North", "Direct", 128_000d * factor, 31_000d * factor, 82);
        AddRow(period, "South", "Partner", 101_000d * factor, 19_500d * factor, 67);
        AddRow(period, "West", "Direct", 116_000d * factor, 27_000d * factor, 74);
    }

    private void AddRow(string period, string region, string channel, double revenue, double profit, int units)
    {
        _source.Add(new GeneratedPivotChartRow
        {
            Id = _nextId++,
            Period = period,
            Region = region,
            Channel = channel,
            Revenue = Math.Round(revenue, 2),
            Profit = Math.Round(profit, 2),
            Units = units
        });
    }

    private void RefreshCharts()
    {
        Pivot.Refresh();
        PivotChart.Refresh();
        PivotChartModel.Refresh();
        int lastRow = Items.Count - 1;
        int firstRow = Math.Max(0, lastRow - 7);
        RangeChartProjection.UpdateRange(new DataGridCellRange(firstRow, lastRow, 1, 5));
        DirectChartModel.Refresh();
        LongFormChartModel.Refresh();
    }

    private void SelectionModelOnSelectionChanged(object? sender, SelectionModelSelectionChangedEventArgs e)
    {
        if (_synchronizingSelection)
        {
            return;
        }

        _synchronizingSelection = true;
        try
        {
            SelectionController.CaptureFrom(SelectionModel, DataGridGeneratedSelectionOrigin.Model);
        }
        finally
        {
            _synchronizingSelection = false;
        }
    }

    private void SelectionControllerOnSelectionChanged(object? sender, DataGridGeneratedSelectionChangedEventArgs e)
    {
        if (_synchronizingSelection)
        {
            return;
        }

        _synchronizingSelection = true;
        try
        {
            SelectionController.ApplyTo(SelectionModel);
        }
        finally
        {
            _synchronizingSelection = false;
        }
    }

    private static int ToRangeCategoryIndex(int sourceIndex, DataGridCellRange range)
    {
        int categoryIndex = sourceIndex - range.StartRow;
        return categoryIndex >= 0 && categoryIndex < range.RowCount ? categoryIndex : -1;
    }

    private void Publish(string message)
    {
        SourceRowCount = _source.Count;
        PivotRowCount = Pivot.Rows.Count;
        PivotColumnCount = Pivot.ColumnDefinitions.Count;
        DirectSeriesCount = DirectChartSource.Series.Count;
        Status = message;
    }
}
