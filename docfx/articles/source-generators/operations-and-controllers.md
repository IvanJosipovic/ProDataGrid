# Operations and controllers

Generated operations compile sorting, filtering, searching, grouping, summaries, paging, currency, distinct values, and header commands against the canonical typed fields. A controller owns the mutable models and their subscriptions; the schema remains immutable reusable metadata.

## Choose the operation owner

Sorting, filtering, and searching must have exactly one execution owner:

| Owner | Use when | Controller setting |
| --- | --- | --- |
| Collection view | `DataGridCollectionView` owns local operations | `DataGridOperationExecution.View` |
| External pipeline | DynamicData or another local reactive pipeline applies descriptors | `DataGridOperationExecution.ExternalPipeline` |
| Remote provider | A service or database executes the query | `DataGridOperationExecution.Remote` |

The generator rejects combinations that would apply operations twice with `PDGSG104`.

## Typed descriptors

Generated fields create descriptors without property-path strings:

```csharp
SortingDescriptor[] sorting =
[
    TradeSchema.Price.Descending(),
    TradeSchema.Symbol.Ascending()
];

FilteringDescriptor[] filtering =
[
    TradeSchema.Price.GreaterThanOrEqual(100m),
    TradeSchema.Symbol.Contains("AVLN", StringComparison.OrdinalIgnoreCase)
];

SearchDescriptor search = TradeSchema.Symbol.Search(
    "A*",
    SearchMatchMode.Wildcard);
```

`CreateSortComparer`, `CreateFilterPredicate`, and `CreateSearchPredicate` compile descriptor collections through generated accessors. Unknown stable field IDs fail deterministically.

## Schema-owned operation controller

Create a framework-neutral controller directly from the provider:

```csharp
using DataGridGeneratedOperationController<Trade> controller =
    TradeSchema.CreateController(DataGridOperationExecution.ExternalPipeline);

controller.OperationsChanged += (_, args) =>
{
    if ((args.Change & DataGridGeneratedOperationChange.Filtering) != 0)
    {
        filterSubject.OnNext(controller.FilterPredicate);
    }
};

controller.SortingModel.SetOrUpdate(TradeSchema.Price.Descending());
controller.FilteringModel.SetOrUpdate(TradeSchema.Symbol.Contains("AVLN"));
```

The controller owns column definitions, fast-path options, sorting/filtering/search models, compiled delegates, revisions, and model subscriptions. It depends on ProDataGrid operation models, not ReactiveUI or DynamicData.

## Named ViewModel controllers

Use `[GenerateDataGridController]` when a ViewModel should receive a complete named lifetime:

```csharp
[GenerateDataGridController(
    typeof(Trade),
    "Trades",
    SourceMember = nameof(_source),
    SourceKind = DataGridGeneratedSourceKind.DynamicDataSourceCache,
    OperationExecution = DataGridOperationExecution.ExternalPipeline,
    Features = DataGridGeneratedFeatures.Columns |
               DataGridGeneratedFeatures.Operations |
               DataGridGeneratedFeatures.Selection,
    Streaming = true)]
public sealed partial class TradesViewModel : ReactiveObject, IDisposable
{
    private readonly SourceCache<Trade, int> _source =
        new(static trade => trade.Id);

    public TradesViewModel()
    {
        InitializeTrades(CreateTradesController());
        Items = ConnectTradesPipeline(
            ReactiveUI.Primitives.Reactive.Concurrency.AvaloniaScheduler.Instance);
    }

    public ReadOnlyObservableCollection<Trade> Items { get; }

    public void Dispose()
    {
        DisposeTrades();
        _source.Dispose();
    }
}
```

The group contains `Trades`, `CreateTradesController`, `InitializeTrades`, and `DisposeTrades`. Source-specific members are added for DynamicData, streams, or remote queries. Multiple named controllers can coexist on one partial ViewModel.

The `Features` flags let a controller request only the capabilities it owns. `SourceMember`, `SourceKind`, and `OperationExecution` are validated together.

## Controller customization

`ImplementationType` accepts an `IDataGridGeneratedControllerFactory<TItem>`. `ConfigureMethod` names a static method on the partial ViewModel with a `ref DataGridGeneratedControllerOptions<TItem>` parameter.

```csharp
[GenerateDataGridController(
    typeof(Trade),
    "Trades",
    ConfigureMethod = nameof(ConfigureTrades))]
public sealed partial class TradesViewModel
{
    private static void ConfigureTrades(
        ref DataGridGeneratedControllerOptions<Trade> options)
    {
        options.OperationExecution = DataGridOperationExecution.View;
    }
}
```

Invalid factories or configure signatures produce `PDGSG004`.

## Presets and command surfaces

Apply a group of descriptors as one revision:

```csharp
DataGridGeneratedOperationPreset riskPreset = new(
    "High-value AVLN",
    sorting: [TradeSchema.Price.Descending()],
    filtering:
    [
        TradeSchema.Symbol.EqualTo("AVLN"),
        TradeSchema.Price.GreaterThanOrEqual(100m)
    ]);

Trades.ApplyPreset(riskPreset);
```

Shared presets can be declared as validated static factories:

```csharp
[GenerateDataGridColumns(
    OperationPresetMethods = [nameof(CreateRiskPreset)])]
public sealed class Trade
{
    public static DataGridGeneratedOperationPreset CreateRiskPreset() => new(
        "risk",
        sorting: [TradeSchema.Price.Descending()],
        filtering: [TradeSchema.Price.GreaterThanOrEqual(100m)]);
}
```

Preset factories are evaluated lazily once and names must be unique. `TryGetOperationPreset` performs ordinal lookup.

Named controllers expose descriptor projections, operation commands, and presets. The framework-neutral `ICommand` surface includes applying presets, removing descriptors, clearing individual/all operations, and search navigation.

## Collection views, paging, and currency

Configure defaults on the schema:

```csharp
[GenerateDataGridColumns(
    DefaultPageSize = 100,
    InitialPageIndex = 0,
    InitialCurrency = DataGridGeneratedInitialCurrency.First,
    PreserveCurrentItemByKey = true,
    PreserveSelectionByKey = true)]
public sealed class Trade
{
    [DataGridKey]
    public long Id { get; init; }
}
```

`CreateCollectionView` installs generated grouping, paging, and initial currency in one typed factory. A keyed provider also emits `CreateCollectionViewController`. The controller maintains a global key index, identity selection, and sticky current-item key across refreshes, page changes, and replacement instances.

`Refresh`, `SetPageSize`, `ReplaceView`, `Capture`, `Restore`, and `TryMoveCurrentToKey` preserve only keys, not unloaded row instances. Invalid paging/currency defaults report `PDGSG140`.

Use generated comparer-based sorting rather than path lookup:

```csharp
TradeSchema.ApplyCollectionViewSorting(
    view,
    [
        TradeSchema.Symbol.Ascending(),
        TradeSchema.Price.Descending(customPriceComparer)
    ]);
```

Passing an empty descriptor list clears sorting.

## Typed grouping

`[DataGridGroup]` creates ordered fields and direct group descriptions:

```csharp
[DataGridColumn(Header = "Region", ColumnKey = "region")]
[DataGridGroup(Order = 0)]
public string Region { get; init; } = string.Empty;
```

`CreateCollectionView` installs the group descriptions without `DataGridPathGroupDescription`. An optional `FormatterMethod` customizes the generated group label through a validated static delegate.

## Rendered and incremental summaries

```csharp
[DataGridColumn(DataGridColumnKind.Numeric,
    Header = "Revenue", ColumnKey = "revenue",
    SummaryCellThemeKey = "RevenueSummaryCellTheme")]
[DataGridSummary(
    DataGridAggregateType.Sum,
    Scope = DataGridSummaryScope.Both,
    Format = "C2",
    Title = "Revenue: ")]
public decimal Revenue { get; init; }
```

Each summary produces:

- an incremental `IDataGridGeneratedSummary<TItem>`;
- a `DataGridSummaryDefinition` attached to the generated column;
- fresh runtime `DataGridSummaryDescription` instances for each materialized grid.

Built-in sum, average, count, distinct-count, minimum, maximum, first, and last calculations use the generated accessor. Incremental aggregates support `Add`, `Remove`, `Replace`, and `Reset`.

Generated views can enable summary surfaces:

```csharp
[GenerateDataGridView(
    typeof(Order),
    ShowTotalSummary = true,
    ShowGroupSummary = true,
    TotalSummaryPosition = DataGridSummaryRowPosition.Bottom,
    GroupSummaryPosition = DataGridGroupSummaryPosition.Footer)]
public sealed partial class OrdersViewModel { }
```

For custom calculations, assign `DataGridSummaryDefinition.Factory` from a column configure method. The factory must return a new description per materialized column.

## Filter editor metadata and distinct values

`DataGridColumn.FilterEditor` can be `Text`, `Numeric`, `DateTime`, `Boolean`, `Enum`, `Range`, `Distinct`, or `Custom`. `FilterFlyoutKey` leaves visual composition in Avalonia resources.

Every generated field provides:

- a bounded local distinct-value provider;
- a cancellable, revisioned remote distinct-value controller;
- stable backend-field metadata for server translation.

Local scans require source/result limits. A remote controller cancels prior work, rejects stale responses, and publishes values/loading/error state.

## Header commands

`CreateHeaderCommandController` returns one cached command set per stable field. It coordinates:

- ascending/descending/clear sort;
- clear or show filter;
- visibility and layout reset;
- pin, freeze, autosize, and grid-instance operations.

Model-only commands call generated sorting/filtering/layout contracts directly. Filter flyouts use `IFilteringModelInteraction`; grid-instance actions use `IDataGridGeneratedHeaderInteraction`. A generated ReactiveUI interaction handler can own the actual grid without exposing Avalonia controls to the ViewModel.

Command availability follows live sorting, filtering, and layout changes even when another controller initiated them.

## Domain-owned collection mutations and new rows

The generator does not infer domain mutation semantics. Inject focused services:

```csharp
[GenerateDataGridColumns(
    MutationHandlerType = typeof(OrderMutations),
    NewRowFactoryType = typeof(OrderFactory))]
public sealed class Order { }

public sealed class OrderMutations :
    IDataGridGeneratedCollectionMutationHandler<Order>
{
    public ValueTask AddAsync(
        int index,
        ReadOnlyMemory<Order> items,
        CancellationToken token) => SaveAddedAsync(index, items, token);

    public ValueTask RemoveAsync(
        int index,
        ReadOnlyMemory<Order> items,
        CancellationToken token) => DeleteAsync(items, token);

    // ReplaceAsync, MoveAsync, and ResetAsync follow the same range contract.
}

public sealed class OrderFactory : IDataGridGeneratedNewRowFactory<Order>
{
    public ValueTask<Order> CreateAsync(CancellationToken token) =>
        ValueTask.FromResult(new Order());
}
```

`CreateCollectionMutationService(handler)` and `CreateNewRowService(factory)` preserve DI ownership. Configured implementation types additionally enable parameterless `CreateConfigured...` factories. Range operations use one `ReadOnlyMemory<TItem>` and one `ValueTask` per mutation and enforce explicit bounds.

## Related articles

- [Reactive, streaming, and remote data](reactive-streaming-remote.md)
- [Selection, navigation, and state](selection-navigation-state.md)
- [Editing and data workflows](editing-and-data-workflows.md)
