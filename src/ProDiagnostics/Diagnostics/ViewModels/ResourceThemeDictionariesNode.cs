using System;
using System.Collections.Generic;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Diagnostics.Services;
using Avalonia.Reactive;
using Avalonia.Styling;

namespace Avalonia.Diagnostics.ViewModels
{
    internal sealed class ResourceThemeDictionariesNode : ResourceTreeNode
    {
        public ResourceThemeDictionariesNode(
            IDictionary<ThemeVariant, IThemeVariantProvider> providers,
            ResourceTreeNode parent,
            IResourceTreeNodeFactory factory,
            IResourceNodeFormatter formatter)
            : base(parent, "Theme Dictionaries", providers.GetType().Name, source: providers)
        {
            Providers = providers;
            Children = new ResourceThemeDictionariesNodeCollection(this, providers, factory, formatter);
        }

        public IDictionary<ThemeVariant, IThemeVariantProvider> Providers { get; }
        public int ProviderCount => Providers.Count;

        public override ResourceTreeNodeCollection Children { get; }

        private sealed class ResourceThemeDictionariesNodeCollection : ResourceTreeNodeCollection
        {
            private readonly IDictionary<ThemeVariant, IThemeVariantProvider> _providers;
            private readonly IResourceTreeNodeFactory _factory;
            private readonly IResourceNodeFormatter _formatter;
            private readonly Dictionary<ThemeVariant, ResourceThemeVariantNode> _nodesByKey = new();
            private IDisposable? _subscription;
            private AvaloniaList<ResourceTreeNode>? _nodes;

            public ResourceThemeDictionariesNodeCollection(
                ResourceTreeNode owner,
                IDictionary<ThemeVariant, IThemeVariantProvider> providers,
                IResourceTreeNodeFactory factory,
                IResourceNodeFormatter formatter)
                : base(owner)
            {
                _providers = providers;
                _factory = factory;
                _formatter = formatter;
            }

            protected override void Initialize(AvaloniaList<ResourceTreeNode> nodes)
            {
                _nodes = nodes;

                if (_providers is IAvaloniaReadOnlyDictionary<ThemeVariant, IThemeVariantProvider> dictionary)
                {
                    _subscription = dictionary.ForEachItem(
                        (key, value) => AddNode(key, value),
                        (key, value) => RemoveNode(key),
                        () => ResetNodes());
                }
                else
                {
                    foreach (var pair in _providers)
                    {
                        AddNode(pair.Key, pair.Value);
                    }
                }
            }

            public override void Dispose()
            {
                _subscription?.Dispose();
                base.Dispose();
            }

            private void AddNode(ThemeVariant variant, IThemeVariantProvider provider)
            {
                if (_nodes is null)
                {
                    return;
                }

                var node = new ResourceThemeVariantNode(variant, provider, Owner, _factory, _formatter);
                _nodesByKey[variant] = node;
                _nodes.Add(node);
            }

            private void RemoveNode(ThemeVariant variant)
            {
                if (_nodes is null)
                {
                    return;
                }

                if (_nodesByKey.TryGetValue(variant, out var node))
                {
                    _nodes.Remove(node);
                    node.Dispose();
                    _nodesByKey.Remove(variant);
                }
            }

            private void ResetNodes()
            {
                if (_nodes is null)
                {
                    return;
                }

                DisposeNodes(_nodes);
                _nodes.Clear();
                _nodesByKey.Clear();

                foreach (var pair in _providers)
                {
                    AddNode(pair.Key, pair.Value);
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
