using System;
using Avalonia.Collections;
using Avalonia.Controls.Templates;
using Avalonia.Diagnostics.Services;
using Avalonia.Reactive;

namespace Avalonia.Diagnostics.ViewModels
{
    internal sealed class ResourceDataTemplatesNode : ResourceTreeNode
    {
        public ResourceDataTemplatesNode(
            DataTemplates templates,
            ResourceTreeNode parent,
            string name,
            IResourceNodeFormatter formatter)
            : base(parent, name, templates.GetType().Name, source: templates)
        {
            Templates = templates;
            Children = new ResourceDataTemplatesNodeCollection(this, templates, formatter);
        }

        public DataTemplates Templates { get; }
        public int TemplateCount => Templates.Count;

        public override ResourceTreeNodeCollection Children { get; }

        private sealed class ResourceDataTemplatesNodeCollection : ResourceTreeNodeCollection
        {
            private readonly DataTemplates _templates;
            private readonly IResourceNodeFormatter _formatter;
            private IDisposable? _subscription;
            private AvaloniaList<ResourceTreeNode>? _nodes;

            public ResourceDataTemplatesNodeCollection(
                ResourceTreeNode owner,
                DataTemplates templates,
                IResourceNodeFormatter formatter)
                : base(owner)
            {
                _templates = templates;
                _formatter = formatter;
            }

            protected override void Initialize(AvaloniaList<ResourceTreeNode> nodes)
            {
                _nodes = nodes;

                if (_templates is IAvaloniaReadOnlyList<IDataTemplate> list)
                {
                    _subscription = list.ForEachItem(
                        (i, item) => nodes.Insert(i, CreateNode(item)),
                        (i, item) => RemoveNode(i),
                        () => ResetNodes());
                }
                else
                {
                    foreach (var template in _templates)
                    {
                        nodes.Add(CreateNode(template));
                    }
                }
            }

            public override void Dispose()
            {
                _subscription?.Dispose();
                base.Dispose();
            }

            private ResourceTreeNode CreateNode(IDataTemplate template)
            {
                return new ResourceDataTemplateNode(template, Owner, _formatter);
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

                foreach (var template in _templates)
                {
                    _nodes.Add(CreateNode(template));
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
