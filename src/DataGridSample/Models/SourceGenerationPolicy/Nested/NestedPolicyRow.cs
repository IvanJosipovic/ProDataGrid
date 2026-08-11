// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

namespace DataGridSample.Models.SourceGenerationPolicy.Nested;

public sealed class NestedPolicyRow
{
    public int Id { get; set; }

    public string ExcludedByNamespacePolicy { get; set; } = string.Empty;
}
