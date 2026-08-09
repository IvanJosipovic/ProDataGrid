// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using Avalonia.Controls;
using Avalonia.Controls.DataGridPivoting;
using Avalonia.Controls.DataGridReporting;
using ProDataGrid.SourceGeneration;

namespace DataGridSample.Models;

[GenerateDataGridColumns(
    ProviderName = "GeneratedOutlineWorkItemSchema",
    SchemaId = "sample/generated-outline-work-item/v1",
    Discovery = DataGridColumnDiscovery.AttributedOnly,
    Strict = true,
    OutlineConfigureMethod = nameof(ConfigureOutline))]
public sealed class GeneratedOutlineWorkItem
{
    [DataGridKey]
    [DataGridColumn(DataGridColumnKind.Numeric, Header = "ID", ColumnKey = "id", Order = 0, Width = "72", IsReadOnly = true)]
    public int Id { get; init; }

    [DataGridColumn(Header = "Region", ColumnKey = "region", Order = 1, Width = "*")]
    [DataGridOutlineField(
        DataGridGeneratedAnalyticsRole.OutlineGroup,
        Order = 0,
        Name = "Region",
        ConfigureMethod = nameof(ConfigureGroup))]
    public string Region { get; init; } = string.Empty;

    [DataGridColumn(Header = "Team", ColumnKey = "team", Order = 2, Width = "*")]
    [DataGridOutlineField(DataGridGeneratedAnalyticsRole.OutlineGroup, Order = 1, Name = "Team")]
    public string Team { get; init; } = string.Empty;

    [DataGridColumn(Header = "Work item", ColumnKey = "work-item", Order = 3, Width = "2*")]
    public string WorkItem { get; init; } = string.Empty;

    [DataGridColumn(DataGridColumnKind.Numeric, Header = "Planned", ColumnKey = "planned", Order = 4, Width = "*", FormatString = "C0")]
    [DataGridOutlineField(
        DataGridGeneratedAnalyticsRole.OutlineDetail,
        Order = 0,
        Name = "Planned",
        Format = "C0",
        Aggregate = DataGridAggregateType.Sum)]
    public decimal Planned { get; init; }

    [DataGridColumn(DataGridColumnKind.Numeric, Header = "Actual", ColumnKey = "actual", Order = 5, Width = "*", FormatString = "C0")]
    [DataGridOutlineField(
        DataGridGeneratedAnalyticsRole.OutlineDetail,
        Order = 1,
        Name = "Actual range",
        Format = "C0",
        Aggregate = DataGridAggregateType.Custom,
        CustomAggregatorFactoryMethod = nameof(CreateRangeAggregator),
        ConfigureMethod = nameof(ConfigureActual))]
    public decimal Actual { get; init; }

    [DataGridColumn(DataGridColumnKind.ProgressBar, Header = "Progress", ColumnKey = "progress", Order = 6, Width = "*")]
    public double Progress { get; init; }

    [DataGridColumn(DataGridColumnKind.CheckBox, Header = "Locked", ColumnKey = "locked", Order = 7, Width = "82", IsReadOnly = true)]
    public bool Locked { get; init; }

    public static IPivotAggregator CreateRangeAggregator() => new GeneratedRangeAggregator();

    public static void ConfigureGroup(OutlineGroupField field) => field.ShowSubtotals = true;

    public static void ConfigureActual(OutlineValueField field) => field.NullLabel = "No actuals";

    public static void ConfigureOutline(OutlineReportModel report)
    {
        report.Layout.RowHeaderLabel = "Region / team";
        report.Layout.ShowSubtotals = true;
        report.Layout.ShowGrandTotal = true;
        report.Layout.ShowDetailRows = true;
        report.Layout.AutoExpandGroups = true;
        report.Layout.DetailLabelSelector = static item =>
            item is GeneratedOutlineWorkItem workItem ? workItem.WorkItem : string.Empty;
    }
}

internal sealed class GeneratedRangeAggregator : IPivotAggregator
{
    public PivotAggregateType AggregateType => PivotAggregateType.Custom;

    public string Name => "Range";

    public IPivotAggregationState CreateState() => new RangeState();

    private sealed class RangeState : IPivotAggregationState
    {
        private decimal _minimum;
        private decimal _maximum;
        private bool _hasValue;

        public void Add(object? value)
        {
            if (value is not IConvertible convertible)
            {
                return;
            }

            decimal current = convertible.ToDecimal(System.Globalization.CultureInfo.InvariantCulture);
            if (!_hasValue)
            {
                _minimum = current;
                _maximum = current;
                _hasValue = true;
                return;
            }

            _minimum = Math.Min(_minimum, current);
            _maximum = Math.Max(_maximum, current);
        }

        public void Merge(IPivotAggregationState other)
        {
            if (other is not RangeState range || !range._hasValue)
            {
                return;
            }

            Add(range._minimum);
            Add(range._maximum);
        }

        public object? GetResult() => _hasValue ? _maximum - _minimum : null;
    }
}
