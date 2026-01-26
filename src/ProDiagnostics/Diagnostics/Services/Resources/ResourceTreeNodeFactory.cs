using Avalonia.Controls;
using Avalonia.Diagnostics.ViewModels;
using Avalonia.Styling;

namespace Avalonia.Diagnostics.Services
{
    internal sealed class ResourceTreeNodeFactory : IResourceTreeNodeFactory
    {
        private readonly IResourceNodeFormatter _formatter;

        public ResourceTreeNodeFactory(IResourceNodeFormatter formatter)
        {
            _formatter = formatter;
        }

        public ResourceHostNode CreateHostNode(IResourceHost host, ResourceTreeNode? parent)
        {
            return new ResourceHostNode(host, parent, this, _formatter);
        }

        public ResourceTreeNode CreateProviderNode(IResourceProvider provider, ResourceTreeNode parent, string? nameOverride = null)
        {
            if (provider is IResourceDictionary dictionary)
            {
                var name = nameOverride ?? _formatter.FormatProviderName(provider);
                return new ResourceDictionaryNode(dictionary, parent, name, this, _formatter);
            }

            if (provider is Styles styles)
            {
                var name = nameOverride ?? _formatter.FormatProviderName(provider);
                return new ResourceStylesNode(styles, parent, name, this, _formatter);
            }

            if (provider is StyleBase style)
            {
                var name = nameOverride ?? _formatter.FormatStyleName(style);
                return new ResourceStyleNode(style, parent, name, this, _formatter);
            }

            var fallbackName = nameOverride ?? _formatter.FormatProviderName(provider);
            var secondaryText = _formatter.FormatProviderSecondaryText(provider, fallbackName);
            return new ResourceProviderNode(provider, parent, fallbackName, secondaryText);
        }

        public ResourceTreeNode CreateEntryNode(object key, object? value, ResourceTreeNode parent)
        {
            var keyDisplay = _formatter.FormatKey(key);
            var valueDescriptor = _formatter.DescribeValue(value);
            if (value is IResourceProvider provider)
            {
                return new ResourceEntryProviderNode(key, provider, parent, keyDisplay, valueDescriptor, this);
            }

            return new ResourceEntryNode(key, value, parent, keyDisplay, valueDescriptor);
        }
    }
}
