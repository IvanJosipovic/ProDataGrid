using System.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using DataGridSample.Models;
using DataGridSample.Pages;
using DataGridSample.ViewModels;
using Xunit;

namespace DataGridSample.Tests;

public sealed class HierarchyFeatureContractsPageTests
{
    [AvaloniaFact]
    public void Page_Is_Lazy_And_Demonstrates_Veto_Filtering_And_Retained_Cells()
    {
        var page = new HierarchyFeatureContractsPage();
        Assert.Null(page.DataContext);

        var window = CreateHostWindow(page);
        try
        {
            window.Show();
            PumpLayout(window);

            var viewModel = Assert.IsType<HierarchyFeatureContractsViewModel>(page.DataContext);
            DataGrid grid = Assert.IsType<DataGrid>(page.FindControl<DataGrid>("FeatureContractsGrid"));
            Assert.Equal(2, grid.Columns.Count);
            Assert.All(grid.Columns, static column =>
                Assert.Equal(DataGridColumnDisplayMode.Retained, column.DisplayMode));
            Assert.NotEmpty(viewModel.Events);

            HierarchyFeatureContractsViewModel.TreeItem restricted =
                viewModel.RootItems[0].Children.Single(static item => item.IsRestricted);
            grid.SelectedItem = restricted;
            PumpLayout(window);

            Assert.Null(grid.SelectedItem);
            Assert.Contains(viewModel.Events, static message => message.Contains("VETO"));

            viewModel.BlockRestrictedSelection = false;
            grid.SelectedItem = restricted;
            PumpLayout(window);
            Assert.Same(restricted, grid.SelectedItem);

            HierarchyFeatureContractsViewModel.TreeItem visible =
                viewModel.RootItems[0].Children.Single(static item => item.Name == "Automation");
            grid.SelectedItem = visible;
            viewModel.FilterText = "Automation";
            ((ICommand)viewModel.ApplyFilterCommand).Execute(null);
            PumpLayout(window);
            Assert.Same(visible, grid.SelectedItem);

            viewModel.FilterText = "Accessibility";
            ((ICommand)viewModel.ApplyFilterCommand).Execute(null);
            PumpLayout(window);
            Assert.Null(grid.SelectedItem);

            ((ICommand)viewModel.ClearFilterCommand).Execute(null);
            PumpLayout(window);
            Assert.Null(grid.SelectedItem);

            int committedBefore = viewModel.Events.Count(static message => message.Contains("Committed"));
            ((ICommand)viewModel.ProgrammaticRenameCommand).Execute(null);
            PumpLayout(window);
            Assert.Equal(
                committedBefore,
                viewModel.Events.Count(static message => message.Contains("Committed")));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Feature_Command_Applies_Selection_Veto_Synchronously()
    {
        var viewModel = new HierarchyFeatureContractsViewModel();
        HierarchyFeatureContractsViewModel.TreeItem restricted =
            viewModel.RootItems[0].Children.Single(static item => item.IsRestricted);
        var request = new DataGridFeatureContractEvent(
            DataGridFeatureContractEventKind.SelectionChanging,
            "test proposal",
            new object[] { restricted });

        ((ICommand)viewModel.FeatureEventCommand).Execute(request);

        Assert.True(request.Cancel);
        Assert.Contains("VETO", Assert.Single(viewModel.Events));
    }

    private static Window CreateHostWindow(Control content)
    {
        var window = new Window
        {
            Width = 1024,
            Height = 720,
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
