using Avalonia.Controls.DataGridHierarchical;
using Avalonia.Diagnostics.ViewModels;

namespace Avalonia.Diagnostics.Services
{
    internal interface IResourceHierarchyModelFactory
    {
        IHierarchicalModel Create(ResourceTreeNode[] roots);
    }
}
