using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using DataGridSample.Pages;
using DataGridSample.ViewModels;
using ReactiveUI.Avalonia;
using Xunit;

namespace DataGridSample.Tests;

public sealed class GeneratedCodeViewTests
{
    [AvaloniaFact]
    public void Avalonia_strategy_binds_generated_members_and_uses_custom_base()
    {
        var viewModel = new GeneratedColumnsAttributesViewModel();
        var view = new GeneratedColumnsCodeView(viewModel);
        var window = new Window { Content = view };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        try
        {
            DataGrid grid = view.GetLogicalDescendants().OfType<DataGrid>().Single();
            Assert.IsAssignableFrom<GeneratedGridViewBase>(view);
            Assert.Same(viewModel.Items, grid.ItemsSource);
            Assert.Same(viewModel.ColumnDefinitions, grid.ColumnDefinitionsSource);
            Assert.Same(viewModel.FastPathOptions, grid.FastPathOptions);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Reactive_ui_strategy_binds_models_and_reactive_source_generated_property()
    {
        using var viewModel = new GeneratedColumnsDynamicDataViewModel();
        var view = new GeneratedReactiveDataGridView(viewModel);
        var window = new Window { Content = view };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        try
        {
            DataGrid grid = view.GetLogicalDescendants().OfType<DataGrid>().Single();
            TextBox searchBox = view.GetLogicalDescendants().OfType<TextBox>().Single();
            Assert.IsAssignableFrom<ReactiveUserControl<GeneratedColumnsDynamicDataViewModel>>(view);
            Assert.Same(viewModel.SortingModel, grid.SortingModel);
            Assert.Same(viewModel.FilteringModel, grid.FilteringModel);
            Assert.Same(viewModel.SearchModel, grid.SearchModel);

            searchBox.Text = "AVLN";
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("AVLN", viewModel.Query);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Generated_view_remains_fully_customizable_by_subclassing_hooks()
    {
        var view = new CustomizedGeneratedView();
        DataGrid grid = view.GetLogicalDescendants().OfType<DataGrid>().Single();

        Assert.False(grid.CanUserSortColumns);
        Assert.Equal("customized", grid.Tag);
    }

    private sealed class CustomizedGeneratedView : GeneratedColumnsCodeView
    {
        protected override void ConfigureGeneratedDataGrid(DataGrid dataGrid)
        {
            dataGrid.CanUserSortColumns = false;
            dataGrid.Tag = "customized";
        }
    }
}
