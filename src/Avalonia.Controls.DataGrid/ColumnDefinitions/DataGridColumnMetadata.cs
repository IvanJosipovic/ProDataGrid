// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable disable

using System;
using Avalonia;

namespace Avalonia.Controls
{
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    class DataGridColumnMetadata
    {
        internal static readonly AttachedProperty<DataGridColumnDefinition> DefinitionProperty =
            AvaloniaProperty.RegisterAttached<DataGridColumnMetadata, DataGridColumn, DataGridColumnDefinition>(
                "Definition");
#if !DATAGRID_INTERNAL
        public
#else
        internal
#endif
        static readonly AttachedProperty<IDataGridColumnValueAccessor> ValueAccessorProperty =
            AvaloniaProperty.RegisterAttached<DataGridColumnMetadata, DataGridColumn, IDataGridColumnValueAccessor>(
                "ValueAccessor");

#if !DATAGRID_INTERNAL
        public
#else
        internal
#endif
        static readonly AttachedProperty<Type> ValueTypeProperty =
            AvaloniaProperty.RegisterAttached<DataGridColumnMetadata, DataGridColumn, Type>(
                "ValueType");

        public static IDataGridColumnValueAccessor GetValueAccessor(DataGridColumn column)
        {
            return column?.GetValue(ValueAccessorProperty);
        }

        public static void SetValueAccessor(DataGridColumn column, IDataGridColumnValueAccessor accessor)
        {
            column?.SetValue(ValueAccessorProperty, accessor);
        }

        public static void ClearValueAccessor(DataGridColumn column)
        {
            column?.ClearValue(ValueAccessorProperty);
        }

        internal static DataGridColumnDefinition GetDefinition(DataGridColumn column)
        {
            return column?.GetValue(DefinitionProperty);
        }

        internal static void SetDefinition(DataGridColumn column, DataGridColumnDefinition definition)
        {
            column?.SetValue(DefinitionProperty, definition);
        }

        internal static void ClearDefinition(DataGridColumn column)
        {
            column?.ClearValue(DefinitionProperty);
        }

        public static Type GetValueType(DataGridColumn column)
        {
            var accessor = GetValueAccessor(column);
            if (accessor != null)
            {
                return accessor.ValueType;
            }

            return column?.GetValue(ValueTypeProperty);
        }

        public static void SetValueType(DataGridColumn column, Type valueType)
        {
            column?.SetValue(ValueTypeProperty, valueType);
        }

        public static void ClearValueType(DataGridColumn column)
        {
            column?.ClearValue(ValueTypeProperty);
        }

        internal static object GetDefinitionKey(DataGridColumnDefinition definition)
        {
            if (definition == null)
            {
                return null;
            }

            return definition.ColumnKey ?? (object)definition;
        }

        internal static object GetColumnId(DataGridColumn column)
        {
            if (column == null)
            {
                return null;
            }

            if (column.ColumnKey != null)
            {
                return column.ColumnKey;
            }

            var definition = GetDefinition(column);
            if (definition != null)
            {
                return GetDefinitionKey(definition);
            }

            return column;
        }

        internal static bool MatchesColumnId(DataGridColumn column, object columnId)
        {
            if (column == null || columnId == null)
            {
                return false;
            }

            if (column.ColumnKey != null && Equals(column.ColumnKey, columnId))
            {
                return true;
            }

            if (ReferenceEquals(columnId, column))
            {
                return true;
            }

            var definition = GetDefinition(column);
            if (definition != null)
            {
                if (ReferenceEquals(columnId, definition))
                {
                    return true;
                }

                if (definition.ColumnKey != null && Equals(definition.ColumnKey, columnId))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
