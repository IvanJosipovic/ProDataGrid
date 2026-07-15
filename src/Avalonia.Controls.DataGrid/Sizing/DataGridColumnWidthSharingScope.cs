// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable disable

using System;
using System.Collections.Generic;

namespace Avalonia.Controls.DataGridSizing
{
    /// <summary>
    /// Synchronizes the display widths of columns that share the same group name across DataGrid controls.
    /// </summary>
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    sealed class DataGridColumnWidthSharingScope
    {
        private readonly Dictionary<string, List<WeakReference<DataGridColumn>>> _groups =
            new(StringComparer.Ordinal);
        private bool _isSynchronizing;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataGridColumnWidthSharingScope"/> class.
        /// </summary>
        public DataGridColumnWidthSharingScope()
        {
        }

        /// <summary>
        /// Recomputes every registered group using its largest current column width.
        /// </summary>
        public void Synchronize()
        {
            if (_isSynchronizing)
            {
                return;
            }

            foreach (string group in new List<string>(_groups.Keys))
            {
                SynchronizeLargestWidth(group);
            }
        }

        internal void RegisterGrid(DataGrid grid)
        {
            if (grid == null)
            {
                return;
            }

            foreach (DataGridColumn column in grid.ColumnsInternal.ItemsInternal)
            {
                RegisterColumn(column);
            }
        }

        internal void UnregisterGrid(DataGrid grid)
        {
            if (grid == null)
            {
                return;
            }

            foreach (string group in new List<string>(_groups.Keys))
            {
                RemoveMatching(group, column => column.OwningGrid == grid);
            }
        }

        internal void RegisterColumn(DataGridColumn column)
        {
            if (column?.OwningGrid == null)
            {
                return;
            }

            string group = DataGridColumnWidthSharing.GetGroup(column);
            if (string.IsNullOrWhiteSpace(group))
            {
                return;
            }

            List<WeakReference<DataGridColumn>> participants = GetOrCreateGroup(group);
            Prune(participants);
            foreach (WeakReference<DataGridColumn> reference in participants)
            {
                if (reference.TryGetTarget(out DataGridColumn existing) && ReferenceEquals(existing, column))
                {
                    return;
                }
            }

            participants.Add(new WeakReference<DataGridColumn>(column));
            SynchronizeLargestWidth(group);
        }

        internal void UnregisterColumn(DataGridColumn column, string group = null)
        {
            if (column == null)
            {
                return;
            }

            group ??= DataGridColumnWidthSharing.GetGroup(column);
            if (!string.IsNullOrWhiteSpace(group))
            {
                RemoveMatching(group, candidate => ReferenceEquals(candidate, column));
            }
        }

        internal void ChangeColumnGroup(DataGridColumn column, string oldGroup, string newGroup)
        {
            UnregisterColumn(column, oldGroup);
            if (!string.IsNullOrWhiteSpace(newGroup))
            {
                RegisterColumn(column);
            }
        }

        internal void ReportWidth(DataGridColumn source)
        {
            if (_isSynchronizing || source?.OwningGrid == null)
            {
                return;
            }

            string group = DataGridColumnWidthSharing.GetGroup(source);
            if (string.IsNullOrWhiteSpace(group) ||
                !_groups.TryGetValue(group, out List<WeakReference<DataGridColumn>> participants))
            {
                return;
            }

            ApplyWidth(participants, source.ActualWidth);
            RemoveGroupIfEmpty(group, participants);
        }

        private void SynchronizeLargestWidth(string group)
        {
            if (!_groups.TryGetValue(group, out List<WeakReference<DataGridColumn>> participants))
            {
                return;
            }

            Prune(participants);
            double width = 0;
            foreach (WeakReference<DataGridColumn> reference in participants)
            {
                if (reference.TryGetTarget(out DataGridColumn column))
                {
                    width = Math.Max(width, column.ActualWidth);
                }
            }

            if (width > 0)
            {
                ApplyWidth(participants, width);
            }

            RemoveGroupIfEmpty(group, participants);
        }

        private void ApplyWidth(List<WeakReference<DataGridColumn>> participants, double requestedWidth)
        {
            if (_isSynchronizing || double.IsNaN(requestedWidth) || double.IsInfinity(requestedWidth))
            {
                return;
            }

            Prune(participants);
            double minimum = 0;
            double maximum = double.PositiveInfinity;
            foreach (WeakReference<DataGridColumn> reference in participants)
            {
                if (reference.TryGetTarget(out DataGridColumn column))
                {
                    minimum = Math.Max(minimum, column.ActualMinWidth);
                    maximum = Math.Min(maximum, column.ActualMaxWidth);
                }
            }

            double sharedWidth = minimum <= maximum
                ? Math.Clamp(requestedWidth, minimum, maximum)
                : requestedWidth;

            _isSynchronizing = true;
            try
            {
                foreach (WeakReference<DataGridColumn> reference in participants)
                {
                    if (!reference.TryGetTarget(out DataGridColumn column) || column.OwningGrid == null)
                    {
                        continue;
                    }

                    double width = minimum <= maximum
                        ? sharedWidth
                        : Math.Clamp(sharedWidth, column.ActualMinWidth, column.ActualMaxWidth);
                    DataGridLength current = column.Width;
                    if (Math.Abs(column.ActualWidth - width) <= 0.001 && !current.IsStar)
                    {
                        continue;
                    }

                    DataGridLength synchronized = current.IsAbsolute || current.IsStar
                        ? new DataGridLength(width)
                        : new DataGridLength(current.Value, current.UnitType, width, width);
                    column.SetWidthInternalNoCallback(synchronized, preserveInheritance: column.InheritsWidth);
                    column.OwningGrid.OnColumnWidthChanged(column);
                }
            }
            finally
            {
                _isSynchronizing = false;
            }
        }

        private List<WeakReference<DataGridColumn>> GetOrCreateGroup(string group)
        {
            if (!_groups.TryGetValue(group, out List<WeakReference<DataGridColumn>> participants))
            {
                participants = new List<WeakReference<DataGridColumn>>();
                _groups.Add(group, participants);
            }

            return participants;
        }

        private void RemoveMatching(string group, Predicate<DataGridColumn> predicate)
        {
            if (!_groups.TryGetValue(group, out List<WeakReference<DataGridColumn>> participants))
            {
                return;
            }

            for (int index = participants.Count - 1; index >= 0; index--)
            {
                if (!participants[index].TryGetTarget(out DataGridColumn column) || predicate(column))
                {
                    participants.RemoveAt(index);
                }
            }

            RemoveGroupIfEmpty(group, participants);
        }

        private static void Prune(List<WeakReference<DataGridColumn>> participants)
        {
            for (int index = participants.Count - 1; index >= 0; index--)
            {
                if (!participants[index].TryGetTarget(out DataGridColumn column) || column.OwningGrid == null)
                {
                    participants.RemoveAt(index);
                }
            }
        }

        private void RemoveGroupIfEmpty(string group, List<WeakReference<DataGridColumn>> participants)
        {
            if (participants.Count == 0)
            {
                _groups.Remove(group);
            }
        }
    }
}
