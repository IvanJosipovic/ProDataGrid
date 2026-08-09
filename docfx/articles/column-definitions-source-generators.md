# Column definitions with source generators

`ProDataGrid.SourceGenerators` creates `DataGridColumnDefinition` collections, typed accessors, compiled Avalonia binding metadata, and strict fast-path options from annotated row contracts. It is the recommended column-definition path for reflection-free and NativeAOT applications.

For controllers, streaming, hierarchy, state, editing, analytics, generated views, and the complete reference, start at [ProDataGrid source generators](source-generators-feature-spec.md).

## Install

```xml
<ItemGroup>
  <PackageReference Include="ProDataGrid" />
  <PackageReference Include="ProDataGrid.SourceGenerators"
                    PrivateAssets="all" />
</ItemGroup>
```

The analyzer injects its attributes into the consuming compilation under `ProDataGrid.SourceGeneration`; there is no runtime attribute assembly.

## Generate definitions

```csharp
using ProDataGrid.SourceGeneration;

[GenerateDataGridColumns(
    ProviderName = "TradeSchema",
    SchemaId = "trading/trade/v1",
    Strict = true)]
public sealed class Trade
{
    [DataGridKey]
    [DataGridColumn(
        DataGridColumnKind.Numeric,
        Header = "ID",
        ColumnKey = "id",
        Order = 0,
        IsReadOnly = true)]
    public int Id { get; init; }

    [DataGridColumn(
        DataGridColumnKind.Text,
        Header = "Symbol",
        ColumnKey = "symbol",
        Order = 1,
        Width = "2*")]
    public string Symbol { get; set; } = string.Empty;

    [DataGridColumn(
        DataGridColumnKind.Numeric,
        Header = "Price",
        ColumnKey = "price",
        Order = 2,
        FormatString = "N2")]
    public decimal Price { get; set; }
}
```

`PublicProperties` discovery includes eligible public instance properties. Use `Discovery = DataGridColumnDiscovery.AttributedOnly` for opt-in generation and `[DataGridIgnoreColumn]` to exclude one public property.

The generated provider creates fresh mutable definitions and immutable reusable field metadata:

```csharp
DataGridColumnDefinitionList columns =
    TradeSchema.CreateColumnDefinitions();

DataGridFastPathOptions fastPath =
    TradeSchema.CreateFastPathOptions();

SortingDescriptor[] sorting =
[
    TradeSchema.Price.Descending(),
    TradeSchema.Symbol.Ascending()
];
```

## Augment a ViewModel

```csharp
[GenerateDataGridViewModel(
    typeof(Trade),
    ProviderName = "TradeSchema")]
public sealed partial class TradesViewModel : ReactiveObject
{
    public IReadOnlyList<Trade> Items { get; } = LoadTrades();
}
```

Bind the generated `ColumnDefinitions` and `FastPathOptions` with compiled XAML:

```xml
<DataGrid ItemsSource="{Binding Items}"
          ColumnDefinitionsSource="{Binding ColumnDefinitions}"
          FastPathOptions="{Binding FastPathOptions}"
          AutoGenerateColumns="False" />
```

The generated ViewModel also exposes `DataGridSchema`. All three names are configurable.

## Supported column families

The generator covers:

- text, checkbox, hyperlink, and image;
- numeric, progress bar, and slider;
- date picker, time picker, masked text, and autocomplete;
- toggle button/switch and button;
- selected-item, selected-value, and text combo boxes;
- hierarchical and formula columns;
- recycling template columns;
- custom-drawing columns.

Common metadata includes stable keys, order/display index, frozen placement, width/min/max, user permissions, visibility/read-only state, formatting, themes, header/filter resources, localization, accessibility, export, backend-field, and search/filter-editor policy.

See [schemas, columns, accessors, and manifests](source-generators/schemas-and-columns.md) and the [attribute reference](source-generators/attribute-reference.md#datagridcolumn).

## Strict accessors and fast paths

Generated bound definitions receive cached `DataGridBindingDefinition` and `DataGridColumnValueAccessor<TItem,TValue>` values. Sorting, filtering, searching, summaries, editing, export, conditional formatting, and analytics resolve stable field IDs through the schema manifest.

`Strict = true` creates accessor-only fast-path options and reports unsupported metadata instead of silently switching to reflection. Runtime-defined dictionary or descriptor shapes require an explicit `DataGridRuntimeSchemaAdapter<TItem>`.

See:

- [AOT-friendly column bindings](column-definitions-aot.md)
- [Fast-path overview](column-definitions-fast-path-overview.md)
- [Hot-path integration](column-definitions-hot-path.md)

## Direct and drawn realization

```csharp
[DataGridColumn(
    DataGridColumnKind.Numeric,
    DisplayMode = DataGridColumnDisplayMode.Drawn)]
public decimal Amount { get; set; }

[DataGridColumn(
    DataGridColumnKind.Text,
    UseDirectTextCell = true,
    UseDirectTextContent = true,
    TrackDirectTextValueChanges = false)]
public string ImmutableSymbol { get; init; } = string.Empty;
```

Hierarchical and custom-drawing columns expose their own direct accessor/presenter flags. Incompatible options report `PDGSG009`. Disable tracking only when values are immutable or source updates replace/recycle the row.

See [layout, templates, and rendering](source-generators/layout-templates-rendering.md) and [optimized retained and drawn cells](optimized-cell-paths.md).

## Customize a definition

```csharp
[DataGridColumn(
    DataGridColumnKind.Text,
    ConfigureMethod = nameof(ConfigureSymbol))]
public string Symbol { get; set; } = string.Empty;

public static void ConfigureSymbol(DataGridTextColumnDefinition column)
{
    column.Watermark = "Ticker";
}
```

`FactoryMethod` replaces construction for one definition. `GenerateDataGridColumns.ConfigureMethod` adjusts the completed list. `ImplementationType` replaces the complete `IDataGridGeneratedSchema<TItem>` while retaining a stable generated facade.

See [registries and customization](source-generators/registries-and-customization.md).

## Indexed and formula columns

`[GenerateDataGridIndexedColumns]` emits typed factories for bounded slot-based families such as spreadsheets. Formula slots bypass the row getter and carry a stable formula name/value type.

Literal formulas on ordinary or indexed generated formula columns are validated at compile time; invalid syntax produces `PDGSG138`.

See:

- [Layout, templates, and rendering](source-generators/layout-templates-rendering.md#runtime-indexed-column-families)
- [Analytics and formulas](source-generators/analytics-and-formulas.md#formula-columns)

## Next steps

- [Getting started and schema discovery](source-generators/getting-started.md)
- [Operations and controllers](source-generators/operations-and-controllers.md)
- [Reactive, streaming, and remote data](source-generators/reactive-streaming-remote.md)
- [Generated views](source-generators/generated-views.md)
- [Diagnostics and validation](source-generators/diagnostics-performance-testing.md)
