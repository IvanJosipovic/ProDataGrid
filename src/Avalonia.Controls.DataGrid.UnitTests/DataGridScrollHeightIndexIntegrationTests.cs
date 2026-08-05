using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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