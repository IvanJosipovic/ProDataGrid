using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using DataGridSample.Collections;
using DataGridSample.Models;
using DataGridSample.Mvvm;

namespace DataGridSample.ViewModels
{
    public class RangeCollectionChangesViewModel : ObservableObject
    {
        private readonly Random _random = new Random();
        private int _seed;
        private int _batchSize = 5;
        private int _insertIndex;
        private int _removeIndex;

        public RangeCollectionChangesViewModel()
        {
            Items = new ObservableRangeCollection<ChangeItem>();
            Events = new ObservableCollection<string>();

            Items.CollectionChanged += OnCollectionChanged;

            AddRangeCommand = new RelayCommand(_ => AddRange());
            InsertRangeCommand = new RelayCommand(_ => InsertRange());
            RemoveRangeCommand = new RelayCommand(_ => RemoveRange());
            ResetCommand = new RelayCommand(_ => ResetItems());

            ResetItems();
        }

        public ObservableRangeCollection<ChangeItem> Items { get; }

        public ObservableCollection<string> Events { get; }

        public RelayCommand AddRangeCommand { get; }
        public RelayCommand InsertRangeCommand { get; }
        public RelayCommand RemoveRangeCommand { get; }
        public RelayCommand ResetCommand { get; }

        public int BatchSize
        {
            get => _batchSize;
            set => SetProperty(ref _batchSize, Math.Max(1, value));
        }

        public int InsertIndex
        {
            get => _insertIndex;
            set => SetProperty(ref _insertIndex, Math.Max(0, value));
        }

        public int RemoveIndex
        {
            get => _removeIndex;
            set => SetProperty(ref _removeIndex, Math.Max(0, value));
        }

        public int ItemsCount => Items.Count;

        private void AddRange()
        {
            Items.AddRange(CreateItems(BatchSize));
        }

        private void InsertRange()
        {
            var index = Math.Clamp(InsertIndex, 0, Items.Count);
            Items.InsertRange(index, CreateItems(BatchSize));
        }

        private void RemoveRange()
        {
            if (Items.Count == 0)
            {
                return;
            }

            var index = Math.Clamp(RemoveIndex, 0, Items.Count - 1);
            var count = Math.Min(BatchSize, Items.Count - index);
            Items.RemoveRange(index, count);
        }

        private void ResetItems()
        {
            _seed = 0;
            Items.ResetWith(CreateItems(12));
            InsertIndex = Math.Min(InsertIndex, Items.Count);
            RemoveIndex = Math.Min(RemoveIndex, Math.Max(0, Items.Count - 1));
            OnPropertyChanged(nameof(ItemsCount));
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(ItemsCount));

            var message = e.Action switch
            {
                NotifyCollectionChangedAction.Add => $"Add: {DescribeCount(e.NewItems)} at {e.NewStartingIndex}",
                NotifyCollectionChangedAction.Remove => $"Remove: {DescribeCount(e.OldItems)} from {e.OldStartingIndex}",
                NotifyCollectionChangedAction.Move => $"Move: {DescribeCount(e.OldItems)} to {e.NewStartingIndex}",
                NotifyCollectionChangedAction.Replace => $"Replace: {DescribeCount(e.OldItems)} -> {DescribeCount(e.NewItems)}",
                NotifyCollectionChangedAction.Reset => "Reset",
                _ => e.Action.ToString()
            };

            Events.Insert(0, message);
            if (Events.Count > 50)
            {
                Events.RemoveAt(Events.Count - 1);
            }
        }

        private IEnumerable<ChangeItem> CreateItems(int count)
        {
            var items = new List<ChangeItem>(count);
            for (var i = 0; i < count; i++)
            {
                items.Add(new ChangeItem
                {
                    Id = ++_seed,
                    Name = $"Item {_seed}",
                    Value = _random.Next(1, 100)
                });
            }

            return items;
        }

        private static string DescribeCount(System.Collections.IList? items)
        {
            return items == null ? "0" : items.Count.ToString();
        }
    }
}
