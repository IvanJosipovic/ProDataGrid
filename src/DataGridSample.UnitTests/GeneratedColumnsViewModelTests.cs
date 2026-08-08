using System;
using System.Collections;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using DataGridSample.Models;
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

    [Fact]
    public void Reactive_event_command_updates_state_and_returns_routed_event_feedback()
    {
        var viewModel = new GeneratedReactiveEventCommandsViewModel
        {
            CancelPendingEdits = true,
            HandleSortingRequests = true
        };
        var addedItems = new ArrayList { viewModel.Items[1] };
        DataGridGeneratedViewEvent<GeneratedEventCommandRow> selection =
            DataGridGeneratedViewEvent<GeneratedEventCommandRow>.CreateSelectionChanged(
                addedItems,
                new ArrayList(),
                DataGridSelectionChangeSource.Keyboard,
                isUserInitiated: true);

        viewModel.GridEventCommand.Execute(selection).Subscribe();

        Assert.Equal(1, viewModel.EventCount);
        Assert.Equal("SelectionChanged", viewModel.LastEvent);
        Assert.Same(selection, viewModel.LastEventData);
        Assert.Equal("SelectionChanged #1", viewModel.Items[1].LastEvent);

        DataGridGeneratedViewEvent<GeneratedEventCommandRow> sorting =
            DataGridGeneratedViewEvent<GeneratedEventCommandRow>.CreateSorting("symbol");
        viewModel.GridEventCommand.Execute(sorting).Subscribe();
        Assert.True(sorting.Handled);

        DataGridGeneratedViewEvent<GeneratedEventCommandRow> edit =
            DataGridGeneratedViewEvent<GeneratedEventCommandRow>.CreateEdit(
                DataGridGeneratedViewEventKinds.BeginningEdit,
                viewModel.Items[0],
                rowIndex: 0,
                columnKey: "symbol",
                editAction: null,
                cancel: false);
        viewModel.GridEventCommand.Execute(edit).Subscribe();
        Assert.True(edit.Cancel);
    }

    [Fact]
    public void Generated_operations_controller_compiles_search_filter_sort_and_presets()
    {
        using var viewModel = new GeneratedOperationsControllerViewModel();

        Assert.Equal(6, viewModel.Items.Count);
        Assert.Equal(
            DataGridGeneratedFeatures.Columns |
            DataGridGeneratedFeatures.Sorting |
            DataGridGeneratedFeatures.Filtering |
            DataGridGeneratedFeatures.Searching,
            viewModel.Operations.Features);
        Assert.True(viewModel.SortingModel.OwnsViewSorts);
        Assert.True(viewModel.FilteringModel.OwnsViewFilter);

        viewModel.Query = "rxui";
        Assert.True(viewModel.Operations.SearchPredicate(viewModel.Items[1]));
        Assert.False(viewModel.Operations.SearchPredicate(viewModel.Items[0]));

        viewModel.ApplyRiskPresetCommand.Execute().Subscribe();
        Assert.Single(viewModel.SortingModel.Descriptors);
        Assert.Equal(2, viewModel.FilteringModel.Descriptors.Count);
        Assert.True(viewModel.Operations.FilterPredicate(viewModel.Items[0]));
        Assert.False(viewModel.Operations.FilterPredicate(viewModel.Items[2]));
        Assert.True(viewModel.Operations.FilterPredicate(viewModel.Items[4]));
        Assert.True(viewModel.Operations.SortComparer.Compare(viewModel.Items[4], viewModel.Items[0]) < 0);

        int count = viewModel.Items.Count;
        viewModel.AddRowCommand.Execute().Subscribe();
        Assert.Equal(count + 1, viewModel.Items.Count);

        viewModel.ClearOperationsCommand.Execute().Subscribe();
        Assert.Empty(viewModel.SortingModel.Descriptors);
        Assert.Empty(viewModel.FilteringModel.Descriptors);
        Assert.Empty(viewModel.SearchModel.Descriptors);
    }

    [Fact]
    public void Generated_source_list_pipeline_batches_and_applies_compiled_operations_upstream()
    {
        using var viewModel = new GeneratedDynamicDataSourceListViewModel();

        Assert.Equal(24, viewModel.Items.Count);
        Assert.Equal(24, viewModel.PublishedItemCount);
        Assert.Equal(1, viewModel.BatchCount);
        Assert.Equal(0, viewModel.ErrorCount);

        viewModel.Query = "RXUI";
        Assert.NotEmpty(viewModel.Items);
        Assert.All(viewModel.Items, static trade => Assert.Equal("RXUI", trade.Symbol));

        viewModel.Query = string.Empty;
        viewModel.ApplyWarsawFilterCommand.Execute().Subscribe();
        Assert.NotEmpty(viewModel.Items);
        Assert.All(viewModel.Items, static trade =>
        {
            Assert.Equal("Warsaw", trade.Desk);
            Assert.True(trade.Price >= 100m);
        });

        viewModel.SortPriceDescendingCommand.Execute().Subscribe();
        decimal[] prices = viewModel.Items.Select(static trade => trade.Price).ToArray();
        Assert.Equal(prices.OrderByDescending(static price => price), prices);

        viewModel.ClearOperationsCommand.Execute().Subscribe();
        viewModel.AddBatchCommand.Execute().Subscribe();
        Assert.Equal(36, viewModel.Items.Count);
        Assert.Equal(36, viewModel.PublishedItemCount);
        Assert.Equal(2, viewModel.BatchCount);
    }
}
