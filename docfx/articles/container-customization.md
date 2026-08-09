# Container customization

ProDataGrid owns and recycles its row and cell containers. Customize their appearance,
content, behavior, and lifecycle through the supported extension points below.

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

## Container ownership

There is no public row or cell container factory. Grid-owned containers preserve
keyboard navigation, validation, selection, row details, accessibility, diagnostics,
frozen-region behavior, and recycling correctness. Use a template, theme, column,
draw-operation, or lifecycle event from the table above instead of replacing the
container itself.

## Compatibility

No existing customization API changes. Existing themes and templates remain the
compatibility default; optimized and drawn display paths are opt-in.
