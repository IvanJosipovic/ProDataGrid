using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using DataGridSample.Models;
using DataGridSample.ViewModels;

namespace DataGridSample.Pages;

public partial class DynamicColumnsPage : UserControl
{
    private int _unboundSeed = 1;
    private ObservableCollection<DataGridColumn> UnboundColumns => (ObservableCollection<DataGridColumn>)UnboundGrid.Columns;

    public DynamicColumnsPage()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.DynamicColumnsViewModel();

        if (UnboundGrid.Columns is INotifyCollectionChanged incc)
        {
            incc.CollectionChanged += OnUnboundColumnsChanged;
        }

        ResetUnboundColumns();
        UnboundGrid.UpdateColumnDisplayIndexesFromCollectionOrder();
    }

    private void OnAddUnboundColumn(object? sender, RoutedEventArgs e)
    {
        UnboundColumns.Add(CreateDynamicColumn("Dynamic"));
        UnboundGrid.UpdateColumnDisplayIndexesFromCollectionOrder();
    }

    private void OnInsertUnboundColumn(object? sender, RoutedEventArgs e)
    {
        UnboundColumns.Insert(0, CreateDynamicColumn("Inserted"));
        UnboundGrid.UpdateColumnDisplayIndexesFromCollectionOrder();
    }

    private void OnReplaceUnboundColumn(object? sender, RoutedEventArgs e)
    {
        if (UnboundColumns.Count > 0)
        {
            UnboundColumns[0] = CreateDynamicColumn("Replaced");
            UnboundGrid.UpdateColumnDisplayIndexesFromCollectionOrder();
        }
    }

    private void OnMoveUnboundColumn(object? sender, RoutedEventArgs e)
    {
        if (UnboundColumns.Count > 1)
        {
            var lastIndex = UnboundColumns.Count - 1;
            var column = UnboundColumns[lastIndex];
            UnboundColumns.RemoveAt(lastIndex);
            UnboundColumns.Insert(0, column);
            UnboundGrid.UpdateColumnDisplayIndexesFromCollectionOrder();
        }
    }

    private void OnSwapUnboundColumns(object? sender, RoutedEventArgs e)
    {
        if (UnboundColumns.Count > 1)
        {
            var lastIndex = UnboundColumns.Count - 1;
            var first = UnboundColumns[0];
            var last = UnboundColumns[lastIndex];

            UnboundColumns.RemoveAt(lastIndex);
            UnboundColumns.RemoveAt(0);

            UnboundColumns.Insert(0, last);
            UnboundColumns.Insert(UnboundColumns.Count, first);
            UnboundGrid.UpdateColumnDisplayIndexesFromCollectionOrder();
        }
    }

    private void OnRemoveUnboundColumn(object? sender, RoutedEventArgs e)
    {
        if (UnboundColumns.Count > 0)
        {
            UnboundColumns.RemoveAt(UnboundColumns.Count - 1);
            UnboundGrid.UpdateColumnDisplayIndexesFromCollectionOrder();
        }
    }

    private void OnClearUnboundColumns(object? sender, RoutedEventArgs e)
    {
        UnboundColumns.Clear();
    }

    private void OnResetUnboundColumns(object? sender, RoutedEventArgs? e)
    {
        ResetUnboundColumns();
    }

    private void ResetUnboundColumns()
    {
        _unboundSeed = 1;
        UnboundColumns.Clear();
        UnboundColumns.Add(CreateTextColumn("First Name", nameof(Person.FirstName), 1.2));
        UnboundColumns.Add(CreateTextColumn("Last Name", nameof(Person.LastName), 1.2));
        UnboundColumns.Add(CreateTextColumn("Age", nameof(Person.Age), 0.8));
        UnboundGrid.UpdateColumnDisplayIndexesFromCollectionOrder();
    }

    private DataGridTextColumn CreateTextColumn(string header, string path, double star)
    {
        return new DataGridTextColumn
        {
            Header = header,
            Binding = new Binding(path),
            Width = new DataGridLength(star, DataGridLengthUnitType.Star)
        };
    }

    private DataGridColumn CreateDynamicColumn(string headerPrefix)
    {
        var index = _unboundSeed++;
        return new DataGridTextColumn
        {
            Header = $"{headerPrefix} {index}",
            Binding = new Binding(nameof(Person.Status)),
            Width = new DataGridLength(1, DataGridLengthUnitType.Star)
        };
    }

    private void OnUnboundColumnsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (DataContext is not DynamicColumnsViewModel vm)
        {
            return;
        }

        var message = e.Action switch
        {
            NotifyCollectionChangedAction.Add => $"Add: {DynamicColumnsViewModel.Describe(e.NewItems)} at {e.NewStartingIndex}",
            NotifyCollectionChangedAction.Remove => $"Remove: {DynamicColumnsViewModel.Describe(e.OldItems)} from {e.OldStartingIndex}",
            NotifyCollectionChangedAction.Replace => $"Replace: {DynamicColumnsViewModel.Describe(e.OldItems)} with {DynamicColumnsViewModel.Describe(e.NewItems)} at {e.NewStartingIndex}",
            NotifyCollectionChangedAction.Move => $"Move: {DynamicColumnsViewModel.Describe(e.OldItems)} from {e.OldStartingIndex} to {e.NewStartingIndex}",
            NotifyCollectionChangedAction.Reset => "Reset",
            _ => e.Action.ToString()
        };

        vm.LogUnboundEvent(message);
    }
}
