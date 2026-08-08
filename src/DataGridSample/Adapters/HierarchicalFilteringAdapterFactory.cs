using Avalonia.Controls;
using Avalonia.Controls.DataGridFiltering;

namespace DataGridSample.Adapters
{
    public sealed class HierarchicalFilteringAdapterFactory : IDataGridFilteringAdapterFactory
    {
        public DataGridFilteringAdapter Create(DataGrid grid, IFilteringModel model)
        {
            return new DataGridHierarchicalFilteringAdapter(
                model,
                () => grid.ColumnDefinitions,
                grid.HierarchicalModel,
                DataGridHierarchyFilterPolicy.KeepAncestorsOfMatches,
                grid.FastPathOptions);
        }
    }
}
