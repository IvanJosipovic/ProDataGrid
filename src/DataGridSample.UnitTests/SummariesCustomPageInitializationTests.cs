using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DataGridSample.ViewModels;
using Xunit;

namespace DataGridSample.Tests;

public sealed class SummariesCustomPageInitializationTests
{
    [AvaloniaFact]
    public void SummariesCustomPage_AddsCustomSummaries_WhenAttached()
    {
        var page = new global::DataGridSample.Pages.SummariesCustomPage();
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

        var viewModel = Assert.IsType<SummariesCustomViewModel>(page.DataContext);
        Assert.NotNull(viewModel);

        var dataGrid = page.GetVisualDescendants()
            .OfType<DataGrid>()
            .First(control => control.Name == "CustomDataGrid");

        Assert.Equal(1, GetCustomSummaryCount(dataGrid, "Quantity", "StdDev: "));
        Assert.Equal(1, GetCustomSummaryCount(dataGrid, "Unit Price", "Wtd Avg: "));
        Assert.Equal(1, GetCustomSummaryCount(dataGrid, "Total", "% Visible: "));
    }

    [AvaloniaFact]
    public void SummariesCustomPage_DoesNotDuplicateCustomSummaries_WhenDataContextChanges()
    {
        var page = new global::DataGridSample.Pages.SummariesCustomPage();

        var window = CreateHostWindow(page);
        try
        {
            window.Show();
            PumpLayout(window);

            var initialGrid = page.GetVisualDescendants()
                .OfType<DataGrid>()
                .First(control => control.Name == "CustomDataGrid");

            Assert.Equal(1, GetCustomSummaryCount(initialGrid, "Quantity", "StdDev: "));
            Assert.Equal(1, GetCustomSummaryCount(initialGrid, "Unit Price", "Wtd Avg: "));
            Assert.Equal(1, GetCustomSummaryCount(initialGrid, "Total", "% Visible: "));

            page.DataContext = new SummariesCustomViewModel();
            PumpLayout(window);
        }
        finally
        {
            window.Close();
        }

        var dataGrid = page.GetVisualDescendants()
            .OfType<DataGrid>()
            .First(control => control.Name == "CustomDataGrid");

        Assert.Equal(1, GetCustomSummaryCount(dataGrid, "Quantity", "StdDev: "));
        Assert.Equal(1, GetCustomSummaryCount(dataGrid, "Unit Price", "Wtd Avg: "));
        Assert.Equal(1, GetCustomSummaryCount(dataGrid, "Total", "% Visible: "));
    }

    private static int GetCustomSummaryCount(DataGrid dataGrid, string header, string summaryTitle)
    {
        var column = dataGrid.Columns.First(control => control.Header?.ToString() == header);
        return column.Summaries
            .OfType<DataGridCustomSummaryDescription>()
            .Count(summary => summary.Title == summaryTitle);
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
