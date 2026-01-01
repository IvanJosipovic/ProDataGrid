# DynamicData Streaming with SourceList

This guide shows how to combine a DynamicData `SourceList<T>` with `SortingModel` and `FilteringModel` to keep high-frequency updates smooth. Sorting and filtering are handled upstream in the DynamicData pipeline so the grid view stays in the fast-path mode.

## Why SourceList

`SourceList<T>` preserves insertion order and supports batched edits. With a rolling window of updates, you can apply adds/removes in a single edit to avoid excess notifications.

## Pipeline setup

```csharp
var source = new SourceList<StreamingItem>();
var sortingFactory = new DynamicDataStreamingSortingAdapterFactory(LogSorts);
var filteringFactory = new DynamicDataStreamingFilteringAdapterFactory(LogFilters);

var sortSubject = new BehaviorSubject<IComparer<StreamingItem>>(sortingFactory.SortComparer);
var filterSubject = new BehaviorSubject<Func<StreamingItem, bool>>(filteringFactory.FilterPredicate);

var subscription = source.Connect()
    .Filter(filterSubject)
    .Sort(sortSubject)
    .Bind(out ReadOnlyObservableCollection<StreamingItem> view)
    .Subscribe();
```

## Wiring sorting and filtering models

```csharp
SortingModel = new SortingModel
{
    MultiSort = true,
    CycleMode = SortCycleMode.AscendingDescendingNone,
    OwnsViewSorts = true
};
SortingModel.SortingChanged += (_, e) =>
{
    sortingFactory.UpdateComparer(e.NewDescriptors);
    sortSubject.OnNext(sortingFactory.SortComparer);
};

FilteringModel = new FilteringModel
{
    OwnsViewFilter = true
};
FilteringModel.FilteringChanged += (_, e) =>
{
    filteringFactory.UpdateFilter(e.NewDescriptors);
    filterSubject.OnNext(filteringFactory.FilterPredicate);
};
```

Bind the view to the grid and attach the adapter factories:

```xml
<DataGrid ItemsSource="{Binding View}"
          SortingModel="{Binding SortingModel}"
          FilteringModel="{Binding FilteringModel}" />
```

```csharp
grid.SortingAdapterFactory = viewModel.SortingAdapterFactory;
grid.FilteringAdapterFactory = viewModel.FilteringAdapterFactory;
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
source.Edit(list =>
{
    var additions = new List<StreamingItem>(batchSize);
    for (var i = 0; i < batchSize; i++)
    {
        additions.Add(CreateItem());
    }

    list.AddRange(additions);

    var removeCount = list.Count - targetCount;
    if (removeCount > 0)
    {
        list.RemoveRange(0, removeCount);
    }
});
```

Batching the edits keeps the change set small and avoids per-item churn.

## Sample

See the **DynamicData Streaming (SourceList)** tab in the sample gallery for a complete implementation.
