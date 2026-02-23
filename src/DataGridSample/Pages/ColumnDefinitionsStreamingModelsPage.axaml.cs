using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using DataGridSample.ViewModels;

namespace DataGridSample.Pages
{
    public partial class ColumnDefinitionsStreamingModelsPage : UserControl
    {
        public ColumnDefinitionsStreamingModelsPage()
        {
            InitializeComponent();
            AttachedToVisualTree += (_, _) => DataContext ??= new ColumnDefinitionsStreamingModelsViewModel();
            DataContextChanged += OnDataContextChanged;
            OnDataContextChanged(this, EventArgs.Empty);
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            if (DataContext is ColumnDefinitionsStreamingModelsViewModel viewModel)
            {
                StreamingGrid.FastPathOptions = viewModel.FastPathOptions;
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
