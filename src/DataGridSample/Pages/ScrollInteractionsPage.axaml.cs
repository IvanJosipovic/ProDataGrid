using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using DataGridSample.ViewModels;

namespace DataGridSample
{
    public partial class ScrollInteractionsPage : UserControl
    {
        private DataGrid? _dataGrid;
        private ScrollViewer? _scrollViewer;
        private ScrollInteractionsViewModel? _viewModel;

        public ScrollInteractionsPage()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            _dataGrid = this.FindControl<DataGrid>("InteractionsGrid");
            if (_dataGrid != null)
            {
                _dataGrid.TemplateApplied += OnDataGridTemplateApplied;
            }

            DataContextChanged += OnDataContextChanged;
            HookViewModel(DataContext as ScrollInteractionsViewModel);
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            HookViewModel(DataContext as ScrollInteractionsViewModel);
        }

        private void HookViewModel(ScrollInteractionsViewModel? viewModel)
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }

            _viewModel = viewModel;

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            }

            ApplySnapPoints();
        }

        private void OnDataGridTemplateApplied(object? sender, TemplateAppliedEventArgs e)
        {
            _scrollViewer = _dataGrid?.FindDescendantOfType<ScrollViewer>();
            ApplySnapPoints();
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ScrollInteractionsViewModel.EnableSnapPoints))
            {
                ApplySnapPoints();
            }
        }

        private void ApplySnapPoints()
        {
            if (_dataGrid == null || _viewModel == null)
                return;

            _scrollViewer ??= _dataGrid.FindDescendantOfType<ScrollViewer>();
            if (_scrollViewer == null)
                return;

            _scrollViewer.VerticalSnapPointsType = _viewModel.EnableSnapPoints
                ? SnapPointsType.MandatorySingle
                : SnapPointsType.None;
            _scrollViewer.VerticalSnapPointsAlignment = SnapPointsAlignment.Near;
        }
    }
}
