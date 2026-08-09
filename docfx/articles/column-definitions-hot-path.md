# Column Definitions: Hot Path Integration

This guide focuses on wiring column definitions into sorting, filtering, searching, and other models without reflection. The goal is an AOT-friendly, fast path where models consume typed accessors.

## 1. Provide accessors for every column

`DataGridBindingDefinition` creates both a compiled binding and a typed value accessor. Prefer overloads that do not require dynamic code:

```csharp
var path = new CompiledBindingPathBuilder()
    .Property(Person.NameProperty, PropertyInfoAccessorFactory.CreateAvaloniaPropertyAccessor)
    .Build();

var nameBinding = DataGridBindingDefinition.Create<Person, string>(
    path,
    getter: p => p.Name);

var nameColumn = new DataGridTextColumnDefinition
{
    Header = "Name",
    Binding = nameBinding,
    SortMemberPath = "Name"
};
```

For computed columns, set a value accessor directly:

```csharp
new DataGridTextColumnDefinition
{
    Header = "Total",
    ValueAccessor = new DataGridColumnValueAccessor<Order, decimal>(o => o.Price * o.Quantity),
    ValueType = typeof(decimal),
    IsReadOnly = true
};
```

## 2. Use column definitions as model ids

Always pass the definition instance as the column id so model descriptors map to the materialized column without string lookups.

```csharp
sortingModel.Apply(new[]
{
    new SortingDescriptor(nameColumn, ListSortDirection.Ascending)
});

filteringModel.SetOrUpdate(new FilteringDescriptor(
    columnId: nameColumn,
    @operator: FilteringOperator.Contains,
    value: "Ada",
    stringComparison: StringComparison.OrdinalIgnoreCase));
```

## 3. Avoid reflection in filtering and searching

Filtering and searching adapters fall back to property-path reflection when no accessor is available. You can enforce accessor-only behavior with `FastPathOptions`:

```xml
<DataGrid FilteringModel="{Binding FilteringModel}"
          SearchModel="{Binding SearchModel}">
  <DataGrid.FastPathOptions>
    <DataGridFastPathOptions UseAccessorsOnly="True"
                             ThrowOnMissingAccessor="True" />
  </DataGrid.FastPathOptions>
</DataGrid>
```

If you need to bind or reuse a `DataGridFastPathOptions` instance from a view model, assign it in code-behind because it is a CLR property (not an AvaloniaProperty).

You can also use the built-in adapter factories directly:

```xml
<DataGrid FilteringModel="{Binding FilteringModel}"
          SearchModel="{Binding SearchModel}"
          FilteringAdapterFactory="{StaticResource AccessorFilteringAdapterFactory}"
          SearchAdapterFactory="{StaticResource AccessorSearchAdapterFactory}"
          ColumnDefinitionsSource="{Binding ColumnDefinitions}" />
```

The factories can be instantiated with `DataGridAccessorFilteringAdapterFactory` and `DataGridAccessorSearchAdapterFactory`. If you want custom behavior, implement your own adapter by overriding `TryApplyModelToView` and resolving values via `DataGridColumnMetadata.GetValueAccessor` or `DataGridColumnSearch.GetTextProvider`.

If filtering should target a different value than the displayed binding, set `DataGridColumnDefinitionOptions.FilterValueAccessor` (or `DataGridColumnFilter.SetValueAccessor`) on the column definition.
For custom operators, use `DataGridColumnDefinitionOptions.FilterPredicateFactory` to supply a predicate builder.

### Diagnostics and strict mode

If you want diagnostics when a fast-path accessor is missing, subscribe to `DataGridFastPathOptions.MissingAccessor`. For a strict mode that enforces accessors and throws, set `StrictMode`:

```csharp
grid.FastPathOptions = new DataGridFastPathOptions
{
    StrictMode = true
};

grid.FastPathOptions.MissingAccessor += (_, args) =>
    Debug.WriteLine($"Missing accessor for {args.Feature}: {args.Message}");
```

## 4. Sorting without path reflection

Sorting uses accessors automatically when present. If you work directly with collection views, use comparer or accessor-based sort descriptions:

```csharp
var accessor = DataGridColumnMetadata.GetValueAccessor(nameColumn);
view.SortDescriptions.Add(DataGridSortDescription.FromAccessor(accessor, propertyPath: "Name"));
```

Avoid `DataGridSortDescription.FromPath` in AOT scenarios.

When the accessor implements `IDataGridColumnValueAccessor<TItem, TValue>`, sorting uses a typed comparer (`DataGridColumnValueAccessorComparer<TItem, TValue>`) to avoid boxing for value types.

If you already have a typed accessor, you can use the generic overload:

```csharp
var ageAccessor = new DataGridColumnValueAccessor<Person, int>(p => p.Age);
view.SortDescriptions.Add(DataGridSortDescription.FromAccessor(ageAccessor, propertyPath: nameof(Person.Age)));
```

`DataGridSortDescription.FromComparer` has an overload that keeps the property path even when you provide a custom comparer. This helps state persistence and model resolution.

For computed sort keys, prefer `DataGridColumnDefinitionOptions.SortValueAccessor` to keep the displayed binding and sort key independent:

```csharp
new DataGridTextColumnDefinition
{
    Header = "Total",
    Binding = DataGridBindingDefinition.Create<Order, decimal>(o => o.Total),
    Options = new DataGridColumnDefinitionOptions
    {
        SortValueAccessor = new DataGridColumnValueAccessor<Order, decimal>(o => o.Price * o.Quantity)
    }
};
```

If you want to compare entire models (multi-field or custom ordering), use the typed options:

```csharp
new DataGridTextColumnDefinition
{
    Header = "Customer",
    Binding = DataGridBindingDefinition.Create<Order, string>(o => o.CustomerName),
    Options = new DataGridColumnDefinitionOptions<Order>
    {
        CompareAscending = (left, right) => string.Compare(left.CustomerName, right.CustomerName, StringComparison.Ordinal),
        CompareDescending = (left, right) => string.Compare(right.CustomerName, left.CustomerName, StringComparison.Ordinal)
    }
};
```

## 5. Searching text sources

Search uses value accessors by default. For non-string values or template columns, provide a text source:

```csharp
DataGridColumnSearch.SetTextProvider(nameColumn, item => ((Person)item).Name);
```

This keeps search highlights and navigation working without reflection.

If you provide custom accessors, implement `IDataGridColumnTextAccessor` (search) and `IDataGridColumnFilterAccessor` (filtering) to keep the fast path fully typed and avoid object-based fallbacks.

## 6. Conditional formatting and summaries

Conditional formatting and summary calculations read values from accessors when available:

```csharp
formattingModel.Apply(new[]
{
    new ConditionalFormattingDescriptor(
        ruleId: "OverBudget",
        columnId: totalColumn,
        @operator: ConditionalFormattingOperator.GreaterThan,
        value: 1000m,
        themeKey: "OverBudgetCellTheme")
});
```

## 7. State, clipboard, and export

- State persistence uses column definition ids by default; provide custom keys if you need stable ids across sessions.
- Clipboard/export bindings can use `DataGridBindingDefinition` to stay compiled and typed.

## 8. Grouping and other view operations

`DataGridPathGroupDescription` uses property paths and reflection. For AOT scenarios, implement a custom `DataGridGroupDescription` that reads values through your accessors.

## 9. Optimized retained display path

For immutable display data, `DataGridTextColumnDefinition.UseDirectTextCell=true`
removes the nested retained presenter and
`TrackDirectTextValueChanges=false` avoids a per-realized-cell property-change
subscription. Leave change tracking enabled for mutable values; recycled cells still
refresh when their row data changes.

`DataGridCustomDrawingColumnDefinition.UseDirectValueAccessor=true` lets a custom
drawing cell read compatible typed accessor metadata without creating a display
binding expression. Set `TrackDirectValueChanges=false` only when the displayed value
is immutable.

The normal generic themes remain the compatibility default. Applications with simple,
fixed-height data surfaces can opt into the partial themes from `Themes/Optimized.xaml`:

```xml
<DataGrid RowTheme="{StaticResource DataGridOptimizedUnfrozenRowTheme}"
          CellTheme="{StaticResource DataGridOptimizedCellTheme}"
          ColumnHeaderTheme="{StaticResource DataGridOptimizedColumnHeaderTheme}"
          UseLightweightFiller="True" />
```

`DataGridOptimizedUnfrozenRowTheme` uses one normal
`DataGridCellsPresenter` and is intended only when both frozen-column counts are zero.
Use `DataGridOptimizedRowTheme` when left or right frozen columns are enabled. These
themes preserve Avalonia controls and layout; they do not switch cells to custom
drawing. Editing, arbitrary templates, and the original generic themes remain
available. Optimized resources use static resource lookup, so use the generic themes
when application code must replace theme resources dynamically at runtime.

## Related articles

- [Column Definitions](column-definitions.md)
- [Column Definitions (AOT-Friendly Bindings)](column-definitions-aot.md)
- [Column Definitions (Model Integration)](column-definitions-models.md)
- [Column Definitions (Hierarchical Columns)](column-definitions-hierarchical.md)
- [Column Definitions: Fast Path Overview](column-definitions-fast-path-overview.md)
