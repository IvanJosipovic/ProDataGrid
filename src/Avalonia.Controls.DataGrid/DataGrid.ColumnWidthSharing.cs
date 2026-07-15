// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable disable

using Avalonia.Controls.DataGridSizing;

namespace Avalonia.Controls
{
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    partial class DataGrid
    {
        private void OnColumnWidthSharingScopeChanged(AvaloniaPropertyChangedEventArgs e)
        {
            DataGridColumnWidthSharingScope oldScope = e.OldValue as DataGridColumnWidthSharingScope;
            DataGridColumnWidthSharingScope newScope = e.NewValue as DataGridColumnWidthSharingScope;

            oldScope?.UnregisterGrid(this);
            newScope?.RegisterGrid(this);
        }

        internal void OnColumnWidthSharingGroupChanged(
            DataGridColumn column,
            string oldGroup,
            string newGroup)
        {
            ColumnWidthSharingScope?.ChangeColumnGroup(column, oldGroup, newGroup);
        }

        internal void OnColumnWidthSharingColumnAttached(DataGridColumn column)
        {
            ColumnWidthSharingScope?.RegisterColumn(column);
        }

        internal void OnColumnWidthSharingColumnDetached(DataGridColumn column)
        {
            ColumnWidthSharingScope?.UnregisterColumn(column);
        }

        internal void OnColumnWidthSharingColumnMeasured(DataGridColumn column)
        {
            ColumnWidthSharingScope?.ReportWidth(column);
        }

        internal void RefreshColumnWidthSharingRegistration()
        {
            DataGridColumnWidthSharingScope scope = ColumnWidthSharingScope;
            if (scope == null)
            {
                return;
            }

            scope.UnregisterGrid(this);
            scope.RegisterGrid(this);
        }
    }
}
