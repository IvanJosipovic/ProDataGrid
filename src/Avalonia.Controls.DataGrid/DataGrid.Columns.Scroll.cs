// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System.Diagnostics;

namespace Avalonia.Controls
{
    public partial class DataGrid
    {

                private bool ScrollColumnIntoView(int columnIndex)
                {
                    Debug.Assert(columnIndex >= 0 && columnIndex < ColumnsItemsInternal.Count);

                    if (DisplayData.FirstDisplayedScrollingCol != -1 &&
                        !ColumnsItemsInternal[columnIndex].IsFrozen &&
                        (columnIndex != DisplayData.FirstDisplayedScrollingCol || _negHorizontalOffset > 0))
                    {
                        int columnsToScroll;
                        if (ColumnsInternal.DisplayInOrder(columnIndex, DisplayData.FirstDisplayedScrollingCol))
                        {
                            columnsToScroll = ColumnsInternal.GetColumnCount(true /* isVisible */, false /* isFrozen */, columnIndex, DisplayData.FirstDisplayedScrollingCol);
                            if (_negHorizontalOffset > 0)
                            {
                                columnsToScroll++;
                            }
                            ScrollColumns(-columnsToScroll);
                        }
                        else if (columnIndex == DisplayData.FirstDisplayedScrollingCol && _negHorizontalOffset > 0)
                        {
                            ScrollColumns(-1);
                        }
                        else if (DisplayData.LastTotallyDisplayedScrollingCol == -1 ||
                                 (DisplayData.LastTotallyDisplayedScrollingCol != columnIndex &&
                                  ColumnsInternal.DisplayInOrder(DisplayData.LastTotallyDisplayedScrollingCol, columnIndex)))
                        {
                            double xColumnLeftEdge = GetColumnXFromIndex(columnIndex);
                            double xColumnRightEdge = xColumnLeftEdge + GetEdgedColumnWidth(ColumnsItemsInternal[columnIndex]);
                            double change = xColumnRightEdge - HorizontalOffset - CellsWidth;
                            double widthRemaining = change;

                            DataGridColumn newFirstDisplayedScrollingCol = ColumnsItemsInternal[DisplayData.FirstDisplayedScrollingCol];
                            DataGridColumn nextColumn = ColumnsInternal.GetNextVisibleColumn(newFirstDisplayedScrollingCol);
                            double newColumnWidth = GetEdgedColumnWidth(newFirstDisplayedScrollingCol) - _negHorizontalOffset;
                            while (nextColumn != null && widthRemaining >= newColumnWidth)
                            {
                                widthRemaining -= newColumnWidth;
                                newFirstDisplayedScrollingCol = nextColumn;
                                newColumnWidth = GetEdgedColumnWidth(newFirstDisplayedScrollingCol);
                                nextColumn = ColumnsInternal.GetNextVisibleColumn(newFirstDisplayedScrollingCol);
                                _negHorizontalOffset = 0;
                            }
                            _negHorizontalOffset += widthRemaining;
                            DisplayData.LastTotallyDisplayedScrollingCol = columnIndex;
                            if (newFirstDisplayedScrollingCol.Index == columnIndex)
                            {
                                _negHorizontalOffset = 0;
                                double frozenColumnWidth = ColumnsInternal.GetVisibleFrozenEdgedColumnsWidth();
                                // If the entire column cannot be displayed, we want to start showing it from its LeftEdge
                                if (newColumnWidth > (CellsWidth - frozenColumnWidth))
                                {
                                    DisplayData.LastTotallyDisplayedScrollingCol = -1;
                                    change = xColumnLeftEdge - HorizontalOffset - frozenColumnWidth;
                                }
                            }
                            DisplayData.FirstDisplayedScrollingCol = newFirstDisplayedScrollingCol.Index;

                            // At this point DisplayData.FirstDisplayedScrollingColumn and LastDisplayedScrollingColumn
                            // should be correct
                            if (change != 0)
                            {
                                UpdateHorizontalOffset(HorizontalOffset + change);
                            }
                        }
                    }
                    return true;
                }



                private void ScrollColumns(int columns)
                {
                    DataGridColumn newFirstVisibleScrollingCol = null;
                    DataGridColumn dataGridColumnTmp;
                    int colCount = 0;
                    if (columns > 0)
                    {
                        if (DisplayData.LastTotallyDisplayedScrollingCol >= 0)
                        {
                            dataGridColumnTmp = ColumnsItemsInternal[DisplayData.LastTotallyDisplayedScrollingCol];
                            while (colCount < columns && dataGridColumnTmp != null)
                            {
                                dataGridColumnTmp = ColumnsInternal.GetNextVisibleColumn(dataGridColumnTmp);
                                colCount++;
                            }

                            if (dataGridColumnTmp == null)
                            {
                                // no more column to display on the right of the last totally seen column
                                return;
                            }
                        }
                        Debug.Assert(DisplayData.FirstDisplayedScrollingCol >= 0);
                        dataGridColumnTmp = ColumnsItemsInternal[DisplayData.FirstDisplayedScrollingCol];
                        colCount = 0;
                        while (colCount < columns && dataGridColumnTmp != null)
                        {
                            dataGridColumnTmp = ColumnsInternal.GetNextVisibleColumn(dataGridColumnTmp);
                            colCount++;
                        }
                        newFirstVisibleScrollingCol = dataGridColumnTmp;
                    }

                    if (columns < 0)
                    {
                        Debug.Assert(DisplayData.FirstDisplayedScrollingCol >= 0);
                        dataGridColumnTmp = ColumnsItemsInternal[DisplayData.FirstDisplayedScrollingCol];
                        if (_negHorizontalOffset > 0)
                        {
                            colCount++;
                        }
                        while (colCount < -columns && dataGridColumnTmp != null)
                        {
                            dataGridColumnTmp = ColumnsInternal.GetPreviousVisibleScrollingColumn(dataGridColumnTmp);
                            colCount++;
                        }
                        newFirstVisibleScrollingCol = dataGridColumnTmp;
                        if (newFirstVisibleScrollingCol == null)
                        {
                            if (_negHorizontalOffset == 0)
                            {
                                // no more column to display on the left of the first seen column
                                return;
                            }
                            else
                            {
                                newFirstVisibleScrollingCol = ColumnsItemsInternal[DisplayData.FirstDisplayedScrollingCol];
                            }
                        }
                    }

                    double newColOffset = 0;
                    foreach (DataGridColumn dataGridColumn in ColumnsInternal.GetVisibleScrollingColumns())
                    {
                        if (dataGridColumn == newFirstVisibleScrollingCol)
                        {
                            break;
                        }
                        newColOffset += GetEdgedColumnWidth(dataGridColumn);
                    }

                    UpdateHorizontalOffset(newColOffset);
                }


    }
}
