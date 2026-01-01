// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.DataGridSorting;
using DataGridSample.Models;
using DynamicData.Binding;

namespace DataGridSample.Adapters
{
    /// <summary>
    /// Adapter factory that translates SortingModel descriptors into a comparer chain for DynamicData.
    /// It bypasses local SortDescriptions by overriding TryApplyModelToView.
    /// </summary>
    public sealed class DynamicDataStreamingSortingAdapterFactory : IDataGridSortingAdapterFactory
    {
        private readonly Action<string> _log;

        public DynamicDataStreamingSortingAdapterFactory(Action<string> log)
        {
            _log = log;
            SortComparer = Comparer<StreamingItem>.Create(static (_, _) => 0);
        }

        public IComparer<StreamingItem> SortComparer { get; private set; }

        public DataGridSortingAdapter Create(DataGrid grid, ISortingModel model)
        {
            return new DynamicDataSortingAdapter(model, () => grid.ColumnDefinitions, UpdateComparer, _log);
        }

        public void UpdateComparer(IReadOnlyList<SortingDescriptor> descriptors)
        {
            SortComparer = BuildComparer(descriptors);
            _log($"Upstream comparer updated: {Describe(descriptors)}");
        }

        private static IComparer<StreamingItem> BuildComparer(IReadOnlyList<SortingDescriptor> descriptors)
        {
            if (descriptors == null || descriptors.Count == 0)
            {
                return Comparer<StreamingItem>.Create(static (_, _) => 0);
            }

            if (descriptors.Any(d => d?.Comparer != null))
            {
                return BuildManualComparer(descriptors);
            }

            SortExpressionComparer<StreamingItem>? comparer = null;

            foreach (var descriptor in descriptors.Where(d => d != null))
            {
                var selector = CreateSelector(descriptor);
                if (selector == null)
                {
                    continue;
                }

                comparer = comparer == null
                    ? descriptor.Direction == ListSortDirection.Ascending
                        ? SortExpressionComparer<StreamingItem>.Ascending(selector)
                        : SortExpressionComparer<StreamingItem>.Descending(selector)
                    : descriptor.Direction == ListSortDirection.Ascending
                        ? comparer.ThenByAscending(selector)
                        : comparer.ThenByDescending(selector);
            }

            if (comparer == null)
            {
                return Comparer<StreamingItem>.Create(static (_, _) => 0);
            }

            return comparer;
        }

        private static IComparer<StreamingItem> BuildManualComparer(IReadOnlyList<SortingDescriptor> descriptors)
        {
            var compiled = descriptors
                .Where(d => d != null)
                .Select(Compile)
                .OfType<CompiledComparer>()
                .ToList();

            if (compiled.Count == 0)
            {
                return Comparer<StreamingItem>.Create(static (_, _) => 0);
            }

            return Comparer<StreamingItem>.Create((left, right) =>
            {
                foreach (var entry in compiled)
                {
                    var l = entry.Getter(left);
                    var r = entry.Getter(right);

                    int result;
                    if (entry.CustomComparer != null)
                    {
                        result = entry.CustomComparer.Compare(l, r);
                    }
                    else
                    {
                        result = Comparer.DefaultInvariant.Compare(l, r);
                    }

                    if (result != 0)
                    {
                        return entry.Direction == ListSortDirection.Ascending ? result : -result;
                    }
                }

                return 0;
            });
        }

        private static CompiledComparer? Compile(SortingDescriptor descriptor)
        {
            var propertyPath = descriptor.PropertyPath ?? descriptor.ColumnId?.ToString();
            if (string.IsNullOrEmpty(propertyPath))
            {
                return null;
            }

            return propertyPath switch
            {
                nameof(StreamingItem.Id) => new CompiledComparer(x => x.Id, descriptor.Direction, descriptor.Comparer),
                nameof(StreamingItem.Symbol) => new CompiledComparer(x => x.Symbol, descriptor.Direction, descriptor.Comparer),
                nameof(StreamingItem.Price) => new CompiledComparer(x => x.Price, descriptor.Direction, descriptor.Comparer),
                nameof(StreamingItem.UpdatedAt) => new CompiledComparer(x => x.UpdatedAt, descriptor.Direction, descriptor.Comparer),
                _ => null
            };
        }

        private static Func<StreamingItem, IComparable>? CreateSelector(SortingDescriptor descriptor)
        {
            var propertyPath = descriptor.PropertyPath ?? descriptor.ColumnId?.ToString();
            if (string.IsNullOrEmpty(propertyPath))
            {
                return null;
            }

            return propertyPath switch
            {
                nameof(StreamingItem.Id) => x => x.Id,
                nameof(StreamingItem.Symbol) => x => x.Symbol,
                nameof(StreamingItem.Price) => x => x.Price,
                nameof(StreamingItem.UpdatedAt) => x => x.UpdatedAt,
                _ => null
            };
        }

        private static string Describe(IReadOnlyList<SortingDescriptor> descriptors)
        {
            if (descriptors == null || descriptors.Count == 0)
            {
                return "(none)";
            }

            return string.Join(", ", descriptors.Where(d => d != null).Select(d =>
                $"{d.PropertyPath ?? d.ColumnId}:{(d.Direction == ListSortDirection.Ascending ? "asc" : "desc")}"));
        }

        private sealed class DynamicDataSortingAdapter : DataGridSortingAdapter
        {
            private readonly Action<IReadOnlyList<SortingDescriptor>> _update;
            private readonly Action<string> _log;

            public DynamicDataSortingAdapter(
                ISortingModel model,
                Func<IEnumerable<DataGridColumn>> columns,
                Action<IReadOnlyList<SortingDescriptor>> update,
                Action<string> log)
                : base(model, columns)
            {
                _update = update;
                _log = log;
            }

            protected override bool TryApplyModelToView(
                IReadOnlyList<SortingDescriptor> descriptors,
                IReadOnlyList<SortingDescriptor> previousDescriptors,
                out bool changed)
            {
                _update(descriptors);
                _log($"Applied to DynamicData: {Describe(descriptors)}");
                changed = true;
                return true;
            }

            private static string Describe(IReadOnlyList<SortingDescriptor> descriptors)
            {
                if (descriptors == null || descriptors.Count == 0)
                {
                    return "(none)";
                }

                return string.Join(", ", descriptors.Where(d => d != null).Select(d =>
                    $"{d.PropertyPath ?? d.ColumnId}:{(d.Direction == ListSortDirection.Ascending ? "asc" : "desc")}"));
            }
        }

        private sealed class CompiledComparer
        {
            public CompiledComparer(Func<StreamingItem, object?> getter, ListSortDirection direction, IComparer? customComparer)
            {
                Getter = getter;
                Direction = direction;
                CustomComparer = customComparer;
            }

            public Func<StreamingItem, object?> Getter { get; }

            public ListSortDirection Direction { get; }

            public IComparer? CustomComparer { get; }
        }
    }
}
