using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using DataGridSample.Models;
using DataGridSample.Pages;
using DataGridSample.ViewModels;
using Xunit;

namespace DataGridSample.Tests;

public sealed class ExplicitInterfaceSortingSampleTests
{
    [Fact]
    public void ViewModel_Provides_All_Supported_Interface_Scenarios()
    {
        var viewModel = new ExplicitInterfaceSortingViewModel();

        Assert.Equal(3, viewModel.DirectRows.Count);
        Assert.IsType<ExplicitSortRow>(viewModel.DirectRows[0]);
        Assert.IsType<AlternateExplicitSortRow>(viewModel.DirectRows[1]);
        Assert.Equal(new[] { "Charlie", "Alice", "Bob" }, viewModel.DirectRows.Select(row => row.Name));

        Assert.Equal(new[] { "Charlie", "Alice", "Bob" }, viewModel.InheritedRows.Select(row => row.Name));
        Assert.Equal(new[] { 30, 10, 20 }, viewModel.InheritedRows.Select(row => row.Priority));

        Assert.Equal(4, viewModel.NestedRows.Count);
        Assert.Null(viewModel.NestedRows[1].Detail);
        Assert.Equal(new int?[] { 30, null, 10, 20 }, viewModel.NestedRows.Select(row => row.Detail?.Score));

        Assert.Equal(3, viewModel.PrimaryLabelRows.Count);
        Assert.Equal(3, viewModel.SecondaryLabelRows.Count);
        for (var index = 0; index < viewModel.PrimaryLabelRows.Count; index++)
        {
            Assert.Same(viewModel.PrimaryLabelRows[index], viewModel.SecondaryLabelRows[index]);
        }
        Assert.Equal(new[] { "Charlie", "Alice", "Bob" }, viewModel.PrimaryLabelRows.Select(row => row.Label));
        Assert.Equal(new[] { "Alpha", "Zulu", "Mike" }, viewModel.SecondaryLabelRows.Select(row => row.Label));
    }

    [AvaloniaFact]
    public void Page_Sorts_Direct_Inherited_Nested_And_Ambiguous_Interface_Paths()
    {
        var page = new ExplicitInterfaceSortingPage
        {
            DataContext = new ExplicitInterfaceSortingViewModel()
        };
        var window = CreateHostWindow(page);

        try
        {
            window.Show();
            PumpLayout(window);

            Assert.IsType<ExplicitInterfaceSortingViewModel>(page.DataContext);

            var directGrid = GetGrid(page, "DirectGrid");
            Sort(directGrid, "Name", ListSortDirection.Ascending);
            Assert.Equal(
                new[] { "Alice", "Bob", "Charlie" },
                directGrid.CollectionView.Cast<IExplicitSortRow>().Select(row => row.Name));
            Sort(directGrid, "Time", ListSortDirection.Descending);
            Assert.Equal(
                new[] { "Charlie", "Bob", "Alice" },
                directGrid.CollectionView.Cast<IExplicitSortRow>().Select(row => row.Name));

            var inheritedGrid = GetGrid(page, "InheritedGrid");
            Sort(inheritedGrid, "Name", ListSortDirection.Ascending);
            Assert.Equal(
                new[] { "Alice", "Bob", "Charlie" },
                inheritedGrid.CollectionView.Cast<IInheritedExplicitSortRow>().Select(row => row.Name));
            Sort(inheritedGrid, "Priority", ListSortDirection.Descending);
            Assert.Equal(
                new[] { 30, 20, 10 },
                inheritedGrid.CollectionView.Cast<IInheritedExplicitSortRow>().Select(row => row.Priority));

            var nestedGrid = GetGrid(page, "NestedGrid");
            Sort(nestedGrid, "Detail score", ListSortDirection.Ascending);
            Assert.Equal(
                new int?[] { null, 10, 20, 30 },
                nestedGrid.CollectionView.Cast<INestedExplicitSortRow>().Select(row => row.Detail?.Score));

            var primaryGrid = GetGrid(page, "PrimaryLabelGrid");
            Sort(primaryGrid, "Primary label", ListSortDirection.Ascending);
            Assert.Equal(
                new[] { "Alice", "Bob", "Charlie" },
                primaryGrid.CollectionView.Cast<IPrimaryExplicitLabel>().Select(row => row.Label));

            var secondaryGrid = GetGrid(page, "SecondaryLabelGrid");
            Sort(secondaryGrid, "Secondary label", ListSortDirection.Ascending);
            Assert.Equal(
                new[] { "Alpha", "Mike", "Zulu" },
                secondaryGrid.CollectionView.Cast<ISecondaryExplicitLabel>().Select(row => row.Label));
        }
        finally
        {
            window.Close();
        }
    }

    private static DataGrid GetGrid(Control page, string name)
    {
        return Assert.IsType<DataGrid>(page.FindControl<DataGrid>(name));
    }

    private static void Sort(DataGrid grid, string header, ListSortDirection direction)
    {
        DataGridColumn column = Assert.Single(grid.Columns, column => Equals(column.Header, header));
        foreach (DataGridColumn otherColumn in grid.Columns)
        {
            if (!ReferenceEquals(otherColumn, column))
            {
                otherColumn.SortDirection = null;
            }
        }
        column.SortDirection = direction;
        PumpLayout(grid);
    }

    private static Window CreateHostWindow(Control content)
    {
        var window = new Window
        {
            Width = 1280,
            Height = 900,
            Content = content
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
