using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Diagnostics.Services;
using Avalonia.LogicalTree;
using Avalonia.Reactive;
using Avalonia.Styling;

namespace Avalonia.Diagnostics.ViewModels
{
    internal sealed class ResourceHostNode : ResourceTreeNode
    {
        public ResourceHostNode(
            IResourceHost host,
            ResourceTreeNode? parent,
            IResourceTreeNodeFactory factory,
            IResourceNodeFormatter formatter)
            : base(parent, formatter.FormatHostName(host), host.GetType().Name, source: host)
        {
            Host = host;
            HostTypeName = host.GetType().Name;
            HostName = ExtractHostName(host);
            Children = new ResourceHostNodeCollection(this, host, factory, formatter);
        }

        public IResourceHost Host { get; }
        public string HostTypeName { get; }
        public string? HostName { get; }
        public bool HasResources => ((IResourceNode)Host).HasResources;
        public bool HasStyles => HasInitializedStyles(Host);
        public bool HasDataTemplates => HasInitializedTemplates(Host);
        public int DataTemplateCount => GetDataTemplateCount(Host);

        public override ResourceTreeNodeCollection Children { get; }

        private static string? ExtractHostName(IResourceHost host)
        {
            if (host is Application app)
            {
                return string.IsNullOrWhiteSpace(app.Name) ? null : app.Name;
            }

            return host is INamed named ? named.Name : null;
        }

        private static bool HasInitializedStyles(IResourceHost host)
        {
            if (host is Application)
            {
                return true;
            }

            if (host is StyledElement styledElement)
            {
                return ((IStyleHost)styledElement).IsStylesInitialized;
            }

            return false;
        }

        private sealed class ResourceHostNodeCollection : ResourceTreeNodeCollection
        {
            private readonly IResourceHost _host;
            private readonly IResourceTreeNodeFactory _factory;
            private readonly IResourceNodeFormatter _formatter;
            private readonly List<ResourceTreeNode> _resourceNodes = new();
            private readonly List<ResourceTreeNode> _logicalNodes = new();
            private IDisposable? _logicalSubscription;
            private AvaloniaList<ResourceTreeNode>? _nodes;

            public ResourceHostNodeCollection(
                ResourceTreeNode owner,
                IResourceHost host,
                IResourceTreeNodeFactory factory,
                IResourceNodeFormatter formatter)
                : base(owner)
            {
                _host = host;
                _factory = factory;
                _formatter = formatter;
            }

            protected override void Initialize(AvaloniaList<ResourceTreeNode> nodes)
            {
                _nodes = nodes;
                RefreshResourceNodes();
                InitializeLogicalNodes();
                _host.ResourcesChanged += HostResourcesChanged;
            }

            public override void Dispose()
            {
                _host.ResourcesChanged -= HostResourcesChanged;
                _logicalSubscription?.Dispose();
                base.Dispose();
            }

            private void InitializeLogicalNodes()
            {
                if (_host is not ILogical logical)
                {
                    return;
                }

                _logicalSubscription = logical.LogicalChildren.ForEachItem(
                    (i, item) => InsertLogicalChild(i, item),
                    (i, item) => RemoveLogicalChild(i),
                    () => ResetLogicalChildren(logical));
            }

            private void InsertLogicalChild(int index, ILogical item)
            {
                if (item is not IResourceHost host)
                {
                    return;
                }

                var node = _factory.CreateHostNode(host, Owner);
                _logicalNodes.Insert(index, node);

                if (_nodes != null)
                {
                    _nodes.Insert(_resourceNodes.Count + index, node);
                }
            }

            private void RemoveLogicalChild(int index)
            {
                if (index < 0 || index >= _logicalNodes.Count)
                {
                    return;
                }

                var node = _logicalNodes[index];
                _logicalNodes.RemoveAt(index);

                if (_nodes != null && _resourceNodes.Count + index < _nodes.Count)
                {
                    _nodes.RemoveAt(_resourceNodes.Count + index);
                }

                node.Dispose();
            }

            private void ResetLogicalChildren(ILogical logical)
            {
                DisposeNodes(_logicalNodes);
                _logicalNodes.Clear();

                foreach (var child in logical.LogicalChildren)
                {
                    if (child is IResourceHost childHost)
                    {
                        _logicalNodes.Add(_factory.CreateHostNode(childHost, Owner));
                    }
                }

                RebuildNodes();
            }

            private void HostResourcesChanged(object? sender, ResourcesChangedEventArgs e)
            {
                RefreshResourceNodes();
            }

            private void RefreshResourceNodes()
            {
                DisposeNodes(_resourceNodes);
                _resourceNodes.Clear();

                if (TryCreateResourcesNode() is { } resourcesNode)
                {
                    _resourceNodes.Add(resourcesNode);
                }

                if (TryCreateStylesNode() is { } stylesNode)
                {
                    _resourceNodes.Add(stylesNode);
                }

                if (TryCreateDataTemplatesNode() is { } templatesNode)
                {
                    _resourceNodes.Add(templatesNode);
                }

                RebuildNodes();
            }

            private ResourceTreeNode? TryCreateResourcesNode()
            {
                if (TryGetResourcesDictionary(_host, out var dictionary) && dictionary is not null)
                {
                    if (HasDictionaryContent(dictionary))
                    {
                        return new ResourceDictionaryNode(dictionary, Owner, "Resources", _factory, _formatter);
                    }
                }

                return null;
            }

            private ResourceTreeNode? TryCreateStylesNode()
            {
                if (TryGetStyles(_host, out var styles) && styles is not null)
                {
                    if (HasStylesContent(styles))
                    {
                        return new ResourceStylesNode(styles, Owner, "Styles", _factory, _formatter);
                    }
                }

                return null;
            }

            private ResourceTreeNode? TryCreateDataTemplatesNode()
            {
                if (TryGetDataTemplates(_host, out var templates) && templates is not null)
                {
                    if (templates.Count > 0)
                    {
                        return new ResourceDataTemplatesNode(templates, Owner, "DataTemplates", _formatter);
                    }
                }

                return null;
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

                for (var i = 0; i < _logicalNodes.Count; i++)
                {
                    _nodes.Add(_logicalNodes[i]);
                }
            }

            private static bool TryGetResourcesDictionary(IResourceHost host, out IResourceDictionary? dictionary)
            {
                dictionary = null;

                if (host is Application application)
                {
                    dictionary = application.Resources;
                    return true;
                }

                if (host is StyledElement styledElement)
                {
                    dictionary = styledElement.Resources;
                    return true;
                }

                return false;
            }

            private static bool TryGetStyles(IResourceHost host, out Styles? styles)
            {
                styles = null;

                if (host is Application application)
                {
                    styles = application.Styles;
                    return true;
                }

                if (host is StyledElement styledElement)
                {
                    if (!((IStyleHost)styledElement).IsStylesInitialized)
                    {
                        return false;
                    }

                    styles = styledElement.Styles;
                    return true;
                }

                return false;
            }

            private static bool TryGetDataTemplates(IResourceHost host, out DataTemplates? templates)
            {
                templates = null;

                if (host is IDataTemplateHost templateHost)
                {
                    if (!templateHost.IsDataTemplatesInitialized)
                    {
                        return false;
                    }

                    templates = templateHost.DataTemplates;
                    return true;
                }

                return false;
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

            private static bool HasStylesContent(Styles styles)
            {
                return styles.Count > 0 || ((IResourceNode)styles).HasResources;
            }

            private static void DisposeNodes(IReadOnlyList<ResourceTreeNode> nodes)
            {
                for (var i = 0; i < nodes.Count; i++)
                {
                    nodes[i].Dispose();
                }
            }
        }

        private static bool HasInitializedTemplates(IResourceHost host)
        {
            if (host is IDataTemplateHost templateHost)
            {
                return templateHost.IsDataTemplatesInitialized && templateHost.DataTemplates.Count > 0;
            }

            return false;
        }

        private static int GetDataTemplateCount(IResourceHost host)
        {
            if (host is IDataTemplateHost templateHost && templateHost.IsDataTemplatesInitialized)
            {
                return templateHost.DataTemplates.Count;
            }

            return 0;
        }
    }
}
