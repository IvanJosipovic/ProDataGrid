using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DataGridSample.Pages;

public partial class LogicalScrollStabilityPage : UserControl
{
    public LogicalScrollStabilityPage()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.LogicalScrollStabilityViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
