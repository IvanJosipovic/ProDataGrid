using Avalonia.Diagnostics.Services;

namespace Avalonia.Diagnostics.ViewModels
{
    internal sealed class ResourceEntryNode : ResourceTreeNode
    {
        public ResourceEntryNode(
            object key,
            object? value,
            ResourceTreeNode parent,
            string keyDisplay,
            ResourceValueDescriptor valueDescriptor)
            : base(parent, keyDisplay, valueDescriptor.TypeName, valueDescriptor.Preview, valueDescriptor.TypeName, value)
        {
            Key = key;
            Value = value;
            KeyDisplay = keyDisplay;
            KeyTypeName = key?.GetType().Name ?? "null";
            ValueTypeName = valueDescriptor.TypeName;
            ValuePreviewText = valueDescriptor.Preview;
            IsDeferred = valueDescriptor.IsDeferred;
            Children = ResourceTreeNodeCollection.Empty;
        }

        public object Key { get; }
        public object? Value { get; }
        public string KeyDisplay { get; }
        public string KeyTypeName { get; }
        public string ValueTypeName { get; }
        public string ValuePreviewText { get; }
        public bool IsDeferred { get; }

        public override ResourceTreeNodeCollection Children { get; }
    }
}
