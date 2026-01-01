using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using Avalonia.Controls.DataGridFiltering;
using Avalonia.Controls.DataGridSorting;
using Avalonia.Threading;
using DataGridSample.Adapters;
using DataGridSample.Models;
using DataGridSample.Mvvm;
using DynamicData;

namespace DataGridSample.ViewModels
{
    public class DynamicDataStreamingSourceListViewModel : ObservableObject, IDisposable
    {
        private readonly SourceList<StreamingItem> _source;
        private readonly ReadOnlyObservableCollection<StreamingItem> _view;
        private readonly CompositeDisposable _cleanup = new();
        private readonly BehaviorSubject<IComparer<StreamingItem>> _sortSubject;
        private readonly BehaviorSubject<Func<StreamingItem, bool>> _filterSubject;
        private readonly DynamicDataStreamingSortingAdapterFactory _sortingAdapterFactory;
        private readonly DynamicDataStreamingFilteringAdapterFactory _filteringAdapterFactory;
        private readonly INotifyCollectionChanged _viewNotifications;
        private readonly DispatcherTimer _timer;
        private readonly Random _random = new Random();
        private int _targetCount = 10000;
        private int _batchSize = 50;
        private int _intervalMs = 33;
        private bool _isRunning;
        private long _updates;
        private int _nextId;
        private string? _symbolFilter;
        private double? _minPrice;
        private double? _maxPrice;
        private ISortingModel? _sortingModel;
        private IFilteringModel? _filteringModel;
        private bool _multiSortEnabled = true;
        private SortCycleMode _sortCycleMode = SortCycleMode.AscendingDescendingNone;

        public DynamicDataStreamingSourceListViewModel()
        {
            _source = new SourceList<StreamingItem>();
            _sortingAdapterFactory = new DynamicDataStreamingSortingAdapterFactory(OnUpstreamSortsChanged);
            _filteringAdapterFactory = new DynamicDataStreamingFilteringAdapterFactory(OnUpstreamFiltersChanged);
            _sortSubject = new BehaviorSubject<IComparer<StreamingItem>>(_sortingAdapterFactory.SortComparer);
            _filterSubject = new BehaviorSubject<Func<StreamingItem, bool>>(_filteringAdapterFactory.FilterPredicate);

            var subscription = _source.Connect()
                .Filter(_filterSubject)
                .Sort(_sortSubject)
                .Bind(out _view)
                .Subscribe();
            _cleanup.Add(subscription);

            _viewNotifications = _view;
            _viewNotifications.CollectionChanged += ViewCollectionChanged;

            SortingModel = new SortingModel
            {
                MultiSort = true,
                CycleMode = SortCycleMode.AscendingDescendingNone,
                OwnsViewSorts = true
            };
            SortingModel.SortingChanged += SortingModelOnSortingChanged;

            FilteringModel = new FilteringModel
            {
                OwnsViewFilter = true
            };
            FilteringModel.FilteringChanged += FilteringModelOnFilteringChanged;

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(_intervalMs)
            };
            _timer.Tick += (_, __) => ApplyUpdates();

            StartCommand = new RelayCommand(_ => Start(), _ => !IsRunning);
            StopCommand = new RelayCommand(_ => Stop(), _ => IsRunning);
            ResetCommand = new RelayCommand(_ => ResetItems());
            ClearSortsCommand = new RelayCommand(_ => SortingModel.Clear());
            ClearFiltersCommand = new RelayCommand(_ => ClearFilters());

            ResetItems();
        }

        public ReadOnlyObservableCollection<StreamingItem> View => _view;

        public DynamicDataStreamingSortingAdapterFactory SortingAdapterFactory => _sortingAdapterFactory;

        public DynamicDataStreamingFilteringAdapterFactory FilteringAdapterFactory => _filteringAdapterFactory;

        public ISortingModel SortingModel
        {
            get => _sortingModel!;
            private set => SetProperty(ref _sortingModel, value);
        }

        public IFilteringModel FilteringModel
        {
            get => _filteringModel!;
            private set => SetProperty(ref _filteringModel, value);
        }

        public ObservableCollection<SortDescriptorSummary> SortSummaries { get; } = new();

        public ObservableCollection<FilterDescriptorSummary> FilterSummaries { get; } = new();

        public ObservableCollection<string> UpstreamSorts { get; } = new();

        public ObservableCollection<string> UpstreamFilters { get; } = new();

        public RelayCommand StartCommand { get; }

        public RelayCommand StopCommand { get; }

        public RelayCommand ResetCommand { get; }

        public RelayCommand ClearSortsCommand { get; }

        public RelayCommand ClearFiltersCommand { get; }

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

        public int ItemsCount => _view.Count;

        public string RunState => IsRunning ? "Running" : "Stopped";

        public string? SymbolFilter
        {
            get => _symbolFilter;
            set
            {
                if (SetProperty(ref _symbolFilter, value))
                {
                    ApplySymbolFilter(value);
                }
            }
        }

        public double? MinPrice
        {
            get => _minPrice;
            set
            {
                if (SetProperty(ref _minPrice, value))
                {
                    ApplyPriceFilter();
                }
            }
        }

        public double? MaxPrice
        {
            get => _maxPrice;
            set
            {
                if (SetProperty(ref _maxPrice, value))
                {
                    ApplyPriceFilter();
                }
            }
        }

        public bool MultiSortEnabled
        {
            get => _multiSortEnabled;
            set
            {
                if (SetProperty(ref _multiSortEnabled, value) && SortingModel != null)
                {
                    SortingModel.MultiSort = value;
                }
            }
        }

        public SortCycleMode SortCycleMode
        {
            get => _sortCycleMode;
            set
            {
                if (SetProperty(ref _sortCycleMode, value) && SortingModel != null)
                {
                    SortingModel.CycleMode = value;
                }
            }
        }

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
            _source.Edit(list =>
            {
                list.Clear();
                _nextId = 0;
                var items = new List<StreamingItem>(_targetCount);
                for (var i = 0; i < _targetCount; i++)
                {
                    items.Add(CreateItem());
                }

                list.AddRange(items);
            });

            Updates = 0;
        }

        private void ApplyUpdates()
        {
            var batch = Math.Max(1, _batchSize);

            _source.Edit(list =>
            {
                var additions = new List<StreamingItem>(batch);
                for (var i = 0; i < batch; i++)
                {
                    additions.Add(CreateItem());
                }

                list.AddRange(additions);

                var removeCount = list.Count - _targetCount;
                if (removeCount > 0)
                {
                    list.RemoveRange(0, removeCount);
                }
            });

            Updates += batch;
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

        private void ApplySymbolFilter(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                FilteringModel.Remove(nameof(StreamingItem.Symbol));
                return;
            }

            var descriptor = new FilteringDescriptor(
                nameof(StreamingItem.Symbol),
                FilteringOperator.Contains,
                nameof(StreamingItem.Symbol),
                text.Trim());
            FilteringModel.SetOrUpdate(descriptor);
        }

        private void ApplyPriceFilter()
        {
            var minPrice = _minPrice;
            var maxPrice = _maxPrice;

            if (minPrice == null && maxPrice == null)
            {
                FilteringModel.Remove(nameof(StreamingItem.Price));
                return;
            }

            if (minPrice != null && maxPrice != null)
            {
                var descriptor = new FilteringDescriptor(
                    nameof(StreamingItem.Price),
                    FilteringOperator.Between,
                    nameof(StreamingItem.Price),
                    values: new object[] { minPrice.Value, maxPrice.Value });
                FilteringModel.SetOrUpdate(descriptor);
                return;
            }

            if (minPrice != null)
            {
                var descriptor = new FilteringDescriptor(
                    nameof(StreamingItem.Price),
                    FilteringOperator.GreaterThanOrEqual,
                    nameof(StreamingItem.Price),
                    minPrice.Value);
                FilteringModel.SetOrUpdate(descriptor);
                return;
            }

            if (maxPrice == null)
            {
                FilteringModel.Remove(nameof(StreamingItem.Price));
                return;
            }

            var maxDescriptor = new FilteringDescriptor(
                nameof(StreamingItem.Price),
                FilteringOperator.LessThanOrEqual,
                nameof(StreamingItem.Price),
                maxPrice.Value);
            FilteringModel.SetOrUpdate(maxDescriptor);
        }

        private void ClearFilters()
        {
            SymbolFilter = string.Empty;
            MinPrice = null;
            MaxPrice = null;
            FilteringModel.Clear();
        }

        private void SortingModelOnSortingChanged(object? sender, SortingChangedEventArgs e)
        {
            UpdateSortSummaries(e.NewDescriptors);
            _sortingAdapterFactory.UpdateComparer(e.NewDescriptors);
            _sortSubject.OnNext(_sortingAdapterFactory.SortComparer);
        }

        private void FilteringModelOnFilteringChanged(object? sender, FilteringChangedEventArgs e)
        {
            UpdateFilterSummaries(e.NewDescriptors);
            _filteringAdapterFactory.UpdateFilter(e.NewDescriptors);
            _filterSubject.OnNext(_filteringAdapterFactory.FilterPredicate);
        }

        private void UpdateSortSummaries(IReadOnlyList<SortingDescriptor> descriptors)
        {
            SortSummaries.Clear();
            if (descriptors == null)
            {
                return;
            }

            foreach (var descriptor in descriptors)
            {
                SortSummaries.Add(new SortDescriptorSummary(
                    descriptor.PropertyPath ?? descriptor.ColumnId?.ToString() ?? "(unknown)",
                    descriptor.Direction.ToString()));
            }
        }

        private void UpdateFilterSummaries(IReadOnlyList<FilteringDescriptor> descriptors)
        {
            FilterSummaries.Clear();
            if (descriptors == null)
            {
                return;
            }

            foreach (var descriptor in descriptors.Where(d => d != null))
            {
                FilterSummaries.Add(new FilterDescriptorSummary(
                    descriptor.PropertyPath ?? descriptor.ColumnId?.ToString() ?? "(unknown)",
                    descriptor.Operator.ToString(),
                    descriptor.Values != null ? string.Join(", ", descriptor.Values) : descriptor.Value?.ToString() ?? "(null)"));
            }
        }

        private void OnUpstreamSortsChanged(string description)
        {
            UpstreamSorts.Insert(0, $"{DateTime.Now:HH:mm:ss} {description}");
            while (UpstreamSorts.Count > 12)
            {
                UpstreamSorts.RemoveAt(UpstreamSorts.Count - 1);
            }
        }

        private void OnUpstreamFiltersChanged(string description)
        {
            UpstreamFilters.Insert(0, $"{DateTime.Now:HH:mm:ss} {description}");
            while (UpstreamFilters.Count > 12)
            {
                UpstreamFilters.RemoveAt(UpstreamFilters.Count - 1);
            }
        }

        public void Dispose()
        {
            Stop();
            SortingModel.SortingChanged -= SortingModelOnSortingChanged;
            FilteringModel.FilteringChanged -= FilteringModelOnFilteringChanged;
            _viewNotifications.CollectionChanged -= ViewCollectionChanged;
            _sortSubject.Dispose();
            _filterSubject.Dispose();
            _cleanup.Dispose();
        }

        public record SortDescriptorSummary(string Column, string Direction);

        public record FilterDescriptorSummary(string Column, string Operator, string Value);

        private void ViewCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
            => OnPropertyChanged(nameof(ItemsCount));
    }
}
