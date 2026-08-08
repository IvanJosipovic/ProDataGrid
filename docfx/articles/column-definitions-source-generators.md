# Column Definitions: Source Generators

`ProDataGrid.SourceGenerators` generates column definitions, typed value accessors, compiled Avalonia binding metadata, data-operation delegates, and view-model properties at compile time. The generated path does not inspect item properties with reflection and is suitable for trimming and Native AOT.

## Install

Reference the grid normally and add the generator as a private analyzer dependency:

```xml
<ItemGroup>
  <PackageReference Include="ProDataGrid" />
  <PackageReference Include="ProDataGrid.SourceGenerators"
                    PrivateAssets="all" />
</ItemGroup>
```

The analyzer injects its configuration attributes into the consuming compilation under `ProDataGrid.SourceGeneration`. It does not add a runtime attribute assembly.

## Generate a schema from a model

Annotate the item type and override individual columns where required:

```csharp
using ProDataGrid.SourceGeneration;

[GenerateDataGridColumns(
    ProviderName = "TradeGridSchema",
    SchemaId = "trading/trade/v1",
    Strict = true)]
public sealed class Trade
{
    [DataGridKey]
    [DataGridColumn(DataGridColumnKind.Numeric,
        Header = "ID", ColumnKey = "trade-id", Order = 0, IsReadOnly = true)]
    public int Id { get; init; }

    [DataGridColumn(DataGridColumnKind.Text,
        Header = "Symbol", ColumnKey = "trade-symbol", Order = 1, Width = "2*")]
    public string Symbol { get; init; } = string.Empty;

    [DataGridColumn(DataGridColumnKind.Numeric,
        Header = "Price", ColumnKey = "trade-price", Order = 2, FormatString = "N2")]
    public decimal Price { get; init; }
}
```

`PublicProperties` discovery includes eligible public properties. Use `Discovery = DataGridColumnDiscovery.AttributedOnly` when the model must opt in property by property, and `[DataGridIgnoreColumn]` to exclude a property.

The generated provider implements `IDataGridGeneratedSchema<TItem>`, which is composed from five focused contracts:

- `IDataGridColumnDefinitionProvider<TItem>` creates a new mutable definition list for each grid.
- `IDataGridSortingCompiler<TItem>` compiles sorting descriptors to an item comparer.
- `IDataGridFilteringCompiler<TItem>` compiles filtering descriptors to an item predicate.
- `IDataGridSearchingCompiler<TItem>` compiles search descriptors to an item predicate.
- `IDataGridFastPathOptionsProvider` creates strict accessor-only options.

All operations use generated `DataGridColumnValueAccessor<TItem,TValue>` instances. Descriptor column ids and property paths are keys only; they are never reflected over.

## Canonical manifest, typed fields, and item identity

Every provider also implements `IDataGridGeneratedSchemaManifestProvider`. Its immutable manifest contains a format version, stable schema ID, deterministic shape hash, item/key types, stable field ordinals, column keys, and the compiled accessors used by the provider. Set `SchemaId` explicitly for schemas whose state is persisted or shared across assemblies; otherwise the generator uses the item metadata name plus `/v1`.

Each column is emitted as a strongly typed static field descriptor. Common operations can therefore be built without property-path strings:

```csharp
SortingDescriptor[] sorting =
{
    TradeGridSchema.Price.Descending(),
    TradeGridSchema.Symbol.Ascending()
};

FilteringDescriptor[] filtering =
{
    TradeGridSchema.Price.GreaterThanOrEqual(100m),
    TradeGridSchema.Symbol.Contains("MS", StringComparison.OrdinalIgnoreCase)
};

SearchDescriptor symbolSearch = TradeGridSchema.Symbol.Search(
    "A*",
    SearchMatchMode.Wildcard);
```

`[DataGridKey]` accepts one accessible, non-nullable field or property. The provider implements `IDataGridItemKey<TItem,TKey>`, exposes the default typed key comparer, and creates `DataGridGeneratedItemIndex<TItem,TKey>` instances. The index handles reset, insert, remove, move, and replace changes while preserving allocation-free typed key lookup on reads:

```csharp
DataGridGeneratedItemIndex<Trade, int> index =
    TradeGridSchema.CreateItemIndex(items);

if (index.TryGetIndex(tradeId, out int rowIndex))
{
    // Coordinate selection, state, drag/drop, or chart focus by stable identity.
}
```

The index rejects duplicate keys and captures the key at insertion time, so an accidental key mutation cannot corrupt dictionary lookup. Replacing the item explicitly refreshes its captured key.

The provider also creates a `DataGridGeneratedOperationController<TItem>`. It owns the three operation models, column definitions, fast-path options, compiled delegates, and model subscriptions behind one disposable lifetime. Select `ExternalPipeline` for DynamicData, async-stream, or server adapters; this disables collection-view sort/filter ownership:

```csharp
using DataGridGeneratedOperationController<Trade> controller =
    TradeGridSchema.CreateController(DataGridOperationExecution.ExternalPipeline);

controller.OperationsChanged += (_, args) =>
{
    if ((args.Change & DataGridGeneratedOperationChange.Filtering) != 0)
    {
        filterSubject.OnNext(controller.FilterPredicate);
    }
};

controller.SortingModel.SetOrUpdate(TradeGridSchema.Price.Descending());
controller.FilteringModel.SetOrUpdate(TradeGridSchema.Symbol.Contains("AVLN"));
```

This controller does not depend on ReactiveUI or DynamicData; it deliberately adapts to the existing ProDataGrid operation models. Optional generated reactive adapters consume its single change stream without duplicating descriptor compilation or model event wiring.

For a named, ViewModel-owned controller, use `GenerateDataGridController`. Multiple independently configured grids may be generated on the same partial ViewModel:

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
    private readonly SourceCache<Trade, int> _source = new(static item => item.Id);

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

The generated group contains `Trades`, `InitializeTrades`, `CreateTradesController`, and `DisposeTrades`. A DynamicData source additionally gets a one-owner `ConnectTradesPipeline`/`DisconnectTradesPipeline` pair plus `TradesErrors` and `TradesCompletion`. The generated pipeline applies compiled predicates/comparers upstream, uses `UseReplaceForUpdates`, and performs the optional scheduler hop at the final binding boundary.

`ImplementationType` accepts an `IDataGridGeneratedControllerFactory<TItem>`. `ConfigureMethod` names a static method on the partial ViewModel with a `ref DataGridGeneratedControllerOptions<TItem>` parameter. Both shapes are validated before emission.

## Augment a view model

The view model must be partial. It may inherit any base type, including `ReactiveObject`:

```csharp
using ProDataGrid.SourceGeneration;
using ReactiveUI;

[GenerateDataGridViewModel(typeof(Trade),
    ProviderName = "TradeGridSchema",
    Streaming = true)]
public sealed partial class TradesViewModel : ReactiveObject
{
    public IReadOnlyList<Trade> Items { get; } = LoadTrades();
}
```

The generator adds:

```csharp
public IDataGridGeneratedSchema<Trade> DataGridSchema { get; }
public DataGridColumnDefinitionList ColumnDefinitions { get; }
public DataGridFastPathOptions FastPathOptions { get; }
```

All three property names are configurable. Existing members are preserved and produce `PDGSG006` instead of being overwritten.

Bind the results with compiled XAML bindings:

```xml
<DataGrid ItemsSource="{Binding Items}"
          AutoGenerateColumns="False"
          ColumnDefinitionsSource="{Binding ColumnDefinitions}"
          FastPathOptions="{Binding FastPathOptions}" />
```

`FastPathOptions` is an Avalonia direct property, so no view code is needed to apply generated options.

## Assembly and namespace coverage

Generation can be applied without modifying model source:

```csharp
[assembly: GenerateDataGridColumns(
    typeof(ExternalModels.Trade),
    ProviderName = "ExternalTradeSchema",
    ProviderNamespace = "MyApp.Generated")]

[assembly: GenerateDataGridViewModel(
    typeof(TradesViewModel),
    typeof(ExternalModels.Trade),
    ProviderName = "ExternalTradeSchema")]
```

Use `[assembly: GenerateDataGridColumnsForNamespace("MyApp.Models")]` to cover every eligible model in a namespace. `IncludeNestedNamespaces` controls recursive coverage. `[assembly: GenerateDataGridViewModelsForNamespace("MyApp.ViewModels")]` augments partial view models and infers their item type from the configured `ItemsPropertyName`.

Assembly and namespace requests are merged deterministically with type and property configuration. Provider-name collisions receive stable suffixes.

Add `[assembly: GenerateDataGridRegistry]` when another assembly needs reflection-free schema discovery. The generated `ProDataGrid.Generated.GeneratedProDataGridRegistration` exposes all manifest providers and `TryGetSchema` overloads for item `Type` and stable schema ID. `RegistryNamespace` and `RegistryName` are configurable. When `Microsoft.Extensions.DependencyInjection` is referenced, the registry also emits `AddGeneratedProDataGrids(IServiceCollection)`; Microsoft DI remains optional.

Existing XAML views can participate in the same reflection-free registry:

```csharp
[assembly: DataGridViewRegistration(typeof(TradesViewModel), typeof(TradesView))]

if (GeneratedProDataGridRegistration.TryCreateView(viewModel, out Control? view))
{
    return view;
}
```

The generator validates that each registered view derives from `Control`, has an accessible parameterless constructor, and that each ViewModel has at most one mapping. The generated type switch constructs the view and assigns its `DataContext`; it does not use naming conventions, `Type.GetType`, or `Activator.CreateInstance`.

## Column coverage and customization

`DataGridColumnKind` covers every current definition builder: text, checkbox, hyperlink, image, numeric, progress bar, slider, date picker, time picker, masked text, autocomplete, toggle button, toggle switch, hierarchical, custom drawing, all three combo-box modes, template, button, and formula.

Common options include stable keys, ordering, widths, visibility, editability, sorting, hiding, resizing, reordering, themes, and filter resources. Kind-specific options include numeric limits, increments, masks, templates, formulas, combo-box members, content, and search behavior.

For code-level customization, specify a public static method on the item type:

```csharp
[DataGridColumn(DataGridColumnKind.Text,
    ConfigureMethod = nameof(ConfigureSymbol))]
public string Symbol { get; set; } = string.Empty;

public static void ConfigureSymbol(DataGridTextColumnDefinition column)
{
    column.Watermark = "Ticker";
}
```

`FactoryMethod` replaces construction of one definition. `GenerateDataGridColumns.ConfigureMethod` customizes the completed list.

For full ownership, set `ImplementationType` to a public parameterless implementation of `IDataGridGeneratedSchema<TItem>`. The generated provider becomes a stable facade that forwards every operation to user code.

Canonical field metadata can also declare export/null formatting, backend names, filter-editor profiles, header/description resource keys, accessibility text, and sensitive-data policy. For reflection-free strongly typed localization, use validated static provider methods:

```csharp
[DataGridColumn(
    Header = "Amount",
    Description = "Order amount",
    HeaderProviderMethod = nameof(GetAmountHeader),
    DescriptionProviderMethod = nameof(GetAmountDescription),
    BackendFieldName = "total_amount",
    FilterEditor = DataGridGeneratedFilterEditorKind.Range,
    AutomationId = "amount-cell")]
public decimal Amount { get; set; }

public static string GetAmountHeader(IFormatProvider provider) =>
    TradingResources.Amount;

public static string GetAmountDescription() =>
    TradingResources.AmountDescription;
```

The generated metadata exposes `ResolveHeader` and `ResolveDescription`; generated column creation calls the provider directly with `CurrentUICulture`.

Typed template columns may name static `(TItem, Control?) -> Control` factories for display, edit, and new-row cells. The generator validates every signature and creates recycling templates without runtime XAML loading or reflection.

## DynamicData, async streams, and snapshot reconciliation

The low-level schema compilers remain available for custom pipelines, but named controllers generate the standard `SourceList<T>` and `SourceCache<T,TKey>` pipeline, descriptor subjects, operation propagation, final scheduler boundary, errors, completion, and disposal automatically. `SourceCache` generation requires its key type to match `[DataGridKey]` or `KeyMember`, and generated external pipelines reject view-owned operation execution at compile time.

For `IAsyncEnumerable<T>` or `ChannelReader<T>`, the named controller emits `Run{Name}StreamAsync`, `Stop{Name}Stream`, `{Name}StreamPump`, and `{Name}StreamMetrics`. Ingestion uses a bounded buffer with explicit `Wait`, `DropNewest`, `DropOldest`, or keyed `CoalesceByKey` overflow policy and one callback per drained batch. Disposal cancels active enumeration.

Keyed schemas also expose reusable streaming primitives directly:

```csharp
DataGridGeneratedSnapshotReconciler<Trade, int> snapshots =
    TradeGridSchema.CreateSnapshotReconciler();

DataGridGeneratedSnapshotMetrics result = snapshots.Reconcile(
    visibleTrades,
    serviceSnapshot,
    revision);
```

Snapshot reconciliation applies keyed add/remove/move/replace changes without clearing the target collection, rejects duplicate keys before mutation, and ignores stale revisions. `CreateStreamBuffer` and `CreateAsyncStreamPump` provide the lower-level allocation-conscious APIs for custom adapters.

## Generated hierarchy, selection, and state helpers

Annotate one children property with `[DataGridChildren]`, an optional writable Boolean property with `[DataGridExpanded]`, and an optional parent-key field/property with `[DataGridParentKey]`. The provider emits typed `CreateHierarchicalOptions()` and `GetParentKey()` methods. Invalid or ambiguous hierarchy members produce `PDGSG109`.

Set `HierarchicalRows = true` on `[GenerateDataGridColumns]` when the schema's columns are displayed by a DataGrid using `HierarchicalRowsEnabled`. Canonical manifest accessors remain typed to the item, while column bindings are generated against `HierarchicalNode` and unwrap `node.Item` with compiled delegates. Sort member paths are emitted under `Item.`. This replaces the common `x:CompileBindings="False"` hierarchical-column workaround.

A keyed schema also emits `CreateIdentitySelectionModel()` and `CreateStateOptions(...)`. Both reuse the same typed identity selector as the item index, streams, snapshot reconciliation, and `SourceCache`; persisted state and selection therefore cannot drift onto a different property-key convention.

## Diagnostics

| Code | Meaning |
| --- | --- |
| `PDGSG001` | Target type is unsupported. |
| `PDGSG002` | No eligible columns were found. |
| `PDGSG003` | A property type cannot use the requested column kind. |
| `PDGSG004` | A configuration or factory method is invalid. |
| `PDGSG005` | A generated view model or containing type is not partial. |
| `PDGSG006` | A requested generated member already exists. |
| `PDGSG007` | A custom schema implementation does not satisfy the contract. |
| `PDGSG008` | A namespace request matched no eligible types. |
| `PDGSG009` | Required column configuration is missing. |
| `PDGSG010` | A requested item property is inaccessible. |
| `PDGSG011` | An item property is ambiguous. |
| `PDGSG012` | A generated-view binding member is missing. |
| `PDGSG013` | A generated-view custom base is invalid. |
| `PDGSG014` | A requested generated-view framework is not referenced. |
| `PDGSG100` | A stable column key is empty or duplicated. |
| `PDGSG101` | A `[DataGridKey]` member is invalid, nullable, or ambiguous. |
| `PDGSG103` | A generated controller source member is missing or incompatible. |
| `PDGSG104` | The source kind and operation owner would execute operations twice or in the wrong layer. |
| `PDGSG109` | Generated hierarchy metadata is invalid or ambiguous. |
| `PDGSG117` | A named controller collides with another generated controller. |
| `PDGSG118` | Persisted schema/state metadata is invalid. |
| `PDGSG121` | Formula names or stable-key dependencies are invalid. |

The generator is incremental and emits stable hint names and deterministic column ordering, making generated-source diffs and build caching predictable.

## Generate code-only Avalonia views

Source generators cannot add XAML to Avalonia's XAML compilation pipeline, so `GenerateDataGridView` emits an equivalent C# control tree. It uses Avalonia binding indexers and `CompiledBindingExtension` paths backed by generated `ClrPropertyInfo` delegates; it does not create string-path reflection bindings.

Add view generation alongside view-model generation:

```csharp
[GenerateDataGridViewModel(typeof(Trade), ProviderName = "TradeGridSchema")]
[GenerateDataGridView(
    typeof(Trade),
    ViewName = "TradesView",
    ViewNamespace = "MyApp.Views",
    Title = "Trades",
    SortingModelPropertyName = nameof(SortingModel),
    FilteringModelPropertyName = nameof(FilteringModel),
    SearchModelPropertyName = nameof(SearchModel),
    SearchTextPropertyName = nameof(Query))]
public sealed partial class TradesViewModel : ReactiveObject
{
    // Items and model properties remain ordinary view-model state.
}
```

The generated view contains a title, an optional two-way search box, and a configured `DataGrid`. It binds items, definitions, fast-path options, and any named sorting, filtering, search, selection, and state models. Recipes add stable toolbar and Explorer, spreadsheet, analytics, or master-detail customization slots. The parameterless constructor supports XAML, DI, and view locators; an overload accepts the typed view model directly.

Every generated control uses stable automation IDs derived from `AutomationId`. The grid also receives an accessible name/help text, and the title is exposed as a level-one automation heading. These identifiers are covered by Avalonia Headless tests and do not require visual-tree reflection for test lookup.

### ReactiveUI strategy

Select the ReactiveUI strategy to generate a typed `ReactiveUserControl<TViewModel>`:

```csharp
[GenerateDataGridView(
    typeof(Trade),
    Framework = DataGridViewFramework.ReactiveUI,
    SearchTextPropertyName = nameof(Query))]
```

The generator recognizes properties produced from ReactiveUI.SourceGenerators `[Reactive]` fields even though Roslyn generators cannot consume one another's output. This allows a field such as `[Reactive] private string _query;` to be the source of the generated two-way `Query` binding. The consumer must reference `ReactiveUI.Avalonia` and initialize current ReactiveUI through `UseReactiveUI` during Avalonia startup.

View emission is selected through an internal strategy registry. Avalonia and ReactiveUI are the first two strategies; another UI MVVM integration can be added without changing column, schema, or view-model discovery.

### Custom base classes and view customization

Set `BaseType = typeof(MyGridViewBase)` to use an accessible, non-sealed `UserControl` base with a parameterless constructor. This supports shared styling, activation, services, and application-specific view infrastructure.

Generated views are inheritable and expose these hooks:

```csharp
protected virtual Control CreateGeneratedContent();
protected virtual DataGrid CreateGeneratedDataGrid();
protected virtual void ConfigureGeneratedDataGrid(DataGrid dataGrid);
protected virtual Control? CreateGeneratedToolbar();
protected virtual Control? CreateGeneratedRecipeContent();
```

Subclass the generated view to replace the layout or fully customize the grid while retaining generated compiled bindings. The view model remains UI-framework agnostic.

Views can also be requested externally with assembly attributes:

```csharp
[assembly: GenerateDataGridView(
    typeof(TradesViewModel),
    typeof(Trade),
    Framework = DataGridViewFramework.ReactiveUI)]

[assembly: GenerateDataGridViewsForNamespace("MyApp.ViewModels")]
```

Namespace view generation infers the item type from `ItemsPropertyName`, matching namespace-level view-model augmentation.

A partial ViewModel can receive multiple named schema projections. Give every projection distinct member names:

```csharp
[GenerateDataGridViewModel(
    typeof(MetricRow),
    ColumnDefinitionsPropertyName = "MetricColumns",
    SchemaPropertyName = "MetricSchema",
    FastPathOptionsPropertyName = "MetricFastPath")]
[GenerateDataGridViewModel(
    typeof(ActivityRow),
    ColumnDefinitionsPropertyName = "ActivityColumns",
    SchemaPropertyName = "ActivitySchema",
    FastPathOptionsPropertyName = "ActivityFastPath")]
public sealed partial class DiagnosticsViewModel;
```
