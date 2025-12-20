// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable disable

using System;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace Avalonia.Controls
{
    /// <summary>
    /// Presenter for summary cells, handles layout to align with columns.
    /// </summary>
#if !DATAGRID_INTERNAL
    public
#endif
    class DataGridSummaryCellsPresenter : Panel
    {
        private DataGrid _owningGrid;
        private DataGridSummaryRow _ownerRow;

        /// <summary>
        /// Gets or sets the owning DataGrid.
        /// </summary>
        internal DataGrid OwningGrid
        {
            get => _owningGrid;
            set => _owningGrid = value;
        }

        /// <summary>
        /// Gets or sets the owner summary row.
        /// </summary>
        internal DataGridSummaryRow OwnerRow
        {
            get => _ownerRow;
            set => _ownerRow = value;
        }

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size availableSize)
        {
            if (OwningGrid == null || OwnerRow == null)
            {
                return base.MeasureOverride(availableSize);
            }

            double totalWidth = 0;
            double maxHeight = 0;
            double rowHeaderWidth = OwningGrid.AreRowHeadersVisible ? OwningGrid.ActualRowHeaderWidth : 0;

            foreach (var column in OwningGrid.ColumnsInternal.GetVisibleColumns())
            {
                var cell = OwnerRow.Cells[column.Index];
                cell.IsVisible = true;

                double width = column.LayoutRoundedWidth;
                cell.Measure(new Size(width, availableSize.Height));
                totalWidth += column.ActualWidth;
                if (cell.DesiredSize.Height > maxHeight)
                {
                    maxHeight = cell.DesiredSize.Height;
                }
            }

            foreach (var column in OwningGrid.ColumnsItemsInternal)
            {
                if (!column.IsVisible)
                {
                    OwnerRow.Cells[column.Index].IsVisible = false;
                }
            }

            return new Size(totalWidth + rowHeaderWidth, maxHeight);
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (OwningGrid == null || OwnerRow == null)
            {
                return base.ArrangeOverride(finalSize);
            }

            double rowHeaderWidth = OwningGrid.AreRowHeadersVisible ? OwningGrid.ActualRowHeaderWidth : 0;
            double horizontalOffset = OwnerRow.ApplyHorizontalOffset ? OwningGrid.HorizontalOffset : 0;
            double frozenLeftWidth = rowHeaderWidth + OwningGrid.GetVisibleFrozenColumnsWidthLeft();
            double frozenRightWidth = OwningGrid.GetVisibleFrozenColumnsWidthRight();
            double rightFrozenStart = frozenRightWidth > 0
                ? rowHeaderWidth + Math.Max(0, OwningGrid.CellsWidth - frozenRightWidth)
                : double.PositiveInfinity;
            double frozenLeftEdge = rowHeaderWidth;
            double rightFrozenEdge = frozenRightWidth > 0 ? rightFrozenStart : 0;
            double scrollingLeftEdge = rowHeaderWidth - horizontalOffset;

            foreach (var column in OwningGrid.ColumnsInternal.GetVisibleColumns())
            {
                var cell = OwnerRow.Cells[column.Index];
                cell.IsVisible = true;

                if (column.IsFrozenLeft)
                {
                    cell.Arrange(new Rect(frozenLeftEdge, 0, column.LayoutRoundedWidth, finalSize.Height));
                    cell.Clip = null;
                    frozenLeftEdge += column.ActualWidth;
                }
                else if (column.IsFrozenRight)
                {
                    cell.Arrange(new Rect(rightFrozenEdge, 0, column.LayoutRoundedWidth, finalSize.Height));
                    cell.Clip = null;
                    rightFrozenEdge += column.ActualWidth;
                }
                else
                {
                    cell.Arrange(new Rect(scrollingLeftEdge, 0, column.LayoutRoundedWidth, finalSize.Height));
                    EnsureCellClip(cell, column.ActualWidth, finalSize.Height, frozenLeftWidth, rightFrozenStart, scrollingLeftEdge);
                }

                scrollingLeftEdge += column.ActualWidth;
            }

            foreach (var column in OwningGrid.ColumnsItemsInternal)
            {
                if (!column.IsVisible)
                {
                    OwnerRow.Cells[column.Index].Arrange(new Rect(0, 0, 0, 0));
                }
            }

            return finalSize;
        }

        private static void EnsureCellClip(DataGridSummaryCell cell, double width, double height, double frozenLeftWidth, double rightFrozenStart, double cellLeftEdge)
        {
            if (cell.Column.IsFrozen)
            {
                cell.Clip = null;
                return;
            }

            double leftClip = Math.Max(0, frozenLeftWidth - cellLeftEdge);
            double rightClip = rightFrozenStart < double.PositiveInfinity
                ? Math.Max(0, (cellLeftEdge + width) - rightFrozenStart)
                : 0;

            if (leftClip > 0 || rightClip > 0)
            {
                var rect = new RectangleGeometry();
                double clipWidth = Math.Max(0, width - leftClip - rightClip);
                rect.Rect = new Rect(leftClip, 0, clipWidth, height);
                cell.Clip = rect;
            }
            else
            {
                cell.Clip = null;
            }
        }
    }
}
