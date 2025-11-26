// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using Xunit;

namespace Avalonia.Controls.DataGridTests.RowHeightEstimators;

/// <summary>
/// Base class providing common test infrastructure for row height estimator tests.
/// </summary>
public abstract class RowHeightEstimatorTestBase
{
    protected const double DefaultRowHeight = 22.0; // Matches the actual default in estimators
    protected const double SmallRowHeight = 20.0;
    protected const double MediumRowHeight = 30.0;
    protected const double LargeRowHeight = 50.0;
    protected const double GroupHeaderHeight = 28.0;
    protected const double DetailsHeight = 100.0;
    protected const double Tolerance = 0.001;

    /// <summary>
    /// Creates a new instance of the estimator being tested.
    /// </summary>
    protected abstract IDataGridRowHeightEstimator CreateEstimator();

    #region Helper Methods

    /// <summary>
    /// Simulates recording heights for a range of rows with uniform height.
    /// </summary>
    protected void RecordUniformHeights(IDataGridRowHeightEstimator estimator, int startSlot, int count, double height)
    {
        for (int i = startSlot; i < startSlot + count; i++)
        {
            estimator.RecordMeasuredHeight(i, height);
        }
    }

    /// <summary>
    /// Simulates recording heights for rows with variable heights based on a pattern.
    /// </summary>
    protected void RecordVariableHeights(IDataGridRowHeightEstimator estimator, int startSlot, int count, Func<int, double> heightFunc)
    {
        for (int i = startSlot; i < startSlot + count; i++)
        {
            estimator.RecordMeasuredHeight(i, heightFunc(i));
        }
    }

    /// <summary>
    /// Simulates a scroll operation by updating displayed rows.
    /// </summary>
    protected void SimulateScroll(
        IDataGridRowHeightEstimator estimator,
        int firstSlot,
        int lastSlot,
        double[] heights,
        double verticalOffset,
        double negVerticalOffset = 0,
        int collapsedSlotCount = 0,
        int detailsCount = 0)
    {
        estimator.UpdateFromDisplayedRows(firstSlot, lastSlot, heights, verticalOffset, negVerticalOffset, collapsedSlotCount, detailsCount);
    }

    /// <summary>
    /// Creates an array of uniform heights.
    /// </summary>
    protected double[] CreateUniformHeights(int count, double height)
    {
        var heights = new double[count];
        for (int i = 0; i < count; i++)
        {
            heights[i] = height;
        }
        return heights;
    }

    /// <summary>
    /// Creates an array of variable heights.
    /// </summary>
    protected double[] CreateVariableHeights(int startSlot, int count, Func<int, double> heightFunc)
    {
        var heights = new double[count];
        for (int i = 0; i < count; i++)
        {
            heights[i] = heightFunc(startSlot + i);
        }
        return heights;
    }

    /// <summary>
    /// Common variable height pattern: alternating small and large rows.
    /// </summary>
    protected double AlternatingHeight(int slot) => slot % 2 == 0 ? SmallRowHeight : LargeRowHeight;

    /// <summary>
    /// Common variable height pattern: gradual increase.
    /// </summary>
    protected double GradualIncreaseHeight(int slot) => SmallRowHeight + (slot * 2);

    /// <summary>
    /// Common variable height pattern: random-like based on slot.
    /// </summary>
    protected double PseudoRandomHeight(int slot)
    {
        // Deterministic "random" pattern for reproducible tests
        var hash = (slot * 31 + 17) % 100;
        return SmallRowHeight + (hash / 100.0) * (LargeRowHeight - SmallRowHeight);
    }

    #endregion

    #region Common Test Scenarios

    protected void VerifyInitialState(IDataGridRowHeightEstimator estimator)
    {
        Assert.Equal(DefaultRowHeight, estimator.RowHeightEstimate);
        Assert.Equal(0, estimator.RowDetailsHeightEstimate);
        Assert.Equal(DefaultRowHeight, estimator.GetRowGroupHeaderHeightEstimate(0));
    }

    protected void VerifyEstimateAfterMeasurements(IDataGridRowHeightEstimator estimator, double expectedAverage, double tolerance = 5.0)
    {
        Assert.InRange(estimator.RowHeightEstimate, expectedAverage - tolerance, expectedAverage + tolerance);
    }

    #endregion
}
