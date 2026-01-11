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
    sealed class DataGridCheckBoxColumnDefinition : DataGridBoundColumnDefinition
    {
        private bool? _isThreeState;

        public bool? IsThreeState
        {
            get => _isThreeState;
            set => SetProperty(ref _isThreeState, value);
        }

        protected override DataGridColumn CreateColumnCore()
        {
            return new DataGridCheckBoxColumn();
        }

        protected override void ApplyColumnProperties(DataGridColumn column, DataGridColumnDefinitionContext context)
        {
            base.ApplyColumnProperties(column, context);

            if (column is DataGridCheckBoxColumn checkBoxColumn)
            {
                if (IsThreeState.HasValue)
                {
                    checkBoxColumn.IsThreeState = IsThreeState.Value;
                }
                else
                {
                    checkBoxColumn.ClearValue(DataGridCheckBoxColumn.IsThreeStateProperty);
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

            if (propertyName == nameof(IsThreeState) && column is DataGridCheckBoxColumn checkBoxColumn)
            {
                if (IsThreeState.HasValue)
                {
                    checkBoxColumn.IsThreeState = IsThreeState.Value;
                }
                else
                {
                    checkBoxColumn.ClearValue(DataGridCheckBoxColumn.IsThreeStateProperty);
                }

                return true;
            }

            return false;
        }
    }
}
