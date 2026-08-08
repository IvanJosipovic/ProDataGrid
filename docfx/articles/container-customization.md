# Container customization and the element-factory decision

ProDataGrid deliberately does not expose a public row/cell container factory.
The supported extension points cover the concrete customization scenarios without
allowing an external factory to bypass the grid's ownership and recycling invariants.

| Scenario | Supported extension point |
|---|---|
| Change row or cell appearance and visual states | `RowTheme`, `CellTheme`, column `CellTheme`, styles, and control themes |
| Supply arbitrary display or editor content | `DataGridTemplateColumn`, `CellTemplate`, and `CellEditingTemplate` |
| Reuse an expensive templated visual while rows recycle | `DataGridTemplateColumn.ReuseCellContent` or `IRecyclingDataTemplate` |
| Implement a new column and editor contract | Derive from `DataGridColumn` or `DataGridBoundColumn` |
| Attach and clear per-realization integration state | `CellPrepared`, `CellClearing`, `LoadingRow`, and `UnloadingRow` |
| Render a display value without a nested retained display control | supported drawn display mode or `DataGridCustomDrawingColumn` |
| Observe or veto grid state transitions | editing, committed-value, and transactional selection events |

These paths preserve the grid-owned `DataGridRow` and `DataGridCell` containers.
That ownership is significant: the containers carry frozen-region membership,
row/column indexes, selection and current-cell state, validation, automation,
editing overlays, diagnostics, and recycle-pool identity.

## Decision

The Phase 5 review did not find an important user outcome that requires replacing
those containers. A general `TreeDataGridElementFactory`-style API would therefore
duplicate existing customization paths while making incompatible ownership and stale
recycled state public failure modes. No container factory is added.

This is an explicit API decision rather than a permanent prohibition. A future proposal
should identify a concrete outcome that cannot be expressed with the table above and
must demonstrate keyboard navigation, validation, selection, row details, accessibility,
diagnostics, frozen regions, and recycling correctness before introducing a focused
factory contract.

## Compatibility

No existing customization API changes. Existing themes and templates remain the
compatibility default; optimized and drawn display paths are opt-in.
