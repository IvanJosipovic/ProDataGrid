using Avalonia.Controls;

namespace DataGridSample.Pages
{
    public partial class StylingShowcasePage : UserControl
    {
        public StylingShowcasePage()
        {
            InitializeComponent();
            AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.StylingShowcaseViewModel();
        }
    }
}

