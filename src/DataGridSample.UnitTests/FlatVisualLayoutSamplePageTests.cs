using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DataGridSample.Models;
using DataGridSample.Pages;
using DataGridSample.ViewModels;
using Xunit;

namespace DataGridSample.Tests;

public sealed class FlatVisualLayoutSamplePageTests
{
    [AvaloniaFact]
    public void Flat_Data_Pages_Use_Matched_Workloads_With_All_Visual_Ownership_Modes()
    {
        AssertMatchedFlatDataPage(new VirtualSurfaceFlatDataPage(), DataGridVisualLayoutMode.Virtualized);
        AssertMatchedFlatDataPage(new FlatSurfaceFlatDataPage(), DataGridVisualLayoutMode.Flat);
        AssertMatchedFlatDataPage(new NestedSurfaceFlatDataPage(), DataGridVisualLayoutMode.Nested);
    }

    [AvaloniaFact]
    public void Hierarchy_Pages_Use_Matched_Models_With_All_Visual_Ownership_Modes()
    {
        AssertMatchedHierarchyPage(new VirtualSurfaceHierarchyPage(), DataGridVisualLayoutMode.Virtualized);
        AssertMatchedHierarchyPage(new FlatSurfaceHierarchyPage(), DataGridVisualLayoutMode.Flat);
        AssertMatchedHierarchyPage(new NestedSurfaceHierarchyPage(), DataGridVisualLayoutMode.Nested);
    }

    [AvaloniaFact]
    public void Virtual_Flat_Data_Page_Covers_All_Supported_Renderer_Modes()
    {
        var page = new VirtualSurfaceFlatDataPage();
        Window window = CreateHostWindow(page);
        try
        {
            window.Show();
            PumpLayout(window);

            var viewModel = Assert.IsType<OptimizedFlatCellPathsViewModel>(page.DataContext);
            DataGrid grid = Assert.Single(page.GetVisualDescendants().OfType<DataGrid>());
            foreach (VirtualSurfaceModeOption mode in viewModel.VirtualModes)
            {
                viewModel.SelectedVirtualMode = mode;
                PumpLayout(window);
                Assert.Equal(DataGridVisualLayoutMode.Virtualized, grid.VisualLayoutMode);
                AssertCellOwnership(
                    grid,
                    DataGridVisualLayoutMode.Virtualized,
                    mode.ExpectsRowlessSurface,
                    mode.Key);
            }
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Virtual_Hierarchy_Page_Covers_All_Supported_Renderer_Modes()
    {
        var page = new VirtualSurfaceHierarchyPage();
        Window window = CreateHostWindow(page);
        try
        {
            window.Show();
            PumpLayout(window);

            var viewModel = Assert.IsType<OptimizedHierarchyCellPathsViewModel>(page.DataContext);
            DataGrid grid = Assert.Single(page.GetVisualDescendants().OfType<DataGrid>());
            viewModel.Model.ExpandAll();
            PumpLayout(window);

            foreach (VirtualSurfaceModeOption mode in viewModel.VirtualModes)
            {
                viewModel.SelectedVirtualMode = mode;
                PumpLayout(window);
                Assert.Equal(DataGridVisualLayoutMode.Virtualized, grid.VisualLayoutMode);
                AssertCellOwnership(
                    grid,
                    DataGridVisualLayoutMode.Virtualized,
                    mode.ExpectsRowlessSurface,
                    mode.Key);
            }
        }
        finally
        {
            window.Close();
        }
    }

    private static void AssertMatchedFlatDataPage(Control page, DataGridVisualLayoutMode expectedMode)
    {
        Window window = CreateHostWindow(page);
        try
        {
            window.Show();
            PumpLayout(window);

            var viewModel = Assert.IsType<OptimizedFlatCellPathsViewModel>(page.DataContext);
            DataGrid grid = Assert.Single(page.GetVisualDescendants().OfType<DataGrid>());
            Assert.Equal(1_000, viewModel.Items.Count);
            Assert.Equal(expectedMode, grid.VisualLayoutMode);
            AssertCellOwnership(grid, expectedMode, expectedMode == DataGridVisualLayoutMode.Virtualized);
        }
        finally
        {
            window.Close();
        }
    }

    private static void AssertMatchedHierarchyPage(Control page, DataGridVisualLayoutMode expectedMode)
    {
        Window window = CreateHostWindow(page);
        try
        {
            window.Show();
            PumpLayout(window);

            var viewModel = Assert.IsType<OptimizedHierarchyCellPathsViewModel>(page.DataContext);
            DataGrid grid = Assert.Single(page.GetVisualDescendants().OfType<DataGrid>());
            Assert.True(grid.HierarchicalRowsEnabled);
            Assert.Equal(expectedMode, grid.VisualLayoutMode);
            AssertCellOwnership(grid, expectedMode, expectedMode == DataGridVisualLayoutMode.Virtualized);

            viewModel.Model.ExpandAll();
            PumpLayout(window);
            Assert.True(viewModel.Model.Flattened.Count > 4);
            AssertCellOwnership(grid, expectedMode, expectedMode == DataGridVisualLayoutMode.Virtualized);
        }
        finally
        {
            window.Close();
        }
    }

    private static void AssertCellOwnership(
        DataGrid grid,
        DataGridVisualLayoutMode mode,
        bool expectsRowlessSurface,
        string? context = null)
    {
        if (expectsRowlessSurface)
        {
            Assert.Empty(grid.GetVisualDescendants().OfType<DataGridRow>());
            Assert.Empty(grid.GetVisualDescendants().OfType<DataGridCell>());
            int surfaceCount = grid.GetVisualDescendants().Count(
                candidate => candidate.GetType().Name == "DataGridVirtualCellSurface");
            string columnSummary = string.Join(
                ", ",
                grid.Columns.Select(column =>
                    $"{column.GetType().Name}(accessor={DataGridColumnMetadata.GetValueAccessor(column)?.GetType().Name ?? "none"}, width={column.Width.UnitType})"));
            Assert.True(
                surfaceCount == 1,
                $"Expected one virtual surface for '{context}', found {surfaceCount}. " +
                $"Definitions: {grid.ColumnDefinitionsSource?.Count ?? -1}. Columns: {columnSummary}.");
            return;
        }

        DataGridRow row = grid.GetVisualDescendants()
            .OfType<DataGridRow>()
            .First(candidate => candidate.Index >= 0);
        DataGridCell cell = grid.GetVisualDescendants().OfType<DataGridCell>().First();

        if (mode != DataGridVisualLayoutMode.Nested)
        {
            Assert.Same(row.GetVisualParent(), cell.GetVisualParent());
        }
        else
        {
            Assert.NotSame(row.GetVisualParent(), cell.GetVisualParent());
            Assert.Contains(
                grid.GetVisualDescendants().OfType<DataGridRow>(),
                candidate => candidate.GetVisualDescendants().Any(descendant => ReferenceEquals(descendant, cell)));
        }
    }

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
