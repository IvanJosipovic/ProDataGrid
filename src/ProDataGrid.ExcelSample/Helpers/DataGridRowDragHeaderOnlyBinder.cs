using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace ProDataGrid.ExcelSample.Helpers;

public sealed class DataGridRowDragHeaderOnlyBinder
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<DataGridRowDragHeaderOnlyBinder, DataGrid, bool>(
            "IsEnabled");

    private static readonly ConditionalWeakTable<DataGrid, DragSubscription> Subscriptions = new();

    static DataGridRowDragHeaderOnlyBinder()
    {
        IsEnabledProperty.Changed.AddClassHandler<DataGrid>(OnIsEnabledChanged);
    }

    public static void SetIsEnabled(AvaloniaObject element, bool value)
    {
        element.SetValue(IsEnabledProperty, value);
    }

    public static bool GetIsEnabled(AvaloniaObject element)
    {
        return element.GetValue(IsEnabledProperty);
    }

    private static void OnIsEnabledChanged(DataGrid grid, AvaloniaPropertyChangedEventArgs args)
    {
        var subscription = Subscriptions.GetOrCreateValue(grid);
        if (args.NewValue is bool isEnabled && isEnabled)
        {
            subscription.Attach(grid);
        }
        else
        {
            subscription.Detach();
        }
    }

    private sealed class DragSubscription
    {
        private DataGrid? _grid;
        private bool _lastPressOnHeader;

        public void Attach(DataGrid grid)
        {
            _grid = grid;
            _lastPressOnHeader = false;

            grid.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
            grid.AddHandler(InputElement.PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel, handledEventsToo: true);
            grid.AddHandler(InputElement.PointerCaptureLostEvent, OnPointerCaptureLost, RoutingStrategies.Tunnel, handledEventsToo: true);
            grid.AddHandler(DataGrid.RowDragStartingEvent, OnRowDragStarting, RoutingStrategies.Bubble, handledEventsToo: true);
        }

        public void Detach()
        {
            if (_grid != null)
            {
                _grid.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
                _grid.RemoveHandler(InputElement.PointerReleasedEvent, OnPointerReleased);
                _grid.RemoveHandler(InputElement.PointerCaptureLostEvent, OnPointerCaptureLost);
                _grid.RemoveHandler(DataGrid.RowDragStartingEvent, OnRowDragStarting);
            }

            _grid = null;
            _lastPressOnHeader = false;
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (_grid == null)
            {
                return;
            }

            var point = e.GetCurrentPoint(_grid);
            if (e.Pointer.Type == PointerType.Mouse && !point.Properties.IsLeftButtonPressed)
            {
                _lastPressOnHeader = false;
                return;
            }

            _lastPressOnHeader = IsRowHeaderVisual(e.Source as Visual);
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            _lastPressOnHeader = false;
        }

        private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
        {
            _lastPressOnHeader = false;
        }

        private void OnRowDragStarting(object? sender, DataGridRowDragStartingEventArgs e)
        {
            if (!_lastPressOnHeader)
            {
                e.Cancel = true;
            }
        }

        private static bool IsRowHeaderVisual(Visual? visual)
        {
            if (visual == null)
            {
                return false;
            }

            foreach (var ancestor in visual.GetSelfAndVisualAncestors())
            {
                if (ancestor is DataGridRowHeader)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
