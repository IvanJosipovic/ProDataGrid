using System;
using System.Collections.Generic;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Diagnostics.Services;
using Avalonia.Reactive;

namespace Avalonia.Diagnostics.ViewModels
{
    internal sealed class ResourceMergedDictionariesNode : ResourceTreeNode
    {
        public ResourceMergedDictionariesNode(
            IList<IResourceProvider> providers,
            ResourceTreeNode parent,
            IResourceTreeNodeFactory factory)
            : base(parent, "Merged Dictionaries", providers.GetType().Name, source: providers)
        {
            Providers = providers;
            Children = new ResourceProviderListNodeCollection(this, providers, factory);
        }

        public IList<IResourceProvider> Providers { get; }
        public int ProviderCount => Providers.Count;

        public override ResourceTreeNodeCollection Children { get; }

        private sealed class ResourceProviderListNodeCollection : ResourceTreeNodeCollection
        {
            private readonly IList<IResourceProvider> _providers;
            private readonly IResourceTreeNodeFactory _factory;
            private IDisposable? _subscription;
            private AvaloniaList<ResourceTreeNode>? _nodes;

            public ResourceProviderListNodeCollection(
                ResourceTreeNode owner,
                IList<IResourceProvider> providers,
                IResourceTreeNodeFactory factory)
                : base(owner)
            {
                _providers = providers;
                _factory = factory;
            }

            protected override void Initialize(AvaloniaList<ResourceTreeNode> nodes)
            {
                _nodes = nodes;

                if (_providers is IAvaloniaReadOnlyList<IResourceProvider> list)
                {
                    _subscription = list.ForEachItem(
                        (i, item) => nodes.Insert(i, _factory.CreateProviderNode(item, Owner)),
                        (i, item) => RemoveNode(i),
                        () => ResetNodes());
                }
                else
                {
                    foreach (var provider in _providers)
                    {
                        nodes.Add(_factory.CreateProviderNode(provider, Owner));
                    }
                }
            }

            public override void Dispose()
            {
                _subscription?.Dispose();
                base.Dispose();
            }

            private void RemoveNode(int index)
            {
                if (_nodes == null || index < 0 || index >= _nodes.Count)
                {
                    return;
                }

                var node = _nodes[index];
                _nodes.RemoveAt(index);
                node.Dispose();
            }

            private void ResetNodes()
            {
                if (_nodes is null)
                {
                    return;
                }

                DisposeNodes(_nodes);
                _nodes.Clear();

                foreach (var provider in _providers)
                {
                    _nodes.Add(_factory.CreateProviderNode(provider, Owner));
                }
            }

            private static void DisposeNodes(AvaloniaList<ResourceTreeNode> nodes)
            {
                for (var i = 0; i < nodes.Count; i++)
                {
                    nodes[i].Dispose();
                }
            }
        }
    }
}
