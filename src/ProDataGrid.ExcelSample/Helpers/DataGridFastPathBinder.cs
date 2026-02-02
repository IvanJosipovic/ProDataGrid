using Avalonia;
using Avalonia.Controls;

namespace ProDataGrid.ExcelSample.Helpers;

public sealed class DataGridFastPathBinder
{
    private DataGridFastPathBinder()
    {
    }

    public static readonly AttachedProperty<DataGridFastPathOptions?> FastPathOptionsProperty =
        AvaloniaProperty.RegisterAttached<DataGridFastPathBinder, DataGrid, DataGridFastPathOptions?>(
            "FastPathOptions");

    static DataGridFastPathBinder()
    {
        FastPathOptionsProperty.Changed.AddClassHandler<DataGrid>(OnFastPathOptionsChanged);
    }

    public static void SetFastPathOptions(AvaloniaObject element, DataGridFastPathOptions? value)
    {
        element.SetValue(FastPathOptionsProperty, value);
    }

    public static DataGridFastPathOptions? GetFastPathOptions(AvaloniaObject element)
    {
        return element.GetValue(FastPathOptionsProperty);
    }

    private static void OnFastPathOptionsChanged(DataGrid grid, AvaloniaPropertyChangedEventArgs args)
    {
        grid.FastPathOptions = args.NewValue as DataGridFastPathOptions;
    }
}
