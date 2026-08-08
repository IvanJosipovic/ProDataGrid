// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia.Controls.DataGridHierarchical;

namespace Avalonia.Controls
{
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    partial class DataGrid
    {
        private int _selectionCommitDepth;
        private bool _raisingSelectionChanging;

        /// <summary>
        /// Occurs after a complete selection proposal has been computed and before any grid
        /// selection, current-cell, anchor, currency, focus, or scrolling state is committed.
        /// Set <see cref="System.ComponentModel.CancelEventArgs.Cancel"/> to reject the proposal.
        /// </summary>
#if !DATAGRID_INTERNAL
        public
#else
        internal
#endif
        event EventHandler<DataGridSelectionChangingEventArgs> SelectionChanging;

        private bool HasSelectionChangingHandlers => SelectionChanging != null;

        private void SetSelectedItemWithPreview(object value)
        {
            if (_areHandlersSuspended || !HasSelectionChangingHandlers || _selectionCommitDepth > 0)
            {
                SetAndRaise(SelectedItemProperty, ref _selectedItem, value);
                return;
            }

            using var origin = BeginSelectionChangeScope(DataGridSelectionChangeSource.Programmatic);
            object normalizedItem = ProjectSelectionItem(value);
            int selectionIndex = normalizedItem == null ? -1 : GetSelectionModelIndexOfItem(normalizedItem);
            int slot = selectionIndex < 0 ? -1 : SlotFromSelectionIndex(selectionIndex);
            int columnIndex = CurrentColumnIndex >= 0
                ? CurrentColumnIndex
                : FirstDisplayedNonFillerColumnIndex;
            bool accepted;
            HierarchicalNode node = normalizedItem == null
                ? null
                : FindMaterializedHierarchyNode(normalizedItem);
            if (node != null && !IsHierarchicalItemVisible(normalizedItem))
            {
                CreateSelectionItemContext(
                    node,
                    out object item,
                    out HierarchicalNode resolvedNode,
                    out IReadOnlyList<HierarchicalNode> path);
                var unrealizedRow = new DataGridSelectionRowInfo(node, item, -1, resolvedNode, path);
                var proposedAnchor = new DataGridSelectionAnchorInfo(
                    node,
                    item,
                    -1,
                    columnIndex,
                    resolvedNode,
                    path,
                    isValid: false);
                accepted = TryRaiseSelectionChanging(
                    CaptureSelectedRowIndexes(),
                    new HashSet<int>(),
                    Array.Empty<DataGridCellInfo>(),
                    Array.Empty<DataGridCellInfo>(),
                    Array.Empty<DataGridColumn>(),
                    Array.Empty<DataGridColumn>(),
                    DataGridCellInfo.Unset,
                    proposedAnchor,
                    unrealizedRow,
                    item);
            }
            else
            {
                accepted = TryPreviewRowSelection(columnIndex, slot, DataGridSelectionAction.SelectCurrent);
            }

            if (!accepted)
            {
                return;
            }

            using var commit = BeginSelectionCommit();
            SetAndRaise(SelectedItemProperty, ref _selectedItem, value);
        }

        private HierarchicalNode FindMaterializedHierarchyNode(object item)
        {
            if (_hierarchicalModel == null || item == null)
            {
                return null;
            }

            HierarchicalNode direct = _hierarchicalModel.FindNode(item);
            if (direct != null)
            {
                return direct;
            }

            HierarchicalNode root = _hierarchicalModel.Root;
            if (root == null)
            {
                return null;
            }

            var pending = new Stack<HierarchicalNode>();
            pending.Push(root);
            while (pending.Count > 0)
            {
                HierarchicalNode current = pending.Pop();
                if (ReferenceEquals(current, item) || ReferenceEquals(current.Item, item))
                {
                    return current;
                }

                IReadOnlyList<HierarchicalNode> children = current.Children;
                for (int i = children.Count - 1; i >= 0; i--)
                {
                    pending.Push(children[i]);
                }
            }

            return null;
        }

        private void SetSelectedIndexWithPreview(int value)
        {
            if (_areHandlersSuspended || !HasSelectionChangingHandlers || _selectionCommitDepth > 0)
            {
                SetAndRaise(SelectedIndexProperty, ref _selectedIndex, value);
                return;
            }

            using var origin = BeginSelectionChangeScope(DataGridSelectionChangeSource.Programmatic);
            int slot = value < 0 ? -1 : SlotFromSelectionIndex(value);
            int columnIndex = CurrentColumnIndex >= 0
                ? CurrentColumnIndex
                : FirstDisplayedNonFillerColumnIndex;
            if (!TryPreviewRowSelection(columnIndex, slot, DataGridSelectionAction.SelectCurrent))
            {
                return;
            }

            using var commit = BeginSelectionCommit();
            SetAndRaise(SelectedIndexProperty, ref _selectedIndex, value);
        }

        internal SelectionCommitScope BeginSelectionCommit()
        {
            _selectionCommitDepth++;
            return new SelectionCommitScope(this);
        }

        internal bool TryPreviewSetRowSelection(int slot, bool isSelected, bool setAnchorSlot)
        {
            if (!HasSelectionChangingHandlers || _selectionCommitDepth > 0)
            {
                return true;
            }

            ThrowIfSelectionChangingReentrant();
            var currentRows = CaptureSelectedRowIndexes();
            var proposedRows = new HashSet<int>(currentRows);
            int rowIndex = slot >= 0 && slot < SlotCount && !IsGroupSlot(slot)
                ? RowIndexFromSlot(slot)
                : -1;
            if (rowIndex >= 0)
            {
                if (isSelected)
                {
                    proposedRows.Add(rowIndex);
                }
                else
                {
                    proposedRows.Remove(rowIndex);
                }
            }

            return TryRaiseSelectionChanging(
                currentRows,
                proposedRows,
                Array.Empty<DataGridCellInfo>(),
                Array.Empty<DataGridCellInfo>(),
                Array.Empty<DataGridColumn>(),
                Array.Empty<DataGridColumn>(),
                CurrentCell,
                setAnchorSlot ? CreateAnchorInfo(slot, CurrentColumnIndex) : GetCurrentSelectionAnchorInfo());
        }

        internal bool TryPreviewClearRowSelection(bool resetAnchorSlot)
        {
            if (!HasSelectionChangingHandlers || _selectionCommitDepth > 0)
            {
                return true;
            }

            ThrowIfSelectionChangingReentrant();
            return TryRaiseSelectionChanging(
                CaptureSelectedRowIndexes(),
                new HashSet<int>(),
                Array.Empty<DataGridCellInfo>(),
                Array.Empty<DataGridCellInfo>(),
                Array.Empty<DataGridColumn>(),
                Array.Empty<DataGridColumn>(),
                CurrentCell,
                resetAnchorSlot ? DataGridSelectionAnchorInfo.Unset : GetCurrentSelectionAnchorInfo());
        }

        private bool TryPreviewClearSelectionAndCurrent()
        {
            if (!HasSelectionChangingHandlers || _selectionCommitDepth > 0)
            {
                return true;
            }

            ThrowIfSelectionChangingReentrant();
            return TryRaiseSelectionChanging(
                CaptureSelectedRowIndexes(),
                new HashSet<int>(),
                Array.Empty<DataGridCellInfo>(),
                Array.Empty<DataGridCellInfo>(),
                Array.Empty<DataGridColumn>(),
                Array.Empty<DataGridColumn>(),
                DataGridCellInfo.Unset,
                DataGridSelectionAnchorInfo.Unset);
        }

        private bool TryPreviewRowSet(
            HashSet<int> proposedRows,
            DataGridCellInfo proposedCurrentCell,
            DataGridSelectionAnchorInfo proposedAnchor)
        {
            if (!HasSelectionChangingHandlers || _selectionCommitDepth > 0)
            {
                return true;
            }

            ThrowIfSelectionChangingReentrant();
            return TryRaiseSelectionChanging(
                CaptureSelectedRowIndexes(),
                proposedRows,
                Array.Empty<DataGridCellInfo>(),
                Array.Empty<DataGridCellInfo>(),
                Array.Empty<DataGridColumn>(),
                Array.Empty<DataGridColumn>(),
                proposedCurrentCell,
                proposedAnchor);
        }

        private bool TryPreviewSelectionModelSelection()
        {
            if (!HasSelectionChangingHandlers || _selectionCommitDepth > 0)
            {
                return true;
            }

            var proposedRows = new HashSet<int>();
            int preferredSelectionIndex = _preferredSelectionIndex >= 0
                ? _preferredSelectionIndex
                : _selectionModelAdapter?.Model.SelectedIndex ?? -1;
            int proposedSlot = -1;
            IReadOnlyList<int> selectedIndexes = _selectionModelAdapter?.Model.SelectedIndexes;
            if (selectedIndexes != null)
            {
                for (int i = 0; i < selectedIndexes.Count; i++)
                {
                    int selectionIndex = selectedIndexes[i];
                    int slot = SlotFromSelectionIndex(selectionIndex);
                    if (slot < 0 || slot >= SlotCount || IsGroupSlot(slot))
                    {
                        continue;
                    }

                    int rowIndex = RowIndexFromSlot(slot);
                    if (rowIndex >= 0)
                    {
                        proposedRows.Add(rowIndex);
                        if (proposedSlot < 0 || selectionIndex == preferredSelectionIndex)
                        {
                            proposedSlot = slot;
                        }
                    }
                }
            }

            int columnIndex = CurrentColumnIndex >= 0
                ? CurrentColumnIndex
                : FirstDisplayedNonFillerColumnIndex;
            return TryPreviewRowSet(
                proposedRows,
                CreateCellInfo(columnIndex, proposedSlot),
                CreateAnchorInfo(proposedSlot, columnIndex));
        }

        private bool TryPreviewCurrentCell(DataGridCellInfo proposedCurrentCell)
        {
            if (!HasSelectionChangingHandlers || _selectionCommitDepth > 0)
            {
                return true;
            }

            ThrowIfSelectionChangingReentrant();
            HashSet<int> rows = CaptureSelectedRowIndexes();
            return TryRaiseSelectionChanging(
                rows,
                new HashSet<int>(rows),
                Array.Empty<DataGridCellInfo>(),
                Array.Empty<DataGridCellInfo>(),
                Array.Empty<DataGridColumn>(),
                Array.Empty<DataGridColumn>(),
                proposedCurrentCell,
                GetCurrentSelectionAnchorInfo());
        }

        private void AddSlotRange(HashSet<int> rows, int firstSlot, int lastSlot)
        {
            int start = Math.Max(0, Math.Min(firstSlot, lastSlot));
            int end = Math.Min(SlotCount - 1, Math.Max(firstSlot, lastSlot));
            for (int slot = start; slot <= end; slot++)
            {
                if (!IsGroupSlot(slot))
                {
                    int rowIndex = RowIndexFromSlot(slot);
                    if (rowIndex >= 0)
                    {
                        rows.Add(rowIndex);
                    }
                }
            }
        }

        private void RemoveSlotRange(HashSet<int> rows, int firstSlot, int lastSlot)
        {
            int start = Math.Max(0, Math.Min(firstSlot, lastSlot));
            int end = Math.Min(SlotCount - 1, Math.Max(firstSlot, lastSlot));
            for (int slot = start; slot <= end; slot++)
            {
                if (!IsGroupSlot(slot))
                {
                    int rowIndex = RowIndexFromSlot(slot);
                    if (rowIndex >= 0)
                    {
                        rows.Remove(rowIndex);
                    }
                }
            }
        }

        private List<DataGridCellInfo> BuildCellSelectionProposal(
            bool append,
            int startRowIndex,
            int endRowIndex,
            int startColumnIndex,
            int endColumnIndex,
            bool columnIndexesAreDisplayIndexes)
        {
            var proposed = new List<DataGridCellInfo>();
            var coordinates = new HashSet<long>();
            if (append)
            {
                for (int i = 0; i < _selectedCellsView.Count; i++)
                {
                    DataGridCellInfo cell = _selectedCellsView[i];
                    if (coordinates.Add(GetCellCoordinateKey(cell.RowIndex, cell.ColumnIndex)))
                    {
                        proposed.Add(cell);
                    }
                }
            }

            if (DataConnection == null || ColumnsInternal == null)
            {
                return proposed;
            }

            int firstRow = Math.Max(0, Math.Min(startRowIndex, endRowIndex));
            int lastRow = Math.Min(DataConnection.Count - 1, Math.Max(startRowIndex, endRowIndex));
            int firstColumn = Math.Min(startColumnIndex, endColumnIndex);
            int lastColumn = Math.Max(startColumnIndex, endColumnIndex);
            for (int rowIndex = firstRow; rowIndex <= lastRow; rowIndex++)
            {
                int slot = SlotFromRowIndex(rowIndex);
                if (slot < 0 || IsGroupSlot(slot))
                {
                    continue;
                }

                object item = DataConnection.GetDataItem(rowIndex);
                for (int position = firstColumn; position <= lastColumn; position++)
                {
                    DataGridColumn column = columnIndexesAreDisplayIndexes
                        ? ColumnsInternal.GetColumnAtDisplayIndex(position)
                        : position >= 0 && position < ColumnsItemsInternal.Count
                            ? ColumnsItemsInternal[position]
                            : null;
                    if (column == null || !column.IsVisible || column is DataGridFillerColumn)
                    {
                        continue;
                    }

                    long key = GetCellCoordinateKey(rowIndex, column.Index);
                    if (coordinates.Add(key))
                    {
                        proposed.Add(new DataGridCellInfo(item, column, rowIndex, column.Index, isValid: true));
                    }
                }
            }

            return proposed;
        }

        private List<DataGridCellInfo> BuildNormalizedCellSelectionProposal(IEnumerable<DataGridCellInfo> cells)
        {
            int capacity = cells is ICollection<DataGridCellInfo> collection ? collection.Count : 0;
            var proposed = new List<DataGridCellInfo>(capacity);
            var coordinates = new HashSet<long>();
            if (cells == null)
            {
                return proposed;
            }

            foreach (DataGridCellInfo cell in cells)
            {
                if (TryNormalizeCell(cell, out DataGridCellInfo normalized) &&
                    coordinates.Add(GetCellCoordinateKey(normalized.RowIndex, normalized.ColumnIndex)))
                {
                    proposed.Add(normalized);
                }
            }

            return proposed;
        }

        private List<DataGridCellInfo> BuildColumnSelectionProposal(IList<DataGridColumn> columns)
        {
            var proposed = new List<DataGridCellInfo>();
            var coordinates = new HashSet<long>();
            int rowCount = DataConnection?.Count ?? 0;
            if (columns == null || rowCount <= 0)
            {
                return proposed;
            }

            for (int columnPosition = 0; columnPosition < columns.Count; columnPosition++)
            {
                DataGridColumn column = columns[columnPosition];
                if (column == null || !column.IsVisible || column is DataGridFillerColumn ||
                    column.Index < 0 || column.Index >= ColumnsItemsInternal.Count)
                {
                    continue;
                }

                for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
                {
                    int slot = SlotFromRowIndex(rowIndex);
                    if (slot < 0 || IsGroupSlot(slot))
                    {
                        continue;
                    }

                    long key = GetCellCoordinateKey(rowIndex, column.Index);
                    if (coordinates.Add(key))
                    {
                        proposed.Add(new DataGridCellInfo(
                            DataConnection.GetDataItem(rowIndex),
                            column,
                            rowIndex,
                            column.Index,
                            isValid: true));
                    }
                }
            }

            return proposed;
        }

        private static void RemoveCellFromProposal(
            List<DataGridCellInfo> proposed,
            int rowIndex,
            int columnIndex)
        {
            for (int i = proposed.Count - 1; i >= 0; i--)
            {
                DataGridCellInfo cell = proposed[i];
                if (cell.RowIndex == rowIndex && cell.ColumnIndex == columnIndex)
                {
                    proposed.RemoveAt(i);
                }
            }
        }

        private static void AddCellsToProposal(
            List<DataGridCellInfo> proposed,
            IReadOnlyList<DataGridCellInfo> cells)
        {
            var coordinates = new HashSet<long>();
            for (int i = 0; i < proposed.Count; i++)
            {
                coordinates.Add(GetCellCoordinateKey(proposed[i].RowIndex, proposed[i].ColumnIndex));
            }

            for (int i = 0; i < cells.Count; i++)
            {
                DataGridCellInfo cell = cells[i];
                if (coordinates.Add(GetCellCoordinateKey(cell.RowIndex, cell.ColumnIndex)))
                {
                    proposed.Add(cell);
                }
            }
        }

        private void RemoveDisplayRangeFromProposal(
            List<DataGridCellInfo> proposed,
            int startRowIndex,
            int endRowIndex,
            int startDisplayIndex,
            int endDisplayIndex)
        {
            int firstRow = Math.Min(startRowIndex, endRowIndex);
            int lastRow = Math.Max(startRowIndex, endRowIndex);
            int firstDisplay = Math.Min(startDisplayIndex, endDisplayIndex);
            int lastDisplay = Math.Max(startDisplayIndex, endDisplayIndex);
            for (int i = proposed.Count - 1; i >= 0; i--)
            {
                DataGridCellInfo cell = proposed[i];
                int displayIndex = GetColumnDisplayIndex(cell.ColumnIndex);
                if (cell.RowIndex >= firstRow && cell.RowIndex <= lastRow &&
                    displayIndex >= firstDisplay && displayIndex <= lastDisplay)
                {
                    proposed.RemoveAt(i);
                }
            }
        }

        private bool TryPreviewCellSelection(
            IReadOnlyList<DataGridCellInfo> proposedCells,
            DataGridCellInfo proposedCurrentCell,
            DataGridSelectionAnchorInfo proposedAnchor,
            HashSet<int> proposedRowsOverride = null)
        {
            if (!HasSelectionChangingHandlers || _selectionCommitDepth > 0)
            {
                return true;
            }

            ThrowIfSelectionChangingReentrant();
            var currentCoordinates = new HashSet<long>();
            var proposedCoordinates = new HashSet<long>();
            var currentCellRows = new HashSet<int>();
            var proposedCellRows = new HashSet<int>();
            var addedCells = new List<DataGridCellInfo>();
            var removedCells = new List<DataGridCellInfo>();

            for (int i = 0; i < _selectedCellsView.Count; i++)
            {
                DataGridCellInfo cell = _selectedCellsView[i];
                currentCoordinates.Add(GetCellCoordinateKey(cell.RowIndex, cell.ColumnIndex));
                currentCellRows.Add(cell.RowIndex);
            }

            for (int i = 0; i < proposedCells.Count; i++)
            {
                DataGridCellInfo cell = proposedCells[i];
                long key = GetCellCoordinateKey(cell.RowIndex, cell.ColumnIndex);
                if (proposedCoordinates.Add(key))
                {
                    proposedCellRows.Add(cell.RowIndex);
                    if (!currentCoordinates.Contains(key))
                    {
                        addedCells.Add(cell);
                    }
                }
            }

            for (int i = 0; i < _selectedCellsView.Count; i++)
            {
                DataGridCellInfo cell = _selectedCellsView[i];
                if (!proposedCoordinates.Contains(GetCellCoordinateKey(cell.RowIndex, cell.ColumnIndex)))
                {
                    removedCells.Add(cell);
                }
            }

            var currentRows = CaptureSelectedRowIndexes();
            var proposedRows = new HashSet<int>(currentRows);
            foreach (int rowIndex in currentCellRows)
            {
                if (!proposedCellRows.Contains(rowIndex))
                {
                    proposedRows.Remove(rowIndex);
                }
            }
            proposedRows.UnionWith(proposedCellRows);
            if (proposedRowsOverride != null)
            {
                proposedRows = new HashSet<int>(proposedRowsOverride);
                proposedRows.UnionWith(proposedCellRows);
            }

            var currentColumns = new HashSet<DataGridColumn>(_selectedColumnsView);
            var proposedColumnCounts = new Dictionary<DataGridColumn, int>();
            for (int i = 0; i < proposedCells.Count; i++)
            {
                DataGridColumn column = proposedCells[i].Column;
                proposedColumnCounts.TryGetValue(column, out int count);
                proposedColumnCounts[column] = count + 1;
            }

            var proposedColumns = new HashSet<DataGridColumn>();
            int rowCount = DataConnection?.Count ?? 0;
            if (rowCount > 0)
            {
                foreach (KeyValuePair<DataGridColumn, int> entry in proposedColumnCounts)
                {
                    if (entry.Value >= rowCount)
                    {
                        proposedColumns.Add(entry.Key);
                    }
                }
            }

            var addedColumns = new List<DataGridColumn>();
            var removedColumns = new List<DataGridColumn>();
            foreach (DataGridColumn column in proposedColumns)
            {
                if (!currentColumns.Contains(column))
                {
                    addedColumns.Add(column);
                }
            }
            foreach (DataGridColumn column in currentColumns)
            {
                if (!proposedColumns.Contains(column))
                {
                    removedColumns.Add(column);
                }
            }

            return TryRaiseSelectionChanging(
                currentRows,
                proposedRows,
                addedCells,
                removedCells,
                addedColumns,
                removedColumns,
                proposedCurrentCell,
                proposedAnchor);
        }

        private HashSet<int> BuildRowSelectionProposal(IList items, out int proposedSlot)
        {
            var proposedRows = new HashSet<int>();
            proposedSlot = -1;
            if (items == null || DataConnection == null)
            {
                return proposedRows;
            }

            int start = SelectionMode == DataGridSelectionMode.Single && items.Count > 0
                ? items.Count - 1
                : 0;
            for (int i = start; i < items.Count; i++)
            {
                object item = items[i];
                int selectionIndex = GetSelectionModelIndexOfItem(item);
                int slot = selectionIndex < 0 ? -1 : SlotFromSelectionIndex(selectionIndex);
                if (slot < 0 || slot >= SlotCount || IsGroupSlot(slot))
                {
                    continue;
                }

                int rowIndex = RowIndexFromSlot(slot);
                if (rowIndex >= 0)
                {
                    proposedRows.Add(rowIndex);
                    if (proposedSlot < 0)
                    {
                        proposedSlot = slot;
                    }
                }
            }

            return proposedRows;
        }

        private static long GetCellCoordinateKey(int rowIndex, int columnIndex) =>
            ((long)rowIndex << 32) | (uint)columnIndex;

        private bool TryPreviewRowSelection(
            int columnIndex,
            int slot,
            DataGridSelectionAction action)
        {
            if (!HasSelectionChangingHandlers || _selectionCommitDepth > 0)
            {
                return true;
            }

            ThrowIfSelectionChangingReentrant();

            var currentRows = CaptureSelectedRowIndexes();
            var proposedRows = new HashSet<int>(currentRows);
            int rowIndex = slot >= 0 && slot < SlotCount && !IsGroupSlot(slot)
                ? RowIndexFromSlot(slot)
                : -1;
            int proposedAnchorSlot = AnchorSlot;

            switch (action)
            {
                case DataGridSelectionAction.AddCurrentToSelection:
                    if (rowIndex >= 0)
                    {
                        proposedRows.Add(rowIndex);
                        proposedAnchorSlot = slot;
                    }
                    break;
                case DataGridSelectionAction.RemoveCurrentFromSelection:
                    if (rowIndex >= 0)
                    {
                        proposedRows.Remove(rowIndex);
                    }
                    break;
                case DataGridSelectionAction.SelectFromAnchorToCurrent:
                    proposedRows.Clear();
                    if (SelectionMode == DataGridSelectionMode.Extended && AnchorSlot >= 0 && rowIndex >= 0)
                    {
                        int anchorRowIndex = RowIndexFromSlot(AnchorSlot);
                        AddRowRange(proposedRows, anchorRowIndex, rowIndex);
                    }
                    else if (rowIndex >= 0)
                    {
                        proposedRows.Add(rowIndex);
                        proposedAnchorSlot = slot;
                    }
                    break;
                case DataGridSelectionAction.SelectCurrent:
                    proposedRows.Clear();
                    proposedAnchorSlot = -1;
                    if (rowIndex >= 0)
                    {
                        proposedRows.Add(rowIndex);
                        proposedAnchorSlot = slot;
                    }
                    break;
                case DataGridSelectionAction.None:
                    break;
            }

            DataGridCellInfo proposedCurrentCell = CreateCellInfo(columnIndex, slot);
            DataGridSelectionAnchorInfo proposedAnchor = CreateAnchorInfo(
                proposedAnchorSlot,
                columnIndex >= 0 ? columnIndex : CurrentColumnIndex);

            return TryRaiseSelectionChanging(
                currentRows,
                proposedRows,
                Array.Empty<DataGridCellInfo>(),
                Array.Empty<DataGridCellInfo>(),
                Array.Empty<DataGridColumn>(),
                Array.Empty<DataGridColumn>(),
                proposedCurrentCell,
                proposedAnchor);
        }

        private bool TryPreviewAllRows()
        {
            if (!HasSelectionChangingHandlers || _selectionCommitDepth > 0)
            {
                return true;
            }

            ThrowIfSelectionChangingReentrant();
            var currentRows = CaptureSelectedRowIndexes();
            var proposedRows = new HashSet<int>();
            if (DataConnection != null)
            {
                for (int rowIndex = 0; rowIndex < DataConnection.Count; rowIndex++)
                {
                    int slot = SlotFromRowIndex(rowIndex);
                    if (slot >= 0 && !IsGroupSlot(slot))
                    {
                        proposedRows.Add(rowIndex);
                    }
                }
            }

            return TryRaiseSelectionChanging(
                currentRows,
                proposedRows,
                Array.Empty<DataGridCellInfo>(),
                Array.Empty<DataGridCellInfo>(),
                Array.Empty<DataGridColumn>(),
                Array.Empty<DataGridColumn>(),
                CurrentCell,
                CreateAnchorInfo(AnchorSlot, CurrentColumnIndex));
        }

        private bool TryRaiseSelectionChanging(
            HashSet<int> currentRows,
            HashSet<int> proposedRows,
            IReadOnlyList<DataGridCellInfo> addedCells,
            IReadOnlyList<DataGridCellInfo> removedCells,
            IReadOnlyList<DataGridColumn> addedColumns,
            IReadOnlyList<DataGridColumn> removedColumns,
            DataGridCellInfo proposedCurrentCell,
            DataGridSelectionAnchorInfo proposedAnchor,
            DataGridSelectionRowInfo proposedUnrealizedRow = null,
            object proposedCurrentItemOverride = null)
        {
            if (!HasSelectionChangingHandlers || _selectionCommitDepth > 0)
            {
                return true;
            }

            var addedRows = new List<DataGridSelectionRowInfo>();
            var removedRows = new List<DataGridSelectionRowInfo>();
            var addedItems = new List<object>();
            var removedItems = new List<object>();

            AppendRowDelta(proposedRows, currentRows, addedRows, addedItems);
            AppendRowDelta(currentRows, proposedRows, removedRows, removedItems);
            if (proposedUnrealizedRow != null)
            {
                addedRows.Add(proposedUnrealizedRow);
                addedItems.Add(proposedUnrealizedRow.Item);
            }

            bool currentChanged = !CurrentCell.Equals(proposedCurrentCell) ||
                CurrentCell.RowIndex != proposedCurrentCell.RowIndex ||
                CurrentCell.ColumnIndex != proposedCurrentCell.ColumnIndex ||
                CurrentCell.IsValid != proposedCurrentCell.IsValid;
            DataGridSelectionAnchorInfo currentAnchor = GetCurrentSelectionAnchorInfo();
            bool anchorChanged = currentAnchor.IsValid != proposedAnchor.IsValid ||
                currentAnchor.RowIndex != proposedAnchor.RowIndex ||
                currentAnchor.ColumnIndex != proposedAnchor.ColumnIndex;

            if (addedRows.Count == 0 && removedRows.Count == 0 &&
                addedCells.Count == 0 && removedCells.Count == 0 &&
                addedColumns.Count == 0 && removedColumns.Count == 0 &&
                !currentChanged && !anchorChanged)
            {
                return true;
            }

            object proposedCurrentItem = proposedCurrentItemOverride;
            HierarchicalNode hierarchyNode = null;
            IReadOnlyList<HierarchicalNode> hierarchyPath = Array.Empty<HierarchicalNode>();
            if (proposedCurrentCell.IsValid)
            {
                CreateSelectionItemContext(
                    proposedCurrentCell.Item,
                    out proposedCurrentItem,
                    out hierarchyNode,
                    out hierarchyPath);
            }
            else if (proposedUnrealizedRow != null)
            {
                hierarchyNode = proposedUnrealizedRow.HierarchicalNode;
                hierarchyPath = proposedUnrealizedRow.HierarchyPath;
            }

            var args = new DataGridSelectionChangingEventArgs(
                addedItems,
                removedItems,
                addedRows,
                removedRows,
                addedCells,
                removedCells,
                addedColumns,
                removedColumns,
                proposedCurrentItem,
                proposedCurrentCell,
                proposedAnchor,
                hierarchyNode,
                hierarchyPath,
                CurrentSelectionChangeSource,
                CurrentSelectionTriggerEvent);

            _raisingSelectionChanging = true;
            try
            {
                SelectionChanging?.Invoke(this, args);
            }
            finally
            {
                _raisingSelectionChanging = false;
            }

            return !args.Cancel;
        }

        private HashSet<int> CaptureSelectedRowIndexes()
        {
            var result = new HashSet<int>();
            if (DataConnection == null)
            {
                return result;
            }

            foreach (int slot in _selectedItems.GetIndexes())
            {
                if (slot >= 0 && slot < SlotCount && !IsGroupSlot(slot))
                {
                    int rowIndex = RowIndexFromSlot(slot);
                    if (rowIndex >= 0 && rowIndex < DataConnection.Count)
                    {
                        result.Add(rowIndex);
                    }
                }
            }

            return result;
        }

        private void AppendRowDelta(
            HashSet<int> source,
            HashSet<int> excluded,
            List<DataGridSelectionRowInfo> rows,
            List<object> items)
        {
            if (DataConnection == null || source.Count == 0)
            {
                return;
            }

            var indexes = new List<int>(source.Count);
            foreach (int rowIndex in source)
            {
                if (!excluded.Contains(rowIndex))
                {
                    indexes.Add(rowIndex);
                }
            }
            indexes.Sort();

            for (int i = 0; i < indexes.Count; i++)
            {
                DataGridSelectionRowInfo row = CreateRowInfo(indexes[i]);
                if (row != null)
                {
                    rows.Add(row);
                    items.Add(row.Item);
                }
            }
        }

        private DataGridSelectionRowInfo CreateRowInfo(int rowIndex)
        {
            if (DataConnection == null || rowIndex < 0 || rowIndex >= DataConnection.Count)
            {
                return null;
            }

            object rowDataContext = DataConnection.GetDataItem(rowIndex);
            CreateSelectionItemContext(
                rowDataContext,
                out object item,
                out HierarchicalNode node,
                out IReadOnlyList<HierarchicalNode> path);
            return new DataGridSelectionRowInfo(rowDataContext, item, rowIndex, node, path);
        }

        private DataGridCellInfo CreateCellInfo(int columnIndex, int slot)
        {
            if (DataConnection == null || columnIndex < 0 || slot < 0 ||
                columnIndex >= ColumnsItemsInternal.Count || slot >= SlotCount ||
                IsGroupSlot(slot) || IsSlotOutOfSelectionBounds(slot))
            {
                return DataGridCellInfo.Unset;
            }

            int rowIndex = RowIndexFromSlot(slot);
            if (rowIndex < 0 || rowIndex >= DataConnection.Count)
            {
                return DataGridCellInfo.Unset;
            }

            DataGridColumn column = ColumnsItemsInternal[columnIndex];
            if (column == null || !column.IsVisible || column is DataGridFillerColumn)
            {
                return DataGridCellInfo.Unset;
            }

            return new DataGridCellInfo(
                DataConnection.GetDataItem(rowIndex),
                column,
                rowIndex,
                columnIndex,
                isValid: true);
        }

        private DataGridSelectionAnchorInfo CreateAnchorInfo(int slot, int columnIndex)
        {
            if (DataConnection == null || slot < 0 || slot >= SlotCount || IsGroupSlot(slot))
            {
                return DataGridSelectionAnchorInfo.Unset;
            }

            int rowIndex = RowIndexFromSlot(slot);
            if (rowIndex < 0 || rowIndex >= DataConnection.Count)
            {
                return DataGridSelectionAnchorInfo.Unset;
            }

            object rowDataContext = DataConnection.GetDataItem(rowIndex);
            CreateSelectionItemContext(
                rowDataContext,
                out object item,
                out HierarchicalNode node,
                out IReadOnlyList<HierarchicalNode> path);
            return new DataGridSelectionAnchorInfo(
                rowDataContext,
                item,
                rowIndex,
                columnIndex,
                node,
                path,
                isValid: true);
        }

        private DataGridSelectionAnchorInfo GetCurrentSelectionAnchorInfo()
        {
            if (SelectionUnit != DataGridSelectionUnit.FullRow && _cellAnchor.Slot >= 0)
            {
                return CreateAnchorInfo(_cellAnchor.Slot, _cellAnchor.ColumnIndex);
            }

            return CreateAnchorInfo(AnchorSlot, CurrentColumnIndex);
        }

        private static void CreateSelectionItemContext(
            object rowDataContext,
            out object item,
            out HierarchicalNode node,
            out IReadOnlyList<HierarchicalNode> hierarchyPath)
        {
            node = rowDataContext as HierarchicalNode;
            item = node?.Item ?? rowDataContext;
            if (node == null)
            {
                hierarchyPath = Array.Empty<HierarchicalNode>();
                return;
            }

            int count = 0;
            for (HierarchicalNode current = node; current != null; current = current.Parent)
            {
                if (current.Level >= 0)
                {
                    count++;
                }
            }

            var path = new HierarchicalNode[count];
            for (HierarchicalNode current = node; current != null; current = current.Parent)
            {
                if (current.Level >= 0)
                {
                    path[--count] = current;
                }
            }

            hierarchyPath = path;
        }

        private void AddRowRange(HashSet<int> rows, int first, int last)
        {
            if (DataConnection == null || first < 0 || last < 0)
            {
                return;
            }

            int start = Math.Min(first, last);
            int end = Math.Min(Math.Max(first, last), DataConnection.Count - 1);
            for (int rowIndex = start; rowIndex <= end; rowIndex++)
            {
                int slot = SlotFromRowIndex(rowIndex);
                if (slot >= 0 && !IsGroupSlot(slot))
                {
                    rows.Add(rowIndex);
                }
            }
        }

        private void ThrowIfSelectionChangingReentrant()
        {
            if (_raisingSelectionChanging)
            {
                throw new InvalidOperationException(
                    "Selection cannot be modified while SelectionChanging is being raised.");
            }
        }

        internal readonly struct SelectionCommitScope : IDisposable
        {
            private readonly DataGrid _owner;

            public SelectionCommitScope(DataGrid owner)
            {
                _owner = owner;
            }

            public void Dispose()
            {
                if (_owner != null)
                {
                    _owner._selectionCommitDepth--;
                }
            }
        }
    }
}
