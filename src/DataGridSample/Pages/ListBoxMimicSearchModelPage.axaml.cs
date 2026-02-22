using Avalonia.Controls;

namespace DataGridSample.Pages;

public partial class ListBoxMimicSearchModelPage : UserControl
{
    public ListBoxMimicSearchModelPage()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.ListBoxMimicSearchModelViewModel();
    }
}
