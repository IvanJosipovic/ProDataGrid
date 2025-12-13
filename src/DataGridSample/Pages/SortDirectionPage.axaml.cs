// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DataGridSample.ViewModels;
using System.ComponentModel;

namespace DataGridSample.Pages
{
    public partial class SortDirectionPage : UserControl
    {
        private DataGrid _grid = null!;
        private SortDirectionViewModel? _vm;

        public SortDirectionPage()
        {
            InitializeComponent();
            this.DataContextChanged += OnDataContextChanged;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            _grid = this.FindControl<DataGrid>("Grid");
            ApplyViewModelSettings();
        }

        private SortDirectionViewModel? ViewModel => DataContext as SortDirectionViewModel;

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            if (_vm != null)
            {
                _vm.PropertyChanged -= OnViewModelPropertyChanged;
            }

            _vm = ViewModel;

            if (_vm != null)
            {
                _vm.PropertyChanged += OnViewModelPropertyChanged;
            }

            ApplyViewModelSettings();
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(SortDirectionViewModel.OwnsSortDescriptions)
                or nameof(SortDirectionViewModel.MultiSortEnabled)
                or nameof(SortDirectionViewModel.SortCycleMode))
            {
                ApplyViewModelSettings();
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
        }

        private void OnApplySavedSorts(object? sender, RoutedEventArgs e)
        {
            if (_grid == null || ViewModel == null)
            {
                return;
            }

            ApplySortStates(ViewModel.SavedSorts);
        }

        private void OnClearSorts(object? sender, RoutedEventArgs e)
        {
            ClearColumnSortDirections();
        }

        private void ApplySortStates(IEnumerable<SortState> states)
        {
            ClearColumnSortDirections();

            foreach (var state in states)
            {
                var column = _grid.ColumnDefinitions
                    .OfType<DataGridColumn>()
                    .FirstOrDefault(c => string.Equals(c.SortMemberPath, state.Property, StringComparison.Ordinal));

                if (column != null)
                {
                    column.SortDirection = state.Direction;
                }
            }
        }

        private void ClearColumnSortDirections()
        {
            if (_grid == null)
            {
                return;
            }

            foreach (var column in _grid.ColumnDefinitions.OfType<DataGridColumn>())
            {
                column.SortDirection = null;
            }
        }
    }
}
