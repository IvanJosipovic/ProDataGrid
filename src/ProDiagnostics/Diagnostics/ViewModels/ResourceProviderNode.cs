using Avalonia.Controls;

namespace Avalonia.Diagnostics.ViewModels
{
    internal sealed class ResourceProviderNode : ResourceTreeNode
    {
        public ResourceProviderNode(IResourceProvider provider, ResourceTreeNode parent, string name, string? secondaryText)
            : base(parent, name, secondaryText, source: provider)
        {
            Provider = provider;
            ProviderTypeName = provider.GetType().Name;
            Children = ResourceTreeNodeCollection.Empty;
        }

        public IResourceProvider Provider { get; }
        public string ProviderTypeName { get; }

        public override ResourceTreeNodeCollection Children { get; }
    }
}
