using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.DataGridConditionalFormatting;
using Avalonia.Controls.DataGridFiltering;
using Avalonia.Controls.DataGridPivoting;
using Avalonia.Controls.DataGridSorting;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using DataGridSample.Generated;
using DataGridSample.Models;
using DataGridSample.Models.SourceGenerationPolicy;
using DataGridSample.Pages;
using DataGridSample.SourceGenerationPolicy.ViewModels;
using DataGridSample.SourceGenerationPolicy.Views;
using DataGridSample.ViewModels;
using ProCharts;
using ProDataGrid.Charting;
using Xunit;

namespace DataGridSample.Tests;

public sealed class GeneratedColumnsViewModelTests
{
    [Fact]
    public void Generated_feature_schema_carries_complete_column_layout_defaults()
    {
        DataGridColumnDefinitionList definitions = GeneratedFeatureRowSchema.Instance.CreateColumnDefinitions();

        Assert.Equal(1, definitions.FrozenColumnCount);
        Assert.Equal(1, definitions.FrozenColumnCountRight);
        Assert.Equal("id", definitions[0].ColumnKey);
        Assert.Equal("timestamp", definitions[^1].ColumnKey);
        Assert.Equal(1, definitions.Single(static column => Equals(column.ColumnKey, "symbol")).DisplayIndex);
    }

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

    [Fact]
    public void Assembly_namespace_policy_applies_defaults_explicit_overrides_exclusions_and_registry_mappings()
    {
        var viewModel = new GeneratedAssemblyNamespacePolicyPageViewModel();

        Assert.Equal(4, viewModel.NamespacePolicy.ColumnDefinitions.Count);
        Assert.True(viewModel.NamespacePolicy.FastPathOptions.StrictMode);
        Assert.False(viewModel.NamespacePolicy.FastPathOptions.HighPerformanceSearchTrackItemChanges);
        Assert.Equal(
            DataGridGeneratedPerformanceProfile.HighFrequencyStreaming,
            NamespacePolicyRowDataGridSchema.CreatePerformanceOptions().Profile);

        Assert.Equal(3, viewModel.ExplicitPolicy.ColumnDefinitions.Count);
        Assert.False(viewModel.ExplicitPolicy.FastPathOptions.StrictMode);
        Assert.True(viewModel.ExplicitPolicy.FastPathOptions.HighPerformanceSearchTrackItemChanges);
        Assert.Equal(2, ExplicitPolicyRowSchema.StateVersion);
        Assert.Equal("sample/explicit-policy-row/v2", ExplicitPolicyRowSchema.SchemaId);
        Assert.Equal(
            DataGridGeneratedPerformanceProfile.Spreadsheet,
            ExplicitPolicyRowSchema.CreatePerformanceOptions().Profile);
        Assert.DoesNotContain(
            viewModel.ExplicitPolicy.ColumnDefinitions,
            static column => Equals(column.ColumnKey, nameof(ExplicitPolicyRow.ExcludedByAttributedOnlyDiscovery)));

        Assert.NotNull(viewModel.NamespaceRegistrySchema);
        Assert.Equal(NamespacePolicyRowDataGridSchema.SchemaId, viewModel.NamespaceRegistrySchema.Manifest.SchemaId);
        Assert.Same(ExplicitPolicyRowSchema.Instance, viewModel.ExplicitRegistrySchema);
        Assert.True(viewModel.IsNestedNamespaceExcluded);

        Assert.Equal(0, NamespacePolicyRowsView.GeneratedRecipe);
        Assert.Equal(DataGridGeneratedPerformanceProfile.HighFrequencyStreaming, NamespacePolicyRowsView.GeneratedPerformanceProfile);
        Assert.Equal(4, ExplicitPolicyRowsView.GeneratedRecipe);
        Assert.Equal(DataGridGeneratedPerformanceProfile.Spreadsheet, ExplicitPolicyRowsView.GeneratedPerformanceProfile);
    }

    [Fact]
    public async Task Generated_header_filters_use_typed_metadata_local_remote_values_and_cached_commands()
    {
        using var viewModel = new GeneratedHeaderFiltersViewModel();

        Assert.Equal(7, viewModel.ColumnDefinitions.Count);
        Assert.True(viewModel.FastPathOptions.StrictMode);
        Assert.Equal(["Warsaw", "London", "New York", "Singapore"], viewModel.LocalDeskValues);
        Assert.Same(
            viewModel.DeskHeaderCommands,
            viewModel.HeaderCommandController.ForField("desk"));

        Assert.True(GeneratedHeaderFilterRowSchema.TryGetField("symbol", out DataGridGeneratedField symbol));
        Assert.True(GeneratedHeaderFilterRowSchema.TryGetField("desk", out DataGridGeneratedField desk));
        Assert.True(GeneratedHeaderFilterRowSchema.TryGetField("side", out DataGridGeneratedField side));
        Assert.True(GeneratedHeaderFilterRowSchema.TryGetField("price", out DataGridGeneratedField price));
        Assert.True(GeneratedHeaderFilterRowSchema.TryGetField("timestamp", out DataGridGeneratedField timestamp));
        Assert.True(GeneratedHeaderFilterRowSchema.TryGetField("active", out DataGridGeneratedField active));
        Assert.Equal(DataGridGeneratedFilterEditorKind.Text, symbol.Metadata.FilterEditor);
        Assert.Equal(DataGridGeneratedFilterEditorKind.Distinct, desk.Metadata.FilterEditor);
        Assert.Equal(DataGridGeneratedFilterEditorKind.Enum, side.Metadata.FilterEditor);
        Assert.Equal(DataGridGeneratedFilterEditorKind.Range, price.Metadata.FilterEditor);
        Assert.Equal(DataGridGeneratedFilterEditorKind.DateTime, timestamp.Metadata.FilterEditor);
        Assert.Equal(DataGridGeneratedFilterEditorKind.Boolean, active.Metadata.FilterEditor);

        await viewModel.ApplyPriceRangeCommand.Execute().ToTask();
        FilteringDescriptor range = Assert.Single(viewModel.FilteringModel.Descriptors);
        Assert.Equal(FilteringOperator.Between, range.Operator);
        Assert.Equal("price", range.ColumnId);
        Assert.True(viewModel.PriceHeaderCommands.ClearFilter.CanExecute(null));

        viewModel.PriceHeaderCommands.SortDescending.Execute(null);
        SortingDescriptor sorting = Assert.Single(viewModel.SortingModel.Descriptors);
        Assert.Equal("price", sorting.ColumnId);
        Assert.Equal(System.ComponentModel.ListSortDirection.Descending, sorting.Direction);

        viewModel.SideHeaderCommands.HideColumn.Execute(null);
        Assert.False(viewModel.ColumnLayoutController.IsVisible("side"));
        Assert.True(viewModel.SideHeaderCommands.ShowColumn.CanExecute(null));
        viewModel.SideHeaderCommands.ShowColumn.Execute(null);
        Assert.True(viewModel.ColumnLayoutController.IsVisible("side"));

        viewModel.RemoteQuery = "o";
        await viewModel.LoadRemoteDeskValuesAsync();
        Assert.Equal(["London", "New York", "Singapore"], viewModel.RemoteDeskValues);
        Assert.Equal(1, viewModel.RemoteRevision);

        viewModel.RemoteQuery = "on";
        await viewModel.RunStaleRemoteRequestAsync();
        Assert.False(viewModel.LastSlowRequestAccepted);
        Assert.Equal(["London"], viewModel.RemoteDeskValues);
        Assert.Equal(3, viewModel.RemoteRevision);
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
        Assert.Single(viewModel.OperationsPresets);
        Assert.Equal("Warsaw high value", viewModel.OperationsPresets[0].Name);

        viewModel.Query = "rxui";
        Assert.True(viewModel.Operations.SearchPredicate(viewModel.Items[1]));
        Assert.False(viewModel.Operations.SearchPredicate(viewModel.Items[0]));

        viewModel.ApplyRiskPresetCommand.Execute().Subscribe();
        Assert.Single(viewModel.SortingModel.Descriptors);
        Assert.Equal(2, viewModel.FilteringModel.Descriptors.Count);
        Assert.Equal(3, viewModel.OperationsDescriptors.Count);
        Assert.Same(viewModel.Operations.Commands, viewModel.OperationsCommands);
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

    [AvaloniaFact]
    public void Generated_source_cache_pipeline_preserves_selection_across_keyed_replacement_and_move()
    {
        using var viewModel = new GeneratedDynamicDataSourceCacheViewModel();

        Assert.Equal(18, viewModel.Items.Count);
        Assert.Equal(18, viewModel.CacheItemCount);
        Assert.Equal(1, viewModel.BatchCount);
        Assert.Equal(0, viewModel.ErrorCount);

        GeneratedTrade original = viewModel.Items.Single(static trade => trade.Id == 8);
        viewModel.RunReplacementScenarioCommand.Execute().Subscribe();
        Dispatcher.UIThread.RunJobs();

        GeneratedTrade replacement = viewModel.Items.Single(static trade => trade.Id == 8);
        Assert.NotSame(original, replacement);
        Assert.Equal(999m, replacement.Price);
        Assert.Equal(1, viewModel.ReplacementCount);
        Assert.Equal(8, viewModel.SelectedKey);
        Assert.Equal(999m, viewModel.SelectedPrice);
        Assert.Same(replacement, viewModel.SelectionModel.SelectedItem);
        Assert.Equal(8, viewModel.Items[0].Id);
        Assert.Contains("Selection preserved stable key 8", viewModel.Status, StringComparison.Ordinal);

        viewModel.Query = "AOT";
        Assert.NotEmpty(viewModel.Items);
        Assert.All(viewModel.Items, static trade => Assert.Equal("AOT", trade.Symbol));

        viewModel.ClearOperationsCommand.Execute().Subscribe();
        viewModel.ApplyLondonFilterCommand.Execute().Subscribe();
        Assert.NotEmpty(viewModel.Items);
        Assert.All(viewModel.Items, static trade =>
        {
            Assert.Equal("London", trade.Desk);
            Assert.True(trade.Price >= 70m);
        });

        viewModel.ClearOperationsCommand.Execute().Subscribe();
        viewModel.AddBatchCommand.Execute().Subscribe();
        Assert.Equal(24, viewModel.Items.Count);
        Assert.Equal(24, viewModel.CacheItemCount);
        Assert.Equal(2, viewModel.BatchCount);
    }

    [AvaloniaFact]
    public void Generated_source_cache_pipeline_disposes_idempotently()
    {
        var viewModel = new GeneratedDynamicDataSourceCacheViewModel();

        viewModel.Dispose();
        viewModel.Dispose();

        Assert.True(viewModel.IsDisposed);
        Assert.Empty(viewModel.SelectionModel.Source.Cast<object>());
    }

    [AvaloniaFact]
    public void Generated_hierarchical_dynamic_data_pipeline_preserves_expansion_and_applies_root_operations()
    {
        using var viewModel = new GeneratedHierarchicalDynamicDataViewModel();

        Assert.Equal(4, viewModel.SourceRootCount);
        Assert.Equal(4, viewModel.VisibleRootCount);
        Assert.Equal(20, viewModel.NodeCount);
        Assert.Equal(20, viewModel.VisibleNodeCount);
        Assert.Equal(0, viewModel.ErrorCount);

        viewModel.CollapseAllCommand.Execute().Subscribe();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(4, viewModel.VisibleNodeCount);
        Assert.All(viewModel.Items, static root => Assert.False(root.IsExpanded));

        viewModel.ExpandAllCommand.Execute().Subscribe();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(20, viewModel.VisibleNodeCount);
        Assert.All(viewModel.Items, static root => Assert.True(root.IsExpanded));

        viewModel.AddChildCommand.Execute().Subscribe();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(21, viewModel.NodeCount);
        Assert.Equal(21, viewModel.VisibleNodeCount);

        GeneratedHierarchyNode[] originalRoots = viewModel.Items.ToArray();
        viewModel.RefreshRootsCommand.Execute().Subscribe();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(4, viewModel.ReplacementCount);
        Assert.Equal(21, viewModel.NodeCount);
        Assert.Equal(21, viewModel.VisibleNodeCount);
        Assert.All(viewModel.Items, replacement =>
            Assert.DoesNotContain(originalRoots, original => ReferenceEquals(original, replacement)));
        Assert.All(viewModel.Items, static root => Assert.True(root.IsExpanded));

        GeneratedHierarchyNode rootOne = viewModel.Items.Single(static root => root.Id == 1);
        GeneratedHierarchyNode rootSix = viewModel.Items.Single(static root => root.Id == 6);
        viewModel.Query = "Warsaw";
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0, viewModel.ErrorCount);
        Assert.Equal(2, viewModel.SearchModel.Descriptors.Count);
        Assert.True(viewModel.TreeRoots.SearchPredicate(rootSix));
        Assert.False(viewModel.TreeRoots.SearchPredicate(rootOne));
        GeneratedHierarchyNode searchedRoot = Assert.Single(viewModel.Items);
        Assert.Equal(6, searchedRoot.Id);

        viewModel.ClearOperationsCommand.Execute().Subscribe();
        viewModel.SortPriceDescendingCommand.Execute().Subscribe();
        Assert.Equal(new[] { 16, 11, 6, 1 }, viewModel.Items.Select(static root => root.Id));

        viewModel.ApplyWarsawFilterCommand.Execute().Subscribe();
        GeneratedHierarchyNode filteredRoot = Assert.Single(viewModel.Items);
        Assert.Equal(6, filteredRoot.Id);
        Assert.Equal("Warsaw", filteredRoot.Desk);
        Assert.True(filteredRoot.Price >= 80m);

        viewModel.ClearOperationsCommand.Execute().Subscribe();
        viewModel.AddRootBatchCommand.Execute().Subscribe();
        Assert.Equal(6, viewModel.SourceRootCount);
        Assert.Equal(6, viewModel.VisibleRootCount);
        Assert.Equal(31, viewModel.NodeCount);
    }

    [AvaloniaFact]
    public void Generated_hierarchical_dynamic_data_pipeline_disposes_idempotently()
    {
        var viewModel = new GeneratedHierarchicalDynamicDataViewModel();

        viewModel.Dispose();
        viewModel.Dispose();
        Dispatcher.UIThread.RunJobs();

        Assert.True(viewModel.IsDisposed);
        Assert.Empty(viewModel.HierarchicalModel.ObservableFlattened);
    }

    [AvaloniaFact]
    public async Task Generated_remote_query_pipeline_pages_caches_filters_and_suppresses_stale_responses()
    {
        using var viewModel = new GeneratedRemoteQueryViewModel();
        await viewModel.Initialization;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(DataGridGeneratedViewState.Content, viewModel.ViewState);
        Assert.Equal(8, viewModel.Items.Count);
        Assert.Equal(64, viewModel.TotalCount);
        Assert.Equal(0, viewModel.PageIndex);
        Assert.Equal(2, viewModel.RequestCount);
        Assert.Equal(Enumerable.Range(57, 8).Reverse(), viewModel.Items.Select(static item => item.Id));
        Assert.Contains("gross_total", viewModel.TranslatedField, StringComparison.Ordinal);

        await viewModel.NextPageCommand.Execute().ToTask();
        Assert.Equal(1, viewModel.PageIndex);
        Assert.Equal(3, viewModel.RequestCount);
        Assert.Equal(Enumerable.Range(49, 8).Reverse(), viewModel.Items.Select(static item => item.Id));

        await viewModel.PreviousPageCommand.Execute().ToTask();
        Assert.Equal(0, viewModel.PageIndex);
        Assert.Equal(3, viewModel.RequestCount);

        viewModel.Query = "Contoso";
        await viewModel.LoadFirstPageCommand.Execute().ToTask();
        Assert.Equal(3, viewModel.SearchModel.Descriptors.Count);
        Assert.Equal(11, viewModel.TotalCount);
        Assert.All(viewModel.Items, static item => Assert.StartsWith("Contoso", item.Customer, StringComparison.Ordinal));

        await viewModel.ClearQueryCommand.Execute().ToTask();
        await viewModel.ApplyEuropeFilterCommand.Execute().ToTask();
        Assert.Equal(2, viewModel.FilteringModel.Descriptors.Count);
        Assert.NotEmpty(viewModel.Items);
        Assert.All(viewModel.Items, static item =>
        {
            Assert.Equal("Europe", item.Region);
            Assert.True(item.Total >= 250m);
        });

        await viewModel.SortTotalDescendingCommand.Execute().ToTask();
        decimal[] totals = viewModel.Items.Select(static item => item.Total).ToArray();
        Assert.Equal(totals.OrderByDescending(static total => total), totals);

        await viewModel.RunStaleScenarioCommand.Execute().ToTask();
        Dispatcher.UIThread.RunJobs();
        Assert.True(viewModel.StaleResponseCount >= 1);
        Assert.True(viewModel.CancellationCount >= 1);
        Assert.Contains("Suppressed stale revision", viewModel.Status, StringComparison.Ordinal);
        Assert.Equal(DataGridGeneratedViewState.Content, viewModel.ViewState);

        await viewModel.SimulateErrorCommand.Execute().ToTask();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(DataGridGeneratedViewState.Error, viewModel.ViewState);
        Assert.NotNull(viewModel.ErrorMessage);
        Assert.True(viewModel.ErrorCount >= 1);

        await viewModel.RetryCommand.Execute().ToTask();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(DataGridGeneratedViewState.Content, viewModel.ViewState);
        Assert.Null(viewModel.ErrorMessage);
    }

    [AvaloniaFact]
    public async Task Generated_remote_query_pipeline_disposes_idempotently()
    {
        var viewModel = new GeneratedRemoteQueryViewModel();
        await viewModel.Initialization;

        viewModel.Dispose();
        viewModel.Dispose();

        Assert.True(viewModel.IsDisposed);
        Assert.Throws<InvalidOperationException>(() => _ = viewModel.OrdersRemoteQuery);
    }

    [AvaloniaFact]
    public async Task Generated_selection_controller_preserves_keys_across_pages_and_replacements()
    {
        using var viewModel = new GeneratedSelectionStateViewModel();

        Assert.Equal(8, viewModel.Items.Count);
        Assert.Equal(2, GeneratedFeatureRowSchema.StateVersion);
        Assert.Equal("symbol", GeneratedFeatureRowSchema.CreateStateDescriptor().ColumnAliases["ticker"]);

        await viewModel.SelectStableKeysCommand.Execute().ToTask();
        Assert.Equal(new[] { 1, 4 }, viewModel.SelectionController.SelectedItemKeys);
        Assert.Equal(2, viewModel.LoadedSelectedCount);
        Assert.Equal(2, viewModel.SelectionModel.SelectedItems.Count);

        await viewModel.NextPageCommand.Execute().ToTask();
        Assert.Equal(2, viewModel.PageNumber);
        Assert.Equal(new[] { 1, 4 }, viewModel.SelectionController.SelectedItemKeys);
        Assert.Equal(0, viewModel.LoadedSelectedCount);
        Assert.Empty(viewModel.SelectionModel.SelectedItems);

        await viewModel.FirstPageCommand.Execute().ToTask();
        Assert.Equal(2, viewModel.LoadedSelectedCount);
        Assert.Equal(2, viewModel.SelectionModel.SelectedItems.Count);

        await viewModel.ReplaceAndReorderCommand.Execute().ToTask();
        Assert.Equal(new[] { 1, 4 }, viewModel.SelectionController.SelectedItemKeys);
        Assert.All(
            viewModel.SelectionController.GetSelectedItems(),
            static row => Assert.EndsWith("*", row.Symbol, StringComparison.Ordinal));
    }

    [AvaloniaFact]
    public void Generated_grouped_shared_selection_uses_typed_groups_and_preserves_stable_keys()
    {
        using var viewModel = new GeneratedGroupedSharedSelectionViewModel();

        Assert.Single(GeneratedFeatureRowSchema.GroupFields);
        Assert.Single(viewModel.GroupedItems.GroupDescriptions);
        Assert.IsNotType<DataGridPathGroupDescription>(viewModel.GroupedItems.GroupDescriptions[0]);
        Assert.Same(viewModel.GroupedItems, viewModel.SelectionModel.Source);

        viewModel.SelectAcrossGroupsCommand.Execute().Subscribe();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(new[] { 2, 7, 11 }, viewModel.SelectionController.SelectedItemKeys);
        Assert.Equal(3, viewModel.SelectionModel.SelectedItems.Count);

        viewModel.ReverseSourceCommand.Execute().Subscribe();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(new[] { 2, 7, 11 }, viewModel.SelectionController.SelectedItemKeys);
        Assert.Equal(
            new[] { 2, 7, 11 },
            viewModel.SelectionModel.SelectedItems.Cast<GeneratedFeatureRow>()
                .Select(static item => item.Id)
                .OrderBy(static id => id));
        Assert.Contains("restored by generated stable keys", viewModel.Status, StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public void Generated_selection_state_view_model_disposes_idempotently()
    {
        var viewModel = new GeneratedSelectionStateViewModel();

        viewModel.Dispose();
        viewModel.Dispose();

        Assert.True(viewModel.IsDisposed);
        Assert.Empty(viewModel.SelectionModel.SelectedItems);
        Assert.Throws<InvalidOperationException>(() => _ = viewModel.StatefulRows);
    }

    [Fact]
    public void Generated_grouping_summaries_update_incrementally_for_add_replace_remove_and_reset()
    {
        var viewModel = new GeneratedGroupingSummariesViewModel();

        Assert.Equal(2, GeneratedGroupedOrderSchema.GroupFields.Count);
        Assert.Equal(2, viewModel.Items.SortDescriptions.Count);
        Assert.Equal(5, viewModel.GeneratedSummaries.Count);
        Assert.Equal(12, viewModel.OrderCount);
        Assert.Equal(6, viewModel.UniqueCustomerCount);
        Assert.Equal(82, viewModel.TotalQuantity);
        Assert.Equal(11, viewModel.GroupCount);
        Assert.Equal("East", Assert.IsType<GeneratedGroupedOrder>(viewModel.Items[0]).Region);
        Assert.Equal(
            new[] { "East", "North", "South", "West" },
            viewModel.Items.Groups!
                .Cast<DataGridCollectionViewGroup>()
                .Select(static group => Assert.IsType<string>(group.Key)));
        Assert.All(
            viewModel.Items.Groups!.Cast<DataGridCollectionViewGroup>(),
            static regionGroup => Assert.Equal(
                regionGroup.Items
                    .Cast<DataGridCollectionViewGroup>()
                    .Select(static group => Assert.IsType<string>(group.Key))
                    .Order(StringComparer.Ordinal),
                regionGroup.Items
                    .Cast<DataGridCollectionViewGroup>()
                    .Select(static group => Assert.IsType<string>(group.Key))));
        decimal initialRevenue = viewModel.TotalRevenue;

        viewModel.AddBatchCommand.Execute().Subscribe();
        Assert.Equal(15, viewModel.OrderCount);
        Assert.Equal(110, viewModel.TotalQuantity);
        Assert.True(viewModel.TotalRevenue > initialRevenue);
        Assert.Contains("Incremental Add", viewModel.Status, StringComparison.Ordinal);
        Assert.Equal(
            new[] { "East", "North", "South", "West" },
            viewModel.Items.Groups!
                .Cast<DataGridCollectionViewGroup>()
                .Select(static group => Assert.IsType<string>(group.Key)));

        viewModel.ReplaceOrderCommand.Execute().Subscribe();
        Assert.Equal(15, viewModel.OrderCount);
        Assert.Equal(7, viewModel.UniqueCustomerCount);
        Assert.Equal(117, viewModel.TotalQuantity);
        Assert.Contains("Incremental Replace", viewModel.Status, StringComparison.Ordinal);

        viewModel.RemoveOrderCommand.Execute().Subscribe();
        Assert.Equal(14, viewModel.OrderCount);
        Assert.Equal(112, viewModel.TotalQuantity);
        Assert.Contains("Incremental Remove", viewModel.Status, StringComparison.Ordinal);

        viewModel.ResetCommand.Execute().Subscribe();
        Assert.Equal(12, viewModel.OrderCount);
        Assert.Equal(6, viewModel.UniqueCustomerCount);
        Assert.Equal(82, viewModel.TotalQuantity);
        Assert.Equal(initialRevenue, viewModel.TotalRevenue);
        Assert.Contains("Reset fallback", viewModel.Status, StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public void Generated_indexed_spreadsheet_view_model_materializes_runtime_slot_families()
    {
        using var viewModel = new GeneratedIndexedSpreadsheetViewModel();

        Assert.Equal(10, viewModel.VisibleColumnCount);
        Assert.Equal(10, viewModel.ColumnDefinitions.Count);
        Assert.Equal(24, viewModel.Items.Count);
        Assert.True(viewModel.FastPathOptions.StrictMode);
        Assert.IsType<DataGridTextColumnDefinition>(viewModel.ColumnDefinitions[0]);
        Assert.IsType<DataGridNumericColumnDefinition>(viewModel.ColumnDefinitions[1]);
        Assert.IsType<DataGridFormulaColumnDefinition>(viewModel.ColumnDefinitions[4]);
        DataGridFormulaColumnDefinition cellFormula = Assert.IsType<DataGridFormulaColumnDefinition>(viewModel.ColumnDefinitions[7]);
        Assert.True(cellFormula.AllowCellFormulas);

        GeneratedSpreadsheetRow first = viewModel.Items[0];
        DataGridNumericColumnDefinition quantity = Assert.IsType<DataGridNumericColumnDefinition>(viewModel.ColumnDefinitions[1]);
        Assert.Equal("B", quantity.SortMemberPath);
        Assert.Equal(4d, first.GetCell(1));

        viewModel.AddColumnCommand.Execute().Subscribe();
        Assert.Equal(11, viewModel.VisibleColumnCount);
        Assert.Equal("K", viewModel.ColumnDefinitions[^1].ColumnKey);
        viewModel.AddColumnCommand.Execute().Subscribe();
        Assert.Equal(12, viewModel.ColumnDefinitions.Count);
        Assert.IsType<DataGridFormulaColumnDefinition>(viewModel.ColumnDefinitions[^1]);

        viewModel.RemoveColumnCommand.Execute().Subscribe();
        Assert.Equal(11, viewModel.VisibleColumnCount);
        Assert.Contains("runtime family", viewModel.Status, StringComparison.Ordinal);
    }

    [Fact]
    public void Generated_indexed_row_uses_stable_spreadsheet_notification_names()
    {
        var row = new GeneratedSpreadsheetRow(1, GeneratedIndexedSpreadsheetViewModel.MaximumColumnCount);
        string? changedProperty = null;
        row.PropertyChanged += (_, args) => changedProperty = args.PropertyName;

        row.SetCell(10, 42d);

        Assert.Equal("K", changedProperty);
        Assert.Equal("AA", GeneratedSpreadsheetRow.GetCellPropertyName(26));
    }

    [Fact]
    public void Generated_indexed_spreadsheet_view_model_disposes_formula_model_idempotently()
    {
        var viewModel = new GeneratedIndexedSpreadsheetViewModel();

        viewModel.Dispose();
        viewModel.Dispose();

        Assert.True(viewModel.IsDisposed);
    }

    [AvaloniaFact]
    public void Generated_conditional_formatting_view_model_uses_typed_rules_and_runtime_model_factory()
    {
        var viewModel = new GeneratedConditionalFormattingViewModel();

        Assert.Equal(9, GeneratedConditionalFormattingRowSchema.ConditionalRules.Count);
        Assert.Equal(9, viewModel.ConditionalFormatting.Descriptors.Count);
        Assert.Equal(2, viewModel.ConditionalFormatting.Descriptors.Count(
            static descriptor => descriptor.Target == ConditionalFormattingTarget.Row));
        IDataGridGeneratedConditionalRule belowTarget = Assert.Single(
            GeneratedConditionalFormattingRowSchema.ConditionalRules,
            static rule => rule.RuleId == "score-below-target");
        Assert.True(belowTarget.IsMatch(viewModel.Items[0]));
        IDataGridGeneratedConditionalRule regionMatch = Assert.Single(
            GeneratedConditionalFormattingRowSchema.ConditionalRules,
            static rule => rule.RuleId == "region-north");
        Assert.True(regionMatch.IsMatch(viewModel.Items[0]));
        IDataGridGeneratedConditionalRule targetRange = Assert.Single(
            GeneratedConditionalFormattingRowSchema.ConditionalRules,
            static rule => rule.RuleId == "target-core-range");
        Assert.True(targetRange.IsMatch(viewModel.Items[1]));
        Assert.True(viewModel.BelowTargetCount > 0);
        Assert.True(viewModel.AtRiskCount > 0);

        viewModel.ToggleRulesCommand.Execute().Subscribe();
        Assert.False(viewModel.RulesEnabled);
        Assert.Empty(viewModel.ConditionalFormatting.Descriptors);

        viewModel.ToggleRulesCommand.Execute().Subscribe();
        Assert.True(viewModel.RulesEnabled);
        Assert.Equal(9, viewModel.ConditionalFormatting.Descriptors.Count);

        viewModel.RandomizeCommand.Execute().Subscribe();
        Assert.Equal(16, viewModel.Items.Count);
        Assert.Contains("generated predicates", viewModel.Status, StringComparison.Ordinal);
    }

    [Fact]
    public void Generated_pivot_chart_view_model_uses_ordered_typed_metadata_and_reactive_projections()
    {
        using var viewModel = new GeneratedPivotChartViewModel();

        Assert.Equal(12, viewModel.SourceRowCount);
        Assert.Equal(2, GeneratedPivotChartRowSchema.AnalyticsFields.Count(
            static field => (field.Role & DataGridGeneratedAnalyticsRole.PivotValue) != 0));
        Assert.All(
            GeneratedPivotChartRowSchema.AnalyticsFields.Where(
                static field => (field.Role & DataGridGeneratedAnalyticsRole.ChartValue) != 0),
            static field => Assert.NotNull(
                Assert.IsAssignableFrom<IDataGridGeneratedNumericAnalyticsField>(field).NumericValueSelector));
        Assert.Single(viewModel.Pivot.RowFields);
        Assert.Single(viewModel.Pivot.ColumnFields);
        Assert.Single(viewModel.Pivot.FilterFields);
        Assert.Equal(new[] { "Revenue", "Profit" }, viewModel.Pivot.ValueFields.Select(static field => field.Header));
        Assert.All(viewModel.Pivot.RowFields, static field => Assert.Null(field.PropertyPath));
        Assert.All(viewModel.Pivot.ValueFields, static field => Assert.Null(field.PropertyPath));
        Assert.Equal(2, viewModel.DirectChartSource.Series.Count);
        Assert.All(viewModel.DirectChartSource.Series, static series => Assert.Null(series.ValuePath));
        Assert.Equal(8, viewModel.RangeChartProjection.Range.RowCount);
        Assert.Equal(8, viewModel.DirectChartModel.Request.WindowCount);
        Assert.Equal(ChartDownsampleMode.None, viewModel.DirectChartModel.Request.DownsampleMode);
        Assert.NotEmpty(viewModel.DirectChartModel.Snapshot.Categories);
        Assert.Equal(2, viewModel.DirectChartModel.Snapshot.Series.Count);
        Assert.NotEmpty(viewModel.PivotChartModel.Snapshot.Series);
        Assert.Equal(new[] { "P1", "P2", "P3", "P4" }, viewModel.LongFormChartModel.Snapshot.Categories);
        Assert.Equal(6, viewModel.LongFormChartModel.Snapshot.Series.Count);

        GeneratedPivotChartRow selected = Assert.IsType<GeneratedPivotChartRow>(viewModel.Items[3]);
        viewModel.SelectionController.SelectOnlyKey(selected.Id);
        Assert.Equal(3, viewModel.DirectChartModel.Interaction.CrosshairCategoryIndex);
        viewModel.DirectChartModel.Interaction.SetCrosshair(5, null, null, 0.5d, 0.5d);
        GeneratedPivotChartRow chartSelected = Assert.IsType<GeneratedPivotChartRow>(viewModel.Items[5]);
        Assert.Equal(new[] { chartSelected.Id }, viewModel.SelectionController.SelectedItemKeys);

        viewModel.AddPeriodCommand.Execute().Subscribe();
        Assert.Equal(15, viewModel.SourceRowCount);
        Assert.Equal(7, viewModel.RangeChartProjection.Range.StartRow);
        Assert.Contains("Added P5", viewModel.Status, StringComparison.Ordinal);
        Assert.Contains("P5", viewModel.DirectChartModel.Snapshot.Categories);
        Assert.Contains("P5", viewModel.LongFormChartModel.Snapshot.Categories);

        viewModel.ToggleMetricCommand.Execute().Subscribe();
        Assert.Equal("Profit", viewModel.SelectedMetric);
        Assert.Same(viewModel.Pivot.ValueFields[1], viewModel.PivotChart.ValueField);

        viewModel.ToggleSeriesSourceCommand.Execute().Subscribe();
        Assert.Equal(PivotChartSeriesSource.Rows, viewModel.PivotChart.SeriesSource);

        viewModel.RemovePeriodCommand.Execute().Subscribe();
        Assert.Equal(12, viewModel.SourceRowCount);
        viewModel.RestoreCommand.Execute().Subscribe();
        Assert.Equal("Revenue", viewModel.SelectedMetric);
        Assert.Equal(12, viewModel.SourceRowCount);
    }

    [Fact]
    public async Task Generated_outline_and_drag_drop_use_typed_fields_stable_keys_and_domain_handler()
    {
        using var viewModel = new GeneratedOutlineDragDropViewModel();

        Assert.Equal(6, viewModel.Items.Count);
        Assert.Equal(2, viewModel.Outline.GroupFields.Count);
        Assert.Equal(2, viewModel.Outline.ValueFields.Count);
        Assert.Equal(new[] { "region", "team" }, viewModel.Outline.GroupFields.Select(static field => field.Key));
        Assert.Equal(new[] { "planned", "actual" }, viewModel.Outline.ValueFields.Select(static field => field.Key));
        Assert.All(viewModel.Outline.GroupFields, static field => Assert.Null(field.PropertyPath));
        Assert.All(viewModel.Outline.ValueFields, static field => Assert.Null(field.PropertyPath));
        Assert.All(viewModel.Outline.ValueFields, static field => Assert.Equal(PivotAggregateType.Sum, field.AggregateType));
        Assert.NotEmpty(viewModel.Outline.Rows);

        bool moved = await viewModel.DropAsync(
            [1005],
            1001,
            DataGridGeneratedDropPosition.Before,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(moved);
        Assert.Equal(1005, viewModel.Items[0].Id);
        Assert.Equal(DataGridGeneratedDropStatus.Applied, viewModel.DragDrop.Status);

        int countBeforeCopy = viewModel.Items.Count;
        bool copied = await viewModel.DropAsync(
            [1002],
            1004,
            DataGridGeneratedDropPosition.After,
            DataGridGeneratedDropOperation.Copy,
            TestContext.Current.CancellationToken);
        Assert.True(copied);
        Assert.Equal(countBeforeCopy + 1, viewModel.Items.Count);
        Assert.Contains(viewModel.Items, static item => item.Id >= 1100 && item.WorkItem.EndsWith("(copy)", StringComparison.Ordinal));

        bool selfDrop = await viewModel.DropAsync(
            [1001],
            1001,
            DataGridGeneratedDropPosition.Before,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(selfDrop);
        Assert.Equal(DataGridGeneratedDropStatus.Rejected, viewModel.DragDrop.Status);
        Assert.Contains("itself", viewModel.DragDrop.Error, StringComparison.Ordinal);

        bool lockedTarget = await viewModel.DropAsync(
            [1001],
            1006,
            DataGridGeneratedDropPosition.After,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(lockedTarget);
        Assert.Contains("locked", viewModel.DragDrop.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Generated_reactive_view_recipes_share_schema_collection_and_compiled_search()
    {
        using var viewModel = new GeneratedReactiveViewRecipesViewModel();

        Assert.Equal(6, viewModel.SourceRowCount);
        Assert.Equal(6, viewModel.VisibleRowCount);
        Assert.Equal(6, viewModel.ColumnDefinitions.Count);
        Assert.True(viewModel.FastPathOptions.StrictMode);
        Assert.Equal(0, GeneratedRecipeGridOnlyView.GeneratedRecipe);
        Assert.Equal(3, GeneratedRecipeExplorerView.GeneratedRecipe);
        Assert.Equal(4, GeneratedRecipeSpreadsheetView.GeneratedRecipe);
        Assert.Equal(5, GeneratedRecipeAnalyticsView.GeneratedRecipe);

        viewModel.Query = "Charts";
        Assert.Equal(1, viewModel.VisibleRowCount);
        Assert.Equal("Analytics slot", Assert.IsType<GeneratedRecipeRow>(viewModel.Items[0]).Name);
        Assert.Contains("compiled search", viewModel.Status, StringComparison.Ordinal);

        viewModel.AddRowCommand.Execute().Subscribe();
        Assert.Equal(7, viewModel.SourceRowCount);
        Assert.Equal(1, viewModel.VisibleRowCount);

        viewModel.AdvanceCommand.Execute().Subscribe();
        Assert.Contains("stable row 1", viewModel.Status, StringComparison.Ordinal);

        viewModel.ClearSearchCommand.Execute().Subscribe();
        Assert.Equal(7, viewModel.VisibleRowCount);
        viewModel.RestoreCommand.Execute().Subscribe();
        Assert.Equal(6, viewModel.SourceRowCount);
        Assert.Equal(6, viewModel.VisibleRowCount);
    }

    [Fact]
    public void Generated_custom_implementations_execute_direct_factory_hooks_policies_and_summary()
    {
        using var viewModel = new GeneratedCustomImplementationsViewModel();

        Assert.Equal(6, viewModel.ColumnDefinitions.Count);
        Assert.True(viewModel.FastPathOptions.StrictMode);

        DataGridTextColumnDefinition title = Assert.IsType<DataGridTextColumnDefinition>(
            viewModel.ColumnDefinitions.Single(static column => Equals(column.ColumnKey, "title")));
        Assert.Equal("Created by the user-defined column factory", title.Watermark);
        Assert.NotNull(title.Binding);
        Assert.Equal("schema-configure-hook", title.Tag);
        Assert.Equal("generated-custom-implementations", title.WidthSharingGroup);

        DataGridTextColumnDefinition severity = Assert.IsType<DataGridTextColumnDefinition>(
            viewModel.ColumnDefinitions.Single(static column => Equals(column.ColumnKey, "severity")));
        Assert.Same(GeneratedSeverityComparer.Instance, severity.Options.SortValueComparer);
        Assert.Equal("custom-comparer-hook", severity.Tag);

        IComparer<GeneratedCustomImplementationRow> comparer =
            GeneratedCustomImplementationRowSchema.Instance.CreateSortComparer(
            [
                GeneratedCustomImplementationRowSchema.Severity.Ascending(GeneratedSeverityComparer.Instance)
            ]);
        GeneratedCustomImplementationRow[] sorted = viewModel.Items
            .Cast<GeneratedCustomImplementationRow>()
            .OrderBy(static row => row, comparer)
            .ToArray();
        Assert.Equal("Critical", sorted[0].Severity);
        Assert.Equal("Low", sorted[^1].Severity);

        DataGridNumericColumnDefinition score = Assert.IsType<DataGridNumericColumnDefinition>(
            viewModel.ColumnDefinitions.Single(static column => Equals(column.ColumnKey, "score")));
        DataGridSummaryDefinition summaryDefinition = Assert.Single(score.SummaryDefinitions);
        DataGridCustomSummaryDescription summary = Assert.IsType<DataGridCustomSummaryDescription>(summaryDefinition.Factory!());
        Assert.IsType<GeneratedWeightedScoreSummaryCalculator>(summary.Calculator);
        Assert.Equal(viewModel.WeightedScore, Assert.IsType<double>(summary.Calculate(viewModel.Items, new DataGridNumericColumn())), 10);

        int originalEffort = Assert.IsType<GeneratedCustomImplementationRow>(viewModel.Items[0]).Effort;
        viewModel.RejectInvalidEditCommand.Execute().Subscribe();
        Assert.Equal(originalEffort, Assert.IsType<GeneratedCustomImplementationRow>(viewModel.Items[0]).Effort);
        Assert.Contains(nameof(DataGridGeneratedEditStatus.ValidationFailed), viewModel.Status, StringComparison.Ordinal);

        viewModel.ApplyValidEditCommand.Execute().Subscribe();
        Assert.Equal(12, Assert.IsType<GeneratedCustomImplementationRow>(viewModel.Items[0]).Effort);
        viewModel.SortSeverityCommand.Execute().Subscribe();
        Assert.Same(GeneratedSeverityComparer.Instance, Assert.Single(viewModel.SortingModel.Descriptors).Comparer);
        viewModel.RestoreCommand.Execute().Subscribe();
        Assert.Empty(viewModel.SortingModel.Descriptors);
        Assert.Equal(8, Assert.IsType<GeneratedCustomImplementationRow>(viewModel.Items[0]).Effort);
    }

    [AvaloniaFact]
    public async Task Generated_editing_clipboard_fill_view_model_exercises_typed_workflows()
    {
        using var viewModel = new GeneratedEditingClipboardFillViewModel();

        Assert.Equal(7, GeneratedEditableOrderSchema.EditFields.Count);
        Assert.DoesNotContain(GeneratedEditableOrderSchema.EditFields, static field => field.ColumnKey is "order-id" or "total");
        Assert.False(viewModel.CanUndo);

        viewModel.ApplyValidEditCommand.Execute().Subscribe();
        Assert.Equal("CATALYST", viewModel.Items[0].Product);
        Assert.Equal(123.46m, viewModel.Items[0].UnitPrice);
        Assert.True(viewModel.CanUndo);

        int originalQuantity = viewModel.Items[1].Quantity;
        decimal lockedPrice = viewModel.Items[^1].UnitPrice;
        viewModel.ApplyInvalidEditCommand.Execute().Subscribe();
        Assert.Equal(originalQuantity, viewModel.Items[1].Quantity);
        Assert.Equal(lockedPrice, viewModel.Items[^1].UnitPrice);
        Assert.Equal(2, viewModel.LastErrorCount);
        Assert.Contains("Locked-row policy=NotEditable", viewModel.Status, StringComparison.Ordinal);

        await viewModel.ValidateAsyncCommand.Execute().ToTask();
        Assert.Equal(148.68m, viewModel.Items[0].UnitPrice);
        Assert.Equal(1, viewModel.LastErrorCount);
        Assert.Contains("approval=ValidationFailed", viewModel.Status, StringComparison.Ordinal);

        viewModel.PasteCommand.Execute().Subscribe();
        Assert.Equal(("OMEGA", 12, 44.13m, 0.10m),
            (viewModel.Items[0].Product, viewModel.Items[0].Quantity, viewModel.Items[0].UnitPrice, viewModel.Items[0].Discount));
        Assert.Equal(4, viewModel.LastAppliedCells);
        Assert.Equal(4, viewModel.LastErrorCount);
        Assert.True(viewModel.EditController.Undo());
        Assert.Equal(("CATALYST", 10, 148.68m, 0.05m),
            (viewModel.Items[0].Product, viewModel.Items[0].Quantity, viewModel.Items[0].UnitPrice, viewModel.Items[0].Discount));

        int[] originalQuantities = viewModel.Items.Select(static item => item.Quantity).ToArray();
        viewModel.FillSeriesCommand.Execute().Subscribe();
        Assert.Equal(new[] { 10, 20, 30, 40, 50, 60 }, viewModel.Items.Select(static item => item.Quantity));
        Assert.True(viewModel.EditController.Undo());
        Assert.Equal(originalQuantities, viewModel.Items.Select(static item => item.Quantity));
        Assert.True(viewModel.EditController.Redo());
        Assert.Equal(new[] { 10, 20, 30, 40, 50, 60 }, viewModel.Items.Select(static item => item.Quantity));

        viewModel.ExportCommand.Execute().Subscribe();
        Assert.StartsWith("product,quantity,unit-price,discount", viewModel.ExportPreview, StringComparison.Ordinal);
        Assert.Contains("Generated Csv export", viewModel.Status, StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public void Generated_editing_clipboard_fill_view_model_disposes_idempotently()
    {
        var viewModel = new GeneratedEditingClipboardFillViewModel();

        viewModel.Dispose();
        viewModel.Dispose();

        Assert.True(viewModel.IsDisposed);
    }
}
