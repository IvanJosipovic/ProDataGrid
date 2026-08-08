// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.DataGridFiltering;
using Avalonia.Controls.DataGridSearching;
using Avalonia.Controls.DataGridSorting;
using DataGridSample.Models;
using DataGridSample.Pages;
using DataGridSample.Services;
using ProDataGrid.SourceGeneration;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using RxVoid = ReactiveUI.Primitives.RxVoid;

namespace DataGridSample.ViewModels;

[GenerateDataGridViewModel(
    typeof(GeneratedHeaderFilterRow),
    ProviderName = "GeneratedHeaderFilterRowSchema")]
[GenerateDataGridController(
    typeof(GeneratedHeaderFilterRow),
    "HeaderRows",
    ProviderName = "GeneratedHeaderFilterRowSchema",
    Features = DataGridGeneratedFeatures.Columns |
               DataGridGeneratedFeatures.Sorting |
               DataGridGeneratedFeatures.Filtering |
               DataGridGeneratedFeatures.Searching,
    OperationExecution = DataGridOperationExecution.View)]
[GenerateDataGridView(
    typeof(GeneratedHeaderFilterRow),
    ViewName = "GeneratedHeaderFiltersGrid",
    ViewNamespace = "DataGridSample.Pages",
    Framework = DataGridViewFramework.ReactiveUI,
    Recipe = DataGridViewRecipe.SearchableGrid,
    Title = "Generated header filters",
    AutomationId = "generated-header-filters-grid",
    ControllerName = "HeaderRows",
    SortingModelPropertyName = nameof(SortingModel),
    FilteringModelPropertyName = nameof(FilteringModel),
    SearchModelPropertyName = nameof(SearchModel),
    SearchTextPropertyName = nameof(Query),
    InteractionPropertyNames = [nameof(HeaderGridInteraction)],
    InteractionHandlerTypes = [typeof(GeneratedHeaderCommandInteractionHandler)])]
public sealed partial class GeneratedHeaderFiltersViewModel : ReactiveObject, IDisposable
{
    private readonly ObservableCollection<GeneratedHeaderFilterRow> _items;
    private readonly IDisposable _querySubscription;
    private readonly GeneratedHeaderInteractionAdapter _headerInteraction;
    private readonly DataGridGeneratedRemoteDistinctValueController<string> _remoteDeskController;
    private bool _disposed;

    [Reactive]
    private string _query = string.Empty;

    [Reactive]
    private string _remoteQuery = "o";

    [Reactive]
    private IReadOnlyList<string> _remoteDeskValues = Array.Empty<string>();

    [Reactive]
    private string _remoteStatus = "Remote values are idle; results are bounded to four desks.";

    [Reactive]
    private string _status = "Generated metadata selects seven typed filter editors; Desk owns the live distinct-value flyout.";

    [Reactive]
    private long _remoteRevision;

    [Reactive]
    private bool _lastSlowRequestAccepted;

    public GeneratedHeaderFiltersViewModel()
    {
        _items = CreateRows();
        InitializeHeaderRows(CreateHeaderRowsController());
        SortingModel.MultiSort = true;
        SortingModel.CycleMode = SortCycleMode.AscendingDescendingNone;
        SearchModel.HighlightMode = SearchHighlightMode.TextAndCell;

        ColumnLayoutController = new DataGridGeneratedColumnLayoutController(
            ColumnDefinitions,
            GeneratedHeaderFilterRowSchema.BandFields);
        _headerInteraction = new GeneratedHeaderInteractionAdapter(
            HeaderGridInteraction,
            HeaderInteractionCompleted);
        HeaderCommandController = GeneratedHeaderFilterRowSchema.CreateHeaderCommandController(
            HeaderRows,
            ColumnLayoutController,
            _headerInteraction);
        SymbolHeaderCommands = HeaderCommandController.ForField("symbol");
        DeskHeaderCommands = HeaderCommandController.ForField("desk");
        SideHeaderCommands = HeaderCommandController.ForField("side");
        PriceHeaderCommands = HeaderCommandController.ForField("price");

        LocalDeskValues = GeneratedHeaderFilterRowSchema.DeskDistinctValues.GetValues(
            _items,
            maximumSourceItems: 100,
            maximumResults: 6);
        _remoteDeskController = GeneratedHeaderFilterRowSchema.CreateDeskRemoteDistinctValues(
            new GeneratedDeskDistinctValueProvider());

        ApplyPriceRangeCommand = ReactiveCommand.Create(ApplyPriceRange);
        ApplyActiveBuyFilterCommand = ReactiveCommand.Create(ApplyActiveBuyFilter);
        ClearOperationsCommand = ReactiveCommand.Create(ClearOperations);
        LoadRemoteDeskValuesCommand = ReactiveCommand.CreateFromTask(LoadRemoteDeskValuesAsync);
        RunStaleRemoteRequestCommand = ReactiveCommand.CreateFromTask(RunStaleRemoteRequestAsync);

        _querySubscription = Changed
            .Where(static change => change.PropertyName == nameof(Query))
            .Select(_ => Query)
            .Subscribe(ApplySearch);
    }

    public ObservableCollection<GeneratedHeaderFilterRow> Items => _items;

    public SortingModel SortingModel => HeaderRows.SortingModel;

    public FilteringModel FilteringModel => HeaderRows.FilteringModel;

    public SearchModel SearchModel => HeaderRows.SearchModel;

    public Interaction<DataGridGeneratedHeaderCommandRequest, bool> HeaderGridInteraction { get; } = new();

    public DataGridGeneratedColumnLayoutController ColumnLayoutController { get; }

    public DataGridGeneratedHeaderCommandController<GeneratedHeaderFilterRow> HeaderCommandController { get; }

    public DataGridGeneratedHeaderCommandSet SymbolHeaderCommands { get; }

    public DataGridGeneratedHeaderCommandSet DeskHeaderCommands { get; }

    public DataGridGeneratedHeaderCommandSet SideHeaderCommands { get; }

    public DataGridGeneratedHeaderCommandSet PriceHeaderCommands { get; }

    public IReadOnlyList<string> LocalDeskValues { get; }

    public string EditorSummary =>
        "ID Numeric · Symbol Text · Desk Distinct · Side Enum · Price Range · Timestamp DateTime · Active Boolean";

    public Task HeaderInteractionCompletion => _headerInteraction.LastExecution;

    public ReactiveCommand<RxVoid, RxVoid> ApplyPriceRangeCommand { get; }

    public ReactiveCommand<RxVoid, RxVoid> ApplyActiveBuyFilterCommand { get; }

    public ReactiveCommand<RxVoid, RxVoid> ClearOperationsCommand { get; }

    public ReactiveCommand<RxVoid, RxVoid> LoadRemoteDeskValuesCommand { get; }

    public ReactiveCommand<RxVoid, RxVoid> RunStaleRemoteRequestCommand { get; }

    public async Task LoadRemoteDeskValuesAsync()
    {
        bool accepted = await _remoteDeskController.LoadAsync(RemoteQuery, maximumResults: 4);
        PublishRemoteState(accepted
            ? $"Accepted remote revision {_remoteDeskController.Revision} for '{RemoteQuery}'."
            : $"Remote revision {_remoteDeskController.Revision} was canceled or rejected.");
    }

    public async Task RunStaleRemoteRequestAsync()
    {
        ValueTask<bool> slow = _remoteDeskController.LoadAsync("slow", maximumResults: 4);
        await Task.Yield();
        bool latestAccepted = await _remoteDeskController.LoadAsync(RemoteQuery, maximumResults: 4);
        LastSlowRequestAccepted = await slow;
        PublishRemoteState(
            $"Latest revision accepted: {latestAccepted}; cancellation-resistant stale revision accepted: {LastSlowRequestAccepted}.");
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _querySubscription.Dispose();
        HeaderCommandController.Dispose();
        ColumnLayoutController.Dispose();
        _headerInteraction.Dispose();
        _remoteDeskController.Dispose();
        DisposeHeaderRows();
    }

    private void ApplyPriceRange()
    {
        HeaderRows.SetFiltering([GeneratedHeaderFilterRowSchema.Price.Between(90m, 160m)]);
        Status = $"Applied the generated typed price range at controller revision {HeaderRows.Version}.";
    }

    private void ApplyActiveBuyFilter()
    {
        HeaderRows.SetFiltering(
        [
            GeneratedHeaderFilterRowSchema.IsActive.EqualTo(true),
            GeneratedHeaderFilterRowSchema.Side.EqualTo(GeneratedHeaderFilterSide.Buy)
        ]);
        Status = $"Applied generated Boolean and enum editors at controller revision {HeaderRows.Version}.";
    }

    private void ClearOperations()
    {
        Query = string.Empty;
        HeaderRows.ClearOperations();
        Status = $"Cleared generated header operations at controller revision {HeaderRows.Version}.";
    }

    private void ApplySearch(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            HeaderRows.SetSearching(Array.Empty<SearchDescriptor>());
            return;
        }

        HeaderRows.SetSearching(
        [
            GeneratedHeaderFilterRowSchema.Symbol.Search(query, comparison: StringComparison.OrdinalIgnoreCase),
            GeneratedHeaderFilterRowSchema.Desk.Search(query, comparison: StringComparison.OrdinalIgnoreCase)
        ]);
        Status = $"Compiled header-filter search '{query}' at revision {HeaderRows.Version}.";
    }

    private void HeaderInteractionCompleted(
        DataGridGeneratedHeaderCommandRequest request,
        bool handled,
        Exception? error)
    {
        Status = error is null
            ? $"Generated view interaction {request.Kind} for '{request.ColumnKey}' handled: {handled}."
            : $"Generated view interaction {request.Kind} failed: {error.Message}";
    }

    private void PublishRemoteState(string message)
    {
        RemoteRevision = _remoteDeskController.Revision;
        RemoteDeskValues = _remoteDeskController.Values;
        RemoteStatus = _remoteDeskController.Error is null
            ? message
            : $"Remote distinct-value error: {_remoteDeskController.Error.Message}";
    }

    private static ObservableCollection<GeneratedHeaderFilterRow> CreateRows()
    {
        string[] symbols = ["AVLN", "RXUI", "GRID", "AOT", "FAST", "LIVE"];
        string[] desks = ["Warsaw", "London", "New York", "Singapore"];
        var rows = new ObservableCollection<GeneratedHeaderFilterRow>();
        for (int index = 0; index < 18; index++)
        {
            rows.Add(new GeneratedHeaderFilterRow
            {
                Id = index + 1,
                Symbol = symbols[index % symbols.Length],
                Desk = desks[index % desks.Length],
                Side = index % 3 == 0 ? GeneratedHeaderFilterSide.Sell : GeneratedHeaderFilterSide.Buy,
                Price = 72m + index * 9.25m,
                Timestamp = new DateTimeOffset(2026, 8, 8, 9, index, 0, TimeSpan.Zero),
                IsActive = index % 4 != 0
            });
        }

        return rows;
    }
}
