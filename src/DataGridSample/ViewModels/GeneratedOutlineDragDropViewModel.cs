// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.DataGridReporting;
using DataGridSample.Models;
using ProDataGrid.SourceGeneration;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using RxVoid = ReactiveUI.Primitives.RxVoid;

namespace DataGridSample.ViewModels;

[GenerateDataGridViewModel(typeof(GeneratedOutlineWorkItem), ProviderName = "GeneratedOutlineWorkItemSchema")]
[GenerateDataGridView(
    typeof(GeneratedOutlineWorkItem),
    ViewName = "GeneratedOutlineDragDropGrid",
    ViewNamespace = "DataGridSample.Pages",
    Framework = DataGridViewFramework.ReactiveUI,
    Recipe = DataGridViewRecipe.SearchableGrid,
    Title = "Generated keyed drag/drop source",
    AutomationId = "generated-outline-drag-drop-grid")]
public sealed partial class GeneratedOutlineDragDropViewModel : ReactiveObject, IDisposable
{
    private readonly GeneratedWorkItemDropHandler _dropHandler;
    private int _nextId = 1100;
    private bool _disposed;

    [Reactive]
    private string _status = "Generated outline selectors and keyed drag/drop are ready.";

    public GeneratedOutlineDragDropViewModel()
    {
        Items = new ObservableCollection<GeneratedOutlineWorkItem>(CreateInitialItems());
        Outline = GeneratedOutlineWorkItemSchema.CreateOutlineReportModel(Items, ConfigureOutline);
        _dropHandler = new GeneratedWorkItemDropHandler(this);
        DragDrop = GeneratedOutlineWorkItemSchema.CreateDragDropController(_dropHandler, ValidateDropAsync);
        DragDrop.StateChanged += OnDragDropStateChanged;

        MoveLastBeforeFirstCommand = ReactiveCommand.CreateFromTask(
            async () => await DropAsync(
                [Items[^1].Id],
                Items[0].Id,
                DataGridGeneratedDropPosition.Before));
        CopySecondAfterFourthCommand = ReactiveCommand.CreateFromTask(
            async () => await DropAsync(
                [Items[1].Id],
                Items[Math.Min(3, Items.Count - 1)].Id,
                DataGridGeneratedDropPosition.After,
                DataGridGeneratedDropOperation.Copy));
        RejectSelfDropCommand = ReactiveCommand.CreateFromTask(
            async () => await DropAsync(
                [Items[0].Id],
                Items[0].Id,
                DataGridGeneratedDropPosition.Before));
        ResetCommand = ReactiveCommand.Create(Reset);
        ExpandAllCommand = ReactiveCommand.Create(() => Outline.HierarchicalModel.ExpandAll());
        CollapseAllCommand = ReactiveCommand.Create(() => Outline.HierarchicalModel.CollapseAll());
    }

    public ObservableCollection<GeneratedOutlineWorkItem> Items { get; }

    public OutlineReportModel Outline { get; }

    public DataGridGeneratedDragDropController<int> DragDrop { get; }

    public ReactiveCommand<RxVoid, bool> MoveLastBeforeFirstCommand { get; }

    public ReactiveCommand<RxVoid, bool> CopySecondAfterFourthCommand { get; }

    public ReactiveCommand<RxVoid, bool> RejectSelfDropCommand { get; }

    public ReactiveCommand<RxVoid, RxVoid> ResetCommand { get; }

    public ReactiveCommand<RxVoid, RxVoid> ExpandAllCommand { get; }

    public ReactiveCommand<RxVoid, RxVoid> CollapseAllCommand { get; }

    public async ValueTask<bool> DropAsync(
        IReadOnlyList<int> itemKeys,
        int targetKey,
        DataGridGeneratedDropPosition position,
        DataGridGeneratedDropOperation operation = DataGridGeneratedDropOperation.Move,
        CancellationToken cancellationToken = default)
    {
        bool applied = await DragDrop.DropAsync(
            itemKeys,
            targetKey,
            position,
            operation,
            cancellationToken);
        Status = applied
            ? $"r{DragDrop.Revision}: {operation} applied with stable keys; outline rows refreshed."
            : $"r{DragDrop.Revision}: {DragDrop.Status}: {DragDrop.Error ?? "request was not applied"}";
        return applied;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        DragDrop.StateChanged -= OnDragDropStateChanged;
        DragDrop.Dispose();
        Outline.Dispose();
    }

    private void OnDragDropStateChanged(object? sender, EventArgs args)
    {
        Status = DragDrop.Error == null
            ? $"r{DragDrop.Revision}: {DragDrop.Status}"
            : $"r{DragDrop.Revision}: {DragDrop.Status}: {DragDrop.Error}";
    }

    private ValueTask<string?> ValidateDropAsync(
        DataGridGeneratedDropRequest<int> request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request.Operation == DataGridGeneratedDropOperation.Link)
        {
            return ValueTask.FromResult<string?>("This sample domain supports Move and Copy only.");
        }
        GeneratedOutlineWorkItem? target = FindByKey(request.TargetKey);
        return ValueTask.FromResult<string?>(target?.Locked == true
            ? $"Work item {target.Id} is locked and cannot be a drop target."
            : null);
    }

    private void Reset()
    {
        Items.Clear();
        GeneratedOutlineWorkItem[] initial = CreateInitialItems();
        for (int index = 0; index < initial.Length; index++)
        {
            Items.Add(initial[index]);
        }
        Status = "Restored the authoritative source order; outline groups refreshed from collection changes.";
    }

    private GeneratedOutlineWorkItem? FindByKey(int key)
    {
        for (int index = 0; index < Items.Count; index++)
        {
            if (Items[index].Id == key)
            {
                return Items[index];
            }
        }
        return null;
    }

    private static void ConfigureOutline(OutlineReportModel report)
    {
        report.Layout.RowHeaderLabel = "Region / team";
        report.Layout.ShowSubtotals = true;
        report.Layout.ShowGrandTotal = true;
        report.Layout.ShowDetailRows = true;
        report.Layout.AutoExpandGroups = true;
        report.Layout.DetailLabelSelector = static item =>
            item is GeneratedOutlineWorkItem workItem ? workItem.WorkItem : string.Empty;
    }

    private static GeneratedOutlineWorkItem[] CreateInitialItems() =>
    [
        CreateItem(1001, "North", "Alpha", "Streaming ingestion", 120_000m, 96_000m, 80d),
        CreateItem(1002, "North", "Alpha", "Search indexing", 80_000m, 52_000m, 65d),
        CreateItem(1003, "North", "Beta", "Audit pipeline", 75_000m, 48_000m, 62d),
        CreateItem(1004, "South", "Gamma", "Remote paging", 110_000m, 91_000m, 83d),
        CreateItem(1005, "South", "Gamma", "State persistence", 62_000m, 39_000m, 58d),
        CreateItem(1006, "West", "Delta", "Compliance export", 95_000m, 95_000m, 100d, locked: true)
    ];

    private static GeneratedOutlineWorkItem CreateItem(
        int id,
        string region,
        string team,
        string workItem,
        decimal planned,
        decimal actual,
        double progress,
        bool locked = false) => new()
    {
        Id = id,
        Region = region,
        Team = team,
        WorkItem = workItem,
        Planned = planned,
        Actual = actual,
        Progress = progress,
        Locked = locked
    };

    private sealed class GeneratedWorkItemDropHandler : IDataGridGeneratedDropHandler<int>
    {
        private readonly GeneratedOutlineDragDropViewModel _owner;

        public GeneratedWorkItemDropHandler(GeneratedOutlineDragDropViewModel owner) => _owner = owner;

        public ValueTask ApplyAsync(
            DataGridGeneratedDropRequest<int> request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            GeneratedOutlineWorkItem? target = _owner.FindByKey(request.TargetKey);
            if (target == null)
            {
                throw new InvalidOperationException($"Target key {request.TargetKey} no longer exists.");
            }

            var sourceItems = new List<GeneratedOutlineWorkItem>(request.ItemKeys.Count);
            for (int keyIndex = 0; keyIndex < request.ItemKeys.Count; keyIndex++)
            {
                GeneratedOutlineWorkItem? item = _owner.FindByKey(request.ItemKeys[keyIndex]);
                if (item == null)
                {
                    throw new InvalidOperationException($"Source key {request.ItemKeys[keyIndex]} no longer exists.");
                }
                sourceItems.Add(item);
            }

            if (request.Operation == DataGridGeneratedDropOperation.Move)
            {
                for (int index = 0; index < sourceItems.Count; index++)
                {
                    _owner.Items.Remove(sourceItems[index]);
                }
            }

            int insertionIndex = _owner.Items.IndexOf(target);
            if (insertionIndex < 0)
            {
                throw new InvalidOperationException("The target changed during the drop operation.");
            }
            if (request.Position != DataGridGeneratedDropPosition.Before)
            {
                insertionIndex++;
            }

            for (int index = 0; index < sourceItems.Count; index++)
            {
                GeneratedOutlineWorkItem item = request.Operation == DataGridGeneratedDropOperation.Copy
                    ? Clone(sourceItems[index], _owner._nextId++)
                    : sourceItems[index];
                _owner.Items.Insert(insertionIndex++, item);
            }
            return ValueTask.CompletedTask;
        }

        private static GeneratedOutlineWorkItem Clone(GeneratedOutlineWorkItem source, int id) => new()
        {
            Id = id,
            Region = source.Region,
            Team = source.Team,
            WorkItem = source.WorkItem + " (copy)",
            Planned = source.Planned,
            Actual = source.Actual,
            Progress = source.Progress,
            Locked = false
        };
    }
}
