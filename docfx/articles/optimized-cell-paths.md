# Optimized retained and drawn cell paths

ProDataGrid keeps the normal generic themes and retained Avalonia cell templates as
the compatibility default. Performance-sensitive grids can opt into smaller partial
themes, direct typed value access, or drawn display cells independently. Editing always
returns to the normal retained editor path.

## Choose the smallest change that fits

| Path | Cell representation | Best fit | Main trade-off |
| --- | --- | --- | --- |
| Standard retained | `DataGridCell` plus normal Avalonia content/template | Existing applications, arbitrary templates, converters, validation, and dynamic resources | Highest visual-tree and binding cost |
| Optimized retained theme | `DataGridCell` plus normal retained content | Applications that require Avalonia controls and layout but want fewer chrome and presenter controls | Explicit theme opt-in; choose lean or feature-preserving row/header variants |
| Retained direct text | A retained cell and `TextBlock`, with a compatible typed accessor | Large read-mostly text grids that still require retained controls | Incompatible binding features automatically fall back to the normal binding path |
| Direct retained text cell | `DataGridDirectTextCell` with a compatible typed accessor | Dense read-mostly flat grids that require retained input, automation, and editing | Uses the optimized direct-cell theme; incompatible accessors fall back to a value binding |
| Direct hierarchy presenter | One retained hierarchy cell containing the expander and optional `TextBlock` | Large tree grids that need normal controls, input, automation, and editing | Custom cell templates continue to use the standard hierarchy presenter |
| Drawn display | A `DataGridCustomDrawingCell` or a supported built-in column in `Drawn` mode | Very dense read-mostly grids where the smallest display tree matters most | Display is drawn; editing still uses retained controls |

Start with the optimized retained theme. It improves proper Avalonia-layout cells and
does not require custom drawing.

## Load the partial optimized resources

Load a normal ProDataGrid theme first, then the optimized resource dictionary:

```xml
<Application.Styles>
  <FluentTheme />
  <StyleInclude Source="avares://Avalonia.Controls.DataGrid/Themes/Fluent.v2.xaml" />
  <StyleInclude Source="avares://Avalonia.Controls.DataGrid/Themes/Optimized.xaml" />
</Application.Styles>
```

`Themes/Optimized.xaml` is additive. It does not replace or modify
`Themes/Generic.xaml`, so existing applications remain on their current templates until
they assign an optimized theme or enable an optimized column property.

## Optimize ordinary retained Avalonia cells

For a feature-complete grid with no frozen columns, use the feature-preserving row and
header variants:

```xml
<DataGrid RowTheme="{StaticResource DataGridOptimizedFeatureUnfrozenRowTheme}"
          CellTheme="{StaticResource DataGridOptimizedCellTheme}"
          ColumnHeaderTheme="{StaticResource DataGridOptimizedFeatureColumnHeaderTheme}"
          UseLightweightFiller="True" />
```

Use `DataGridOptimizedFeatureRowTheme` when left or right frozen columns are enabled.
These variants retain row headers, row details, grid lines, current/focus/selection
chrome, sort and filter indicators, resize handles, and column dragging.

For fixed-height, read-mostly surfaces that do not need those row/header features, use
`DataGridOptimizedUnfrozenRowTheme` (or `DataGridOptimizedRowTheme` with frozen
columns) and `DataGridOptimizedColumnHeaderTheme`. The lean variants intentionally omit
row-header/details presenters and the generic header's feature controls.

Arbitrary `DataGridTemplateColumn` content remains an ordinary retained Avalonia
control tree. The optimized cell theme only removes redundant cell chrome and hosts the
content directly.

## Avoid a display binding without giving up retained controls

`DataGridTextColumn.UseDirectTextContent="True"` keeps the ordinary retained
`DataGridCell` and text element. When the column has compatible typed accessor metadata,
the text element reads that accessor without creating a binding expression per realized
cell:

```xml
<DataGridTextColumn Header="Name"
                    Binding="{Binding Name}"
                    UseDirectTextContent="True"
                    TrackDirectTextValueChanges="True" />
```

The equivalent column-definition properties are
`DataGridTextColumnDefinition.UseDirectTextContent` and
`TrackDirectTextValueChanges`. Set change tracking to `False` only when the displayed
value is immutable; recycled cells still refresh for a new row item.

Direct access is decided for each realized item. A converter, explicit binding source,
fallback/target-null behavior, incompatible runtime item type, or missing typed accessor
uses the normal Avalonia binding path. This fallback preserves binding semantics for
heterogeneous and templated grids.

Set `UseDirectTextCell="True"` when the retained display can use
`DataGridDirectTextCell` instead of a `DataGridCell` containing a text element. The
column still creates the normal retained editor when editing begins. Column definitions
use `DataGridTextColumnDefinition.UseDirectTextCell`.

## Optimize retained hierarchy cells

`DataGridHierarchicalColumn` offers two retained-control optimizations:

```xml
<DataGridHierarchicalColumn Header="Name"
                            Binding="{Binding Item.Name}"
                            UseOptimizedPresenter="True"
                            UseDirectTextContent="True"
                            TrackDirectTextValueChanges="True" />
```

`UseOptimizedPresenter` combines the cell and expander-presenter roles while retaining a
normal Avalonia text control, hierarchy input, UI Automation, focus, and editing
behavior. `UseDirectTextContent` additionally uses a compatible typed accessor. Set
`UseDirectCell="True"` for the leanest retained hierarchy container. Custom hierarchy
cell templates automatically stay on the standard presenter path.

## Opt into drawn display cells

Supported built-in columns accept `DisplayMode="Drawn"`:

```xml
<DataGridTextColumn Header="Name"
                    Binding="{Binding Name}"
                    DisplayMode="Drawn" />
<DataGridNumericColumn Header="Total"
                       Binding="{Binding Total}"
                       DisplayMode="Drawn" />
```

Unsupported configurations fall back to `Retained`. Use
`DataGridCustomDrawingColumn` for a custom drawing operation or renderer. Drawn cells
preserve selection/current/focus chrome and automation names, and switch to the normal
retained editor when editing starts.

## Sample gallery workloads

The sample gallery contains two dedicated workload pages:

- **Optimized Cell Paths (Flat)** switches one grid between standard retained,
  optimized retained theme, retained direct accessor, direct retained text cell,
  built-in drawn text, and custom Skia draw-operation paths. It starts with a 1,000-row
  preview and can generate up to 1,000,000 immutable rows; the default profiling target
  is 250,000 rows across seven columns.
- **Optimized Cell Paths (Hierarchy)** switches one tree grid between standard retained,
  optimized retained theme, optimized hierarchy presenter, direct retained hierarchy,
  built-in drawn companion cells, and custom Skia companion cells. Its default target is
  149,792 nodes with separate load, expand-all, collapse-all, and equal-distance jump
  actions.

Each page displays the active container type and exact configuration, uses typed column
definitions, and reports a whole-process managed-heap snapshot. Run one path per fresh
process with the same window and dataset settings when comparing memory or frame
behavior. The pages are exploratory profiling surfaces; use the benchmark harness for
controlled elapsed-time and allocation results.

## Validate the chosen path

Benchmark first layout, an equal-distance scroll/jump workload, and the application's
actual templates separately. Record both elapsed time and allocated bytes. A drawn-cell
win does not prove that a retained template improved, which is why ProDataGrid's
benchmark matrix keeps standard retained, optimized retained, and drawn modes as
separate lanes.

Keep the generic path when runtime resource replacement, an application-specific control
template, or a binding feature is more important than the extra realized controls. The
optimized paths are explicit options, not global behavior changes.

## Related articles

- [Column Definitions: Hot Path Integration](column-definitions-hot-path.md)
- [Custom Drawing Columns and Skia](custom-drawing-columns.md)
- [Hierarchical Model: End-to-End](hierarchical-model-end-to-end.md)
- [Scrolling and Virtualization](scrolling-virtualization.md)
