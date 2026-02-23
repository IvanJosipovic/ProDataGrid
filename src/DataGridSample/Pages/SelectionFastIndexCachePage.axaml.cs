using Avalonia.Controls;

namespace DataGridSample.Pages
{
    public partial class SelectionFastIndexCachePage : UserControl
    {
        public SelectionFastIndexCachePage()
        {
            InitializeComponent();
            AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.SelectionFastIndexCacheViewModel();
        }
    }
}

