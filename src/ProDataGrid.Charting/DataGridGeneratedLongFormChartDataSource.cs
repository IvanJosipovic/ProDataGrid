// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Controls;
using ProCharts;

namespace ProDataGrid.Charting
{
    /// <summary>
    /// Builds aligned chart series from long-form rows using generated category,
    /// series-discriminator, and numeric value selectors without property paths.
    /// </summary>
    public sealed class DataGridGeneratedLongFormChartDataSource : IChartDataSource, IChartWindowInfoProvider, IDisposable
    {
        private readonly IEnumerable _items;
        private readonly IDataGridGeneratedAnalyticsField _categoryField;
        private readonly IDataGridGeneratedAnalyticsField _seriesField;
        private readonly ValueField[] _valueFields;
        private readonly INotifyCollectionChanged? _observableItems;
        private readonly HashSet<INotifyPropertyChanged> _observedItems = new(ReferenceEqualityComparer.Instance);
        private bool _disposed;
        private int _version;
        private int? _lastCategoryCount;

        internal DataGridGeneratedLongFormChartDataSource(
            IEnumerable items,
            IReadOnlyList<IDataGridGeneratedAnalyticsField> fields,
            int maximumItems,
            int maximumSeries)
        {
            _items = items ?? throw new ArgumentNullException(nameof(items));
            ArgumentNullException.ThrowIfNull(fields);
            if (maximumItems <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumItems));
            }
            if (maximumSeries <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumSeries));
            }

            MaximumItems = maximumItems;
            MaximumSeries = maximumSeries;
            _categoryField = FindRequiredField(fields, DataGridGeneratedAnalyticsRole.ChartCategory, "category");
            _seriesField = FindRequiredField(fields, DataGridGeneratedAnalyticsRole.ChartSeries, "series discriminator");
            _valueFields = FindValueFields(fields);
            if (_valueFields.Length == 0)
            {
                throw new ArgumentException("Long-form charts require at least one generated chart value field.", nameof(fields));
            }

            _observableItems = items as INotifyCollectionChanged;
            if (_observableItems != null)
            {
                _observableItems.CollectionChanged += OnCollectionChanged;
            }
            RewireItemNotifications();
        }

        /// <inheritdoc />
        public event EventHandler? DataInvalidated;

        /// <summary>Gets the maximum number of input rows accepted by one snapshot.</summary>
        public int MaximumItems { get; }

        /// <summary>Gets the maximum generated discriminator/value series count.</summary>
        public int MaximumSeries { get; }

        /// <summary>Gets or sets the emitted chart series kind.</summary>
        public ChartSeriesKind SeriesKind { get; set; } = ChartSeriesKind.Line;

        /// <summary>Gets or sets the culture used for category, discriminator, and labels.</summary>
        public CultureInfo Culture { get; set; } = CultureInfo.CurrentCulture;

        /// <inheritdoc />
        public int? GetTotalCategoryCount() => _lastCategoryCount;

        /// <inheritdoc />
        public ChartDataSnapshot BuildSnapshot(ChartDataRequest request)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var categories = new List<string?>();
            var categoryIndexes = new Dictionary<string, int>(StringComparer.Ordinal);
            var seriesBuilders = new List<SeriesBuilder>();
            var seriesIndexes = new Dictionary<SeriesKey, int>();
            int itemCount = 0;

            foreach (object? candidate in _items)
            {
                if (candidate == null)
                {
                    continue;
                }
                if (++itemCount > MaximumItems)
                {
                    throw new InvalidOperationException($"Long-form chart input exceeds the configured {MaximumItems}-item bound.");
                }

                string category = Convert.ToString(_categoryField.GetValue(candidate), Culture) ?? string.Empty;
                if (!categoryIndexes.TryGetValue(category, out int categoryIndex))
                {
                    categoryIndex = categories.Count;
                    categoryIndexes.Add(category, categoryIndex);
                    categories.Add(category);
                    for (int builderIndex = 0; builderIndex < seriesBuilders.Count; builderIndex++)
                    {
                        seriesBuilders[builderIndex].AddCategory();
                    }
                }

                string discriminator = Convert.ToString(_seriesField.GetValue(candidate), Culture) ?? string.Empty;
                for (int valueIndex = 0; valueIndex < _valueFields.Length; valueIndex++)
                {
                    ValueField valueField = _valueFields[valueIndex];
                    var key = new SeriesKey(discriminator, valueField.Field.ColumnKey);
                    if (!seriesIndexes.TryGetValue(key, out int seriesIndex))
                    {
                        if (seriesBuilders.Count >= MaximumSeries)
                        {
                            throw new InvalidOperationException($"Long-form chart output exceeds the configured {MaximumSeries}-series bound.");
                        }

                        seriesIndex = seriesBuilders.Count;
                        seriesIndexes.Add(key, seriesIndex);
                        seriesBuilders.Add(new SeriesBuilder(
                            BuildSeriesName(discriminator, valueField.Field, _valueFields.Length),
                            valueField,
                            categories.Count));
                    }

                    double? value = valueField.NumericSelector(candidate);
                    if (value.HasValue)
                    {
                        seriesBuilders[seriesIndex].AddValue(categoryIndex, value.Value);
                    }
                }
            }

            _lastCategoryCount = categories.Count;
            GetWindow(request, categories.Count, out int windowStart, out int windowCount);
            var visibleCategories = new string?[windowCount];
            for (int index = 0; index < windowCount; index++)
            {
                visibleCategories[index] = categories[windowStart + index];
            }

            var series = new ChartSeriesSnapshot[seriesBuilders.Count];
            for (int index = 0; index < seriesBuilders.Count; index++)
            {
                series[index] = seriesBuilders[index].Build(SeriesKind, windowStart, windowCount, Culture);
            }
            return new ChartDataSnapshot(visibleCategories, series, _version);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            if (_observableItems != null)
            {
                _observableItems.CollectionChanged -= OnCollectionChanged;
            }
            ClearItemNotifications();
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RewireItemNotifications();
            Invalidate();
        }

        private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e) => Invalidate();

        private void Invalidate()
        {
            _version++;
            DataInvalidated?.Invoke(this, EventArgs.Empty);
        }

        private void RewireItemNotifications()
        {
            ClearItemNotifications();
            foreach (object? item in _items)
            {
                if (item is INotifyPropertyChanged notifying && _observedItems.Add(notifying))
                {
                    notifying.PropertyChanged += OnItemPropertyChanged;
                }
            }
        }

        private void ClearItemNotifications()
        {
            foreach (INotifyPropertyChanged item in _observedItems)
            {
                item.PropertyChanged -= OnItemPropertyChanged;
            }
            _observedItems.Clear();
        }

        private static IDataGridGeneratedAnalyticsField FindRequiredField(
            IReadOnlyList<IDataGridGeneratedAnalyticsField> fields,
            DataGridGeneratedAnalyticsRole role,
            string description)
        {
            IDataGridGeneratedAnalyticsField? result = null;
            for (int index = 0; index < fields.Count; index++)
            {
                IDataGridGeneratedAnalyticsField field = fields[index];
                if ((field.Role & role) != 0 &&
                    (result == null || field.Order < result.Order ||
                     field.Order == result.Order && string.CompareOrdinal(field.ColumnKey, result.ColumnKey) < 0))
                {
                    result = field;
                }
            }
            return result ?? throw new ArgumentException($"Long-form charts require a generated {description} field.", nameof(fields));
        }

        private static ValueField[] FindValueFields(IReadOnlyList<IDataGridGeneratedAnalyticsField> fields)
        {
            var values = new List<IDataGridGeneratedAnalyticsField>();
            for (int index = 0; index < fields.Count; index++)
            {
                if ((fields[index].Role & DataGridGeneratedAnalyticsRole.ChartValue) != 0)
                {
                    values.Add(fields[index]);
                }
            }
            values.Sort(static (left, right) =>
            {
                int order = left.Order.CompareTo(right.Order);
                return order != 0 ? order : string.CompareOrdinal(left.ColumnKey, right.ColumnKey);
            });
            var result = new ValueField[values.Count];
            for (int index = 0; index < values.Count; index++)
            {
                IDataGridGeneratedAnalyticsField field = values[index];
                Func<object, double?> selector =
                    (field as IDataGridGeneratedNumericAnalyticsField)?.NumericValueSelector ??
                    (item => ToNullableDouble(field.GetValue(item)));
                result[index] = new ValueField(field, selector);
            }
            return result;
        }

        private static string BuildSeriesName(
            string discriminator,
            IDataGridGeneratedAnalyticsField value,
            int valueFieldCount) => valueFieldCount == 1
                ? discriminator
                : discriminator + " · " + (value.Name ?? value.ColumnKey);

        private static void GetWindow(ChartDataRequest request, int total, out int start, out int count)
        {
            start = Math.Clamp(request?.WindowStart ?? 0, 0, total);
            count = Math.Clamp(request?.WindowCount ?? total - start, 0, total - start);
        }

        private static double? ToNullableDouble(object? value) => value switch
        {
            null => null,
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
            IConvertible convertible => convertible.ToDouble(CultureInfo.InvariantCulture),
            _ => null
        };

        private readonly struct ValueField
        {
            public ValueField(IDataGridGeneratedAnalyticsField field, Func<object, double?> numericSelector)
            {
                Field = field;
                NumericSelector = numericSelector;
            }

            public IDataGridGeneratedAnalyticsField Field { get; }
            public Func<object, double?> NumericSelector { get; }
        }

        private readonly struct SeriesKey : IEquatable<SeriesKey>
        {
            public SeriesKey(string discriminator, string fieldKey)
            {
                Discriminator = discriminator;
                FieldKey = fieldKey;
            }

            public string Discriminator { get; }
            public string FieldKey { get; }
            public bool Equals(SeriesKey other) =>
                string.Equals(Discriminator, other.Discriminator, StringComparison.Ordinal) &&
                string.Equals(FieldKey, other.FieldKey, StringComparison.Ordinal);
            public override bool Equals(object? obj) => obj is SeriesKey other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(
                StringComparer.Ordinal.GetHashCode(Discriminator),
                StringComparer.Ordinal.GetHashCode(FieldKey));
        }

        private sealed class SeriesBuilder
        {
            private readonly string _name;
            private readonly ValueField _valueField;
            private readonly List<AggregateCell> _cells;

            public SeriesBuilder(string name, ValueField valueField, int categoryCount)
            {
                _name = name;
                _valueField = valueField;
                _cells = new List<AggregateCell>(categoryCount);
                for (int index = 0; index < categoryCount; index++)
                {
                    _cells.Add(default);
                }
            }

            public void AddCategory() => _cells.Add(default);

            public void AddValue(int categoryIndex, double value)
            {
                AggregateCell cell = _cells[categoryIndex];
                cell.Add(value);
                _cells[categoryIndex] = cell;
            }

            public ChartSeriesSnapshot Build(
                ChartSeriesKind kind,
                int windowStart,
                int windowCount,
                CultureInfo culture)
            {
                var values = new double?[windowCount];
                DataGridChartAggregation aggregation = ToChartAggregation(_valueField.Field.Aggregate);
                for (int index = 0; index < windowCount; index++)
                {
                    values[index] = _cells[windowStart + index].GetValue(aggregation);
                }
                string? format = _valueField.Field.Format;
                return new ChartSeriesSnapshot(
                    _name,
                    kind,
                    values,
                    dataLabelFormatter: string.IsNullOrWhiteSpace(format)
                        ? null
                        : value => value.ToString(format, culture));
            }
        }

        private struct AggregateCell
        {
            private double _sum;
            private double _min;
            private double _max;
            private double _first;
            private double _last;
            private int _count;

            public void Add(double value)
            {
                if (_count == 0)
                {
                    _min = value;
                    _max = value;
                    _first = value;
                }
                else
                {
                    _min = Math.Min(_min, value);
                    _max = Math.Max(_max, value);
                }
                _last = value;
                _sum += value;
                _count++;
            }

            public double? GetValue(DataGridChartAggregation aggregation) => _count == 0
                ? null
                : aggregation switch
                {
                    DataGridChartAggregation.Average => _sum / _count,
                    DataGridChartAggregation.Min => _min,
                    DataGridChartAggregation.Max => _max,
                    DataGridChartAggregation.Count => _count,
                    DataGridChartAggregation.First => _first,
                    DataGridChartAggregation.Last => _last,
                    _ => _sum
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
