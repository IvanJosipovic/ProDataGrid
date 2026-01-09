# Cell Template Reuse

Large DataGridTemplateColumn templates can become expensive when rows are recycled and
cells are rebuilt on every scroll. ProDataGrid now lets you opt into reusing existing
cell content when the template itself does not support `IRecyclingDataTemplate`.

## When to use
Enable reuse when your cell template:
- Is driven by `DataContext` bindings.
- Does not rely on one-time initialization per data item.
- Can safely handle `DataContext` changes without rebuilding the visual tree.

This is a good fit for property grids and diagnostic tools where the editor control is
heavy and rows are frequently recycled.

## How it works
`DataGridTemplateColumn` has a new `ReuseCellContent` flag (default `false`). When enabled,
existing cell content is kept for recycled rows and the template is not rebuilt unless
it already supports recycling via `IRecyclingDataTemplate`.

Placeholder rows (`NewItemPlaceholder`) and forced refresh after editing still rebuild
content to preserve correctness.

## Example
```xaml
<DataGridTemplateColumn Header="Value" ReuseCellContent="True">
  <DataGridTemplateColumn.CellTemplate>
    <DataTemplate>
      <local:PropertyValueEditorView/>
    </DataTemplate>
  </DataGridTemplateColumn.CellTemplate>
</DataGridTemplateColumn>
```

## Caveats
- If your template uses explicit `Binding Source=...` instead of `DataContext`, reuse
  may leave stale bindings. Prefer `DataContext`-driven bindings or implement a recycling
  template.
- Reuse is opt-in to avoid changing behavior in existing apps.

## ProDiagnostics
The ProDiagnostics property grid keeps editors visible by using
`PropertyValueEditorView` in the value column and enables `ReuseCellContent` so the
editor view is reused as rows recycle. Text validation runs only while an editor is
focused to avoid expensive parsing during scroll.
