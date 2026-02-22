using Avalonia.Controls;

namespace DataGridSample.Pages;

public partial class ListBoxMimicSortModelPage : UserControl
{
    public ListBoxMimicSortModelPage()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.ListBoxMimicSortModelViewModel();
    }
}
