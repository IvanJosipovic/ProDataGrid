// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable disable

namespace Avalonia.Controls
{
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    sealed class DataGridHyperlinkColumnDefinition : DataGridBoundColumnDefinition
    {
        private string _targetName;
        private string _watermark;
        private DataGridBindingDefinition _contentBinding;

        public string TargetName
        {
            get => _targetName;
            set => SetProperty(ref _targetName, value);
        }

        public string Watermark
        {
            get => _watermark;
            set => SetProperty(ref _watermark, value);
        }

        public DataGridBindingDefinition ContentBinding
        {
            get => _contentBinding;
            set => SetProperty(ref _contentBinding, value);
        }

        protected override DataGridColumn CreateColumnCore()
        {
            return new DataGridHyperlinkColumn();
        }

        protected override void ApplyColumnProperties(DataGridColumn column, DataGridColumnDefinitionContext context)
        {
            base.ApplyColumnProperties(column, context);

            if (column is DataGridHyperlinkColumn hyperlinkColumn)
            {
                if (TargetName != null)
                {
                    hyperlinkColumn.TargetName = TargetName;
                }
                else
                {
                    hyperlinkColumn.ClearValue(DataGridHyperlinkColumn.TargetNameProperty);
                }

                if (Watermark != null)
                {
                    hyperlinkColumn.Watermark = Watermark;
                }
                else
                {
                    hyperlinkColumn.ClearValue(DataGridHyperlinkColumn.WatermarkProperty);
                }

                hyperlinkColumn.ContentBinding = ContentBinding?.CreateBinding();
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

            if (column is not DataGridHyperlinkColumn hyperlinkColumn)
            {
                return false;
            }

            switch (propertyName)
            {
                case nameof(TargetName):
                    if (TargetName != null)
                    {
                        hyperlinkColumn.TargetName = TargetName;
                    }
                    else
                    {
                        hyperlinkColumn.ClearValue(DataGridHyperlinkColumn.TargetNameProperty);
                    }
                    return true;
                case nameof(Watermark):
                    if (Watermark != null)
                    {
                        hyperlinkColumn.Watermark = Watermark;
                    }
                    else
                    {
                        hyperlinkColumn.ClearValue(DataGridHyperlinkColumn.WatermarkProperty);
                    }
                    return true;
                case nameof(ContentBinding):
                    hyperlinkColumn.ContentBinding = ContentBinding?.CreateBinding();
                    return true;
            }

            return false;
        }
    }
}
