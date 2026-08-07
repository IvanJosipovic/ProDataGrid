using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.DataGridTests;

public sealed class DataGridScrollHeightIndexIntegrationTests
{
    [AvaloniaFact]
    public void LargeLogicalScroll_DoesNotPerformLinearExactHeightLookups()
    {
        var items = new List<ScrollTestItem>(5000);
        for (int index = 0; index < 5000; index++)
        {
            items.Add(new ScrollTestItem($"Item {index}"));
        }

        using var lookupCounter = new ExactHeightLookupCounter();
        var root = new Window
        {
            Width = 320,
            Height = 240,
        };
        root.SetThemeStyles();

        var grid = new DataGrid
        {
            ItemsSource = items,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            UseLogicalScrollable = true,
        };
        grid.ColumnsInternal.Add(new DataGridTextColumn
        {
            Header = "Name",
            Binding = new Binding(nameof(ScrollTestItem.Name)),
        });
        root.Content = grid;

        try
        {
            root.Show();
            root.UpdateLayout();

            var presenter = grid.GetSelfAndVisualDescendants()
                .OfType<DataGridRowsPresenter>()
                .Single();
            long lookupsBeforeScroll = lookupCounter.Lookups;

            presenter.Offset = new Vector(0, 50000);
            root.UpdateLayout();

            long lookups = lookupCounter.Lookups - lookupsBeforeScroll;
            Assert.Equal(0, lookups);
            Assert.True(grid.DisplayData.FirstScrollingSlot > 1000);
            Assert.Contains(grid.GetSelfAndVisualDescendants().OfType<DataGridRow>(), row => row.IsVisible);
        }
        finally
        {
            root.Close();
        }
    }

    [AvaloniaFact]
    public void DirectScrollIntoView_DoesNotTraverseEveryCrossedSlot()
    {
        var items = new List<ScrollTestItem>(5000);
        for (int index = 0; index < 5000; index++)
        {
            items.Add(new ScrollTestItem($"Item {index}"));
        }

        var estimator = new CountingRowHeightEstimator();
        var root = new Window
        {
            Width = 320,
            Height = 240,
        };
        root.SetThemeStyles();

        var grid = new DataGrid
        {
            ItemsSource = items,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            RowHeightEstimator = estimator,
            UseLogicalScrollable = true,
        };
        grid.ColumnsInternal.Add(new DataGridTextColumn
        {
            Header = "Name",
            Binding = new Binding(nameof(ScrollTestItem.Name)),
        });
        root.Content = grid;

        try
        {
            root.Show();
            root.UpdateLayout();

            Assert.True(grid.ScrollSlotIntoView(1000, scrolledHorizontally: false));
            root.UpdateLayout();

            estimator.ResetCount();
            Assert.True(grid.ScrollSlotIntoView(4000, scrolledHorizontally: false));
            root.UpdateLayout();

            Assert.InRange(estimator.EstimatedHeightCalls, 0, 128);
            Assert.True(grid.DisplayData.FirstScrollingSlot <= 4000);
            Assert.True(grid.DisplayData.LastScrollingSlot >= 4000);
            Assert.Contains(grid.GetSelfAndVisualDescendants().OfType<DataGridRow>(), row => row.IsVisible && row.Index == 4000);
        }
        finally
        {
            root.Close();
        }
    }

    [AvaloniaFact]
    public void ModerateJumpOnLargeGridDoesNotBuildEveryIndexedSlot()
    {
        var item = new ScrollTestItem("Item");
        var items = Enumerable.Repeat(item, 100_000).ToArray();
        var estimator = new CountingRowHeightEstimator();
        var root = CreateRoot();
        var grid = CreateGrid(items, estimator);
        root.Content = grid;

        try
        {
            root.Show();
            root.UpdateLayout();
            var presenter = GetRowsPresenter(grid);
            estimator.ResetCount();

            presenter.Offset = new Vector(0, 1_500);
            root.UpdateLayout();

            Assert.InRange(estimator.EstimatedHeightCalls, 0, 5_000);
            Assert.InRange(grid.DisplayData.FirstScrollingSlot, 1, 1_000);
        }
        finally
        {
            root.Close();
        }
    }

    [AvaloniaFact]
    public void EstimatorWideHeightChangeInvalidatesIndexedGeometry()
    {
        var item = new ScrollTestItem("Item");
        var items = Enumerable.Repeat(item, 5_000).ToArray();
        var estimator = new SwitchingRowHeightEstimator();
        var root = CreateRoot();
        var grid = CreateGrid(items, estimator);
        root.Content = grid;

        try
        {
            root.Show();
            root.UpdateLayout();
            var presenter = GetRowsPresenter(grid);

            presenter.Offset = new Vector(0, 50_000);
            root.UpdateLayout();
            Assert.Equal(100, estimator.RowHeightEstimate);

            presenter.Offset = new Vector(0, 200_000);
            root.UpdateLayout();

            Assert.True(
                grid.DisplayData.FirstScrollingSlot is >= 1_750 and <= 2_250,
                $"Expected the refreshed index to target the middle range, but got slot {grid.DisplayData.FirstScrollingSlot}; " +
                $"extent {presenter.Extent.Height:F1}; offset {presenter.Offset.Y:F1}; estimate {estimator.RowHeightEstimate:F1}.");
        }
        finally
        {
            root.Close();
        }
    }

    [AvaloniaFact]
    public void GlobalDetailsVisibilityRebuildsIndexedGeometry()
    {
        var item = new ScrollTestItem("Item");
        var items = Enumerable.Repeat(item, 5_000).ToArray();
        var root = CreateRoot();
        var grid = CreateGrid(items, new AdvancedRowHeightEstimator());
        grid.RowDetailsTemplate = new FuncDataTemplate<ScrollTestItem>(
            static (_, _) => new Border { Height = 60 });
        grid.RowDetailsVisibilityMode = DataGridRowDetailsVisibilityMode.Collapsed;
        root.Content = grid;

        try
        {
            root.Show();
            root.UpdateLayout();
            var presenter = GetRowsPresenter(grid);

            presenter.Offset = new Vector(0, 50_000);
            root.UpdateLayout();

            grid.RowDetailsVisibilityMode = DataGridRowDetailsVisibilityMode.Visible;
            root.UpdateLayout();
            presenter.Offset = new Vector(0, presenter.Extent.Height * 0.5);
            root.UpdateLayout();

            Assert.InRange(grid.DisplayData.FirstScrollingSlot, 1_500, 3_500);
        }
        finally
        {
            root.Close();
        }
    }

    private static Window CreateRoot()
    {
        var root = new Window
        {
            Width = 320,
            Height = 240,
        };
        root.SetThemeStyles();
        return root;
    }

    private static DataGrid CreateGrid(IEnumerable<ScrollTestItem> items, IDataGridRowHeightEstimator estimator)
    {
        var grid = new DataGrid
        {
            ItemsSource = items,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            RowHeightEstimator = estimator,
            UseLogicalScrollable = true,
        };
        grid.ColumnsInternal.Add(new DataGridTextColumn
        {
            Header = "Name",
            Binding = new Binding(nameof(ScrollTestItem.Name)),
        });
        return grid;
    }

    private static DataGridRowsPresenter GetRowsPresenter(DataGrid grid) =>
        grid.GetSelfAndVisualDescendants().OfType<DataGridRowsPresenter>().Single();

    private sealed class ScrollTestItem
    {
        public ScrollTestItem(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }

    private sealed class CountingRowHeightEstimator : IDataGridRowHeightEstimator
    {
        public double DefaultRowHeight { get; set; } = 22;

        public double RowHeightEstimate => DefaultRowHeight;

        public double RowDetailsHeightEstimate => 0;

        public int EstimatedHeightCalls { get; private set; }

        public double GetRowGroupHeaderHeightEstimate(int level) => DefaultRowHeight;

        public void RecordMeasuredHeight(int slot, double measuredHeight, bool hasDetails = false, double detailsHeight = 0)
        {
        }

        public void RecordRowGroupHeaderHeight(int slot, int level, double measuredHeight)
        {
        }

        public double GetEstimatedHeight(int slot, bool isRowGroupHeader = false, int rowGroupLevel = 0, bool hasDetails = false)
        {
            EstimatedHeightCalls++;
            return DefaultRowHeight;
        }

        public double CalculateTotalHeight(int totalSlotCount, int collapsedSlotCount, int[] rowGroupHeaderCounts, int detailsVisibleCount)
        {
            return Math.Max(0, totalSlotCount - collapsedSlotCount) * DefaultRowHeight;
        }

        public int EstimateSlotAtOffset(double verticalOffset, int totalSlotCount)
        {
            return totalSlotCount <= 0 ? 0 : Math.Clamp((int)(verticalOffset / DefaultRowHeight), 0, totalSlotCount - 1);
        }

        public double EstimateOffsetToSlot(int slot) => Math.Max(0, slot) * DefaultRowHeight;

        public void UpdateFromDisplayedRows(int firstDisplayedSlot, int lastDisplayedSlot, double[] displayedHeights, double verticalOffset, double negVerticalOffset, int collapsedSlotCount, int detailsCount)
        {
        }

        public void Reset()
        {
            ResetCount();
        }

        public void OnDataSourceChanged(int newItemCount)
        {
        }

        public void OnItemsInserted(int startIndex, int count)
        {
        }

        public void OnItemsRemoved(int startIndex, int count)
        {
        }

        public RowHeightEstimatorDiagnostics GetDiagnostics()
        {
            return new RowHeightEstimatorDiagnostics
            {
                AlgorithmName = nameof(CountingRowHeightEstimator),
                CurrentRowHeightEstimate = DefaultRowHeight,
                TotalRowCount = 5000,
                EstimatedTotalHeight = 5000 * DefaultRowHeight,
                MinMeasuredHeight = DefaultRowHeight,
                MaxMeasuredHeight = DefaultRowHeight,
                AverageMeasuredHeight = DefaultRowHeight,
            };
        }

        public void ResetCount()
        {
            EstimatedHeightCalls = 0;
        }
    }

    private sealed class SwitchingRowHeightEstimator : IDataGridRowHeightEstimator
    {
        private double _rowHeightEstimate = 20;

        public double DefaultRowHeight { get; set; } = 20;

        public double RowHeightEstimate => _rowHeightEstimate;

        public double RowDetailsHeightEstimate => 0;

        public double GetRowGroupHeaderHeightEstimate(int level) => _rowHeightEstimate;

        public void RecordMeasuredHeight(int slot, double measuredHeight, bool hasDetails = false, double detailsHeight = 0)
        {
            if (slot >= 1_000)
            {
                _rowHeightEstimate = 100;
            }
        }

        public void RecordRowGroupHeaderHeight(int slot, int level, double measuredHeight)
        {
        }

        public double GetEstimatedHeight(int slot, bool isRowGroupHeader = false, int rowGroupLevel = 0, bool hasDetails = false) =>
            _rowHeightEstimate;

        public double CalculateTotalHeight(int totalSlotCount, int collapsedSlotCount, int[] rowGroupHeaderCounts, int detailsVisibleCount) =>
            Math.Max(0, totalSlotCount - collapsedSlotCount) * _rowHeightEstimate;

        public int EstimateSlotAtOffset(double verticalOffset, int totalSlotCount) =>
            totalSlotCount <= 0
                ? 0
                : Math.Clamp((int)(verticalOffset / _rowHeightEstimate), 0, totalSlotCount - 1);

        public double EstimateOffsetToSlot(int slot) => Math.Max(0, slot) * _rowHeightEstimate;

        public void UpdateFromDisplayedRows(int firstDisplayedSlot, int lastDisplayedSlot, double[] displayedHeights, double verticalOffset, double negVerticalOffset, int collapsedSlotCount, int detailsCount)
        {
        }

        public void Reset() => _rowHeightEstimate = DefaultRowHeight;

        public void OnDataSourceChanged(int newItemCount)
        {
        }

        public void OnItemsInserted(int startIndex, int count)
        {
        }

        public void OnItemsRemoved(int startIndex, int count)
        {
        }

        public RowHeightEstimatorDiagnostics GetDiagnostics() => new()
        {
            AlgorithmName = nameof(SwitchingRowHeightEstimator),
            CurrentRowHeightEstimate = _rowHeightEstimate,
        };
    }

    private sealed class ExactHeightLookupCounter : IDisposable
    {
        private readonly MeterListener _listener;
        private long _lookups;

        public ExactHeightLookupCounter()
        {
            _listener = new MeterListener
            {
                InstrumentPublished = (instrument, listener) =>
                {
                    if (instrument.Meter.Name == DataGridDiagnostics.MeterName &&
                        instrument.Name == DataGridDiagnostics.Meters.RowsScrollExactSlotHeightLookupCountName)
                    {
                        listener.EnableMeasurementEvents(instrument);
                    }
                },
            };
            _listener.SetMeasurementEventCallback<long>((instrument, measurement, _, _) =>
            {
                _lookups += measurement;
            });
            _listener.Start();
        }

        public long Lookups => _lookups;

        public void Dispose()
        {
            _listener.Dispose();
        }
    }
}
