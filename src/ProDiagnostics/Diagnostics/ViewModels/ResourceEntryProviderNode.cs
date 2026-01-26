using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Diagnostics.Services;

namespace Avalonia.Diagnostics.ViewModels
{
    internal sealed class ResourceEntryProviderNode : ResourceTreeNode
    {
        public ResourceEntryProviderNode(
            object key,
            IResourceProvider provider,
            ResourceTreeNode parent,
            string keyDisplay,
            ResourceValueDescriptor valueDescriptor,
            IResourceTreeNodeFactory factory)
            : base(parent, keyDisplay, valueDescriptor.TypeName, valueDescriptor.Preview, valueDescriptor.TypeName, provider)
        {
            Key = key;
            Provider = provider;
            KeyDisplay = keyDisplay;
            KeyTypeName = key?.GetType().Name ?? "null";
            ValueTypeName = valueDescriptor.TypeName;
            ValuePreviewText = valueDescriptor.Preview;
            IsDeferred = valueDescriptor.IsDeferred;
            ProviderTypeName = provider.GetType().Name;
            Children = new ResourceEntryProviderNodeCollection(this, provider, factory);
        }

        public object Key { get; }
        public IResourceProvider Provider { get; }
        public string KeyDisplay { get; }
        public string KeyTypeName { get; }
        public string ValueTypeName { get; }
        public string ValuePreviewText { get; }
        public bool IsDeferred { get; }
        public string ProviderTypeName { get; }

        public override ResourceTreeNodeCollection Children { get; }

        private sealed class ResourceEntryProviderNodeCollection : ResourceTreeNodeCollection
        {
            private readonly IResourceProvider _provider;
            private readonly IResourceTreeNodeFactory _factory;
            private ResourceTreeNode? _node;

            public ResourceEntryProviderNodeCollection(
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
                _node = _factory.CreateProviderNode(_provider, Owner);
                nodes.Add(_node);
            }
        }
    }
}
