// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Avalonia.Controls.Utils;
using Avalonia.Utilities;
using System;
using System.Diagnostics;

namespace Avalonia.Controls
{
    public partial class DataGrid
    {

                /// <summary>
                /// Decreases the width of a non-star column by the given amount, if possible.  If the total desired
                /// adjustment amount could not be met, the remaining amount of adjustment is returned.  The adjustment
                /// stops when the column's target width has been met.
                /// </summary>
                /// <param name="column">Column to adjust.</param>
                /// <param name="targetWidth">The target width of the column (in pixels).</param>
                /// <param name="amount">Amount to decrease (in pixels).</param>
                /// <returns>The remaining amount of adjustment.</returns>
                private static double DecreaseNonStarColumnWidth(DataGridColumn column, double targetWidth, double amount)
                {
                    Debug.Assert(amount < 0);
                    Debug.Assert(column.Width.UnitType != DataGridLengthUnitType.Star);

                    if (MathUtilities.GreaterThanOrClose(targetWidth, column.Width.DisplayValue))
                    {
                        return amount;
                    }

                    double adjustment = Math.Max(
                        column.ActualMinWidth - column.Width.DisplayValue,
                        Math.Max(targetWidth - column.Width.DisplayValue, amount));

                    column.SetWidthDisplayValue(column.Width.DisplayValue + adjustment);
                    return amount - adjustment;
                }



                /// <summary>
                /// Decreases the widths of all non-star columns with DisplayIndex >= displayIndex such that the total
                /// width is decreased by the given amount, if possible.  If the total desired adjustment amount
                /// could not be met, the remaining amount of adjustment is returned.  The adjustment stops when
                /// the column's target width has been met.
                /// </summary>
                /// <param name="displayIndex">Starting column DisplayIndex.</param>
                /// <param name="targetWidth">The target width of the column (in pixels).</param>
                /// <param name="amount">Amount to decrease (in pixels).</param>
                /// <param name="reverse">Whether or not to reverse the order in which the columns are traversed.</param>
                /// <param name="affectNewColumns">Whether or not to adjust widths of columns that do not yet have their initial desired width.</param>
                /// <returns>The remaining amount of adjustment.</returns>
                private double DecreaseNonStarColumnWidths(int displayIndex, Func<DataGridColumn, double> targetWidth, double amount, bool reverse, bool affectNewColumns)
                {
                    if (MathUtilities.GreaterThanOrClose(amount, 0))
                    {
                        return amount;
                    }

                    foreach (DataGridColumn column in ColumnsInternal.GetDisplayedColumns(reverse,
                        column =>
                            column.IsVisible &&
                            column.Width.UnitType != DataGridLengthUnitType.Star &&
                            column.DisplayIndex >= displayIndex &&
                            column.ActualCanUserResize &&
                            (affectNewColumns || column.IsInitialDesiredWidthDetermined)))
                    {
                        amount = DecreaseNonStarColumnWidth(column, Math.Max(column.ActualMinWidth, targetWidth(column)), amount);
                        if (MathUtilities.IsZero(amount))
                        {
                            break;
                        }
                    }
                    return amount;
                }



                /// <summary>
                /// Increases the width of a non-star column by the given amount, if possible.  If the total desired
                /// adjustment amount could not be met, the remaining amount of adjustment is returned.  The adjustment
                /// stops when the column's target width has been met.
                /// </summary>
                /// <param name="column">Column to adjust.</param>
                /// <param name="targetWidth">The target width of the column (in pixels).</param>
                /// <param name="amount">Amount to increase (in pixels).</param>
                /// <returns>The remaining amount of adjustment.</returns>
                private static double IncreaseNonStarColumnWidth(DataGridColumn column, double targetWidth, double amount)
                {
                    Debug.Assert(amount > 0);
                    Debug.Assert(column.Width.UnitType != DataGridLengthUnitType.Star);

                    if (targetWidth <= column.Width.DisplayValue)
                    {
                        return amount;
                    }

                    double adjustment = Math.Min(
                        column.ActualMaxWidth - column.Width.DisplayValue,
                        Math.Min(targetWidth - column.Width.DisplayValue, amount));

                    column.SetWidthDisplayValue(column.Width.DisplayValue + adjustment);
                    return amount - adjustment;
                }



                /// <summary>
                /// Increases the widths of all non-star columns with DisplayIndex >= displayIndex such that the total
                /// width is increased by the given amount, if possible.  If the total desired adjustment amount
                /// could not be met, the remaining amount of adjustment is returned.  The adjustment stops when
                /// the column's target width has been met.
                /// </summary>
                /// <param name="displayIndex">Starting column DisplayIndex.</param>
                /// <param name="targetWidth">The target width of the column (in pixels).</param>
                /// <param name="amount">Amount to increase (in pixels).</param>
                /// <param name="reverse">Whether or not to reverse the order in which the columns are traversed.</param>
                /// <param name="affectNewColumns">Whether or not to adjust widths of columns that do not yet have their initial desired width.</param>
                /// <returns>The remaining amount of adjustment.</returns>
                private double IncreaseNonStarColumnWidths(int displayIndex, Func<DataGridColumn, double> targetWidth, double amount, bool reverse, bool affectNewColumns)
                {
                    if (MathUtilities.LessThanOrClose(amount, 0))
                    {
                        return amount;
                    }

                    foreach (DataGridColumn column in ColumnsInternal.GetDisplayedColumns(reverse,
                        column =>
                            column.IsVisible &&
                            column.Width.UnitType != DataGridLengthUnitType.Star &&
                            column.DisplayIndex >= displayIndex &&
                            column.ActualCanUserResize &&
                            (affectNewColumns || column.IsInitialDesiredWidthDetermined)))
                    {
                        amount = IncreaseNonStarColumnWidth(column, Math.Min(column.ActualMaxWidth, targetWidth(column)), amount);
                        if (MathUtilities.IsZero(amount))
                        {
                            break;
                        }
                    }
                    return amount;
                }


    }
}
