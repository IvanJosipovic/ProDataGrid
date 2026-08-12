// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable enable

using Avalonia.Collections;
using Avalonia.Utilities;

namespace Avalonia.Controls;

#if !DATAGRID_INTERNAL
public
#else
internal
#endif
partial class DataGrid
{
    private DataGridRow? _virtualCompatibilityRow;

    internal bool UsesFlatVisualLayout => VisualLayoutMode != DataGridVisualLayoutMode.Nested;

    internal bool UsesVirtualCellSurface =>
        VisualLayoutMode == DataGridVisualLayoutMode.Virtualized && CanDrawAllVisibleColumnsOnVirtualSurface();

    internal bool UsesVirtualCellSurfaceFallback =>
        VisualLayoutMode == DataGridVisualLayoutMode.Virtualized && !UsesVirtualCellSurface;

    internal bool UsesDefaultVirtualRowPipeline =>
        UsesVirtualCellSurface &&
        UsesDefaultRealizationFactory &&
        GetType() == typeof(DataGrid);

    private bool CanRetargetDefaultVirtualRowsNow()
    {
        if (!UsesDefaultVirtualRowPipeline ||
            AreRowHeadersVisible ||
            ShowRowNumbers ||
            RowDetailsTemplate is not null)
        {
            return false;
        }

        if (HasLoadingRowHandlers())
        {
            return false;
        }

        using var unloadingRoute = BuildEventRoute(UnloadingRowEvent);
        return !unloadingRoute.HasHandlers;
    }

    private bool HasLoadingRowHandlers()
    {
        using var route = BuildEventRoute(LoadingRowEvent);
        return route.HasHandlers;
    }

    private bool HasUnloadingRowHandlers()
    {
        using var route = BuildEventRoute(UnloadingRowEvent);
        return route.HasHandlers;
    }

    internal bool CanRetargetDefaultVirtualRow(DataGridRow row, int slot)
    {
        if (row.GetType() != typeof(DataGridRow) ||
            !IsRowRecyclable(row) ||
            row.IsKeyboardFocusWithin ||
            IsGroupSlot(slot) ||
            !MathUtilities.AreClose(
                row.DesiredSize.Height,
                DataGridRow.GetFlatDesiredHeight(this, RowHeight)))
        {
            return false;
        }

        object item = DataConnection.GetDataItem(RowIndexFromSlot(slot));
        return item is not DataGridRow;
    }

    internal void RetargetDefaultVirtualRow(DataGridRow row, int slot)
    {
        int rowIndex = RowIndexFromSlot(slot);
        object item = DataConnection.GetDataItem(rowIndex);

        DataGridDiagnostics.RecordRowRetargeted();
        row.ClearDragDropState();
        row.Index = rowIndex;
        row.Slot = slot;
        row.DataContext = item;
        row.IsPlaceholder = ReferenceEquals(item, DataGridCollectionView.NewItemPlaceholder);
        PrepareDefaultVirtualSurfaceRow(row, item);
        row.ApplyState();
        _rowsPresenter?.InvalidateChildIndex(row);
        DataGridDiagnostics.RecordRowRealized(DataGridDiagnostics.Sources.Retargeted);
    }

    private bool CanDrawAllVisibleColumnsOnVirtualSurface()
    {
        if (!double.IsFinite(RowHeight) ||
            CellTheme is not null ||
            (_conditionalFormattingModel?.Descriptors.Count ?? 0) > 0 ||
            (_searchModel?.Descriptors.Count ?? 0) > 0 ||
            HasCellPreparedHandlers ||
            HasCellClearingHandlers)
        {
            return false;
        }

        bool foundVisibleColumn = false;
        for (int index = 0; index < ColumnsItemsInternal.Count; index++)
        {
            DataGridColumn column = ColumnsItemsInternal[index];
            if (!column.IsVisible)
            {
                continue;
            }

            foundVisibleColumn = true;
            if (column.CellTheme is not null ||
                column.Width.IsAuto ||
                column.Width.IsSizeToCells ||
                !column.SupportsVirtualCellSurface)
            {
                return false;
            }
        }

        return foundVisibleColumn;
    }

    private void RefreshVirtualCellBackendAfterColumnsChanged()
    {
        if (VisualLayoutMode != DataGridVisualLayoutMode.Virtualized)
        {
            return;
        }

        foreach (DataGridRow row in GetAllRows())
        {
            CompleteCellsCollection(row);
        }

        _rowsPresenter?.ResetFlatVisualLayout();
        _rowsPresenter?.InvalidateMeasure();
        InvalidateMeasure();
    }

    internal void RefreshVirtualCellBackendIfEligibilityChanged()
    {
        if (VisualLayoutMode != DataGridVisualLayoutMode.Virtualized)
        {
            return;
        }

        bool surfaceAttached = _rowsPresenter?.VirtualSurfaceCount > 0;
        if (surfaceAttached != UsesVirtualCellSurface)
        {
            RefreshVirtualCellBackendAfterColumnsChanged();
        }
        else
        {
            _rowsPresenter?.InvalidateVirtualCellSurface();
        }
    }

    internal bool IsVirtualCompatibilityRow(DataGridRow row) =>
        UsesVirtualCellSurface && ReferenceEquals(_virtualCompatibilityRow, row);

    internal bool IsVirtualCompatibilityCell(DataGridRow row, DataGridColumn column) =>
        ReferenceEquals(EditingRow, row) && column.Index == CurrentColumnIndex;

    internal void EnsureVirtualCompatibilityRow(DataGridRow row)
    {
        if (!UsesVirtualCellSurface)
        {
            return;
        }

        if (!ReferenceEquals(_virtualCompatibilityRow, row))
        {
            ReleaseVirtualCompatibilityRow();
            _virtualCompatibilityRow = row;
        }

        CompleteCellsCollection(row);
        _rowsPresenter?.InvalidateMeasure();
    }

    private void ReleaseVirtualCompatibilityRow()
    {
        DataGridRow? row = _virtualCompatibilityRow;
        _virtualCompatibilityRow = null;
        if (row is null)
        {
            return;
        }

        CompleteCellsCollection(row);
        _rowsPresenter?.InvalidateMeasure();
    }

    private void OnVisualLayoutModeChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.OldValue is DataGridVisualLayoutMode oldMode &&
            e.NewValue is DataGridVisualLayoutMode newMode &&
            oldMode == newMode)
        {
            return;
        }

        if (!_measured)
        {
            _rowsPresenter?.ResetFlatVisualLayout();
            InvalidateMeasure();
            return;
        }

        _rowsPresenter?.ResetFlatVisualLayout();
        _virtualCompatibilityRow = null;
        UnloadElements(recycle: false);
        RefreshRowsAndColumns(clearRows: false);
        RefreshSelectionFromModel();
        InvalidateColumnHeadersMeasure();
        InvalidateMeasure();
    }
}
