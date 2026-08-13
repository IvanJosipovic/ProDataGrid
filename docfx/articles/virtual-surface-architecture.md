# Virtual cell-surface architecture

`DataGridVisualLayoutMode.Virtualized` is ProDataGrid's control-free display-cell
architecture. Its default fixed-height path preserves the DataGrid data, selection,
editing, and column models while replacing both the realized display-cell matrix and
the scrolling `DataGridRow` window with presenter-owned lightweight row records and
one drawing surface. Retained controls are materialized only when a feature requires
their semantics, most notably editing and automation.

This article describes the implementation contract. For setup and mode selection,
see [Flat row and cell layout](flat-row-cell-layout.md). For measurement rules, see
[Layout performance benchmark methodology](layout-performance-benchmarking.md).

## Goals and boundaries

The architecture is designed for large, fixed-height, read-mostly grids whose
visible columns can read values through typed accessors. Its goals are to:

- keep the public DataGrid, slot, selection, and editing models unchanged;
- make fixed-height scrolling direct slot arithmetic instead of a row-container
  recycle/generate operation;
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
└─ DataGridVirtualRowInfo × visible rows (non-visual value records)
```

The steady-state eligible path has zero `DataGridRow` and zero `DataGridCell`
controls. `DataGridRowsPresenter` owns reusable `DataGridVirtualRowInfo` records
containing slot, row index, item, top, and fixed height. Selection, current-cell,
pointer, hierarchy-expander, value-notification, render, measure, and arrange logic
consume those records directly.

Compatibility is deliberate rather than partial. Editing materializes the retained
visible row window and the existing editor cell pipeline for the duration of the
edit. Creating the grid automation peer permanently selects retained rows for that
grid instance so existing row peers and automation contracts remain valid. Row
headers, row numbers, row details, loading/unloading handlers, grouped or collapsed
slot tables, custom grids/factories, and item-owned `DataGridRow` containers also use
the retained-row path.

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
| `DataGridDatePickerColumn` | Direct raw binding and typed `DateTime` accessor | Short, long, or custom formatted date text with column alignment |
| `DataGridTimePickerColumn` | Direct raw binding and typed `TimeSpan` accessor | 12/24-hour, optional-seconds, or custom formatted time text |
| `DataGridMaskedTextColumn` | Direct text binding and compatible typed text accessor | Raw bound display text; mask, prompts, culture, and watermark remain editor concerns |
| `DataGridAutoCompleteColumn` | Direct text binding and compatible typed text accessor | Raw bound display text; suggestions, filtering, completion, item templates, and watermark remain editor concerns |
| `DataGridSliderColumn` | `ShowValueText`, direct `Binding`, and compatible typed text accessor | Centered `ValueTextFormat` display; graphical slider display remains retained and the interactive slider remains the editor |
| `DataGridComboBoxColumn` | `IsEditable`, direct `TextBinding`, no selected-item/value binding, and compatible typed text accessor | Formatted text and dropdown glyph; items, templates, free-form input, selection, and dropdown interaction remain on the editor |
| `DataGridImageColumn` | Fixed image dimensions, direct raw binding, typed accessor | Stretch and stretch-direction geometry |
| `DataGridProgressBarColumn` | No progress text, direct raw binding, typed accessor | Background, foreground, min/max, and fixed bar height |
| `DataGridHierarchicalColumn` | No custom cell template and compatible typed text accessor | Indentation, expander, and hierarchy text |

Derived columns remain conservative unless their exact implementation participates
in the surface contract. Combo-box selected-item/value display, template columns,
buttons, toggle controls with custom content or commands, arbitrary converters
requiring the binding engine, and custom draw operations currently use retained
fallback.

## End-to-end pipeline

### 1. Model projection and slot geometry

Flat and hierarchical sources both enter the existing DataGrid slot model.
`HierarchicalModel` owns expansion and exposes a flattened sequence of
`HierarchicalNode` objects. The visual backend does not traverse the domain tree.

Vertical scrolling still enters through `ScrollSlotsByHeight`. When the lightweight
fixed-height guards pass, it clamps the offset, derives the first slot and fractional
offset arithmetically, updates the displayed virtual range, and rewrites the existing
presenter-owned row-record list. It does not unload, pool, retarget, generate, attach,
measure, or arrange `DataGridRow` controls. Unsupported cases continue through the
existing retained scrolling, anchoring, collapsed-slot, and variable-height paths.

For the supported fixed-height surface workload, total extent can be calculated
without visiting every slot. This keeps extent calculation independent of the
expanded row count.

### 2. Lightweight row projection and compatibility realization

`DataGridDisplayData` can represent a virtual scrolling range without owning a
matching circular list of controls. `DataGridRowsPresenter` keeps one bounded list
of `DataGridVirtualRowInfo` structs, reuses its storage across jumps, and projects
the target items directly from the slot model. The list is the sole visible-row
input for virtual rendering, hit testing, notification tracking, measure, and
arrange.

The steady-state scroll entry calls the lightweight projector directly instead of
re-entering the general displayed-row dispatcher. Because eligibility already
proves that the range is ungrouped and uncollapsed, slot and row indexes are equal
and the range is contiguous. A reusable staging buffer preserves list identity and
transactional publication. Overlapping slots reuse their existing item references;
only entering slots query the data connection. General refreshes request a full
projection so source changes cannot reuse stale items.

The projection is enabled only for the exact built-in grid and realization factory
with a finite fixed row height and no row-level compatibility feature. Discovering
an item-owned `DataGridRow` aborts the projection before publishing the range and
selects the normal retained lifecycle. That keeps custom cleanup, derived types,
headers, details, grouping, automation, and editing on the established container
contracts.

### 3. Presenter measure and arrange

`DataGridRowsPresenter` remains the single layout owner.

During measure it:

1. updates the realized row range;
2. attaches or detaches the virtual surface;
3. computes visible column layouts once;
4. computes the fixed row-record range and logical-scroll extent; and
5. measures the surface to the viewport.

During arrange it:

1. assigns row-record tops from `-NegVerticalOffset` and fixed row height;
2. arranges the surface across the presenter; and
3. synchronizes visible value-change subscriptions.

Column layout records contain the resolved column, its presenter-relative left
edge, horizontal visibility, and the final visible left edge and width after cells-
viewport and frozen-region clipping. Frozen-left, scrolling, and frozen-right
regions are resolved once per column-layout pass rather than once per visible cell
during rendering, hit testing, and bounds lookup.

Immediately before recording the surface, the presenter derives a compact render
plan for each visible column. A plan snapshots the column layout, renderer kind,
typed value provider/accessor, text converter/parameter/format/culture, text style
and alignment, and brush cache-key identity for that render pass. The row loop
traverses the plans as a contiguous read-only span and consumes each value by
reference. Column type selection, attached-property metadata lookup, and binding
formatter lookup therefore happen once per column, not once per row/cell. The plan
is transient prepared state: the DataGrid column remains the source of truth and
property changes are observed on the next render.

The presenter also owns a value cache aligned to the bounded lightweight row
window. A cache entry contains only the projected item reference and one value per
visible render plan; it does not contain a row, cell, binding, or visual. Slot and
item identity preserve entries across overlapping scroll projections. Entering
rows reuse storage released by leaving rows. A render-plan version invalidates the
window when visible column state changes, while the tracked value-change version
invalidates it when a subscribed item reports a display-value change. Detaching
the surface, entering retained fallback, or clearing lightweight rows clears both
item and value references.

### Discontinuous fixed-height scrolling

Large logical jumps in retained modes unload or retarget the displayed container
window. The lightweight path has no window of controls to preserve. It updates the
first/last virtual slots, rebinds a bounded list of value records, invalidates the
surface, and requests pointer-over refresh. Fixed row height makes top and extent
calculation arithmetic; selection remains in the grid model and is read during
drawing rather than copied into row properties.

The retained-row retarget path remains available to compatibility states and is
still protected by its transactional validation, lifecycle, and layout-validity
guards. It is no longer paid by the normal rowless virtual scroll workload.

Separately, the exact built-in retained `Flat` path can rotate an overlapping
fixed-height row window without detaching and reacquiring its controls. It rebinds
only entering rows; a same-slot fractional move keeps the extra realized row as
overscan without row lifecycle work. Drawn cells, discontinuous jumps, and
feature-rich compatibility states continue through the normal guarded fallback.
The component and clean-process results are recorded in the
[flat retained-row report](https://github.com/wieslawsoltes/ProDataGrid/blob/main/tests/ProDataGrid.FlatLayout.Benchmarks/FLAT-ROW-RETARGET-RESULTS-2026-08-13.md).

The focused evidence for the typed retarget-entry buffer is recorded in the
[virtual retarget-buffer report](https://github.com/wieslawsoltes/ProDataGrid/blob/main/tests/ProDataGrid.FlatLayout.Benchmarks/VIRTUAL-RETARGET-RESULTS-2026-08-13.md).
The follow-up evidence for batched lifecycle counters is recorded in the
[virtual row lifecycle batch report](https://github.com/wieslawsoltes/ProDataGrid/blob/main/tests/ProDataGrid.FlatLayout.Benchmarks/VIRTUAL-ROW-BATCH-RESULTS-2026-08-13.md).
The retarget-apply ownership and sparse-state evidence is recorded in the
[virtual row retarget-apply report](https://github.com/wieslawsoltes/ProDataGrid/blob/main/tests/ProDataGrid.FlatLayout.Benchmarks/VIRTUAL-ROW-APPLY-RESULTS-2026-08-13.md).
The rowless follow-up is recorded in the
[lightweight virtual-row report](https://github.com/wieslawsoltes/ProDataGrid/blob/main/tests/ProDataGrid.FlatLayout.Benchmarks/VIRTUAL-LIGHTWEIGHT-ROWS-RESULTS-2026-08-13.md).
The direct scroll projection and overlapping-item reuse follow-up is recorded in the
[virtual layout projection report](https://github.com/wieslawsoltes/ProDataGrid/blob/main/tests/ProDataGrid.FlatLayout.Benchmarks/VIRTUAL-LAYOUT-PROJECTION-RESULTS-2026-08-13.md).
Surface render attribution and the empty-selection fast path are recorded in the
[virtual surface render report](https://github.com/wieslawsoltes/ProDataGrid/blob/main/tests/ProDataGrid.FlatLayout.Benchmarks/VIRTUAL-SURFACE-RENDER-RESULTS-2026-08-13.md).
The follow-up text command ownership and measurement are recorded in the
[virtual surface text-batch report](https://github.com/wieslawsoltes/ProDataGrid/blob/main/tests/ProDataGrid.FlatLayout.Benchmarks/VIRTUAL-SURFACE-TEXT-BATCH-RESULTS-2026-08-13.md).
Smooth-scroll workload coverage and the precomputed cell-clip evidence are recorded
in the
[virtual smooth-scroll report](https://github.com/wieslawsoltes/ProDataGrid/blob/main/tests/ProDataGrid.FlatLayout.Benchmarks/VIRTUAL-SMOOTH-SCROLL-RESULTS-2026-08-13.md).
The prepared per-column renderer/accessor/style evidence is recorded in the
[virtual column render-plan report](https://github.com/wieslawsoltes/ProDataGrid/blob/main/tests/ProDataGrid.FlatLayout.Benchmarks/VIRTUAL-COLUMN-RENDER-PLAN-RESULTS-2026-08-13.md).
The overlapping-row value reuse and formatter-plan evidence is recorded in the
[virtual row-value cache report](https://github.com/wieslawsoltes/ProDataGrid/blob/main/tests/ProDataGrid.FlatLayout.Benchmarks/VIRTUAL-ROW-VALUE-CACHE-RESULTS-2026-08-13.md).

### 4. Surface rendering

`DataGridVirtualCellSurface` is deliberately stateless. Its `Render` method calls
back into the presenter, which owns all geometry and caches. Rendering iterates the
lightweight visible-row records and prepared visible-column render plans.

For each cell the presenter:

1. derives its rectangle from record top/height and the precomputed column layout;
2. reads the plan's precomputed cells-viewport and frozen-region clip;
3. draws selection background when selected;
4. reads the value from the row-aligned cache, resolving it through the plan's
   typed provider/accessor only on a cache miss;
5. draws text, checkbox, date/time text, image, progress, or hierarchy content;
6. draws current-cell chrome; and
7. draws a vertical grid line when enabled.

The presenter snapshots whether any selected cells exist once per surface pass. An
empty selection therefore skips all per-cell selection-dictionary lookups. It also
resolves the current column once per visible row; only that row can draw current-cell
chrome. These are lookup fast paths, not alternate selection or currency models.

A clip scope is created only when the visible rectangle differs from the complete
cell rectangle. The surface skips the active editor cell so the retained overlay is
the only representation while editing.

## Text shaping and cache ownership

Text is the dominant allocation source for many read-only grids. The surface owns a
bounded disposable LRU of text-layout entries. Each entry keeps the `TextLayout`
and, when all runs and brushes support it, immutable glyph-run render data. Its key
includes:

- text and culture;
- font family, size, style, weight, and stretch;
- alignment, trimming, wrapping, and flow direction;
- maximum width and height; and
- brush kind, color, opacity, or reference identity.

The virtual cache holds 4,096 entries, enough for several benchmark viewports and
discontinuous jumps. Supported glyph runs are collected into one custom scene
operation per surface pass; partial-cell clips are replayed per command and cell
chrome is recorded afterward so draw order is unchanged. Unsupported text or brush
features use `TextLayout.Draw` directly.

Eviction disposes the layout and releases the cache's immutable render-data
reference. A queued scene operation owns a separate reference until Avalonia
disposes it, so clearing the cache cannot invalidate render-thread work already in
flight. Detaching the surface clears the cache. The larger bound is
surface-specific; retained drawn-cell cache defaults are not changed.

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
lightweight row geometry and visible column geometry. A hierarchy-expander hit toggles
the node directly; other hits route through the DataGrid's existing selection,
currency, and edit-trigger logic.

Editing does not introduce a second editor model:

1. the grid temporarily requires retained virtual rows and materializes the visible window;
2. `CompleteCellsCollection` materializes normal cells for the editing row;
3. the selected cell becomes a flat presenter child above the surface;
4. the existing column creates its normal retained editor and binding;
5. commit, validation, or cancel uses the existing editing pipeline; and
6. leaving edit mode releases the requirement so a following layout can return to
   zero rows and zero display cells.

Programmatic `GetCellContent` can also request this compatibility materialization.
Applications should not call it repeatedly while profiling the zero-cell display
path.

Creating a `DataGridAutomationPeer` switches that grid instance to retained rows,
so row-level selection, hierarchy, and existing unrealized-row automation contracts
remain provided by the established peers. The surface does not expose one peer per
drawn display cell.

## Invalidation and mode transitions

Changes that only affect drawn values invalidate the surface visual. Geometry or
eligibility changes reset flat ownership, invalidate presenter measure, and may
transition between the surface and retained fallback. Changing
`VisualLayoutMode` after first measure unloads realized elements, refreshes rows and
columns, restores selection, and remeasures headers and rows.

The implementation avoids calling `UpdateLayout` from production paths. Layout is
driven by Avalonia invalidation; explicit `UpdateLayout` is confined to tests and
benchmark phase boundaries.

## Retained-row compatibility path

When a row-level feature requires control semantics, the presenter returns to the
existing recycle/generate or guarded retarget pipeline. This preserves arbitrary
row types, lifecycle handlers, templates, automation peers, editing, validation,
headers, details, and custom factories. The rowless path never attempts to simulate
those contracts.

## Structural and behavioral invariants

A valid surface state must satisfy all of these conditions:

- exactly one `DataGridVirtualCellSurface` is attached;
- retained realized row count is zero in the eligible steady-state path;
- lightweight row-record count remains bounded by the viewport and prefetch policy;
- no display `DataGridCell` exists outside an active compatibility operation;
- editing or automation materializes retained rows before using row semantics;
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
