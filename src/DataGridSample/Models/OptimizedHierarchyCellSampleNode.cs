using System.Collections.Generic;

namespace DataGridSample.Models;

public sealed class OptimizedHierarchyCellSampleNode
{
    public OptimizedHierarchyCellSampleNode(
        string name,
        string kind,
        string owner,
        string region,
        string state,
        string detail,
        IReadOnlyList<OptimizedHierarchyCellSampleNode> children,
        bool isExpanded)
    {
        Name = name;
        Kind = kind;
        Owner = owner;
        Region = region;
        State = state;
        Detail = detail;
        Children = children;
        IsExpanded = isExpanded;
    }

    public string Name { get; }

    public string Kind { get; }

    public string Owner { get; }

    public string Region { get; }

    public string State { get; }

    public string Detail { get; }

    public IReadOnlyList<OptimizedHierarchyCellSampleNode> Children { get; }

    public bool IsExpanded { get; set; }
}
