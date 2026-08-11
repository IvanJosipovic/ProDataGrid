# Hierarchical data

Generated hierarchy support keeps the domain item type canonical while adapting ProDataGrid's flattened `HierarchicalNode` rows. It covers children/expansion metadata, wrapper-aware compiled bindings, DynamicData roots, filtering, stable-key state, and transactional asynchronous expansion.

## Declare the hierarchy

```csharp
[GenerateDataGridColumns(
    ProviderName = "NodeSchema",
    Discovery = DataGridColumnDiscovery.AttributedOnly,
    HierarchicalRows = true,
    Strict = true)]
public sealed class Node
{
    [DataGridKey]
    [DataGridColumn(DataGridColumnKind.Numeric,
        Header = "ID", ColumnKey = "id", IsReadOnly = true)]
    public int Id { get; init; }

    [DataGridColumn(DataGridColumnKind.Hierarchical,
        Header = "Name", ColumnKey = "name", Width = "2*")]
    public string Name { get; set; } = string.Empty;

    [DataGridChildren(LoaderMethod = nameof(LoadChildrenAsync))]
    public ObservableCollection<Node> Children { get; } = new();

    [DataGridExpanded]
    public bool IsExpanded { get; set; }

    [DataGridParentKey]
    public int? ParentId { get; init; }

    private ValueTask<IReadOnlyList<Node>> LoadChildrenAsync(
        CancellationToken cancellationToken) =>
        nodeService.LoadChildrenAsync(Id, cancellationToken);
}
```

One property may carry `[DataGridChildren]`. `[DataGridExpanded]` identifies an optional writable Boolean expansion property. `[DataGridParentKey]` provides optional direct parent identity for keyed operations. Invalid or ambiguous members report `PDGSG109`.

## Domain rows and flattened rows

With `HierarchicalRows = true`:

- the manifest and all data-operation accessors remain typed to `TItem`;
- generated column bindings are typed to `HierarchicalNode`;
- binding delegates unwrap `node.Item` directly;
- generated sort paths are prefixed with `Item.` where a path identifier is required.

This removes the need to disable compiled XAML bindings for hierarchical pages. Filtering, searching, selection, state, and DynamicData continue to use the domain item contract.

## Create the model and adapter

```csharp
HierarchicalModel<Node> hierarchy = NodeSchema.CreateHierarchicalModel();
DataGridHierarchicalAdapter<Node> operations =
    NodeSchema.CreateHierarchicalAdapter(hierarchy);
```

Equivalent lower-level construction is available through `CreateHierarchicalOptions()`.

The options include direct children, expansion, loading, and key delegates. The adapter owns coherent hierarchy mutations and expansion operations.

## Bind a generated view

```csharp
[GenerateDataGridViewModel(typeof(Node), ProviderName = "NodeSchema")]
[GenerateDataGridView(
    typeof(Node),
    Framework = DataGridViewFramework.ReactiveUI,
    Recipe = DataGridViewRecipe.Explorer,
    HierarchicalModelPropertyName = nameof(Hierarchy))]
public sealed partial class ExplorerViewModel : ReactiveObject
{
    public ExplorerViewModel()
    {
        Hierarchy = NodeSchema.CreateHierarchicalModel();
        HierarchyOperations = NodeSchema.CreateHierarchicalAdapter(Hierarchy);
    }

    public HierarchicalModel<Node> Hierarchy { get; }
    public DataGridHierarchicalAdapter<Node> HierarchyOperations { get; }
}
```

The generated view binds `DataGrid.HierarchicalModel`, enables hierarchical rows, and adds the `hierarchical` class. It deliberately omits the ordinary root `ItemsSource` binding: the hierarchical model exclusively owns the flattened wrapper list.

## Hierarchy-aware filtering

When a generated view names both hierarchy and filtering models, it creates and installs a `DataGridHierarchicalFilteringAdapterFactory` before the filter binding becomes active:

```csharp
[GenerateDataGridView(
    typeof(Node),
    HierarchicalModelPropertyName = nameof(Hierarchy),
    FilteringModelPropertyName = nameof(Filtering),
    HierarchyFilterPolicy =
        DataGridHierarchyFilterPolicy.KeepAncestorsOfMatches |
        DataGridHierarchyFilterPolicy.KeepDescendantsOfMatches)]
public sealed partial class ExplorerViewModel : ReactiveObject
{
    public HierarchicalModel<Node> Hierarchy { get; }
    public FilteringModel Filtering { get; }
}
```

The default policy is `KeepAncestorsOfMatches`, which preserves the path required to reach each matching item. Add `KeepDescendantsOfMatches` when a matching group/node should retain its subtree.

Schema factories are also available directly:

```csharp
DataGridHierarchicalFilteringAdapter<Node> adapter =
    NodeSchema.CreateHierarchicalFilteringAdapter(
        hierarchy,
        filteringModel,
        DataGridHierarchyFilterPolicy.KeepAncestorsOfMatches);
```

An overload/factory accepts before/after refresh callbacks and defaults to strict generated fast-path options. Override `CreateGeneratedHierarchicalFilteringAdapterFactory` in a generated-view base when construction requires application services.

## Transactional asynchronous expansion

Use the adapter's asynchronous bulk operation:

```csharp
await HierarchyOperations.ExpandAllAsync(
    node: null,
    maxDepth: 4,
    cancellationToken);
```

Expansion remains behind the hierarchy's virtualization guard until a coherent visible-node commit is ready. Cancellation prevents an incomplete bulk operation from leaking a sequence of partially refreshed flattened lists.

Collapse and individual expansion continue to use the same adapter/model, so generated and user-triggered operations share one owner.

## DynamicData roots

A named controller can own a DynamicData root source while the generated hierarchical model owns flattening. Use external-pipeline operation ownership for root sorting/filtering/searching and preserve the same generated key selector for hierarchy resets.

The formal `GeneratedHierarchicalDynamicDataPage` sample combines:

- a generated strict hierarchical schema;
- DynamicData root updates;
- generated hierarchy model and adapter factories;
- asynchronous expand-all;
- ancestor/descendant-aware filtering;
- wrapper-aware compiled columns;
- deterministic disposal.

## Stable selection and state

Hierarchy selection and persisted expansion state reuse the schema key. Do not introduce a separate node identity convention in the ViewModel.

For state restoration, set an explicit `SchemaId` and increment `StateVersion` when hierarchy-relevant persisted metadata changes. Use `PreviousColumnKeys` for renamed columns.

## Direct hierarchical cell options

Hierarchical columns can opt into optimized realization:

```csharp
[DataGridColumn(DataGridColumnKind.Hierarchical,
    UseDirectCell = true,
    UseDirectTextContent = true,
    UseOptimizedPresenter = true,
    TrackDirectTextValueChanges = false)]
public string Name { get; init; } = string.Empty;
```

Disable value tracking only when the displayed value is immutable or the row is replaced/recycled on change. Applying hierarchical-only options to another column kind reports `PDGSG009`.

## Customization boundaries

Generated hierarchy code owns typed metadata and standard adapters. User code continues to own:

- domain loading and authorization;
- retry policy and error presentation;
- domain-specific sort/filter semantics beyond generated field comparisons;
- drag/drop validation and mutation;
- application-specific hierarchy filtering construction through the protected factory override.

## Related articles

- [Hierarchical data runtime guide](../hierarchical-data.md)
- [Hierarchical model end-to-end](../hierarchical-model-end-to-end.md)
- [Hierarchical high-frequency updates](../hierarchical-high-frequency-updates.md)
- [Reactive, streaming, and remote data](reactive-streaming-remote.md)
