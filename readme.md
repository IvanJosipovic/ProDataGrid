# ProDataGrid

[![Build](https://github.com/wieslawsoltes/Avalonia.Controls.DataGrid/actions/workflows/build.yml/badge.svg)](https://github.com/wieslawsoltes/Avalonia.Controls.DataGrid/actions/workflows/build.yml)

[![Release](https://github.com/wieslawsoltes/Avalonia.Controls.DataGrid/actions/workflows/release.yml/badge.svg)](https://github.com/wieslawsoltes/Avalonia.Controls.DataGrid/actions/workflows/release.yml)
[![GitHub Release](https://img.shields.io/github/v/release/wieslawsoltes/Avalonia.Controls.DataGrid.svg)](https://github.com/wieslawsoltes/Avalonia.Controls.DataGrid/releases)

[![NuGet](https://img.shields.io/nuget/v/ProDataGrid.svg)](https://www.nuget.org/packages/ProDataGrid/)

## About

`ProDataGrid` is a hard fork of the original `Avalonia.Controls.DataGrid` control for [Avalonia](https://github.com/AvaloniaUI/Avalonia).

It displays repeating data in a customizable grid with enhanced features and improved performance, and is maintained as an independent NuGet package to evolve faster than the in-box control.

## Features

| Area | Highlights |
| --- | --- |
| Virtualization & scrolling | ScrollViewer-based `ILogicalScrollable` presenter, smooth wheel/gesture handling, snap points, anchor support, predictive row prefetch, frozen columns. |
| Columns | Text, template, checkbox columns; auto/star/pixel sizing; reordering, resizing, visibility control, frozen sections. |
| Rows | Variable-height support with pluggable estimators; row details; grouping headers; selection modes; row headers. |
| Editing & navigation | In-place editing, commit/cancel, keyboard navigation, clipboard copy modes, current cell tracking. |
| Data operations | Sorting, grouping, paging, currency management via `DataGridCollectionView` family; selection built on Avalonia `SelectionModel` for stable binding across sort/filter. |
| Styling & theming | Fluent/Simple v2 ScrollViewer templates, row/cell styling, template overrides, theme resources, focus/selection visuals. |
| Data binding | Auto-generates columns from `DataTable.DefaultView` and binds cells via TypeDescriptor (no manual indexers), `SelectedItems` two-way binding support, `DataGridCollectionView` for sorting/grouping/editing. |

## Supported targets

- .NET 6.0 and 10.0; .NET Standard 2.0 for compatibility.
- Avalonia 11.3.x (see `Directory.Packages.props`).
- Windows, Linux, and macOS (via Avalonia’s cross-platform stack).

## Installation

Install from NuGet:

```sh
dotnet add package ProDataGrid
```

Or add a package reference:

```xml
<PackageReference Include="ProDataGrid" Version="..." />
```

## Usage

Basic setup with common column types and width modes (pixel, auto, star):

```xml
<DataGrid Items="{Binding People}"
          AutoGenerateColumns="False"
          CanUserResizeColumns="True"
          UseLogicalScrollable="True"
          GridLinesVisibility="Horizontal">
  <DataGrid.Columns>
    <!-- Pixel width -->
    <DataGridTextColumn Header="ID"
                        Binding="{Binding Id}"
                        Width="60" />

    <!-- Auto sizes to content -->
    <DataGridTextColumn Header="Name"
                        Binding="{Binding Name}"
                        Width="Auto" />

    <!-- Fixed pixel width checkbox column -->
    <DataGridCheckBoxColumn Header="Active"
                            Binding="{Binding IsActive}"
                            Width="80" />

    <!-- Star sizing shares remaining space -->
    <DataGridTextColumn Header="Department"
                        Binding="{Binding Department}"
                        Width="*" />

    <!-- Template column with custom content and weighted star width -->
    <DataGridTemplateColumn Header="Notes"
                            Width="2*">
      <DataGridTemplateColumn.CellTemplate>
        <DataTemplate>
          <TextBlock Text="{Binding Notes}"
                     TextWrapping="Wrap" />
        </DataTemplate>
      </DataGridTemplateColumn.CellTemplate>
    </DataGridTemplateColumn>
  </DataGrid.Columns>
</DataGrid>
```

Widths accept pixel values (`"80"`), `Auto` (content-based), `*` or weighted stars (e.g., `2*`) that share remaining space.

## Using SelectionModel with DataGrid

DataGrid exposes a `Selection` property (`ISelectionModel`) so you can plug in your own selection model or share one across controls.

- Bind `Selection` to your view-model’s `SelectionModel<T>`:

  ```xml
  <DataGrid ItemsSource="{Binding Items}"
            Selection="{Binding MySelectionModel}"
            SelectionMode="Extended" />
  ```

  ```csharp
  public SelectionModel<MyItem> MySelectionModel { get; } = new()
  {
      SingleSelect = false
  };
  ```

- Let the grid assign `Selection.Source` to its collection view; avoid pre-setting `Source` to prevent mismatches.
- You can share the same `SelectionModel<T>` with other controls (e.g., `ListBox Selection="{Binding MySelectionModel}"`) to keep selection in sync.
- `SelectedItems` binding still works; when `Selection` is set, it reflects the model’s selection and updates it when the binding changes.

## Package Rename

This package has been renamed from `Avalonia.Controls.DataGrid` to `ProDataGrid`.

The new name gives the fork its own NuGet identity (so it can ship independently of Avalonia), avoids collisions with the built-in control, and signals the performance/features added in this branch.

The fork is maintained at https://github.com/wieslawsoltes/ProDataGrid.

### Migration

To migrate from the original package, update your NuGet reference:

```xml
<!-- Old -->
<PackageReference Include="Avalonia.Controls.DataGrid" Version="..." />

<!-- New -->
<PackageReference Include="ProDataGrid" Version="..." />
```

## ScrollViewer-based implementation (v2)

ProDataGrid now ships a ScrollViewer-based template that implements `ILogicalScrollable` on `DataGridRowsPresenter`. This removes the custom `PART_VerticalScrollbar`/`PART_HorizontalScrollbar` pair and lets Avalonia handle scroll bars, scroll chaining, and inertia.

Add the v2 theme to opt into the new template (enables `UseLogicalScrollable` by default):

```xml
<!-- Fluent -->
<StyleInclude Source="avares://Avalonia.Controls.DataGrid/Themes/Fluent.v2.xaml" />

<!-- or Simple -->
<StyleInclude Source="avares://Avalonia.Controls.DataGrid/Themes/Simple.v2.xaml" />
```

If you use a custom control template, wrap `DataGridRowsPresenter` in a `ScrollViewer` named `PART_ScrollViewer` and set `UseLogicalScrollable="True"`. Keep the column headers in a separate row so they stay fixed while rows scroll.

## Row height estimators

Scrolling with variable row heights is now driven by pluggable estimators via `RowHeightEstimator`:

- `AdvancedRowHeightEstimator` (default): regional averages + Fenwick tree for accurate offsets.
- `CachingRowHeightEstimator`: caches per-row heights for predictable datasets.
- `DefaultRowHeightEstimator`: average-based for uniform rows.

Override the estimator per grid:

```xml
<!-- declare xmlns:controls="clr-namespace:Avalonia.Controls;assembly=ProDataGrid" -->
<DataGrid UseLogicalScrollable="True">
  <DataGrid.RowHeightEstimator>
    <controls:CachingRowHeightEstimator />
  </DataGrid.RowHeightEstimator>
</DataGrid>
```

## Migrating existing usage

- Prefer the v2 theme or update your template to use the ScrollViewer pattern; legacy scroll bars remain available when `UseLogicalScrollable="False"`.
- Replace direct access to template scroll bars with `ScrollViewer` APIs (`ScrollChanged`, `Offset`, `Extent`, `Viewport`).
- When handling wheel/gesture input, rely on the built-in logic (it routes through `UpdateScroll` when `UseLogicalScrollable` is true).
- For theme v2, ensure frozen columns and header separators are kept in sync with horizontal offset (the supplied templates already do this).
- If you depend on stable scroll positioning with dynamic row heights, choose the estimator that matches your data set and reset it after data source changes if needed.

## Selection model integration

ProDataGrid now routes row selection through Avalonia’s `SelectionModel<object?>`, giving stable `SelectedItem/SelectedItems/SelectedIndex` bindings across sorting, filtering, paging, and collection mutations. Highlights:

- `SelectedItems` remains an `IList` (for bindings) but is backed by the selection model; adding/removing in the bound collection updates the grid, and vice versa.
- Selection survives collection changes (including sorted `DataGridCollectionView` inserts/moves) without losing currency; current row is preserved when possible.
- Multi-select gestures and `SelectionMode` map to the model (`SelectionMode=Single` ↔ `SingleSelect=true`).
- A thin adapter keeps row index ↔ slot mapping internal, so custom selection models can be injected later.

## Sorting model integration

Sorting is now driven by a dedicated `ISortingModel` and adapter instead of directly mutating `SortDescriptions` from the header. This keeps sort state explicit, pluggable, and testable:

- Configure gesture policies on the model via `IsMultiSortEnabled`, `SortCycleMode` (2- or 3-state), and `OwnsSortDescriptions` (strict vs observe external changes).
- Per-column comparers/culture and `SortMemberPath` flow into `SortingDescriptor`s; duplicate-column guards and batch updates prevent drift.
- Plug in a custom model without subclassing the grid: set `SortingModel` or `SortingModelFactory` before use to inject alternate sort pipelines (e.g., server-side).
- Plug in a custom adapter without subclassing the grid: set `SortingAdapterFactory` to supply a specialized adapter (e.g., DynamicData/server-side) that can short-circuit local `SortDescriptions` churn via `TryApplyModelToView`.
- The adapter mirrors `Sorting` ↔ `DataGridCollectionView.SortDescriptions`, logging/rolling back on failures and refreshing rows while preserving selection snapshots.
- Gestures are unchanged (click/shift/meta), but glyphs and programmatic updates now reflect the model, so tests and automation can drive sorting through the same surface.
- Centralized descriptors plus `SortingChanging/SortingChanged` events make sort state observable; batch updates and duplicate-column guards prevent drift.
- Observe mode keeps the model in sync when `SortDescriptions` is mutated externally; strict mode keeps the model authoritative.
- Per-column `CustomSortComparer`/culture/paths are preserved in `SortingDescriptor`s, so custom comparers are first-class (including natural/ordinal/culture-aware string sorts).
- Sample: see `Sorting Model Playground` in `src/DataGridSample` for presets, external-sort reconciliation, and event logging.

### Using SortingModel with DataGrid

Bind a custom sorting model or factory just like selection:

```xml
<DataGrid ItemsSource="{Binding Items}"
          SortingModel="{Binding MySortingModel}"
          IsMultiSortEnabled="True"
          SortCycleMode="AscendingDescendingNone"
          OwnsSortDescriptions="True">
  <DataGrid.Columns>
    <DataGridTextColumn Header="Name"
                        Binding="{Binding Name}"
                        SortMemberPath="Name" />
    <DataGridTextColumn Header="Status"
                        Binding="{Binding Status}"
                        SortMemberPath="Status"
                        CustomSortComparer="{Binding StatusComparer}" />
  </DataGrid.Columns>
</DataGrid>
```

```csharp
public ISortingModel MySortingModel { get; } = new SortingModel
{
    MultiSort = true,
    CycleMode = SortCycleMode.AscendingDescendingNone,
    OwnsViewSorts = true
};

public IComparer StatusComparer { get; } = new StatusComparer();

public void ApplyPreset()
{
    MySortingModel.Apply(new[]
    {
        new SortingDescriptor("Status", ListSortDirection.Ascending, "Status", StatusComparer),
        new SortingDescriptor("Name", ListSortDirection.Ascending, "Name")
    });
}
```

Or swap the model creation globally:

```csharp
dataGrid.SortingModelFactory = new MyCustomSortingFactory();
```

### DynamicData integration

To keep sorting upstream in a DynamicData pipeline (no local `SortDescriptions` churn), supply a custom adapter factory and feed a comparer subject into `Sort`:

```csharp
var source = new SourceList<Deployment>();
source.AddRange(Deployment.CreateSeed());

var adapterFactory = new DynamicDataSortingAdapterFactory(log => Debug.WriteLine(log));
var comparerSubject = new BehaviorSubject<IComparer<Deployment>>(adapterFactory.SortComparer);

source.Connect()
      .Sort(comparerSubject) // DynamicData performs the sort
      .Bind(out _view)
      .Subscribe();

SortingModel sortingModel = new SortingModel
{
    MultiSort = true,
    CycleMode = SortCycleMode.AscendingDescendingNone,
    OwnsViewSorts = true
};

sortingModel.SortingChanged += (_, e) =>
{
    adapterFactory.UpdateComparer(e.NewDescriptors);
    comparerSubject.OnNext(adapterFactory.SortComparer);
};

// In code-behind (since SortingAdapterFactory cannot be bound directly in XAML):
grid.SortingModel = sortingModel;
grid.SortingAdapterFactory = adapterFactory;
```

Header clicks update the `SortingModel`; the custom adapter overrides `TryApplyModelToView`, so instead of touching `SortDescriptions` it rebuilds a `SortExpressionComparer` chain and pushes it to the DynamicData `Sort` operator. The grid still shows glyphs/state from the `SortingModel`, but the actual sort happens in the DynamicData pipeline.

## Samples

- The sample app (`src/DataGridSample`) includes pages for pixel-perfect columns, frozen columns, large datasets, and variable-height scenarios (`Pages/*Page.axaml`).
- Run it locally with `dotnet run --project src/DataGridSample/DataGridSample.csproj` to see templates, estimators, `SelectedItems` binding, and the `DataTable.DefaultView` page that demonstrates the TypeDescriptor-based column binding.

## License

ProDataGrid is licensed under the [MIT License](licence.md).

The original Avalonia.Controls.DataGrid license is preserved in [licence-avalonia.md](licence-avalonia.md).
