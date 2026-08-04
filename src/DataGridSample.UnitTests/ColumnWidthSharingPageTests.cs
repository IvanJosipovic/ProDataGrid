using Avalonia.Controls;
using Avalonia.Controls.DataGridSizing;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using DataGridSample.Pages;
using DataGridSample.ViewModels;
using Xunit;

namespace DataGridSample.Tests;

public sealed class ColumnWidthSharingPageTests
{
    [AvaloniaFact]
    public void Xaml_And_Definition_Columns_Share_Widths()
    {
        var page = new ColumnWidthSharingPage
        {
            DataContext = new ColumnWidthSharingViewModel()
        };
        var window = new Window
        {
            Width = 1100,
            Height = 720,
            Content = page
        };
        window.ApplySampleTheme();

        try
        {
            window.Show();
            PumpLayout(window);

            DataGrid? xamlGrid = page.FindControl<DataGrid>("XamlColumnsGrid");
            DataGrid? definitionGrid = page.FindControl<DataGrid>("DefinitionColumnsGrid");
            Assert.NotNull(xamlGrid);
            Assert.NotNull(definitionGrid);
            Assert.Same(xamlGrid.ColumnWidthSharingScope, definitionGrid.ColumnWidthSharingScope);
            Assert.Equal("owner", DataGridColumnWidthSharing.GetGroup(xamlGrid.Columns[1]));
            Assert.Equal("owner", DataGridColumnWidthSharing.GetGroup(definitionGrid.Columns[1]));
            Assert.Equal(xamlGrid.Columns[1].ActualWidth, definitionGrid.Columns[1].ActualWidth);

            xamlGrid.Columns[1].Width = new DataGridLength(260);
            PumpLayout(window);

            Assert.Equal(260, xamlGrid.Columns[1].ActualWidth);
            Assert.Equal(260, definitionGrid.Columns[1].ActualWidth);
        }
        finally
        {
            window.Close();
        }
    }

    private static void PumpLayout(Control control)
    {
        Dispatcher.UIThread.RunJobs();
        control.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
    }
}
