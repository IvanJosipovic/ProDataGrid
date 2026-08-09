using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using DataGridSample.Models;

namespace DataGridSample.Behaviors;

public sealed class DataGridFeatureContractBehavior : AvaloniaObject
{
    public static readonly AttachedProperty<ICommand?> EventCommandProperty =
        AvaloniaProperty.RegisterAttached<DataGridFeatureContractBehavior, DataGrid, ICommand?>(
            "EventCommand");

    private static readonly AttachedProperty<EventSubscription?> SubscriptionProperty =
        AvaloniaProperty.RegisterAttached<DataGridFeatureContractBehavior, DataGrid, EventSubscription?>(
            "Subscription");

    static DataGridFeatureContractBehavior()
    {
        EventCommandProperty.Changed.AddClassHandler<DataGrid>(OnEventCommandChanged);
    }

    private DataGridFeatureContractBehavior()
    {
    }

    public static ICommand? GetEventCommand(DataGrid grid) =>
        grid.GetValue(EventCommandProperty);

    public static void SetEventCommand(DataGrid grid, ICommand? value) =>
        grid.SetValue(EventCommandProperty, value);

    private static void OnEventCommandChanged(DataGrid grid, AvaloniaPropertyChangedEventArgs e)
    {
        grid.GetValue(SubscriptionProperty)?.Dispose();
        grid.ClearValue(SubscriptionProperty);

        if (e.NewValue is ICommand command)
        {
            grid.SetValue(SubscriptionProperty, new EventSubscription(grid, command));
        }
    }

    private sealed class EventSubscription : IDisposable
    {
        private DataGrid? _grid;
        private readonly ICommand _command;

        public EventSubscription(DataGrid grid, ICommand command)
        {
            _grid = grid;
            _command = command;
            grid.CellPrepared += OnCellPrepared;
            grid.CellClearing += OnCellClearing;
            grid.CellValueChanged += OnCellValueChanged;
            grid.SelectionChanging += OnSelectionChanging;
        }

        public void Dispose()
        {
            if (_grid is not { } grid)
            {
                return;
            }

            grid.CellPrepared -= OnCellPrepared;
            grid.CellClearing -= OnCellClearing;
            grid.CellValueChanged -= OnCellValueChanged;
            grid.SelectionChanging -= OnSelectionChanging;
            _grid = null;
        }

        private void OnCellPrepared(object? sender, DataGridCellLifecycleEventArgs e)
        {
            Execute(new DataGridFeatureContractEvent(
                DataGridFeatureContractEventKind.CellPrepared,
                $"Prepared row={e.Row.Index} column={DescribeColumn(e.Column)} item={Describe(e.Item)} path={e.HierarchyPath.Count}"));
        }

        private void OnCellClearing(object? sender, DataGridCellLifecycleEventArgs e)
        {
            Execute(new DataGridFeatureContractEvent(
                DataGridFeatureContractEventKind.CellClearing,
                $"Clearing row={e.Row.Index} column={DescribeColumn(e.Column)} item={Describe(e.Item)} path={e.HierarchyPath.Count}"));
        }

        private void OnCellValueChanged(object? sender, DataGridCellValueChangedEventArgs e)
        {
            Execute(new DataGridFeatureContractEvent(
                DataGridFeatureContractEventKind.CellValueChanged,
                $"Committed column={DescribeColumn(e.Column)} item={Describe(e.Item)} old={Describe(e.OldValue)} new={Describe(e.NewValue)} origin={e.Origin}"));
        }

        private void OnSelectionChanging(object? sender, DataGridSelectionChangingEventArgs e)
        {
            var request = new DataGridFeatureContractEvent(
                DataGridFeatureContractEventKind.SelectionChanging,
                $"Selection {e.Guarantee} source={e.Source} add={DescribeItems(e.AddedItems)} remove={DescribeItems(e.RemovedItems)}",
                e.AddedItems,
                e.RemovedItems);
            Execute(request);
            e.Cancel = request.Cancel;
        }

        private void Execute(DataGridFeatureContractEvent message)
        {
            if (_command.CanExecute(message))
            {
                _command.Execute(message);
            }
        }

        private static string DescribeColumn(DataGridColumn? column) =>
            Convert.ToString(column?.Header, CultureInfo.InvariantCulture) ?? "(none)";

        private static string Describe(object? value) =>
            Convert.ToString(value, CultureInfo.InvariantCulture) ?? "(null)";

        private static string DescribeItems(IReadOnlyList<object> items) =>
            items.Count == 0
                ? "[]"
                : $"[{string.Join(", ", items.Select(Describe))}]";
    }
}
