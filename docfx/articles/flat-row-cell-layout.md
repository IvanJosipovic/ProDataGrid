# Flat row and cell layout

The flat row and cell layout is an opt-in rendering architecture for large,
fixed-height grids. `Flat` keeps the existing row and cell containers but places
them directly under one `DataGridRowsPresenter`. `Virtualized` keeps retained
rows but draws compatible display cells on one presenter-owned surface. It
materializes only the active editing cell and falls back to flat retained cells
whenever a column or grid feature requires a control.

The default is still `DataGridVisualLayoutMode.Nested`. Existing applications do
not change unless they select the keyed `DataGridFlatTheme` or set
`VisualLayoutMode="Flat"` or `VisualLayoutMode="Virtualized"` with a compatible
row theme.

> [!IMPORTANT]
> The initial flat-layout contract requires a finite `RowHeight`, pixel or star
> visible-column widths, and `HeadersVisibility="Column"`. Row details, row
> headers, and collection-view group rows should continue to use the nested
> layout.

## Why this architecture exists

The retained and drawn cell paths reduce the visual subtree *inside* each cell,
but the standard layout still creates a nested presenter for every realized row:

```text
DataGridRowsPresenter
└─ DataGridRow
   └─ DataGridCellsPresenter
      ├─ DataGridCell
      ├─ DataGridCell
      └─ ...
```

The flat mode removes that repeated layout boundary:

```text
DataGridRowsPresenter
├─ DataGridRow
├─ DataGridCell  (row 0, column 0)
├─ DataGridCell  (row 0, column 1)
├─ DataGridRow
├─ DataGridCell  (row 1, column 0)
└─ ...
```

The virtual cell mode removes the display-cell controls as well:

```text
DataGridRowsPresenter
├─ DataGridVirtualCellSurface  (all compatible display cells)
├─ DataGridRow
├─ DataGridRow
├─ ...
└─ DataGridCell               (active editor only, while editing)
```

This follows the central idea used by CDP's `FlatSplitPanel`: keep the semantic
model intact, compute geometry centrally, and arrange the realized controls as
siblings on one surface. In ProDataGrid, the data model can be either a flat
collection or a `HierarchicalModel`; visual flattening is independent of data
hierarchy.

## Architecture contract

`DataGridRowsPresenter` is the single visual-layout owner in flat mode. During
its measure pass it synchronizes the cells belonging to the realized scrolling
rows and measures them with the owning column's resolved width and the row's
fixed height. Column positions, rounded widths, and horizontal visibility are
computed once per presenter pass rather than once per row. Recycled cells remain
in a bounded, hidden presenter-owned pool so
collapse and scroll cycles do not repeatedly detach and rebuild their visual and
logical relationships. During arrange it
computes each cell rectangle from:

- the row's vertical slot;
- the column's resolved width;
- horizontal scroll offset;
- left- and right-frozen column regions; and
- the visible cells viewport.

The implementation preserves these invariants:

| Concern | Invariant |
|---|---|
| Semantic ownership | `DataGridCell.OwningRow` and `OwningColumn` remain authoritative; each cell remains a logical child of its row. |
| Container lifecycle | The existing display-data and realization-factory paths create, recycle, and unload rows and cells. |
| Visual ownership | Every realized scrolling cell has `DataGridRowsPresenter` as its direct visual parent. |
| Data context | A promoted cell mirrors its logical owning row's data context through a guarded local value; recycling updates it only when the item changes. |
| Virtualization | Cells outside the realization window remain hidden in the bounded recycled pool; horizontally hidden cells are not measured or arranged. |
| Drawn text | One presenter-owned text-layout cache reuses shaping results across rows and recycling cycles; original cell cache settings are restored when flat ownership ends. |
| Grid lines | Fixed row content keeps `RowHeight`; a horizontal line contributes the same extra pixel as nested layout and is drawn directly without another visual. |
| Frozen columns | Left, scrolling, and right-frozen regions use the same column metadata as the nested path. |
| Compatibility | Nested layout remains the default and retains its existing templates and behavior. |
| AOT | The path uses ordinary typed APIs and introduces no reflection. |

Rows remain controls because they carry selection, automation, drag, and
lifecycle semantics. In `Flat`, cells remain controls. In `Virtualized`, the
surface centrally owns cell geometry, drawing, clipping, hit testing, selection,
current-cell state, frozen regions, hierarchy expanders, and shared text layout.
One normal cell is overlaid for editing and validation. Visible model objects
implementing `INotifyPropertyChanged` invalidate the surface without creating
bindings or cell controls.

The virtual surface supports text, masked text, autocomplete text, editable combo-box text,
slider value text, numeric, checkbox, date, time, image, progress, and hierarchical columns
when they have compatible typed accessors. Text, masked/autocomplete/combo-box binding
formatting, numeric, date, time and slider-value formatting, text column typography,
combo-box dropdown glyphs, image stretch, progress styling,
hierarchy indentation, selection, grid lines, and frozen clipping are drawn directly. It deliberately
falls back to `Flat` for arbitrary templates, interactive display controls,
custom grid or column cell themes, auto/size-to-cells columns, progress text, custom converters
that need the binding engine, conditional formatting/search descriptors, and
cell lifecycle event handlers. This fallback is the compatibility mechanism;
the grid never presents an incomplete drawn substitute for those features.

## Enable the flat layout

Include one base theme and the optional flat resources:

```xml
<Application.Styles>
  <FluentTheme />
  <StyleInclude Source="avares://Avalonia.Controls.DataGrid/Themes/Fluent.v2.xaml" />
  <StyleInclude Source="avares://Avalonia.Controls.DataGrid/Themes/Flat.xaml" />
</Application.Styles>
```

`Fluent.Flat.xaml` and `Simple.Flat.xaml` are convenience includes that combine
the corresponding v2 DataGrid theme with `Flat.xaml`. The application must still
include Avalonia's `FluentTheme` or `SimpleTheme`.

Apply the keyed theme only to grids intended for the fast path:

```xml
<DataGrid Theme="{StaticResource DataGridFlatTheme}"
          RowHeight="32"
          HeadersVisibility="Column"
          AutoGenerateColumns="False"
          ItemsSource="{CompiledBinding Rows}">
  <DataGrid.Columns>
    <DataGridTextColumn Header="Id"
                        Width="80"
                        Binding="{Binding Id}" />
    <DataGridTextColumn Header="Name"
                        Width="240"
                        Binding="{Binding Name}" />
  </DataGrid.Columns>
</DataGrid>
```

Select the fully drawn display path explicitly after applying the same flat
theme:

```xml
<DataGrid Theme="{StaticResource DataGridFlatTheme}"
          VisualLayoutMode="Virtualized"
          RowHeight="32" />
```

Use column definitions with compiled/typed value accessors. A hand-created
column can opt in by assigning `DataGridColumnMetadata.ValueAccessor`; the
surface does not introduce reflection to evaluate binding paths.

The keyed theme sets:

- `VisualLayoutMode="Flat"`;
- a row theme without `DataGridCellsPresenter`;
- `RowHeight="32"`;
- `UseLightweightFiller="True"`.

Including `Flat.xaml` alone never changes the default theme of another grid.

## Flat data and hierarchical data

For ordinary flat data, bind `ItemsSource` as usual. For hierarchical data,
continue to configure `HierarchicalModel`, `HierarchicalRowsEnabled`, and a
`DataGridHierarchicalColumn`. The hierarchy model owns expansion and exposes its
flattened node sequence; the flat visual layout only changes where realized cell
controls live.

This distinction is deliberate:

```text
hierarchical domain model ──flattened nodes──► row virtualization
                                                   │
                                                   ▼
                                  flat sibling visual surface
```

Collapsing a branch changes the flattened node sequence. Normal DataGrid
realization then unloads the affected rows, and the flat surface hides their
cells during the next synchronized layout pass so the containers can be reused
without reconstructing the flat relationships.

## Comparison samples

The sample application contains four standalone pages so the visual layout and
the data shape can be varied independently:

| Data shape | Nested baseline | Flat sibling surface |
|---|---|---|
| Flat rows | `NestedSurfaceFlatDataPage` | `FlatSurfaceFlatDataPage` |
| Hierarchical rows | `NestedSurfaceHierarchyPage` | `FlatSurfaceHierarchyPage` |

The paired pages use the same ViewModels, columns, 520-pixel grid viewport, and
cell-path selector. The flat-data pair can load up to one million items. The
hierarchy pair's representative workload expands to 149,792 nodes and exposes
the existing standard retained, optimized retained, optimized hierarchy
presenter, direct hierarchy, built-in drawn, and custom Skia paths.

## Compatibility matrix

| Feature | Initial flat mode | Guidance |
|---|---:|---|
| Fixed-height flat rows | Supported | Preferred workload. |
| `HierarchicalModel` rows | Supported | Use fixed heights and explicit column widths. |
| Text, checkbox, template, direct, retained, and drawn cells | Supported by `Flat` | Cell controls keep their existing behavior. |
| Typed text, masked/autocomplete/editable-combo-box/slider text, numeric, checkbox, date, time, image, progress, hierarchy display | Drawn by `Virtualized` | Uses one surface and zero display-cell controls. |
| Editing and validation | Supported | `Virtualized` overlays one normal active editor cell. |
| Templates, interactive display controls, custom themes | Retained fallback | `Virtualized` automatically uses flat retained cells. |
| Frozen left/right columns | Supported | Geometry and clipping are computed centrally. |
| Selection, current cell, fill geometry | Supported | Virtual geometry is used when no cell control exists. |
| Pixel and star column sizing | Supported | Columns must resolve to finite widths. |
| Auto/size-to-cells column sizing | Retained fallback | Resolve columns to pixel or star widths for the virtual surface. |
| Variable/auto row heights | Not in the initial contract | Use `Nested`. |
| Row headers | Not in the initial flat theme | Use `HeadersVisibility="Column"` or `Nested`. |
| Row details | Not in the initial flat theme | Use `Nested`. |
| Collection-view group headers | Not optimized | Use `Nested` for grouped views. |

If an application needs a feature outside this table, keep that grid on the
default nested mode. The mode is per-grid, so both architectures can coexist in
the same view.

## Performance validation

Treat the flat mode as a performance option, not an assumption that every grid
will be faster. Compare identical datasets, viewport sizes, columns, cell modes,
and scroll traces.

At minimum, record:

1. realized row and cell counts after initial layout and after a long scroll;
2. visual and logical descendant counts and maximum visual depth;
3. layout-pass duration and allocation rate during repeated vertical and
   horizontal scrolling; and
4. retained objects after recycling and closing the host window.

The structural headless tests assert that flat cells are direct presenter
children, nested grids retain per-row presenters, and scrolling removes stale
direct children. Benchmark numbers should be published only with runtime,
platform, viewport, dataset, and build configuration attached.

The matched hierarchy benchmark is
`OptimizedHierarchyFlatLayoutPerformanceTests`. It expands the representative
149,792-node model, collapses it, pumps layout, captures a headless frame, and
writes the raw samples to
`artifacts/performance/optimized-hierarchy-layout/optimized-hierarchy-layout-comparison.json`.
Run it with:

```bash
dotnet test src/DataGridSample.UnitTests/DataGridSample.UnitTests.csproj \
  -c Release \
  --filter 'FullyQualifiedName~OptimizedHierarchyFlatLayoutPerformanceTests'
```

The model-only `HierarchyCollapseBenchmarks` remains useful for separating
hierarchy mutation cost from UI work:

```bash
dotnet run --project tests/ProDataGrid.Hierarchy.Benchmarks/ProDataGrid.Hierarchy.Benchmarks.csproj \
  -c Release -- --filter '*HierarchyCollapseBenchmarks*' --job short
```

### Representative local result

The authoritative 2026-08-12 run uses BenchmarkDotNet 0.15.8, three independent
launches, managed-allocation diagnostics, tiered compilation disabled, .NET
10.0.5 Arm64, macOS 26.6, and an Apple M3 Pro. The complete distributions and
all six cell paths are recorded in the
[checked-in benchmark report](https://github.com/wieslawsoltes/Avalonia.Controls.DataGrid/blob/master/tests/ProDataGrid.FlatLayout.Benchmarks/RESULTS-2026-08-12.md).

For built-in drawn text, pending layout measured 4.085 ms nested and 2.702 ms
flat: 34% faster with 41% less allocation. The complete collapse, layout, and
render operation improved from 10.135 to 8.643 ms (15%) with 39% less
allocation. Retained paths improved by 1–6%; they retain the same cell content
tree and therefore cannot get a major gain merely from removing one presenter
per realized row. The model-only 149,792-node `CollapseAll` run remained
independent of the visual architecture at 4.39 KB allocated.

The later virtual-surface comparison on the same machine and built-in-drawn
149,792-node path measured pending collapse layout at 4.193 ms nested, 3.039 ms
flat, and 2.772 ms virtualized. End-to-end collapse plus layout measured 10.130,
8.411, and 8.306 ms respectively. The ShortRun samples are directional because
each iteration is shorter than BenchmarkDotNet's recommended 100 ms; the native
source comparison below supplies the broader first-render, expand, collapse,
scroll, allocation, and structural evidence.

The full [virtual-surface report](https://github.com/wieslawsoltes/ProDataGrid/blob/main/tests/ProDataGrid.FlatLayout.Benchmarks/VIRTUAL-SURFACE-RESULTS-2026-08-12.md),
including the matched ProDataGrid retained, drawn-cell, and Wieslaw TreeDataGrid
source runs, is checked in with the benchmark harness. Raw output remains under
the gitignored `artifacts/virtual-cell-surface` directory.

## Design boundaries and extension path

The initial implementation intentionally does not create a second row/cell
lifecycle or a parallel column model. New container types should continue to use
`IDataGridRealizationFactory`; new cell renderers should continue to use the
existing column contracts. Future support for auto sizing, row headers, or row
details belongs behind focused geometry/decoration abstractions rather than
special cases in cell controls.

The approach is inspired by the sibling-surface layout in the
[CDP project](https://github.com/wieslawsoltes/CDP). See also
[Scrolling and virtualization](scrolling-virtualization.md),
[Optimized retained and drawn cells](optimized-cell-paths.md), and
[Hierarchical data](hierarchical-data.md). The implementation-level contracts are
documented in [Virtual cell-surface architecture](virtual-surface-architecture.md),
and the phase model and acceptance rules are in
[Layout performance benchmark methodology](layout-performance-benchmarking.md).
