using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using DataGridSample.Pages;
using DataGridSample.ViewModels;
using Xunit;

namespace DataGridSample.Tests;

public sealed class ChartingPageInitializationTests
{
    [AvaloniaFact]
    public void DefaultSampleKind_CreatesViewModel_WhenAttached()
    {
        var page = new ChartingPage();
        Assert.Null(page.DataContext);

        var window = CreateHostWindow(page);
        try
        {
            window.Show();
            PumpLayout(window);
        }
        finally
        {
            window.Close();
        }

        var viewModel = Assert.IsType<ChartSampleViewModel>(page.DataContext);
        Assert.Equal(ChartSampleKind.Line, viewModel.Kind);
    }

    [AvaloniaFact]
    public void SampleKindSetBeforeAttach_CreatesViewModelOnAttach_WithRequestedKind()
    {
        var page = new ChartingPage
        {
            SampleKind = ChartSampleKind.Pie
        };

        Assert.Null(page.DataContext);

        var window = CreateHostWindow(page);
        try
        {
            window.Show();
            PumpLayout(window);
        }
        finally
        {
            window.Close();
        }

        var viewModelAfterAttach = Assert.IsType<ChartSampleViewModel>(page.DataContext);
        Assert.Equal(ChartSampleKind.Pie, viewModelAfterAttach.Kind);
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
