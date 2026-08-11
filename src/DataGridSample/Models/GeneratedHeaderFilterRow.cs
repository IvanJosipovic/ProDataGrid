// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using Avalonia.Controls;
using ProDataGrid.SourceGeneration;

namespace DataGridSample.Models;

[GenerateDataGridColumns(
    ProviderName = "GeneratedHeaderFilterRowSchema",
    SchemaId = "sample/generated-header-filter-row/v1",
    Discovery = DataGridColumnDiscovery.AttributedOnly,
    Strict = true)]
public sealed class GeneratedHeaderFilterRow
{
    [DataGridKey]
    [DataGridColumn(
        DataGridColumnKind.Numeric,
        Header = "ID",
        ColumnKey = "id",
        Width = "64",
        IsReadOnly = true,
        FilterEditor = DataGridGeneratedFilterEditorKind.Numeric)]
    public int Id { get; init; }

    [DataGridColumn(
        Header = "Symbol",
        ColumnKey = "symbol",
        Width = "*",
        FilterEditor = DataGridGeneratedFilterEditorKind.Text)]
    public string Symbol { get; init; } = string.Empty;

    [DataGridColumn(
        Header = "Desk",
        ColumnKey = "desk",
        Width = "*",
        FilterEditor = DataGridGeneratedFilterEditorKind.Distinct,
        FilterFlyoutKey = "GeneratedHeaderDeskDistinctFilterFlyout")]
    public string Desk { get; init; } = string.Empty;

    [DataGridColumn(
        Header = "Side",
        ColumnKey = "side",
        Width = "88",
        FilterEditor = DataGridGeneratedFilterEditorKind.Enum)]
    public GeneratedHeaderFilterSide Side { get; init; }

    [DataGridColumn(
        DataGridColumnKind.Numeric,
        Header = "Price",
        ColumnKey = "price",
        Width = "105",
        FormatString = "N2",
        FilterEditor = DataGridGeneratedFilterEditorKind.Range)]
    public decimal Price { get; init; }

    [DataGridColumn(
        DataGridColumnKind.DatePicker,
        Header = "Timestamp",
        ColumnKey = "timestamp",
        Width = "145",
        IsReadOnly = true,
        FilterEditor = DataGridGeneratedFilterEditorKind.DateTime)]
    public DateTimeOffset Timestamp { get; init; }

    [DataGridColumn(
        DataGridColumnKind.CheckBox,
        Header = "Active",
        ColumnKey = "active",
        Width = "78",
        FilterEditor = DataGridGeneratedFilterEditorKind.Boolean)]
    public bool IsActive { get; init; }
}

public enum GeneratedHeaderFilterSide
{
    Buy,
    Sell
}
