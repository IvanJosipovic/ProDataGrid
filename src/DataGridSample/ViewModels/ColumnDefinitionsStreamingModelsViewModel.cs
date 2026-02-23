using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.DataGridFiltering;
using Avalonia.Controls.DataGridSearching;
using Avalonia.Controls.DataGridSorting;
using Avalonia.Threading;
using DataGridSample.Collections;
using DataGridSample.Helpers;
using DataGridSample.Models;
using DataGridSample.Mvvm;

namespace DataGridSample.ViewModels
{
    public class ColumnDefinitionsStreamingModelsViewModel : ObservableObject
    {
        private readonly DispatcherTimer _timer;
        private readonly Random _random = new Random();
        private int _targetCount = 5000;
        private int _batchSize = 50;
        private int _intervalMs = 33;
        private bool _isRunning;
        private long _updates;
        private int _nextId;
        private string _query = string.Empty;
        private int _resultCount;
        private int _currentResultIndex;
        private readonly DataGridColumnDefinition _idColumn;
        private readonly DataGridColumnDefinition _symbolColumn;
        private readonly DataGridColumnDefinition _priceColumn;
        private readonly DataGridColumnDefinition _updatedColumn;

        public ColumnDefinitionsStreamingModelsViewModel()
        {
            Items = new ObservableRangeCollection<StreamingItem>();
            View = new DataGridCollectionView(Items)
            {
                Culture = CultureInfo.InvariantCulture
            };

            FilteringModel = new FilteringModel();
            SearchModel = new SearchModel
            {
                HighlightMode = SearchHighlightMode.TextAndCell,
                HighlightCurrent = true,
                WrapNavigation = true
            };
            SortingModel = new SortingModel
            {
                MultiSort = true,
                CycleMode = SortCycleMode.AscendingDescendingNone,
                OwnsViewSorts = true
            };
            FastPathOptions = new DataGridFastPathOptions
            {
                UseAccessorsOnly = true,
                EnableHighPerformanceSearching = true,
                HighPerformanceSearchTrackItemChanges = false
            };

            SearchModel.ResultsChanged += SearchModelOnResultsChanged;
            SearchModel.CurrentChanged += SearchModelOnCurrentChanged;

            SymbolFilter = new TextFilterContext(
                "Symbol contains",
                apply: text => ApplyTextFilter(_symbolColumn, text),
                clear: () => ClearFilter(_symbolColumn, () => SymbolFilter.Text = string.Empty));

            PriceFilter = new NumberFilterContext(
                "Price between",
                apply: (min, max) => ApplyPriceFilter(min, max),
                clear: () => ClearFilter(_priceColumn, () =>
                {
                    PriceFilter.MinValue = null;
                    PriceFilter.MaxValue = null;
                }))
            {
                Minimum = 0,
                Maximum = 2000
            };

            _idColumn = new DataGridTextColumnDefinition
            {
                Header = "Id",
                Binding = ColumnDefinitionBindingFactory.CreateBinding<StreamingItem, int>(nameof(StreamingItem.Id), s => s.Id),
                SortMemberPath = nameof(StreamingItem.Id),
                Width = new DataGridLength(0.6, DataGridLengthUnitType.Star)
            };
            _symbolColumn = new DataGridTextColumnDefinition
            {
                Header = "Symbol",
                Binding = ColumnDefinitionBindingFactory.CreateBinding<StreamingItem, string>(nameof(StreamingItem.Symbol), s => s.Symbol),
                SortMemberPath = nameof(StreamingItem.Symbol),
                Width = new DataGridLength(1.3, DataGridLengthUnitType.Star)
            };
            _priceColumn = CreatePriceColumnDefinition();
            _updatedColumn = CreateUpdatedColumnDefinition();

            ColumnDefinitions = new ObservableCollection<DataGridColumnDefinition>
            {
                _idColumn,
                _symbolColumn,
                _priceColumn,
                _updatedColumn
            };

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(_intervalMs)
            };
            _timer.Tick += (_, __) => ApplyUpdates();

            StartCommand = new RelayCommand(_ => Start(), _ => !IsRunning);
            StopCommand = new RelayCommand(_ => Stop(), _ => IsRunning);
            ResetCommand = new RelayCommand(_ => ResetItems());
            ClearFiltersCommand = new RelayCommand(_ => ClearFilters());
            ClearSortsCommand = new RelayCommand(_ => SortingModel.Clear());
            NextCommand = new RelayCommand(_ => SearchModel.MoveNext(), _ => SearchModel.Results.Count > 0);
            PreviousCommand = new RelayCommand(_ => SearchModel.MovePrevious(), _ => SearchModel.Results.Count > 0);
            ClearSearchCommand = new RelayCommand(_ => Query = string.Empty);

            ResetItems();
        }

        public ObservableRangeCollection<StreamingItem> Items { get; }

        public DataGridCollectionView View { get; }

        public FilteringModel FilteringModel { get; }

        public SearchModel SearchModel { get; }

        public SortingModel SortingModel { get; }

        public DataGridFastPathOptions FastPathOptions { get; }

        public ObservableCollection<DataGridColumnDefinition> ColumnDefinitions { get; }

        public TextFilterContext SymbolFilter { get; }

        public NumberFilterContext PriceFilter { get; }

        public RelayCommand StartCommand { get; }

        public RelayCommand StopCommand { get; }

        public RelayCommand ResetCommand { get; }

        public RelayCommand ClearFiltersCommand { get; }

        public RelayCommand ClearSortsCommand { get; }

        public RelayCommand NextCommand { get; }

        public RelayCommand PreviousCommand { get; }

        public RelayCommand ClearSearchCommand { get; }

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

        public string Query
        {
            get => _query;
            set
            {
                if (SetProperty(ref _query, value))
                {
                    ApplySearch();
                }
            }
        }

        public int ResultCount
        {
            get => _resultCount;
            private set
            {
                if (SetProperty(ref _resultCount, value))
                {
                    OnPropertyChanged(nameof(ResultSummary));
                }
            }
        }

        public int CurrentResultIndex
        {
            get => _currentResultIndex;
            private set
            {
                if (SetProperty(ref _currentResultIndex, value))
                {
                    OnPropertyChanged(nameof(ResultSummary));
                }
            }
        }

        public string ResultSummary => ResultCount == 0
            ? "No results"
            : $"{CurrentResultIndex} of {ResultCount}";

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

        private void ApplyTextFilter(DataGridColumnDefinition columnId, string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                FilteringModel.Remove(columnId);
                return;
            }

            FilteringModel.SetOrUpdate(new FilteringDescriptor(
                columnId: columnId,
                @operator: FilteringOperator.Contains,
                value: text.Trim(),
                stringComparison: StringComparison.OrdinalIgnoreCase));
        }

        private void ApplyPriceFilter(double? min, double? max)
        {
            if (min == null && max == null)
            {
                FilteringModel.Remove(_priceColumn);
                return;
            }

            var lower = min ?? double.MinValue;
            var upper = max ?? double.MaxValue;

            FilteringModel.SetOrUpdate(new FilteringDescriptor(
                columnId: _priceColumn,
                @operator: FilteringOperator.Between,
                values: new object[] { lower, upper }));
        }

        private void ClearFilter(DataGridColumnDefinition columnId, Action reset)
        {
            reset();
            FilteringModel.Remove(columnId);
        }

        private void ClearFilters()
        {
            SymbolFilter.ClearCommand.Execute(null);
            PriceFilter.ClearCommand.Execute(null);
        }

        private void ApplySearch()
        {
            if (string.IsNullOrWhiteSpace(_query))
            {
                SearchModel.Clear();
                return;
            }

            SearchModel.SetOrUpdate(new SearchDescriptor(
                _query.Trim(),
                matchMode: SearchMatchMode.Contains,
                termMode: SearchTermCombineMode.Any,
                scope: SearchScope.AllColumns,
                comparison: StringComparison.OrdinalIgnoreCase));
        }

        private void SearchModelOnResultsChanged(object? sender, SearchResultsChangedEventArgs e)
        {
            ResultCount = SearchModel.Results.Count;
            CurrentResultIndex = SearchModel.CurrentIndex >= 0 ? SearchModel.CurrentIndex + 1 : 0;
            NextCommand.RaiseCanExecuteChanged();
            PreviousCommand.RaiseCanExecuteChanged();
        }

        private void SearchModelOnCurrentChanged(object? sender, SearchCurrentChangedEventArgs e)
        {
            CurrentResultIndex = SearchModel.CurrentIndex >= 0 ? SearchModel.CurrentIndex + 1 : 0;
        }

        private static DataGridColumnDefinition CreatePriceColumnDefinition()
        {
            var priceBinding = ColumnDefinitionBindingFactory.CreateBinding<StreamingItem, double>(nameof(StreamingItem.Price), s => s.Price);
            priceBinding.StringFormat = "{0:F2}";
            return new DataGridNumericColumnDefinition
            {
                Header = "Price",
                Binding = priceBinding,
                SortMemberPath = nameof(StreamingItem.Price),
                Width = new DataGridLength(0.8, DataGridLengthUnitType.Star),
                FormatString = "F2"
            };
        }

        private static DataGridColumnDefinition CreateUpdatedColumnDefinition()
        {
            var updatedBinding = ColumnDefinitionBindingFactory.CreateBinding<StreamingItem, DateTime>(nameof(StreamingItem.UpdatedAt), s => s.UpdatedAt);
            updatedBinding.StringFormat = "{0:T}";
            return new DataGridTextColumnDefinition
            {
                Header = "Updated",
                Binding = updatedBinding,
                SortMemberPath = nameof(StreamingItem.UpdatedAt),
                Width = new DataGridLength(0.9, DataGridLengthUnitType.Star)
            };
        }
    }
}
