using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DataGridSample.Pages
{
    public partial class RowDetailsSelectionPage : UserControl
    {
        public RowDetailsSelectionPage()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
