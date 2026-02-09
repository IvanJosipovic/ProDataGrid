using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using DataGridSample.Mvvm;

namespace DataGridSample.ViewModels;

public sealed class TabSwitchBenchmarkViewModel : ObservableObject
{
    private int _rowCount = 400;
    private int _iterations = 30;
    private int _selectedInnerTabIndex;
    private bool _isRunning;
    private string _benchmarkSummary = "Run the benchmark to measure tab switch latency.";

    public TabSwitchBenchmarkViewModel()
    {
        Items = new ObservableCollection<TabSwitchBenchmarkRow>();
        Runs = new ObservableCollection<TabSwitchBenchmarkRun>();

        RegenerateRowsCommand = new RelayCommand(_ => RegenerateRows(), _ => !IsRunning);
        RunBenchmarkCommand = new RelayCommand(_ => _ = RunBenchmarkAsync(), _ => !IsRunning);
        ShowGridTabCommand = new RelayCommand(_ => SelectedInnerTabIndex = 0, _ => !IsRunning);
        ShowPlaceholderTabCommand = new RelayCommand(_ => SelectedInnerTabIndex = 1, _ => !IsRunning);

        RegenerateRows();
    }

    public ObservableCollection<TabSwitchBenchmarkRow> Items { get; }

    public ObservableCollection<TabSwitchBenchmarkRun> Runs { get; }

    public RelayCommand RegenerateRowsCommand { get; }

    public RelayCommand RunBenchmarkCommand { get; }

    public RelayCommand ShowGridTabCommand { get; }

    public RelayCommand ShowPlaceholderTabCommand { get; }

    public int RowCount
    {
        get => _rowCount;
        set => SetProperty(ref _rowCount, Math.Max(1, value));
    }

    public int Iterations
    {
        get => _iterations;
        set => SetProperty(ref _iterations, Math.Max(1, value));
    }

    public int SelectedInnerTabIndex
    {
        get => _selectedInnerTabIndex;
        set => SetProperty(ref _selectedInnerTabIndex, value);
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetProperty(ref _isRunning, value))
            {
                RegenerateRowsCommand.RaiseCanExecuteChanged();
                RunBenchmarkCommand.RaiseCanExecuteChanged();
                ShowGridTabCommand.RaiseCanExecuteChanged();
                ShowPlaceholderTabCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string BenchmarkSummary
    {
        get => _benchmarkSummary;
        private set => SetProperty(ref _benchmarkSummary, value);
    }

    private void RegenerateRows()
    {
        Items.Clear();

        for (var i = 0; i < RowCount; i++)
        {
            Items.Add(TabSwitchBenchmarkRow.Create(i));
        }

        BenchmarkSummary = $"Prepared {Items.Count.ToString("N0", CultureInfo.InvariantCulture)} rows. Switch tabs manually or run benchmark.";
    }

    private async Task RunBenchmarkAsync()
    {
        if (IsRunning)
        {
            return;
        }

        IsRunning = true;

        try
        {
            BenchmarkSummary = "Running benchmark...";

            var samples = new List<double>(Iterations * 2);

            // Warm-up to stabilize template/materialization costs.
            await MeasureSwitchAsync(targetTabIndex: 1, samples: null);
            await MeasureSwitchAsync(targetTabIndex: 0, samples: null);

            for (var i = 0; i < Iterations; i++)
            {
                await MeasureSwitchAsync(targetTabIndex: 1, samples);
                await MeasureSwitchAsync(targetTabIndex: 0, samples);
            }

            if (samples.Count == 0)
            {
                BenchmarkSummary = "No samples collected.";
                return;
            }

            samples.Sort();
            var average = samples.Average();
            var min = samples[0];
            var max = samples[^1];
            var p95Index = (int)Math.Ceiling(samples.Count * 0.95) - 1;
            p95Index = Math.Max(0, Math.Min(samples.Count - 1, p95Index));
            var p95 = samples[p95Index];

            var run = new TabSwitchBenchmarkRun(
                DateTimeOffset.Now,
                RowCount,
                Iterations,
                average,
                p95,
                max,
                min);

            Runs.Insert(0, run);
            while (Runs.Count > 12)
            {
                Runs.RemoveAt(Runs.Count - 1);
            }

            BenchmarkSummary = string.Create(
                CultureInfo.InvariantCulture,
                $"Rows={RowCount:N0}, Iterations={Iterations}, Samples={samples.Count}: avg={average:F2} ms, p95={p95:F2} ms, min={min:F2} ms, max={max:F2} ms.");
        }
        finally
        {
            IsRunning = false;
        }
    }

    private async Task MeasureSwitchAsync(int targetTabIndex, List<double>? samples)
    {
        var stopwatch = Stopwatch.StartNew();
        SelectedInnerTabIndex = targetTabIndex;

        await Dispatcher.UIThread.InvokeAsync(static () => { }, DispatcherPriority.Render);
        await Dispatcher.UIThread.InvokeAsync(static () => { }, DispatcherPriority.Background);

        stopwatch.Stop();

        if (samples != null)
        {
            samples.Add(stopwatch.Elapsed.TotalMilliseconds);
        }
    }
}

public sealed record TabSwitchBenchmarkRun(
    DateTimeOffset Timestamp,
    int RowCount,
    int Iterations,
    double AverageMs,
    double P95Ms,
    double MaxMs,
    double MinMs)
{
    public string Summary => string.Create(
        CultureInfo.InvariantCulture,
        $"{Timestamp:HH:mm:ss} rows={RowCount:N0} avg={AverageMs:F2}ms p95={P95Ms:F2}ms min={MinMs:F2}ms max={MaxMs:F2}ms");
}

public sealed class TabSwitchBenchmarkRow
{
    public int Index { get; init; }

    public string C01 { get; init; } = string.Empty;
    public string C02 { get; init; } = string.Empty;
    public string C03 { get; init; } = string.Empty;
    public string C04 { get; init; } = string.Empty;
    public string C05 { get; init; } = string.Empty;
    public string C06 { get; init; } = string.Empty;
    public string C07 { get; init; } = string.Empty;
    public string C08 { get; init; } = string.Empty;
    public string C09 { get; init; } = string.Empty;
    public string C10 { get; init; } = string.Empty;
    public string C11 { get; init; } = string.Empty;
    public string C12 { get; init; } = string.Empty;
    public string C13 { get; init; } = string.Empty;
    public string C14 { get; init; } = string.Empty;
    public string C15 { get; init; } = string.Empty;
    public string C16 { get; init; } = string.Empty;

    public static TabSwitchBenchmarkRow Create(int index)
    {
        var bucket = index % 12;
        var category = index % 4;

        return new TabSwitchBenchmarkRow
        {
            Index = index,
            C01 = $"Name {index}",
            C02 = $"Bucket {bucket}",
            C03 = $"Category {category}",
            C04 = (index * 17 % 101).ToString(CultureInfo.InvariantCulture),
            C05 = (index * 19 % 211).ToString(CultureInfo.InvariantCulture),
            C06 = (index * 23 % 307).ToString(CultureInfo.InvariantCulture),
            C07 = (index * 29 % 401).ToString(CultureInfo.InvariantCulture),
            C08 = (index * 31 % 503).ToString(CultureInfo.InvariantCulture),
            C09 = $"{DateTime.Today.AddDays(index % 31):yyyy-MM-dd}",
            C10 = $"{DateTime.Today.AddMinutes(index % 120):HH:mm}",
            C11 = $"Flag {(index % 2 == 0 ? "A" : "B")}",
            C12 = $"State {(index % 3 == 0 ? "Open" : "Closed")}",
            C13 = (index * 7 % 97).ToString(CultureInfo.InvariantCulture),
            C14 = (index * 11 % 89).ToString(CultureInfo.InvariantCulture),
            C15 = (index * 13 % 79).ToString(CultureInfo.InvariantCulture),
            C16 = $"Item-{index:D4}"
        };
    }
}
