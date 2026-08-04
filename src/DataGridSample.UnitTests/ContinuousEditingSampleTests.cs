using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using DataGridSample.Models;
using DataGridSample.Pages;
using DataGridSample.ViewModels;
using Xunit;

namespace DataGridSample.Tests;

public sealed class ContinuousEditingSampleTests
{
    [Fact]
    public void ViewModel_Provides_Seed_Data_And_Editor_Choices()
    {
        var viewModel = new ContinuousEditingViewModel();

        Assert.Equal(3, viewModel.Rows.Count);
        Assert.Contains("Morgan", viewModel.OwnerSuggestions);
        Assert.Contains("Critical", viewModel.Priorities);
    }

    [Fact]
    public void Row_Raises_Notifications_For_Editable_Properties()
    {
        var row = new ContinuousEditingRow();
        string? changedProperty = null;
        row.PropertyChanged += (_, args) => changedProperty = args.PropertyName;

        row.Estimate = 8;

        Assert.Equal(nameof(ContinuousEditingRow.Estimate), changedProperty);
        Assert.Equal(8, row.Estimate);
    }

    [AvaloniaFact]
    public void Page_Configures_Continuous_Next_Cell_Editing()
    {
        var page = new ContinuousEditingPage();
        var grid = page.FindControl<DataGrid>("ContinuousEditingGrid");
        var viewModel = Assert.IsType<ContinuousEditingViewModel>(page.DataContext);

        Assert.NotNull(grid);
        Assert.Same(viewModel.Rows, grid.ItemsSource);
        Assert.Equal(DataGridEnterKeyNavigationMode.NextCell, grid.EnterKeyNavigationMode);
        Assert.True(grid.ContinueEditingOnEnter);
        Assert.True(grid.CanUserAddRows);
        Assert.Collection(
            grid.Columns,
            column => Assert.IsType<DataGridTextColumn>(column),
            column => Assert.IsType<DataGridAutoCompleteColumn>(column),
            column => Assert.IsType<DataGridComboBoxColumn>(column),
            column => Assert.IsType<DataGridNumericColumn>(column),
            column => Assert.IsType<DataGridDatePickerColumn>(column));
    }
}
