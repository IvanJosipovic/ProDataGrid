using System;
using Avalonia.Controls.Templates;
using Avalonia.Diagnostics;
using Avalonia.Diagnostics.Services;

namespace Avalonia.Diagnostics.ViewModels
{
    internal sealed class ResourceDataTemplateNode : ResourceTreeNode
    {
        public ResourceDataTemplateNode(
            IDataTemplate template,
            ResourceTreeNode parent,
            IResourceNodeFormatter formatter)
            : base(parent,
                GetTemplateName(template),
                GetTemplateSecondaryText(template),
                formatter.DescribeValue(template).Preview,
                formatter.DescribeValue(template).TypeName,
                template)
        {
            Template = template;
            TemplateTypeName = template.GetType().Name;
            Description = template.ToString();
            IsRecycling = template is IRecyclingDataTemplate;
            IsTreeTemplate = template is ITreeDataTemplate;
            DataType = (template as ITypedDataTemplate)?.DataType;
            DataTypeName = DataType?.GetTypeName();
            Children = ResourceTreeNodeCollection.Empty;
        }

        public IDataTemplate Template { get; }
        public string TemplateTypeName { get; }
        public string? Description { get; }
        public bool IsRecycling { get; }
        public bool IsTreeTemplate { get; }
        public Type? DataType { get; }
        public string? DataTypeName { get; }

        public override ResourceTreeNodeCollection Children { get; }

        private static string GetTemplateName(IDataTemplate template)
        {
            if (template is ITypedDataTemplate typed && typed.DataType is { } dataType)
            {
                return dataType.GetTypeName();
            }

            return template.GetType().Name;
        }

        private static string? GetTemplateSecondaryText(IDataTemplate template)
        {
            if (template is ITypedDataTemplate typed && typed.DataType is { })
            {
                return template.GetType().Name;
            }

            return null;
        }
    }
}
