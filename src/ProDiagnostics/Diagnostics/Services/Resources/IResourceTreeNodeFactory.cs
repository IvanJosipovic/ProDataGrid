using Avalonia.Controls;
using Avalonia.Diagnostics.ViewModels;

namespace Avalonia.Diagnostics.Services
{
    internal interface IResourceTreeNodeFactory
    {
        ResourceHostNode CreateHostNode(IResourceHost host, ResourceTreeNode? parent);
        ResourceTreeNode CreateProviderNode(IResourceProvider provider, ResourceTreeNode parent, string? nameOverride = null);
        ResourceTreeNode CreateEntryNode(object key, object? value, ResourceTreeNode parent);
    }
}
