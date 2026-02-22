using Avalonia.Controls;

namespace DataGridSample.Pages;

public partial class HeaderSelectionPage : UserControl
{
    public HeaderSelectionPage()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.HeaderSelectionViewModel();
    }
}
