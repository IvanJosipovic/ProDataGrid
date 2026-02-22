using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DataGridSample.Models;
using DataGridSample.ViewModels;

namespace DataGridSample
{
    public partial class VariableHeightPage : UserControl
    {
        private DataGrid? _dataGrid;
        private VariableHeightViewModel? _viewModel;

        public VariableHeightPage()
        {
            InitializeComponent();
            AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.VariableHeightViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            _dataGrid = this.FindControl<DataGrid>("VariableHeightDataGrid");
            var scrollToButton = this.FindControl<Button>("ScrollToButton");

            if (scrollToButton != null)
                scrollToButton.Click += OnScrollToClick;

            if (_dataGrid != null)
            {
                _dataGrid.PropertyChanged += OnDataGridPropertyChanged;
                _dataGrid.TemplateApplied += OnDataGridTemplateApplied;
            }

            DataContextChanged += OnDataContextChanged;
            HookViewModel(DataContext as VariableHeightViewModel);

            Dispatcher.UIThread.InvokeAsync(() => _viewModel?.GenerateItems(), DispatcherPriority.Loaded);
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            HookViewModel(DataContext as VariableHeightViewModel);
        }

        private void HookViewModel(VariableHeightViewModel? viewModel)
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                _viewModel.ItemsRegenerated -= OnItemsRegenerated;
            }

            _viewModel = viewModel;

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
                _viewModel.ItemsRegenerated += OnItemsRegenerated;
                ApplyEstimatorFromSelection(_viewModel.SelectedEstimator);
            }
        }

        private void OnDataGridTemplateApplied(object? sender, TemplateAppliedEventArgs e)
        {
            // Try to find the internal ScrollViewer for more detailed scroll tracking
            if (_dataGrid != null)
            {
                var scrollViewer = _dataGrid.FindDescendantOfType<ScrollViewer>();
                if (scrollViewer != null)
                {
                    scrollViewer.ScrollChanged += (s, args) => UpdateScrollInfo();
                }
            }
        }

        private void OnScrollToClick(object? sender, RoutedEventArgs e)
        {
            if (_dataGrid != null && _viewModel != null)
            {
                int index = _viewModel.ScrollToIndex;
                if (index >= 0 && index < _viewModel.Items.Count)
                {
                    _dataGrid.ScrollIntoView(_viewModel.Items[index], null);
                    _dataGrid.SelectedIndex = index;
                }
            }
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(VariableHeightViewModel.SelectedEstimator))
            {
                ApplyEstimatorFromSelection(_viewModel?.SelectedEstimator);
            }
        }

        private void ApplyEstimatorFromSelection(string? name)
        {
            if (_dataGrid == null || string.IsNullOrWhiteSpace(name))
                return;

            _dataGrid.RowHeightEstimator = name switch
            {
                "Caching" => new CachingRowHeightEstimator(),
                "Default" => new DefaultRowHeightEstimator(),
                _ => new AdvancedRowHeightEstimator(),
            };
        }

        private void OnDataGridPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property.Name == "VerticalOffset" || e.Property.Name == "ViewportHeight")
            {
                UpdateScrollInfo();
            }
        }

        private void OnItemsRegenerated()
        {
            if (_viewModel == null)
                return;

            _viewModel.ScrollToIndex = Math.Clamp(_viewModel.ScrollToIndex, 0, Math.Max(_viewModel.Items.Count - 1, 0));
            UpdateScrollInfo();
        }

        private void UpdateScrollInfo()
        {
            if (_dataGrid == null || _viewModel == null) return;

            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    var scrollViewer = _dataGrid.FindDescendantOfType<ScrollViewer>();
                    if (scrollViewer != null)
                    {
                        _viewModel.ScrollInfoText = $"Scroll: {scrollViewer.Offset.Y:F1} / {scrollViewer.Extent.Height:F1}";
                    }

                    int firstVisible = -1;
                    int lastVisible = -1;

                    // Find visible rows by checking the DataGridRowsPresenter
                    var rowsPresenter = _dataGrid.FindDescendantOfType<DataGridRowsPresenter>();
                    if (rowsPresenter != null)
                    {
                        foreach (var child in rowsPresenter.Children)
                        {
                            if (child is DataGridRow row && row.IsVisible)
                            {
                                int index = row.Index;
                                if (index >= 0)
                                {
                                    if (firstVisible < 0 || index < firstVisible)
                                        firstVisible = index;
                                    if (index > lastVisible)
                                        lastVisible = index;
                                }
                            }
                        }
                    }

                    if (firstVisible >= 0 && lastVisible >= 0)
                    {
                        _viewModel.VisibleRangeText = $"Visible: {firstVisible} - {lastVisible} ({lastVisible - firstVisible + 1} rows)";
                    }
                    else
                    {
                        _viewModel.VisibleRangeText = "Visible Range: N/A";
                    }
                }
                catch
                {
                    // Ignore errors during scroll info updates
                }
            });
        }
    }
}
