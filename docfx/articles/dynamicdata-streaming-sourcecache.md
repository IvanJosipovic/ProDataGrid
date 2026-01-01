# DynamicData Streaming with SourceCache

This guide shows how to use a DynamicData `SourceCache<T, TKey>` for high-frequency updates while still driving sorting and filtering through the grid's models. The cache keeps keyed updates efficient and the DynamicData pipeline applies the view transformations.

## Why SourceCache

`SourceCache<T, TKey>` is a good fit when items are identified by a stable key (for example, `Id`). You can add or update items by key and remove old items without list scans.

## Pipeline setup

```csharp
var cache = new SourceCache<StreamingItem, int>(item => item.Id);
var sortingFactory = new DynamicDataStreamingSortingAdapterFactory(LogSorts);
var filteringFactory = new DynamicDataStreamingFilteringAdapterFactory(LogFilters);

var sortSubject = new BehaviorSubject<IComparer<StreamingItem>>(sortingFactory.SortComparer);
var filterSubject = new BehaviorSubject<Func<StreamingItem, bool>>(filteringFactory.FilterPredicate);

var subscription = cache.Connect()
    .Filter(filterSubject)
    .SortAndBind(out ReadOnlyObservableCollection<StreamingItem> view, sortSubject)
    .Subscribe();
```

## Wiring sorting and filtering models

Use the same `SortingModel` and `FilteringModel` wiring as the SourceList example:

```csharp
SortingModel = new SortingModel { OwnsViewSorts = true };
FilteringModel = new FilteringModel { OwnsViewFilter = true };
```

## Basic XAML setup

```xml
<DataGrid ItemsSource="{Binding View}"
          AutoGenerateColumns="False"
          CanUserSortColumns="True"
          SortingModel="{Binding SortingModel}"
          FilteringModel="{Binding FilteringModel}">
  <DataGrid.Columns>
    <DataGridTextColumn Header="Id" Binding="{Binding Id}" SortMemberPath="Id" />
    <DataGridTextColumn Header="Symbol" Binding="{Binding Symbol}" SortMemberPath="Symbol" />
    <DataGridTextColumn Header="Price" Binding="{Binding PriceDisplay}" SortMemberPath="Price" />
    <DataGridTextColumn Header="Updated" Binding="{Binding UpdatedAtDisplay}" SortMemberPath="UpdatedAt" />
  </DataGrid.Columns>
</DataGrid>
```

## Rolling window updates

```csharp
var idQueue = new Queue<int>();
var count = cache.Count;

cache.Edit(updater =>
{
    for (var i = 0; i < batchSize; i++)
    {
        var item = CreateItem();
        updater.AddOrUpdate(item);
        idQueue.Enqueue(item.Id);
        count++;
    }

    while (count > targetCount && idQueue.Count > 0)
    {
        updater.RemoveKey(idQueue.Dequeue());
        count--;
    }
});
```

The queue preserves insertion order so you can evict the oldest items without scanning the cache.

## Sample

See the **DynamicData Streaming (SourceCache)** tab in the sample gallery for a complete implementation.
