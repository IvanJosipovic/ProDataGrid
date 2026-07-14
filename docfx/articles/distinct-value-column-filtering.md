# Distinct-Value Column Filtering

`DataGridDistinctValueFilterFlyout` provides an Excel/DataGrip-style filter surface for a column. The popup contains a substring search box and a `ListBox`; each row shows a checkbox, the formatted value, and its source-row count.

Selections update the grid's central `IFilteringModel` immediately:

- No checked values means no descriptor and therefore no filter.
- One or more checked values produce a `FilteringOperator.In` descriptor.
- Unchecking the final selected value removes that column's descriptor.
- Searching changes only the visible options. Selections hidden by the search remain selected.
- Reopening the popup rebuilds counts from the collection view's underlying `SourceCollection`, so values do not disappear merely because the current column is already filtered.

## Configure a typed column definition

The distinct-value popup reads values through `IDataGridColumnValueAccessor`. Typed column definitions create this accessor as part of their binding, so they are the recommended reflection-free setup.

Declare the reusable flyout in XAML:

```xml
<UserControl
    xmlns="https://github.com/avaloniaui"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:filtering="clr-namespace:Avalonia.Controls.DataGridFiltering;assembly=Avalonia.Controls.DataGrid">
  <UserControl.Resources>
    <filtering:DataGridDistinctValueFilterFlyout
        x:Key="StatusDistinctValueFilter"
        Placement="Bottom" />
  </UserControl.Resources>

  <DataGrid ItemsSource="{Binding View}"
            FilteringModel="{Binding FilteringModel}"
            ColumnDefinitionsSource="{Binding ColumnDefinitions}" />
</UserControl>
```

Point the typed definition at that resource key:

```csharp
var statusColumn = new DataGridTextColumnDefinition
{
    ColumnKey = "Status",
    Header = "Status",
    Binding = DataGridBindingDefinition.Create<Order, string>(order => order.Status),
    FilterFlyoutKey = "StatusDistinctValueFilter"
};
```

The definition owns no view object; it only carries a stable resource key. The view resolves the flyout while materializing the column.

## Configure an existing column

If a column was not produced from a typed definition, register a value accessor explicitly in the composition layer:

```csharp
DataGridColumnFilter.SetValueAccessor(
    statusColumn,
    new DataGridColumnValueAccessor<Order, string>(order => order.Status));
```

Then assign the flyout in XAML:

```xml
<DataGridTextColumn Header="Status"
                    ColumnKey="Status"
                    Binding="{Binding Status}"
                    SortMemberPath="Status">
  <DataGridTextColumn.FilterFlyout>
    <filtering:DataGridDistinctValueFilterFlyout />
  </DataGridTextColumn.FilterFlyout>
</DataGridTextColumn>
```

The flyout deliberately does not reflect over `SortMemberPath` or the binding path to read values. If no typed accessor is available, opening is canceled and `LastError` explains the missing requirement.

## Formatting and equality

Use `DisplayFormatter` to customize visible text. Null values display as `(Empty)` by default. Use `ValueComparer` when grouping and membership should use custom equality, such as case-insensitive string equality:

```csharp
var flyout = new DataGridDistinctValueFilterFlyout
{
    ValueComparer = StringComparer.OrdinalIgnoreCase,
    DisplayFormatter = value => value?.ToString() ?? "(No value)"
};
```

When a custom comparer is supplied, the generated descriptor carries a typed predicate so the popup's grouping semantics and the actual row filter remain consistent.

## Custom presentation

The default compiled-binding template is `DataGridFilterDistinctValuesEditorTemplate` in `Themes/Generic.xaml`. It consumes the small `IFilterDistinctValuesContext` and `IFilterDistinctValueOption` contracts. Applications can replace `ContentTemplate` on the flyout while retaining the built-in counting, searching, selection, and filtering behavior.

The standard template uses these theme tokens:

- `DataGridFilterDistinctValuesEditorWidth`
- `DataGridFilterDistinctValuesListMaxHeight`
- `DataGridFilterEditorSpacing`
- `DataGridFilterEditorActionSpacing`
