using System;

namespace Avalonia.Diagnostics.ViewModels
{
    internal abstract class ResourceTreeNode : ViewModelBase, IDisposable
    {
        private bool _isExpanded;

        protected ResourceTreeNode(
            ResourceTreeNode? parent,
            string name,
            string? secondaryText = null,
            string? valuePreview = null,
            string? valueType = null,
            object? source = null)
        {
            Parent = parent;
            Name = name;
            SecondaryText = secondaryText;
            ValuePreview = valuePreview;
            ValueType = valueType;
            Source = source;
        }

        public ResourceTreeNode? Parent { get; }
        public string Name { get; }
        public string? SecondaryText { get; }
        public string? ValuePreview { get; }
        public string? ValueType { get; }
        public object? Source { get; }

        public abstract ResourceTreeNodeCollection Children { get; }

        public bool IsExpanded
        {
            get => _isExpanded;
            set => RaiseAndSetIfChanged(ref _isExpanded, value);
        }

        public virtual void Dispose()
        {
            Children.Dispose();
        }
    }
}
