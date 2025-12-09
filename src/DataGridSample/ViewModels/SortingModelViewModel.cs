// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System.Collections.ObjectModel;
using Avalonia.Collections;
using Avalonia.Controls.DataGridSorting;
using DataGridSample.Models;
using DataGridSample.Mvvm;

namespace DataGridSample.ViewModels
{
    public class SortingModelViewModel : ObservableObject
    {
        private bool _ownsSortDescriptions = true;
        private bool _multiSortEnabled = true;
        private SortCycleMode _sortCycleMode = SortCycleMode.AscendingDescending;

        public SortingModelViewModel()
        {
            Items = new ObservableCollection<Country>(Countries.All);
            ItemsView = new DataGridCollectionView(Items);

            ApplySortCommand = new RelayCommand(_ => ApplyProgrammaticSort());
            ExternalSortCommand = new RelayCommand(_ => PushExternalSorts());
            ClearSortsCommand = new RelayCommand(_ => ItemsView.SortDescriptions.Clear());
        }

        public ObservableCollection<Country> Items { get; }

        public DataGridCollectionView ItemsView { get; }

        public bool OwnsSortDescriptions
        {
            get => _ownsSortDescriptions;
            set => SetProperty(ref _ownsSortDescriptions, value);
        }

        public bool MultiSortEnabled
        {
            get => _multiSortEnabled;
            set => SetProperty(ref _multiSortEnabled, value);
        }

        public SortCycleMode SortCycleMode
        {
            get => _sortCycleMode;
            set => SetProperty(ref _sortCycleMode, value);
        }

        public RelayCommand ApplySortCommand { get; }

        public RelayCommand ExternalSortCommand { get; }

        public RelayCommand ClearSortsCommand { get; }

        private void ApplyProgrammaticSort()
        {
            using (ItemsView.DeferRefresh())
            {
                ItemsView.SortDescriptions.Clear();
                ItemsView.SortDescriptions.Add(DataGridSortDescription.FromPath(nameof(Country.Name), System.ComponentModel.ListSortDirection.Ascending));
                ItemsView.SortDescriptions.Add(DataGridSortDescription.FromPath(nameof(Country.Population), System.ComponentModel.ListSortDirection.Descending));
            }
        }

        private void PushExternalSorts()
        {
            using (ItemsView.DeferRefresh())
            {
                ItemsView.SortDescriptions.Clear();
                ItemsView.SortDescriptions.Add(DataGridSortDescription.FromPath(nameof(Country.Region), System.ComponentModel.ListSortDirection.Descending));
                // Intentional duplicate to showcase deduplication when syncing from the view.
                ItemsView.SortDescriptions.Add(DataGridSortDescription.FromPath(nameof(Country.Region), System.ComponentModel.ListSortDirection.Ascending));
            }
        }
    }
}
