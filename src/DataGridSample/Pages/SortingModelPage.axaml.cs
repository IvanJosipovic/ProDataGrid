// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.ComponentModel;
using DataGridSample.ViewModels;

namespace DataGridSample.Pages
{
    public partial class SortingModelPage : UserControl
    {
        private DataGrid _grid;
        private SortingModelViewModel _vm;

        public SortingModelPage()
        {
            InitializeComponent();
            this.DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, System.EventArgs e)
        {
            if (_vm != null)
            {
                _vm.PropertyChanged -= OnViewModelPropertyChanged;
            }

            _vm = DataContext as SortingModelViewModel;
            if (_vm != null)
            {
                _vm.PropertyChanged += OnViewModelPropertyChanged;
                ApplyViewModelSettings();
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            _grid = this.FindControl<DataGrid>("Grid");
            ApplyViewModelSettings();
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_grid == null)
            {
                return;
            }

            switch (e.PropertyName)
            {
                case nameof(SortingModelViewModel.OwnsSortDescriptions):
                case nameof(SortingModelViewModel.MultiSortEnabled):
                case nameof(SortingModelViewModel.SortCycleMode):
                    ApplyViewModelSettings();
                    break;
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
    }
}
