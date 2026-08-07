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

[GenerateDataGridColumns(ProviderName = "TradeGridSchema", Strict = true)]
public sealed class Trade
{
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

## DynamicData and streaming bypass

Set `OwnsViewSorts` and `OwnsViewFilter` to `false`, then compile model descriptors directly into a DynamicData pipeline:

```csharp
var sorts = new BehaviorSubject<IComparer<Trade>>(
    DataGridSchema.CreateSortComparer(Array.Empty<SortingDescriptor>()));
var filters = new BehaviorSubject<Func<Trade, bool>>(
    DataGridSchema.CreateFilterPredicate(Array.Empty<FilteringDescriptor>()));
var searches = new BehaviorSubject<Func<Trade, bool>>(
    DataGridSchema.CreateSearchPredicate(Array.Empty<SearchDescriptor>()));

source.Connect()
    .Filter(filters)
    .Filter(searches)
    .SortAndBind(out items, sorts)
    .Subscribe();
```

Forward `SortingChanged`, `FilteringChanged`, and `SearchChanged` descriptor lists to the corresponding compiler. This bypasses collection-view sorting and filtering while preserving the grid's header, filter, and search models. `Streaming = true` also configures high-performance search not to track item property changes, because the upstream stream owns change propagation.

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

The generated view contains a title, an optional two-way search box, and a configured `DataGrid`. It binds items, definitions, fast-path options, and any named sorting, filtering, and search models. The parameterless constructor supports XAML, DI, and view locators; an overload accepts the typed view model directly.

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
