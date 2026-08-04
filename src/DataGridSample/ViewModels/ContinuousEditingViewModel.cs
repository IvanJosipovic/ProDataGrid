using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DataGridSample.Models;
using ReactiveUI;

namespace DataGridSample.ViewModels;

/// <summary>
/// Supplies editable rows and editor choices for the continuous editing sample.
/// </summary>
public sealed class ContinuousEditingViewModel : ReactiveObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContinuousEditingViewModel"/> class.
    /// </summary>
    public ContinuousEditingViewModel()
    {
        OwnerSuggestions = new[] { "Avery", "Jordan", "Morgan", "Riley", "Sam" };
        Priorities = new[] { "Low", "Normal", "High", "Critical" };
        Rows = new ObservableCollection<ContinuousEditingRow>
        {
            new()
            {
                Task = "Confirm release notes",
                Owner = "Avery",
                Priority = "High",
                Estimate = 2,
                DueDate = new DateTime(2026, 8, 5)
            },
            new()
            {
                Task = "Run keyboard navigation checks",
                Owner = "Jordan",
                Priority = "Normal",
                Estimate = 4,
                DueDate = new DateTime(2026, 8, 6)
            },
            new()
            {
                Task = "Prepare the sample gallery",
                Owner = "Morgan",
                Priority = "Critical",
                Estimate = 3,
                DueDate = new DateTime(2026, 8, 7)
            }
        };
    }

    /// <summary>
    /// Gets the editable source rows.
    /// </summary>
    public ObservableCollection<ContinuousEditingRow> Rows { get; }

    /// <summary>
    /// Gets the autocomplete choices for the owner column.
    /// </summary>
    public IReadOnlyList<string> OwnerSuggestions { get; }

    /// <summary>
    /// Gets the choices for the editable priority column.
    /// </summary>
    public IReadOnlyList<string> Priorities { get; }
}
