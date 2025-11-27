using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace DataGridSample
{
    public partial class EditablePage : UserControl
    {
        public EditablePage()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnNumericUpDownTemplateApplied(object sender, TemplateAppliedEventArgs e)
        {
            // Focus the internal TextBox after the template is applied so the user can type immediately.
            if (e.NameScope.Find<TextBox>("PART_TextBox") is { } textBox)
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    textBox.Focus();
                    textBox.SelectAll();
                }, DispatcherPriority.Loaded);
            }
        }
    }
}
