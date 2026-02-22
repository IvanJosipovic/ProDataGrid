using Avalonia.Controls;

namespace DataGridSample.Pages;

public partial class MailboxMimicPage : UserControl
{
    public MailboxMimicPage()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.MailboxMimicViewModel();
    }
}
