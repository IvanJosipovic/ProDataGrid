using Avalonia.Controls;

namespace DataGridSample.Pages;

public partial class DetachWhileEditingPage : UserControl
{
    public DetachWhileEditingPage()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.DetachWhileEditingViewModel();
    }
}
