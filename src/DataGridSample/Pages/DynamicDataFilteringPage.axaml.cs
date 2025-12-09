// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using DataGridSample.ViewModels;

namespace DataGridSample.Pages
{
    public partial class DynamicDataFilteringPage : UserControl
    {
        public DynamicDataFilteringPage()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            if (DataContext is DynamicDataFilteringViewModel vm)
            {
                Grid.FilteringAdapterFactory = vm.AdapterFactory;
                Grid.FilteringModel = vm.FilteringModel;
            }
        }
    }
}
