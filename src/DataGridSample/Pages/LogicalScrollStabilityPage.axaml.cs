using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DataGridSample.Pages;

public partial class LogicalScrollStabilityPage : UserControl
{
    public LogicalScrollStabilityPage()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
