using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Controls.Selection;
using Avalonia.Utilities;
using DataGridSample.Mvvm;

namespace DataGridSample.ViewModels;

public class SelectionFastIndexInterfaceViewModel : ObservableObject
{
    private readonly RowItem _target;
    private string _summary;

    public SelectionFastIndexInterfaceViewModel()
    {
        Items = CreateItems(50_000);
        _target = Items[Math.Max(0, Items.Count - 5)];
        SelectionModel = new SelectionModel<object> { SingleSelect = false };
        SelectNearEndCommand = new RelayCommand(_ => RunSelectionLoop());
        Summary = $"Rows: {Items.Count:n0} | Target: {_target.Id}";
    }

    public FastIndexObservableCollection Items { get; }

    public SelectionModel<object> SelectionModel { get; }

    public RelayCommand SelectNearEndCommand { get; }

    public string Summary
    {
        get => _summary;
        private set => SetProperty(ref _summary, value);
    }

    private void RunSelectionLoop()
    {
        if (SelectionModel.Source == null)
        {
            Summary = "Selection source is not ready yet.";
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        for (var i = 0; i < 100; i++)
        {
            SelectionModel.Clear();
            SelectionModel.Select(_target);
        }

        stopwatch.Stop();
        Summary = $"Selected item {_target.Id} 100x in {stopwatch.ElapsedMilliseconds} ms using IDataGridIndexOf.";
    }

    private static FastIndexObservableCollection CreateItems(int count)
    {
        var items = new List<RowItem>(count);
        for (var i = 0; i < count; i++)
        {
            items.Add(new RowItem(i, $"Item {i}"));
        }

        return new FastIndexObservableCollection(items);
    }

    public sealed class RowItem
    {
        public RowItem(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; }

        public string Name { get; }
    }

    public sealed class FastIndexObservableCollection : ObservableCollection<RowItem>, IDataGridIndexOf
    {
        private readonly Dictionary<object, int> _referenceIndex = new(ReferenceEqualityComparer.Instance);

        public FastIndexObservableCollection(IList<RowItem> items)
            : base(items)
        {
            RebuildReferenceIndex();
        }

        public bool TryGetReferenceIndex(object item, out int index)
        {
            return _referenceIndex.TryGetValue(item, out index);
        }

        protected override void InsertItem(int index, RowItem item)
        {
            base.InsertItem(index, item);
            RebuildReferenceIndex();
        }

        protected override void SetItem(int index, RowItem item)
        {
            base.SetItem(index, item);
            RebuildReferenceIndex();
        }

        protected override void RemoveItem(int index)
        {
            base.RemoveItem(index);
            RebuildReferenceIndex();
        }

        protected override void MoveItem(int oldIndex, int newIndex)
        {
            base.MoveItem(oldIndex, newIndex);
            RebuildReferenceIndex();
        }

        protected override void ClearItems()
        {
            base.ClearItems();
            _referenceIndex.Clear();
        }

        private void RebuildReferenceIndex()
        {
            _referenceIndex.Clear();
            for (var i = 0; i < Count; i++)
            {
                var item = this[i];
                if (!_referenceIndex.ContainsKey(item))
                {
                    _referenceIndex[item] = i;
                }
            }
        }
    }
}
