using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Controls.DataGridFiltering;
using Avalonia.Controls.DataGridSearching;
using Avalonia.Controls.DataGridSorting;
using DataGridSample.Models;
using DynamicData;
using ProDataGrid.SourceGeneration;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using RxVoid = ReactiveUI.Primitives.RxVoid;

namespace DataGridSample.ViewModels;

[GenerateDataGridViewModel(typeof(GeneratedTrade), ProviderName = "GeneratedTradeSchema", Streaming = true)]
[GenerateDataGridView(
    typeof(GeneratedTrade),
    ViewName = "GeneratedReactiveDataGridView",
    ViewNamespace = "DataGridSample.Pages",
    Framework = DataGridViewFramework.ReactiveUI,
    Title = "Generated ReactiveUI + DynamicData view",
    SortingModelPropertyName = nameof(SortingModel),
    FilteringModelPropertyName = nameof(FilteringModel),
    SearchModelPropertyName = nameof(SearchModel),
    SearchTextPropertyName = nameof(Query))]
public sealed partial class GeneratedColumnsDynamicDataViewModel : ReactiveObject, IDisposable
{
    private static readonly string[] s_symbols = ["AVLN", "RXUI", "DDYN", "GRID", "AOT"];
    private static readonly string[] s_desks = ["Warsaw", "London", "New York"];
    private readonly SourceCache<GeneratedTrade, int> _source = new(static trade => trade.Id);
    private readonly BehaviorSubject<IComparer<GeneratedTrade>> _sort;
    private readonly BehaviorSubject<Func<GeneratedTrade, bool>> _filter;
    private readonly BehaviorSubject<Func<GeneratedTrade, bool>> _search;
    private readonly CompositeDisposable _subscriptions = new();
    private readonly Random _random = new(2408);
    private readonly ReadOnlyObservableCollection<GeneratedTrade> _items;
    private int _nextId;

    [Reactive]
    private string _query = string.Empty;

    [Reactive]
    private string _deskFilter = string.Empty;

    [Reactive]
    private decimal _minimumPrice;

    public GeneratedColumnsDynamicDataViewModel()
    {
        SortingModel = new SortingModel
        {
            MultiSort = true,
            CycleMode = SortCycleMode.AscendingDescendingNone,
            OwnsViewSorts = false
        };
        FilteringModel = new FilteringModel { OwnsViewFilter = false };
        SearchModel = new SearchModel
        {
            HighlightMode = SearchHighlightMode.TextAndCell,
            HighlightCurrent = true,
            WrapNavigation = true
        };

        _sort = new BehaviorSubject<IComparer<GeneratedTrade>>(DataGridSchema.CreateSortComparer(Array.Empty<SortingDescriptor>()));
        _filter = new BehaviorSubject<Func<GeneratedTrade, bool>>(DataGridSchema.CreateFilterPredicate(Array.Empty<FilteringDescriptor>()));
        _search = new BehaviorSubject<Func<GeneratedTrade, bool>>(DataGridSchema.CreateSearchPredicate(Array.Empty<SearchDescriptor>()));
        AddStreamingBatchCommand = ReactiveCommand.Create(AddStreamingBatch);
        SortPriceDescendingCommand = ReactiveCommand.Create(SortPriceDescending);
        ClearSortsCommand = ReactiveCommand.Create(ClearSorts);
        ClearFiltersCommand = ReactiveCommand.Create(ClearFilters);

        _subscriptions.Add(_source.Connect()
            .Filter(_filter)
            .Filter(_search)
            .SortAndBind(out _items, _sort, new() { UseReplaceForUpdates = true })
            .Subscribe());

        SortingModel.SortingChanged += OnSortingChanged;
        FilteringModel.FilteringChanged += OnFilteringChanged;
        SearchModel.SearchChanged += OnSearchChanged;

        _subscriptions.Add(this.WhenAnyValue(static viewModel => viewModel.Query)
            .Subscribe(ApplySearch));
        _subscriptions.Add(this.WhenAnyValue(static viewModel => viewModel.DeskFilter, static viewModel => viewModel.MinimumPrice)
            .Subscribe(_ => ApplyFilters()));

        AddTrades(500);
    }

    public ReadOnlyObservableCollection<GeneratedTrade> Items => _items;

    public SortingModel SortingModel { get; }

    public FilteringModel FilteringModel { get; }

    public SearchModel SearchModel { get; }

    public ReactiveCommand<RxVoid, RxVoid> AddStreamingBatchCommand { get; }

    public ReactiveCommand<RxVoid, RxVoid> SortPriceDescendingCommand { get; }

    public ReactiveCommand<RxVoid, RxVoid> ClearSortsCommand { get; }

    public ReactiveCommand<RxVoid, RxVoid> ClearFiltersCommand { get; }

    private void AddStreamingBatch() => AddTrades(50);

    private void SortPriceDescending()
    {
        SortingModel.SetOrUpdate(new SortingDescriptor(
            "trade-price",
            ListSortDirection.Descending,
            nameof(GeneratedTrade.Price)));
    }

    private void ClearSorts() => SortingModel.Clear();

    private void ClearFilters()
    {
        DeskFilter = string.Empty;
        MinimumPrice = 0;
    }

    public void Dispose()
    {
        SortingModel.SortingChanged -= OnSortingChanged;
        FilteringModel.FilteringChanged -= OnFilteringChanged;
        SearchModel.SearchChanged -= OnSearchChanged;
        _subscriptions.Dispose();
        _sort.Dispose();
        _filter.Dispose();
        _search.Dispose();
        _source.Dispose();
    }

    private void AddTrades(int count)
    {
        _source.Edit(updater =>
        {
            for (int index = 0; index < count; index++)
            {
                int id = ++_nextId;
                updater.AddOrUpdate(new GeneratedTrade
                {
                    Id = id,
                    Symbol = s_symbols[id % s_symbols.Length],
                    Desk = s_desks[id % s_desks.Length],
                    Price = 20m + (decimal)(_random.NextDouble() * 180d),
                    Quantity = _random.Next(1, 5000),
                    Timestamp = DateTimeOffset.UtcNow.AddMilliseconds(index)
                });
            }
        });
    }

    private void ApplySearch(string query)
    {
        SearchModel.Clear();
        if (!string.IsNullOrWhiteSpace(query))
        {
            SearchModel.SetOrUpdate(new SearchDescriptor(
                query,
                comparison: StringComparison.OrdinalIgnoreCase));
        }
    }

    private void ApplyFilters()
    {
        using IDisposable update = FilteringModel.DeferRefresh();
        FilteringModel.Clear();
        if (!string.IsNullOrWhiteSpace(DeskFilter))
        {
            FilteringModel.SetOrUpdate(new FilteringDescriptor(
                "trade-desk",
                FilteringOperator.Contains,
                nameof(GeneratedTrade.Desk),
                DeskFilter,
                stringComparison: StringComparison.OrdinalIgnoreCase));
        }

        if (MinimumPrice > 0)
        {
            FilteringModel.SetOrUpdate(new FilteringDescriptor(
                "trade-price",
                FilteringOperator.GreaterThanOrEqual,
                nameof(GeneratedTrade.Price),
                MinimumPrice));
        }
    }

    private void OnSortingChanged(object? sender, SortingChangedEventArgs args) =>
        _sort.OnNext(DataGridSchema.CreateSortComparer(args.NewDescriptors));

    private void OnFilteringChanged(object? sender, FilteringChangedEventArgs args) =>
        _filter.OnNext(DataGridSchema.CreateFilterPredicate(args.NewDescriptors));

    private void OnSearchChanged(object? sender, SearchChangedEventArgs args) =>
        _search.OnNext(DataGridSchema.CreateSearchPredicate(args.NewDescriptors));
}
