# Conditional Formatting Model: End-to-End Usage

This guide shows complete `ConditionalFormattingModel` usage: themes, rule descriptors, runtime updates, and row/cell targeting.

## What this gives you

- Declarative formatting rules independent of templates and code-behind events.
- Consistent styling behavior for both per-cell and per-row conditions.
- Runtime rule updates (`SetOrUpdate`, `Remove`, `Apply`, `Clear`) without rebuilding columns.
- Priority and short-circuit control when multiple rules match.

## End-to-end flow

1. Define `ControlTheme` resources for each visual state.
2. Build `ConditionalFormattingDescriptor` rules in a model.
3. Bind `ConditionalFormattingModel` to the grid.
4. Adapter evaluates descriptors against visible rows/cells and applies themes.

## 1. Theme resources

```xml
<ControlTheme x:Key="ScoreHighCellTheme"
              TargetType="DataGridCell"
              BasedOn="{StaticResource {x:Type DataGridCell}}">
  <Setter Property="Background" Value="#1C2ECC71" />
  <Setter Property="Foreground" Value="#145A32" />
</ControlTheme>

<ControlTheme x:Key="ScoreLowCellTheme"
              TargetType="DataGridCell"
              BasedOn="{StaticResource {x:Type DataGridCell}}">
  <Setter Property="Background" Value="#1CE74C3C" />
  <Setter Property="Foreground" Value="#C0392B" />
</ControlTheme>

<ControlTheme x:Key="NegativeDeltaTheme"
              TargetType="DataGridCell"
              BasedOn="{StaticResource {x:Type DataGridCell}}">
  <Setter Property="Background" Value="#1CF39C12" />
  <Setter Property="Foreground" Value="#7E5109" />
</ControlTheme>

<ControlTheme x:Key="RowAlertTheme"
              TargetType="DataGridRow"
              BasedOn="{StaticResource {x:Type DataGridRow}}">
  <Style Selector="^ /template/ Rectangle#BackgroundRectangle">
    <Setter Property="Fill" Value="#1CD32F2F" />
    <Setter Property="Opacity" Value="1" />
  </Style>
</ControlTheme>
```

## 2. ViewModel rule wiring

```csharp
using Avalonia.Controls.DataGridConditionalFormatting;

public sealed class MetricsViewModel
{
    public IConditionalFormattingModel ConditionalFormatting { get; } = new ConditionalFormattingModel();

    public MetricsViewModel()
    {
        ConditionalFormatting.Apply(new[]
        {
            new ConditionalFormattingDescriptor(
                ruleId: "score-high",
                @operator: ConditionalFormattingOperator.GreaterThanOrEqual,
                columnId: "score",
                propertyPath: nameof(MetricRow.Score),
                value: 90d,
                themeKey: "ScoreHighCellTheme",
                target: ConditionalFormattingTarget.Cell,
                priority: 0),

            new ConditionalFormattingDescriptor(
                ruleId: "score-low",
                @operator: ConditionalFormattingOperator.LessThan,
                columnId: "score",
                propertyPath: nameof(MetricRow.Score),
                value: 60d,
                themeKey: "ScoreLowCellTheme",
                target: ConditionalFormattingTarget.Cell,
                priority: 1),

            new ConditionalFormattingDescriptor(
                ruleId: "row-overdue",
                @operator: ConditionalFormattingOperator.Equals,
                propertyPath: nameof(MetricRow.Status),
                value: "Overdue",
                target: ConditionalFormattingTarget.Row,
                valueSource: ConditionalFormattingValueSource.Item,
                themeKey: "RowAlertTheme",
                priority: 0)
        });
    }

    public void DisableLowScoreRule() => ConditionalFormatting.Remove("score-low");
}
```

## 3. XAML grid wiring

```xml
<DataGrid ItemsSource="{Binding Rows}"
          ConditionalFormattingModel="{Binding ConditionalFormatting}"
          AutoGenerateColumns="False">
  <DataGrid.Columns>
    <DataGridTextColumn Header="Region" ColumnKey="region" Binding="{Binding Region}" />
    <DataGridNumericColumn Header="Score" ColumnKey="score" Binding="{Binding Score}" />
    <DataGridNumericColumn Header="Change" ColumnKey="change" Binding="{Binding Change}" />
    <DataGridTextColumn Header="Status" ColumnKey="status" Binding="{Binding Status}" />
  </DataGrid.Columns>
</DataGrid>
```

## 4. Runtime updates and batching

```csharp
using (ConditionalFormatting.DeferRefresh())
{
    ConditionalFormatting.SetOrUpdate(new ConditionalFormattingDescriptor(
        ruleId: "change-negative",
        @operator: ConditionalFormattingOperator.LessThan,
        columnId: "change",
        propertyPath: nameof(MetricRow.Change),
        value: 0d,
        themeKey: "NegativeDeltaTheme",
        priority: 2));

    ConditionalFormatting.Remove("score-low");
}
```

Use `FormattingChanging` to validate rule sets before commit.

## 5. Rule order and stop behavior

- `Priority` controls evaluation order (lower is earlier).
- `StopIfTrue=true` short-circuits additional rules once matched.
- `Target` determines whether a row or a cell receives the theme.
- For row rules, `ValueSource` is forced to `Item`.

## 6. Custom predicates

For complex logic, use `ConditionalFormattingOperator.Custom` and `predicate`:

```csharp
new ConditionalFormattingDescriptor(
    ruleId: "complex",
    @operator: ConditionalFormattingOperator.Custom,
    predicate: ctx => ((MetricRow)ctx.Item).Score > 80 && ((MetricRow)ctx.Item).Status == "At Risk",
    target: ConditionalFormattingTarget.Row,
    themeKey: "RowWarningTheme")
```

## Troubleshooting

- Rule matches but no visual change:
  Verify `themeKey` resolves in grid resources and target type matches (`DataGridCell` vs `DataGridRow`).
- Cell rule never fires:
  Ensure `columnId` matches the real column id (`ColumnKey`/column/definition id) or use `propertyPath`.
- Conflicting styles:
  Set explicit `priority` and `stopIfTrue` to control composition.

## Full sample references

- `src/DataGridSample/Pages/ConditionalFormattingPage.axaml`
- `src/DataGridSample/ViewModels/ConditionalFormattingSampleViewModel.cs`
- `src/DataGridSample/Pages/PowerFxRulesPage.axaml`

## Related articles

- [Conditional Formatting](conditional-formatting.md)
- [Column Definitions (Model Integration)](column-definitions-models.md)
- [Filtering Model: End-to-End Usage](filtering-model-end-to-end.md)
