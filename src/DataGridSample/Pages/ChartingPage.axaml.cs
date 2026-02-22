using Avalonia;
using Avalonia.Controls;
using DataGridSample.ViewModels;

namespace DataGridSample.Pages
{
    public partial class ChartingPage : UserControl
    {
        public static readonly StyledProperty<ChartSampleKind> SampleKindProperty =
            AvaloniaProperty.Register<ChartingPage, ChartSampleKind>(nameof(SampleKind));

        static ChartingPage()
        {
            SampleKindProperty.Changed.AddClassHandler<ChartingPage>((control, _) => control.UpdateViewModel());
        }

        public ChartingPage()
        {
            InitializeComponent();
            AttachedToVisualTree += (_, _) => UpdateViewModel();
        }

        public ChartSampleKind SampleKind
        {
            get => GetValue(SampleKindProperty);
            set => SetValue(SampleKindProperty, value);
        }

        private void OnCellEditEnded(object? sender, DataGridCellEditEndedEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit)
            {
                return;
            }

            if (DataContext is ChartSampleViewModel viewModel)
            {
                viewModel.Chart.Refresh();
            }
        }

        private void UpdateViewModel()
        {
            if (VisualRoot is null)
            {
                return;
            }

            if (DataContext is ChartSampleViewModel viewModel && viewModel.Kind == SampleKind)
            {
                return;
            }

            DataContext = new ChartSampleViewModel(SampleKind);
        }
    }
}
