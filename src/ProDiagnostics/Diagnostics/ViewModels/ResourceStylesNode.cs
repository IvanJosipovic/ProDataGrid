using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Diagnostics.Services;
using Avalonia.Reactive;
using Avalonia.Styling;

namespace Avalonia.Diagnostics.ViewModels
{
    internal sealed class ResourceStylesNode : ResourceTreeNode
    {
        public ResourceStylesNode(
            Styles styles,
            ResourceTreeNode parent,
            string name,
            IResourceTreeNodeFactory factory,
            IResourceNodeFormatter formatter)
            : base(parent, name, formatter.FormatProviderSecondaryText(styles, name), source: styles)
        {
            Styles = styles;
            Children = new ResourceStylesNodeCollection(this, styles, factory, formatter);
        }

        public Styles Styles { get; }
        public int StyleCount => Styles.Count;
        public bool HasResources => ((IResourceNode)Styles).HasResources;

        public override ResourceTreeNodeCollection Children { get; }

        private sealed class ResourceStylesNodeCollection : ResourceTreeNodeCollection
        {
            private readonly Styles _styles;
            private readonly IResourceTreeNodeFactory _factory;
            private readonly IResourceNodeFormatter _formatter;
            private readonly List<ResourceTreeNode> _resourceNodes = new();
            private readonly List<ResourceTreeNode> _styleNodes = new();
            private IDisposable? _stylesSubscription;
            private AvaloniaList<ResourceTreeNode>? _nodes;
            private IResourceHost? _owner;

            public ResourceStylesNodeCollection(
                ResourceTreeNode owner,
                Styles styles,
                IResourceTreeNodeFactory factory,
                IResourceNodeFormatter formatter)
                : base(owner)
            {
                _styles = styles;
                _factory = factory;
                _formatter = formatter;
            }

            protected override void Initialize(AvaloniaList<ResourceTreeNode> nodes)
            {
                _nodes = nodes;
                _styles.OwnerChanged += OwnerChanged;
                AttachOwner(_styles.Owner);

                RefreshResourceNodes();

                ResetStyleNodes();
                SubscribeToStylesChanged();
            }

            public override void Dispose()
            {
                _styles.OwnerChanged -= OwnerChanged;
                DetachOwner();
                _stylesSubscription?.Dispose();
                base.Dispose();
            }

            private void SubscribeToStylesChanged()
            {
                if (_styles is not INotifyCollectionChanged notifyCollectionChanged)
                {
                    return;
                }

                NotifyCollectionChangedEventHandler handler = (_, __) => ResetStyleNodes();
                notifyCollectionChanged.CollectionChanged += handler;
                _stylesSubscription = Disposable.Create(() => notifyCollectionChanged.CollectionChanged -= handler);
            }

            private void OwnerChanged(object? sender, EventArgs e)
            {
                DetachOwner();
                AttachOwner(_styles.Owner);
                RefreshResourceNodes();
            }

            private void AttachOwner(IResourceHost? owner)
            {
                _owner = owner;
                if (_owner != null)
                {
                    _owner.ResourcesChanged += OwnerResourcesChanged;
                }
            }

            private void DetachOwner()
            {
                if (_owner != null)
                {
                    _owner.ResourcesChanged -= OwnerResourcesChanged;
                    _owner = null;
                }
            }

            private void OwnerResourcesChanged(object? sender, ResourcesChangedEventArgs e)
            {
                RefreshResourceNodes();
            }

            private void InsertStyleNode(int index, IStyle style)
            {
                var node = CreateStyleNode(style);
                _styleNodes.Insert(index, node);

                if (_nodes != null)
                {
                    _nodes.Insert(_resourceNodes.Count + index, node);
                }
            }

            private void RemoveStyleNode(int index)
            {
                if (index < 0 || index >= _styleNodes.Count)
                {
                    return;
                }

                var node = _styleNodes[index];
                _styleNodes.RemoveAt(index);

                if (_nodes != null && _resourceNodes.Count + index < _nodes.Count)
                {
                    _nodes.RemoveAt(_resourceNodes.Count + index);
                }

                node.Dispose();
            }

            private void ResetStyleNodes()
            {
                DisposeNodes(_styleNodes);
                _styleNodes.Clear();

                foreach (var style in _styles)
                {
                    _styleNodes.Add(CreateStyleNode(style));
                }

                RebuildNodes();
            }

            private void RefreshResourceNodes()
            {
                DisposeNodes(_resourceNodes);
                _resourceNodes.Clear();

                if (TryGetResourcesDictionary(_styles, out var dictionary) && dictionary is not null)
                {
                    if (HasDictionaryContent(dictionary))
                    {
                        _resourceNodes.Add(new ResourceDictionaryNode(dictionary, Owner, "Resources", _factory, _formatter));
                    }
                }

                RebuildNodes();
            }

            private void RebuildNodes()
            {
                if (_nodes is null)
                {
                    return;
                }

                _nodes.Clear();

                for (var i = 0; i < _resourceNodes.Count; i++)
                {
                    _nodes.Add(_resourceNodes[i]);
                }

                for (var i = 0; i < _styleNodes.Count; i++)
                {
                    _nodes.Add(_styleNodes[i]);
                }
            }

            private ResourceTreeNode CreateStyleNode(IStyle style)
            {
                if (style is IResourceProvider provider)
                {
                    return _factory.CreateProviderNode(provider, Owner);
                }

                return new ResourceStyleLeafNode(style, Owner, _formatter.FormatStyleName(style));
            }

            private static bool TryGetResourcesDictionary(Styles styles, out IResourceDictionary? dictionary)
            {
                dictionary = null;
                dictionary = styles.Resources;
                return true;
            }

            private static bool HasDictionaryContent(IResourceDictionary dictionary)
            {
                if (dictionary.Count > 0)
                {
                    return true;
                }

                if (dictionary.MergedDictionaries.Count > 0)
                {
                    return true;
                }

                if (dictionary.ThemeDictionaries.Count > 0)
                {
                    return true;
                }

                return false;
            }

            private static void DisposeNodes(IReadOnlyList<ResourceTreeNode> nodes)
            {
                for (var i = 0; i < nodes.Count; i++)
                {
                    nodes[i].Dispose();
                }
            }
        }
    }
}
