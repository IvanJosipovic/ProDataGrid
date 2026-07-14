// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable disable

using Avalonia;
using Avalonia.Controls;

namespace Avalonia.Controls.DataGridSizing
{
    /// <summary>
    /// Provides the attached column group used by <see cref="DataGridColumnWidthSharingScope"/>.
    /// </summary>
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    static class DataGridColumnWidthSharing
    {
        /// <summary>
        /// Identifies the Group attached property.
        /// </summary>
        public static readonly AttachedProperty<string> GroupProperty =
            AvaloniaProperty.RegisterAttached<DataGridColumn, string>(
                "Group",
                typeof(DataGridColumnWidthSharing));

        static DataGridColumnWidthSharing()
        {
            GroupProperty.Changed.AddClassHandler<DataGridColumn>(
                (column, args) => column.OwningGrid?.OnColumnWidthSharingGroupChanged(
                    column,
                    args.OldValue as string,
                    args.NewValue as string));
        }

        /// <summary>
        /// Gets the width-sharing group assigned to a column.
        /// </summary>
        /// <param name="target">The target column.</param>
        /// <returns>The group name, or <see langword="null"/> when sharing is disabled.</returns>
        public static string GetGroup(AvaloniaObject target)
        {
            return target.GetValue(GroupProperty);
        }

        /// <summary>
        /// Sets the width-sharing group assigned to a column.
        /// </summary>
        /// <param name="target">The target column.</param>
        /// <param name="value">The group name, or <see langword="null"/> to disable sharing.</param>
        public static void SetGroup(AvaloniaObject target, string value)
        {
            target.SetValue(GroupProperty, value);
        }
    }
}
