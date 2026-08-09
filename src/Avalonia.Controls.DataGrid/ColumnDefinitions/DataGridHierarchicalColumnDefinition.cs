// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable disable

using Avalonia.Controls.Templates;

namespace Avalonia.Controls
{
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    sealed class DataGridHierarchicalColumnDefinition : DataGridBoundColumnDefinition
    {
        private double? _indent;
        private string _cellTemplateKey;
        private bool _useDirectCell;
        private bool _useDirectTextContent;
        private bool _useOptimizedPresenter;
        private bool _trackDirectTextValueChanges = true;

        public double? Indent
        {
            get => _indent;
            set => SetProperty(ref _indent, value);
        }

        public string CellTemplateKey
        {
            get => _cellTemplateKey;
            set => SetProperty(ref _cellTemplateKey, value);
        }

        /// <summary>
        /// Gets or sets whether the generated hierarchical column uses the optimized retained direct cell.
        /// </summary>
        public bool UseDirectCell
        {
            get => _useDirectCell;
            set => SetProperty(ref _useDirectCell, value);
        }

        /// <summary>
        /// Gets or sets whether compatible hierarchy cells use their typed accessor directly.
        /// Ordinary retained cells keep their presenter and Avalonia content template.
        /// </summary>
        public bool UseDirectTextContent
        {
            get => _useDirectTextContent;
            set => SetProperty(ref _useDirectTextContent, value);
        }

        /// <summary>
        /// Gets or sets whether retained hierarchy cells combine the cell and expander-presenter
        /// roles while retaining normal Avalonia control content.
        /// </summary>
        public bool UseOptimizedPresenter
        {
            get => _useOptimizedPresenter;
            set => SetProperty(ref _useOptimizedPresenter, value);
        }

        /// <summary>
        /// Gets or sets whether the generated optimized hierarchy text cells subscribe to
        /// wrapped-item property changes. Disable this only for immutable display values.
        /// </summary>
        public bool TrackDirectTextValueChanges
        {
            get => _trackDirectTextValueChanges;
            set => SetProperty(ref _trackDirectTextValueChanges, value);
        }

        protected override DataGridColumn CreateColumnCore()
        {
            return new DataGridHierarchicalColumn();
        }

        protected override void ApplyColumnProperties(DataGridColumn column, DataGridColumnDefinitionContext context)
        {
            base.ApplyColumnProperties(column, context);

            if (column is DataGridHierarchicalColumn hierarchicalColumn)
            {
                hierarchicalColumn.UseDirectCell = UseDirectCell;
                hierarchicalColumn.UseDirectTextContent = UseDirectTextContent;
                hierarchicalColumn.UseOptimizedPresenter = UseOptimizedPresenter;
                hierarchicalColumn.TrackDirectTextValueChanges = TrackDirectTextValueChanges;
                hierarchicalColumn.CellTemplate = CellTemplateKey != null
                    ? context?.ResolveResource<IDataTemplate>(CellTemplateKey)
                    : null;

                if (Indent.HasValue)
                {
                    hierarchicalColumn.Indent = Indent.Value;
                }
                else
                {
                    hierarchicalColumn.ClearValue(DataGridHierarchicalColumn.IndentProperty);
                }
            }
        }

        protected override bool ApplyColumnPropertyChange(
            DataGridColumn column,
            DataGridColumnDefinitionContext context,
            string propertyName)
        {
            if (base.ApplyColumnPropertyChange(column, context, propertyName))
            {
                return true;
            }

            if (column is not DataGridHierarchicalColumn hierarchicalColumn)
            {
                return false;
            }

            switch (propertyName)
            {
                case nameof(UseDirectCell):
                    hierarchicalColumn.UseDirectCell = UseDirectCell;
                    return true;
                case nameof(UseDirectTextContent):
                    hierarchicalColumn.UseDirectTextContent = UseDirectTextContent;
                    return true;
                case nameof(UseOptimizedPresenter):
                    hierarchicalColumn.UseOptimizedPresenter = UseOptimizedPresenter;
                    return true;
                case nameof(TrackDirectTextValueChanges):
                    hierarchicalColumn.TrackDirectTextValueChanges = TrackDirectTextValueChanges;
                    return true;
                case nameof(CellTemplateKey):
                    hierarchicalColumn.CellTemplate = CellTemplateKey != null
                        ? context?.ResolveResource<IDataTemplate>(CellTemplateKey)
                        : null;
                    return true;
                case nameof(Indent):
                    if (Indent.HasValue)
                    {
                        hierarchicalColumn.Indent = Indent.Value;
                    }
                    else
                    {
                        hierarchicalColumn.ClearValue(DataGridHierarchicalColumn.IndentProperty);
                    }
                    return true;
            }

            return false;
        }
    }
}
