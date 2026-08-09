using System;
using System.Collections.Generic;

namespace DataGridSample.Models;

public enum DataGridFeatureContractEventKind
{
    CellPrepared,
    CellClearing,
    CellValueChanged,
    SelectionChanging,
}

public sealed class DataGridFeatureContractEvent
{
    public DataGridFeatureContractEvent(
        DataGridFeatureContractEventKind kind,
        string message,
        IReadOnlyList<object>? addedItems = null,
        IReadOnlyList<object>? removedItems = null)
    {
        Kind = kind;
        Message = message ?? throw new ArgumentNullException(nameof(message));
        AddedItems = addedItems ?? Array.Empty<object>();
        RemovedItems = removedItems ?? Array.Empty<object>();
    }

    public DataGridFeatureContractEventKind Kind { get; }

    public string Message { get; }

    public IReadOnlyList<object> AddedItems { get; }

    public IReadOnlyList<object> RemovedItems { get; }

    public bool Cancel { get; set; }
}
