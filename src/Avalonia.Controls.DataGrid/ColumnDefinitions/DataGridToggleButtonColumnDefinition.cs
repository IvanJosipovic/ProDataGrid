// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable disable

using Avalonia.Controls.Primitives;

namespace Avalonia.Controls
{
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    sealed class DataGridToggleButtonColumnDefinition : DataGridBoundColumnDefinition
    {
        private object _content;
        private object _checkedContent;
        private object _uncheckedContent;
        private bool? _isThreeState;
        private ClickMode? _clickMode;

        public object Content
        {
            get => _content;
            set => SetProperty(ref _content, value);
        }

        public object CheckedContent
        {
            get => _checkedContent;
            set => SetProperty(ref _checkedContent, value);
        }

        public object UncheckedContent
        {
            get => _uncheckedContent;
            set => SetProperty(ref _uncheckedContent, value);
        }

        public bool? IsThreeState
        {
            get => _isThreeState;
            set => SetProperty(ref _isThreeState, value);
        }

        public ClickMode? ClickMode
        {
            get => _clickMode;
            set => SetProperty(ref _clickMode, value);
        }

        protected override DataGridColumn CreateColumnCore()
        {
            return new DataGridToggleButtonColumn();
        }

        protected override void ApplyColumnProperties(DataGridColumn column, DataGridColumnDefinitionContext context)
        {
            base.ApplyColumnProperties(column, context);

            if (column is DataGridToggleButtonColumn toggleColumn)
            {
                if (Content != null)
                {
                    toggleColumn.Content = Content;
                }
                else
                {
                    toggleColumn.ClearValue(DataGridToggleButtonColumn.ContentProperty);
                }

                if (CheckedContent != null)
                {
                    toggleColumn.CheckedContent = CheckedContent;
                }
                else
                {
                    toggleColumn.ClearValue(DataGridToggleButtonColumn.CheckedContentProperty);
                }

                if (UncheckedContent != null)
                {
                    toggleColumn.UncheckedContent = UncheckedContent;
                }
                else
                {
                    toggleColumn.ClearValue(DataGridToggleButtonColumn.UncheckedContentProperty);
                }

                if (IsThreeState.HasValue)
                {
                    toggleColumn.IsThreeState = IsThreeState.Value;
                }
                else
                {
                    toggleColumn.ClearValue(DataGridToggleButtonColumn.IsThreeStateProperty);
                }

                if (ClickMode.HasValue)
                {
                    toggleColumn.ClickMode = ClickMode.Value;
                }
                else
                {
                    toggleColumn.ClearValue(DataGridToggleButtonColumn.ClickModeProperty);
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

            if (column is not DataGridToggleButtonColumn toggleColumn)
            {
                return false;
            }

            switch (propertyName)
            {
                case nameof(Content):
                    if (Content != null)
                    {
                        toggleColumn.Content = Content;
                    }
                    else
                    {
                        toggleColumn.ClearValue(DataGridToggleButtonColumn.ContentProperty);
                    }
                    return true;
                case nameof(CheckedContent):
                    if (CheckedContent != null)
                    {
                        toggleColumn.CheckedContent = CheckedContent;
                    }
                    else
                    {
                        toggleColumn.ClearValue(DataGridToggleButtonColumn.CheckedContentProperty);
                    }
                    return true;
                case nameof(UncheckedContent):
                    if (UncheckedContent != null)
                    {
                        toggleColumn.UncheckedContent = UncheckedContent;
                    }
                    else
                    {
                        toggleColumn.ClearValue(DataGridToggleButtonColumn.UncheckedContentProperty);
                    }
                    return true;
                case nameof(IsThreeState):
                    if (IsThreeState.HasValue)
                    {
                        toggleColumn.IsThreeState = IsThreeState.Value;
                    }
                    else
                    {
                        toggleColumn.ClearValue(DataGridToggleButtonColumn.IsThreeStateProperty);
                    }
                    return true;
                case nameof(ClickMode):
                    if (ClickMode.HasValue)
                    {
                        toggleColumn.ClickMode = ClickMode.Value;
                    }
                    else
                    {
                        toggleColumn.ClearValue(DataGridToggleButtonColumn.ClickModeProperty);
                    }
                    return true;
            }

            return false;
        }
    }
}
