# Virtual cell-surface architecture

`DataGridVisualLayoutMode.Virtualized` is ProDataGrid's control-free display-cell
architecture. It preserves the DataGrid data, selection, row realization, editing,
and column models, but replaces the matrix of retained display-cell controls with
one presenter-owned drawing surface. Retained controls are materialized only when a
feature requires their semantics, most notably editing.

This article describes the implementation contract. For setup and mode selection,
see [Flat row and cell layout](flat-row-cell-layout.md). For measurement rules, see
[Layout performance benchmark methodology](layout-performance-benchmarking.md).

## Goals and boundaries

The architecture is designed for large, fixed-height, read-mostly grids whose
visible columns can read values through typed accessors. Its goals are to:

- keep row virtualization and the public DataGrid model unchanged;
- remove per-cell controls, presenters, bindings, and layout calls from display;
- compute visible column geometry once per presenter pass;
- share bounded text-shaping state across rows and recycling cycles;
- preserve selection, current-cell, frozen-column, hierarchy, pointer, and editing
  behavior; and
- fall back to retained cells when the surface cannot preserve a feature exactly.

It is not a general replacement for arbitrary Avalonia cell templates. The normal
`Nested` mode remains the compatibility default. `Flat` is the retained-control
middle ground.

## Three visual topologies

```text
Nested
DataGridRowsPresenter
└─ DataGridRow
   └─ DataGridCellsPresenter
      └─ DataGridCell × visible columns

Flat
DataGridRowsPresenter
├─ DataGridRow × realized rows
└─ DataGridCell × realized rows × visible columns

Virtualized
DataGridRowsPresenter
├─ DataGridVirtualCellSurface × 1
├─ DataGridRow × realized rows
└─ DataGridCell × 1 only while an editor/compatibility cell is active
```

Rows remain retained controls. They own row selection state, row headers,
hierarchy automation, drag/lifecycle state, and the vertical geometry consumed by
the surface. Removing rows would require a separate row semantics and automation
model; the current architecture deliberately does not create one.

## Backend selection and retained fallback

`DataGrid.UsesVirtualCellSurface` is true only when the requested mode is
`Virtualized` and every visible column and grid-wide feature satisfies the surface
contract. Eligibility is reevaluated when columns, bindings, display modes, themes,
or relevant feature state change.

The surface currently requires:

- a finite `RowHeight`;
- at least one visible column;
- no grid-wide or column-specific cell theme;
- no active conditional-formatting or search descriptors;
- no `CellPrepared` or `CellClearing` handlers;
- no `Auto` or `SizeToCells` visible-column widths; and
- surface support and compatible typed accessor metadata for every visible column.

If any condition fails, the requested `Virtualized` mode remains visible through
the public property, while `UsesVirtualCellSurfaceFallback` becomes true and the
presenter uses flat retained cells. This is an all-visible-columns decision: the
grid never mixes an incomplete drawn representation with retained display cells.
When eligibility returns, the presenter unloads the fallback cells and attaches
the single surface again.

## Supported display columns

The surface currently draws these built-in column configurations:

| Column | Surface contract | Display behavior |
|---|---|---|
| `DataGridTextColumn` | Compatible typed text accessor | Typography, trimming, foreground, and left alignment |
| `DataGridNumericColumn` | Direct raw binding and typed accessor | Column formatting and right-aligned text |
| `DataGridCheckBoxColumn` | Direct raw binding and typed accessor | Centered two- or three-state indicator |
| `DataGridImageColumn` | Fixed image dimensions, direct raw binding, typed accessor | Stretch and stretch-direction geometry |
| `DataGridProgressBarColumn` | No progress text, direct raw binding, typed accessor | Background, foreground, min/max, and fixed bar height |
| `DataGridHierarchicalColumn` | No custom cell template and compatible typed text accessor | Indentation, expander, and hierarchy text |

Derived columns remain conservative unless their exact implementation participates
in the surface contract. Template columns, buttons, toggle controls with custom
content or commands, arbitrary converters requiring the binding engine, and custom
draw operations currently use retained fallback.

## End-to-end pipeline

### 1. Model projection and slot geometry

Flat and hierarchical sources both enter the existing DataGrid slot model.
`HierarchicalModel` owns expansion and exposes a flattened sequence of
`HierarchicalNode` objects. The visual backend does not traverse the domain tree.

Vertical scrolling continues through `ScrollSlotsByHeight`. Small movements reuse
adjacent realized rows. Large movements use the row-height estimator or the indexed
height path, reset or trim the displayed range, and then call
`UpdateDisplayedRows`. The virtual surface therefore shares the same scrolling
correctness, anchoring, collapsed-slot, and variable-height estimator machinery as
the retained modes.

For the supported fixed-height surface workload, total extent can be calculated
without visiting every slot. This keeps extent calculation independent of the
expanded row count.

### 2. Row realization and recycling

`DataGridDisplayData` owns the displayed range and recycle pools. A normal surface
row has an empty `Cells` collection. `CompleteCellsCollection` exits after removing
stale cells unless the row is the active compatibility row.

The exact built-in `DataGrid`/`DataGridRow` path also avoids a redundant
item → `null` → item `DataContext` transition during recycle. It preserves valid
fixed-height measure state until the row is rebound. Custom grids, custom
realization factories, derived row types, placeholder transitions, and retained
fallbacks keep the complete cleanup and regeneration lifecycle.

This distinction is intentional: the optimized path depends on the built-in row
having no retained display-cell bindings or custom cleanup contract.

### 3. Presenter measure and arrange

`DataGridRowsPresenter` remains the single layout owner.

During measure it:

1. updates the realized row range;
2. attaches or detaches the virtual surface;
3. computes visible column layouts once;
4. measures retained row controls;
5. measures only the active compatibility cell, if any;
6. updates row-height observations and logical-scroll extent; and
7. measures the surface to the viewport.

During arrange it:

1. places retained rows vertically from `-NegVerticalOffset`;
2. arranges only the active compatibility cell;
3. arranges the surface across the presenter; and
4. synchronizes visible value-change subscriptions.

Column layout records contain the resolved column, its presenter-relative left
edge, and horizontal visibility. Frozen-left, scrolling, and frozen-right regions
are resolved once rather than once per row.

### 4. Surface rendering

`DataGridVirtualCellSurface` is deliberately stateless. Its `Render` method calls
back into the presenter, which owns all geometry and caches. Rendering iterates the
realized scrolling rows and visible column-layout records.

For each cell the presenter:

1. derives its rectangle from row bounds, row-header width, and column layout;
2. intersects it with the cells viewport and frozen regions;
3. draws selection background when selected;
4. reads the value through the column's typed provider/accessor;
5. draws text, checkbox, image, progress, or hierarchy content;
6. draws current-cell chrome; and
7. draws a vertical grid line when enabled.

A clip scope is created only when the visible rectangle differs from the complete
cell rectangle. The surface skips the active editor cell so the retained overlay is
the only representation while editing.

## Text shaping and cache ownership

Text is the dominant allocation source for many read-only grids. The surface owns a
bounded disposable LRU of `TextLayout` instances. Its key includes:

- text and culture;
- font family, size, style, weight, and stretch;
- alignment, trimming, wrapping, and flow direction;
- maximum width and height; and
- brush kind, color, opacity, or reference identity.

The virtual cache holds 4,096 entries, enough for several benchmark viewports and
discontinuous jumps. Eviction disposes the layout. Detaching the surface clears the
cache. The larger bound is surface-specific; retained drawn-cell cache defaults are
not changed.

## Value invalidation

The surface has no per-cell binding expressions. When a participating column opts
into change tracking, the presenter subscribes once to each visible
`INotifyPropertyChanged` row item and, for hierarchy rows, the wrapped item. The two
reference-identity sets are swapped during arrange so entering items are subscribed
and leaving items are unsubscribed without rebuilding unrelated state.

Any relevant property notification invalidates the surface. Notifications arriving
off the UI thread are posted at render priority. Immutable workloads can disable
tracking on supported text and hierarchy columns to remove these subscriptions.

## Hit testing, selection, hierarchy, and editing

The surface implements custom hit testing. Pointer coordinates are resolved against
realized row bounds and visible column geometry. A hierarchy-expander hit toggles
the node directly; other hits route through the DataGrid's existing selection,
currency, and edit-trigger logic.

Editing does not introduce a second editor model:

1. the grid designates the realized row as its virtual compatibility row;
2. `CompleteCellsCollection` materializes normal cells for that row;
3. the selected cell becomes a flat presenter child above the surface;
4. the existing column creates its normal retained editor and binding;
5. commit, validation, or cancel uses the existing editing pipeline; and
6. leaving edit mode releases the row's cells and returns to zero display cells.

Programmatic `GetCellContent` can also request this compatibility materialization.
Applications should not call it repeatedly while profiling the zero-cell display
path.

Row-level selection and hierarchy automation remain provided by retained row peers.
The surface does not currently expose one automation peer per drawn display cell;
features requiring retained cell peers should use `Flat` or `Nested`.

## Invalidation and mode transitions

Changes that only affect drawn values invalidate the surface visual. Geometry or
eligibility changes reset flat ownership, invalidate presenter measure, and may
transition between the surface and retained fallback. Changing
`VisualLayoutMode` after first measure unloads realized elements, refreshes rows and
columns, restores selection, and remeasures headers and rows.

The implementation avoids calling `UpdateLayout` from production paths. Layout is
driven by Avalonia invalidation; explicit `UpdateLayout` is confined to tests and
benchmark phase boundaries.

## Structural and behavioral invariants

A valid surface state must satisfy all of these conditions:

- exactly one `DataGridVirtualCellSurface` is attached;
- realized row count remains bounded by the viewport and prefetch policy;
- no display `DataGridCell` exists outside an active compatibility operation;
- every realized row has zero cells outside compatibility materialization;
- surface bounds and logical-scroll extent match the presenter viewport and data;
- frozen clipping, selection geometry, and hit testing use the same column layout;
- leaving the mode unsubscribes value notifiers and disposes cached layouts; and
- retained fallback preserves the normal cell lifecycle instead of simulating it.

Headless tests assert these invariants across initial layout, large scrolling,
recycling, pointer selection, value changes, editing, eligibility transitions, and
unsupported columns.

## Extension checklist

Adding another surface-rendered column requires more than drawing a similar shape.
The implementation and tests must prove:

1. direct typed value access with no reflection;
2. exact display formatting and null-state behavior;
3. correct property-change invalidation policy;
4. selection/current/grid-line and frozen clipping composition;
5. pointer and keyboard behavior;
6. normal retained editor materialization, commit, cancel, and validation;
7. conservative fallback for derived or unsupported configurations;
8. zero display-cell structural validation; and
9. matched application-level timing, allocation, and memory evidence.

## Current design limits

Use `Flat` or `Nested` when the grid depends on arbitrary cell templates, custom
cell themes, cell lifecycle handlers, search/conditional-formatting cell visuals,
cell automation peers, auto/size-to-cells measurement, or unsupported interactive
display controls. Variable row heights, row details, and collection-view group rows
share the general row machinery but are not the optimized surface contract and
should be validated on the application's exact templates before adoption.
