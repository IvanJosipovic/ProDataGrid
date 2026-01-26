using Avalonia.Styling;

namespace Avalonia.Diagnostics.ViewModels
{
    internal sealed class ResourceStyleLeafNode : ResourceTreeNode
    {
        public ResourceStyleLeafNode(IStyle style, ResourceTreeNode parent, string name)
            : base(parent, name, style.GetType().Name, source: style)
        {
            Style = style;
            StyleTypeName = style.GetType().Name;
            Children = ResourceTreeNodeCollection.Empty;
        }

        public IStyle Style { get; }
        public string StyleTypeName { get; }

        public override ResourceTreeNodeCollection Children { get; }
    }
}
