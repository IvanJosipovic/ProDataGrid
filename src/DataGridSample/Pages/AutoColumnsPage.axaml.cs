using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DataGridSample
{
    public partial class AutoColumnsPage : UserControl
    {
        public AutoColumnsPage()
        {
            InitializeComponent();
            AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.AutoColumnsViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
