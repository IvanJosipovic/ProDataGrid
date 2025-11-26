// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using Xunit;

namespace Avalonia.Controls.DataGridTests.RowHeightEstimators;

/// <summary>
/// Unit tests for <see cref="DefaultRowHeightEstimator"/>.
/// </summary>
public class DefaultRowHeightEstimatorTests : RowHeightEstimatorTestBase
{
    protected override IDataGridRowHeightEstimator CreateEstimator()
    {
        return new DefaultRowHeightEstimator();
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
        const int visibleRows = 10;
        const double rowHeight = DefaultRowHeight;

        // Simulate initial display
        RecordUniformHeights(estimator, 0, visibleRows, rowHeight);
        var heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, 0, visibleRows - 1, heights, 0);

        Assert.Equal(rowHeight, estimator.RowHeightEstimate, Tolerance);
    }

    [Fact]
    public void SameHeight_NoGrouping_SmallScrollDown_MaintainsEstimate()
    {
        var estimator = CreateEstimator();
        const int visibleRows = 10;
        const double rowHeight = DefaultRowHeight;

        // Initial display
        RecordUniformHeights(estimator, 0, visibleRows, rowHeight);
        var heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, 0, visibleRows - 1, heights, 0);

        // Scroll down by 3 rows
        RecordUniformHeights(estimator, visibleRows, 3, rowHeight);
        heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, 3, visibleRows + 2, heights, rowHeight * 3);

        Assert.Equal(rowHeight, estimator.RowHeightEstimate, Tolerance);
    }

    [Fact]
    public void SameHeight_NoGrouping_LargeScroll_UpdatesEstimate()
    {
        var estimator = CreateEstimator();
        const int visibleRows = 10;
        const double rowHeight = DefaultRowHeight;
        const int totalRows = 1000;

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
    public void SameHeight_NoGrouping_ScrollToBottom_MaintainsEstimate()
    {
        var estimator = CreateEstimator();
        const int visibleRows = 10;
        const double rowHeight = DefaultRowHeight;
        const int totalRows = 100;

        // Initial display
        RecordUniformHeights(estimator, 0, visibleRows, rowHeight);
        var heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, 0, visibleRows - 1, heights, 0);

        // Scroll to bottom
        int bottomStart = totalRows - visibleRows;
        RecordUniformHeights(estimator, bottomStart, visibleRows, rowHeight);
        heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, bottomStart, totalRows - 1, heights, bottomStart * rowHeight);

        Assert.Equal(rowHeight, estimator.RowHeightEstimate, Tolerance);
    }

    [Fact]
    public void SameHeight_NoGrouping_ScrollToTop_AfterBottom()
    {
        var estimator = CreateEstimator();
        const int visibleRows = 10;
        const double rowHeight = DefaultRowHeight;
        const int totalRows = 100;

        // Initial display
        RecordUniformHeights(estimator, 0, visibleRows, rowHeight);
        var heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, 0, visibleRows - 1, heights, 0);

        // Scroll to bottom
        int bottomStart = totalRows - visibleRows;
        RecordUniformHeights(estimator, bottomStart, visibleRows, rowHeight);
        heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, bottomStart, totalRows - 1, heights, bottomStart * rowHeight);

        // Scroll back to top
        heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, 0, visibleRows - 1, heights, 0);

        // Estimate should still be correct
        Assert.Equal(rowHeight, estimator.RowHeightEstimate, Tolerance);
    }

    [Fact]
    public void SameHeight_NoGrouping_CalculateTotalHeight_ReturnsCorrectValue()
    {
        var estimator = CreateEstimator();
        const int visibleRows = 10;
        const double rowHeight = DefaultRowHeight;
        const int totalRows = 100;

        // Record some heights to establish estimate
        RecordUniformHeights(estimator, 0, visibleRows, rowHeight);
        var heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, 0, visibleRows - 1, heights, 0);

        double totalHeight = estimator.CalculateTotalHeight(totalRows, 0, Array.Empty<int>(), 0);
        double expectedHeight = totalRows * rowHeight;

        Assert.Equal(expectedHeight, totalHeight, Tolerance);
    }

    [Fact]
    public void SameHeight_NoGrouping_EstimateSlotAtOffset_ReturnsCorrectSlot()
    {
        var estimator = CreateEstimator();
        const int visibleRows = 10;
        const double rowHeight = DefaultRowHeight;
        const int totalRows = 100;

        // Record heights
        RecordUniformHeights(estimator, 0, visibleRows, rowHeight);
        var heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, 0, visibleRows - 1, heights, 0);

        // Test various offsets
        Assert.Equal(0, estimator.EstimateSlotAtOffset(0, totalRows));
        Assert.Equal(4, estimator.EstimateSlotAtOffset(rowHeight * 4.5, totalRows));
        Assert.Equal(50, estimator.EstimateSlotAtOffset(rowHeight * 50, totalRows));
    }

    [Fact]
    public void SameHeight_NoGrouping_EstimateOffsetToSlot_ReturnsCorrectOffset()
    {
        var estimator = CreateEstimator();
        const int visibleRows = 10;
        const double rowHeight = DefaultRowHeight;

        // Record heights
        RecordUniformHeights(estimator, 0, visibleRows, rowHeight);
        var heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, 0, visibleRows - 1, heights, 0);

        // Test various slots
        Assert.Equal(0, estimator.EstimateOffsetToSlot(0), Tolerance);
        Assert.Equal(rowHeight * 5, estimator.EstimateOffsetToSlot(5), Tolerance);
        Assert.Equal(rowHeight * 50, estimator.EstimateOffsetToSlot(50), Tolerance);
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
        const int visibleRows = 10;
        const double rowHeight = DefaultRowHeight;
        const int totalSlots = 110; // 100 rows + 10 group headers
        int[] groupHeaderCounts = { 5, 5 }; // 5 level-0 headers, 5 level-1 headers

        // Record heights
        RecordUniformHeights(estimator, 0, visibleRows, rowHeight);
        var heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, 0, visibleRows - 1, heights, 0);

        // Record group header heights
        estimator.RecordRowGroupHeaderHeight(0, 0, GroupHeaderHeight);
        estimator.RecordRowGroupHeaderHeight(1, 1, GroupHeaderHeight);

        double totalHeight = estimator.CalculateTotalHeight(totalSlots, 0, groupHeaderCounts, 0);
        double expectedRowHeight = 100 * rowHeight;
        double expectedHeaderHeight = 10 * GroupHeaderHeight;

        Assert.Equal(expectedRowHeight + expectedHeaderHeight, totalHeight, 10.0);
    }

    [Fact]
    public void SameHeight_WithGrouping_CollapsedGroups_ReducesTotalHeight()
    {
        var estimator = CreateEstimator();
        const int visibleRows = 10;
        const double rowHeight = DefaultRowHeight;
        const int totalSlots = 100;
        const int collapsedSlots = 20;

        // Record heights
        RecordUniformHeights(estimator, 0, visibleRows, rowHeight);
        var heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, 0, visibleRows - 1, heights, 0);

        double totalHeight = estimator.CalculateTotalHeight(totalSlots, collapsedSlots, Array.Empty<int>(), 0);
        double expectedHeight = (totalSlots - collapsedSlots) * rowHeight;

        Assert.Equal(expectedHeight, totalHeight, Tolerance);
    }

    [Fact]
    public void SameHeight_WithGrouping_ScrollPastCollapsedGroup()
    {
        var estimator = CreateEstimator();
        const int visibleRows = 10;
        const double rowHeight = DefaultRowHeight;

        // Initial display
        RecordUniformHeights(estimator, 0, visibleRows, rowHeight);
        var heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, 0, visibleRows - 1, heights, 0, 0, 0);

        // Scroll past a collapsed group (some slots are collapsed)
        RecordUniformHeights(estimator, 20, visibleRows, rowHeight);
        heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, 20, 29, heights, 15 * rowHeight, 0, 5); // 5 collapsed slots before slot 20

        // Estimate should still be based on visible rows
        Assert.Equal(rowHeight, estimator.RowHeightEstimate, Tolerance);
    }

    #endregion

    #region Variable Height Rows - No Grouping

    [Fact]
    public void VariableHeight_NoGrouping_InitialDisplay_ComputesAverage()
    {
        var estimator = CreateEstimator();
        const int visibleRows = 10;

        // Record alternating heights
        RecordVariableHeights(estimator, 0, visibleRows, AlternatingHeight);
        var heights = CreateVariableHeights(0, visibleRows, AlternatingHeight);
        SimulateScroll(estimator, 0, visibleRows - 1, heights, 0);

        double expectedAverage = (SmallRowHeight + LargeRowHeight) / 2;
        VerifyEstimateAfterMeasurements(estimator, expectedAverage);
    }

    [Fact]
    public void VariableHeight_NoGrouping_SmallScroll_UpdatesEstimate()
    {
        var estimator = CreateEstimator();
        const int visibleRows = 10;

        // Initial display with alternating heights
        RecordVariableHeights(estimator, 0, visibleRows, AlternatingHeight);
        var heights = CreateVariableHeights(0, visibleRows, AlternatingHeight);
        SimulateScroll(estimator, 0, visibleRows - 1, heights, 0);

        // Scroll down by 2 rows
        RecordVariableHeights(estimator, visibleRows, 2, AlternatingHeight);
        heights = CreateVariableHeights(2, visibleRows, AlternatingHeight);
        double offset = SmallRowHeight + LargeRowHeight; // Height of first two rows
        SimulateScroll(estimator, 2, visibleRows + 1, heights, offset);

        // Estimate should be close to average
        double expectedAverage = (SmallRowHeight + LargeRowHeight) / 2;
        VerifyEstimateAfterMeasurements(estimator, expectedAverage);
    }

    [Fact]
    public void VariableHeight_NoGrouping_LargeScroll_UpdatesEstimate()
    {
        var estimator = CreateEstimator();
        const int visibleRows = 10;
        const int totalRows = 1000;

        // Initial display
        RecordVariableHeights(estimator, 0, visibleRows, AlternatingHeight);
        var heights = CreateVariableHeights(0, visibleRows, AlternatingHeight);
        SimulateScroll(estimator, 0, visibleRows - 1, heights, 0);

        // Large scroll to middle
        int scrollToSlot = 500;
        RecordVariableHeights(estimator, scrollToSlot, visibleRows, AlternatingHeight);
        heights = CreateVariableHeights(scrollToSlot, visibleRows, AlternatingHeight);
        double expectedAverage = (SmallRowHeight + LargeRowHeight) / 2;
        SimulateScroll(estimator, scrollToSlot, scrollToSlot + visibleRows - 1, heights, scrollToSlot * expectedAverage);

        VerifyEstimateAfterMeasurements(estimator, expectedAverage);
    }

    [Fact]
    public void VariableHeight_NoGrouping_GradualIncrease_TracksEstimate()
    {
        var estimator = CreateEstimator();
        const int visibleRows = 10;

        // Initial display with gradually increasing heights
        RecordVariableHeights(estimator, 0, visibleRows, GradualIncreaseHeight);
        var heights = CreateVariableHeights(0, visibleRows, GradualIncreaseHeight);
        SimulateScroll(estimator, 0, visibleRows - 1, heights, 0);

        // The estimate should be somewhere in the measured range
        var diag = estimator.GetDiagnostics();
        Assert.True(diag.CurrentRowHeightEstimate >= SmallRowHeight);
    }

    [Fact]
    public void VariableHeight_NoGrouping_ScrollToBottom_Fast()
    {
        var estimator = CreateEstimator();
        const int visibleRows = 10;
        const int totalRows = 100;

        // Initial display
        RecordVariableHeights(estimator, 0, visibleRows, AlternatingHeight);
        var heights = CreateVariableHeights(0, visibleRows, AlternatingHeight);
        SimulateScroll(estimator, 0, visibleRows - 1, heights, 0);

        // Fast scroll to bottom
        int bottomStart = totalRows - visibleRows;
        RecordVariableHeights(estimator, bottomStart, visibleRows, AlternatingHeight);
        heights = CreateVariableHeights(bottomStart, visibleRows, AlternatingHeight);
        double expectedAverage = (SmallRowHeight + LargeRowHeight) / 2;
        SimulateScroll(estimator, bottomStart, totalRows - 1, heights, bottomStart * expectedAverage);

        // Should have reasonable estimate
        Assert.True(estimator.RowHeightEstimate > 0);
    }

    [Fact]
    public void VariableHeight_NoGrouping_EstimateSlotAtOffset_ReasonableAccuracy()
    {
        var estimator = CreateEstimator();
        const int visibleRows = 20;
        const int totalRows = 100;

        // Record alternating heights for more rows
        RecordVariableHeights(estimator, 0, visibleRows, AlternatingHeight);
        var heights = CreateVariableHeights(0, visibleRows, AlternatingHeight);
        SimulateScroll(estimator, 0, visibleRows - 1, heights, 0);

        double expectedAverage = (SmallRowHeight + LargeRowHeight) / 2;

        // Estimate should be within a few rows
        int estimatedSlot = estimator.EstimateSlotAtOffset(expectedAverage * 50, totalRows);
        Assert.InRange(estimatedSlot, 45, 55);
    }

    [Fact]
    public void VariableHeight_NoGrouping_PseudoRandomHeights_HandlesVariation()
    {
        var estimator = CreateEstimator();
        const int visibleRows = 15;

        // Record pseudo-random heights
        RecordVariableHeights(estimator, 0, visibleRows, PseudoRandomHeight);
        var heights = CreateVariableHeights(0, visibleRows, PseudoRandomHeight);
        SimulateScroll(estimator, 0, visibleRows - 1, heights, 0);

        // Should have computed some estimate
        Assert.True(estimator.RowHeightEstimate >= SmallRowHeight);
        Assert.True(estimator.RowHeightEstimate <= LargeRowHeight);
    }

    #endregion

    #region Variable Height Rows - With Grouping

    [Fact]
    public void VariableHeight_WithGrouping_MixedContent()
    {
        var estimator = CreateEstimator();

        // Simulate mixed content: group header, rows, group header, rows
        estimator.RecordRowGroupHeaderHeight(0, 0, GroupHeaderHeight);
        estimator.RecordMeasuredHeight(1, SmallRowHeight);
        estimator.RecordMeasuredHeight(2, LargeRowHeight);
        estimator.RecordMeasuredHeight(3, SmallRowHeight);
        estimator.RecordRowGroupHeaderHeight(4, 0, GroupHeaderHeight);
        estimator.RecordMeasuredHeight(5, LargeRowHeight);

        var heights = new[] { GroupHeaderHeight, SmallRowHeight, LargeRowHeight, SmallRowHeight, GroupHeaderHeight, LargeRowHeight };
        SimulateScroll(estimator, 0, 5, heights, 0);

        // Group header estimates should be correct
        Assert.Equal(GroupHeaderHeight, estimator.GetRowGroupHeaderHeightEstimate(0), Tolerance);
    }

    [Fact]
    public void VariableHeight_WithGrouping_NestedGroups()
    {
        var estimator = CreateEstimator();

        // Record nested group headers
        estimator.RecordRowGroupHeaderHeight(0, 0, GroupHeaderHeight);
        estimator.RecordRowGroupHeaderHeight(1, 1, GroupHeaderHeight - 5);
        estimator.RecordMeasuredHeight(2, SmallRowHeight);
        estimator.RecordMeasuredHeight(3, LargeRowHeight);
        estimator.RecordRowGroupHeaderHeight(4, 1, GroupHeaderHeight - 5);
        estimator.RecordMeasuredHeight(5, MediumRowHeight);

        Assert.Equal(GroupHeaderHeight, estimator.GetRowGroupHeaderHeightEstimate(0), Tolerance);
        Assert.Equal(GroupHeaderHeight - 5, estimator.GetRowGroupHeaderHeightEstimate(1), Tolerance);
    }

    [Fact]
    public void VariableHeight_WithGrouping_ScrollAcrossGroups()
    {
        var estimator = CreateEstimator();
        const int visibleRows = 8;

        // Initial display in first group
        estimator.RecordRowGroupHeaderHeight(0, 0, GroupHeaderHeight);
        RecordVariableHeights(estimator, 1, visibleRows - 1, AlternatingHeight);
        var heights = new double[visibleRows];
        heights[0] = GroupHeaderHeight;
        for (int i = 1; i < visibleRows; i++)
        {
            heights[i] = AlternatingHeight(i);
        }
        SimulateScroll(estimator, 0, visibleRows - 1, heights, 0);

        // Scroll to see next group
        estimator.RecordRowGroupHeaderHeight(20, 0, GroupHeaderHeight);
        RecordVariableHeights(estimator, 21, visibleRows - 1, AlternatingHeight);
        heights[0] = GroupHeaderHeight;
        for (int i = 1; i < visibleRows; i++)
        {
            heights[i] = AlternatingHeight(20 + i);
        }
        SimulateScroll(estimator, 20, 20 + visibleRows - 1, heights, 500);

        // Should maintain group header estimate
        Assert.Equal(GroupHeaderHeight, estimator.GetRowGroupHeaderHeightEstimate(0), Tolerance);
    }

    [Fact]
    public void VariableHeight_WithGrouping_CalculateTotalHeight_Complex()
    {
        var estimator = CreateEstimator();
        const int visibleRows = 10;
        const int totalSlots = 120; // 100 rows + 20 group headers (10 level 0, 10 level 1)
        int[] groupHeaderCounts = { 10, 10 };

        // Record some heights
        RecordVariableHeights(estimator, 0, visibleRows, AlternatingHeight);
        var heights = CreateVariableHeights(0, visibleRows, AlternatingHeight);
        SimulateScroll(estimator, 0, visibleRows - 1, heights, 0);

        // Record group headers
        estimator.RecordRowGroupHeaderHeight(0, 0, GroupHeaderHeight);
        estimator.RecordRowGroupHeaderHeight(1, 1, GroupHeaderHeight - 3);

        double totalHeight = estimator.CalculateTotalHeight(totalSlots, 0, groupHeaderCounts, 0);

        // Should be greater than zero and reasonable
        Assert.True(totalHeight > 0);
        Assert.True(totalHeight < totalSlots * LargeRowHeight);
    }

    #endregion

    #region Data Source Changes

    [Fact]
    public void OnDataSourceChanged_ResetsEstimates()
    {
        var estimator = CreateEstimator();

        // Record some heights
        RecordUniformHeights(estimator, 0, 10, LargeRowHeight);
        var heights = CreateUniformHeights(10, LargeRowHeight);
        SimulateScroll(estimator, 0, 9, heights, 0);

        // Change data source
        estimator.OnDataSourceChanged(50);

        // Should be reset
        Assert.Equal(DefaultRowHeight, estimator.RowHeightEstimate, Tolerance);
    }

    [Fact]
    public void OnItemsInserted_UpdatesState()
    {
        var estimator = CreateEstimator();

        // Record some heights
        RecordUniformHeights(estimator, 0, 10, DefaultRowHeight);
        var heights = CreateUniformHeights(10, DefaultRowHeight);
        SimulateScroll(estimator, 0, 9, heights, 0);

        // Insert items
        estimator.OnItemsInserted(5, 10);

        // Should still function
        Assert.True(estimator.RowHeightEstimate > 0);
    }

    [Fact]
    public void OnItemsRemoved_UpdatesState()
    {
        var estimator = CreateEstimator();

        // Record some heights
        RecordUniformHeights(estimator, 0, 20, DefaultRowHeight);
        var heights = CreateUniformHeights(10, DefaultRowHeight);
        SimulateScroll(estimator, 0, 9, heights, 0);

        // Remove items
        estimator.OnItemsRemoved(5, 5);

        // Should still function
        Assert.True(estimator.RowHeightEstimate > 0);
    }

    [Fact]
    public void Reset_ClearsAllState()
    {
        var estimator = CreateEstimator();

        // Record heights and details
        RecordUniformHeights(estimator, 0, 10, LargeRowHeight);
        estimator.RecordMeasuredHeight(0, LargeRowHeight, true, DetailsHeight);
        estimator.RecordRowGroupHeaderHeight(0, 0, GroupHeaderHeight);

        // Reset
        estimator.Reset();

        // Should be back to defaults
        Assert.Equal(DefaultRowHeight, estimator.RowHeightEstimate, Tolerance);
        Assert.Equal(0, estimator.RowDetailsHeightEstimate, Tolerance);
    }

    #endregion

    #region Row Details

    [Fact]
    public void RowDetails_RecordsDetailsHeight()
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

    [Fact]
    public void RowDetails_CalculateTotalHeight_IncludesDetails()
    {
        var estimator = CreateEstimator();
        const int totalRows = 100;
        const int detailsCount = 5;

        RecordUniformHeights(estimator, 0, 10, DefaultRowHeight);
        var heights = CreateUniformHeights(10, DefaultRowHeight);
        SimulateScroll(estimator, 0, 9, heights, 0);

        estimator.RecordMeasuredHeight(0, DefaultRowHeight, true, DetailsHeight);

        double totalHeight = estimator.CalculateTotalHeight(totalRows, 0, Array.Empty<int>(), detailsCount);
        double baseHeight = totalRows * DefaultRowHeight;
        double expectedDetailsHeight = detailsCount * DetailsHeight;

        Assert.Equal(baseHeight + expectedDetailsHeight, totalHeight, Tolerance);
    }

    #endregion

    #region Diagnostics

    [Fact]
    public void GetDiagnostics_ReturnsValidData()
    {
        var estimator = CreateEstimator();

        RecordUniformHeights(estimator, 0, 10, DefaultRowHeight);
        var heights = CreateUniformHeights(10, DefaultRowHeight);
        SimulateScroll(estimator, 0, 9, heights, 0);

        var diag = estimator.GetDiagnostics();

        Assert.False(string.IsNullOrEmpty(diag.AlgorithmName));
        Assert.True(diag.CurrentRowHeightEstimate > 0);
    }

    #endregion

    #region Bidirectional Scrolling Tests

    [Fact]
    public void ScrollDown_SmallIncrement_UpdatesEstimate()
    {
        var estimator = CreateEstimator();
        int visibleRows = 10;
        double rowHeight = DefaultRowHeight;

        // Start at top
        RecordUniformHeights(estimator, 0, visibleRows, rowHeight);
        var heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, 0, visibleRows - 1, heights, 0);

        // Scroll down by 5 rows
        RecordUniformHeights(estimator, visibleRows, 5, rowHeight);
        heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, 5, 14, heights, 5 * rowHeight);

        Assert.Equal(rowHeight, estimator.RowHeightEstimate, Tolerance);
    }

    [Fact]
    public void ScrollUp_SmallIncrement_UpdatesEstimate()
    {
        var estimator = CreateEstimator();
        int visibleRows = 10;
        double rowHeight = DefaultRowHeight;

        // Start in middle
        RecordUniformHeights(estimator, 50, visibleRows, rowHeight);
        var heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, 50, 59, heights, 50 * rowHeight);

        // Scroll up
        RecordUniformHeights(estimator, 45, 5, rowHeight);
        heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, 45, 54, heights, 45 * rowHeight);

        Assert.Equal(rowHeight, estimator.RowHeightEstimate, Tolerance);
    }

    [Fact]
    public void ScrollDown_ThenUp_MaintainsAccuracy()
    {
        var estimator = CreateEstimator();
        int visibleRows = 10;
        double rowHeight = DefaultRowHeight;

        // Start at top
        RecordUniformHeights(estimator, 0, visibleRows, rowHeight);
        var heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, 0, visibleRows - 1, heights, 0);

        // Scroll down
        RecordUniformHeights(estimator, 30, visibleRows, rowHeight);
        heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, 30, 39, heights, 30 * rowHeight);

        // Scroll back up
        RecordUniformHeights(estimator, 10, visibleRows, rowHeight);
        heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, 10, 19, heights, 10 * rowHeight);

        Assert.Equal(rowHeight, estimator.RowHeightEstimate, Tolerance);
    }

    [Fact]
    public void ScrollUp_ThenDown_MaintainsAccuracy()
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

        // Scroll down
        RecordUniformHeights(estimator, 40, visibleRows, rowHeight);
        heights = CreateUniformHeights(visibleRows, rowHeight);
        SimulateScroll(estimator, 40, 49, heights, 40 * rowHeight);

        Assert.Equal(rowHeight, estimator.RowHeightEstimate, Tolerance);
    }

    [Fact]
    public void VariableHeight_ScrollDown_AdaptsEstimate()
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

        Assert.True(estimator.RowHeightEstimate > initialEstimate,
            "Estimate should increase after seeing larger rows");
    }

    [Fact]
    public void VariableHeight_ScrollUp_AdaptsEstimate()
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

        // DefaultRowHeightEstimator averages all measured heights, so it should be between small and large
        Assert.InRange(estimator.RowHeightEstimate, SmallRowHeight, initialEstimate);
    }

    #endregion

    #region Multi-Region Scrolling Tests

    [Fact]
    public void MultiRegion_TopMiddleBottom_ConsistentEstimate()
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

        Assert.Equal(rowHeight, estimator.RowHeightEstimate, Tolerance);
    }

    [Fact]
    public void MultiRegion_DifferentHeights_AveragesCorrectly()
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

        // Estimate should be somewhere between
        Assert.InRange(estimator.RowHeightEstimate, SmallRowHeight, LargeRowHeight);
    }

    [Fact]
    public void MultiRegion_RandomJumps_StableEstimate()
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

        Assert.Equal(rowHeight, estimator.RowHeightEstimate, Tolerance);
    }

    [Fact]
    public void MultiRegion_SequentialNavigation_TracksAll()
    {
        var estimator = CreateEstimator();
        int visibleRows = 10;
        double rowHeight = DefaultRowHeight;
        int totalRows = 500;

        // Top → Middle → Bottom → Middle → Top
        int[] positions = { 0, 250, 490, 250, 0 };

        foreach (int pos in positions)
        {
            RecordUniformHeights(estimator, pos, visibleRows, rowHeight);
            var heights = CreateUniformHeights(visibleRows, rowHeight);
            SimulateScroll(estimator, pos, pos + visibleRows - 1, heights, pos * rowHeight);
        }

        Assert.Equal(rowHeight, estimator.RowHeightEstimate, Tolerance);
    }

    #endregion
}
