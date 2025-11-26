// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using Xunit;

namespace Avalonia.Controls.DataGridTests.RowHeightEstimators;

/// <summary>
/// Unit tests for <see cref="CachingRowHeightEstimator"/>.
/// </summary>
public class CachingRowHeightEstimatorTests : RowHeightEstimatorTestBase
{
    protected override IDataGridRowHeightEstimator CreateEstimator()
    {
        return new CachingRowHeightEstimator();
    }

    #region Initial State Tests

    [Fact]
    public void InitialState_HasDefaultValues()
    {
        var estimator = CreateEstimator();
        VerifyInitialState(estimator);
    }

    [Fact]
    public void DefaultRowHeight_CanBeSet()
    {
        var estimator = CreateEstimator();
        estimator.DefaultRowHeight = 30.0;
        Assert.Equal(30.0, estimator.DefaultRowHeight);
    }

    #endregion

    #region Same Height Rows - No Grouping

    [Fact]
    public void SameHeight_NoGrouping_SmallScroll_UpdatesEstimate()
    {
        var estimator = CreateEstimator();
        int visibleRows = 10;
        double rowHeight = DefaultRowHeight;

        // Simulate initial display
        RecordUniformHeights(estimator, 0, visibleRows, rowHeight);
        var heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, 0, visibleRows - 1, heights, 0);

        Assert.Equal(rowHeight, estimator.RowHeightEstimate, Tolerance);
    }

    [Fact]
    public void SameHeight_NoGrouping_CachesIndividualHeights()
    {
        var estimator = CreateEstimator();
        int visibleRows = 10;
        double rowHeight = DefaultRowHeight;

        RecordUniformHeights(estimator, 0, visibleRows, rowHeight);

        // Each slot should return the cached height
        for (int i = 0; i < visibleRows; i++)
        {
            Assert.Equal(rowHeight, estimator.GetEstimatedHeight(i), Tolerance);
        }
    }

    [Fact]
    public void SameHeight_NoGrouping_SmallScrollDown_MaintainsCache()
    {
        var estimator = CreateEstimator();
        int visibleRows = 10;
        double rowHeight = DefaultRowHeight;

        // Initial display
        RecordUniformHeights(estimator, 0, visibleRows, rowHeight);
        var heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, 0, visibleRows - 1, heights, 0);

        // Scroll down by 3 rows
        RecordUniformHeights(estimator, visibleRows, 3, rowHeight);
        heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, 3, visibleRows + 2, heights, rowHeight * 3);

        // Original cached heights should still be accessible
        Assert.Equal(rowHeight, estimator.GetEstimatedHeight(0), Tolerance);
        Assert.Equal(rowHeight, estimator.GetEstimatedHeight(visibleRows), Tolerance);
    }

    [Fact]
    public void SameHeight_NoGrouping_LargeScroll_MaintainsEstimate()
    {
        var estimator = CreateEstimator();
        int visibleRows = 10;
        double rowHeight = DefaultRowHeight;
        int totalRows = 1000;

        // Initial display
        RecordUniformHeights(estimator, 0, visibleRows, rowHeight);
        var heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, 0, visibleRows - 1, heights, 0);

        // Large scroll to middle
        int scrollToSlot = 500;
        RecordUniformHeights(estimator, scrollToSlot, visibleRows, rowHeight);
        heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, scrollToSlot, scrollToSlot + visibleRows - 1, heights, scrollToSlot * rowHeight);

        Assert.Equal(rowHeight, estimator.RowHeightEstimate, Tolerance);
    }

    [Fact]
    public void SameHeight_NoGrouping_ScrollToBottom_CachesNewHeights()
    {
        var estimator = CreateEstimator();
        int visibleRows = 10;
        double rowHeight = DefaultRowHeight;
        int totalRows = 100;

        // Initial display
        RecordUniformHeights(estimator, 0, visibleRows, rowHeight);
        var heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, 0, visibleRows - 1, heights, 0);

        // Scroll to bottom
        int bottomStart = totalRows - visibleRows;
        RecordUniformHeights(estimator, bottomStart, visibleRows, rowHeight);
        heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, bottomStart, totalRows - 1, heights, bottomStart * rowHeight);

        // Bottom rows should be cached
        Assert.Equal(rowHeight, estimator.GetEstimatedHeight(bottomStart), Tolerance);
        Assert.Equal(rowHeight, estimator.GetEstimatedHeight(totalRows - 1), Tolerance);
    }

    [Fact]
    public void SameHeight_NoGrouping_CalculateTotalHeight_UsesCachedValues()
    {
        var estimator = CreateEstimator();
        int visibleRows = 10;
        double rowHeight = DefaultRowHeight;
        int totalRows = 100;

        // Record some heights to establish cache
        RecordUniformHeights(estimator, 0, visibleRows, rowHeight);
        var heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, 0, visibleRows - 1, heights, 0);

        double totalHeight = estimator.CalculateTotalHeight(totalRows, 0, Array.Empty<int>(), 0);
        double expectedHeight = totalRows * rowHeight;

        Assert.Equal(expectedHeight, totalHeight, 1.0);
    }

    [Fact]
    public void SameHeight_NoGrouping_EstimateSlotAtOffset_UsesCachedHeights()
    {
        var estimator = CreateEstimator();
        int visibleRows = 10;
        double rowHeight = DefaultRowHeight;
        int totalRows = 100;

        // Record heights
        RecordUniformHeights(estimator, 0, visibleRows, rowHeight);
        var heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, 0, visibleRows - 1, heights, 0);

        // Test that cached heights are used
        int estimatedSlot = estimator.EstimateSlotAtOffset(rowHeight * 5, totalRows);
        Assert.InRange(estimatedSlot, 4, 6);
    }

    [Fact]
    public void SameHeight_NoGrouping_EstimateOffsetToSlot_UsesCachedHeights()
    {
        var estimator = CreateEstimator();
        int visibleRows = 10;
        double rowHeight = DefaultRowHeight;

        // Record heights
        RecordUniformHeights(estimator, 0, visibleRows, rowHeight);
        var heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, 0, visibleRows - 1, heights, 0);

        // Cached slots should use exact heights
        Assert.Equal(rowHeight * 5, estimator.EstimateOffsetToSlot(5), Tolerance);
    }

    #endregion

    #region Same Height Rows - With Grouping

    [Fact]
    public void SameHeight_WithGrouping_RecordsGroupHeaderHeights()
    {
        var estimator = CreateEstimator();

        // Record group header heights at different levels
        estimator.RecordRowGroupHeaderHeight(0, 0, GroupHeaderHeight);
        estimator.RecordRowGroupHeaderHeight(5, 1, GroupHeaderHeight + 5);

        Assert.Equal(GroupHeaderHeight, estimator.GetRowGroupHeaderHeightEstimate(0), Tolerance);
        Assert.Equal(GroupHeaderHeight + 5, estimator.GetRowGroupHeaderHeightEstimate(1), Tolerance);
    }

    [Fact]
    public void SameHeight_WithGrouping_CalculateTotalHeight_IncludesHeaders()
    {
        var estimator = CreateEstimator();
        int visibleRows = 10;
        double rowHeight = DefaultRowHeight;
        int totalSlots = 110;
        int[] groupHeaderCounts = { 5, 5 };

        RecordUniformHeights(estimator, 0, visibleRows, rowHeight);
        var heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, 0, visibleRows - 1, heights, 0);

        estimator.RecordRowGroupHeaderHeight(0, 0, GroupHeaderHeight);
        estimator.RecordRowGroupHeaderHeight(1, 1, GroupHeaderHeight);

        double totalHeight = estimator.CalculateTotalHeight(totalSlots, 0, groupHeaderCounts, 0);

        Assert.True(totalHeight > 0);
    }

    [Fact]
    public void SameHeight_WithGrouping_CollapsedGroups_ReducesTotalHeight()
    {
        var estimator = CreateEstimator();
        int visibleRows = 10;
        double rowHeight = DefaultRowHeight;
        int totalSlots = 100;
        int collapsedSlots = 20;

        RecordUniformHeights(estimator, 0, visibleRows, rowHeight);
        var heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, 0, visibleRows - 1, heights, 0);

        double fullHeight = estimator.CalculateTotalHeight(totalSlots, 0, Array.Empty<int>(), 0);
        double collapsedHeight = estimator.CalculateTotalHeight(totalSlots, collapsedSlots, Array.Empty<int>(), 0);

        Assert.True(collapsedHeight < fullHeight);
    }

    #endregion

    #region Variable Height Rows - No Grouping

    [Fact]
    public void VariableHeight_NoGrouping_CachesEachHeight()
    {
        var estimator = CreateEstimator();
        int visibleRows = 10;

        // Record alternating heights
        RecordVariableHeights(estimator, 0, visibleRows, AlternatingHeight);

        // Each slot should have its specific cached height
        for (int i = 0; i < visibleRows; i++)
        {
            Assert.Equal(AlternatingHeight(i), estimator.GetEstimatedHeight(i), Tolerance);
        }
    }

    [Fact]
    public void VariableHeight_NoGrouping_ComputesRunningAverage()
    {
        var estimator = CreateEstimator();
        int visibleRows = 10;

        RecordVariableHeights(estimator, 0, visibleRows, AlternatingHeight);
        var heights = CreateVariableHeights(0, visibleRows, AlternatingHeight);
        SimulateScroll(estimator, 0, visibleRows - 1, heights, 0);

        double expectedAverage = (SmallRowHeight + LargeRowHeight) / 2;
        VerifyEstimateAfterMeasurements(estimator, expectedAverage);
    }

    [Fact]
    public void VariableHeight_NoGrouping_UncachedSlots_UseEstimate()
    {
        var estimator = CreateEstimator();
        int visibleRows = 10;

        RecordVariableHeights(estimator, 0, visibleRows, AlternatingHeight);
        var heights = CreateVariableHeights(0, visibleRows, AlternatingHeight);
        SimulateScroll(estimator, 0, visibleRows - 1, heights, 0);

        // Uncached slot should use the estimate
        double uncachedHeight = estimator.GetEstimatedHeight(100);
        Assert.Equal(estimator.RowHeightEstimate, uncachedHeight, Tolerance);
    }

    [Fact]
    public void VariableHeight_NoGrouping_SmallScroll_MergesCachedData()
    {
        var estimator = CreateEstimator();
        int visibleRows = 10;

        // Initial display
        RecordVariableHeights(estimator, 0, visibleRows, AlternatingHeight);

        // Scroll down slightly
        RecordVariableHeights(estimator, visibleRows, 3, AlternatingHeight);

        // All recorded slots should be cached
        for (int i = 0; i < visibleRows + 3; i++)
        {
            Assert.Equal(AlternatingHeight(i), estimator.GetEstimatedHeight(i), Tolerance);
        }
    }

    [Fact]
    public void VariableHeight_NoGrouping_LargeScroll_CachesBothEnds()
    {
        var estimator = CreateEstimator();
        int visibleRows = 10;
        int totalRows = 1000;

        // Initial display
        RecordVariableHeights(estimator, 0, visibleRows, AlternatingHeight);

        // Large scroll
        int scrollToSlot = 500;
        RecordVariableHeights(estimator, scrollToSlot, visibleRows, AlternatingHeight);

        // Both ranges should be cached
        Assert.Equal(AlternatingHeight(0), estimator.GetEstimatedHeight(0), Tolerance);
        Assert.Equal(AlternatingHeight(scrollToSlot), estimator.GetEstimatedHeight(scrollToSlot), Tolerance);
    }

    [Fact]
    public void VariableHeight_NoGrouping_EstimateOffsetToSlot_MixesCachedAndEstimated()
    {
        var estimator = CreateEstimator();
        int visibleRows = 10;

        RecordVariableHeights(estimator, 0, visibleRows, AlternatingHeight);
        var heights = CreateVariableHeights(0, visibleRows, AlternatingHeight);
        SimulateScroll(estimator, 0, visibleRows - 1, heights, 0);

        // For cached slots, should use actual heights
        double offset5 = estimator.EstimateOffsetToSlot(5);
        double expectedOffset = 0;
        for (int i = 0; i < 5; i++)
        {
            expectedOffset += AlternatingHeight(i);
        }
        Assert.Equal(expectedOffset, offset5, Tolerance);
    }

    [Fact]
    public void VariableHeight_NoGrouping_GradualIncrease_CachesAllHeights()
    {
        var estimator = CreateEstimator();
        int visibleRows = 10;

        RecordVariableHeights(estimator, 0, visibleRows, GradualIncreaseHeight);

        // All heights should be cached with their specific values
        for (int i = 0; i < visibleRows; i++)
        {
            Assert.Equal(GradualIncreaseHeight(i), estimator.GetEstimatedHeight(i), Tolerance);
        }
    }

    [Fact]
    public void VariableHeight_NoGrouping_PseudoRandomHeights_TracksMinMax()
    {
        var estimator = CreateEstimator();
        int visibleRows = 20;

        RecordVariableHeights(estimator, 0, visibleRows, PseudoRandomHeight);
        var heights = CreateVariableHeights(0, visibleRows, PseudoRandomHeight);
        SimulateScroll(estimator, 0, visibleRows - 1, heights, 0);

        var diag = estimator.GetDiagnostics();
        Assert.True(diag.MinMeasuredHeight >= SmallRowHeight);
        Assert.True(diag.MaxMeasuredHeight <= LargeRowHeight);
    }

    #endregion

    #region Variable Height Rows - With Grouping

    [Fact]
    public void VariableHeight_WithGrouping_MixedContent()
    {
        var estimator = CreateEstimator();

        estimator.RecordRowGroupHeaderHeight(0, 0, GroupHeaderHeight);
        estimator.RecordMeasuredHeight(1, SmallRowHeight);
        estimator.RecordMeasuredHeight(2, LargeRowHeight);
        estimator.RecordRowGroupHeaderHeight(3, 0, GroupHeaderHeight);

        Assert.Equal(GroupHeaderHeight, estimator.GetEstimatedHeight(0, isRowGroupHeader: true, rowGroupLevel: 0), Tolerance);
        Assert.Equal(SmallRowHeight, estimator.GetEstimatedHeight(1), Tolerance);
        Assert.Equal(LargeRowHeight, estimator.GetEstimatedHeight(2), Tolerance);
    }

    [Fact]
    public void VariableHeight_WithGrouping_NestedGroups()
    {
        var estimator = CreateEstimator();

        estimator.RecordRowGroupHeaderHeight(0, 0, GroupHeaderHeight);
        estimator.RecordRowGroupHeaderHeight(1, 1, GroupHeaderHeight - 5);
        estimator.RecordMeasuredHeight(2, SmallRowHeight);

        Assert.Equal(GroupHeaderHeight, estimator.GetRowGroupHeaderHeightEstimate(0), Tolerance);
        Assert.Equal(GroupHeaderHeight - 5, estimator.GetRowGroupHeaderHeightEstimate(1), Tolerance);
    }

    #endregion

    #region Data Source Changes

    [Fact]
    public void OnDataSourceChanged_ClearsCache()
    {
        var estimator = CreateEstimator();

        RecordUniformHeights(estimator, 0, 10, LargeRowHeight);
        estimator.OnDataSourceChanged(50);

        // Cache should be cleared
        var diag = estimator.GetDiagnostics();
        Assert.Equal(0, diag.CachedHeightCount);
    }

    [Fact]
    public void OnItemsInserted_ShiftsCachedHeights()
    {
        var estimator = CreateEstimator();

        // Record heights for slots 0-9
        for (int i = 0; i < 10; i++)
        {
            estimator.RecordMeasuredHeight(i, DefaultRowHeight + i);
        }

        // Insert 5 items at position 5
        estimator.OnItemsInserted(5, 5);

        // Slots 0-4 should be unchanged
        for (int i = 0; i < 5; i++)
        {
            Assert.Equal(DefaultRowHeight + i, estimator.GetEstimatedHeight(i), Tolerance);
        }

        // Slots 5-9 should now be at 10-14
        for (int i = 5; i < 10; i++)
        {
            Assert.Equal(DefaultRowHeight + i, estimator.GetEstimatedHeight(i + 5), Tolerance);
        }
    }

    [Fact]
    public void OnItemsRemoved_ShiftsCachedHeights()
    {
        var estimator = CreateEstimator();

        // Record heights for slots 0-14
        for (int i = 0; i < 15; i++)
        {
            estimator.RecordMeasuredHeight(i, DefaultRowHeight + i);
        }

        // Remove 5 items starting at position 5
        estimator.OnItemsRemoved(5, 5);

        // Slots 0-4 should be unchanged
        for (int i = 0; i < 5; i++)
        {
            Assert.Equal(DefaultRowHeight + i, estimator.GetEstimatedHeight(i), Tolerance);
        }

        // Slots 10-14 should now be at 5-9
        for (int i = 10; i < 15; i++)
        {
            Assert.Equal(DefaultRowHeight + i, estimator.GetEstimatedHeight(i - 5), Tolerance);
        }
    }

    [Fact]
    public void Reset_ClearsCache()
    {
        var estimator = CreateEstimator();

        RecordUniformHeights(estimator, 0, 10, LargeRowHeight);
        estimator.Reset();

        var diag = estimator.GetDiagnostics();
        Assert.Equal(0, diag.CachedHeightCount);
        Assert.Equal(DefaultRowHeight, estimator.RowHeightEstimate, Tolerance);
    }

    #endregion

    #region Row Details

    [Fact]
    public void RowDetails_CachesDetailsHeight()
    {
        var estimator = CreateEstimator();

        estimator.RecordMeasuredHeight(0, DefaultRowHeight, true, DetailsHeight);

        Assert.Equal(DetailsHeight, estimator.RowDetailsHeightEstimate, Tolerance);
    }

    [Fact]
    public void RowDetails_GetEstimatedHeight_IncludesDetails()
    {
        var estimator = CreateEstimator();

        estimator.RecordMeasuredHeight(0, DefaultRowHeight, true, DetailsHeight);

        double heightWithDetails = estimator.GetEstimatedHeight(0, hasDetails: true);
        double heightWithoutDetails = estimator.GetEstimatedHeight(0, hasDetails: false);

        Assert.Equal(heightWithoutDetails + DetailsHeight, heightWithDetails, Tolerance);
    }

    #endregion

    #region Diagnostics

    [Fact]
    public void GetDiagnostics_ReportsCacheSize()
    {
        var estimator = CreateEstimator();

        RecordUniformHeights(estimator, 0, 10, DefaultRowHeight);

        var diag = estimator.GetDiagnostics();

        Assert.Equal(10, diag.CachedHeightCount);
        Assert.Contains("Caching", diag.AlgorithmName);
    }

    [Fact]
    public void GetDiagnostics_ReportsStatistics()
    {
        var estimator = CreateEstimator();

        RecordVariableHeights(estimator, 0, 10, AlternatingHeight);
        var heights = CreateVariableHeights(0, 10, AlternatingHeight);
        SimulateScroll(estimator, 0, 9, heights, 0);

        var diag = estimator.GetDiagnostics();

        Assert.Equal(SmallRowHeight, diag.MinMeasuredHeight, Tolerance);
        Assert.Equal(LargeRowHeight, diag.MaxMeasuredHeight, Tolerance);
    }

    #endregion

    #region Bidirectional Scrolling Tests

    [Fact]
    public void ScrollDown_SmallIncrement_CachesNewRows()
    {
        var estimator = CreateEstimator();
        int visibleRows = 10;
        double rowHeight = DefaultRowHeight;

        // Start at top
        RecordUniformHeights(estimator, 0, visibleRows, rowHeight);
        var heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, 0, visibleRows - 1, heights, 0);

        var initialDiag = estimator.GetDiagnostics();
        int initialCacheCount = initialDiag.CachedHeightCount;

        // Scroll down by 5 rows
        RecordUniformHeights(estimator, visibleRows, 5, rowHeight);
        heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, 5, 14, heights, 5 * rowHeight);

        var afterDiag = estimator.GetDiagnostics();
        Assert.True(afterDiag.CachedHeightCount >= initialCacheCount, 
            "Cache should grow when scrolling to new rows");
    }

    [Fact]
    public void ScrollUp_SmallIncrement_CachesNewRows()
    {
        var estimator = CreateEstimator();
        int visibleRows = 10;
        double rowHeight = DefaultRowHeight;

        // Start in middle
        RecordUniformHeights(estimator, 50, visibleRows, rowHeight);
        var heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, 50, 59, heights, 50 * rowHeight);

        // Scroll up
        RecordUniformHeights(estimator, 45, 5, rowHeight); // New rows coming into view
        heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, 45, 54, heights, 45 * rowHeight);

        Assert.Equal(rowHeight, estimator.RowHeightEstimate, Tolerance);
    }

    [Fact]
    public void ScrollDown_ThenUp_MaintainsCache()
    {
        var estimator = CreateEstimator();
        int visibleRows = 10;
        double rowHeight = DefaultRowHeight;

        // Start at top
        RecordUniformHeights(estimator, 0, visibleRows, rowHeight);
        var heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, 0, visibleRows - 1, heights, 0);

        // Scroll down
        RecordUniformHeights(estimator, 20, visibleRows, rowHeight);
        heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, 20, 29, heights, 20 * rowHeight);

        // Scroll back up
        RecordUniformHeights(estimator, 10, visibleRows, rowHeight);
        heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, 10, 19, heights, 10 * rowHeight);

        Assert.Equal(rowHeight, estimator.RowHeightEstimate, Tolerance);
    }

    [Fact]
    public void ScrollUp_ThenDown_MaintainsCache()
    {
        var estimator = CreateEstimator();
        int visibleRows = 10;
        double rowHeight = DefaultRowHeight;

        // Start in middle
        RecordUniformHeights(estimator, 50, visibleRows, rowHeight);
        var heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, 50, 59, heights, 50 * rowHeight);

        // Scroll up
        RecordUniformHeights(estimator, 30, visibleRows, rowHeight);
        heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, 30, 39, heights, 30 * rowHeight);

        // Scroll back down
        RecordUniformHeights(estimator, 40, visibleRows, rowHeight);
        heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, 40, 49, heights, 40 * rowHeight);

        Assert.Equal(rowHeight, estimator.RowHeightEstimate, Tolerance);
    }

    [Fact]
    public void VariableHeight_ScrollDown_UpdatesCacheWithNewHeights()
    {
        var estimator = CreateEstimator();
        int visibleRows = 10;

        // Top region with small rows
        RecordUniformHeights(estimator, 0, visibleRows, SmallRowHeight);
        var heights = CreateUniformHeights(visibleRows, SmallRowHeight);
        SimulateScroll(estimator, 0, visibleRows - 1, heights, 0);

        double initialEstimate = estimator.RowHeightEstimate;

        // Scroll to region with large rows
        RecordUniformHeights(estimator, 50, visibleRows, LargeRowHeight);
        heights = CreateUniformHeights(visibleRows, LargeRowHeight);
        SimulateScroll(estimator, 50, 59, heights, 50 * SmallRowHeight);

        // Estimate should adapt
        Assert.True(estimator.RowHeightEstimate > initialEstimate,
            "Estimate should increase after seeing larger rows");
    }

    [Fact]
    public void VariableHeight_ScrollUp_UpdatesCacheWithNewHeights()
    {
        var estimator = CreateEstimator();
        int visibleRows = 10;

        // Bottom region with large rows
        RecordUniformHeights(estimator, 90, visibleRows, LargeRowHeight);
        var heights = CreateUniformHeights(visibleRows, LargeRowHeight);
        SimulateScroll(estimator, 90, 99, heights, 90 * LargeRowHeight);

        double initialEstimate = estimator.RowHeightEstimate;

        // Scroll up to region with small rows
        RecordUniformHeights(estimator, 0, visibleRows, SmallRowHeight);
        heights = CreateUniformHeights(visibleRows, SmallRowHeight);
        SimulateScroll(estimator, 0, 9, heights, 0);

        // Estimate should adapt
        Assert.True(estimator.RowHeightEstimate < initialEstimate,
            "Estimate should decrease after seeing smaller rows");
    }

    #endregion

    #region Multi-Region Scrolling Tests

    [Fact]
    public void MultiRegion_TopMiddleBottom_CachesAllRegions()
    {
        var estimator = CreateEstimator();
        int visibleRows = 10;
        double rowHeight = DefaultRowHeight;
        int totalRows = 500;

        // Sample top
        RecordUniformHeights(estimator, 0, visibleRows, rowHeight);
        var heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, 0, visibleRows - 1, heights, 0);

        // Sample middle
        int middle = 250;
        RecordUniformHeights(estimator, middle, visibleRows, rowHeight);
        heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, middle, middle + visibleRows - 1, heights, middle * rowHeight);

        // Sample bottom
        int bottom = totalRows - visibleRows;
        RecordUniformHeights(estimator, bottom, visibleRows, rowHeight);
        heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, bottom, totalRows - 1, heights, bottom * rowHeight);

        var diag = estimator.GetDiagnostics();
        Assert.Equal(30, diag.CachedHeightCount); // 3 regions * 10 rows each
    }

    [Fact]
    public void MultiRegion_DifferentHeights_CalculatesCorrectAverage()
    {
        var estimator = CreateEstimator();
        int visibleRows = 10;

        // Region 1: small rows
        RecordUniformHeights(estimator, 0, visibleRows, SmallRowHeight);
        var heights = CreateUniformHeights(visibleRows, SmallRowHeight);
        SimulateScroll(estimator, 0, visibleRows - 1, heights, 0);

        // Region 2: large rows
        RecordUniformHeights(estimator, 50, visibleRows, LargeRowHeight);
        heights = CreateUniformHeights(visibleRows, LargeRowHeight);
        SimulateScroll(estimator, 50, 59, heights, 50 * SmallRowHeight);

        var diag = estimator.GetDiagnostics();
        double expectedAvg = (SmallRowHeight + LargeRowHeight) / 2;
        Assert.InRange(diag.AverageMeasuredHeight, SmallRowHeight, LargeRowHeight);
    }

    [Fact]
    public void MultiRegion_RandomJumps_CachesAllVisitedRows()
    {
        var estimator = CreateEstimator();
        int visibleRows = 10;
        double rowHeight = DefaultRowHeight;

        int[] positions = { 0, 100, 50, 200, 25, 150 };

        foreach (int pos in positions)
        {
            RecordUniformHeights(estimator, pos, visibleRows, rowHeight);
            var heights = CreateUniformHeights(visibleRows, rowHeight);
            SimulateScroll(estimator, pos, pos + visibleRows - 1, heights, pos * rowHeight);
        }

        var diag = estimator.GetDiagnostics();
        Assert.Equal(positions.Length * visibleRows, diag.CachedHeightCount);
    }

    #endregion
}
