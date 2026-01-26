using Avalonia;
using Avalonia.Diagnostics.ViewModels;

namespace Avalonia.Diagnostics.Services
{
    internal interface IResourceTreeNodeProvider
    {
        ResourceTreeNode[] Create(AvaloniaObject root);
    }
}
