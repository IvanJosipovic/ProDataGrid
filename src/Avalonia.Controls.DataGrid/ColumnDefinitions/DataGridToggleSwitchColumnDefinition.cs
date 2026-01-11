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
    sealed class DataGridToggleSwitchColumnDefinition : DataGridBoundColumnDefinition
    {
        private object _onContent;
        private object _offContent;
        private string _onContentTemplateKey;
        private string _offContentTemplateKey;
        private bool? _isThreeState;

        public object OnContent
        {
            get => _onContent;
            set => SetProperty(ref _onContent, value);
        }

        public object OffContent
        {
            get => _offContent;
            set => SetProperty(ref _offContent, value);
        }

        public string OnContentTemplateKey
        {
            get => _onContentTemplateKey;
            set => SetProperty(ref _onContentTemplateKey, value);
        }

        public string OffContentTemplateKey
        {
            get => _offContentTemplateKey;
            set => SetProperty(ref _offContentTemplateKey, value);
        }

        public bool? IsThreeState
        {
            get => _isThreeState;
            set => SetProperty(ref _isThreeState, value);
        }

        protected override DataGridColumn CreateColumnCore()
        {
            return new DataGridToggleSwitchColumn();
        }

        protected override void ApplyColumnProperties(DataGridColumn column, DataGridColumnDefinitionContext context)
        {
            base.ApplyColumnProperties(column, context);

            if (column is DataGridToggleSwitchColumn toggleColumn)
            {
                if (OnContent != null)
                {
                    toggleColumn.OnContent = OnContent;
                }
                else
                {
                    toggleColumn.ClearValue(DataGridToggleSwitchColumn.OnContentProperty);
                }

                if (OffContent != null)
                {
                    toggleColumn.OffContent = OffContent;
                }
                else
                {
                    toggleColumn.ClearValue(DataGridToggleSwitchColumn.OffContentProperty);
                }

                if (OnContentTemplateKey != null)
                {
                    toggleColumn.OnContentTemplate = context?.ResolveResource<IDataTemplate>(OnContentTemplateKey);
                }
                else
                {
                    toggleColumn.ClearValue(DataGridToggleSwitchColumn.OnContentTemplateProperty);
                }

                if (OffContentTemplateKey != null)
                {
                    toggleColumn.OffContentTemplate = context?.ResolveResource<IDataTemplate>(OffContentTemplateKey);
                }
                else
                {
                    toggleColumn.ClearValue(DataGridToggleSwitchColumn.OffContentTemplateProperty);
                }

                if (IsThreeState.HasValue)
                {
                    toggleColumn.IsThreeState = IsThreeState.Value;
                }
                else
                {
                    toggleColumn.ClearValue(DataGridToggleSwitchColumn.IsThreeStateProperty);
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

            if (column is not DataGridToggleSwitchColumn toggleColumn)
            {
                return false;
            }

            switch (propertyName)
            {
                case nameof(OnContent):
                    if (OnContent != null)
                    {
                        toggleColumn.OnContent = OnContent;
                    }
                    else
                    {
                        toggleColumn.ClearValue(DataGridToggleSwitchColumn.OnContentProperty);
                    }
                    return true;
                case nameof(OffContent):
                    if (OffContent != null)
                    {
                        toggleColumn.OffContent = OffContent;
                    }
                    else
                    {
                        toggleColumn.ClearValue(DataGridToggleSwitchColumn.OffContentProperty);
                    }
                    return true;
                case nameof(OnContentTemplateKey):
                    if (OnContentTemplateKey != null)
                    {
                        toggleColumn.OnContentTemplate = context?.ResolveResource<IDataTemplate>(OnContentTemplateKey);
                    }
                    else
                    {
                        toggleColumn.ClearValue(DataGridToggleSwitchColumn.OnContentTemplateProperty);
                    }
                    return true;
                case nameof(OffContentTemplateKey):
                    if (OffContentTemplateKey != null)
                    {
                        toggleColumn.OffContentTemplate = context?.ResolveResource<IDataTemplate>(OffContentTemplateKey);
                    }
                    else
                    {
                        toggleColumn.ClearValue(DataGridToggleSwitchColumn.OffContentTemplateProperty);
                    }
                    return true;
                case nameof(IsThreeState):
                    if (IsThreeState.HasValue)
                    {
                        toggleColumn.IsThreeState = IsThreeState.Value;
                    }
                    else
                    {
                        toggleColumn.ClearValue(DataGridToggleSwitchColumn.IsThreeStateProperty);
                    }
                    return true;
            }

            return false;
        }
    }
}
