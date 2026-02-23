# Search Model: High-Performance Mode

This guide explains the opt-in high-performance search mode designed for large, frequently changing collections (for example, streaming updates with 50k to 500k rows).

The mode preserves all existing `SearchModel` features and navigation behavior, while reducing expensive full recomputation during add/remove/replace update bursts.

## When to use this mode

Use high-performance mode when all of the following are true:

- You already use `SearchModel`.
- Your `ItemsSource` changes frequently (streaming, rolling windows, incremental ingest).
- Search recomputation is a visible bottleneck.

## What this mode changes

When enabled, accessor-based search can apply many collection changes incrementally instead of recomputing all results every time.

Key points:

- It is opt-in.
- Default search behavior remains unchanged unless you enable it.
- It works best with accessor-based columns (fast path).
- It can track item property changes, or skip them for maximum throughput.

## API surface

The mode is controlled by `DataGridFastPathOptions`:

- `EnableHighPerformanceSearching`
- `HighPerformanceSearchTrackItemChanges`

You usually combine it with:

- `UseAccessorsOnly`
- optional `ThrowOnMissingAccessor` during rollout

## Recommended configurations

### Streaming append/remove (highest throughput)

Use this when your rows are mostly added/removed and existing row values rarely change:

```csharp
grid.FastPathOptions = new DataGridFastPathOptions
{
    UseAccessorsOnly = true,
    EnableHighPerformanceSearching = true,
    HighPerformanceSearchTrackItemChanges = false
};
```

### Mixed updates (rows also mutate in place)

Use this when existing row properties change and those changes must immediately affect search results:

```csharp
grid.FastPathOptions = new DataGridFastPathOptions
{
    UseAccessorsOnly = true,
    EnableHighPerformanceSearching = true,
    HighPerformanceSearchTrackItemChanges = true
};
```

## End-to-end setup

## 1. Keep normal SearchModel wiring

```csharp
public SearchModel SearchModel { get; } = new()
{
    HighlightMode = SearchHighlightMode.TextAndCell,
    HighlightCurrent = true,
    WrapNavigation = true
};
```

```xml
<DataGrid ItemsSource="{Binding View}"
          SearchModel="{Binding SearchModel}" />
```

## 2. Enable fast-path options on the DataGrid

`FastPathOptions` is a CLR property, so assign it in code-behind or view setup:

```csharp
StreamingGrid.FastPathOptions = new DataGridFastPathOptions
{
    UseAccessorsOnly = true,
    EnableHighPerformanceSearching = true,
    HighPerformanceSearchTrackItemChanges = false
};
```

## 3. Use accessor-based search adapter (recommended)

If you configure a custom search adapter factory, use the accessor adapter factory:

```xml
<UserControl.Resources>
  <dataGridSearching:DataGridAccessorSearchAdapterFactory x:Key="AccessorSearchAdapterFactory" />
</UserControl.Resources>

<DataGrid SearchAdapterFactory="{StaticResource AccessorSearchAdapterFactory}" />
```

If you do not override `SearchAdapterFactory`, `DataGrid` will still create the accessor adapter automatically when `UseAccessorsOnly = true`.

## 4. Ensure columns provide accessors

For column definitions, prefer `DataGridBindingDefinition`/typed binding helpers so accessors are available.

For custom or template columns, provide `ValueAccessor` (or equivalent options) so searching can remain on the fast path.

If a required accessor is missing:

- with `ThrowOnMissingAccessor = false`: that column is skipped and a diagnostic event is raised.
- with `ThrowOnMissingAccessor = true`: searching throws for the missing column (useful in development/CI).

## Behavior and fallbacks

High-performance search applies incremental updates when safe. It falls back to full recompute when needed to preserve correctness.

Typical fallback triggers:

- Search descriptor set changes.
- Collection reset.
- Unsupported/ambiguous change patterns where row mapping cannot be resolved safely.

This is intentional: correctness first, performance where safe.

## Practical tuning checklist

- Start with:
  - `UseAccessorsOnly = true`
  - `EnableHighPerformanceSearching = true`
  - `HighPerformanceSearchTrackItemChanges = false` (streaming)
- If in-place row edits must update search immediately, switch `HighPerformanceSearchTrackItemChanges` to `true`.
- During rollout, set `ThrowOnMissingAccessor = true` in dev/test to catch incomplete column accessor wiring.
- Keep your existing `SearchModel` descriptors/navigation unchanged; only infrastructure changes.

## Sample reference: Column Definitions - Streaming Models

The sample app includes a full implementation:

- `src/DataGridSample/ViewModels/ColumnDefinitionsStreamingModelsViewModel.cs`
- `src/DataGridSample/Pages/ColumnDefinitionsStreamingModelsPage.axaml`
- `src/DataGridSample/Pages/ColumnDefinitionsStreamingModelsPage.axaml.cs`

The sample enables:

- `UseAccessorsOnly = true`
- `EnableHighPerformanceSearching = true`
- `HighPerformanceSearchTrackItemChanges = false`

This is the recommended baseline for high-volume streaming scenarios.

## Related articles

- [Search Model: End-to-End Usage](search-model-end-to-end.md)
- [Column Definitions: Fast Path Overview](column-definitions-fast-path-overview.md)
- [High-Frequency Updates](high-frequency-updates.md)
- [Column Definitions: Hot Path Integration](column-definitions-hot-path.md)
