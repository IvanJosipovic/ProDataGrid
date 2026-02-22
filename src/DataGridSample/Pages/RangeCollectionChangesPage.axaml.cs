using Avalonia.Controls;

namespace DataGridSample.Pages
{
    public partial class RangeCollectionChangesPage : UserControl
    {
        public RangeCollectionChangesPage()
        {
            InitializeComponent();
            AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.RangeCollectionChangesViewModel();
        }
    }
}
