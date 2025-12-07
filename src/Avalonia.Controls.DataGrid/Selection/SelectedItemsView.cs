// (c) ProDataGrid selection model scaffolding
// IList bridge over ISelectionModel for binding compatibility.

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Controls.Selection;
using System.Linq;

namespace Avalonia.Controls.DataGridSelection
{
    public class SelectedItemsView : IList, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private readonly ISelectionModel _model;

        public SelectedItemsView(ISelectionModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _model.SelectionChanged += OnSelectionChanged;
            _model.SourceReset += (_, __) => RaiseReset();
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        public object this[int index]
        {
            get
            {
                var items = _model.SelectedItems;
                if (index < 0 || index >= items.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
                return items[index];
            }
            set => throw new NotSupportedException();
        }

        public bool IsReadOnly => false;

        public bool IsFixedSize => false;

        public int Count => _model.SelectedItems.Count;

        public object SyncRoot => this;

        public bool IsSynchronized => false;

        public int Add(object value)
        {
            int index = ResolveIndex(value);

            if (_model.SingleSelect)
            {
                _model.SelectedIndex = index;
            }
            else
            {
                _model.Select(index);
            }

            return IndexOf(value);
        }

        public void Clear() => _model.Clear();

        public bool Contains(object value) => IndexOf(value) != -1;

        public int IndexOf(object value)
        {
            var items = _model.SelectedItems;
            for (int i = 0; i < items.Count; i++)
            {
                if (Equals(items[i], value))
                {
                    return i;
                }
            }
            return -1;
        }

        public void Insert(int index, object value) => throw new NotSupportedException();

        public void Remove(object value)
        {
            int index = ResolveIndex(value);
            if (_model.IsSelected(index))
            {
                _model.Deselect(index);
            }
        }

        public void RemoveAt(int index)
        {
            var items = _model.SelectedItems;
            if (index < 0 || index >= items.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            var value = items[index];
            Remove(value);
        }

        public void CopyTo(Array array, int index)
        {
            if (array is null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            int i = index;
            foreach (var item in this)
            {
                array.SetValue(item, i++);
            }
        }

        public IEnumerator GetEnumerator()
        {
            return _model.SelectedItems.GetEnumerator();
        }

        private int ResolveIndex(object value)
        {
            if (_model.Source is IList list)
            {
                return list.IndexOf(value);
            }

            int i = 0;
            if (_model.Source is IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    if (Equals(item, value))
                    {
                        return i;
                    }
                    i++;
                }
            }

            throw new ArgumentException("Item not found in selection model source.", nameof(value));
        }

        private void OnSelectionChanged(object sender, SelectionModelSelectionChangedEventArgs e)
        {
            RaiseDiff(e.SelectedItems?.Cast<object?>(), e.DeselectedItems?.Cast<object?>());
        }

        private void RaiseDiff(IEnumerable<object?>? addedItems, IEnumerable<object?>? removedItems)
        {
            var changed = false;

            if (removedItems != null)
            {
                foreach (var item in removedItems)
                {
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
                    changed = true;
                }
            }

            if (addedItems != null)
            {
                foreach (var item in addedItems)
                {
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
                    changed = true;
                }
            }

            if (changed)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
            }
        }

        private void RaiseReset()
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        }
    }
}
