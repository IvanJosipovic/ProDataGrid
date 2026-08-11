// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using Avalonia.Controls;
using Avalonia.Data.Core;
using ProDataGrid.SourceGeneration;

namespace DataGridSample.Models;

[GenerateDataGridColumns(
    ProviderName = "GeneratedCustomImplementationRowSchema",
    SchemaId = "sample/generated-custom-implementation-row/v1",
    Discovery = DataGridColumnDiscovery.AttributedOnly,
    Strict = true,
    ConfigureMethod = nameof(ConfigureColumns))]
public sealed class GeneratedCustomImplementationRow
{
    [DataGridKey]
    [DataGridColumn(DataGridColumnKind.Numeric, Header = "ID", ColumnKey = "id", Width = "64", IsReadOnly = true)]
    public int Id { get; init; }

    [DataGridColumn(
        Header = "Work item",
        ColumnKey = "title",
        Width = "1.8*",
        FactoryMethod = nameof(CreateTitleColumn))]
    public string Title { get; set; } = string.Empty;

    [DataGridColumn(
        Header = "Severity",
        ColumnKey = "severity",
        Width = "112",
        ConfigureMethod = nameof(ConfigureSeverityColumn))]
    public string Severity { get; set; } = string.Empty;

    [DataGridColumn(
        DataGridColumnKind.Numeric,
        Header = "Effort",
        ColumnKey = "effort",
        Width = "92",
        Minimum = 1,
        Maximum = 40,
        ValidatorMethod = nameof(ValidateEffort))]
    public int Effort { get; set; }

    [DataGridSummary(DataGridAggregateType.Average, Scope = DataGridSummaryScope.Total, Format = "N1", Title = "Weighted score: ")]
    [DataGridColumn(
        DataGridColumnKind.Numeric,
        Header = "Score",
        ColumnKey = "score",
        Width = "170",
        FormatString = "N1",
        ConfigureMethod = nameof(ConfigureScoreColumn))]
    public double Score { get; set; }

    [DataGridColumn(DataGridColumnKind.CheckBox, Header = "Ready", ColumnKey = "ready", Width = "82")]
    public bool IsReady { get; set; }

    public static DataGridColumnDefinition CreateTitleColumn() =>
        DataGridColumnDefinitionBuilder.For<GeneratedCustomImplementationRow>().Text(
            "Work item",
            new ClrPropertyInfo(
                nameof(Title),
                static target => ((GeneratedCustomImplementationRow)target).Title,
                static (target, value) => ((GeneratedCustomImplementationRow)target).Title = (string?)value ?? string.Empty,
                typeof(string)),
            static row => row.Title,
            static (row, value) => row.Title = value,
            static column => column.Watermark = "Created by the user-defined column factory");

    public static void ConfigureSeverityColumn(DataGridTextColumnDefinition column)
    {
        column.Options = new DataGridColumnDefinitionOptions
        {
            SortValueComparer = GeneratedSeverityComparer.Instance
        };
        column.Tag = "custom-comparer-hook";
    }

    public static string? ValidateEffort(GeneratedCustomImplementationRow item, int value)
    {
        if (value is < 1 or > 40)
        {
            return "Effort must be between 1 and 40.";
        }

        return item.Severity == "Critical" && value > 16
            ? "Critical work is split when effort exceeds 16."
            : null;
    }

    public static void ConfigureScoreColumn(DataGridNumericColumnDefinition column)
    {
        for (int index = 0; index < column.SummaryDefinitions.Count; index++)
        {
            column.SummaryDefinitions[index].Factory = static () =>
                new DataGridCustomSummaryDescription
                {
                    Calculator = new GeneratedWeightedScoreSummaryCalculator()
                };
        }
    }

    public static void ConfigureColumns(DataGridColumnDefinitionList columns)
    {
        for (int index = 0; index < columns.Count; index++)
        {
            columns[index].WidthSharingGroup = "generated-custom-implementations";
            if (columns[index].Tag is null)
            {
                columns[index].Tag = "schema-configure-hook";
            }
        }
    }
}
