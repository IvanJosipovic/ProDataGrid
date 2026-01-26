using System;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;

namespace Avalonia.Diagnostics.Services
{
    internal sealed class ResourceNodeFormatter : IResourceNodeFormatter
    {
        private const int PreviewLimit = 120;

        public string FormatHostName(IResourceHost host)
        {
            if (host is Application app)
            {
                return string.IsNullOrWhiteSpace(app.Name)
                    ? "Application"
                    : $"Application ({app.Name})";
            }

            if (host is INamed named && !string.IsNullOrWhiteSpace(named.Name))
            {
                return $"{host.GetType().Name} #{named.Name}";
            }

            return host.GetType().Name;
        }

        public string FormatProviderName(IResourceProvider provider)
        {
            if (provider is StyleBase style)
            {
                return FormatStyleName(style);
            }

            var providerType = provider.GetType();

            if (provider is ResourceDictionary && providerType != typeof(ResourceDictionary))
            {
                return providerType.Name;
            }

            if (provider is Styles && providerType != typeof(Styles))
            {
                return providerType.Name;
            }

            return provider switch
            {
                ResourceDictionary => "ResourceDictionary",
                Styles => "Styles",
                _ => providerType.Name
            };
        }

        public string? FormatProviderSecondaryText(IResourceProvider provider, string name)
        {
            var typeName = provider.GetType().Name;
            var source = TryGetProviderSource(provider);

            if (!string.IsNullOrWhiteSpace(source))
            {
                if (!string.Equals(typeName, name, StringComparison.Ordinal))
                {
                    return $"{typeName} · {source}";
                }

                return source;
            }

            if (string.Equals(typeName, name, StringComparison.Ordinal))
            {
                return null;
            }

            return typeName;
        }

        public string FormatStyleName(IStyle style)
        {
            return style switch
            {
                ControlTheme theme => theme.TargetType?.Name ?? "ControlTheme",
                Style selectorStyle => selectorStyle.ToString(),
                _ => style.GetType().Name
            };
        }

        public string FormatThemeVariant(ThemeVariant variant) => variant.ToString();

        public string FormatKey(object key)
        {
            if (key is null)
            {
                return "(null)";
            }

            if (key is Type type)
            {
                return type.FullName ?? type.Name;
            }

            return key.ToString() ?? key.GetType().Name;
        }

        public ResourceValueDescriptor DescribeValue(object? value)
        {
            if (value is null)
            {
                return new ResourceValueDescriptor("(null)", "null", false);
            }

            if (ReferenceEquals(value, AvaloniaProperty.UnsetValue))
            {
                return new ResourceValueDescriptor("(unset)", "UnsetValue", false);
            }

            if (value is IDeferredContent)
            {
                return new ResourceValueDescriptor("[Deferred]", value.GetType().Name, true);
            }

            var preview = NormalizePreview(value.ToString());
            var typeName = value.GetType().Name;
            return new ResourceValueDescriptor(preview, typeName, false);
        }

        private static string NormalizePreview(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            var normalized = text.Replace('\r', ' ').Replace('\n', ' ');
            if (normalized.Length > PreviewLimit)
            {
                return normalized.Substring(0, PreviewLimit) + "...";
            }

            return normalized;
        }

        private static string? TryGetProviderSource(IResourceProvider provider)
        {
            var property = provider.GetType().GetProperty("Source", BindingFlags.Instance | BindingFlags.Public);
            if (property?.GetValue(provider) is Uri uri)
            {
                return uri.ToString();
            }

            if (property?.GetValue(provider) is string text)
            {
                return text;
            }

            return null;
        }
    }
}
