using Avalonia.Controls;
using DataGridSample.ViewModels;

namespace DataGridSample.Pages
{
    public partial class SelectionFastIndexResolverPage : UserControl
    {
        private readonly DataGrid? _dataGrid;

        public SelectionFastIndexResolverPage()
        {
            InitializeComponent();
            _dataGrid = this.FindControl<DataGrid>("ResolverDataGrid");
            AttachedToVisualTree += (_, _) =>
            {
                DataContext ??= new SelectionFastIndexResolverViewModel();
                ApplyResolver();
            };
            DataContextChanged += (_, _) => ApplyResolver();
        }

        private void ApplyResolver()
        {
            if (_dataGrid == null)
            {
                return;
            }

            _dataGrid.ReferenceIndexResolver = (DataContext as SelectionFastIndexResolverViewModel)?.ReferenceIndexResolver;
        }
    }
}
