// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.DataGridSelection;
using DataGridSample.Models;
using ProDataGrid.SourceGeneration;
using ReactiveUI;
using RxVoid = ReactiveUI.Primitives.RxVoid;

namespace DataGridSample.ViewModels;

[GenerateDataGridColumns(
    typeof(Country),
    ProviderName = "GeneratedPagingCountrySchema",
    DefaultPageSize = 10,
    InitialCurrency = DataGridGeneratedInitialCurrency.First,
    PreserveCurrentItemByKey = true,
    PreserveSelectionByKey = true)]
[GenerateDataGridViewModel(typeof(Country), ProviderName = "GeneratedPagingCountrySchema")]
public sealed partial class PagingSelectionViewModel : ReactiveObject, IDisposable
{
    private ObservableCollection<Country> _items;
    private readonly DataGridGeneratedCollectionViewController<Country, string> _collectionView;
    private int _pageSize = GeneratedPagingCountrySchema.DefaultPageSize;
    private bool _disposed;

    public PagingSelectionViewModel()
    {
        _items = new ObservableCollection<Country>(Countries.All);
        _collectionView = GeneratedPagingCountrySchema.CreateCollectionViewController(
            _items,
            selectionProfile: new DataGridGeneratedSelectionProfile
            {
                Mode = DataGridSelectionMode.Extended,
                Unit = DataGridSelectionUnit.FullRow,
                PreserveUnloadedKeys = true
            });
        SelectedItems = [];
        _collectionView.View.PageChanged += OnPageChanged;
        _collectionView.View.CurrentChanged += OnCurrentChanged;
        _collectionView.SelectionController.SelectionChanged += OnGeneratedSelectionChanged;

        NextPageCommand = ReactiveCommand.Create(MoveNext);
        PreviousPageCommand = ReactiveCommand.Create(MovePrevious);
        FirstPageCommand = ReactiveCommand.Create(MoveFirst);
        LastPageCommand = ReactiveCommand.Create(MoveLast);
        ClearSelectionCommand = ReactiveCommand.Create(ClearSelection);
        SelectAcrossPagesCommand = ReactiveCommand.Create(SelectAcrossPages);
        RefreshCommand = ReactiveCommand.Create(RefreshPreservingState);
        ReplaceSourceCommand = ReactiveCommand.Create(ReplaceSourcePreservingState);
    }

    public DataGridCollectionView ItemsView => _collectionView.View;

    public IdentitySelectionModel SelectionModel => _collectionView.SelectionModel;

    public ObservableCollection<Country> SelectedItems { get; }

    public ReactiveCommand<RxVoid, RxVoid> NextPageCommand { get; }

    public ReactiveCommand<RxVoid, RxVoid> PreviousPageCommand { get; }

    public ReactiveCommand<RxVoid, RxVoid> FirstPageCommand { get; }

    public ReactiveCommand<RxVoid, RxVoid> LastPageCommand { get; }

    public ReactiveCommand<RxVoid, RxVoid> ClearSelectionCommand { get; }

    public ReactiveCommand<RxVoid, RxVoid> SelectAcrossPagesCommand { get; }

    public ReactiveCommand<RxVoid, RxVoid> RefreshCommand { get; }

    public ReactiveCommand<RxVoid, RxVoid> ReplaceSourceCommand { get; }

    public int PageIndex => ItemsView.PageIndex;

    public int PageCount => ItemsView.PageSize > 0
        ? Math.Max(1, (ItemsView.ItemCount + ItemsView.PageSize - 1) / ItemsView.PageSize)
        : 1;

    public string PageStatus => $"Page {PageIndex + 1} / {PageCount}";

    public int PageSize
    {
        get => _pageSize;
        set
        {
            int normalized = Math.Max(1, value);
            if (_pageSize == normalized)
            {
                return;
            }

            _collectionView.SetPageSize(normalized);
            this.RaiseAndSetIfChanged(ref _pageSize, normalized);
            RaisePageProperties();
        }
    }

    public int SelectedCount => _collectionView.SelectionController.SelectedItemKeys.Count;

    public string SelectionSummary => $"{SelectedCount} selected by generated stable keys";

    public string CurrencyStatus => _collectionView.HasCurrentKey
        ? $"Current key: {_collectionView.CurrentKey}"
        : "No current item";

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _collectionView.View.PageChanged -= OnPageChanged;
        _collectionView.View.CurrentChanged -= OnCurrentChanged;
        _collectionView.SelectionController.SelectionChanged -= OnGeneratedSelectionChanged;
        _collectionView.Dispose();
        NextPageCommand.Dispose();
        PreviousPageCommand.Dispose();
        FirstPageCommand.Dispose();
        LastPageCommand.Dispose();
        ClearSelectionCommand.Dispose();
        SelectAcrossPagesCommand.Dispose();
        RefreshCommand.Dispose();
        ReplaceSourceCommand.Dispose();
        _disposed = true;
    }

    private void MoveNext()
    {
        ItemsView.MoveToNextPage();
        RaisePageProperties();
    }

    private void MovePrevious()
    {
        ItemsView.MoveToPreviousPage();
        RaisePageProperties();
    }

    private void MoveFirst()
    {
        ItemsView.MoveToFirstPage();
        RaisePageProperties();
    }

    private void MoveLast()
    {
        ItemsView.MoveToLastPage();
        RaisePageProperties();
    }

    private void ClearSelection() => _collectionView.SelectionController.Clear();

    private void SelectAcrossPages()
    {
        _collectionView.SelectionController.Clear();
        for (int index = 0; index < _items.Count; index++)
        {
            if (index % 7 == 0 || index == _items.Count - 1)
            {
                _collectionView.SelectionController.SelectKey(_items[index].Name);
            }
        }
    }

    private void RefreshPreservingState()
    {
        _collectionView.Refresh();
        RaisePageProperties();
        RefreshSelectedItems();
    }

    private void ReplaceSourcePreservingState()
    {
        var replacements = new ObservableCollection<Country>();
        for (int index = _items.Count - 1; index >= 0; index--)
        {
            replacements.Add(_items[index]);
        }

        _collectionView.View.PageChanged -= OnPageChanged;
        _collectionView.View.CurrentChanged -= OnCurrentChanged;
        _items = replacements;
        _collectionView.ReplaceView(GeneratedPagingCountrySchema.CreateCollectionView(_items));
        _collectionView.View.PageChanged += OnPageChanged;
        _collectionView.View.CurrentChanged += OnCurrentChanged;
        _pageSize = ItemsView.PageSize;
        this.RaisePropertyChanged(nameof(ItemsView));
        this.RaisePropertyChanged(nameof(PageSize));
        RaisePageProperties();
        RefreshSelectedItems();
    }

    private void OnPageChanged(object? sender, EventArgs e) => RaisePageProperties();

    private void OnCurrentChanged(object? sender, EventArgs e) =>
        this.RaisePropertyChanged(nameof(CurrencyStatus));

    private void OnGeneratedSelectionChanged(object? sender, DataGridGeneratedSelectionChangedEventArgs e)
    {
        RefreshSelectedItems();
        this.RaisePropertyChanged(nameof(SelectedCount));
        this.RaisePropertyChanged(nameof(SelectionSummary));
    }

    private void RefreshSelectedItems()
    {
        IReadOnlyList<Country> selected = _collectionView.SelectionController.GetSelectedItems();
        SelectedItems.Clear();
        for (int index = 0; index < selected.Count; index++)
        {
            SelectedItems.Add(selected[index]);
        }
    }

    private void RaisePageProperties()
    {
        this.RaisePropertyChanged(nameof(PageIndex));
        this.RaisePropertyChanged(nameof(PageCount));
        this.RaisePropertyChanged(nameof(PageStatus));
        this.RaisePropertyChanged(nameof(CurrencyStatus));
    }
}
