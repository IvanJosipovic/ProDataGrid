using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using DataGridSample.Pages;
using DataGridSample.ViewModels;
using Xunit;

namespace DataGridSample.Tests;

public sealed class SelectionFastIndexSamplePageTests
{
    [AvaloniaFact]
    public void Interface_Page_Selects_Target_Item_By_Command()
    {
        var page = new SelectionFastIndexInterfacePage();
        var window = CreateHostWindow(page);

        try
        {
            window.Show();
            PumpLayout(window);

            var viewModel = Assert.IsType<SelectionFastIndexInterfaceViewModel>(page.DataContext);
            viewModel.SelectNearEndCommand.Execute(null);
            PumpLayout(window);

            Assert.True(viewModel.SelectionModel.SelectedIndex >= 0);
            Assert.Contains("IDataGridIndexOf", viewModel.Summary);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Cache_Page_Uses_Default_Fast_Path()
    {
        var page = new SelectionFastIndexCachePage();
        var window = CreateHostWindow(page);

        try
        {
            window.Show();
            PumpLayout(window);

            var viewModel = Assert.IsType<SelectionFastIndexCacheViewModel>(page.DataContext);
            viewModel.SelectNearEndCommand.Execute(null);
            PumpLayout(window);

            Assert.True(viewModel.SelectionModel.SelectedIndex >= 0);
            Assert.Contains("default cache path", viewModel.Summary);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Resolver_Page_Binds_Custom_Resolver_And_Selects_Target()
    {
        var page = new SelectionFastIndexResolverPage();
        var window = CreateHostWindow(page);

        try
        {
            window.Show();
            PumpLayout(window);

            var grid = page.FindControl<DataGrid>("ResolverDataGrid");
            Assert.NotNull(grid);
            Assert.NotNull(grid!.ReferenceIndexResolver);

            var viewModel = Assert.IsType<SelectionFastIndexResolverViewModel>(page.DataContext);
            viewModel.SelectNearEndCommand.Execute(null);
            PumpLayout(window);

            Assert.True(viewModel.SelectionModel.SelectedIndex >= 0);
            Assert.Contains("custom resolver", viewModel.Summary);
        }
        finally
        {
            window.Close();
        }
    }

    private static Window CreateHostWindow(Control content)
    {
        var window = new Window
        {
            Width = 1024,
            Height = 720,
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
