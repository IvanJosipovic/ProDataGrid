// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using Avalonia.Controls;
using DataGridSample.ViewModels;

namespace DataGridSample.Pages
{
    public partial class DynamicDataSearchPage : UserControl
    {
        public DynamicDataSearchPage()
        {
            InitializeComponent();
            AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.DynamicDataSearchViewModel();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            if (DataContext is DynamicDataSearchViewModel vm)
            {
                Grid.SearchAdapterFactory = vm.AdapterFactory;
                Grid.SearchModel = vm.SearchModel;
            }
        }
    }
}
