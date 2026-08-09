// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
// Exposes a paged DataGridCollectionView as a flat, unpaged enumerable for selection.

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.Collections;

namespace Avalonia.Controls.DataGridSelection
{
    internal sealed class DataGridPagedSelectionSource : IReadOnlyList<object>, IList, INotifyCollectionChanged, IDisposable
    {
        private readonly DataGridCollectionView _view;
        private bool _disposed;
        private int _lastPageIndex;

        public DataGridPagedSelectionSource(DataGridCollectionView view)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _lastPageIndex = _view.PageIndex;
            if (_view is INotifyCollectionChanged incc)
            {
                incc.CollectionChanged += OnViewCollectionChanged;
            }
            _view.PageChanged += OnPageChanged;
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public int Count => _view.ItemCount;

        public object this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return _view.GetGlobalItemAt(index);
            }
            set => throw new NotSupportedException();
        }

        public bool IsReadOnly => true;

        public bool IsFixedSize => true;

        public object SyncRoot => this;

        public bool IsSynchronized => false;

        public IEnumerator<object> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return _view.GetGlobalItemAt(i);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int Add(object value) => throw new NotSupportedException();

        public void Clear() => throw new NotSupportedException();

        public bool Contains(object value) => _view.GetGlobalIndexOf(value) >= 0;

        public int IndexOf(object value) => _view.GetGlobalIndexOf(value);

        public void Insert(int index, object value) => throw new NotSupportedException();

        public void Remove(object value) => throw new NotSupportedException();

        public void RemoveAt(int index) => throw new NotSupportedException();

        public void CopyTo(Array array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            int i = index;
            foreach (var item in this)
            {
                array.SetValue(item, i++);
            }
        }

        private void OnPageChanged(object sender, EventArgs e)
        {
            _lastPageIndex = _view.PageIndex;
        }

        private void OnViewCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // PageIndex is updated before the view publishes its Reset and PageChanged is raised
            // afterward. Detect the page transition here so the global selection source does not
            // discard selections that merely moved off the visible page.
            if (e.Action == NotifyCollectionChangedAction.Reset &&
                (_view.IsPageChanging ||
                 _view.IsPageProjectionChanging ||
                 _view.PageIndex != _lastPageIndex))
            {
                _lastPageIndex = _view.PageIndex;
                return;
            }

            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (_view is INotifyCollectionChanged incc)
            {
                incc.CollectionChanged -= OnViewCollectionChanged;
            }
            _view.PageChanged -= OnPageChanged;

            _disposed = true;
        }
    }
}
