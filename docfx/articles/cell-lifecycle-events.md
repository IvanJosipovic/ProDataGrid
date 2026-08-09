# Cell realization and committed-value events

`DataGrid` exposes three opt-in events for integrations that need to associate
state with virtualized cell containers or observe successful editor commits:

- `CellPrepared` is raised once after each cell container is assigned to a
  realized row item. It is raised again when the same container is recycled for
  another item.
- `CellClearing` is raised once before the row data context, slot, column, and
  automation associations are cleared or reused.
- `CellValueChanged` is raised when a changed value is successfully committed
  through a grid cell editor.

The realization events are dormant when they have no subscribers, so the normal
scrolling path does not allocate event arguments or hierarchy paths.

## Lifecycle sample

```csharp
grid.CellPrepared += (_, e) =>
{
    // e.Cell and e.Row are valid and e.Row.DataContext is still e.RowDataContext.
    telemetry.CellRealized(e.Item, e.Column, e.HierarchyPath);
};

grid.CellClearing += (_, e) =>
{
    // Remove state before the recycled cell receives a different row context.
    telemetry.CellUnrealized(e.Item, e.Column, e.HierarchyPath);
};
```

For flat rows, `Item` and `RowDataContext` are the same object and
`HierarchyPath` is empty. For hierarchical rows, `RowDataContext` is the
`HierarchicalNode`, `Item` is `HierarchicalNode.Item`, and `HierarchyPath`
contains visible nodes from the root item to the current node. A model's hidden
virtual root is not included.

One logical cell container produces one event per assignment. Frozen and
scrolling column regions do not generate duplicate events for the same logical
cell.

## Committed value sample

```csharp
grid.CellValueChanged += (_, e) =>
{
    audit.Write(
        e.Item,
        e.Column,
        e.OldValue,
        e.NewValue,
        e.Origin,
        e.HierarchyPath);
};
```

The initial contract is deliberately limited to changed values committed by a
DataGrid editor. The event is raised after source update and validation succeed,
after the display content has been restored, and immediately before
`CellEditEnded`. Text, numeric, CheckBox, and other built-in columns that use the
normal edit transaction share this contract.

An editable `DataGridTemplateColumn` participates when the column has a stable
source-value accessor (for example, `DataGridColumnMetadata.SetValueAccessor`)
and its editing template updates that source before commit. This lets the grid
compare typed old/new values without inspecting the template or using
reflection.

The following cases do not raise `CellValueChanged`:

- a cancelled edit;
- a commit rejected by validation;
- a successful commit whose source value compares equal to its original value;
- a direct programmatic model-property update;
- formula recalculation;
- undo or redo performed outside a cell edit transaction.

Use model notifications or the formula/undo subsystem events when those broader
change origins are required. This boundary prevents duplicate and inconsistent
notifications from bindings while keeping the commit event deterministic.

The focused contract tests cover successful text, CheckBox, and editable-template
commits; cancelled and no-op edits; validation rejection; direct programmatic
updates; formula recalculation; and model-level undo/redo after a grid commit.
Only the three successful grid edit transactions raise the event.

## Compatibility

These events are additive. Existing `LoadingRow`, `UnloadingRow`, editing,
templates, themes, and retained-cell behavior are unchanged. Applications that
do not subscribe remain on the allocation-free event fast path.

## Sample

The [Hierarchy Feature Contracts sample](https://github.com/wieslawsoltes/ProDataGrid/blob/master/src/DataGridSample/Pages/HierarchyFeatureContractsPage.axaml)
routes all three events through an attached behavior to a ReactiveUI command.
It keeps the view passive, uses retained Avalonia cell templates, and shows the
prepare/clear/commit ordering in a bounded event log.
