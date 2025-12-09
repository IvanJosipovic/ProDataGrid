// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using DataGridSample.ViewModels;

namespace DataGridSample.Pages
{
    public partial class SortingModelPlaygroundPage : UserControl
    {
        private DataGrid? _grid;
        private SortingModelPlaygroundViewModel? _vm;

        public SortingModelPlaygroundPage()
        {
            InitializeComponent();
            RefreshViewModelSubscription();
            ApplyViewModelSettings();
            AttachSortingModel();
            DataContextChanged += OnDataContextChanged;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            _grid = this.FindControl<DataGrid>("Grid");
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            RefreshViewModelSubscription();
            ApplyViewModelSettings();
            AttachSortingModel();
        }

        private void RefreshViewModelSubscription()
        {
            if (_vm != null)
            {
                _vm.PropertyChanged -= OnViewModelPropertyChanged;
            }

            _vm = DataContext as SortingModelPlaygroundViewModel;

            if (_vm != null)
            {
                _vm.PropertyChanged += OnViewModelPropertyChanged;
            }
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SortingModelPlaygroundViewModel.OwnsSortDescriptions):
                case nameof(SortingModelPlaygroundViewModel.MultiSortEnabled):
                case nameof(SortingModelPlaygroundViewModel.SortCycleMode):
                    ApplyViewModelSettings();
                    break;
            }
        }

        private void AttachSortingModel()
        {
            if (_grid?.SortingModel != null && _vm != null)
            {
                _vm.AttachSortingModel(_grid.SortingModel);
            }
        }

        private void ApplyViewModelSettings()
        {
            if (_grid == null || _vm == null)
            {
                return;
            }

            _grid.OwnsSortDescriptions = _vm.OwnsSortDescriptions;
            _grid.IsMultiSortEnabled = _vm.MultiSortEnabled;
            _grid.SortCycleMode = _vm.SortCycleMode;

            if (_grid.Columns.Count >= 3)
            {
                if (_grid.Columns[0] is DataGridTextColumn serviceColumn)
                {
                    serviceColumn.CustomSortComparer = _vm.ServiceSorter;
                }

                if (_grid.Columns[1] is DataGridTextColumn statusColumn)
                {
                    statusColumn.CustomSortComparer = _vm.StatusSorter;
                }

                if (_grid.Columns[2] is DataGridTextColumn ringColumn)
                {
                    ringColumn.CustomSortComparer = _vm.RingSorter;
                }
            }
        }
    }
}
