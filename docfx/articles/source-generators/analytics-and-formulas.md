# Analytics and formulas

Analytics attributes reuse the canonical grid fields for pivot tables, outline reports, charts, formula dependencies, and spreadsheet projections. Generated selectors call typed accessors directly and keep stable ordering independent of CLR property order.

## Declare analytics roles

```csharp
[GenerateDataGridColumns(
    ProviderName = "SalesSchema",
    Discovery = DataGridColumnDiscovery.AttributedOnly,
    Strict = true,
    PivotConfigureMethod = nameof(ConfigurePivot),
    OutlineConfigureMethod = nameof(ConfigureOutline))]
public sealed class Sale
{
    [DataGridColumn(Header = "Period", ColumnKey = "period")]
    [DataGridPivotAxis(
        DataGridGeneratedAnalyticsRole.PivotColumn,
        Order = 0)]
    [DataGridChartField(
        DataGridGeneratedAnalyticsRole.ChartCategory,
        Order = 0)]
    public string Period { get; init; } = string.Empty;

    [DataGridColumn(Header = "Region", ColumnKey = "region")]
    [DataGridPivotAxis(
        DataGridGeneratedAnalyticsRole.PivotRow,
        Order = 0,
        ConfigureMethod = nameof(ConfigurePivotAxis))]
    [DataGridOutlineField(
        DataGridGeneratedAnalyticsRole.OutlineGroup,
        Order = 0,
        ConfigureMethod = nameof(ConfigureOutlineGroup))]
    public string Region { get; init; } = string.Empty;

    [DataGridColumn(
        DataGridColumnKind.Numeric,
        Header = "Revenue",
        ColumnKey = "revenue")]
    [DataGridPivotValue(
        PivotAggregateType.Sum,
        Order = 0,
        Format = "C0")]
    [DataGridChartField(
        DataGridGeneratedAnalyticsRole.ChartValue,
        Order = 0,
        Series = "Revenue",
        Format = "C0",
        Aggregate = DataGridAggregateType.Sum)]
    [DataGridOutlineField(
        DataGridGeneratedAnalyticsRole.OutlineDetail,
        Order = 0,
        Name = "Revenue",
        Format = "C0",
        Aggregate = DataGridAggregateType.Sum)]
    public double Revenue { get; init; }

    public static void ConfigurePivotAxis(PivotAxisField field) =>
        field.ShowSubtotals = false;

    public static void ConfigureOutlineGroup(OutlineGroupField field) =>
        field.ShowSubtotals = true;

    public static void ConfigurePivot(PivotTableModel model) =>
        model.Layout.RowLayout = PivotRowLayout.Tabular;

    public static void ConfigureOutline(OutlineReportModel model) =>
        model.Layout.ShowGrandTotal = true;
}
```

Roles are ordered globally by `Order`, then stable column key. Generated fields leave runtime property paths unset.

## Pivot factories

The provider exposes:

- `AnalyticsFields`;
- `CreatePivotAxisFields`;
- `CreatePivotValueFields`;
- `CreatePivotTableModel`.

```csharp
PivotTableModel pivot = SalesSchema.CreatePivotTableModel(
    items,
    static model =>
    {
        model.Layout.RowLayout = PivotRowLayout.Tabular;
        model.Layout.ValuesPosition = PivotValuesPosition.Columns;
        model.Layout.ShowRowSubtotals = false;
    });
```

The factory suspends auto-refresh while installing rows, columns, filters, values, generated policy, and the optional caller callback. Refresh is enabled only after configuration succeeds.

Per-field configure methods must accept the exact `PivotAxisField` or `PivotValueField` type. `PivotConfigureMethod` accepts `PivotTableModel`.

## Calculated and custom pivot values

```csharp
[DataGridColumn(
    DataGridColumnKind.Numeric,
    Header = "Margin",
    ColumnKey = "margin")]
[DataGridPivotValue(
    PivotAggregateType.None,
    Order = 1,
    Format = "P1",
    Formula = "[revenue] * 0.2",
    Dependencies = ["revenue"])]
public double Margin { get; init; }
```

Dependencies use stable generated column keys. Missing or duplicate formula metadata reports `PDGSG121`.

For `PivotAggregateType.Custom`, set `CustomAggregatorFactoryMethod` to an accessible static parameterless factory returning `IPivotAggregator`. Formula/custom-aggregate conflicts report `PDGSG009`.

## Outline reports

The provider emits `CreateOutlineGroupFields`, `CreateOutlineValueFields`, and `CreateOutlineReportModel`:

```csharp
OutlineReportModel outline = SalesSchema.CreateOutlineReportModel(
    items,
    static report =>
    {
        report.Layout.ShowSubtotals = true;
        report.Layout.ShowGrandTotal = true;
        report.Layout.ShowDetailRows = true;
        report.Layout.DetailLabelSelector =
            static item => ((Sale)item).Period;
    });
```

Group roles create `OutlineGroupField`; detail roles create aggregated `OutlineValueField`. Custom outline aggregation uses the same `IPivotAggregator` factory boundary.

## Chart models

Projects referencing `ProDataGrid.Charting` can create direct chart projections:

```csharp
DataGridChartModel chart = DataGridGeneratedChartAdapter.CreateModel(
    items,
    SalesSchema.AnalyticsFields);
```

Generated numeric fields provide a cached `Func<object, double?>`, avoiding boxed value extraction and runtime numeric conversion in the chart hot path. User-defined/non-numeric fields retain the compatible conversion path.

## Spreadsheet range charts

Project only generated value columns inside a bounded inclusive grid range:

```csharp
DataGridGeneratedChartRangeProjection rangeChart =
    DataGridGeneratedChartAdapter.CreateRangeProjection(
        items,
        SalesSchema.AnalyticsFields,
        columnDefinitions,
        new DataGridCellRange(20, 99, 1, 4),
        maximumRows: 4096);

rangeChart.UpdateRange(new DataGridCellRange(100, 179, 1, 4));
```

The adapter includes selected stable-key chart-value columns, chooses a selected/first category, preserves exact indexes by disabling downsampling, and rejects ranges above the bound.

## Stable-key chart/grid selection

```csharp
DataGridGeneratedSelectionController<Sale, int> selection =
    SalesSchema.CreateSelectionController();

DataGridGeneratedListChartKeyMap<Sale, int> keyMap = new(
    items,
    SalesSchema.Instance);

DataGridGeneratedChartSelectionSynchronizer<Sale, int> sync = new(
    keyMap,
    selection,
    rangeChart.Model.Interaction,
    categoryToSourceIndex:
        index => rangeChart.Range.StartRow + index,
    sourceToCategoryIndex:
        index => index >= rangeChart.Range.StartRow &&
                 index <= rangeChart.Range.EndRow
            ? index - rangeChart.Range.StartRow
            : -1);
```

The key map applies observable list changes to the generated item index. Malformed/coalesced notifications fall back to an atomic reset. Origin tagging prevents chart/grid feedback loops.

`IDataGridGeneratedChartKeyMap<TKey>` is the customization boundary for grouped, remote, or downsampled sources.

## Long-form series

```csharp
using DataGridGeneratedLongFormChartDataSource longForm =
    DataGridGeneratedChartAdapter.CreateLongFormSource(
        items,
        SalesSchema.AnalyticsFields,
        maximumItems: 65_536,
        maximumSeries: 256);

ChartModel model = new() { DataSource = longForm };
```

The source walks input once, uses generated numeric selectors, preserves first category/series order, aggregates duplicate pairs, observes collection/item changes, and bounds input plus emitted series.

## Formula columns

```csharp
[DataGridColumn(
    DataGridColumnKind.Formula,
    Header = "Total",
    ColumnKey = "total",
    FormulaName = "total",
    Formula = "=SUM([@Amount], [@Tax])",
    IsReadOnly = true)]
[DataGridFormulaField(
    "total",
    Dependencies = ["amount", "tax"],
    Order = 0,
    Format = "C2")]
public decimal Total { get; set; }
```

Literal formulas are validated during compilation. `PDGSG138` reports the source property, formula position, and parser message. The analyzer uses the production Excel tokenizer/parser support for functions, arrays, operators, A1/R1C1, sheets, external references, and structured references.

Dynamic formula text remains a runtime responsibility. Applications and editors can call `ExcelFormulaSyntaxValidator.TryValidate` directly.

## Bind a formula model

```csharp
[GenerateDataGridView(
    typeof(SpreadsheetRow),
    Framework = DataGridViewFramework.ReactiveUI,
    Recipe = DataGridViewRecipe.Spreadsheet,
    FormulaModelPropertyName = nameof(FormulaModel))]
public sealed partial class SpreadsheetViewModel : ReactiveObject
{
    public IDataGridFormulaModel FormulaModel { get; } =
        new DataGridFormulaModel();
}
```

The member must implement `IDataGridFormulaModel`; otherwise the generator reports `PDGSG130`.

## Schema compatibility

Analytics roles, calculated dependencies, factories, and configure hooks participate in `SchemaHash`. Persisted layouts/state can therefore reject or migrate when analytical meaning changes even if the visible CLR property list did not.

## Related articles

- [Pivot tables](../pivot-tables.md)
- [Pivot calculated measures](../pivot-calculated-measures.md)
- [Pivot charts](../pivot-charts.md)
- [Formula engine integration](../formula-engine-integration.md)
- [ProCharts integration](../procharts-datagrid-integration.md)
