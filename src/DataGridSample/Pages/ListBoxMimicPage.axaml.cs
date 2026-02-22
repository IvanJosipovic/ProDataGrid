using Avalonia.Controls;

namespace DataGridSample.Pages;

public partial class ListBoxMimicPage : UserControl
{
    public ListBoxMimicPage()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.ListBoxMimicViewModel();
    }
}
