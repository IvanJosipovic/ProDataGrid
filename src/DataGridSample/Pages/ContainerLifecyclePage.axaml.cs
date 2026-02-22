using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DataGridSample.Pages;

public partial class ContainerLifecyclePage : UserControl, INotifyPropertyChanged
{
    private readonly Random _random = new();
    public new event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<SampleItem> Items { get; } = new();
    public ObservableCollection<string> Logs { get; } = new();

    public string LogSummary => $"Prepared: {_prepared}, Cleared: {_cleared}, Cleaned: {_cleaned}";

    private int _prepared;
    private int _cleared;
    private int _cleaned;

    public ContainerLifecyclePage()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) =>
        {
            DataContext ??= this;
            if (Items.Count == 0)
            {
                SeedItems(120);
            }
        };
    }

    private void SeedItems(int count)
    {
        for (var i = 0; i < count; i++)
        {
            Items.Add(new SampleItem
            {
                Id = i,
                Name = $"Person {i}",
                City = Cities[i % Cities.Length]
            });
        }
    }

    private void OnAddItem(object? sender, RoutedEventArgs e)
    {
        var id = Items.Count == 0 ? 0 : Items.Max(x => x.Id) + 1;
        Items.Insert(0, new SampleItem
        {
            Id = id,
            Name = $"Inserted {id}",
            City = Cities[_random.Next(Cities.Length)]
        });
    }

    private void OnRemoveFirst(object? sender, RoutedEventArgs e)
    {
        if (Items.Count > 0)
        {
            Items.RemoveAt(0);
        }
    }

    private void OnScrollBottom(object? sender, RoutedEventArgs e)
    {
        if (Items.Count > 0)
        {
            LifecycleGrid.ScrollIntoView(Items[^1], null);
            LifecycleGrid.UpdateLayout();
        }
    }

    private void OnScrollTop(object? sender, RoutedEventArgs e)
    {
        if (Items.Count > 0)
        {
            LifecycleGrid.ScrollIntoView(Items[0], null);
            LifecycleGrid.UpdateLayout();
        }
    }

    private void OnContainerEvent(object? sender, ContainerEventArgs e)
    {
        switch (e.Kind)
        {
            case ContainerEventKind.Prepared:
                _prepared++;
                break;
            case ContainerEventKind.Cleared:
                _cleared++;
                break;
            case ContainerEventKind.Cleaned:
                _cleaned++;
                break;
        }

        Logs.Insert(0, $"{DateTime.Now:HH:mm:ss} [{e.Kind}] {e.Item}");
        if (Logs.Count > 100)
            Logs.RemoveAt(Logs.Count - 1);

        OnPropertyChanged(nameof(LogSummary));
    }

    private static readonly string[] Cities = new[]
    {
        "London", "Oslo", "Helsinki", "Warsaw", "Madrid",
        "Lisbon", "Prague", "Vienna", "Zurich", "Berlin",
        "Budapest", "Rome", "Copenhagen", "Dublin", "Paris"
    };
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public record SampleItem
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
}

public sealed class TrackingDataGrid : DataGrid
{
    public event EventHandler<ContainerEventArgs>? ContainerEvent;
    protected override Type StyleKeyOverride => typeof(DataGrid);

    protected override void PrepareContainerForItemOverride(DataGridRow element, object item)
    {
        base.PrepareContainerForItemOverride(element, item);
        ContainerEvent?.Invoke(this, new ContainerEventArgs(ContainerEventKind.Prepared, item));
    }

    protected override void ClearContainerForItemOverride(DataGridRow element, object item)
    {
        ContainerEvent?.Invoke(this, new ContainerEventArgs(ContainerEventKind.Cleared, item));
        base.ClearContainerForItemOverride(element, item);
    }

    protected override void OnCleanUpVirtualizedItem(DataGridRow element)
    {
        if (element.DataContext is { } item)
            ContainerEvent?.Invoke(this, new ContainerEventArgs(ContainerEventKind.Cleaned, item));
        base.OnCleanUpVirtualizedItem(element);
    }
}

public enum ContainerEventKind
{
    Prepared,
    Cleared,
    Cleaned
}

public sealed class ContainerEventArgs : EventArgs
{
    public ContainerEventArgs(ContainerEventKind kind, object item)
    {
        Kind = kind;
        Item = item;
    }

    public ContainerEventKind Kind { get; }
    public object Item { get; }
}
