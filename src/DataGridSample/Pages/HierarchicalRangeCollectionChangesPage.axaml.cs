using Avalonia.Controls;

namespace DataGridSample.Pages
{
    public partial class HierarchicalRangeCollectionChangesPage : UserControl
    {
        public HierarchicalRangeCollectionChangesPage()
        {
            InitializeComponent();
            AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.HierarchicalRangeCollectionChangesViewModel();
        }
    }
}
