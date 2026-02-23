# Column Definitions: Fast Path Overview

This guide explains how to build a reflection-free, AOT-friendly fast path for sorting, filtering, searching, and related models using column definitions. It includes architecture context and a step-by-step setup.

## Architecture overview

At a high level the fast path works by attaching typed accessors to materialized columns and letting models consume those accessors instead of property-path reflection.

```
View model: DataGridColumnDefinition list
  -> DataGrid materializes DataGridColumn instances
     -> DataGridColumnMetadata attaches Definition, ValueAccessor, ValueType
        -> Models (SortingModel, FilteringModel, SearchModel, etc.)
           -> Adapters project to DataGridCollectionView
```

Key moving parts:
- `DataGridColumnDefinition` describes columns in view models.
- `DataGridBindingDefinition` creates compiled bindings and typed accessors.
- `DataGridColumnMetadata` stores the accessor and type on the materialized column.
- Models (`SortingModel`, `FilteringModel`, `SearchModel`) use descriptors with column ids (definition instance or `ColumnKey`).
- Adapters apply model changes to the view. Fast path adapters read values via accessors.
- `DataGridFastPathOptions` toggles accessor-only behavior and diagnostics.

## Step 1 - Define typed bindings and accessors

Use `DataGridBindingDefinition` with `IPropertyInfo` or a prebuilt `CompiledBindingPath` to avoid runtime expression compilation.

```csharp
var nameInfo = new ClrPropertyInfo(
    nameof(Person.Name),
    target => ((Person)target).Name,
    (target, value) => ((Person)target).Name = (string)value,
    typeof(string));

var nameBinding = DataGridBindingDefinition.Create<Person, string>(
    nameInfo,
    getter: p => p.Name,
    setter: (p, v) => p.Name = v);

var nameColumn = new DataGridTextColumnDefinition
{
    Header = "Name",
    Binding = nameBinding,
    Width = new DataGridLength(1.2, DataGridLengthUnitType.Star)
};
```

If you prefer a typed builder, use `DataGridColumnDefinitionBuilder` to create definitions without reflection or expression compilation:

```csharp
var builder = DataGridColumnDefinitionBuilder.For<Person>();
var nameColumn = builder.Text("Name", nameInfo, p => p.Name, (p, v) => p.Name = v);
```

For computed columns or template columns, provide a value accessor directly:

```csharp
var totalColumn = new DataGridTextColumnDefinition
{
    Header = "Total",
    ValueAccessor = new DataGridColumnValueAccessor<Order, decimal>(o => o.Price * o.Quantity),
    ValueType = typeof(decimal),
    IsReadOnly = true
};
```

## Step 2 - Assign stable column keys

If you need ids that survive re-materialization, assign `ColumnKey` and use it in model descriptors:

```csharp
nameColumn.ColumnKey = "name";
```

## Step 3 - Create the model objects

Create the models and keep them in the view model alongside `ColumnDefinitions`.

```csharp
public FilteringModel FilteringModel { get; } = new();
public SortingModel SortingModel { get; } = new();
public SearchModel SearchModel { get; } = new();
```

Use the definition instance or `ColumnKey` when you create descriptors:

```csharp
SortingModel.Apply(new[]
{
    new SortingDescriptor("name", ListSortDirection.Ascending)
});

FilteringModel.SetOrUpdate(new FilteringDescriptor(
    columnId: "name",
    @operator: FilteringOperator.Contains,
    value: "Ada",
    stringComparison: StringComparison.OrdinalIgnoreCase));

SearchModel.SetOrUpdate(new SearchDescriptor(
    query: "Ada",
    scope: SearchScope.AllColumns,
    comparison: StringComparison.OrdinalIgnoreCase));
```

## Step 4 - Wire the DataGrid

Bind the column definitions and models in XAML:

```xml
<DataGrid ItemsSource="{Binding View}"
          ColumnDefinitionsSource="{Binding ColumnDefinitions}"
          FilteringModel="{Binding FilteringModel}"
          SortingModel="{Binding SortingModel}"
          SearchModel="{Binding SearchModel}"
          AutoGenerateColumns="False" />
```

## Step 5 - Enable accessor-only fast path

Use `DataGridFastPathOptions` to avoid reflection and surface missing accessors. Because `FastPathOptions` is a CLR property, assign it in code-behind or view setup:

```csharp
Grid.FastPathOptions = new DataGridFastPathOptions
{
    UseAccessorsOnly = true,
    ThrowOnMissingAccessor = true,
    EnableHighPerformanceSearching = true,
    HighPerformanceSearchTrackItemChanges = true
};
```

`FastPathOptions` controls filtering and searching adapters. Sorting uses accessors automatically when they are present.

`EnableHighPerformanceSearching` is enabled by default for accessor search adapters. Set it to `false` if you need legacy full-recompute behavior for diagnostics or comparison:

```csharp
Grid.FastPathOptions.EnableHighPerformanceSearching = false;
```

Optional: if you prefer explicit adapter factories, use the built-in accessor factories:

```xml
<!-- Add namespaces: dataGridFiltering, dataGridSearching -->
<UserControl.Resources>
  <dataGridFiltering:DataGridAccessorFilteringAdapterFactory x:Key="AccessorFilteringAdapterFactory" />
  <dataGridSearching:DataGridAccessorSearchAdapterFactory x:Key="AccessorSearchAdapterFactory" />
</UserControl.Resources>

<DataGrid FilteringAdapterFactory="{StaticResource AccessorFilteringAdapterFactory}"
          SearchAdapterFactory="{StaticResource AccessorSearchAdapterFactory}" />
```

## Step 6 - Attach per-column fast path options

Use `DataGridColumnDefinitionOptions` when search/filter/sort should use a different value than the displayed binding:

```csharp
var fullNameColumn = new DataGridTemplateColumnDefinition
{
    Header = "Full Name",
    CellTemplateKey = "FullNameTemplate",
    IsReadOnly = true,
    Options = new DataGridColumnDefinitionOptions
    {
        SearchTextProvider = item => ((Person)item).FirstName + " " + ((Person)item).LastName,
        SortValueAccessor = new DataGridColumnValueAccessor<Person, string>(
            p => p.LastName + ", " + p.FirstName)
    }
};
```

Other option hooks:
- `FilterPredicateFactory` for custom operators.
- `FilterValueAccessor` when filtering should use a different value.
- `SortValueComparer` for custom ordering of the sort key.
- `SearchMemberPath` and `SearchFormatProvider` to align search with formatted values.

## Step 7 - Sorting with accessors in collection views

If you sort directly via `DataGridCollectionView`, use accessor-based sort descriptions:

```csharp
var accessor = DataGridColumnMetadata.GetValueAccessor(nameColumn);
View.SortDescriptions.Add(DataGridSortDescription.FromAccessor(accessor, propertyPath: nameof(Person.Name)));
```

Use the generic overload when you already have a typed accessor:

```csharp
var ageAccessor = new DataGridColumnValueAccessor<Person, int>(p => p.Age);
View.SortDescriptions.Add(DataGridSortDescription.FromAccessor(ageAccessor, propertyPath: nameof(Person.Age)));
```

## Step 8 - Diagnostics and strict mode

Subscribe to missing accessor diagnostics to catch gaps early:

```csharp
Grid.FastPathOptions.MissingAccessor += (_, args) =>
    Debug.WriteLine($"{args.Feature}: {args.Message}");
```

Set `StrictMode` to enforce accessors for both filtering and searching:

```csharp
Grid.FastPathOptions.StrictMode = true;
```

## Step 9 - Template columns and computed values

Template columns do not have bindings by default. Provide at least one of:
- `ValueAccessor` (preferred for fast path filtering/sorting/searching)
- `Options.SearchTextProvider` for search-only scenarios

If you use `StrictMode`, missing accessors will throw in filtering/searching.

## Other fast path consumers

These features also use value accessors when available:

- Conditional formatting rules (`ConditionalFormattingModel`).
- Summaries and aggregation.
- State capture/restore when `ColumnKey` is used for stable ids.
- Clipboard/export values when `ClipboardContentBinding` is supplied.

## Performance notes

- Sort comparers created from accessors are cached per accessor and culture to avoid repeated allocations.
- Filtering predicates created by the accessor adapter are cached per descriptor, accessor, and operator.
- When you reuse `IPropertyInfo` instances across columns, enable the compiled binding path cache for AOT-friendly reuse.

## Checklist

- [ ] Column definitions use `DataGridBindingDefinition` or `ValueAccessor` for every column.
- [ ] `ColumnKey` assigned for stable ids used by models and state.
- [ ] Models (sorting/filtering/searching) use definition ids or keys, not property paths.
- [ ] `FastPathOptions` enabled and attached in code-behind or view setup.
- [ ] Template columns have accessors or search text providers.
- [ ] Custom filter/sort/search behavior uses `DataGridColumnDefinitionOptions`.

## Related articles

- [Column Definitions](column-definitions.md)
- [Column Definitions: AOT-Friendly Bindings](column-definitions-aot.md)
- [Column Definitions: Model Integration and Fast Path](column-definitions-models.md)
- [Column Definitions: Hot Path Integration](column-definitions-hot-path.md)
- [Column Definitions: Hierarchical Columns](column-definitions-hierarchical.md)
