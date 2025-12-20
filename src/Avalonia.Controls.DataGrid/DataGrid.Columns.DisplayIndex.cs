// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Diagnostics;

namespace Avalonia.Controls
{
    public partial class DataGrid
    {

        private void CorrectColumnDisplayIndexesAfterDeletion(DataGridColumn deletedColumn)
        {
            // Column indexes have already been adjusted.
            // This column has already been detached and has retained its old Index and DisplayIndex

            Debug.Assert(deletedColumn != null);
            Debug.Assert(deletedColumn.OwningGrid == null);
            Debug.Assert(deletedColumn.Index >= 0);
            Debug.Assert(deletedColumn.DisplayIndexWithFiller >= 0);

            try
            {
                InDisplayIndexAdjustments = true;

                // The DisplayIndex of columns greater than the deleted column need to be decremented,
                // as do the DisplayIndexMap values of modified column Indexes
                DataGridColumn column;
                ColumnsInternal.DisplayIndexMap.RemoveAt(deletedColumn.DisplayIndexWithFiller);
                for (int displayIndex = 0; displayIndex < ColumnsInternal.DisplayIndexMap.Count; displayIndex++)
                {
                    if (ColumnsInternal.DisplayIndexMap[displayIndex] > deletedColumn.Index)
                    {
                        ColumnsInternal.DisplayIndexMap[displayIndex]--;
                    }
                    if (displayIndex >= deletedColumn.DisplayIndexWithFiller)
                    {
                        column = ColumnsInternal.GetColumnAtDisplayIndex(displayIndex);
                        column.DisplayIndexWithFiller = column.DisplayIndexWithFiller - 1;
                        column.DisplayIndexHasChanged = true; // OnColumnDisplayIndexChanged needs to be raised later on
                    }
                }

                // Now raise all the OnColumnDisplayIndexChanged events
                FlushDisplayIndexChanged(true /*raiseEvent*/);
            }
            finally
            {
                InDisplayIndexAdjustments = false;
                FlushDisplayIndexChanged(false /*raiseEvent*/);
            }
        }



        private void CorrectColumnDisplayIndexesAfterInsertion(DataGridColumn insertedColumn)
        {
            Debug.Assert(insertedColumn != null);
            Debug.Assert(insertedColumn.OwningGrid == this);
            if (insertedColumn.DisplayIndexWithFiller == -1 || insertedColumn.DisplayIndexWithFiller >= ColumnsItemsInternal.Count)
            {
                // Developer did not assign a DisplayIndex or picked a large number.
                // Choose the Index as the DisplayIndex.
                insertedColumn.DisplayIndexWithFiller = insertedColumn.Index;
            }

            try
            {
                InDisplayIndexAdjustments = true;

                // The DisplayIndex of columns greater than the inserted column need to be incremented,
                // as do the DisplayIndexMap values of modified column Indexes
                DataGridColumn column;
                for (int displayIndex = 0; displayIndex < ColumnsInternal.DisplayIndexMap.Count; displayIndex++)
                {
                    if (ColumnsInternal.DisplayIndexMap[displayIndex] >= insertedColumn.Index)
                    {
                        ColumnsInternal.DisplayIndexMap[displayIndex]++;
                    }
                    if (displayIndex >= insertedColumn.DisplayIndexWithFiller)
                    {
                        column = ColumnsInternal.GetColumnAtDisplayIndex(displayIndex);
                        column.DisplayIndexWithFiller++;
                        column.DisplayIndexHasChanged = true; // OnColumnDisplayIndexChanged needs to be raised later on
                    }
                }
                ColumnsInternal.DisplayIndexMap.Insert(insertedColumn.DisplayIndexWithFiller, insertedColumn.Index);

                // Now raise all the OnColumnDisplayIndexChanged events
                FlushDisplayIndexChanged(true /*raiseEvent*/);
            }
            finally
            {
                InDisplayIndexAdjustments = false;
                FlushDisplayIndexChanged(false /*raiseEvent*/);
            }
        }



        private void CorrectColumnFrozenStates()
        {
            int index = 0;
            int totalColumns = ColumnsInternal.DisplayIndexMap.Count;
            int leftCount = FrozenColumnCountWithFiller;
            int rightCount = FrozenColumnCountRightEffective;
            int rightStartIndex = Math.Max(leftCount, totalColumns - rightCount);

            double oldLeftFrozenWidth = 0;
            double newLeftFrozenWidth = 0;

            foreach (DataGridColumn column in ColumnsInternal.GetDisplayedColumns())
            {
                if (column.IsFrozenLeft)
                {
                    oldLeftFrozenWidth += column.ActualWidth;
                }

                DataGridFrozenColumnPosition frozenPosition;
                if (index < leftCount)
                {
                    frozenPosition = DataGridFrozenColumnPosition.Left;
                }
                else if (index >= rightStartIndex)
                {
                    frozenPosition = DataGridFrozenColumnPosition.Right;
                }
                else
                {
                    frozenPosition = DataGridFrozenColumnPosition.None;
                }

                if (frozenPosition == DataGridFrozenColumnPosition.Left)
                {
                    newLeftFrozenWidth += column.ActualWidth;
                }

                column.FrozenPosition = frozenPosition;
                index++;
            }

            if (HorizontalOffset > Math.Max(0, newLeftFrozenWidth - oldLeftFrozenWidth))
            {
                UpdateHorizontalOffset(HorizontalOffset - newLeftFrozenWidth + oldLeftFrozenWidth);
            }
            else
            {
                UpdateHorizontalOffset(0);
            }
        }



        private void CorrectColumnIndexesAfterDeletion(DataGridColumn deletedColumn)
        {
            Debug.Assert(deletedColumn != null);
            for (int columnIndex = deletedColumn.Index; columnIndex < ColumnsItemsInternal.Count; columnIndex++)
            {
                ColumnsItemsInternal[columnIndex].Index = ColumnsItemsInternal[columnIndex].Index - 1;
                Debug.Assert(ColumnsItemsInternal[columnIndex].Index == columnIndex);
            }
        }



        private void CorrectColumnIndexesAfterInsertion(DataGridColumn insertedColumn, int insertionCount)
        {
            Debug.Assert(insertedColumn != null);
            Debug.Assert(insertionCount > 0);
            for (int columnIndex = insertedColumn.Index + insertionCount; columnIndex < ColumnsItemsInternal.Count; columnIndex++)
            {
                ColumnsItemsInternal[columnIndex].Index = columnIndex;
            }
        }



        private void FlushDisplayIndexChanged(bool raiseEvent)
        {
            foreach (DataGridColumn column in ColumnsItemsInternal)
            {
                if (column.DisplayIndexHasChanged)
                {
                    column.DisplayIndexHasChanged = false;
                    if (raiseEvent)
                    {
                        Debug.Assert(column != ColumnsInternal.RowGroupSpacerColumn);
                        OnColumnDisplayIndexChanged(column);
                    }
                }
            }
        }


    }
}
