using Avalonia.Controls;

namespace DataGridSample.Pages;

public partial class ListBoxMimicFilterModelPage : UserControl
{
    public ListBoxMimicFilterModelPage()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.ListBoxMimicFilterModelViewModel();
    }
}
