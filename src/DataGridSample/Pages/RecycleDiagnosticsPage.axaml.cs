using System;
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

    private readonly RecycleDiagnosticsViewModel _viewModel;

    public RecycleDiagnosticsPage()
    {
        InitializeComponent();
        _viewModel = (RecycleDiagnosticsViewModel)DataContext!;
        LayoutUpdated += (_, _) => UpdateMetrics();
        DiagnosticGrid.LayoutUpdated += (_, _) => UpdateMetrics();
        DiagnosticGrid.AttachedToVisualTree += (_, _) => UpdateMetrics();
    }

    private void UpdateMetrics()
    {
        if (DiagnosticGrid == null)
        {
            return;
        }

        _viewModel.RealizedRows = DiagnosticGrid
            .GetVisualDescendants()
            .OfType<DataGridRow>()
            .Count();

        _viewModel.RecycledRows = GetRecyclePoolCount(DiagnosticGrid);
        _viewModel.ViewportHeight = GetViewportHeight(DiagnosticGrid);
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
