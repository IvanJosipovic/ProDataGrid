// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable disable

using Avalonia;
using Avalonia.Controls.Templates;

namespace Avalonia.Controls
{
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    sealed class DataGridTemplateColumnDefinition : DataGridColumnDefinition
    {
        private string _cellTemplateKey;
        private string _cellEditingTemplateKey;
        private string _newRowCellTemplateKey;
        private bool? _reuseCellContent;

        public string CellTemplateKey
        {
            get => _cellTemplateKey;
            set => SetProperty(ref _cellTemplateKey, value);
        }

        public string CellEditingTemplateKey
        {
            get => _cellEditingTemplateKey;
            set => SetProperty(ref _cellEditingTemplateKey, value);
        }

        public string NewRowCellTemplateKey
        {
            get => _newRowCellTemplateKey;
            set => SetProperty(ref _newRowCellTemplateKey, value);
        }

        public bool? ReuseCellContent
        {
            get => _reuseCellContent;
            set => SetProperty(ref _reuseCellContent, value);
        }

        protected override DataGridColumn CreateColumnCore()
        {
            return new DataGridTemplateColumn();
        }

        protected override void ApplyColumnProperties(DataGridColumn column, DataGridColumnDefinitionContext context)
        {
            if (column is DataGridTemplateColumn templateColumn)
            {
                if (ReuseCellContent.HasValue)
                {
                    templateColumn.ReuseCellContent = ReuseCellContent.Value;
                }
                else
                {
                    templateColumn.ClearValue(DataGridTemplateColumn.ReuseCellContentProperty);
                }

                var reuseCellContent = templateColumn.ReuseCellContent;
                templateColumn.CellTemplate = ResolveTemplate(context, CellTemplateKey, reuseCellContent);
                templateColumn.CellEditingTemplate = ResolveTemplate(context, CellEditingTemplateKey, reuseCellContent);
                templateColumn.NewRowCellTemplate = ResolveTemplate(context, NewRowCellTemplateKey, reuseCellContent);
            }
        }

        protected override bool ApplyColumnPropertyChange(
            DataGridColumn column,
            DataGridColumnDefinitionContext context,
            string propertyName)
        {
            if (column is not DataGridTemplateColumn templateColumn)
            {
                return false;
            }

            switch (propertyName)
            {
                case nameof(CellTemplateKey):
                    templateColumn.CellTemplate = ResolveTemplate(
                        context,
                        CellTemplateKey,
                        ReuseCellContent ?? templateColumn.ReuseCellContent);
                    return true;
                case nameof(CellEditingTemplateKey):
                    templateColumn.CellEditingTemplate = ResolveTemplate(
                        context,
                        CellEditingTemplateKey,
                        ReuseCellContent ?? templateColumn.ReuseCellContent);
                    return true;
                case nameof(NewRowCellTemplateKey):
                    templateColumn.NewRowCellTemplate = ResolveTemplate(
                        context,
                        NewRowCellTemplateKey,
                        ReuseCellContent ?? templateColumn.ReuseCellContent);
                    return true;
                case nameof(ReuseCellContent):
                    if (ReuseCellContent.HasValue)
                    {
                        templateColumn.ReuseCellContent = ReuseCellContent.Value;
                    }
                    else
                    {
                        templateColumn.ClearValue(DataGridTemplateColumn.ReuseCellContentProperty);
                    }

                    var reuseCellContent = templateColumn.ReuseCellContent;
                    templateColumn.CellTemplate = ResolveTemplate(context, CellTemplateKey, reuseCellContent);
                    templateColumn.CellEditingTemplate = ResolveTemplate(context, CellEditingTemplateKey, reuseCellContent);
                    templateColumn.NewRowCellTemplate = ResolveTemplate(context, NewRowCellTemplateKey, reuseCellContent);
                    return true;
            }

            return false;
        }

        private static IDataTemplate ResolveTemplate(
            DataGridColumnDefinitionContext context,
            string key,
            bool reuseCellContent)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            var template = context?.ResolveResource<IDataTemplate>(key);
            if (template != null)
            {
                return template;
            }

            return context?.Grid != null
                ? new DeferredResourceTemplate(context.Grid, key, reuseCellContent)
                : null;
        }

        private sealed class DeferredResourceTemplate : IRecyclingDataTemplate
        {
            private readonly IResourceHost _resourceHost;
            private readonly object _key;
            private readonly bool _reuseCellContent;

            public DeferredResourceTemplate(IResourceHost resourceHost, object key, bool reuseCellContent)
            {
                _resourceHost = resourceHost;
                _key = key;
                _reuseCellContent = reuseCellContent;
            }

            public bool Match(object data)
            {
                return true;
            }

            public Control Build(object data)
            {
                var template = ResolveTemplate();
                if (template == null)
                {
                    throw DataGridError.DataGridTemplateColumn.MissingTemplateForType(typeof(DataGridTemplateColumn));
                }

                return template.Build(data);
            }

            public Control Build(object data, Control existing)
            {
                var template = ResolveTemplate();
                if (template == null)
                {
                    throw DataGridError.DataGridTemplateColumn.MissingTemplateForType(typeof(DataGridTemplateColumn));
                }

                if (template is IRecyclingDataTemplate recycling)
                {
                    return recycling.Build(data, existing);
                }

                if (_reuseCellContent && existing != null)
                {
                    return existing;
                }

                return template.Build(data);
            }

            private IDataTemplate ResolveTemplate()
            {
                if (_resourceHost.TryFindResource(_key, out var resource) && resource is IDataTemplate template)
                {
                    return template;
                }

                if (Application.Current != null &&
                    Application.Current.TryFindResource(_key, out resource) &&
                    resource is IDataTemplate appTemplate)
                {
                    return appTemplate;
                }

                return null;
            }
        }
    }
}
