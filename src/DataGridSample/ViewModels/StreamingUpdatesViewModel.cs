using System;
using System.Collections.Generic;
using Avalonia.Threading;
using DataGridSample.Collections;
using DataGridSample.Models;
using DataGridSample.Mvvm;

namespace DataGridSample.ViewModels
{
    public class StreamingUpdatesViewModel : ObservableObject
    {
        private readonly DispatcherTimer _timer;
        private readonly Random _random = new Random();
        private int _targetCount = 10000;
        private int _batchSize = 50;
        private int _intervalMs = 33;
        private bool _isRunning;
        private long _updates;
        private int _nextId;

        public StreamingUpdatesViewModel()
        {
            Items = new ObservableRangeCollection<StreamingItem>();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(_intervalMs)
            };
            _timer.Tick += (_, __) => ApplyUpdates();

            StartCommand = new RelayCommand(_ => Start(), _ => !IsRunning);
            StopCommand = new RelayCommand(_ => Stop(), _ => IsRunning);
            ResetCommand = new RelayCommand(_ => ResetItems());

            ResetItems();
        }

        public ObservableRangeCollection<StreamingItem> Items { get; }

        public RelayCommand StartCommand { get; }
        public RelayCommand StopCommand { get; }
        public RelayCommand ResetCommand { get; }

        public int TargetCount
        {
            get => _targetCount;
            set
            {
                var next = Math.Max(0, value);
                if (SetProperty(ref _targetCount, next) && !IsRunning)
                {
                    ResetItems();
                }
            }
        }

        public int BatchSize
        {
            get => _batchSize;
            set => SetProperty(ref _batchSize, Math.Max(1, value));
        }

        public int IntervalMs
        {
            get => _intervalMs;
            set
            {
                var next = Math.Max(1, value);
                if (SetProperty(ref _intervalMs, next) && _timer.IsEnabled)
                {
                    _timer.Interval = TimeSpan.FromMilliseconds(_intervalMs);
                }
            }
        }

        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                if (SetProperty(ref _isRunning, value))
                {
                    StartCommand.RaiseCanExecuteChanged();
                    StopCommand.RaiseCanExecuteChanged();
                    OnPropertyChanged(nameof(RunState));
                }
            }
        }

        public long Updates
        {
            get => _updates;
            private set => SetProperty(ref _updates, value);
        }

        public int ItemsCount => Items.Count;

        public string RunState => IsRunning ? "Running" : "Stopped";

        private void Start()
        {
            if (IsRunning)
            {
                return;
            }

            _timer.Interval = TimeSpan.FromMilliseconds(_intervalMs);
            _timer.Start();
            IsRunning = true;
        }

        private void Stop()
        {
            if (!IsRunning)
            {
                return;
            }

            _timer.Stop();
            IsRunning = false;
        }

        private void ResetItems()
        {
            Updates = 0;
            _nextId = 0;

            var items = new List<StreamingItem>(_targetCount);
            for (var i = 0; i < _targetCount; i++)
            {
                items.Add(CreateItem());
            }

            Items.ResetWith(items);

            OnPropertyChanged(nameof(ItemsCount));
        }

        private void ApplyUpdates()
        {
            var batch = Math.Max(1, _batchSize);
            var newItems = new List<StreamingItem>(batch);
            for (var i = 0; i < batch; i++)
            {
                newItems.Add(CreateItem());
            }

            Items.AddRange(newItems);

            var removeCount = Items.Count - _targetCount;
            if (removeCount > 0)
            {
                Items.RemoveRange(0, removeCount);
            }

            Updates += batch;
            OnPropertyChanged(nameof(ItemsCount));
        }

        private StreamingItem CreateItem()
        {
            var id = ++_nextId;
            var price = Math.Round(_random.NextDouble() * 1000, 2);
            var updatedAt = DateTime.Now;
            return new StreamingItem
            {
                Id = id,
                Symbol = $"SYM{id % 1000:D3}",
                Price = price,
                UpdatedAt = updatedAt,
                PriceDisplay = price.ToString("F2"),
                UpdatedAtDisplay = updatedAt.ToString("T")
            };
        }
    }
}
