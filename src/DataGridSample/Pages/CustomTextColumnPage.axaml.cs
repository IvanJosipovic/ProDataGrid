using Avalonia.Controls;

namespace DataGridSample.Pages;

public partial class CustomTextColumnPage : UserControl
{
    public CustomTextColumnPage()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.CustomTextColumnViewModel();
    }
}
