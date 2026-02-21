using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using Avalonia.Controls;
using DataGridSample.Models;
using DataGridSample.Mvvm;

namespace DataGridSample.ViewModels;

public sealed class LogicalScrollPerformanceViewModel : ObservableObject
{
    private const int DefaultRowCount = 200_000;
    private int _itemCount = DefaultRowCount;
    private string _selectedEstimator = "Advanced";
    private string _summary = "Preparing rows...";
    private IDataGridRowHeightEstimator _rowHeightEstimator = new AdvancedRowHeightEstimator();

    public LogicalScrollPerformanceViewModel()
    {
        Estimators = new[] { "Advanced", "Caching", "Default" };
        Rows = new ObservableCollection<LogicalScrollPerformanceRow>();
        RegenerateCommand = new RelayCommand(_ => PopulateRows());
        PopulateRows();
    }

    public ObservableCollection<LogicalScrollPerformanceRow> Rows { get; }

    public IReadOnlyList<string> Estimators { get; }

    public ICommand RegenerateCommand { get; }

    public int ItemCount
    {
        get => _itemCount;
        set => SetProperty(ref _itemCount, value);
    }

    public string SelectedEstimator
    {
        get => _selectedEstimator;
        set
        {
            if (SetProperty(ref _selectedEstimator, value))
            {
                RowHeightEstimator = CreateEstimator(value);
                UpdateSummary(0);
            }
        }
    }

    public IDataGridRowHeightEstimator RowHeightEstimator
    {
        get => _rowHeightEstimator;
        private set => SetProperty(ref _rowHeightEstimator, value);
    }

    public string Summary
    {
        get => _summary;
        private set => SetProperty(ref _summary, value);
    }

    private static IDataGridRowHeightEstimator CreateEstimator(string? name)
    {
        return name switch
        {
            "Caching" => new CachingRowHeightEstimator(),
            "Default" => new DefaultRowHeightEstimator(),
            _ => new AdvancedRowHeightEstimator()
        };
    }

    private void PopulateRows()
    {
        var stopwatch = Stopwatch.StartNew();
        Rows.Clear();

        var random = new Random(12345);
        for (int i = 1; i <= ItemCount; i++)
        {
            Rows.Add(new LogicalScrollPerformanceRow
            {
                Id = i,
                Category = $"CAT-{i % 50:D2}",
                Code = $"CODE-{i % 1000:D4}",
                Amount = Math.Round((random.NextDouble() * 100000) - 50000, 2),
                CreatedAt = DateTime.UtcNow.AddSeconds(-i),
                Description = $"Row {i} | sample payload for scroll performance checks"
            });
        }

        stopwatch.Stop();
        UpdateSummary(stopwatch.Elapsed.TotalSeconds);
    }

    private void UpdateSummary(double loadSeconds)
    {
        Summary = loadSeconds > 0
            ? $"Rows: {Rows.Count:n0} | Loaded in {loadSeconds:N2}s | Estimator: {SelectedEstimator}"
            : $"Rows: {Rows.Count:n0} | Estimator: {SelectedEstimator}";
    }
}
