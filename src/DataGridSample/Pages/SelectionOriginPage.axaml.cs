using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DataGridSample.ViewModels;

namespace DataGridSample.Pages
{
    public partial class SelectionOriginPage : UserControl
    {
        public SelectionOriginPage()
        {
            InitializeComponent();
            AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.SelectionOriginViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DataContext is SelectionOriginViewModel vm &&
                e is DataGridSelectionChangedEventArgs args)
            {
                vm.RecordSelectionChange(args);
            }
        }
    }
}
