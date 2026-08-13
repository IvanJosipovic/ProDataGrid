using System;

namespace DataGridSample.Models;

public sealed record OptimizedCellSampleRow(
    string Id,
    string Name,
    string Category,
    string Owner,
    string Region,
    string State,
    string Detail,
    bool IsActive,
    DateTime Date,
    TimeSpan Time,
    string Phone,
    double SliderValue);
