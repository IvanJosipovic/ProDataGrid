# Summaries and Aggregation

ProDataGrid supports column summaries for totals and aggregates. Summaries can appear in total rows, group headers/footers, and hierarchical views.

## Total and Group Summaries

Enable total and group summaries on the grid and attach summary descriptions to columns:

```xml
<DataGrid ShowTotalSummary="True"
          ShowGroupSummary="True"
          TotalSummaryPosition="Bottom"
          GroupSummaryPosition="Footer">
  <DataGrid.Columns>
    <DataGridTextColumn Header="Quantity" Binding="{Binding Quantity}">
      <DataGridTextColumn.Summaries>
        <DataGridAggregateSummaryDescription Aggregate="Sum"
                                             Scope="Both"
                                             Title="Sum: "
                                             StringFormat="N0" />
      </DataGridTextColumn.Summaries>
    </DataGridTextColumn>
  </DataGrid.Columns>
</DataGrid>
```

Built-in aggregates include `Sum`, `Average`, `Count`, `CountDistinct`, `Min`, `Max`, `First`, and `Last`.

`GroupSummaryPosition` supports `Header`, `Footer`, or `Both`. `TotalSummaryPosition` can be `Top` or `Bottom`.

## Summary Description Options

Each `DataGridSummaryDescription` supports:

- `Scope`: `Total`, `Group`, or `Both`.
- `Title` and `StringFormat` for labeling/formatting.
- `Converter`/`ConverterParameter` for custom formatting.
- `ContentTemplate` for fully custom visuals.

## Custom Calculators

Create custom summary calculators by implementing `IDataGridSummaryCalculator` and attach them with `DataGridCustomSummaryDescription`:

```csharp
column.Summaries.Add(new DataGridCustomSummaryDescription
{
    Calculator = new StandardDeviationCalculator(),
    Title = "StdDev: ",
    StringFormat = "N2"
});
```

## Summary Row Styling and Timing

- `SummaryRowTheme` lets you theme total and group summary rows.
- `SummaryRecalculationDelayMs` throttles recalculation during rapid updates.
- `SummaryRecalculated` fires after recalculation completes.

## Notes

- Summary values update as the view filters or expands hierarchical nodes.
- Group summaries honor the same descriptors and scopes as total summaries.
