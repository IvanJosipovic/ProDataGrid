// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Controls.DataGridPivoting;
using Avalonia.Utilities;
using ProCharts;

namespace ProDataGrid.Charting
{
    public sealed class PivotChartDataSource : IChartIncrementalDataSource, IChartWindowInfoProvider, INotifyPropertyChanged, IDisposable
    {
        private PivotChartModel? _pivotChart;
        private ChartSeriesKind _seriesKind = ChartSeriesKind.Column;
        private Func<PivotChartSeries, ChartSeriesKind>? _seriesKindSelector;
        private Func<PivotChartSeries, ChartValueAxisAssignment>? _valueAxisAssignmentSelector;
        private Func<PivotChartSeries, ChartSeriesStyle?>? _seriesStyleSelector;

        public event PropertyChangedEventHandler? PropertyChanged;

        public event EventHandler? DataInvalidated;

        public PivotChartModel? PivotChart
        {
            get => _pivotChart;
            set
            {
                if (ReferenceEquals(_pivotChart, value))
                {
                    return;
                }

                DetachPivot();
                _pivotChart = value;
                AttachPivot();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PivotChart)));
                RequestRefresh();
            }
        }

        public ChartSeriesKind SeriesKind
        {
            get => _seriesKind;
            set
            {
                if (_seriesKind == value)
                {
                    return;
                }

                _seriesKind = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SeriesKind)));
                RequestRefresh();
            }
        }

        public Func<PivotChartSeries, ChartSeriesKind>? SeriesKindSelector
        {
            get => _seriesKindSelector;
            set
            {
                if (_seriesKindSelector == value)
                {
                    return;
                }

                _seriesKindSelector = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SeriesKindSelector)));
                RequestRefresh();
            }
        }

        public Func<PivotChartSeries, ChartValueAxisAssignment>? ValueAxisAssignmentSelector
        {
            get => _valueAxisAssignmentSelector;
            set
            {
                if (_valueAxisAssignmentSelector == value)
                {
                    return;
                }

                _valueAxisAssignmentSelector = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ValueAxisAssignmentSelector)));
                RequestRefresh();
            }
        }

        public Func<PivotChartSeries, ChartSeriesStyle?>? SeriesStyleSelector
        {
            get => _seriesStyleSelector;
            set
            {
                if (_seriesStyleSelector == value)
                {
                    return;
                }

                _seriesStyleSelector = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SeriesStyleSelector)));
                RequestRefresh();
            }
        }

        public ChartDataSnapshot BuildSnapshot(ChartDataRequest request)
        {
            if (_pivotChart == null)
            {
                return ChartDataSnapshot.Empty;
            }

            var totalCategories = _pivotChart.Categories.Count;
            var windowStart = request?.WindowStart ?? 0;
            var windowCount = request?.WindowCount ?? totalCategories;
            NormalizeWindow(totalCategories, ref windowStart, ref windowCount);

            var categories = new List<string?>(windowCount);
            if (windowCount > 0)
            {
                for (var i = 0; i < windowCount; i++)
                {
                    var sourceIndex = windowStart + i;
                    if (sourceIndex < 0 || sourceIndex >= totalCategories)
                    {
                        break;
                    }

                    categories.Add(_pivotChart.Categories[sourceIndex]);
                }
            }

            var seriesSnapshots = new List<ChartSeriesSnapshot>(_pivotChart.Series.Count);

            foreach (var series in _pivotChart.Series)
            {
                var values = new List<double?>(windowCount);
                if (windowCount > 0)
                {
                    var end = Math.Min(series.Values.Count, windowStart + windowCount);
                    for (var i = windowStart; i < end; i++)
                    {
                        values.Add(series.Values[i]);
                    }
                }

                var kind = _seriesKindSelector?.Invoke(series) ?? _seriesKind;
                var axis = _valueAxisAssignmentSelector?.Invoke(series) ?? ChartValueAxisAssignment.Primary;
                var style = _seriesStyleSelector?.Invoke(series);

                seriesSnapshots.Add(new ChartSeriesSnapshot(
                    series.Name,
                    kind,
                    values,
                    null,
                    null,
                    null,
                    axis,
                    ChartTrendlineType.None,
                    2,
                    ChartErrorBarType.None,
                    1d,
                    style));
            }

            return new ChartDataSnapshot(categories, seriesSnapshots);
        }

        public bool TryBuildUpdate(ChartDataRequest request, ChartDataSnapshot previousSnapshot, out ChartDataUpdate update)
        {
            var snapshot = BuildSnapshot(request);
            var delta = BuildDelta(previousSnapshot, snapshot);
            update = new ChartDataUpdate(snapshot, delta);
            return true;
        }

        public int? GetTotalCategoryCount()
        {
            return _pivotChart?.Categories.Count;
        }

        public void Dispose()
        {
            DetachPivot();
        }

        private void AttachPivot()
        {
            if (_pivotChart == null)
            {
                return;
            }

            WeakEventHandlerManager.Subscribe<PivotChartModel, PivotChartChangedEventArgs, PivotChartDataSource>(
                _pivotChart,
                nameof(PivotChartModel.ChartChanged),
                PivotChartOnChartChanged);
        }

        private void DetachPivot()
        {
            if (_pivotChart == null)
            {
                return;
            }

            WeakEventHandlerManager.Unsubscribe<PivotChartChangedEventArgs, PivotChartDataSource>(
                _pivotChart,
                nameof(PivotChartModel.ChartChanged),
                PivotChartOnChartChanged);
        }

        private void PivotChartOnChartChanged(object? sender, PivotChartChangedEventArgs e)
        {
            RequestRefresh();
        }

        private void RequestRefresh()
        {
            DataInvalidated?.Invoke(this, EventArgs.Empty);
        }

        private static void NormalizeWindow(int total, ref int windowStart, ref int windowCount)
        {
            if (total <= 0)
            {
                windowStart = 0;
                windowCount = 0;
                return;
            }

            if (windowStart < 0)
            {
                windowStart = 0;
            }

            if (windowStart > total)
            {
                windowStart = total;
            }

            if (windowCount < 0)
            {
                windowCount = 0;
            }

            if (windowCount > total)
            {
                windowCount = total;
            }

            if (windowStart + windowCount > total)
            {
                windowCount = Math.Max(0, total - windowStart);
            }
        }

        private static ChartDataDelta BuildDelta(ChartDataSnapshot previous, ChartDataSnapshot current)
        {
            if (previous == null)
            {
                return ChartDataDelta.Full;
            }

            if (!AreCategoriesEqual(previous.Categories, current.Categories))
            {
                return ChartDataDelta.Full;
            }

            if (previous.Series.Count != current.Series.Count)
            {
                return ChartDataDelta.Full;
            }

            var changedSeries = new List<int>();
            for (var i = 0; i < current.Series.Count; i++)
            {
                var previousSeries = previous.Series[i];
                var currentSeries = current.Series[i];
                if (!AreSeriesMetadataEqual(previousSeries, currentSeries))
                {
                    return ChartDataDelta.Full;
                }

                if (!AreValuesEqual(previousSeries.Values, currentSeries.Values) ||
                    !AreOptionalValuesEqual(previousSeries.XValues, currentSeries.XValues) ||
                    !AreOptionalValuesEqual(previousSeries.SizeValues, currentSeries.SizeValues))
                {
                    changedSeries.Add(i);
                }
            }

            if (changedSeries.Count == 0)
            {
                return ChartDataDelta.None;
            }

            return new ChartDataDelta(
                ChartDataDeltaKind.Update,
                0,
                0,
                current.Categories.Count,
                changedSeries);
        }

        private static bool AreCategoriesEqual(IReadOnlyList<string?> left, IReadOnlyList<string?> right)
        {
            if (left.Count != right.Count)
            {
                return false;
            }

            for (var i = 0; i < left.Count; i++)
            {
                if (!string.Equals(left[i], right[i], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AreSeriesMetadataEqual(ChartSeriesSnapshot left, ChartSeriesSnapshot right)
        {
            if (!string.Equals(left.Name, right.Name, StringComparison.Ordinal))
            {
                return false;
            }

            if (left.Kind != right.Kind ||
                left.ValueAxisAssignment != right.ValueAxisAssignment ||
                left.TrendlineType != right.TrendlineType ||
                left.TrendlinePeriod != right.TrendlinePeriod ||
                left.ErrorBarType != right.ErrorBarType)
            {
                return false;
            }

            if (Math.Abs(left.ErrorBarValue - right.ErrorBarValue) > double.Epsilon)
            {
                return false;
            }

            if (!ReferenceEquals(left.Style, right.Style))
            {
                return false;
            }

            if (!ReferenceEquals(left.DataLabelFormatter, right.DataLabelFormatter))
            {
                return false;
            }

            return true;
        }

        private static bool AreValuesEqual(IReadOnlyList<double?> left, IReadOnlyList<double?> right)
        {
            if (left.Count != right.Count)
            {
                return false;
            }

            for (var i = 0; i < left.Count; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AreOptionalValuesEqual(IReadOnlyList<double>? left, IReadOnlyList<double>? right)
        {
            if (left == null || right == null)
            {
                return left == right;
            }

            if (left.Count != right.Count)
            {
                return false;
            }

            for (var i = 0; i < left.Count; i++)
            {
                if (!left[i].Equals(right[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AreOptionalValuesEqual(IReadOnlyList<double?>? left, IReadOnlyList<double?>? right)
        {
            if (left == null || right == null)
            {
                return left == right;
            }

            if (left.Count != right.Count)
            {
                return false;
            }

            for (var i = 0; i < left.Count; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
