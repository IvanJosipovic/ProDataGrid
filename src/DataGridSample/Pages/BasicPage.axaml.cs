using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using DataGridSample.ViewModels;

namespace DataGridSample
{
    public partial class BasicPage : UserControl
    {
        public BasicPage()
        {
            InitializeComponent();
            AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.BasicViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnCountriesSorting(object? sender, DataGridColumnEventArgs e)
        {
            if (DataContext is not BasicViewModel viewModel)
            {
                return;
            }

            var binding = (e.Column as DataGridBoundColumn)?.Binding as Binding;

            if (binding?.Path is { } propertyPath)
            {
                viewModel.EnsureCustomSort(propertyPath);
            }
        }
    }
}
