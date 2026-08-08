# Transactional selection veto

`DataGrid.SelectionChanging` exposes one complete selection proposal before the
grid changes selected rows, cells or columns, the current cell, selection
anchor, collection-view currency, focus, or scroll position. Set `Cancel` to
`true` to reject the entire proposal.

```csharp
grid.SelectionChanging += (_, e) =>
{
    if (e.AddedItems.Any(item => !authorization.CanSelect(item)))
    {
        e.Cancel = true;
    }
};
```

The existing `SelectionChanged`, `SelectedCellsChanged`, and
`SelectedColumnsChanged` events are raised only after an accepted proposal has
committed. They are not raised for a vetoed proposal.

## Proposal data

The event arguments contain:

- `AddedItems` and `RemovedItems`, projected to the underlying data items;
- `AddedRows` and `RemovedRows`, including row indexes and hierarchy context;
- `AddedCells` and `RemovedCells`;
- `AddedColumns` and `RemovedColumns` when a whole column becomes selected or
  unselected;
- `ProposedCurrentItem`, `ProposedCurrentCell`, and `ProposedAnchor`;
- `HierarchyNode` and the root-to-node `HierarchyPath` for a hierarchical
  current item;
- `Source`, `IsUserInitiated`, and the triggering routed event when one exists.

`Source` is a flags value. Pointer range drags combine `Pointer` and
`DragInteraction`. Keyboard navigation, commands such as select-all,
programmatic properties and collections, items-source currency changes, and
external `ISelectionModel` synchronization report their corresponding origin.

For a materialized descendant hidden below a collapsed ancestor, the proposal
contains an added row with `RowIndex == -1` and a complete hierarchy path. The
event is raised before automatic ancestor expansion, so cancellation leaves the
tree collapsed. The preview does not load or materialize children.

## Atomic behavior

All supported entry points use the same preview/commit boundary: pointer and
keyboard selection, row and column headers, cell/range drag, select-all,
`SelectedItem`, `SelectedIndex`, `SelectedItems`, `SelectedCells`,
`SelectedColumns`, selection-state restore, source synchronization, and
external selection-model changes.

If an external bound selection collection or `ISelectionModel` has already
published its proposed mutation, a veto synchronously restores it to the last
committed grid selection. Observers therefore do not see a partially committed
grid/model pair after the call returns.

Changing selection again from inside a `SelectionChanging` handler is not
supported and throws `InvalidOperationException`. Schedule a follow-up change
on the UI dispatcher after the handler returns when a replacement selection is
required.

## Compatibility and performance

`SelectionChanging` is additive. Existing themes, templates, selection events,
and retained/drawn cell behavior are unchanged. With no subscribers, proposal
collections, hierarchy paths, and event arguments are not created; the normal
selection and scrolling hot paths retain their previous allocation behavior.
