// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.DataGridSelection;
using Avalonia.Controls.Selection;
using DataGridSample.Models;
using ProDataGrid.SourceGeneration;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using RxVoid = ReactiveUI.Primitives.RxVoid;

namespace DataGridSample.ViewModels;

[GenerateDataGridViewModel(typeof(GeneratedFeatureRow), ProviderName = "GeneratedFeatureRowSchema")]
[GenerateDataGridView(
    typeof(GeneratedFeatureRow),
    ViewName = "GeneratedGroupedSharedSelectionGrid",
    ViewNamespace = "DataGridSample.Pages",
    Framework = DataGridViewFramework.ReactiveUI,
    Recipe = DataGridViewRecipe.GridOnly,
    Title = "Generated grouped shared selection",
    AutomationId = "generated-grouped-shared-selection-grid",
    ItemsPropertyName = nameof(GroupedItems),
    SelectionModelPropertyName = nameof(SelectionModel),
    SelectionMode = DataGridSelectionMode.Extended,
    SelectionUnit = DataGridSelectionUnit.FullRow,
    IsReadOnly = true)]
public sealed partial class GeneratedGroupedSharedSelectionViewModel : ReactiveObject, IDisposable
{
    private readonly ObservableCollection<GeneratedFeatureRow> _source;
    private bool _synchronizingSelection;
    private bool _disposed;

    [Reactive]
    private string _selectedKeys = "None";

    [Reactive]
    private string _status = "Generated grouping and one identity selection model are shared by the grid and list.";

    public GeneratedGroupedSharedSelectionViewModel()
    {
        _source = new ObservableCollection<GeneratedFeatureRow>(CreateRows());
        GroupedItems = GeneratedFeatureRowSchema.CreateCollectionView(_source);
        SelectionController = GeneratedFeatureRowSchema.CreateSelectionController(
            new DataGridGeneratedSelectionProfile
            {
                Mode = DataGridSelectionMode.Extended,
                Unit = DataGridSelectionUnit.FullRow,
                PreserveUnloadedKeys = true
            });
        SelectionController.ResetSource(_source);
        SelectionModel = SelectionController.CreateIdentitySelectionModel(GroupedItems);
        SelectionModel.SelectionChanged += SelectionModelOnSelectionChanged;
        SelectionController.SelectionChanged += SelectionControllerOnSelectionChanged;

        SelectAcrossGroupsCommand = ReactiveCommand.Create(SelectAcrossGroups);
        ReverseSourceCommand = ReactiveCommand.Create(ReverseSource);
        ClearSelectionCommand = ReactiveCommand.Create(ClearSelection);
        RefreshSelectionProjection();
    }

    public DataGridCollectionView GroupedItems { get; }

    public IdentitySelectionModel SelectionModel { get; }

    public DataGridGeneratedSelectionController<GeneratedFeatureRow, int> SelectionController { get; }

    public ReactiveCommand<RxVoid, RxVoid> SelectAcrossGroupsCommand { get; }

    public ReactiveCommand<RxVoid, RxVoid> ReverseSourceCommand { get; }

    public ReactiveCommand<RxVoid, RxVoid> ClearSelectionCommand { get; }

    public bool IsDisposed => _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        SelectionModel.SelectionChanged -= SelectionModelOnSelectionChanged;
        SelectionController.SelectionChanged -= SelectionControllerOnSelectionChanged;
        SelectionModel.Source = Array.Empty<GeneratedFeatureRow>();
    }

    private void SelectAcrossGroups()
    {
        _synchronizingSelection = true;
        try
        {
            SelectionController.Clear(DataGridGeneratedSelectionOrigin.Programmatic);
            SelectionController.SelectKey(2, DataGridGeneratedSelectionOrigin.Programmatic);
            SelectionController.SelectKey(7, DataGridGeneratedSelectionOrigin.Programmatic);
            SelectionController.SelectKey(11, DataGridGeneratedSelectionOrigin.Programmatic);
            SelectionController.ApplyTo(SelectionModel);
        }
        finally
        {
            _synchronizingSelection = false;
        }

        RefreshSelectionProjection();
        Status = "Selected stable keys 2, 7, and 11 across generated desk groups.";
    }

    private void ReverseSource()
    {
        GeneratedFeatureRow[] rows = _source.Reverse().ToArray();
        _synchronizingSelection = true;
        try
        {
            _source.Clear();
            for (int index = 0; index < rows.Length; index++)
            {
                _source.Add(rows[index]);
            }

            SelectionController.ResetSource(_source, DataGridGeneratedSelectionOrigin.Model);
            SelectionController.ApplyTo(SelectionModel);
        }
        finally
        {
            _synchronizingSelection = false;
        }

        RefreshSelectionProjection();
        Status = "Reversed the domain source; grouped selection was restored by generated stable keys.";
    }

    private void ClearSelection()
    {
        _synchronizingSelection = true;
        try
        {
            SelectionController.Clear(DataGridGeneratedSelectionOrigin.Programmatic);
            SelectionController.ApplyTo(SelectionModel);
        }
        finally
        {
            _synchronizingSelection = false;
        }

        RefreshSelectionProjection();
        Status = "Cleared the shared generated selection.";
    }

    private void SelectionModelOnSelectionChanged(object? sender, SelectionModelSelectionChangedEventArgs e)
    {
        if (_synchronizingSelection)
        {
            return;
        }

        _synchronizingSelection = true;
        try
        {
            SelectionController.CaptureFrom(SelectionModel, DataGridGeneratedSelectionOrigin.Model);
        }
        finally
        {
            _synchronizingSelection = false;
        }

        RefreshSelectionProjection();
        Status = "The shared Avalonia selection model updated generated stable-key state.";
    }

    private void SelectionControllerOnSelectionChanged(object? sender, DataGridGeneratedSelectionChangedEventArgs e)
    {
        if (!_synchronizingSelection && e.Origin != DataGridGeneratedSelectionOrigin.Model)
        {
            _synchronizingSelection = true;
            try
            {
                SelectionController.ApplyTo(SelectionModel);
            }
            finally
            {
                _synchronizingSelection = false;
            }
        }

        RefreshSelectionProjection();
    }

    private void RefreshSelectionProjection()
    {
        IReadOnlyList<int> keys = SelectionController.SelectedItemKeys;
        SelectedKeys = keys.Count == 0 ? "None" : string.Join(", ", keys);
    }

    private static IEnumerable<GeneratedFeatureRow> CreateRows()
    {
        string[] symbols = ["GRID", "RXUI", "KEY", "GROUP"];
        string[] desks = ["Warsaw", "London", "New York"];
        DateTimeOffset origin = new(2026, 8, 8, 8, 0, 0, TimeSpan.Zero);
        for (int id = 1; id <= 12; id++)
        {
            yield return new GeneratedFeatureRow
            {
                Id = id,
                Symbol = $"{symbols[(id - 1) % symbols.Length]}-{id:00}",
                Desk = desks[(id - 1) % desks.Length],
                Amount = 25_000m + id * 8_500m,
                Timestamp = origin.AddMinutes(id * 7)
            };
        }
    }
}
