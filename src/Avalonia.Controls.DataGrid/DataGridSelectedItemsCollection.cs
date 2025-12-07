// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

#nullable disable

using System;
using System.ComponentModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace Avalonia.Controls
{
    internal class DataGridSelectedItemsCollection : IList, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private List<object> _oldSelectedItemsCache;
        private List<object> _selectedItemsCache;
        private readonly SortedSet<int> _selectedSlots;

        public DataGridSelectedItemsCollection(DataGrid owningGrid)
        {
            OwningGrid = owningGrid;
            _oldSelectedItemsCache = new List<object>();
            _selectedItemsCache = new List<object>();
            _selectedSlots = new SortedSet<int>();
        }

        public object this[int index]
        {
            get
            {
                if (index < 0 || index >= _selectedSlots.Count)
                {
                    throw DataGridError.DataGrid.ValueMustBeBetween("index", "Index", 0, true, _selectedSlots.Count, false);
                }
                int slot = GetNthSlot(index);
                Debug.Assert(slot > -1);
                return OwningGrid.DataConnection.GetDataItem(OwningGrid.RowIndexFromSlot(slot));
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public bool IsFixedSize
        {
            get
            {
                return false;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public int Add(object dataItem)
        {
            if (OwningGrid.SelectionMode == DataGridSelectionMode.Single)
            {
                throw DataGridError.DataGridSelectedItemsCollection.CannotChangeSelectedItemsCollectionInSingleMode();
            }

            int itemIndex = OwningGrid.DataConnection.IndexOf(dataItem);
            if (itemIndex == -1)
            {
                throw DataGridError.DataGrid.ItemIsNotContainedInTheItemsSource("dataItem");
            }
            Debug.Assert(itemIndex >= 0);

            int slot = OwningGrid.SlotFromRowIndex(itemIndex);
            if (_selectedSlots.Count == 0)
            {
                OwningGrid.SelectedItem = dataItem;
            }
            else
            {
                OwningGrid.SetRowSelection(slot, true /*isSelected*/, false /*setAnchorSlot*/);
            }
            return IndexOf(dataItem);
        }

        public void Clear()
        {
            if (OwningGrid.SelectionMode == DataGridSelectionMode.Single)
            {
                throw DataGridError.DataGridSelectedItemsCollection.CannotChangeSelectedItemsCollectionInSingleMode();
            }

            if (_selectedSlots.Count > 0)
            {
                // Clearing the selection does not reset the potential current cell.
                if (!OwningGrid.CommitEdit(DataGridEditingUnit.Row, true /*exitEditing*/))
                {
                    // Edited value couldn't be committed or aborted
                    return;
                }
                OwningGrid.ClearRowSelection(true /*resetAnchorSlot*/);
            }
        }

        public bool Contains(object dataItem)
        {
            int itemIndex = OwningGrid.DataConnection.IndexOf(dataItem);
            if (itemIndex == -1)
            {
                return false;
            }
            Debug.Assert(itemIndex >= 0);

            return ContainsSlot(OwningGrid.SlotFromRowIndex(itemIndex));
        }

        public int IndexOf(object dataItem)
        {
            int itemIndex = OwningGrid.DataConnection.IndexOf(dataItem);
            if (itemIndex == -1)
            {
                return -1;
            }
            Debug.Assert(itemIndex >= 0);
            int slot = OwningGrid.SlotFromRowIndex(itemIndex);
            if (_selectedSlots.Contains(slot))
            {
                return _selectedSlots.TakeWhile(s => s != slot).Count();
            }

            return -1;
        }

        public void Insert(int index, object dataItem)
        {
            throw new NotSupportedException();
        }

        public void Remove(object dataItem)
        {
            if (OwningGrid.SelectionMode == DataGridSelectionMode.Single)
            {
                throw DataGridError.DataGridSelectedItemsCollection.CannotChangeSelectedItemsCollectionInSingleMode();
            }

            int itemIndex = OwningGrid.DataConnection.IndexOf(dataItem);
            if (itemIndex == -1)
            {
                return;
            }
            Debug.Assert(itemIndex >= 0);

            if (itemIndex == OwningGrid.CurrentSlot &&
                !OwningGrid.CommitEdit(DataGridEditingUnit.Row, true /*exitEditing*/))
            {
                // Edited value couldn't be committed or aborted
                return;
            }

            OwningGrid.SetRowSelection(OwningGrid.SlotFromRowIndex(itemIndex), false /*isSelected*/, false /*setAnchorSlot*/);
        }

        public void RemoveAt(int index)
        {
            if (OwningGrid.SelectionMode == DataGridSelectionMode.Single)
            {
                throw DataGridError.DataGridSelectedItemsCollection.CannotChangeSelectedItemsCollectionInSingleMode();
            }

            if (index < 0 || index >= _selectedSlots.Count)
            {
                throw DataGridError.DataGrid.ValueMustBeBetween("index", "Index", 0, true, _selectedSlots.Count, false);
            }
            int rowIndex = GetNthSlot(index);
            Debug.Assert(rowIndex > -1);

            if (rowIndex == OwningGrid.CurrentSlot &&
                !OwningGrid.CommitEdit(DataGridEditingUnit.Row, true /*exitEditing*/))
            {
                // Edited value couldn't be committed or aborted
                return;
            }

            OwningGrid.SetRowSelection(rowIndex, false /*isSelected*/, false /*setAnchorSlot*/);
        }

        public int Count
        {
            get
            {
                return _selectedSlots.Count;
            }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsSynchronized
        {
            get
            {
                return false;
            }
        }

        public object SyncRoot
        {
            get
            {
                return this;
            }
        }

        public void CopyTo(System.Array array, int index)
        {
            throw new NotImplementedException();
        }

        public IEnumerator GetEnumerator()
        {
            Debug.Assert(OwningGrid != null);
            Debug.Assert(OwningGrid.DataConnection != null);
            Debug.Assert(_selectedSlots != null);

            foreach (int slot in _selectedSlots)
            {
                int rowIndex = OwningGrid.RowIndexFromSlot(slot);
                Debug.Assert(rowIndex > -1);
                yield return OwningGrid.DataConnection.GetDataItem(rowIndex);
            }
        }

        internal DataGrid OwningGrid
        {
            get;
            private set;
        }

        internal List<object> SelectedItemsCache
        {
            get
            {
                return _selectedItemsCache;
            }
            set
            {
                _selectedItemsCache = value ?? new List<object>();
                UpdateIndexes();
            }
        }

        internal void ClearRows()
        {
            _selectedSlots.Clear();
            _selectedItemsCache.Clear();
        }

        internal bool ContainsSlot(int slot)
        {
            return _selectedSlots.Contains(slot);
        }

        internal bool ContainsAll(int startSlot, int endSlot)
        {
            int itemSlot = OwningGrid.RowGroupHeadersTable.GetNextGap(startSlot - 1);
            while (itemSlot <= endSlot)
            {
                int nextRowGroupHeaderSlot = OwningGrid.RowGroupHeadersTable.GetNextIndex(itemSlot);
                int lastItemSlot = nextRowGroupHeaderSlot == -1 ? endSlot : Math.Min(endSlot, nextRowGroupHeaderSlot - 1);
                for (int slot = itemSlot; slot <= lastItemSlot; slot++)
                {
                    if (!_selectedSlots.Contains(slot))
                    {
                        return false;
                    }
                }
                itemSlot = OwningGrid.RowGroupHeadersTable.GetNextGap(lastItemSlot);
            }

            return true;
        }

        // Called when an item is deleted from the ItemsSource as opposed to just being unselected
        internal void Delete(int slot, object item)
        {
            if (_selectedSlots.Contains(slot))
            {
                OwningGrid.SelectionHasChanged = true;
                DeleteSlot(slot);
                _selectedItemsCache.Remove(item);
            }
        }

        internal void DeleteSlot(int slot)
        {
            _selectedSlots.Remove(slot);
            _oldSelectedItemsCache.Remove(OwningGrid.DataConnection.GetDataItem(OwningGrid.RowIndexFromSlot(slot)));
        }

        // Returns the inclusive index count between lowerBound and upperBound of all indexes with the given value
        internal int GetIndexCount(int lowerBound, int upperBound)
        {
            return _selectedSlots.Count(slot => slot >= lowerBound && slot <= upperBound);
        }

        internal IEnumerable<int> GetIndexes()
        {
            return _selectedSlots;
        }

        internal IEnumerable<int> GetSlots(int startSlot)
        {
            foreach (int slot in _selectedSlots)
            {
                if (slot >= startSlot)
                {
                    yield return slot;
                }
            }
        }

        internal SelectionChangedEventArgs GetSelectionChangedEventArgs()
        {
            List<object> addedSelectedItems = new List<object>();
            List<object> removedSelectedItems = new List<object>();
            int previousCount = _oldSelectedItemsCache.Count;

            foreach (int newSlot in _selectedSlots)
            {
                object newItem = OwningGrid.DataConnection.GetDataItem(OwningGrid.RowIndexFromSlot(newSlot));
                if (_oldSelectedItemsCache.Contains(newItem))
                {
                    _oldSelectedItemsCache.Remove(newItem);
                }
                else
                {
                    addedSelectedItems.Add(newItem);
                }
            }

            removedSelectedItems.AddRange(_oldSelectedItemsCache);

            _oldSelectedItemsCache = new List<object>(_selectedItemsCache);

            RaiseCollectionChanged(previousCount, addedSelectedItems, removedSelectedItems);

            return new SelectionChangedEventArgs(DataGrid.SelectionChangedEvent, removedSelectedItems, addedSelectedItems)
            {
                Source = OwningGrid
            };
        }

        private void RaiseCollectionChanged(int oldCount, List<object> addedItems, List<object> removedItems)
        {
            if (CollectionChanged != null)
            {
                if (removedItems.Count > 0)
                {
                    CollectionChanged(this, new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Remove,
                        removedItems,
                        -1));
                }

                if (addedItems.Count > 0)
                {
                    CollectionChanged(this, new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Add,
                        addedItems,
                        -1));
                }
            }

            if (addedItems.Count > 0 || removedItems.Count > 0)
            {
                if (oldCount != _selectedItemsCache.Count)
                {
                    OnPropertyChanged(nameof(Count));
                }

                OnPropertyChanged("Item[]");
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        internal void InsertIndex(int slot)
        {
            if (_selectedSlots.Count == 0)
            {
                return;
            }

            var updated = new SortedSet<int>();
            foreach (var existing in _selectedSlots)
            {
                updated.Add(existing >= slot ? existing + 1 : existing);
            }

            _selectedSlots.Clear();
            foreach (var adjusted in updated)
            {
                _selectedSlots.Add(adjusted);
            }
        }

        internal void SelectSlot(int slot, bool select)
        {
            if (OwningGrid.RowGroupHeadersTable.Contains(slot))
            {
                return;
            }
            if (select)
            {
                if (_selectedSlots.Add(slot))
                {
                    _selectedItemsCache.Add(OwningGrid.DataConnection.GetDataItem(OwningGrid.RowIndexFromSlot(slot)));
                }
            }
            else
            {
                if (_selectedSlots.Remove(slot))
                {
                    _selectedItemsCache.Remove(OwningGrid.DataConnection.GetDataItem(OwningGrid.RowIndexFromSlot(slot)));
                }
            }
        }

        internal void SelectSlots(int startSlot, int endSlot, bool select)
        {
            int itemSlot = OwningGrid.RowGroupHeadersTable.GetNextGap(startSlot - 1);
            int endItemSlot = OwningGrid.RowGroupHeadersTable.GetPreviousGap(endSlot + 1);
            if (select)
            {
                while (itemSlot <= endItemSlot)
                {
                    int nextRowGroupHeaderSlot = OwningGrid.RowGroupHeadersTable.GetNextIndex(itemSlot);
                    int lastItemSlot = nextRowGroupHeaderSlot == -1 ? endItemSlot : Math.Min(endItemSlot, nextRowGroupHeaderSlot - 1);
                    for (int slot = itemSlot; slot <= lastItemSlot; slot++)
                    {
                        SelectSlot(slot, true);
                    }
                    itemSlot = OwningGrid.RowGroupHeadersTable.GetNextGap(lastItemSlot);
                }
            }
            else
            {
                while (itemSlot <= endItemSlot)
                {
                    int nextRowGroupHeaderSlot = OwningGrid.RowGroupHeadersTable.GetNextIndex(itemSlot);
                    int lastItemSlot = nextRowGroupHeaderSlot == -1 ? endItemSlot : Math.Min(endItemSlot, nextRowGroupHeaderSlot - 1);
                    for (int slot = itemSlot; slot <= lastItemSlot; slot++)
                    {
                        SelectSlot(slot, false);
                    }
                    itemSlot = OwningGrid.RowGroupHeadersTable.GetNextGap(lastItemSlot);
                }
            }
        }

        internal void UpdateIndexes()
        {
            _selectedSlots.Clear();
            _oldSelectedItemsCache.Clear();

            if (OwningGrid.DataConnection.DataSource == null)
            {
                if (_selectedItemsCache.Count > 0)
                {
                    OwningGrid.SelectionHasChanged = true;
                    _selectedItemsCache.Clear();
                }
                return;
            }

            List<object> tempSelectedItemsCache = new List<object>();
            foreach (object item in _selectedItemsCache)
            {
                int index = OwningGrid.DataConnection.IndexOf(item);
                if (index != -1)
                {
                    tempSelectedItemsCache.Add(item);
                    _selectedSlots.Add(OwningGrid.SlotFromRowIndex(index));
                }
                else
                {
                    OwningGrid.SelectionHasChanged = true;
                }
            }

            _selectedItemsCache = tempSelectedItemsCache;
            _oldSelectedItemsCache = new List<object>(_selectedItemsCache);
        }

        private int GetNthSlot(int index)
        {
            return _selectedSlots.ElementAt(index);
        }
    }
}
