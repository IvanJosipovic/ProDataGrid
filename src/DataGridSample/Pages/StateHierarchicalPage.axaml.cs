using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DataGridSample.ViewModels;

namespace DataGridSample.Pages
{
    public partial class StateHierarchicalPage : UserControl
    {
        private DataGridHierarchicalState? _state;

        public StateHierarchicalPage()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            Grid = this.FindControl<DataGrid>("Grid");
        }

        private StateSampleHierarchicalViewModel? ViewModel => DataContext as StateSampleHierarchicalViewModel;

        private void OnCapture(object? sender, RoutedEventArgs e)
        {
            _state = Grid.CaptureHierarchicalState();
        }

        private void OnRestore(object? sender, RoutedEventArgs e)
        {
            if (_state != null)
            {
                Grid.RestoreHierarchicalState(_state);
            }
        }

        private void OnExpandAll(object? sender, RoutedEventArgs e)
        {
            ViewModel?.Model.ExpandAll();
        }

        private void OnCollapseAll(object? sender, RoutedEventArgs e)
        {
            ViewModel?.Model.CollapseAll();
        }
    }
}
