// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System.Collections;

namespace DataGridSample.Models;

public sealed class GeneratedSeverityComparer : IComparer
{
    public static GeneratedSeverityComparer Instance { get; } = new();

    public int Compare(object? x, object? y) => Rank(x as string).CompareTo(Rank(y as string));

    private static int Rank(string? value) => value switch
    {
        "Critical" => 0,
        "High" => 1,
        "Medium" => 2,
        "Low" => 3,
        _ => 4
    };
}
