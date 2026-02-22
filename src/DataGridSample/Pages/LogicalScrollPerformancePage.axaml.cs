using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DataGridSample.Pages;

public partial class LogicalScrollPerformancePage : UserControl
{
    public LogicalScrollPerformancePage()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.LogicalScrollPerformanceViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
