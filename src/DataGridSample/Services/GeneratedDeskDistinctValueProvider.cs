// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace DataGridSample.Services;

internal sealed class GeneratedDeskDistinctValueProvider : IDataGridGeneratedRemoteDistinctValueProvider<string>
{
    private static readonly string[] s_desks =
    [
        "Warsaw",
        "London",
        "New York",
        "Singapore",
        "Frankfurt",
        "Sydney"
    ];

    public async ValueTask<IReadOnlyList<string>> ExecuteAsync(
        DataGridGeneratedDistinctValueQuery query,
        CancellationToken cancellationToken)
    {
        if (string.Equals(query.SearchText, "slow", StringComparison.OrdinalIgnoreCase))
        {
            // Deliberately ignore cancellation to prove revision-based stale-response suppression.
            await Task.Delay(80, CancellationToken.None);
        }
        else
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
        }

        string search = query.SearchText?.Trim() ?? string.Empty;
        var result = new List<string>(Math.Min(query.MaximumResults, s_desks.Length));
        for (int index = 0; index < s_desks.Length && result.Count < query.MaximumResults; index++)
        {
            string desk = s_desks[index];
            if (search.Length == 0 || desk.Contains(search, StringComparison.OrdinalIgnoreCase))
            {
                result.Add(desk);
            }
        }

        return result.AsReadOnly();
    }
}
