using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia.Controls.Selection;
using Avalonia.Utilities;
using DataGridSample.Mvvm;

namespace DataGridSample.ViewModels;

public class SelectionFastIndexResolverViewModel : ObservableObject
{
    private readonly Dictionary<object, int> _referenceIndex;
    private readonly RowItem _target;
    private string _summary;

    public SelectionFastIndexResolverViewModel()
    {
        Items = CreateItems(50_000);
        _referenceIndex = BuildReferenceIndex(Items);
        _target = Items[Math.Max(0, Items.Count - 5)];
        SelectionModel = new SelectionModel<object> { SingleSelect = false };
        ReferenceIndexResolver = ResolveIndex;
        SelectNearEndCommand = new RelayCommand(_ => RunSelectionLoop());
        Summary = $"Rows: {Items.Count:n0} | Target: {_target.Id}";
    }

    public ObservableCollection<RowItem> Items { get; }

    public SelectionModel<object> SelectionModel { get; }

    public Func<IList, object, int> ReferenceIndexResolver { get; }

    public RelayCommand SelectNearEndCommand { get; }

    public string Summary
    {
        get => _summary;
        private set => SetProperty(ref _summary, value);
    }

    private int ResolveIndex(IList _, object item)
    {
        return _referenceIndex.TryGetValue(item, out var index) ? index : -1;
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
        Summary = $"Selected item {_target.Id} 100x in {stopwatch.ElapsedMilliseconds} ms using custom resolver.";
    }

    private static ObservableCollection<RowItem> CreateItems(int count)
    {
        var items = new List<RowItem>(count);
        for (var i = 0; i < count; i++)
        {
            items.Add(new RowItem(i, $"Item {i}"));
        }

        return new ObservableCollection<RowItem>(items);
    }

    private static Dictionary<object, int> BuildReferenceIndex(IList<RowItem> items)
    {
        var index = new Dictionary<object, int>(ReferenceEqualityComparer.Instance);
        for (var i = 0; i < items.Count; i++)
        {
            index[items[i]] = i;
        }

        return index;
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
}

