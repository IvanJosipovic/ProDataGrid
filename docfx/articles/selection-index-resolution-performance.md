# Selection Index Resolution Performance

Selection index resolution can become a performance bottleneck on large datasets when item-to-index mapping relies on linear reference scans.
This article documents the supported optimization paths and their fallback behavior.

ProDataGrid now supports three complementary solutions:

1. Built-in reference index cache (default, no setup)
2. Specialized collection interface (`IDataGridIndexOf`)
3. Per-grid resolver hook (`DataGrid.ReferenceIndexResolver`)

## Default behavior

By default, `DataGridDataConnection` builds and reuses an internal reference-index cache for `IList` data sources.
The cache is invalidated on collection changes and data source swaps, then rebuilt lazily on demand.

This keeps selection and current-item index resolution fast for repeated operations on large lists without any application changes.

## Option 1: `IDataGridIndexOf`

Implement `IDataGridIndexOf` on your source collection when you already maintain an O(1) reference lookup:

```csharp
public sealed class FastItems : ObservableCollection<MyItem>, IDataGridIndexOf
{
    private readonly Dictionary<object, int> _index = new(ReferenceEqualityComparer.Instance);

    public bool TryGetReferenceIndex(object item, out int index) =>
        _index.TryGetValue(item, out index);
}
```

Use this when your data source can efficiently maintain a reference-to-index map.

## Option 2: `ReferenceIndexResolver`

Provide a resolver delegate per grid when you cannot (or do not want to) change the collection type:

```csharp
dataGrid.ReferenceIndexResolver = (list, item) =>
{
    return lookup.TryGetValue(item, out var index) ? index : -1;
};
```

Return:

- `>= 0` to use the resolved index
- `< 0` to fall back to the built-in strategies

## Resolution order

For reference-based item lookup, DataGrid uses:

1. `ReferenceIndexResolver` (if configured)
2. `IDataGridIndexOf` (if available)
3. internal cache
4. linear reference scan fallback

This preserves compatibility while giving fast paths for advanced scenarios.

## Sample pages

See these DataGridSample pages:

- **Selection Fast Index (Interface)** — `SelectionFastIndexInterfacePage`
- **Selection Fast Index (Cache)** — `SelectionFastIndexCachePage`
- **Selection Fast Index (Resolver)** — `SelectionFastIndexResolverPage`
