using Avalonia.Controls;

namespace DataGridSample.Pages;

public partial class ChatMimicPage : UserControl
{
    public ChatMimicPage()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.ChatMimicViewModel();
    }
}
