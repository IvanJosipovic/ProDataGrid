// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using Avalonia.Controls;
using ProDataGrid.SourceGeneration;

namespace DataGridSample.Models;

[GenerateDataGridColumns(
    ProviderName = "GeneratedOutlineWorkItemSchema",
    SchemaId = "sample/generated-outline-work-item/v1",
    Discovery = DataGridColumnDiscovery.AttributedOnly,
    Strict = true)]
public sealed class GeneratedOutlineWorkItem
{
    [DataGridKey]
    [DataGridColumn(DataGridColumnKind.Numeric, Header = "ID", ColumnKey = "id", Order = 0, Width = "72", IsReadOnly = true)]
    public int Id { get; init; }

    [DataGridColumn(Header = "Region", ColumnKey = "region", Order = 1, Width = "*")]
    [DataGridOutlineField(DataGridGeneratedAnalyticsRole.OutlineGroup, Order = 0, Name = "Region")]
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
        Name = "Actual",
        Format = "C0",
        Aggregate = DataGridAggregateType.Sum)]
    public decimal Actual { get; init; }

    [DataGridColumn(DataGridColumnKind.ProgressBar, Header = "Progress", ColumnKey = "progress", Order = 6, Width = "*")]
    public double Progress { get; init; }

    [DataGridColumn(DataGridColumnKind.CheckBox, Header = "Locked", ColumnKey = "locked", Order = 7, Width = "82", IsReadOnly = true)]
    public bool Locked { get; init; }
}
