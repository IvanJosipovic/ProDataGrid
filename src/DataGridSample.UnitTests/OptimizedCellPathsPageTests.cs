using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DataGridSample.Pages;
using DataGridSample.ViewModels;
using Xunit;

namespace DataGridSample.Tests;

public sealed class OptimizedCellPathsPageTests
{
    [AvaloniaFact]
    public void Flat_Page_Switches_All_Realized_Cell_Paths()
    {
        var page = new OptimizedFlatCellPathsPage();
        Assert.Null(page.DataContext);
        var window = CreateHostWindow(page);

        try
        {
            window.Show();
            PumpLayout(window);

            var viewModel = Assert.IsType<OptimizedFlatCellPathsViewModel>(page.DataContext);
            var grid = Assert.IsType<DataGrid>(page.FindControl<DataGrid>("OptimizedFlatCellPathsGrid"));
            Assert.Equal(1_000, viewModel.Items.Count);
            Assert.Equal(6, viewModel.Paths.Count);
            Assert.Equal(7, grid.Columns.Count);
            Assert.IsType<DataGridCell>(GetRealizedCell(grid, 0));
            Assert.DoesNotContain("optimized-cell-path", grid.Classes);

            SelectPath(viewModel, "optimized-theme");
            PumpLayout(window);
            Assert.Contains("optimized-cell-path", grid.Classes);
            Assert.IsType<DataGridCell>(GetRealizedCell(grid, 0));

            SelectPath(viewModel, "direct-accessor");
            PumpLayout(window);
            var directAccessorColumn = Assert.IsType<DataGridTextColumn>(grid.Columns[0]);
            Assert.True(directAccessorColumn.UseDirectTextContent);
            Assert.IsType<DataGridCell>(GetRealizedCell(grid, 0));

            SelectPath(viewModel, "direct-cell");
            PumpLayout(window);
            var directCellColumn = Assert.IsType<DataGridTextColumn>(grid.Columns[0]);
            Assert.True(directCellColumn.UseDirectTextCell);
            Assert.IsType<DataGridDirectTextCell>(GetRealizedCell(grid, 0));

            SelectPath(viewModel, "built-in-drawn");
            PumpLayout(window);
            Assert.Equal(DataGridColumnDisplayMode.Drawn, grid.Columns[0].DisplayMode);
            Assert.IsType<DataGridCustomDrawingCell>(GetRealizedCell(grid, 0));

            SelectPath(viewModel, "custom-drawn");
            PumpLayout(window);
            Assert.IsType<DataGridCustomDrawingColumn>(grid.Columns[0]);
            Assert.IsType<DataGridCustomDrawingCell>(GetRealizedCell(grid, 0));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Hierarchy_Page_Switches_All_Realized_Cell_Paths()
    {
        var page = new OptimizedHierarchyCellPathsPage();
        Assert.Null(page.DataContext);
        var window = CreateHostWindow(page);

        try
        {
            window.Show();
            PumpLayout(window);

            var viewModel = Assert.IsType<OptimizedHierarchyCellPathsViewModel>(page.DataContext);
            var grid = Assert.IsType<DataGrid>(page.FindControl<DataGrid>("OptimizedHierarchyCellPathsGrid"));
            Assert.Equal(6, viewModel.Paths.Count);
            Assert.Equal(6, grid.Columns.Count);
            Assert.IsType<DataGridCell>(GetRealizedCell(grid, 0));
            Assert.DoesNotContain("optimized-cell-path", grid.Classes);

            SelectPath(viewModel, "optimized-theme");
            PumpLayout(window);
            Assert.Contains("optimized-cell-path", grid.Classes);
            Assert.IsType<DataGridCell>(GetRealizedCell(grid, 0));

            SelectPath(viewModel, "optimized-presenter");
            PumpLayout(window);
            var optimizedColumn = Assert.IsType<DataGridHierarchicalColumn>(grid.Columns[0]);
            Assert.True(optimizedColumn.UseOptimizedPresenter);
            Assert.True(optimizedColumn.UseDirectTextContent);
            Assert.IsType<DataGridDirectHierarchicalCell>(GetRealizedCell(grid, 0));

            SelectPath(viewModel, "direct-hierarchy");
            PumpLayout(window);
            var directColumn = Assert.IsType<DataGridHierarchicalColumn>(grid.Columns[0]);
            Assert.True(directColumn.UseDirectCell);
            Assert.IsType<DataGridDirectHierarchicalCell>(GetRealizedCell(grid, 0));
            Assert.IsType<DataGridDirectTextCell>(GetRealizedCell(grid, 1));

            SelectPath(viewModel, "built-in-drawn");
            PumpLayout(window);
            Assert.IsType<DataGridDirectHierarchicalCell>(GetRealizedCell(grid, 0));
            Assert.Equal(DataGridColumnDisplayMode.Drawn, grid.Columns[1].DisplayMode);
            Assert.IsType<DataGridCustomDrawingCell>(GetRealizedCell(grid, 1));

            SelectPath(viewModel, "custom-drawn");
            PumpLayout(window);
            Assert.IsType<DataGridDirectHierarchicalCell>(GetRealizedCell(grid, 0));
            Assert.IsType<DataGridCustomDrawingColumn>(grid.Columns[1]);
            Assert.IsType<DataGridCustomDrawingCell>(GetRealizedCell(grid, 1));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task Workload_Generation_Uses_Configured_Flat_And_Hierarchy_Sizes()
    {
        var flat = new OptimizedFlatCellPathsViewModel
        {
            TargetRowCount = 2_500,
        };
        await flat.LoadRepresentativeWorkloadAsync();
        Assert.Equal(2_500, flat.Items.Count);
        Assert.StartsWith("Loaded ", flat.Summary);

        var hierarchy = new OptimizedHierarchyCellPathsViewModel
        {
            RootCount = 3,
            BranchingFactor = 3,
            Depth = 3,
        };
        await hierarchy.LoadRepresentativeWorkloadAsync();
        Assert.Equal(12, hierarchy.Model.Flattened.Count);

        hierarchy.Model.ExpandAll();
        Assert.Equal(120, hierarchy.Model.Flattened.Count);
    }

    private static void SelectPath(OptimizedFlatCellPathsViewModel viewModel, string key) =>
        viewModel.SelectedPath = viewModel.Paths.Single(path => path.Key == key);

    private static void SelectPath(OptimizedHierarchyCellPathsViewModel viewModel, string key) =>
        viewModel.SelectedPath = viewModel.Paths.Single(path => path.Key == key);

    private static DataGridRow GetFirstRealizedRow(DataGrid grid) =>
        grid.GetVisualDescendants().OfType<DataGridRow>().First(row => row.Index >= 0);

    private static DataGridCell GetRealizedCell(DataGrid grid, int columnIndex) =>
        GetFirstRealizedRow(grid)
            .GetVisualDescendants()
            .OfType<DataGridCell>()
            .OrderBy(cell => cell.Bounds.Left)
            .ElementAt(columnIndex);

    private static Window CreateHostWindow(Control content)
    {
        var window = new Window
        {
            Width = 1_200,
            Height = 760,
            Content = content,
        };
        window.ApplySampleTheme();
        return window;
    }

    private static void PumpLayout(Control control)
    {
        Dispatcher.UIThread.RunJobs();
        control.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
    }
}
