using System;
using System.Diagnostics.CodeAnalysis;
using ReactiveUI;

namespace DataGridSample.Models;

/// <summary>
/// Represents one editable task in the continuous editing sample.
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicProperties)]
public sealed class ContinuousEditingRow : ReactiveObject
{
    private string _task = string.Empty;
    private string _owner = string.Empty;
    private string _priority = "Normal";
    private decimal _estimate;
    private DateTime? _dueDate;

    /// <summary>
    /// Gets or sets the task description.
    /// </summary>
    public string Task
    {
        get => _task;
        set => this.RaiseAndSetIfChanged(ref _task, value);
    }

    /// <summary>
    /// Gets or sets the task owner.
    /// </summary>
    public string Owner
    {
        get => _owner;
        set => this.RaiseAndSetIfChanged(ref _owner, value);
    }

    /// <summary>
    /// Gets or sets the task priority.
    /// </summary>
    public string Priority
    {
        get => _priority;
        set => this.RaiseAndSetIfChanged(ref _priority, value);
    }

    /// <summary>
    /// Gets or sets the estimate in hours.
    /// </summary>
    public decimal Estimate
    {
        get => _estimate;
        set => this.RaiseAndSetIfChanged(ref _estimate, value);
    }

    /// <summary>
    /// Gets or sets the optional due date.
    /// </summary>
    public DateTime? DueDate
    {
        get => _dueDate;
        set => this.RaiseAndSetIfChanged(ref _dueDate, value);
    }
}
