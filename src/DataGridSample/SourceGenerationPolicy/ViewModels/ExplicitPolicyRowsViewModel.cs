// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System.Collections.ObjectModel;
using Avalonia.Controls;
using DataGridSample.Models.SourceGenerationPolicy;
using ProDataGrid.SourceGeneration;
using ReactiveUI;

namespace DataGridSample.SourceGenerationPolicy.ViewModels;

[GenerateDataGridViewModel(typeof(ExplicitPolicyRow), Strict = false, Streaming = false)]
[GenerateDataGridView(
    typeof(ExplicitPolicyRow),
    Framework = DataGridViewFramework.ReactiveUI,
    Recipe = DataGridViewRecipe.Spreadsheet,
    Title = "Explicit type override",
    AutomationId = "generated-explicit-policy-rows",
    IsReadOnly = false,
    PerformanceProfile = DataGridGeneratedPerformanceProfile.Spreadsheet)]
public sealed partial class ExplicitPolicyRowsViewModel : ReactiveObject
{
    public ExplicitPolicyRowsViewModel()
    {
        Items =
        [
            new ExplicitPolicyRow { Id = 10, Name = "Attributed-only discovery", Budget = 720_000m, ExcludedByAttributedOnlyDiscovery = "Not generated" },
            new ExplicitPolicyRow { Id = 20, Name = "Custom provider and schema ID", Budget = 1_240_000m, ExcludedByAttributedOnlyDiscovery = "Not generated" },
            new ExplicitPolicyRow { Id = 30, Name = "Spreadsheet view override", Budget = 980_000m, ExcludedByAttributedOnlyDiscovery = "Not generated" }
        ];
    }

    public ObservableCollection<ExplicitPolicyRow> Items { get; }
}
