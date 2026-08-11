// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls;

namespace ProDataGrid.Charting
{
    /// <summary>
    /// Adapts canonical source-generated analytics fields to a reflection-free chart model.
    /// </summary>
    public static class DataGridGeneratedChartAdapter
    {
        /// <summary>Creates and configures a chart model for the supplied generated fields.</summary>
        public static DataGridChartModel CreateModel(
            IEnumerable items,
            IReadOnlyList<IDataGridGeneratedAnalyticsField> fields)
        {
            ArgumentNullException.ThrowIfNull(items);
            var model = new DataGridChartModel { ItemsSource = items };
            Configure(model, fields);
            return model;
        }

        /// <summary>
        /// Configures category and series selectors without property paths or runtime reflection.
        /// Existing generated series definitions are replaced.
        /// </summary>
        public static void Configure(
            DataGridChartModel model,
            IReadOnlyList<IDataGridGeneratedAnalyticsField> fields)
            => ConfigureCore(model, fields, null);

        /// <summary>
        /// Configures a chart from the generated analytics fields whose stable column keys
        /// fall inside an inclusive spreadsheet selection.
        /// </summary>
        public static void ConfigureRange(
            DataGridChartModel model,
            IReadOnlyList<IDataGridGeneratedAnalyticsField> fields,
            IReadOnlyList<DataGridColumnDefinition> columns,
            DataGridCellRange range)
        {
            ArgumentNullException.ThrowIfNull(model);
            ArgumentNullException.ThrowIfNull(fields);
            ArgumentNullException.ThrowIfNull(columns);
            ValidateColumnRange(columns, range);

            var selectedKeys = new HashSet<string>(StringComparer.Ordinal);
            for (int columnIndex = range.StartColumn; columnIndex <= range.EndColumn; columnIndex++)
            {
                string? key = columns[columnIndex].ColumnKey?.ToString();
                if (!string.IsNullOrWhiteSpace(key))
                {
                    selectedKeys.Add(key);
                }
            }

            ConfigureCore(model, fields, selectedKeys);
            if (model.Series.Count == 0)
            {
                throw new ArgumentException("The selected range does not contain a generated chart value field.", nameof(range));
            }
        }

        /// <summary>Creates an owned, bounded spreadsheet-range chart projection.</summary>
        public static DataGridGeneratedChartRangeProjection CreateRangeProjection(
            IEnumerable items,
            IReadOnlyList<IDataGridGeneratedAnalyticsField> fields,
            IReadOnlyList<DataGridColumnDefinition> columns,
            DataGridCellRange range,
            int maximumRows = 65536) =>
            new(items, fields, columns, range, maximumRows);

        /// <summary>
        /// Creates a bounded long-form chart source whose generated chart-series field
        /// partitions rows into series and whose category field aligns their values.
        /// </summary>
        public static DataGridGeneratedLongFormChartDataSource CreateLongFormSource(
            IEnumerable items,
            IReadOnlyList<IDataGridGeneratedAnalyticsField> fields,
            int maximumItems = 65536,
            int maximumSeries = 256) =>
            new(items, fields, maximumItems, maximumSeries);

        private static void ConfigureCore(
            DataGridChartModel model,
            IReadOnlyList<IDataGridGeneratedAnalyticsField> fields,
            HashSet<string>? selectedKeys)
        {
            ArgumentNullException.ThrowIfNull(model);
            ArgumentNullException.ThrowIfNull(fields);

            IDataGridGeneratedAnalyticsField? category = FindFirst(
                fields,
                DataGridGeneratedAnalyticsRole.ChartCategory,
                selectedKeys);
            category ??= FindFirst(fields, DataGridGeneratedAnalyticsRole.ChartCategory, null);
            model.CategoryPath = null;
            model.CategorySelector = category == null
                ? null
                : item => Convert.ToString(category.GetValue(item), CultureInfo.CurrentCulture);

            model.Series.Clear();
            var values = new List<IDataGridGeneratedAnalyticsField>();
            for (int index = 0; index < fields.Count; index++)
            {
                IDataGridGeneratedAnalyticsField field = fields[index];
                if ((field.Role & DataGridGeneratedAnalyticsRole.ChartValue) != 0 &&
                    (selectedKeys == null || selectedKeys.Contains(field.ColumnKey)))
                {
                    values.Add(field);
                }
            }

            values.Sort(static (left, right) =>
            {
                int order = left.Order.CompareTo(right.Order);
                return order != 0 ? order : string.CompareOrdinal(left.ColumnKey, right.ColumnKey);
            });

            for (int index = 0; index < values.Count; index++)
            {
                IDataGridGeneratedAnalyticsField value = values[index];
                IDataGridGeneratedAnalyticsField? xValue = FindCompanion(fields, DataGridGeneratedAnalyticsRole.ChartXValue, value);
                IDataGridGeneratedAnalyticsField? size = FindCompanion(fields, DataGridGeneratedAnalyticsRole.ChartSize, value);
                string? format = value.Format;
                Func<object, double?> valueSelector = GetNumericSelector(value) ??
                    (item => ToNullableDouble(value.GetValue(item)));
                Func<object, double?>? xValueSelector = GetNumericSelector(xValue) ??
                    (xValue == null ? null : item => ToNullableDouble(xValue.GetValue(item)));
                Func<object, double?>? sizeSelector = GetNumericSelector(size) ??
                    (size == null ? null : item => ToNullableDouble(size.GetValue(item)));
                model.Series.Add(new DataGridChartSeriesDefinition
                {
                    Name = value.Name ?? value.ColumnKey,
                    ValueSelector = valueSelector,
                    XValueSelector = xValueSelector,
                    SizeSelector = sizeSelector,
                    Aggregation = ToChartAggregation(value.Aggregate),
                    DataLabelFormatter = string.IsNullOrWhiteSpace(format)
                        ? null
                        : number => number.ToString(format, CultureInfo.CurrentCulture)
                });
            }
        }

        private static IDataGridGeneratedAnalyticsField? FindFirst(
            IReadOnlyList<IDataGridGeneratedAnalyticsField> fields,
            DataGridGeneratedAnalyticsRole role,
            HashSet<string>? selectedKeys)
        {
            IDataGridGeneratedAnalyticsField? result = null;
            for (int index = 0; index < fields.Count; index++)
            {
                IDataGridGeneratedAnalyticsField candidate = fields[index];
                if ((candidate.Role & role) == 0 ||
                    (selectedKeys != null && !selectedKeys.Contains(candidate.ColumnKey)))
                {
                    continue;
                }

                if (result == null || candidate.Order < result.Order ||
                    candidate.Order == result.Order && string.CompareOrdinal(candidate.ColumnKey, result.ColumnKey) < 0)
                {
                    result = candidate;
                }
            }

            return result;
        }

        private static void ValidateColumnRange(
            IReadOnlyList<DataGridColumnDefinition> columns,
            DataGridCellRange range)
        {
            if (range.StartRow < 0 || range.EndRow < range.StartRow)
            {
                throw new ArgumentOutOfRangeException(nameof(range), "The row range must be non-negative and ordered.");
            }

            if (range.StartColumn < 0 || range.EndColumn < range.StartColumn || range.EndColumn >= columns.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(range), "The column range must be ordered and inside the generated column set.");
            }
        }

        private static Func<object, double?>? GetNumericSelector(IDataGridGeneratedAnalyticsField? field) =>
            (field as IDataGridGeneratedNumericAnalyticsField)?.NumericValueSelector;

        private static IDataGridGeneratedAnalyticsField? FindCompanion(
            IReadOnlyList<IDataGridGeneratedAnalyticsField> fields,
            DataGridGeneratedAnalyticsRole role,
            IDataGridGeneratedAnalyticsField value)
        {
            IDataGridGeneratedAnalyticsField? fallback = null;
            for (int index = 0; index < fields.Count; index++)
            {
                IDataGridGeneratedAnalyticsField candidate = fields[index];
                if ((candidate.Role & role) == 0)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(value.Name) && string.Equals(candidate.Name, value.Name, StringComparison.Ordinal))
                {
                    return candidate;
                }

                if (candidate.Order == value.Order)
                {
                    fallback = candidate;
                }
                else if (fallback == null)
                {
                    fallback = candidate;
                }
            }

            return fallback;
        }

        private static double? ToNullableDouble(object? value)
        {
            if (value == null)
            {
                return null;
            }

            return value switch
            {
                double number => number,
                float number => number,
                decimal number => (double)number,
                long number => number,
                ulong number => number,
                int number => number,
                uint number => number,
                short number => number,
                ushort number => number,
                byte number => number,
                sbyte number => number,
                _ when value is IConvertible convertible => convertible.ToDouble(CultureInfo.InvariantCulture),
                _ => null
            };
        }

        private static DataGridChartAggregation ToChartAggregation(int aggregate) =>
            (DataGridAggregateType)aggregate switch
            {
                DataGridAggregateType.Average => DataGridChartAggregation.Average,
                DataGridAggregateType.Min => DataGridChartAggregation.Min,
                DataGridAggregateType.Max => DataGridChartAggregation.Max,
                DataGridAggregateType.Count or DataGridAggregateType.CountDistinct => DataGridChartAggregation.Count,
                _ => DataGridChartAggregation.Sum
            };
    }
}
