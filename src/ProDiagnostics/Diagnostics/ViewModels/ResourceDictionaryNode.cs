using System;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Diagnostics.Services;

namespace Avalonia.Diagnostics.ViewModels
{
    internal sealed class ResourceDictionaryNode : ResourceTreeNode
    {
        public ResourceDictionaryNode(
            IResourceDictionary dictionary,
            ResourceTreeNode parent,
            string name,
            IResourceTreeNodeFactory factory,
            IResourceNodeFormatter formatter)
            : base(parent, name, formatter.FormatProviderSecondaryText(dictionary, name), source: dictionary)
        {
            Dictionary = dictionary;
            Children = new ResourceDictionaryNodeCollection(this, dictionary, factory, formatter);
        }

        public IResourceDictionary Dictionary { get; }

        public int EntryCount => Dictionary.Count;
        public int MergedCount => Dictionary.MergedDictionaries.Count;
        public int ThemeCount => Dictionary.ThemeDictionaries.Count;
        public string? OwnerTypeName => Dictionary.Owner?.GetType().Name;

        public override ResourceTreeNodeCollection Children { get; }

        private sealed class ResourceDictionaryNodeCollection : ResourceTreeNodeCollection
        {
            private readonly IResourceDictionary _dictionary;
            private readonly IResourceTreeNodeFactory _factory;
            private readonly IResourceNodeFormatter _formatter;
            private AvaloniaList<ResourceTreeNode>? _nodes;
            private IResourceHost? _owner;

            public ResourceDictionaryNodeCollection(
                ResourceTreeNode owner,
                IResourceDictionary dictionary,
                IResourceTreeNodeFactory factory,
                IResourceNodeFormatter formatter)
                : base(owner)
            {
                _dictionary = dictionary;
                _factory = factory;
                _formatter = formatter;
            }

            protected override void Initialize(AvaloniaList<ResourceTreeNode> nodes)
            {
                _nodes = nodes;
                _dictionary.OwnerChanged += OwnerChanged;
                AttachOwner(_dictionary.Owner);
                RefreshNodes();
            }

            public override void Dispose()
            {
                _dictionary.OwnerChanged -= OwnerChanged;
                DetachOwner();
                base.Dispose();
            }

            private void OwnerChanged(object? sender, EventArgs e)
            {
                DetachOwner();
                AttachOwner(_dictionary.Owner);
                RefreshNodes();
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
                RefreshNodes();
            }

            private void RefreshNodes()
            {
                if (_nodes is null)
                {
                    return;
                }

                DisposeNodes(_nodes);
                _nodes.Clear();

                if (_dictionary.MergedDictionaries.Count > 0)
                {
                    _nodes.Add(new ResourceMergedDictionariesNode(
                        _dictionary.MergedDictionaries,
                        Owner,
                        _factory));
                }

                if (_dictionary.ThemeDictionaries.Count > 0)
                {
                    _nodes.Add(new ResourceThemeDictionariesNode(
                        _dictionary.ThemeDictionaries,
                        Owner,
                        _factory,
                        _formatter));
                }

                foreach (var entry in _dictionary)
                {
                    if (entry.Value is IResourceProvider provider)
                    {
                        var keyDisplay = _formatter.FormatKey(entry.Key);
                        _nodes.Add(_factory.CreateProviderNode(provider, Owner, keyDisplay));
                    }
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
