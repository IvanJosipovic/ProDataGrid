using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DataGridSample.ViewModels;

namespace DataGridSample.Pages
{
    public partial class StateGroupingPage : UserControl
    {
        private DataGridGroupingState? _state;

        public StateGroupingPage()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            Grid = this.FindControl<DataGrid>("Grid");
        }

        private StateSampleGroupingViewModel? ViewModel => DataContext as StateSampleGroupingViewModel;

        private void OnCapture(object? sender, RoutedEventArgs e)
        {
            _state = Grid.CaptureGroupingState();
        }

        private void OnRestore(object? sender, RoutedEventArgs e)
        {
            if (_state != null)
            {
                Grid.RestoreGroupingState(_state);
            }
        }

        private void OnExpandAll(object? sender, RoutedEventArgs e)
        {
            Grid.ExpandAllGroups();
        }

        private void OnCollapseAll(object? sender, RoutedEventArgs e)
        {
            Grid.CollapseAllGroups();
        }

        private void OnClearGrouping(object? sender, RoutedEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }

            ViewModel.ItemsView.GroupDescriptions.Clear();
            ViewModel.ItemsView.Refresh();
        }
    }
}
