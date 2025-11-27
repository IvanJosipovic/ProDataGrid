using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DataGridSample
{
    public partial class LargeVariableHeightPage : UserControl
    {
        public LargeVariableHeightPage()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
