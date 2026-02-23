// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable disable

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Avalonia.Controls
{
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    sealed class DataGridFastPathOptions : INotifyPropertyChanged
    {
        private bool _useAccessorsOnly;
        private bool _throwOnMissingAccessor;
        private bool _enableHighPerformanceSearching = true;
        private bool _highPerformanceSearchTrackItemChanges = true;

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<DataGridFastPathMissingAccessorEventArgs> MissingAccessor;

        public bool UseAccessorsOnly
        {
            get => _useAccessorsOnly;
            set
            {
                if (SetProperty(ref _useAccessorsOnly, value))
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StrictMode)));
                }
            }
        }

        public bool ThrowOnMissingAccessor
        {
            get => _throwOnMissingAccessor;
            set
            {
                if (SetProperty(ref _throwOnMissingAccessor, value))
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StrictMode)));
                }
            }
        }

        /// <summary>
        /// Enables incremental search result updates optimized for high-frequency collection changes.
        /// Enabled by default.
        /// </summary>
        public bool EnableHighPerformanceSearching
        {
            get => _enableHighPerformanceSearching;
            set => SetProperty(ref _enableHighPerformanceSearching, value);
        }

        /// <summary>
        /// When high-performance searching is enabled, controls whether item property changes are tracked.
        /// Disable this for maximum throughput in append/remove streaming scenarios.
        /// </summary>
        public bool HighPerformanceSearchTrackItemChanges
        {
            get => _highPerformanceSearchTrackItemChanges;
            set => SetProperty(ref _highPerformanceSearchTrackItemChanges, value);
        }

        public bool StrictMode
        {
            get => UseAccessorsOnly && ThrowOnMissingAccessor;
            set
            {
                UseAccessorsOnly = value;
                ThrowOnMissingAccessor = value;
            }
        }

        internal void ReportMissingAccessor(
            DataGridFastPathFeature feature,
            DataGridColumn column,
            object columnId,
            string message)
        {
            MissingAccessor?.Invoke(this, new DataGridFastPathMissingAccessorEventArgs(feature, column, columnId, message));
        }

        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value))
            {
                return false;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }

#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    enum DataGridFastPathFeature
    {
        Filtering,
        Searching,
        Sorting
    }

#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    sealed class DataGridFastPathMissingAccessorEventArgs : EventArgs
    {
        public DataGridFastPathMissingAccessorEventArgs(
            DataGridFastPathFeature feature,
            DataGridColumn column,
            object columnId,
            string message)
        {
            Feature = feature;
            Column = column;
            ColumnId = columnId;
            Message = message;
        }

        public DataGridFastPathFeature Feature { get; }

        public DataGridColumn Column { get; }

        public object ColumnId { get; }

        public string Message { get; }
    }
}
