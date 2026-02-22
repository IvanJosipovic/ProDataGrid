// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using Avalonia.Controls;

namespace DataGridSample.Pages
{
    public partial class HierarchicalSelectionModelExpandCollapsePage : UserControl
    {
        public HierarchicalSelectionModelExpandCollapsePage()
        {
            InitializeComponent();
            AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.HierarchicalSelectionModelExpandCollapseViewModel();
        }
    }
}
