using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia.Controls.Selection;
using DataGridSample.Mvvm;

namespace DataGridSample.ViewModels;

public class SelectionFastIndexCacheViewModel : ObservableObject
{
    private readonly RowItem _target;
    private string _summary;

    public SelectionFastIndexCacheViewModel()
    {
        Items = CreateItems(50_000);
        _target = Items[Math.Max(0, Items.Count - 5)];
        SelectionModel = new SelectionModel<object> { SingleSelect = false };
        SelectNearEndCommand = new RelayCommand(_ => RunSelectionLoop());
        Summary = $"Rows: {Items.Count:n0} | Target: {_target.Id}";
    }

    public ObservableCollection<RowItem> Items { get; }

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
        Summary = $"Selected item {_target.Id} 100x in {stopwatch.ElapsedMilliseconds} ms using default cache path.";
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

