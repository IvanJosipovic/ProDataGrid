# Reactive, streaming, and remote data

Named generated controllers can own DynamicData pipelines, bounded async ingestion, keyed snapshot reconciliation, and remote query lifetimes. Operation descriptors remain canonical; the source kind decides where they execute.

## DynamicData `SourceList`

```csharp
[GenerateDataGridController(
    typeof(Quote),
    "Quotes",
    SourceMember = nameof(_source),
    SourceKind = DataGridGeneratedSourceKind.DynamicDataSourceList,
    OperationExecution = DataGridOperationExecution.ExternalPipeline,
    Streaming = true)]
public sealed partial class QuotesViewModel : ReactiveObject, IDisposable
{
    private readonly SourceList<Quote> _source = new();

    public QuotesViewModel()
    {
        InitializeQuotes(CreateQuotesController());
        Items = ConnectQuotesPipeline(
            ReactiveUI.Primitives.Reactive.Concurrency.AvaloniaScheduler.Instance);
    }

    public ReadOnlyObservableCollection<Quote> Items { get; }

    public void Dispose()
    {
        DisposeQuotes();
        _source.Dispose();
    }
}
```

The emitted pipeline owns:

- one `Connect()` subscription;
- sorting, filtering, and searching propagation;
- replace-aware ordering;
- one optional final scheduler boundary;
- errors and completion;
- idempotent disconnection/disposal.

Batch source edits remain batches. Generated code does not expand changes into per-row callbacks.

## DynamicData `SourceCache`

A cache source requires a generated key whose type matches the source key:

```csharp
[GenerateDataGridController(
    typeof(Trade),
    "Trades",
    SourceMember = nameof(_source),
    SourceKind = DataGridGeneratedSourceKind.DynamicDataSourceCache,
    OperationExecution = DataGridOperationExecution.ExternalPipeline,
    Features = DataGridGeneratedFeatures.Columns |
               DataGridGeneratedFeatures.Operations |
               DataGridGeneratedFeatures.Selection)]
public sealed partial class TradesViewModel : ReactiveObject, IDisposable
{
    private readonly SourceCache<Trade, int> _source =
        new(static trade => trade.Id);
}
```

The pipeline reuses the same identity selector for cache updates, selection, indexes, snapshots, and state. Replacing a selected instance with the same key preserves identity even when the active sort moves it.

## Insert an application transform

`PipelineTransformMethod` runs once after `Connect()` and before generated filtering/sorting:

```csharp
[GenerateDataGridController(
    typeof(Trade),
    "Trades",
    SourceMember = nameof(_source),
    SourceKind = DataGridGeneratedSourceKind.DynamicDataSourceCache,
    OperationExecution = DataGridOperationExecution.ExternalPipeline,
    PipelineTransformMethod = nameof(TransformTrades))]
public sealed partial class TradesViewModel
{
    private IObservable<IChangeSet<Trade, int>> TransformTrades(
        IObservable<IChangeSet<Trade, int>> changes) =>
        changes.Do(_ => SourceBatchCount++);
}
```

The method must accept and return the exact list/cache change-set shape. It may be private, static, or instance. Use it for domain transforms, telemetry, grouping projections, or summary taps—not untyped property access.

## Bounded async streams and channels

For `IAsyncEnumerable<T>` or `ChannelReader<T>`, the controller emits:

- `Run{Name}StreamAsync`;
- `Stop{Name}Stream`;
- `{Name}StreamPump`;
- `{Name}StreamMetrics`.

Ingestion uses a bounded buffer and one callback per drained batch. Choose an overflow policy deliberately:

| Policy | Behavior |
| --- | --- |
| `Wait` | Backpressure the producer until capacity is available. |
| `DropNewest` | Reject the arriving item while full. |
| `DropOldest` | Make room by removing the oldest pending item. |
| `CoalesceByKey` | Retain only the latest pending update for each stable key. |

Disposal cancels active enumeration and wakes blocked writers/readers. Metrics expose accepted, dropped, coalesced, and drained counts.

Keyed schemas also expose lower-level `CreateStreamBuffer` and `CreateAsyncStreamPump` factories for custom application lifetimes.

## Snapshot reconciliation

Use snapshot reconciliation when a service periodically returns a complete keyed snapshot:

```csharp
DataGridGeneratedSnapshotReconciler<Trade, int> reconciler =
    TradeSchema.CreateSnapshotReconciler();

DataGridGeneratedSnapshotMetrics metrics = reconciler.Reconcile(
    visibleTrades,
    serviceSnapshot,
    revision);
```

The reconciler applies keyed add/remove/move/replace changes without clearing the target collection. It validates duplicate keys before mutation and ignores a stale revision. This preserves row containers, selection, current cell, and scroll position more effectively than reset-based updates.

## Remote query controllers

Use a remote source and remote operation ownership:

```csharp
[GenerateDataGridController(
    typeof(Order),
    "Orders",
    ProviderName = "OrderSchema",
    SourceMember = nameof(_provider),
    SourceKind = DataGridGeneratedSourceKind.Remote,
    OperationExecution = DataGridOperationExecution.Remote,
    Features = DataGridGeneratedFeatures.Columns |
               DataGridGeneratedFeatures.Sorting |
               DataGridGeneratedFeatures.Filtering |
               DataGridGeneratedFeatures.Searching)]
public sealed partial class OrdersViewModel : ReactiveObject, IDisposable
{
    private readonly IDataGridQueryProvider<Order, int> _provider;

    private void InitializeQueries()
    {
        InitializeOrders(CreateOrdersController());
        InitializeOrdersRemoteQuery(CreateOrdersRemoteQueryController(
            debounce: TimeSpan.FromMilliseconds(50),
            pageCacheCapacity: 4,
            fieldNameTranslator: TranslateBackendField));
    }
}
```

Generated members include:

- `Create{Name}RemoteQueryController`;
- `Initialize{Name}RemoteQuery` and `Dispose{Name}RemoteQuery`;
- `Query{Name}Async` and `Prefetch{Name}Async`;
- `{Name}RemoteQuery` state.

The controller builds immutable `DataGridRemoteQuery<TItem>` requests from the current descriptors. Network and persistence details stay behind `IDataGridQueryProvider<TItem,TKey>`.

## Revisions, cancellation, and stale responses

Each foreground request receives a monotonically increasing revision. A new request cancels the prior foreground request. A response is accepted only when both its response revision and the controller's current revision match.

This rule also rejects providers that ignore cancellation. A stale or canceled result returns `null` and cannot overwrite visible rows.

`StateChanged` reports loading, error, accepted content, and stale suppression. Generated views can bind these states to loading/empty/error surfaces.

## Paging and cache keys

`DataGridPageRequest` supports offset and opaque-cursor paging. The optional page cache is bounded by entry count and uses caller-provided stable keys. Include every operation/data revision that affects the page:

```csharp
string cacheKey = $"v{Orders.Version}:page:{pageIndex}";
```

`Prefetch{Name}Async` is cache-only: it does not cancel foreground work or change loading/error/content state. It requires a stable cache key and returns `false` if caching is disabled or disposal wins.

## Backend field translation

Generated descriptors carry stable schema field IDs. Configure `BackendFieldName` per column or supply a translator when backend names differ:

```csharp
private static string TranslateBackendField(string stableFieldId) =>
    stableFieldId switch
    {
        "order-id" => "order_id",
        "total" => "gross_total",
        _ => stableFieldId
    };
```

Translation changes query serialization, not schema identity or persisted state.

## Scheduling and lifetime

Generated pipelines perform application operations before the final UI scheduler boundary. The ViewModel selects the scheduler when connecting; the schema and controller remain UI-framework neutral.

Every generated source lifetime has explicit initialize/connect/stop/disconnect/dispose methods. Dispose generated lifetimes from the owning ViewModel. Do not independently subscribe to the same source and operation models unless a separate projection is intentionally required.

## Sample coverage

- `GeneratedDynamicDataSourceListPage`: batched list operations and upstream sort/filter/search.
- `GeneratedDynamicDataSourceCachePage`: keyed replacement and stable selection.
- `GeneratedRemoteQueryPage`: offset paging, cache reuse, field translation, stale suppression, retry, and view-state projection.
- `GeneratedHierarchicalDynamicDataPage`: DynamicData roots and hierarchy operations.

See [samples and production validation](samples-and-production-validation.md) for the complete map.
