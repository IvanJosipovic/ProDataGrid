// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls.DataGridHierarchical;
using Xunit;

namespace Avalonia.Controls.DataGridTests.Hierarchical;

public class ObservableRangeCollectionTests
{
    [Fact]
    public void AddRange_Raises_Add_With_All_Items()
    {
        var collection = new ObservableRangeCollection<int>();
        NotifyCollectionChangedEventArgs? args = null;
        var propertyChanges = new List<string?>();
        ((INotifyPropertyChanged)collection).PropertyChanged += (_, e) => propertyChanges.Add(e.PropertyName);
        collection.CollectionChanged += (_, e) => args = e;

        collection.AddRange(new[] { 1, 2, 3 });

        Assert.NotNull(args);
        Assert.Equal(NotifyCollectionChangedAction.Add, args!.Action);
        Assert.NotNull(args.NewItems);
        Assert.Equal(3, args.NewItems!.Count);
        Assert.Equal(new[] { 1, 2, 3 }, args.NewItems.Cast<int>().ToArray());
        Assert.Equal(0, args.NewStartingIndex);
        Assert.Contains(nameof(collection.Count), propertyChanges);
        Assert.Contains("Item[]", propertyChanges);
        Assert.Equal(new[] { 1, 2, 3 }, collection.ToArray());
    }

    [Fact]
    public void RemoveRange_Raises_Remove_With_All_Items()
    {
        var collection = new ObservableRangeCollection<int>(new[] { 1, 2, 3, 4 });
        NotifyCollectionChangedEventArgs? args = null;
        var propertyChanges = new List<string?>();
        ((INotifyPropertyChanged)collection).PropertyChanged += (_, e) => propertyChanges.Add(e.PropertyName);
        collection.CollectionChanged += (_, e) => args = e;

        collection.RemoveRange(1, 2);

        Assert.NotNull(args);
        Assert.Equal(NotifyCollectionChangedAction.Remove, args!.Action);
        Assert.NotNull(args.OldItems);
        Assert.Equal(2, args.OldItems!.Count);
        Assert.Equal(new[] { 2, 3 }, args.OldItems.Cast<int>().ToArray());
        Assert.Equal(1, args.OldStartingIndex);
        Assert.Contains(nameof(collection.Count), propertyChanges);
        Assert.Contains("Item[]", propertyChanges);
        Assert.Equal(new[] { 1, 4 }, collection.ToArray());
    }

    [Fact]
    public void AddRange_Throws_When_Items_Null()
    {
        var collection = new ObservableRangeCollection<int>();

        Assert.Throws<ArgumentNullException>(() => collection.AddRange(null!));
    }

    [Fact]
    public void AddRange_Empty_Does_Not_Raise_CollectionChanged()
    {
        var collection = new ObservableRangeCollection<int>();
        var raised = false;
        collection.CollectionChanged += (_, __) => raised = true;

        collection.AddRange(Array.Empty<int>());

        Assert.False(raised);
        Assert.Empty(collection);
    }

    [Fact]
    public void AddRange_Materializes_Enumerable_Items()
    {
        var collection = new ObservableRangeCollection<int>();
        IEnumerable<int> items = YieldItems();
        NotifyCollectionChangedEventArgs? args = null;
        collection.CollectionChanged += (_, e) => args = e;

        collection.AddRange(items);

        Assert.Equal(new[] { 1, 2, 3 }, collection.ToArray());
        Assert.NotNull(args);
        Assert.Equal(3, args!.NewItems!.Count);
    }

    [Fact]
    public void InsertRange_Inserts_At_Index_And_Raises_Add()
    {
        var collection = new ObservableRangeCollection<int>(new[] { 1, 4 });
        NotifyCollectionChangedEventArgs? args = null;
        collection.CollectionChanged += (_, e) => args = e;

        collection.InsertRange(1, new[] { 2, 3 });

        Assert.Equal(new[] { 1, 2, 3, 4 }, collection.ToArray());
        Assert.NotNull(args);
        Assert.Equal(NotifyCollectionChangedAction.Add, args!.Action);
        Assert.Equal(1, args.NewStartingIndex);
        Assert.Equal(new[] { 2, 3 }, args.NewItems!.Cast<int>().ToArray());
    }

    [Fact]
    public void InsertRange_Uses_List_For_Notify_When_Items_Are_GenericOnlyList()
    {
        var collection = new ObservableRangeCollection<int>();
        var items = new GenericOnlyList<int> { 7, 8 };
        NotifyCollectionChangedEventArgs? args = null;
        collection.CollectionChanged += (_, e) => args = e;

        collection.InsertRange(0, items);

        Assert.Equal(new[] { 7, 8 }, collection.ToArray());
        Assert.NotNull(args);
        Assert.NotNull(args!.NewItems);
        Assert.Equal(2, args.NewItems!.Count);
        Assert.Equal(new[] { 7, 8 }, args.NewItems.Cast<int>().ToArray());
    }

    [Fact]
    public void InsertRange_Empty_Does_Not_Raise_CollectionChanged()
    {
        var collection = new ObservableRangeCollection<int>(new[] { 1, 2 });
        var raised = false;
        collection.CollectionChanged += (_, __) => raised = true;

        collection.InsertRange(1, Array.Empty<int>());

        Assert.False(raised);
        Assert.Equal(new[] { 1, 2 }, collection.ToArray());
    }

    [Fact]
    public void InsertRange_Throws_When_Items_Null()
    {
        var collection = new ObservableRangeCollection<int>();

        Assert.Throws<ArgumentNullException>(() => collection.InsertRange(0, null!));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void InsertRange_Throws_When_Index_Invalid(int index)
    {
        var collection = new ObservableRangeCollection<int>(new[] { 1, 2 });

        Assert.Throws<ArgumentOutOfRangeException>(() => collection.InsertRange(index, new[] { 3 }));
    }

    [Fact]
    public void GetRange_Returns_Copy_Of_Items()
    {
        var collection = new ObservableRangeCollection<int>(new[] { 1, 2, 3, 4 });

        var range = collection.GetRange(1, 2);

        Assert.Equal(new[] { 2, 3 }, range.ToArray());
        range[0] = 99;
        Assert.Equal(2, collection[1]);
    }

    [Fact]
    public void GetRange_Throws_When_Index_Negative()
    {
        var collection = new ObservableRangeCollection<int>(new[] { 1, 2 });

        Assert.Throws<ArgumentOutOfRangeException>(() => collection.GetRange(-1, 1));
    }

    [Fact]
    public void GetRange_Throws_When_Count_Negative()
    {
        var collection = new ObservableRangeCollection<int>(new[] { 1, 2 });

        Assert.Throws<ArgumentOutOfRangeException>(() => collection.GetRange(0, -1));
    }

    [Fact]
    public void GetRange_Throws_When_End_Beyond_Count()
    {
        var collection = new ObservableRangeCollection<int>(new[] { 1, 2, 3 });

        Assert.Throws<ArgumentOutOfRangeException>(() => collection.GetRange(2, 2));
    }

    [Fact]
    public void RemoveRange_Count_Zero_Does_Not_Raise_CollectionChanged()
    {
        var collection = new ObservableRangeCollection<int>(new[] { 1, 2 });
        var raised = false;
        collection.CollectionChanged += (_, __) => raised = true;

        collection.RemoveRange(0, 0);

        Assert.False(raised);
        Assert.Equal(new[] { 1, 2 }, collection.ToArray());
    }

    [Fact]
    public void RemoveRange_Throws_When_Index_Negative()
    {
        var collection = new ObservableRangeCollection<int>(new[] { 1, 2 });

        Assert.Throws<ArgumentOutOfRangeException>(() => collection.RemoveRange(-1, 1));
    }

    [Fact]
    public void RemoveRange_Throws_When_End_Beyond_Count()
    {
        var collection = new ObservableRangeCollection<int>(new[] { 1, 2 });

        Assert.Throws<ArgumentOutOfRangeException>(() => collection.RemoveRange(1, 2));
    }

    [Fact]
    public void ReplaceRange_WithEqualCounts_RaisesOneReplaceAfterFinalStateIsVisible()
    {
        var collection = new ObservableRangeCollection<int>(new[] { 1, 2, 3, 4 });
        var changes = new List<NotifyCollectionChangedEventArgs>();
        var propertyChanges = new List<string?>();
        int[]? snapshotInEvent = null;
        ((INotifyPropertyChanged)collection).PropertyChanged +=
            (_, e) => propertyChanges.Add(e.PropertyName);
        collection.CollectionChanged += (_, e) =>
        {
            changes.Add(e);
            snapshotInEvent = collection.ToArray();
        };

        collection.ReplaceRange(1, 2, new[] { 20, 30 });

        NotifyCollectionChangedEventArgs change = Assert.Single(changes);
        Assert.Equal(NotifyCollectionChangedAction.Replace, change.Action);
        Assert.Equal(1, change.NewStartingIndex);
        Assert.Equal(new[] { 20, 30 }, change.NewItems!.Cast<int>().ToArray());
        Assert.Equal(new[] { 2, 3 }, change.OldItems!.Cast<int>().ToArray());
        Assert.Equal(new[] { 1, 20, 30, 4 }, snapshotInEvent);
        Assert.Equal(snapshotInEvent, collection.ToArray());
        Assert.Equal(new[] { "Item[]" }, propertyChanges);
    }

    [Fact]
    public void ReplaceRange_WithDifferentCounts_RaisesOneResetAfterFinalStateIsVisible()
    {
        var collection = new ObservableRangeCollection<int>(new[] { 1, 2, 3, 4 });
        var changes = new List<NotifyCollectionChangedEventArgs>();
        int[]? snapshotInEvent = null;
        collection.CollectionChanged += (_, e) =>
        {
            changes.Add(e);
            snapshotInEvent = collection.ToArray();
        };

        collection.ReplaceRange(1, 1, new[] { 20, 30, 40 });

        NotifyCollectionChangedEventArgs change = Assert.Single(changes);
        Assert.Equal(NotifyCollectionChangedAction.Reset, change.Action);
        Assert.Equal(new[] { 1, 20, 30, 40, 3, 4 }, snapshotInEvent);
        Assert.Equal(snapshotInEvent, collection.ToArray());
    }

    [Fact]
    public void ReplaceRange_WithZeroOldCount_RaisesOneAddForInsertedRange()
    {
        var collection = new ObservableRangeCollection<int>(new[] { 1, 4 });
        NotifyCollectionChangedEventArgs? change = null;
        collection.CollectionChanged += (_, e) => change = e;

        collection.ReplaceRange(1, 0, new[] { 2, 3 });

        Assert.NotNull(change);
        Assert.Equal(NotifyCollectionChangedAction.Add, change!.Action);
        Assert.Equal(1, change.NewStartingIndex);
        Assert.Equal(new[] { 2, 3 }, change.NewItems!.Cast<int>().ToArray());
        Assert.Equal(new[] { 1, 2, 3, 4 }, collection.ToArray());
    }

    [Fact]
    public void ReplaceRange_WithNoReplacement_RaisesOneRemoveForOldRange()
    {
        var collection = new ObservableRangeCollection<int>(new[] { 1, 2, 3, 4 });
        NotifyCollectionChangedEventArgs? change = null;
        collection.CollectionChanged += (_, e) => change = e;

        collection.ReplaceRange(1, 2, Array.Empty<int>());

        Assert.NotNull(change);
        Assert.Equal(NotifyCollectionChangedAction.Remove, change!.Action);
        Assert.Equal(1, change.OldStartingIndex);
        Assert.Equal(new[] { 2, 3 }, change.OldItems!.Cast<int>().ToArray());
        Assert.Equal(new[] { 1, 4 }, collection.ToArray());
    }

    [Fact]
    public void ReplaceRange_WithCollectionItself_SnapshotsReplacementBeforeMutation()
    {
        var collection = new ObservableRangeCollection<int>(new[] { 1, 2, 3 });

        collection.ReplaceRange(0, collection.Count, collection);

        Assert.Equal(new[] { 1, 2, 3 }, collection.ToArray());
    }

    [Fact]
    public void ResetWith_Throws_When_Items_Null()
    {
        var collection = new ObservableRangeCollection<int>();

        Assert.Throws<ArgumentNullException>(() => collection.ResetWith(null!));
    }

    [Fact]
    public void ResetWith_Raises_Reset_And_Replaces_Items()
    {
        var collection = new ObservableRangeCollection<int>(new[] { 1, 2 });
        NotifyCollectionChangedEventArgs? args = null;
        var propertyChanges = new List<string?>();
        ((INotifyPropertyChanged)collection).PropertyChanged += (_, e) => propertyChanges.Add(e.PropertyName);
        collection.CollectionChanged += (_, e) => args = e;

        collection.ResetWith(new[] { 3, 4 });

        Assert.Equal(new[] { 3, 4 }, collection.ToArray());
        Assert.NotNull(args);
        Assert.Equal(NotifyCollectionChangedAction.Reset, args!.Action);
        Assert.Contains(nameof(collection.Count), propertyChanges);
        Assert.Contains("Item[]", propertyChanges);
    }

    private sealed class GenericOnlyList<T> : IList<T>
    {
        private readonly List<T> _items = new();

        public T this[int index]
        {
            get => _items[index];
            set => _items[index] = value;
        }

        public int Count => _items.Count;

        public bool IsReadOnly => false;

        public void Add(T item)
        {
            _items.Add(item);
        }

        public void Clear()
        {
            _items.Clear();
        }

        public bool Contains(T item)
        {
            return _items.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _items.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return _items.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            _items.Insert(index, item);
        }

        public bool Remove(T item)
        {
            return _items.Remove(item);
        }

        public void RemoveAt(int index)
        {
            _items.RemoveAt(index);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }
    }

    private static IEnumerable<int> YieldItems()
    {
        yield return 1;
        yield return 2;
        yield return 3;
    }
}
