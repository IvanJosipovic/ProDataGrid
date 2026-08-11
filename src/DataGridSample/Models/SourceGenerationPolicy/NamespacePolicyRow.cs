// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

namespace DataGridSample.Models.SourceGenerationPolicy;

public sealed class NamespacePolicyRow
{
    public int Sequence { get; set; }

    public string Symbol { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public bool IsStreaming { get; set; }
}
