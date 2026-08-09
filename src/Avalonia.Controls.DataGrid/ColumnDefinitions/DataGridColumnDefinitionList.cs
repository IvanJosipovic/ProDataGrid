// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace Avalonia.Controls
{
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    sealed class DataGridColumnDefinitionList : ObservableCollection<DataGridColumnDefinition>
    {
        private int _suspendNotifications;
        private bool _hasPendingChanges;
        private int? _frozenColumnCount;
        private int? _frozenColumnCountRight;

        public DataGridColumnDefinitionList()
        {
        }

        public DataGridColumnDefinitionList(IEnumerable<DataGridColumnDefinition> items)
            : base(items)
        {
        }

        /// <summary>Gets or sets the initial number of left-frozen columns applied when this list is bound.</summary>
        public int? FrozenColumnCount
        {
            get => _frozenColumnCount;
            set => _frozenColumnCount = !value.HasValue || value.Value >= 0
                ? value
                : throw new ArgumentOutOfRangeException(nameof(value));
        }

        /// <summary>Gets or sets the initial number of right-frozen columns applied when this list is bound.</summary>
        public int? FrozenColumnCountRight
        {
            get => _frozenColumnCountRight;
            set => _frozenColumnCountRight = !value.HasValue || value.Value >= 0
                ? value
                : throw new ArgumentOutOfRangeException(nameof(value));
        }

        public void AddRange(IEnumerable<DataGridColumnDefinition> items)
        {
            InsertRange(Count, items);
        }

        public IDisposable SuspendNotifications()
        {
            _suspendNotifications++;
            return new NotificationSuspension(this);
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (_suspendNotifications > 0)
            {
                _hasPendingChanges = true;
                return;
            }

            base.OnCollectionChanged(e);
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (_suspendNotifications > 0)
            {
                _hasPendingChanges = true;
                return;
            }

            base.OnPropertyChanged(e);
        }

        private void InsertRange(int index, IEnumerable<DataGridColumnDefinition> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            var materialized = Materialize(items);
            if (materialized.Count == 0)
            {
                return;
            }

            CheckReentrancy();

            for (var i = 0; i < materialized.Count; i++)
            {
                Items.Insert(index + i, materialized[i]);
            }

            if (_suspendNotifications > 0)
            {
                _hasPendingChanges = true;
                return;
            }

            var notifyItems = materialized as IList ?? materialized.ToList();
            RaiseAdd(notifyItems, index);
        }

        private void ResumeNotifications()
        {
            if (_suspendNotifications == 0)
            {
                return;
            }

            _suspendNotifications--;

            if (_suspendNotifications == 0 && _hasPendingChanges)
            {
                _hasPendingChanges = false;
                RaiseReset();
            }
        }

        private void RaiseAdd(IList items, int index)
        {
            base.OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            base.OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            base.OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Add,
                items,
                index));
        }

        private void RaiseReset()
        {
            base.OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            base.OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            base.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        private static IList<DataGridColumnDefinition> Materialize(IEnumerable<DataGridColumnDefinition> items)
        {
            if (items is IList<DataGridColumnDefinition> list)
            {
                return list;
            }

            return items.ToList();
        }

        private sealed class NotificationSuspension : IDisposable
        {
            private DataGridColumnDefinitionList _owner;

            public NotificationSuspension(DataGridColumnDefinitionList owner)
            {
                _owner = owner;
            }

            public void Dispose()
            {
                if (_owner == null)
                {
                    return;
                }

                var owner = _owner;
                _owner = null;
                owner.ResumeNotifications();
            }
        }
    }
}
