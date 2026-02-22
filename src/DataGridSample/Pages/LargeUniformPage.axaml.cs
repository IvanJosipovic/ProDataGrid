using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using DataGridSample.ViewModels;

namespace DataGridSample
{
    public partial class LargeUniformPage : UserControl
    {
        private DataGrid? _dataGrid;
        private LargeUniformViewModel? _viewModel;

        public LargeUniformPage()
        {
            InitializeComponent();
            AttachedToVisualTree += (_, _) => DataContext ??= new LargeUniformViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            _dataGrid = this.FindControl<DataGrid>("UniformDataGrid");

            if (_dataGrid != null)
            {
                _dataGrid.PropertyChanged += OnDataGridPropertyChanged;
                _dataGrid.TemplateApplied += OnDataGridTemplateApplied;
            }

            DataContextChanged += OnDataContextChanged;
            HookViewModel(DataContext as LargeUniformViewModel);
        }

        private void OnDataContextChanged(object? sender, System.EventArgs e)
        {
            HookViewModel(DataContext as LargeUniformViewModel);
        }

        private void HookViewModel(LargeUniformViewModel? viewModel)
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }

            _viewModel = viewModel;

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
                ApplyEstimator(_viewModel.SelectedEstimator);
            }
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LargeUniformViewModel.SelectedEstimator))
            {
                ApplyEstimator(_viewModel?.SelectedEstimator);
            }
        }

        private void OnDataGridPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property.Name == "RowHeightEstimator" && _viewModel != null)
            {
                // Keep VM in sync if estimator changes from outside.
                var estimator = _dataGrid?.RowHeightEstimator?.GetType().Name;
                if (estimator != null)
                {
                    _viewModel.SelectedEstimator = estimator.Contains("Caching") ? "Caching" :
                        estimator.Contains("Default") ? "Default" : "Advanced";
                }
            }
        }

        private void OnDataGridTemplateApplied(object? sender, TemplateAppliedEventArgs e)
        {
            ApplyEstimator(_viewModel?.SelectedEstimator);
        }

        private void ApplyEstimator(string? name)
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
    }
}
