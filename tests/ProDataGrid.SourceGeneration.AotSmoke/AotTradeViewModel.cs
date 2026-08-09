// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Controls.DataGridFiltering;
using Avalonia.Controls.DataGridSearching;
using Avalonia.Controls.DataGridSorting;
using ProDataGrid.SourceGeneration;
using ReactiveUI;

namespace ProDataGrid.SourceGeneration.AotSmoke;

[GenerateDataGridViewModel(typeof(AotTradeRow), ProviderName = "AotTradeRowSchema")]
[GenerateDataGridController(
    typeof(AotTradeRow),
    "Trades",
    ProviderName = "AotTradeRowSchema",
    Features = DataGridGeneratedFeatures.Columns |
               DataGridGeneratedFeatures.Sorting |
               DataGridGeneratedFeatures.Filtering |
               DataGridGeneratedFeatures.Searching,
    OperationExecution = DataGridOperationExecution.View,
    Strict = true,
    Streaming = true)]
[GenerateDataGridView(
    typeof(AotTradeRow),
    ViewName = "AotGeneratedGridView",
    ViewNamespace = "ProDataGrid.SourceGeneration.AotSmoke.Views",
    Framework = DataGridViewFramework.Avalonia,
    BaseType = typeof(AotGeneratedViewBase),
    Recipe = DataGridViewRecipe.SearchableGrid,
    Title = "NativeAOT generated grid",
    AutomationId = "native-aot-generated-grid",
    ControllerName = "Trades",
    SortingModelPropertyName = nameof(SortingModel),
    FilteringModelPropertyName = nameof(FilteringModel),
    SearchModelPropertyName = nameof(SearchModel),
    SearchTextPropertyName = nameof(Query),
    DiagnosticsStatusPropertyName = nameof(DiagnosticsStatus),
    ViewThemeKey = "AotGeneratedViewTheme",
    DataGridThemeKey = "AotGeneratedDataGridTheme",
    ViewClasses = new[] { "generated-aot-view", "dense" },
    DataGridClasses = new[] { "generated-aot-grid" })]
[GenerateDataGridView(
    typeof(AotTradeRow),
    ViewName = "AotGeneratedReactiveGridView",
    ViewNamespace = "ProDataGrid.SourceGeneration.AotSmoke.Views",
    Framework = DataGridViewFramework.ReactiveUI,
    Recipe = DataGridViewRecipe.OperationsToolbar,
    Title = "NativeAOT generated ReactiveUI grid",
    AutomationId = "native-aot-generated-reactive-grid",
    ControllerName = "Trades",
    SortingModelPropertyName = nameof(SortingModel),
    FilteringModelPropertyName = nameof(FilteringModel),
    SearchModelPropertyName = nameof(SearchModel),
    SearchTextPropertyName = nameof(Query))]
internal sealed partial class AotTradeViewModel : ReactiveObject, IDisposable
{
    private readonly ObservableCollection<AotTradeRow> _items;
    private string _query = string.Empty;
    private bool _disposed;

    public AotTradeViewModel()
    {
        _items =
        [
            new AotTradeRow
            {
                Id = 1,
                Symbol = "AOT",
                Desk = "Warsaw",
                Price = 128.40m,
                Timestamp = new DateTimeOffset(2026, 8, 8, 9, 0, 0, TimeSpan.Zero)
            },
            new AotTradeRow
            {
                Id = 2,
                Symbol = "GRID",
                Desk = "London",
                Price = 94.15m,
                Timestamp = new DateTimeOffset(2026, 8, 8, 9, 1, 0, TimeSpan.Zero)
            }
        ];

        InitializeTrades(CreateTradesController());
    }

    public ObservableCollection<AotTradeRow> Items => _items;

    public SortingModel SortingModel => Trades.SortingModel;

    public FilteringModel FilteringModel => Trades.FilteringModel;

    public SearchModel SearchModel => Trades.SearchModel;

    public string DiagnosticsStatus => $"NativeAOT rows: {_items.Count}";

    public string Query
    {
        get => _query;
        set => this.RaiseAndSetIfChanged(ref _query, value);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        DisposeTrades();
    }
}
