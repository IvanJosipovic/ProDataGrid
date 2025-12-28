# Column Types Reference

This reference summarizes the built-in column types shipped with ProDataGrid and the primary properties used to configure them. For layout, sizing, and editing flow, see [Columns and Editing](columns-and-editing.md).

## Bound vs. Template Columns

- Most columns derive from `DataGridBoundColumn` and use `Binding` to map a row item property to a control value.
- Some columns use specialized bindings (for example, combo box selected item/value bindings).
- `DataGridTemplateColumn` gives you full control over display and editing with templates.

## Built-in Column Catalog

| Column | Control | Notes |
| --- | --- | --- |
| `DataGridTextColumn` | `TextBox` / `TextBlock` | `Binding` to text; text styling and watermark support. |
| `DataGridCheckBoxColumn` | `CheckBox` | `Binding` to `bool?`; `IsThreeState`. |
| `DataGridComboBoxColumn` | `ComboBox` | `ItemsSource` plus `SelectedItemBinding`/`SelectedValueBinding`/`TextBinding`. |
| `DataGridAutoCompleteColumn` | `AutoCompleteBox` | `ItemsSource`, `FilterMode`, `MinimumPrefixLength`, `Binding` to text. |
| `DataGridMaskedTextColumn` | `MaskedTextBox` | `Mask`, `PromptChar`, and other mask options. |
| `DataGridDatePickerColumn` | `CalendarDatePicker` | `Binding` to `SelectedDate`, date range and format options. |
| `DataGridTimePickerColumn` | `TimePicker` | `Binding` to `SelectedTime`. |
| `DataGridNumericColumn` | `NumericUpDown` | `Binding` to value; `Minimum`/`Maximum`/`Increment` and format options. |
| `DataGridSliderColumn` | `Slider` | `Binding` to value; `Minimum`/`Maximum`/`SmallChange`/`LargeChange`. |
| `DataGridProgressBarColumn` | `ProgressBar` | `Binding` to value; `Minimum`/`Maximum`, `ShowProgressText`. |
| `DataGridToggleSwitchColumn` | `ToggleSwitch` | `Binding` to `IsChecked`; on/off content and templates. |
| `DataGridToggleButtonColumn` | `ToggleButton` | `Binding` to `IsChecked`; checked/unchecked content, `IsThreeState`. |
| `DataGridImageColumn` | `Image` | `Binding` to `Source`. |
| `DataGridHyperlinkColumn` | `HyperlinkButton` | `Binding` to `NavigateUri`; optional `ContentBinding`. |
| `DataGridButtonColumn` | `Button` | `Content`, `Command`, and `CommandParameter` (row item by default). |
| `DataGridTemplateColumn` | `DataTemplate` | `CellTemplate`, `CellEditingTemplate`, and `NewRowCellTemplate`. |
| `DataGridHierarchicalColumn` | `DataGridHierarchicalPresenter` | Tree column with `Indent`; used with hierarchical models. |

## Tips

- Prefer built-in columns over templates when possible; they wire editing, validation, and theme resources for you.
- Use `ClipboardContentBinding` on a column when you need different export text than the displayed value.
- For hierarchical data, combine `DataGridHierarchicalColumn` with [Hierarchical Data](hierarchical-data.md).
