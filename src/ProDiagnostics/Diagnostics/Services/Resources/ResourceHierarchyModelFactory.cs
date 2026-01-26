using Avalonia.Controls.DataGridHierarchical;
using Avalonia.Diagnostics.ViewModels;

namespace Avalonia.Diagnostics.Services
{
    internal sealed class ResourceHierarchyModelFactory : IResourceHierarchyModelFactory
    {
        public IHierarchicalModel Create(ResourceTreeNode[] roots)
        {
            var options = new HierarchicalOptions<ResourceTreeNode>
            {
                ChildrenSelector = node => node.Children,
                IsLeafSelector = node => node.Children.Count == 0,
                IsExpandedPropertyPath = nameof(ResourceTreeNode.IsExpanded),
                VirtualizeChildren = true,
                AllowExpandToItemSearch = true
            };

            var model = new HierarchicalModel<ResourceTreeNode>(options);
            model.SetRoots(roots);
            return model;
        }
    }
}
