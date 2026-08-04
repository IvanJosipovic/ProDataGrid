using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Data.Core;
using ReactiveUI;

namespace DataGridSample.ViewModels;

public sealed class ColumnWidthSharingViewModel : ReactiveObject
{
    public ColumnWidthSharingViewModel()
    {
        CurrentItems =
        [
            new ComparisonRow("Refresh customer dashboard", "Ava", "In progress"),
            new ComparisonRow("Review export accessibility", "Marek", "Ready"),
            new ComparisonRow("Publish release notes", "Noah", "Blocked")
        ];

        BaselineItems =
        [
            new ComparisonRow("Dashboard", "Ava Kowalska", "Complete"),
            new ComparisonRow("Accessibility audit", "Marek", "In review"),
            new ComparisonRow("Release notes", "Noah", "Complete")
        ];

        BaselineColumns =
        [
            new DataGridTextColumnDefinition
            {
                Header = "Work item",
                Binding = CreateBinding(nameof(ComparisonRow.WorkItem), static row => row.WorkItem),
                Width = DataGridLength.Auto,
                WidthSharingGroup = "work-item"
            },
            new DataGridTextColumnDefinition
            {
                Header = "Owner",
                Binding = CreateBinding(nameof(ComparisonRow.Owner), static row => row.Owner),
                Width = new DataGridLength(210),
                WidthSharingGroup = "owner"
            },
            new DataGridTextColumnDefinition
            {
                Header = "Status",
                Binding = CreateBinding(nameof(ComparisonRow.Status), static row => row.Status),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                WidthSharingGroup = "status"
            }
        ];
    }

    public IReadOnlyList<ComparisonRow> CurrentItems { get; }

    public IReadOnlyList<ComparisonRow> BaselineItems { get; }

    public DataGridColumnDefinitionList BaselineColumns { get; }

    private static DataGridBindingDefinition CreateBinding(
        string propertyName,
        Func<ComparisonRow, string> getter)
    {
        var propertyInfo = new ClrPropertyInfo(
            propertyName,
            target => target is ComparisonRow row ? getter(row) : null,
            setter: null,
            typeof(string));

        return DataGridBindingDefinition.Create<ComparisonRow, string>(propertyInfo, getter);
    }

    public sealed record ComparisonRow(string WorkItem, string Owner, string Status);
}
