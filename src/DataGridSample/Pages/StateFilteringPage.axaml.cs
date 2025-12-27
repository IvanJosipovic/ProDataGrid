using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.DataGridFiltering;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DataGridSample;
using DataGridSample.ViewModels;

namespace DataGridSample.Pages
{
    public partial class StateFilteringPage : UserControl
    {
        private DataGridFilteringState? _state;

        public StateFilteringPage()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            Grid = this.FindControl<DataGrid>("Grid");
        }

        private StateSampleViewModel? ViewModel => DataContext as StateSampleViewModel;

        private DataGridStateOptions? CreateOptions()
        {
            return ViewModel == null ? null : StateSampleOptionsFactory.Create(Grid, ViewModel.Items);
        }

        private void OnCapture(object? sender, RoutedEventArgs e)
        {
            _state = Grid.CaptureFilteringState(CreateOptions());
        }

        private void OnRestore(object? sender, RoutedEventArgs e)
        {
            if (_state != null)
            {
                Grid.RestoreFilteringState(_state, CreateOptions());
            }
        }

        private void OnApplyFilter(object? sender, RoutedEventArgs e)
        {
            var categoryColumn = Grid.Columns.FirstOrDefault(column => Equals(column.Header, "Category"));
            if (categoryColumn == null)
            {
                return;
            }

            Grid.FilteringModel.Apply(new[]
            {
                new FilteringDescriptor(
                    categoryColumn,
                    FilteringOperator.Equals,
                    "Category",
                    "Alpha",
                    stringComparison: StringComparison.Ordinal),
            });
        }

        private void OnClearFilter(object? sender, RoutedEventArgs e)
        {
            Grid.FilteringModel.Clear();
        }
    }
}
