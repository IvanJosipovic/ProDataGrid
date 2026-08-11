// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System.Collections;
using System.Collections.Generic;
using Avalonia.Controls;

namespace DataGridSample.Models;

public sealed class GeneratedWeightedScoreSummaryCalculator : IDataGridSummaryCalculator
{
    public string Name => "Generated weighted score";

    public bool SupportsIncremental => false;

    public double Calculate(IEnumerable<GeneratedCustomImplementationRow> items)
    {
        double weightedTotal = 0d;
        int totalEffort = 0;
        foreach (GeneratedCustomImplementationRow row in items)
        {
            weightedTotal += row.Score * row.Effort;
            totalEffort += row.Effort;
        }

        return totalEffort == 0 ? 0d : weightedTotal / totalEffort;
    }

    public object? Calculate(IEnumerable items, DataGridColumn column, string? propertyName)
    {
        double weightedTotal = 0d;
        int totalEffort = 0;
        foreach (object? item in items)
        {
            if (item is not GeneratedCustomImplementationRow row)
            {
                continue;
            }

            weightedTotal += row.Score * row.Effort;
            totalEffort += row.Effort;
        }

        return totalEffort == 0 ? 0d : weightedTotal / totalEffort;
    }

    public IDataGridSummaryState? CreateState() => null;
}
