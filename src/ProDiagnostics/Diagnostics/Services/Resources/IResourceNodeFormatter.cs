using Avalonia.Controls;
using Avalonia.Styling;

namespace Avalonia.Diagnostics.Services
{
    internal readonly struct ResourceValueDescriptor
    {
        public ResourceValueDescriptor(string preview, string typeName, bool isDeferred)
        {
            Preview = preview;
            TypeName = typeName;
            IsDeferred = isDeferred;
        }

        public string Preview { get; }
        public string TypeName { get; }
        public bool IsDeferred { get; }
    }

    internal interface IResourceNodeFormatter
    {
        string FormatHostName(IResourceHost host);
        string FormatProviderName(IResourceProvider provider);
        string? FormatProviderSecondaryText(IResourceProvider provider, string name);
        string FormatStyleName(IStyle style);
        string FormatThemeVariant(ThemeVariant variant);
        string FormatKey(object key);
        ResourceValueDescriptor DescribeValue(object? value);
    }
}
