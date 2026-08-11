// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.DataGridSorting;
using DataGridSample.Models;
using DataGridSample.Pages;
using ProDataGrid.SourceGeneration;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using RxVoid = ReactiveUI.Primitives.RxVoid;

namespace DataGridSample.ViewModels;

[GenerateDataGridViewModel(
    typeof(GeneratedCustomImplementationRow),
    ProviderName = "GeneratedCustomImplementationRowSchema")]
[GenerateDataGridView(
    typeof(GeneratedCustomImplementationRow),
    ViewName = "GeneratedCustomImplementationsGeneratedView",
    ViewNamespace = "DataGridSample.Pages",
    Framework = DataGridViewFramework.ReactiveUI,
    Recipe = DataGridViewRecipe.OperationsToolbar,
    BaseType = typeof(GeneratedCustomImplementationsViewBase),
    Title = "Generated custom implementations",
    AutomationId = "generated-custom-implementations",
    SortingModelPropertyName = nameof(SortingModel),
    ShowTotalSummary = true,
    IsReadOnly = false)]
public sealed partial class GeneratedCustomImplementationsViewModel : ReactiveObject, IDisposable
{
    private readonly ObservableCollection<GeneratedCustomImplementationRow> _source;
    private readonly Dictionary<int, GeneratedCustomImplementationRow> _byId;
    private bool _disposed;

    [Reactive]
    private string _status = "Factory, schema, comparer, validator, summary, base-view, and view-hook customizations are active.";

    [Reactive]
    private double _weightedScore;

    public GeneratedCustomImplementationsViewModel()
    {
        _source =
        [
            CreateRow(1, "Generated schema facade", "High", 8, 91d, true),
            CreateRow(2, "Direct column factory", "Medium", 5, 84d, true),
            CreateRow(3, "Custom sorting policy", "Critical", 13, 96d, false),
            CreateRow(4, "Compiled validator hook", "Low", 3, 78d, true),
            CreateRow(5, "Summary calculator", "High", 8, 89d, true)
        ];
        _byId = new Dictionary<int, GeneratedCustomImplementationRow>(_source.Count);
        for (int index = 0; index < _source.Count; index++)
        {
            _byId.Add(_source[index].Id, _source[index]);
        }

        Items = GeneratedCustomImplementationRowSchema.CreateCollectionView(_source);
        SortingModel = new SortingModel
        {
            MultiSort = false,
            CycleMode = SortCycleMode.AscendingDescendingNone
        };
        EditController = GeneratedCustomImplementationRowSchema.CreateEditController(key => _byId[key]);

        RejectInvalidEditCommand = ReactiveCommand.Create(RejectInvalidEdit);
        ApplyValidEditCommand = ReactiveCommand.Create(ApplyValidEdit);
        SortSeverityCommand = ReactiveCommand.Create(SortSeverity);
        RestoreCommand = ReactiveCommand.Create(Restore);
        RefreshWeightedScore();
    }

    public DataGridCollectionView Items { get; }

    public SortingModel SortingModel { get; }

    public DataGridGeneratedEditController<GeneratedCustomImplementationRow, int> EditController { get; }

    public ReactiveCommand<RxVoid, RxVoid> RejectInvalidEditCommand { get; }

    public ReactiveCommand<RxVoid, RxVoid> ApplyValidEditCommand { get; }

    public ReactiveCommand<RxVoid, RxVoid> SortSeverityCommand { get; }

    public ReactiveCommand<RxVoid, RxVoid> RestoreCommand { get; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        EditController.Dispose();
    }

    private void RejectInvalidEdit()
    {
        DataGridGeneratedEditResult result = EditController.TrySetValue(_source[0], "effort", 0);
        Status = $"Custom validator returned {result.Status}: {result.Error}";
    }

    private void ApplyValidEdit()
    {
        DataGridGeneratedEditResult result = EditController.TrySetValue(_source[0], "effort", 12);
        Items.Refresh();
        RefreshWeightedScore();
        Status = $"Custom validator accepted effort 12: {result.Status}.";
    }

    private void SortSeverity()
    {
        Avalonia.Controls.DataGridSorting.SortingDescriptor descriptor =
            GeneratedCustomImplementationRowSchema.Severity.Ascending(GeneratedSeverityComparer.Instance);
        SortingModel.Apply(
        [
            descriptor
        ]);
        IComparer<GeneratedCustomImplementationRow> compiled =
            GeneratedCustomImplementationRowSchema.Instance.CreateSortComparer([descriptor]);
        IComparer rowComparer = Comparer<object>.Create(
            (left, right) => compiled.Compare(
                (GeneratedCustomImplementationRow)left,
                (GeneratedCustomImplementationRow)right));
        Items.SortDescriptions.Clear();
        Items.SortDescriptions.Add(DataGridSortDescription.FromComparer(rowComparer, ListSortDirection.Ascending));
        Status = "Applied the user-defined Critical, High, Medium, Low comparer through the generated field descriptor.";
    }

    private void Restore()
    {
        SortingModel.Clear();
        Items.SortDescriptions.Clear();
        DataGridGeneratedEditResult result = EditController.TrySetValue(_source[0], "effort", 8);
        Items.Refresh();
        RefreshWeightedScore();
        Status = $"Restored source order and baseline effort: {result.Status}.";
    }

    private void RefreshWeightedScore()
    {
        WeightedScore = new GeneratedWeightedScoreSummaryCalculator().Calculate(_source);
    }

    private static GeneratedCustomImplementationRow CreateRow(
        int id,
        string title,
        string severity,
        int effort,
        double score,
        bool isReady) =>
        new()
        {
            Id = id,
            Title = title,
            Severity = severity,
            Effort = effort,
            Score = score,
            IsReady = isReady
        };
}
