// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using Avalonia.Controls;
using ProDataGrid.SourceGeneration;

namespace DataGridSample.Models.SourceGenerationPolicy;

[GenerateDataGridColumns(
    ProviderName = "ExplicitPolicyRowSchema",
    ProviderNamespace = "DataGridSample.Generated",
    SchemaId = "sample/explicit-policy-row/v2",
    StateVersion = 2,
    Discovery = DataGridColumnDiscovery.AttributedOnly,
    Strict = false,
    Streaming = false,
    PerformanceProfile = DataGridGeneratedPerformanceProfile.Spreadsheet)]
public sealed class ExplicitPolicyRow
{
    [DataGridKey]
    [DataGridColumn(DataGridColumnKind.Numeric, Header = "ID", ColumnKey = "id", Width = "72", IsReadOnly = true)]
    public int Id { get; set; }

    [DataGridColumn(Header = "Explicit name", ColumnKey = "name", Width = "1.5*")]
    public string Name { get; set; } = string.Empty;

    [DataGridColumn(DataGridColumnKind.Numeric, Header = "Budget", ColumnKey = "budget", Width = "120", FormatString = "N0")]
    public decimal Budget { get; set; }

    public string ExcludedByAttributedOnlyDiscovery { get; set; } = string.Empty;
}
