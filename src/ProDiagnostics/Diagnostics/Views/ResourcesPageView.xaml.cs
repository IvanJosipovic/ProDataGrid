using Avalonia.Controls;
using Avalonia.Controls.DataGridHierarchical;
using Avalonia.Diagnostics.ViewModels;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

namespace Avalonia.Diagnostics.Views
{
    partial class ResourcesPageView : UserControl
    {
        private DataGridRow? _hovered;
        private DataGrid _tree;
        private System.IDisposable? _adorner;

        public ResourcesPageView()
        {
            InitializeComponent();
            _tree = this.GetControl<DataGrid>("resourcesTree");
        }

        protected void UpdateAdorner(object? sender, PointerEventArgs e)
        {
            if (e.Source is not StyledElement source)
            {
                return;
            }

            var row = source.FindLogicalAncestorOfType<DataGridRow>();
            if (row == _hovered)
            {
                return;
            }

            _adorner?.Dispose();

            if (row is null || row.OwningGrid != _tree)
            {
                _hovered = null;
                return;
            }

            _hovered = row;

            var node = ResolveNode(row.DataContext);
            var visual = ResolveVisual(node);
            var shouldVisualizeMarginPadding = (DataContext as ResourcesPageViewModel)?.MainView.ShouldVisualizeMarginPadding;
            if (visual is null || shouldVisualizeMarginPadding is null)
            {
                return;
            }

            _adorner = Controls.ControlHighlightAdorner.Add(visual, visualizeMarginPadding: shouldVisualizeMarginPadding == true);
        }

        private void RemoveAdorner(object? sender, PointerEventArgs e)
        {
            _adorner?.Dispose();
            _adorner = null;
        }

        private static ResourceTreeNode? ResolveNode(object? dataContext)
        {
            return dataContext switch
            {
                HierarchicalNode node => node.Item as ResourceTreeNode,
                ResourceTreeNode resourceNode => resourceNode,
                _ => null
            };
        }

        private static Visual? ResolveVisual(ResourceTreeNode? node)
        {
            if (node is null)
            {
                return null;
            }

            if (node is ResourceHostNode hostNode)
            {
                return hostNode.Host as Visual;
            }

            return node.Source as Visual;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
