using Avalonia.Controls;
using Avalonia.Diagnostics.Services;
using Avalonia.Styling;
using Avalonia.Collections;

namespace Avalonia.Diagnostics.ViewModels
{
    internal sealed class ResourceThemeVariantNode : ResourceTreeNode
    {
        public ResourceThemeVariantNode(
            ThemeVariant variant,
            IThemeVariantProvider provider,
            ResourceTreeNode parent,
            IResourceTreeNodeFactory factory,
            IResourceNodeFormatter formatter)
            : base(parent, $"Theme: {formatter.FormatThemeVariant(variant)}", provider.GetType().Name, source: provider)
        {
            Variant = variant;
            VariantDisplay = formatter.FormatThemeVariant(variant);
            Provider = provider;
            Children = new SingleProviderNodeCollection(this, provider, factory);
        }

        public ThemeVariant Variant { get; }
        public string VariantDisplay { get; }
        public IThemeVariantProvider Provider { get; }

        public override ResourceTreeNodeCollection Children { get; }

        private sealed class SingleProviderNodeCollection : ResourceTreeNodeCollection
        {
            private readonly IResourceProvider _provider;
            private readonly IResourceTreeNodeFactory _factory;

            public SingleProviderNodeCollection(
                ResourceTreeNode owner,
                IResourceProvider provider,
                IResourceTreeNodeFactory factory)
                : base(owner)
            {
                _provider = provider;
                _factory = factory;
            }

            protected override void Initialize(AvaloniaList<ResourceTreeNode> nodes)
            {
                nodes.Add(_factory.CreateProviderNode(_provider, Owner));
            }
        }
    }
}
