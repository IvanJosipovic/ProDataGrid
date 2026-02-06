using System;
using System.Collections.ObjectModel;
using DataGridSample.Models;
using ReactiveUI;

namespace DataGridSample.ViewModels;

public sealed class LogicalScrollStabilityViewModel : ReactiveObject
{
    public LogicalScrollStabilityViewModel()
    {
        Items = new ObservableCollection<LogicalScrollStabilityRow>(CreateRows(200));
    }

    public ObservableCollection<LogicalScrollStabilityRow> Items { get; }

    private static LogicalScrollStabilityRow[] CreateRows(int count)
    {
        var random = new Random(42);
        var categories = new[] { "Alpha", "Beta", "Gamma", "Delta" };
        var statuses = new[] { "New", "Queued", "Running", "Done" };
        var owners = new[] { "Ava", "Ben", "Chen", "Diya", "Eli" };

        var rows = new LogicalScrollStabilityRow[count];
        for (int i = 1; i <= count; i++)
        {
            rows[i - 1] = new LogicalScrollStabilityRow
            {
                Id = i,
                Name = $"Item {i:000}",
                Category = categories[i % categories.Length],
                Status = statuses[i % statuses.Length],
                Owner = owners[i % owners.Length],
                Quantity = random.Next(1, 500),
                Price = Math.Round(random.NextDouble() * 1000, 2),
                Delta = Math.Round(random.NextDouble() * 50 - 25, 2),
                Score = Math.Round(random.NextDouble() * 100, 1),
                Ratio = Math.Round(random.NextDouble() * 10, 3)
            };
        }

        return rows;
    }
}
