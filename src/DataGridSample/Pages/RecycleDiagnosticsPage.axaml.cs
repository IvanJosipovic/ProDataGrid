using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using DataGridSample.ViewModels;

namespace DataGridSample.Pages;

public partial class RecycleDiagnosticsPage : UserControl
{
    private static readonly PropertyInfo? DisplayDataProperty =
        typeof(DataGrid).GetProperty("DisplayData", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly PropertyInfo? RowsPresenterSizeProperty =
        typeof(DataGrid).GetProperty("RowsPresenterAvailableSize", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo? RecycledRowsField =
        DisplayDataProperty?.PropertyType.GetField("_recycledRows", BindingFlags.Instance | BindingFlags.NonPublic);

    public RecycleDiagnosticsPage()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) =>
        {
            DataContext ??= new RecycleDiagnosticsViewModel();
            UpdateMetrics();
        };
        LayoutUpdated += (_, _) => UpdateMetrics();
        DiagnosticGrid.LayoutUpdated += (_, _) => UpdateMetrics();
        DiagnosticGrid.AttachedToVisualTree += (_, _) => UpdateMetrics();
    }

    private void UpdateMetrics()
    {
        if (DiagnosticGrid == null || DataContext is not RecycleDiagnosticsViewModel viewModel)
        {
            return;
        }

        viewModel.RealizedRows = DiagnosticGrid
            .GetVisualDescendants()
            .OfType<DataGridRow>()
            .Count();

        viewModel.RecycledRows = GetRecyclePoolCount(DiagnosticGrid);
        viewModel.ViewportHeight = GetViewportHeight(DiagnosticGrid);
    }

    private static int GetRecyclePoolCount(DataGrid grid)
    {
        if (DisplayDataProperty?.GetValue(grid) is not object displayData ||
            RecycledRowsField?.GetValue(displayData) is not System.Collections.ICollection stack)
        {
            return 0;
        }

        return stack.Count;
    }

    private static double GetViewportHeight(DataGrid grid)
    {
        if (RowsPresenterSizeProperty?.GetValue(grid) is Size size)
        {
            return size.Height;
        }

        return grid.Bounds.Height;
    }
}
