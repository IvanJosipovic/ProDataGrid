using System;
using System.Linq;
using Avalonia.Headless.XUnit;
using DataGridSample.ViewModels;
using Xunit;

namespace DataGridSample.Tests;

public sealed class GeneratedColumnsViewModelTests
{
    [Fact]
    public void Attribute_view_model_exposes_generated_schema_columns_and_fast_options()
    {
        var viewModel = new GeneratedColumnsAttributesViewModel();

        Assert.Equal(6, viewModel.ColumnDefinitions.Count);
        Assert.Equal(3, viewModel.Items.Count);
        Assert.True(viewModel.FastPathOptions.UseAccessorsOnly);
        Assert.True(viewModel.FastPathOptions.ThrowOnMissingAccessor);
    }

    [Fact]
    public void Assembly_and_custom_implementations_are_available_through_generated_facades()
    {
        var assemblyViewModel = new GeneratedColumnsAssemblyViewModel();
        var customViewModel = new GeneratedColumnsCustomViewModel();

        Assert.Equal(4, assemblyViewModel.ColumnDefinitions.Count);
        Assert.Equal(3, customViewModel.ColumnDefinitions.Count);
        Assert.Equal("custom-label", customViewModel.ColumnDefinitions[1].ColumnKey);
    }

    [AvaloniaFact]
    public void Dynamic_data_view_model_uses_generated_filter_and_search_compilers_upstream()
    {
        using var viewModel = new GeneratedColumnsDynamicDataViewModel();

        Assert.Equal(500, viewModel.Items.Count);

        viewModel.Query = "AVLN";
        Assert.NotEmpty(viewModel.Items);
        Assert.All(viewModel.Items, static trade => Assert.Equal("AVLN", trade.Symbol));

        viewModel.Query = string.Empty;
        viewModel.DeskFilter = "Warsaw";
        Assert.NotEmpty(viewModel.Items);
        Assert.All(viewModel.Items, static trade => Assert.Equal("Warsaw", trade.Desk));
    }

    [AvaloniaFact]
    public void Dynamic_data_sort_and_stream_commands_update_the_bound_collection()
    {
        using var viewModel = new GeneratedColumnsDynamicDataViewModel();
        int initialCount = viewModel.Items.Count;

        viewModel.AddStreamingBatchCommand.Execute().Subscribe();
        Assert.Equal(initialCount + 50, viewModel.Items.Count);

        viewModel.SortPriceDescendingCommand.Execute().Subscribe();
        decimal[] prices = viewModel.Items.Select(static trade => trade.Price).ToArray();
        Assert.Equal(prices.OrderByDescending(static price => price), prices);
    }
}
