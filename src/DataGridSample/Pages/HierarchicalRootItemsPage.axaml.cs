// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using Avalonia.Controls;

namespace DataGridSample.Pages
{
    public partial class HierarchicalRootItemsPage : UserControl
    {
        public HierarchicalRootItemsPage()
        {
            InitializeComponent();
            AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.HierarchicalRootItemsViewModel();
        }
    }
}
