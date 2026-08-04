# Column Banding and Stacked Headers

Column banding lets you define multi-level headers for non-pivot tables. The `ColumnBandModel` builds column definitions with stacked header segments based on a band tree.

## Basic setup

```csharp
using Avalonia.Controls.DataGridBanding;

var model = new ColumnBandModel
{
    HeaderLayout = ColumnBandHeaderLayout.Grouped
};

var salesColumn = new DataGridNumericColumnDefinition
{
    Header = "Sales",
    Binding = DataGridBindingDefinition.Create<Sale, double>(item => item.Sales)
};

model.Bands.Add(new ColumnBand
{
    Header = "Financials",
    Children =
    {
        new ColumnBand { Header = "Sales", ColumnDefinition = salesColumn }
    }
});
```

Bind the generated definitions to the grid:

```xml
<DataGrid ItemsSource="{CompiledBinding Items}"
          ColumnDefinitionsSource="{CompiledBinding Bands.ColumnDefinitions}"
          AutoGenerateColumns="False" />
```

## Header layouts

`HeaderLayout = ColumnBandHeaderLayout.Grouped` renders common ancestors once as non-interactive header cells spanning adjacent leaf columns. Leaves at a shallower depth automatically span the unused rows, producing a conventional grouped-header outline:

```text
|         Order         |      Merchandise      |          Financials          |
| Date | Region | Segment | Category | Product |      Revenue      |  Volume  |
|      |        |         |          |         | Sales | Profit    |  Units   |
```

Use `HeaderLayout = ColumnBandHeaderLayout.Stacked` (the default) when every leaf header should display its complete band path independently. Grouped headers split automatically at frozen-column boundaries so frozen and scrolling regions remain aligned.

## Notes

- `ColumnBand.Header` becomes a stacked segment or grouped spanning cell, depending on `HeaderLayout`.
- Leaf nodes supply the `DataGridColumnDefinition` used by the grid.
- The model applies the `DataGridColumnBandHeaderTemplate` by default; override `HeaderTemplateKey` to customize.

## Sample

Run the sample app and open the "Column Banding" tab for a multi-level header layout.
