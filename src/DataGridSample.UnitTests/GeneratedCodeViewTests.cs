using System;
using System.IO;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
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
    public void Generated_reactive_view_state_projections_bind_state_message_and_retry_command()
    {
        var viewModel = new GeneratedReactiveViewStatesViewModel();
        var view = new GeneratedReactiveViewStatesPage(viewModel);
        var window = new Window { Width = 900, Height = 560, Content = view };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        try
        {
            DataGrid grid = view.GetLogicalDescendants().OfType<DataGrid>().Single();
            Control loading = view.GetLogicalDescendants().OfType<Control>().Single(control => control.Name == "GeneratedLoadingState");
            Control empty = view.GetLogicalDescendants().OfType<Control>().Single(control => control.Name == "GeneratedEmptyState");
            Control error = view.GetLogicalDescendants().OfType<Control>().Single(control => control.Name == "GeneratedErrorState");
            TextBlock errorMessage = view.GetLogicalDescendants().OfType<TextBlock>().Single(control => control.Name == "GeneratedErrorMessage");
            Button retry = view.GetLogicalDescendants().OfType<Button>().Single(control => control.Name == "GeneratedRetryButton");

            Assert.False(grid.IsVisible);
            Assert.False(loading.IsVisible);
            Assert.False(empty.IsVisible);
            Assert.True(error.IsVisible);
            Assert.Equal(viewModel.ErrorMessage, errorMessage.Text);
            Assert.Same(viewModel.RetryCommand, retry.Command);

            viewModel.ErrorMessage = null;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("Generated trades could not be loaded.", errorMessage.Text);

            viewModel.ViewState = DataGridGeneratedViewState.Loading;
            Dispatcher.UIThread.RunJobs();
            Assert.True(loading.IsVisible);
            Assert.False(error.IsVisible);

            viewModel.ViewState = DataGridGeneratedViewState.Empty;
            Dispatcher.UIThread.RunJobs();
            Assert.True(empty.IsVisible);
            Assert.False(loading.IsVisible);

            retry.Command!.Execute(null);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(DataGridGeneratedViewState.Content, viewModel.ViewState);
            Assert.True(grid.IsVisible);
            Assert.False(empty.IsVisible);
            Assert.Equal(3, viewModel.Items.Count);
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

    [AvaloniaFact]
    public void Explorer_recipe_exposes_automation_and_named_slots_and_can_capture_populated_view()
    {
        var viewModel = new GeneratedColumnsAttributesViewModel();
        var view = new GeneratedColumnsCodeView(viewModel);
        var window = new Window
        {
            Width = 1000,
            Height = 640,
            Content = view
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        try
        {
            DataGrid grid = view.GetLogicalDescendants().OfType<DataGrid>().Single();
            ContentControl toolbar = view.GetLogicalDescendants().OfType<ContentControl>()
                .Single(control => control.Name == "GeneratedToolbarSlot");
            ContentControl explorer = view.GetLogicalDescendants().OfType<ContentControl>()
                .Single(control => control.Name == "GeneratedExplorerSlot");

            Assert.Equal("generated-columns-code-grid", AutomationProperties.GetAutomationId(grid));
            Assert.Equal("generated-columns-code-grid-toolbar", AutomationProperties.GetAutomationId(toolbar));
            Assert.Equal("generated-columns-code-grid-recipe", AutomationProperties.GetAutomationId(explorer));
            Assert.Equal(3, grid.ItemsSource!.Cast<object>().Count());

            string? screenshotDirectory = Environment.GetEnvironmentVariable("AVALONIA_SCREENSHOT_DIR");
            if (!string.IsNullOrWhiteSpace(screenshotDirectory))
            {
                using var frame = window.CaptureRenderedFrame();
                Assert.NotNull(frame);
                Directory.CreateDirectory(screenshotDirectory);
                string path = Path.GetFullPath(Path.Combine(screenshotDirectory, "generated-explorer-recipe.png"));
                using (FileStream stream = File.Create(path))
                {
                    frame.Save(stream, new Avalonia.Media.Imaging.PngBitmapEncoderOptions());
                }
                Assert.True(new FileInfo(path).Length > 0);
            }
        }
        finally
        {
            window.Close();
        }
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
