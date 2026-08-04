using System;
using System.Collections.ObjectModel;
using DataGridSample.Models;
using ReactiveUI;

namespace DataGridSample.ViewModels;

/// <summary>
/// Supplies interface-typed collections for the explicit-interface sorting showcase.
/// </summary>
public sealed class ExplicitInterfaceSortingViewModel : ReactiveObject
{
    /// <summary>Initializes every supported explicit-interface sorting scenario.</summary>
    public ExplicitInterfaceSortingViewModel()
    {
        DirectRows = new ObservableCollection<IExplicitSortRow>
        {
            new ExplicitSortRow("Charlie", new DateTime(2026, 7, 30, 12, 30, 0)),
            new AlternateExplicitSortRow("Alice", new DateTime(2026, 7, 30, 8, 15, 0)),
            new ExplicitSortRow("Bob", new DateTime(2026, 7, 30, 10, 45, 0))
        };
        InheritedRows = new ObservableCollection<IInheritedExplicitSortRow>
        {
            new InheritedExplicitSortRow("Charlie", 30),
            new InheritedExplicitSortRow("Alice", 10),
            new InheritedExplicitSortRow("Bob", 20)
        };
        NestedRows = new ObservableCollection<INestedExplicitSortRow>
        {
            new NestedExplicitSortRow("ROW-3", new ExplicitSortDetail("Charlie", 30)),
            new NestedExplicitSortRow("ROW-0", null),
            new NestedExplicitSortRow("ROW-1", new ExplicitSortDetail("Alice", 10)),
            new NestedExplicitSortRow("ROW-2", new ExplicitSortDetail("Bob", 20))
        };

        DualExplicitLabelRow[] labels =
        {
            new("Charlie", "Alpha"),
            new("Alice", "Zulu"),
            new("Bob", "Mike")
        };
        PrimaryLabelRows = new ObservableCollection<IPrimaryExplicitLabel>(labels);
        SecondaryLabelRows = new ObservableCollection<ISecondaryExplicitLabel>(labels);
    }

    /// <summary>Gets direct and polymorphic explicit-interface rows.</summary>
    public ObservableCollection<IExplicitSortRow> DirectRows { get; }

    /// <summary>Gets rows whose sortable name is declared by a base interface.</summary>
    public ObservableCollection<IInheritedExplicitSortRow> InheritedRows { get; }

    /// <summary>Gets rows with nullable nested explicit-interface paths.</summary>
    public ObservableCollection<INestedExplicitSortRow> NestedRows { get; }

    /// <summary>Gets dual-label rows viewed through the primary interface.</summary>
    public ObservableCollection<IPrimaryExplicitLabel> PrimaryLabelRows { get; }

    /// <summary>Gets the same dual-label rows viewed through the secondary interface.</summary>
    public ObservableCollection<ISecondaryExplicitLabel> SecondaryLabelRows { get; }
}
