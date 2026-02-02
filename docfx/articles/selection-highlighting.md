# Selection Highlighting

Selection visuals in ProDataGrid follow the selection unit and selection mode. Row selection and cell selection are surfaced separately so you can style them independently.

## Selection units and visuals

- `SelectionUnit=FullRow` highlights full rows and uses row headers for selection.
- `SelectionUnit=Cell` highlights individual cells; rows still track selection for `SelectedItems` unless you suppress row visuals.
- `SelectionUnit=CellOrRowHeader` lets row headers select rows and cell clicks select cells.
- `SelectionUnit=CellOrColumnHeader` lets column headers select columns and cell clicks select cells.
- `SelectionUnit=CellOrRowOrColumnHeader` enables both row and column header selection.

Use `SelectionMode=Extended` for range selection (Shift) and multi-select (Ctrl/Cmd). `SelectionMode=Single` keeps a single active selection.

## Pseudo classes

### DataGridRow

- `:selected` - row selection state.
- `:current` - current row (currency).
- `:pointerover` - mouse hover state.

### DataGridCell

- `:selected` - selected state for the cell; maps to cell selection when the selection unit includes cells.
- `:row-selected` - the row of this cell is selected.
- `:cell-selected` - the cell is selected.
- `:current` - current cell.
- `:focus` - current cell with focus.

`DataGridCell:row-selected` lets you show a subtle row highlight even when only some cells are selected, while `DataGridCell:cell-selected` can be a stronger highlight for the selected cells themselves.

### DataGridColumnHeader

- `:selected` - column is fully selected (all visible rows are selected in that column).
- `:current` - current column (current cell’s column).
- `:pointerover` - hover state.

## Theme resource keys

Cell selection brushes can be customized via theme resources (Fluent and Simple define defaults):

- `DataGridCellSelectedBackgroundBrush`
- `DataGridCellSelectedHoveredBackgroundBrush`
- `DataGridCellSelectedUnfocusedBackgroundBrush`
- `DataGridCellSelectedHoveredUnfocusedBackgroundBrush`

Row selection continues to use the existing row brushes such as `DataGridRowSelectedBackgroundBrush`.

Column header selection brushes:

- `DataGridColumnHeaderSelectedBackgroundBrush`
- `DataGridColumnHeaderSelectedHoveredBackgroundBrush`
- `DataGridColumnHeaderSelectedForegroundBrush`
- `DataGridColumnHeaderCurrentBackgroundBrush`
- `DataGridColumnHeaderCurrentForegroundBrush`

## Styling examples

Override the brushes:

```xml
<Styles.Resources>
  <SolidColorBrush x:Key="DataGridCellSelectedBackgroundBrush" Color="#FFBBDEFB" />
  <SolidColorBrush x:Key="DataGridCellSelectedHoveredBackgroundBrush" Color="#FF90CAF9" />
  <SolidColorBrush x:Key="DataGridCellSelectedUnfocusedBackgroundBrush" Color="#FFCFE8FC" />
  <SolidColorBrush x:Key="DataGridCellSelectedHoveredUnfocusedBackgroundBrush" Color="#FFB3E5FC" />
</Styles.Resources>
```

Target pseudo classes directly:

```xml
<Style Selector="DataGridCell:row-selected">
  <Setter Property="Background" Value="#1F64B5F6" />
</Style>

<Style Selector="DataGridCell:cell-selected">
  <Setter Property="Background" Value="#FFBBDEFB" />
  <Setter Property="BorderBrush" Value="#FF1E88E5" />
  <Setter Property="BorderThickness" Value="1" />
</Style>

<Style Selector="DataGridRow:selected /template/ Rectangle#BackgroundRectangle">
  <Setter Property="Fill" Value="#FFE8F1FF" />
</Style>
```

## Usage tips

- For cell highlighting, set `SelectionUnit=Cell` or `SelectionUnit=CellOrRowHeader`.
- For row highlighting without cell selection, use `SelectionUnit=FullRow`.
- For column header selection, use `SelectionUnit=CellOrColumnHeader` (or `CellOrRowOrColumnHeader`) and set `CanUserSelectColumns="True"`.
- Bind `SelectedCells` when working with cell selection so you can inspect selected ranges.

## Samples

See the sample app pages:

- `Selection Units` for side-by-side selection unit comparisons.
- `Selection Highlighting` for custom row and cell visuals.
- `Cell Selection` for `SelectedCells` binding and preset ranges.
- `Cell Selection Only` to suppress row visuals and show cell highlights exclusively.
