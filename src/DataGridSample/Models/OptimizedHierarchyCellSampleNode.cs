using System;
using System.Collections.Generic;

namespace DataGridSample.Models;

public sealed class OptimizedHierarchyCellSampleNode
{
    public OptimizedHierarchyCellSampleNode(
        int id,
        string name,
        string kind,
        string owner,
        string region,
        string state,
        string detail,
        IReadOnlyList<OptimizedHierarchyCellSampleNode> children,
        bool isExpanded)
    {
        Id = id;
        Name = name;
        Kind = kind;
        Owner = owner;
        Region = region;
        State = state;
        Detail = detail;
        Children = children;
        IsExpanded = isExpanded;
        Date = new DateTime(2020, 1, 1).AddDays(id % 3_650);
        Time = TimeSpan.FromSeconds(id % 86_400);
        Phone = $"(555) {id % 1_000:D3}-{id % 10_000:D4}";
        Category = $"Category-{id % 997:D3}";
        SliderValue = (id % 1_000) / 10d;
    }

    public int Id { get; }

    public string Name { get; }

    public string Kind { get; }

    public string Owner { get; }

    public string Region { get; }

    public string State { get; }

    public string Detail { get; }

    public bool HasChildren => Children.Count != 0;

    public DateTime Date { get; }

    public TimeSpan Time { get; }

    public string Phone { get; }

    public string Category { get; }

    public double SliderValue { get; }

    public IReadOnlyList<OptimizedHierarchyCellSampleNode> Children { get; }

    public bool IsExpanded { get; set; }
}
