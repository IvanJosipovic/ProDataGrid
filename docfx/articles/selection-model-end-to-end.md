# Selection Model: End-to-End Usage

This guide shows complete `SelectionModel` usage with `DataGrid`: shared selection across controls, stable selection during data mutations, and programmatic selection workflows.

## What this gives you

- A single selection source for grid and companion controls.
- Stable selection across sorting, filtering, inserts/removes, and reordering.
- Programmatic selection control without directly manipulating row visuals.
- Clear separation of selection state from UI containers.

## End-to-end flow

1. User gestures update the grid selection.
2. Grid synchronizes with bound `SelectionModel<T>`.
3. `SelectionModel` events update logs, badges, or side panels.
4. Programmatic operations (`Select`, `Clear`, batch restore) use the same model.

## 1. ViewModel wiring

```csharp
using System.Collections.ObjectModel;
using Avalonia.Collections;
using Avalonia.Controls.Selection;

public sealed class CountriesViewModel
{
    public ObservableCollection<Country> Items { get; }
    public DataGridCollectionView View { get; }
    public SelectionModel<Country> SelectionModel { get; } = new() { SingleSelect = false };

    public CountriesViewModel()
    {
        Items = new ObservableCollection<Country>(Countries.All);
        View = new DataGridCollectionView(Items);

        // Optional when sharing across controls that use the same data source.
        SelectionModel.Source = View;
    }

    public void SelectFirstThree()
    {
        using (SelectionModel.BatchUpdate())
        {
            SelectionModel.Clear();
            SelectionModel.Select(0);
            SelectionModel.Select(1);
            SelectionModel.Select(2);
        }
    }
}
```

## 2. XAML wiring (grid + shared control)

```xml
<Grid ColumnDefinitions="2*,*">
  <DataGrid Grid.Column="0"
            ItemsSource="{Binding View}"
            Selection="{Binding SelectionModel}"
            SelectionMode="Extended"
            AutoGenerateColumns="True" />

  <ListBox Grid.Column="1"
           ItemsSource="{Binding View}"
           Selection="{Binding SelectionModel}"
           SelectionMode="Multiple" />
</Grid>
```

The same `SelectionModel<T>` keeps both controls synchronized.

## 3. Preserving selection while mutating data

When reordering/replacing items, snapshot selected items, mutate, then restore by index:

```csharp
using System.Linq;

var snapshot = SelectionModel.SelectedItems.OfType<Country>().ToList();

MutateItems();

using (SelectionModel.BatchUpdate())
{
    SelectionModel.Clear();
    foreach (var item in snapshot)
    {
        var index = Items.IndexOf(item);
        if (index >= 0)
        {
            SelectionModel.Select(index);
        }
    }
}
```

This pattern is used in the stability sample to keep selection attached to items after shuffle/sort/insert/remove.

## 4. Binding with `SelectedItems` and `SelectedCells`

`SelectionModel` works with existing bindings:

```xml
<DataGrid ItemsSource="{Binding View}"
          Selection="{Binding SelectionModel}"
          SelectedItems="{Binding SelectedItems, Mode=TwoWay}"
          SelectionMode="Extended" />
```

For cell selection workflows:

```xml
<DataGrid ItemsSource="{Binding View}"
          SelectionUnit="Cell"
          SelectedCells="{Binding SelectedCells, Mode=TwoWay}" />
```

## 5. Selection behavior switches

- `SelectionMode="Single"` maps to single-select behavior.
- `SelectionUnit` controls row/cell/header selection granularity.
- `CanUserSelectRows` and `CanUserSelectColumns` gate header selection gestures.

## Troubleshooting

- Selection not synchronized between controls:
  Ensure both controls share the same `SelectionModel<T>` and compatible `Source`.
- Selection jumps after data reset:
  Restore by item identity after mutations (batch update pattern above).
- Programmatic select targets wrong row:
  Use indices from the current source (`DataGridCollectionView`), not the original raw collection order.

## Full sample references

- `src/DataGridSample/Pages/SharedSelectionPage.axaml`
- `src/DataGridSample/ViewModels/SharedSelectionViewModel.cs`
- `src/DataGridSample/Pages/SelectionModelStabilityPage.axaml`
- `src/DataGridSample/ViewModels/SelectionModelStabilityViewModel.cs`

## Related articles

- [Selection and Navigation](selection-and-navigation.md)
- [Selection Highlighting](selection-highlighting.md)
- [Sorting Model: End-to-End Usage](sorting-model-end-to-end.md)
- [Filtering Model: End-to-End Usage](filtering-model-end-to-end.md)
