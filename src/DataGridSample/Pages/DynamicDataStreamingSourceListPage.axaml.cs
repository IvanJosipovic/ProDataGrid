using System;
using Avalonia.Controls;
using DataGridSample.ViewModels;

namespace DataGridSample.Pages
{
    public partial class DynamicDataStreamingSourceListPage : UserControl
    {
        public DynamicDataStreamingSourceListPage()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            if (DataContext is DynamicDataStreamingSourceListViewModel vm)
            {
                Grid.SortingAdapterFactory = vm.SortingAdapterFactory;
                Grid.FilteringAdapterFactory = vm.FilteringAdapterFactory;
                Grid.SortingModel = vm.SortingModel;
                Grid.FilteringModel = vm.FilteringModel;
            }
        }
    }
}
