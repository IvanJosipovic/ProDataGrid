using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DataGridSample.Pages;
using DataGridSample.ViewModels;
using Xunit;

namespace DataGridSample.Tests;

public sealed class FlatVisualLayoutSamplePageTests
{
    [AvaloniaFact]
    public void Flat_Data_Pages_Use_Matched_Workloads_With_Different_Visual_Ownership()
    {
        AssertMatchedFlatDataPage(new FlatSurfaceFlatDataPage(), expectFlat: true);
        AssertMatchedFlatDataPage(new NestedSurfaceFlatDataPage(), expectFlat: false);
    }

    [AvaloniaFact]
    public void Hierarchy_Pages_Use_Matched_Models_With_Different_Visual_Ownership()
    {
        AssertMatchedHierarchyPage(new FlatSurfaceHierarchyPage(), expectFlat: true);
        AssertMatchedHierarchyPage(new NestedSurfaceHierarchyPage(), expectFlat: false);
    }

    private static void AssertMatchedFlatDataPage(Control page, bool expectFlat)
    {
        Window window = CreateHostWindow(page);
        try
        {
            window.Show();
            PumpLayout(window);

            var viewModel = Assert.IsType<OptimizedFlatCellPathsViewModel>(page.DataContext);
            DataGrid grid = Assert.Single(page.GetVisualDescendants().OfType<DataGrid>());
            Assert.Equal(1_000, viewModel.Items.Count);
            Assert.Equal(expectFlat ? DataGridVisualLayoutMode.Flat : DataGridVisualLayoutMode.Nested,
                grid.VisualLayoutMode);
            AssertCellOwnership(grid, expectFlat);
        }
        finally
        {
            window.Close();
        }
    }

    private static void AssertMatchedHierarchyPage(Control page, bool expectFlat)
    {
        Window window = CreateHostWindow(page);
        try
        {
            window.Show();
            PumpLayout(window);

            var viewModel = Assert.IsType<OptimizedHierarchyCellPathsViewModel>(page.DataContext);
            DataGrid grid = Assert.Single(page.GetVisualDescendants().OfType<DataGrid>());
            Assert.True(grid.HierarchicalRowsEnabled);
            Assert.Equal(expectFlat ? DataGridVisualLayoutMode.Flat : DataGridVisualLayoutMode.Nested,
                grid.VisualLayoutMode);
            AssertCellOwnership(grid, expectFlat);

            viewModel.Model.ExpandAll();
            PumpLayout(window);
            Assert.True(viewModel.Model.Flattened.Count > 4);
            AssertCellOwnership(grid, expectFlat);
        }
        finally
        {
            window.Close();
        }
    }

    private static void AssertCellOwnership(DataGrid grid, bool expectFlat)
    {
        DataGridRow row = grid.GetVisualDescendants()
            .OfType<DataGridRow>()
            .First(candidate => candidate.Index >= 0);
        DataGridCell cell = grid.GetVisualDescendants().OfType<DataGridCell>().First();

        if (expectFlat)
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
