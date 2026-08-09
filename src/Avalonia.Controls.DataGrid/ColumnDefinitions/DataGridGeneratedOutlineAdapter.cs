// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia.Controls.DataGridPivoting;
using Avalonia.Controls.DataGridReporting;

namespace Avalonia.Controls
{
    /// <summary>Adapts canonical generated analytics fields to reflection-free outline reports.</summary>
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    static class DataGridGeneratedOutlineAdapter
    {
        /// <summary>Creates an outline grouping field from a generated role.</summary>
        public static OutlineGroupField CreateGroupField(IDataGridGeneratedAnalyticsField field)
        {
            ArgumentNullException.ThrowIfNull(field);
            if ((field.Role & DataGridGeneratedAnalyticsRole.OutlineGroup) == 0)
            {
                throw new ArgumentException("The generated field does not define an outline group role.", nameof(field));
            }

            var result = new OutlineGroupField
            {
                Key = field.ColumnKey,
                Header = field.Name ?? field.ColumnKey,
                ValueSelector = field.GetValue,
                StringFormat = field.Format
            };
            if (field is IDataGridGeneratedAdvancedAnalyticsField advanced)
            {
                advanced.ConfigureOutlineGroup?.Invoke(result);
            }
            return result;
        }

        /// <summary>Creates an outline value/detail field from a generated role.</summary>
        public static OutlineValueField CreateValueField(IDataGridGeneratedAnalyticsField field)
        {
            ArgumentNullException.ThrowIfNull(field);
            if ((field.Role & DataGridGeneratedAnalyticsRole.OutlineDetail) == 0)
            {
                throw new ArgumentException("The generated field does not define an outline detail role.", nameof(field));
            }

            var result = new OutlineValueField
            {
                Key = field.ColumnKey,
                Header = field.Name ?? field.ColumnKey,
                ValueSelector = field.GetValue,
                StringFormat = field.Format,
                AggregateType = ToPivotAggregate((DataGridAggregateType)field.Aggregate)
            };
            if (field is IDataGridGeneratedAdvancedAnalyticsField advanced)
            {
                result.CustomAggregator = advanced.CustomAggregatorFactory?.Invoke();
                advanced.ConfigureOutlineValue?.Invoke(result);
            }
            return result;
        }

        /// <summary>Creates globally ordered outline grouping fields.</summary>
        public static IReadOnlyList<OutlineGroupField> CreateGroupFields(
            IReadOnlyList<IDataGridGeneratedAnalyticsField> fields)
        {
            List<IDataGridGeneratedAnalyticsField> matches = GetOrderedFields(
                fields,
                DataGridGeneratedAnalyticsRole.OutlineGroup);
            var result = new OutlineGroupField[matches.Count];
            for (int index = 0; index < matches.Count; index++)
            {
                result[index] = CreateGroupField(matches[index]);
            }
            return result;
        }

        /// <summary>Creates globally ordered outline value/detail fields.</summary>
        public static IReadOnlyList<OutlineValueField> CreateValueFields(
            IReadOnlyList<IDataGridGeneratedAnalyticsField> fields)
        {
            List<IDataGridGeneratedAnalyticsField> matches = GetOrderedFields(
                fields,
                DataGridGeneratedAnalyticsRole.OutlineDetail);
            var result = new OutlineValueField[matches.Count];
            for (int index = 0; index < matches.Count; index++)
            {
                result[index] = CreateValueField(matches[index]);
            }
            return result;
        }

        /// <summary>Creates a populated outline model whose selectors come only from generated metadata.</summary>
        public static OutlineReportModel CreateModel(
            IEnumerable items,
            IReadOnlyList<IDataGridGeneratedAnalyticsField> fields,
            Action<OutlineReportModel>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(items);
            ArgumentNullException.ThrowIfNull(fields);

            var model = new OutlineReportModel { AutoRefresh = false };
            try
            {
                IReadOnlyList<OutlineGroupField> groupFields = CreateGroupFields(fields);
                for (int index = 0; index < groupFields.Count; index++)
                {
                    model.GroupFields.Add(groupFields[index]);
                }

                IReadOnlyList<OutlineValueField> valueFields = CreateValueFields(fields);
                for (int index = 0; index < valueFields.Count; index++)
                {
                    model.ValueFields.Add(valueFields[index]);
                }

                model.ItemsSource = items;
                configure?.Invoke(model);
                model.AutoRefresh = true;
                return model;
            }
            catch
            {
                model.Dispose();
                throw;
            }
        }

        private static List<IDataGridGeneratedAnalyticsField> GetOrderedFields(
            IReadOnlyList<IDataGridGeneratedAnalyticsField> fields,
            DataGridGeneratedAnalyticsRole role)
        {
            ArgumentNullException.ThrowIfNull(fields);
            var matches = new List<IDataGridGeneratedAnalyticsField>();
            for (int index = 0; index < fields.Count; index++)
            {
                IDataGridGeneratedAnalyticsField field = fields[index] ??
                    throw new ArgumentException("Generated analytics fields cannot contain null entries.", nameof(fields));
                if ((field.Role & role) != 0)
                {
                    matches.Add(field);
                }
            }
            matches.Sort(static (left, right) =>
            {
                int order = left.Order.CompareTo(right.Order);
                return order != 0 ? order : string.CompareOrdinal(left.ColumnKey, right.ColumnKey);
            });
            return matches;
        }

        private static PivotAggregateType ToPivotAggregate(DataGridAggregateType aggregate) => aggregate switch
        {
            DataGridAggregateType.None => PivotAggregateType.None,
            DataGridAggregateType.Sum => PivotAggregateType.Sum,
            DataGridAggregateType.Average => PivotAggregateType.Average,
            DataGridAggregateType.Count => PivotAggregateType.Count,
            DataGridAggregateType.CountDistinct => PivotAggregateType.CountDistinct,
            DataGridAggregateType.Min => PivotAggregateType.Min,
            DataGridAggregateType.Max => PivotAggregateType.Max,
            DataGridAggregateType.First => PivotAggregateType.First,
            DataGridAggregateType.Last => PivotAggregateType.Last,
            DataGridAggregateType.Custom => PivotAggregateType.Custom,
            _ => throw new ArgumentOutOfRangeException(
                nameof(aggregate),
                aggregate,
                "Unsupported generated outline aggregate type.")
        };
    }
}
