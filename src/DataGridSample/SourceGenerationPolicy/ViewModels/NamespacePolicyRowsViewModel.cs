// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System.Collections.ObjectModel;
using DataGridSample.Models.SourceGenerationPolicy;
using ReactiveUI;

namespace DataGridSample.SourceGenerationPolicy.ViewModels;

public sealed partial class NamespacePolicyRowsViewModel : ReactiveObject
{
    public NamespacePolicyRowsViewModel()
    {
        Items =
        [
            new NamespacePolicyRow { Sequence = 1, Symbol = "AVLN", Price = 128.40m, IsStreaming = true },
            new NamespacePolicyRow { Sequence = 2, Symbol = "RXUI", Price = 84.15m, IsStreaming = true },
            new NamespacePolicyRow { Sequence = 3, Symbol = "AOT", Price = 212.90m, IsStreaming = true }
        ];
    }

    public ObservableCollection<NamespacePolicyRow> Items { get; }
}
