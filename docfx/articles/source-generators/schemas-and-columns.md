# Schemas, columns, accessors, and manifests

A generated schema is the reflection-free contract shared by column creation, sorting, filtering, searching, identity, editing, transfer, hierarchy, state, and analytics. Configure the row once; downstream generated features reuse the same accessors and stable keys.

## Generated schema contracts

The provider implements `IDataGridGeneratedSchema<TItem>`, which composes focused interfaces:

- `IDataGridColumnDefinitionProvider<TItem>` creates fresh column-definition lists.
- `IDataGridSortingCompiler<TItem>` compiles descriptor lists into comparers.
- `IDataGridFilteringCompiler<TItem>` compiles descriptors into predicates.
- `IDataGridSearchingCompiler<TItem>` compiles search descriptors into predicates.
- `IDataGridFastPathOptionsProvider` creates accessor-only fast-path options.
- `IDataGridGeneratedSchemaManifestProvider` exposes stable schema and field metadata.

Descriptor column IDs and paths are identifiers only. Generated operations resolve them through the manifest and never call runtime property lookup.

## Column kinds

`DataGridColumnKind` covers the current definition builders:

| Family | Kinds |
| --- | --- |
| Basic bound | `Text`, `CheckBox`, `Hyperlink`, `Image` |
| Numeric and range | `Numeric`, `ProgressBar`, `Slider` |
| Date and text input | `DatePicker`, `TimePicker`, `MaskedText`, `AutoComplete` |
| Toggle and commands | `ToggleButton`, `ToggleSwitch`, `Button` |
| Selection | `ComboBoxSelectedItem`, `ComboBoxSelectedValue`, `ComboBoxText` |
| Advanced | `Hierarchical`, `CustomDrawing`, `Template`, `Formula` |
| Inferred | `Auto` |

Example:

```csharp
[GenerateDataGridColumns(
    ProviderName = "ProductSchema",
    Discovery = DataGridColumnDiscovery.AttributedOnly)]
public sealed class Product
{
    [DataGridKey]
    [DataGridColumn(DataGridColumnKind.Numeric,
        Header = "ID", ColumnKey = "id", IsReadOnly = true)]
    public int Id { get; init; }

    [DataGridColumn(DataGridColumnKind.Text,
        Header = "Product", ColumnKey = "name", Width = "2*")]
    public string Name { get; set; } = string.Empty;

    [DataGridColumn(DataGridColumnKind.Numeric,
        Header = "Price", ColumnKey = "price", FormatString = "C2",
        Minimum = 0, Increment = 0.25)]
    public decimal Price { get; set; }

    [DataGridColumn(DataGridColumnKind.CheckBox,
        Header = "Active", ColumnKey = "active")]
    public bool IsActive { get; set; }
}
```

## Common column options

The common metadata surface controls:

- identity: `ColumnKey`, `PreviousColumnKeys`;
- presentation: `Header`, `Description`, `Order`, `DisplayIndex`, `Width`, `MinWidth`, `MaxWidth`, `FormatString`, `Watermark`;
- behavior: `CanUserSort`, `CanUserHide`, `CanUserResize`, `CanUserReorder`, `IsReadOnly`, `IsVisible`, `ShowFilterButton`;
- layout: `FrozenPlacement`, `WidthSharingGroup`;
- resources: header/cell/summary/filter themes, templates, filter flyouts, resource keys;
- operations: `SortMemberPath`, `SearchMemberPath`, `IsSearchable`, `BackendFieldName`, `FilterEditor`;
- transfer and security: export formatting, null text, automation metadata, and `IsSensitive`.

Kind-specific options are validated. For example, a numeric bound applies `Minimum`, `Maximum`, and `Increment`; a masked-text column uses `Mask`; a combo box uses items/display/value members; a button uses content/command members. Incompatible options report `PDGSG009`.

For an exhaustive property list, see the [attribute reference](attribute-reference.md#datagridcolumn).

## Stable typed fields

Each generated column is also a static strongly typed field descriptor:

```csharp
SortingDescriptor[] sorting =
[
    ProductSchema.Price.Descending(),
    ProductSchema.Name.Ascending()
];

FilteringDescriptor[] filtering =
[
    ProductSchema.Price.Between(10m, 100m),
    ProductSchema.Name.Contains("pro", StringComparison.OrdinalIgnoreCase)
];

SearchDescriptor search = ProductSchema.Name.Search(
    "desk*",
    SearchMatchMode.Wildcard);
```

Field ordinals and stable keys are deterministic. The same field provides the typed accessor used by columns, operations, summaries, editing, export, conditional formatting, and analytics.

## Stable identity and item indexes

Mark one accessible, non-nullable field or property with `[DataGridKey]`:

```csharp
[DataGridKey]
public long Id { get; init; }
```

For composite identity, name a static selector:

```csharp
[GenerateDataGridColumns(KeySelectorMethod = nameof(CreateGridKey))]
public sealed class Order
{
    public int TenantId { get; init; }
    public long OrderId { get; init; }

    public static (int TenantId, long OrderId) CreateGridKey(Order item) =>
        (item.TenantId, item.OrderId);
}
```

Reference types whose identity intentionally is the object instance may set `UseReferenceIdentityKey = true`. Key members, selector methods, and reference identity are mutually exclusive.

A keyed provider implements `IDataGridItemKey<TItem,TKey>` and creates an incrementally maintained index:

```csharp
DataGridGeneratedItemIndex<Order, (int, long)> index =
    OrderDataGridSchema.CreateItemIndex(items);

if (index.TryGetIndex((tenantId, orderId), out int rowIndex))
{
    // Coordinate selection, state, drag/drop, or chart focus.
}
```

The index applies insert, remove, move, replace, and reset changes. It captures keys at insertion and rejects duplicates so accidental key mutation cannot corrupt lookup.

## Canonical manifest

`Manifest` contains:

- a manifest format version;
- `SchemaId`, `StateVersion`, and deterministic `SchemaHash`;
- item and key types;
- ordered fields, stable column keys, previous-key aliases, and accessors;
- capability metadata used by operations, workflows, hierarchy, and analytics.

Set `SchemaId` explicitly for persisted or cross-assembly schemas. Increment `StateVersion` for a migration boundary. The schema hash changes when behaviorally significant column, grouping, summary, band, analytics, formula, factory, or configuration metadata changes.

## Column factories and configure hooks

Use a static configure method for last-mile changes:

```csharp
[DataGridColumn(DataGridColumnKind.Text,
    ConfigureMethod = nameof(ConfigureSymbol))]
public string Symbol { get; set; } = string.Empty;

public static void ConfigureSymbol(DataGridTextColumnDefinition column)
{
    column.Watermark = "Ticker";
}
```

`FactoryMethod` replaces construction for one column. A replacement factory owns the initial binding and concrete definition type. After it returns, the generator applies canonical keys/accessors, attribute options, summary metadata, and the optional per-column configure method.

`GenerateDataGridColumns.ConfigureMethod` receives the completed definition list and runs after individual columns. The ordering is:

1. Generated or user factory construction.
2. Canonical generated metadata.
3. Per-column configure method.
4. Schema-list configure method.

## Full schema implementations

Set `ImplementationType` to an accessible implementation of `IDataGridGeneratedSchema<TItem>` for complete ownership:

```csharp
[GenerateDataGridColumns(
    ProviderName = "TradeSchema",
    ImplementationType = typeof(CustomTradeSchema))]
public sealed class Trade
{
    public string Symbol { get; set; } = string.Empty;
}
```

The emitted provider remains a stable facade and forwards the schema contracts to the implementation. `PDGSG007` reports an invalid implementation shape.

## Runtime-defined shapes

Compile-time generation cannot discover arbitrary dictionary keys or dynamic descriptors. Use `DataGridRuntimeSchemaAdapter<TItem>` and an explicit `IDataGridRuntimeSchemaProvider<TItem>`:

```csharp
[GenerateDataGridColumns(
    Discovery = DataGridColumnDiscovery.AttributedOnly,
    ImplementationType = typeof(RuntimeRowSchema),
    ProviderName = "RuntimeRowFacade",
    SchemaId = "orders/runtime/v1")]
public sealed class RuntimeRow : Dictionary<string, object?> { }

public sealed class RuntimeRowSchema : DataGridRuntimeSchemaAdapter<RuntimeRow>
{
    public RuntimeRowSchema() : base(new Provider()) { }

    private sealed class Provider : IDataGridRuntimeSchemaProvider<RuntimeRow>
    {
        public string SchemaId => "orders/runtime/v1";

        public IReadOnlyList<DataGridRuntimeSchemaField<RuntimeRow>> CreateFields() =>
        [
            new(
                "symbol",
                "Symbol",
                new DataGridColumnValueAccessor<RuntimeRow, string>(
                    static row => (string)row["Symbol"]!,
                    static (row, value) => row["Symbol"] = value),
                static () => new DataGridTextColumnDefinition { Header = "Symbol" })
        ];

        public DataGridFastPathOptions CreateFastPathOptions() => new()
        {
            UseAccessorsOnly = true,
            ThrowOnMissingAccessor = true,
            EnableHighPerformanceSearching = true
        };
    }
}
```

The adapter validates and freezes the field shape once, creates fresh definition lists, builds a runtime manifest, and reuses the normal accessor-based operation engine. Any necessary discovery of an external shape belongs inside the explicit provider. Known dictionary keys can remain completely reflection-free.

Without `ImplementationType`, a known dynamic shape produces `PDGSG134`.

## Fast-path policy

Generated schemas create strict accessor-only options. This avoids expression compilation and property lookup in sorting, filtering, searching, editing, and rendering paths.

Use `Strict = false` only when an application intentionally permits a compatibility fallback. A generated strict path reports missing or incompatible metadata rather than silently changing execution strategy.

For direct and drawn cell realization, custom-drawing caches, and indexed families, see [layout, templates, and rendering](layout-templates-rendering.md).
