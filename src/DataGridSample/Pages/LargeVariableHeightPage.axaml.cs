using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DataGridSample
{
    public partial class LargeVariableHeightPage : UserControl
    {
        public LargeVariableHeightPage()
        {
            InitializeComponent();
            AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.LargeVariableHeightViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
