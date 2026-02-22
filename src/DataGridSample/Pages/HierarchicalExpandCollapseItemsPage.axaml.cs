// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using Avalonia.Controls;

namespace DataGridSample.Pages
{
    public partial class HierarchicalExpandCollapseItemsPage : UserControl
    {
        public HierarchicalExpandCollapseItemsPage()
        {
            InitializeComponent();
            AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.HierarchicalExpandCollapseItemsViewModel();
        }
    }
}
