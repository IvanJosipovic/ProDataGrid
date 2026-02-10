# Hierarchical Model: End-to-End Usage

This guide shows complete `HierarchicalModel` usage with `DataGrid`: building the tree model, binding flattened nodes automatically, hierarchical sorting, and expansion workflows.

## What this gives you

- Tree data rendered in `DataGrid` without manually maintaining a flattened list.
- Expand/collapse behavior driven by a model instead of UI container state.
- Optional sibling sorting integrated with `SortingModel`.
- Scalable updates through `VirtualizeChildren` and model refresh APIs.

## End-to-end flow

1. Build `HierarchicalOptions<T>` with child selectors and behavior flags.
2. Create `HierarchicalModel<T>` and set roots.
3. Bind `HierarchicalModel` to grid with `HierarchicalRowsEnabled="True"`.
4. Grid consumes flattened visible nodes and updates as model expansion changes.

## 1. ViewModel wiring

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Controls.DataGridHierarchical;
using Avalonia.Controls.DataGridSorting;

public sealed class FilesViewModel
{
    public HierarchicalModel<TreeItem> Model { get; }
    public ISortingModel SortingModel { get; } = new SortingModel();

    public FilesViewModel(TreeItem root)
    {
        var options = new HierarchicalOptions<TreeItem>
        {
            ItemsSelector = item => item.Children,
            IsLeafSelector = item => !item.IsDirectory,
            AutoExpandRoot = true,
            MaxAutoExpandDepth = 0,
            VirtualizeChildren = true
        };

        Model = new HierarchicalModel<TreeItem>(options);
        Model.SetRoot(root);

        SortingModel.SortingChanged += (_, e) =>
        {
            var comparer = BuildComparer(e.NewDescriptors);
            Model.ApplySiblingComparer(comparer, recursive: true);
        };
    }

    public void ExpandAll() => Model.ExpandAll();
    public void CollapseAll() => Model.CollapseAll();
    public void RefreshRoot() => Model.Refresh(Model.Root);

    private static IComparer<TreeItem> BuildComparer(IReadOnlyList<SortingDescriptor> descriptors)
    {
        return Comparer<TreeItem>.Create((left, right) =>
        {
            foreach (var descriptor in descriptors)
            {
                if (string.Equals(descriptor.PropertyPath, "Item.Name", StringComparison.Ordinal))
                {
                    var result = string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
                    if (result != 0)
                    {
                        return descriptor.Direction == ListSortDirection.Descending ? -result : result;
                    }
                }
            }

            return 0;
        });
    }
}
```

Minimal item shape used by the sample:

```csharp
public sealed class TreeItem
{
    public string Name { get; init; } = string.Empty;
    public string Kind { get; init; } = string.Empty;
    public long Size { get; init; }
    public DateTimeOffset Modified { get; init; }
    public bool IsDirectory { get; init; }
    public IReadOnlyList<TreeItem> Children { get; init; } = Array.Empty<TreeItem>();
}
```

## 2. XAML wiring

```xml
<DataGrid HierarchicalModel="{Binding Model}"
          HierarchicalRowsEnabled="True"
          SortingModel="{Binding SortingModel}"
          SortingAdapterFactory="{StaticResource HierarchicalSortingAdapterFactory}"
          AutoGenerateColumns="False"
          CanUserSortColumns="True">
  <DataGrid.Columns>
    <DataGridHierarchicalColumn Header="Name"
                                SortMemberPath="Item.Name"
                                Binding="{Binding Item}"
                                Width="2*" />
    <DataGridTemplateColumn Header="Kind" SortMemberPath="Item.Kind" Width="120" />
    <DataGridTemplateColumn Header="Size" SortMemberPath="Item.Size" Width="120" />
    <DataGridTemplateColumn Header="Modified" SortMemberPath="Item.Modified" Width="160" />
  </DataGrid.Columns>
</DataGrid>
```

When `HierarchicalRowsEnabled` is true and `ItemsSource` is omitted, the grid binds to the hierarchical model output automatically.

## 3. Expansion and path behavior

Use model commands (`Expand`, `Collapse`, `ExpandAll`, `CollapseAll`) rather than manipulating row containers directly.

For persistent expansion/selection scenarios, configure:

- `ExpandedStateKeyMode` (`Item`, `Path`, `Custom`)
- `ItemPathSelector`
- `IsExpandedSelector` / `IsExpandedSetter`

These options keep expansion stable across refreshes and source updates.

## 4. Refresh and high-frequency updates

- Call `Model.Refresh(node)` when child content changes.
- For observable child collections, model flattening updates incrementally.
- Keep `VirtualizeChildren=true` for large trees to avoid retaining collapsed child nodes.

## 5. Filtering/searching with hierarchical data

You can combine hierarchical data with filtering/search models. Common pattern:

1. Translate descriptors to predicates (often via custom adapter factories).
2. Apply tree-aware predicates in your pipeline.
3. Refresh the hierarchical model after predicate changes.

Reference implementations:

- `src/DataGridSample/Adapters/HierarchicalFilteringAdapterFactory.cs`
- `src/DataGridSample/Adapters/HierarchicalSearchAdapterFactory.cs`
- `src/DataGridSample/Adapters/HierarchicalSortingAdapterFactory.cs`

## Troubleshooting

- Grid shows flat rows only:
  Ensure `HierarchicalRowsEnabled="True"` and a valid `HierarchicalModel` is assigned.
- Expansion toggles do nothing:
  Verify `ItemsSelector`/`ChildrenSelector` returns children for directory/non-leaf nodes.
- Sort headers toggle but tree order does not change:
  Apply sibling comparer changes in `SortingChanged` (or use a hierarchical sorting adapter).

## Full sample references

- `src/DataGridSample/Pages/HierarchicalSamplePage.axaml`
- `src/DataGridSample/ViewModels/HierarchicalSampleViewModel.cs`
- `src/DataGridSample/Pages/ColumnDefinitionsHierarchicalPage.axaml`

## Related articles

- [Hierarchical Data](hierarchical-data.md)
- [Hierarchical High-Frequency Updates](hierarchical-high-frequency-updates.md)
- [Sorting Model: End-to-End Usage](sorting-model-end-to-end.md)
- [Filtering Model: End-to-End Usage](filtering-model-end-to-end.md)
