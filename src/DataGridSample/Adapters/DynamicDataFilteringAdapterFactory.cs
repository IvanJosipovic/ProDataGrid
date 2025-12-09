// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.DataGridFiltering;
using DataGridSample.Models;

namespace DataGridSample.Adapters
{
    /// <summary>
    /// Adapter factory that translates FilteringModel descriptors into a DynamicData filter predicate.
    /// It bypasses the local view filter by overriding TryApplyModelToView.
    /// </summary>
    public sealed class DynamicDataFilteringAdapterFactory : IDataGridFilteringAdapterFactory
    {
        private readonly Action<string> _log;

        public DynamicDataFilteringAdapterFactory(Action<string> log)
        {
            _log = log;
            FilterPredicate = static _ => true;
        }

        public Func<Deployment, bool> FilterPredicate { get; private set; }

        public DataGridFilteringAdapter Create(DataGrid grid, IFilteringModel model)
        {
            return new DynamicDataFilteringAdapter(model, () => grid.Columns, UpdateFilter, _log);
        }

        public void UpdateFilter(IReadOnlyList<FilteringDescriptor> descriptors)
        {
            FilterPredicate = BuildPredicate(descriptors);
            _log($"Upstream filter updated: {Describe(descriptors)}");
        }

        private static Func<Deployment, bool> BuildPredicate(IReadOnlyList<FilteringDescriptor> descriptors)
        {
            if (descriptors == null || descriptors.Count == 0)
            {
                return AlwaysTrue;
            }

            var compiled = new List<Func<Deployment, bool>>(descriptors.Count);
            foreach (var descriptor in descriptors)
            {
                var predicate = Compile(descriptor);
                if (predicate != null)
                {
                    compiled.Add(predicate);
                }
            }

            if (compiled.Count == 0)
            {
                return AlwaysTrue;
            }

            if (compiled.Count == 1)
            {
                return compiled[0];
            }

            return item =>
            {
                for (int i = 0; i < compiled.Count; i++)
                {
                    if (!compiled[i](item))
                    {
                        return false;
                    }
                }

                return true;
            };
        }

        private static Func<Deployment, bool>? Compile(FilteringDescriptor descriptor)
        {
            if (descriptor == null)
            {
                return null;
            }

            if (descriptor.Predicate != null)
            {
                var predicate = descriptor.Predicate;
                return item => predicate(item);
            }

            var selector = CreateSelector(descriptor);
            if (selector == null)
            {
                return null;
            }

            var culture = descriptor.Culture ?? CultureInfo.InvariantCulture;
            var stringComparison = descriptor.StringComparisonMode ?? StringComparison.OrdinalIgnoreCase;
            var values = descriptor.Values;
            var value = descriptor.Value;

            return descriptor.Operator switch
            {
                FilteringOperator.Equals => item => Equals(selector(item), value),
                FilteringOperator.NotEquals => item => !Equals(selector(item), value),
                FilteringOperator.Contains => item => Contains(selector(item), value, stringComparison),
                FilteringOperator.StartsWith => item => StartsWith(selector(item), value, stringComparison),
                FilteringOperator.EndsWith => item => EndsWith(selector(item), value, stringComparison),
                FilteringOperator.GreaterThan => item => Compare(selector(item), value, culture) > 0,
                FilteringOperator.GreaterThanOrEqual => item => Compare(selector(item), value, culture) >= 0,
                FilteringOperator.LessThan => item => Compare(selector(item), value, culture) < 0,
                FilteringOperator.LessThanOrEqual => item => Compare(selector(item), value, culture) <= 0,
                FilteringOperator.Between => item => Between(selector(item), values, culture),
                FilteringOperator.In => item => In(selector(item), values),
                _ => AlwaysTrue
            };
        }

        private static Func<Deployment, object?>? CreateSelector(FilteringDescriptor descriptor)
        {
            var key = descriptor.PropertyPath ?? descriptor.ColumnId?.ToString();
            return key switch
            {
                nameof(Deployment.Service) => d => d.Service,
                nameof(Deployment.Status) => d => d.Status,
                nameof(Deployment.Region) => d => d.Region,
                nameof(Deployment.Ring) => d => d.Ring,
                nameof(Deployment.Started) => d => d.Started,
                nameof(Deployment.ErrorRate) => d => d.ErrorRate,
                nameof(Deployment.Incidents) => d => d.Incidents,
                _ => null
            };
        }

        private static bool Contains(object? source, object? target, StringComparison comparison)
        {
            if (source == null || target == null)
            {
                return false;
            }

            if (source is string s && target is string t)
            {
                return s.IndexOf(t, comparison) >= 0;
            }

            if (source is IEnumerable<object> enumerable)
            {
                foreach (var item in enumerable)
                {
                    if (Equals(item, target))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool StartsWith(object? source, object? target, StringComparison comparison)
        {
            if (source is string s && target is string t)
            {
                return s.StartsWith(t, comparison);
            }

            return false;
        }

        private static bool EndsWith(object? source, object? target, StringComparison comparison)
        {
            if (source is string s && target is string t)
            {
                return s.EndsWith(t, comparison);
            }

            return false;
        }

        private static int Compare(object? left, object? right, CultureInfo culture)
        {
            if (left == null && right == null)
            {
                return 0;
            }

            if (left == null)
            {
                return -1;
            }

            if (right == null)
            {
                return 1;
            }

            if (left is IComparable comparable)
            {
                return comparable.CompareTo(right);
            }

            var comparer = culture != null
                ? Comparer<object>.Create((x, y) =>
                    string.Compare(Convert.ToString(x, culture), Convert.ToString(y, culture), StringComparison.Ordinal))
                : Comparer<object>.Default;

            return comparer.Compare(left, right);
        }

        private static bool Between(object? value, IReadOnlyList<object>? bounds, CultureInfo culture)
        {
            if (bounds == null || bounds.Count < 2)
            {
                return false;
            }

            var lower = Compare(value, bounds[0], culture) >= 0;
            var upper = Compare(value, bounds[1], culture) <= 0;
            return lower && upper;
        }

        private static bool In(object? value, IReadOnlyList<object>? values)
        {
            if (values == null || values.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < values.Count; i++)
            {
                if (Equals(value, values[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static string Describe(IReadOnlyList<FilteringDescriptor> descriptors)
        {
            if (descriptors == null || descriptors.Count == 0)
            {
                return "(none)";
            }

            return string.Join(", ", descriptors.Where(d => d != null).Select(d =>
            {
                var op = d.Operator.ToString();
                var value = d.Value ?? (d.Values != null ? string.Join("|", d.Values) : "(null)");
                return $"{d.PropertyPath ?? d.ColumnId}:{op}={value}";
            }));
        }

        private static bool AlwaysTrue(Deployment _) => true;

        private sealed class DynamicDataFilteringAdapter : DataGridFilteringAdapter
        {
            private readonly Action<IReadOnlyList<FilteringDescriptor>> _update;
            private readonly Action<string> _log;

            public DynamicDataFilteringAdapter(
                IFilteringModel model,
                Func<IEnumerable<DataGridColumn>> columns,
                Action<IReadOnlyList<FilteringDescriptor>> update,
                Action<string> log)
                : base(model, columns)
            {
                _update = update;
                _log = log;
            }

            protected override bool TryApplyModelToView(
                IReadOnlyList<FilteringDescriptor> descriptors,
                IReadOnlyList<FilteringDescriptor> previousDescriptors,
                out bool changed)
            {
                _update(descriptors);
                _log($"Applied to DynamicData: {Describe(descriptors)}");
                changed = true;
                return true;
            }

            private static string Describe(IReadOnlyList<FilteringDescriptor> descriptors)
            {
                if (descriptors == null || descriptors.Count == 0)
                {
                    return "(none)";
                }

                return string.Join(", ", descriptors.Where(d => d != null).Select(d =>
                {
                    var op = d.Operator.ToString();
                    var value = d.Value ?? (d.Values != null ? string.Join("|", d.Values) : "(null)");
                    return $"{d.PropertyPath ?? d.ColumnId}:{op}={value}";
                }));
            }
        }
    }
}
