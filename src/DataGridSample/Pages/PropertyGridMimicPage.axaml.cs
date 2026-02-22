using Avalonia.Controls;

namespace DataGridSample.Pages;

public partial class PropertyGridMimicPage : UserControl
{
    public PropertyGridMimicPage()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.PropertyGridMimicViewModel();
    }
}
