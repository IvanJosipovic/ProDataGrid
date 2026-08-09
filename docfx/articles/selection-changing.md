# Transactional selection veto

`DataGrid.SelectionChanging` exposes one complete selection proposal. For
DataGrid-controlled operations, the event is raised before the grid changes
selected rows, cells or columns, the current cell, selection anchor,
collection-view currency, focus, or scroll position. Set `Cancel` to `true` to
reject the entire proposal.

`DataGridSelectionChangingEventArgs.Guarantee` states which boundary applies.
`AtomicPreflight` means none of the proposed DataGrid state has committed.
`PostChangeReconciliation` means a caller-owned source, collection, or selection
model published its change before the DataGrid could observe it; the grid is
reconciling that external proposal with its last committed state.

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
- `Source`, `Guarantee`, `IsUserInitiated`, and the triggering routed event when
  one exists.

`Source` is a flags value. Pointer range drags combine `Pointer` and
`DragInteraction`. Keyboard navigation, commands such as select-all,
programmatic properties and collections, items-source currency changes, and
external `ISelectionModel` synchronization report their corresponding origin.

For a materialized descendant hidden below a collapsed ancestor, the proposal
contains an added row with `RowIndex == -1` and a complete hierarchy path. The
event is raised before automatic ancestor expansion, so cancellation leaves the
tree collapsed. The preview does not load or materialize children.

## Guarantee by origin

`Source` identifies why selection is changing. `Guarantee` identifies whether
the DataGrid can preview the operation before its producer changes state. The
same `Source` flag can therefore appear with different guarantees.

| Origin or entry point | Guarantee | Boundary |
| --- | --- | --- |
| Pointer, keyboard, commands, row/column headers, range drag, and select-all | `AtomicPreflight` | The proposal precedes selection, current cell, anchor, currency, focus, capture/auto-scroll, and scrolling changes. |
| `SelectedItem`, `SelectedIndex`, `CurrentCell`, DataGrid-owned selected collections, row `IsSelected`, and selection-state restore | `AtomicPreflight` | The DataGrid computes and previews the complete proposal before committing it. |
| `ItemsSource` replacement and all externally initiated item-source/view notifications, including those entering through the built-in `DataGridCollectionView` boundary | `PostChangeReconciliation` | The upstream source or property change is already observable. The built-in view hook still produces one coherent proposal before its own selection/currency reconciliation. |
| Incremental changes made directly to a caller-owned bound `SelectedItems`, `SelectedCells`, or `SelectedColumns` collection | `PostChangeReconciliation` | The external collection has already notified its observers; the DataGrid still exposes its last committed selection while it evaluates the proposal. |
| Changes made directly to a caller-owned `ISelectionModel` | `PostChangeReconciliation` | The model has already published its change; the DataGrid evaluates it before committing matching grid state. |
| Custom collection views, hierarchy sources, or other external producers without a supported preflight hook | `PostChangeReconciliation` | The producer mutation is already observable, so the DataGrid can only reconcile it with committed grid state. |

For `AtomicPreflight`, cancellation commits none of the proposed DataGrid state
and no post-selection event is raised. An accepted proposal commits first; the
existing `SelectionChanged`, `SelectedCellsChanged`, and
`SelectedColumnsChanged` events follow the final state.

## External-producer boundary

A DataGrid cannot run code before an arbitrary caller-owned collection,
`ISelectionModel`, or custom view notifies its observers. It also cannot erase a
notification those observers already received. Such proposals therefore report
`PostChangeReconciliation` rather than claiming atomic preflight.

During reconciliation, DataGrid selection, current cell, anchor, focus, and
scrolling remain at their last committed values until the grid computes the
coherent final state allowed by the changed producer. A veto synchronously
restores a mutable external *selection* producer to that committed selection
when supported. External observers can consequently see both the producer's
original notification and the compensating notification. This is convergence
after an external change, not rollback of external observation history.

The guarantee concerns selection state. Vetoing selection after an item-source
remove, reset, replacement, filter, or sort does not undo that data/view
operation. Surviving selected items are reconciled by identity and their
row/cell coordinates can change with the new view. A selected identity that is
no longer present cannot be retained merely because the selection proposal was
vetoed.

Changing selection again from inside a `SelectionChanging` handler is not
supported and throws `InvalidOperationException`. Schedule a follow-up change
on the UI dispatcher after the handler returns when a replacement selection is
required.

## Compatibility and performance

`SelectionChanging` is additive. Existing themes, templates, selection events,
and retained/drawn cell behavior are unchanged. With no subscribers, proposal
collections, hierarchy paths, and event arguments are not created; the normal
selection and scrolling hot paths retain their previous allocation behavior.

## MVVM sample

The [Hierarchy Feature Contracts sample](https://github.com/wieslawsoltes/ProDataGrid/blob/master/src/DataGridSample/Pages/HierarchyFeatureContractsPage.axaml)
uses an attached behavior to map `SelectionChanging` to a ReactiveUI command.
The command vetoes restricted items synchronously without putting an event
handler in the view's code-behind, and the same page demonstrates how accepted
and rejected proposals appear alongside lifecycle and filtering events.
