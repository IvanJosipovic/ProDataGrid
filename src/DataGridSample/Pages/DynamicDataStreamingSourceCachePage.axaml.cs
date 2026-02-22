using System;
using Avalonia.Controls;
using DataGridSample.ViewModels;

namespace DataGridSample.Pages
{
    public partial class DynamicDataStreamingSourceCachePage : UserControl
    {
        public DynamicDataStreamingSourceCachePage()
        {
            InitializeComponent();
            AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.DynamicDataStreamingSourceCacheViewModel();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            if (DataContext is DynamicDataStreamingSourceCacheViewModel vm)
            {
                Grid.SortingAdapterFactory = vm.SortingAdapterFactory;
                Grid.FilteringAdapterFactory = vm.FilteringAdapterFactory;
                Grid.SortingModel = vm.SortingModel;
                Grid.FilteringModel = vm.FilteringModel;
            }
        }
    }
}
