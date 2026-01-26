using System;
using System.Collections.Generic;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Diagnostics.Services;
using Avalonia.Styling;

namespace Avalonia.Diagnostics.ViewModels
{
    internal sealed class ResourceStyleNode : ResourceTreeNode
    {
        public ResourceStyleNode(
            StyleBase style,
            ResourceTreeNode parent,
            string name,
            IResourceTreeNodeFactory factory,
            IResourceNodeFormatter formatter)
            : base(parent, name, style.GetType().Name, source: style)
        {
            Style = style;
            StyleTypeName = style.GetType().Name;
            StyleDescription = style.ToString();
            Children = new ResourceStyleNodeCollection(this, style, factory, formatter);
        }

        public StyleBase Style { get; }
        public string StyleTypeName { get; }
        public string StyleDescription { get; }
        public int ChildStyleCount => ((IStyle)Style).Children.Count;
        public bool HasResources => ((IResourceNode)Style).HasResources;

        public override ResourceTreeNodeCollection Children { get; }

        private sealed class ResourceStyleNodeCollection : ResourceTreeNodeCollection
        {
            private readonly StyleBase _style;
            private readonly IResourceTreeNodeFactory _factory;
            private readonly IResourceNodeFormatter _formatter;
            private readonly List<ResourceTreeNode> _resourceNodes = new();
            private readonly List<ResourceTreeNode> _childNodes = new();
            private AvaloniaList<ResourceTreeNode>? _nodes;
            private IResourceHost? _owner;

            public ResourceStyleNodeCollection(
                ResourceTreeNode owner,
                StyleBase style,
                IResourceTreeNodeFactory factory,
                IResourceNodeFormatter formatter)
                : base(owner)
            {
                _style = style;
                _factory = factory;
                _formatter = formatter;
            }

            protected override void Initialize(AvaloniaList<ResourceTreeNode> nodes)
            {
                _nodes = nodes;
                _style.OwnerChanged += OwnerChanged;
                AttachOwner(_style.Owner);

                RefreshResourceNodes();
                BuildChildNodes();
                RebuildNodes();
            }

            public override void Dispose()
            {
                _style.OwnerChanged -= OwnerChanged;
                DetachOwner();
                base.Dispose();
            }

            private void OwnerChanged(object? sender, EventArgs e)
            {
                DetachOwner();
                AttachOwner(_style.Owner);
                RefreshResourceNodes();
                RebuildNodes();
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
                RebuildNodes();
            }

            private void BuildChildNodes()
            {
                _childNodes.Clear();

                foreach (var child in ((IStyle)_style).Children)
                {
                    if (child is IResourceProvider provider)
                    {
                        _childNodes.Add(_factory.CreateProviderNode(provider, Owner));
                    }
                    else
                    {
                        _childNodes.Add(new ResourceStyleLeafNode(child, Owner, _formatter.FormatStyleName(child)));
                    }
                }
            }

            private void RefreshResourceNodes()
            {
                DisposeNodes(_resourceNodes);
                _resourceNodes.Clear();

                if (TryGetResourcesDictionary(_style, out var dictionary) && dictionary is not null)
                {
                    if (HasDictionaryContent(dictionary))
                    {
                        _resourceNodes.Add(new ResourceDictionaryNode(dictionary, Owner, "Resources", _factory, _formatter));
                    }
                }
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

                for (var i = 0; i < _childNodes.Count; i++)
                {
                    _nodes.Add(_childNodes[i]);
                }
            }

            private static bool TryGetResourcesDictionary(StyleBase style, out IResourceDictionary? dictionary)
            {
                dictionary = null;
                dictionary = style.Resources;
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
