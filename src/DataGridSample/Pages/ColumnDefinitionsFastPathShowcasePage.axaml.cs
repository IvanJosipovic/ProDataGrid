using System;
using Avalonia.Controls;
using DataGridSample.ViewModels;

namespace DataGridSample.Pages
{
    public partial class ColumnDefinitionsFastPathShowcasePage : UserControl
    {
        public ColumnDefinitionsFastPathShowcasePage()
        {
            InitializeComponent();
            AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.ColumnDefinitionsFastPathShowcaseViewModel();
            DataContextChanged += OnDataContextChanged;
            OnDataContextChanged(this, EventArgs.Empty);
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            if (DataContext is ColumnDefinitionsFastPathShowcaseViewModel viewModel)
            {
                FastPathGrid.FastPathOptions = viewModel.FastPathOptions;
            }
        }

    }
}
