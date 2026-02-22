using Avalonia.Controls;

namespace DataGridSample.Pages
{
    public partial class HierarchicalCollapseGapsPage : UserControl
    {
        public HierarchicalCollapseGapsPage()
        {
            InitializeComponent();
            AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.HierarchicalCollapseGapsViewModel();
        }
    }
}
