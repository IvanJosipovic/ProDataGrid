# Keyboard Shortcuts

## Default Gestures

| Action | Default | Notes |
| --- | --- | --- |
| Tab | `Tab` | Moves to the next/previous editable cell while editing; `Shift+Tab` reverses. When not editing, focus can leave the grid. |
| MoveUp | `Up` | Moves selection up. `Ctrl+Up` jumps to the first row. `Shift` extends selection in `SelectionMode=Extended`. |
| MoveDown | `Down` | Moves selection down. `Ctrl+Down` jumps to the last row. `Shift` extends selection in `SelectionMode=Extended`. |
| MoveLeft | `Left` | Moves to the previous column. `Ctrl+Left` jumps to the first column. Collapses row groups or hierarchical nodes; `Alt+Left` collapses the subtree. |
| MoveRight | `Right` | Moves to the next column. `Ctrl+Right` jumps to the last column. Expands row groups or hierarchical nodes; `Alt+Right` expands the subtree. |
| MovePageUp | `PageUp` | Moves up by a viewport page. `Shift` extends selection in `SelectionMode=Extended`. |
| MovePageDown | `PageDown` | Moves down by a viewport page. `Shift` extends selection in `SelectionMode=Extended`. |
| MoveHome | `Home` | Moves to the first column. `Ctrl+Home` jumps to the first row. |
| MoveEnd | `End` | Moves to the last column. `Ctrl+End` jumps to the last row. |
| Enter | `Enter` | Commits edits and moves according to `EnterKeyNavigationMode`. `Ctrl+Enter` commits without moving. |
| CancelEdit | `Escape` | Cancels cell/row editing. |
| BeginEdit | `F2` | Begins editing the current cell (default also honors `Alt+F2`). |
| SelectAll | `Ctrl/Cmd+A` | Selects all rows/cells when `SelectionMode=Extended`. |
| Copy | `Ctrl/Cmd+C` | Copies selection to the clipboard (requires `ClipboardCopyMode` to be enabled). |
| CopyAlternate | `Ctrl/Cmd+Insert` | Alternate copy gesture. |
| Delete | `Delete` | Removes selected rows when `CanUserDeleteRows` and the data source allows deletion. |
| ExpandAll | `Multiply` | Expands all children under the current hierarchical node or group. |

## Override Defaults

Use `DataGrid.KeyboardGestureOverrides` to remap built-in actions. Any non-null gesture replaces the default mapping for that action; set `Key.None` to disable an action. Built-in handling always respects `e.Handled`, so custom `KeyDown` handlers can opt out of the defaults.

```xml
<DataGrid KeyboardGestureOverrides="{Binding KeyboardGestureOverrides}" />
```

```csharp
KeyboardGestureOverrides = new DataGridKeyboardGestures
{
    MoveDown = new KeyGesture(Key.J),
    MoveUp = new KeyGesture(Key.K),
    ExpandAll = new KeyGesture(Key.E)
};
```

## Continuous Editing

`Tab` and `Shift+Tab` always commit the active cell and open the editor in the next or previous writable cell. When `CanUserAddRows="True"` and the collection view supports `AddNew`, pressing `Tab` in the last writable cell commits the row, creates a new item, and opens its first writable cell.

Use the Enter navigation properties when a data-entry workflow should also keep editing after Enter:

```xml
<DataGrid ItemsSource="{Binding Items}"
          CanUserAddRows="True"
          EnterKeyNavigationMode="NextCell"
          ContinueEditingOnEnter="True" />
```

`EnterKeyNavigationMode="Down"` keeps the current column and moves to the row below. `NextCell` follows the same writable-cell order as Tab and wraps to the next row. `ContinueEditingOnEnter` defaults to `False`, preserving the standard commit-and-navigate behavior.

Built-in compound editors are configured as a single tab stop:

- `DataGridNumericColumn` selects its text when editing begins, and Tab leaves the spinner as one editor instead of visiting its internal buttons.
- `DataGridComboBoxColumn` supports direct text entry with `IsEditable="True"`; Tab commits the current selection or text and advances the grid.
- `DataGridAutoCompleteColumn` is the filtering editor for suggestion lists. Configure `FilterMode`, `MinimumPrefixLength`, and `IsTextCompletionEnabled`, then use Tab to commit the completed text and advance.
- `DataGridDatePickerColumn` focuses its editable text input, so dates can be typed directly without opening the calendar.

If row creation is unavailable for the source, Tab from the last cell leaves the grid normally after the current edit is committed.
