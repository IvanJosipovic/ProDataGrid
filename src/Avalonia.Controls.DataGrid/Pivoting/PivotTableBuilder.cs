// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Data.Converters;

namespace Avalonia.Controls.DataGridPivoting
{
    internal sealed partial class PivotTableBuilder
    {
        private static readonly IPropertyInfo RowDisplayValuesProperty = new ClrPropertyInfo(
            nameof(PivotRow.RowDisplayValues),
            target => ((PivotRow)target).RowDisplayValues,
            null,
            typeof(object?[]));

        private static readonly IPropertyInfo CellValuesProperty = new ClrPropertyInfo(
            nameof(PivotRow.CellValues),
            target => ((PivotRow)target).CellValues,
            null,
            typeof(object?[]));

        private static readonly Func<PivotRow, object?[]> RowDisplayValuesGetter = row => row.RowDisplayValues;
        private static readonly Func<PivotRow, object?[]> CellValuesGetter = row => row.CellValues;

        private readonly PivotTableModel _model;
        private readonly PivotAggregatorRegistry _aggregators;
        private readonly CultureInfo _culture;

        public PivotTableBuilder(PivotTableModel model, PivotAggregatorRegistry aggregators, CultureInfo culture)
        {
            _model = model;
            _aggregators = aggregators;
            _culture = culture;
        }

        public PivotBuildResult Build()
        {
            var itemsSource = _model.ItemsSource;
            if (itemsSource == null)
            {
                return PivotBuildResult.Empty;
            }

            var rowFields = _model.RowFields;
            var columnFields = _model.ColumnFields;
            var valueFields = _model.ValueFields;
            var filterFields = _model.FilterFields;

            if (valueFields.Count == 0)
            {
                return PivotBuildResult.Empty;
            }

            var layout = _model.Layout;
            var rowFieldCount = rowFields.Count;
            var columnFieldCount = columnFields.Count;
            var valueFieldCount = valueFields.Count;
            var hasCalculatedFields = valueFields.Any(field => field.IsCalculated);
            PivotFormulaEvaluator? formulaEvaluator = null;
            if (hasCalculatedFields)
            {
                formulaEvaluator = new PivotFormulaEvaluator(valueFields);
            }
            var formulaUsage = formulaEvaluator?.Usage;

            var rowSubtotalLevels = new bool[rowFieldCount];
            if (layout.ShowRowSubtotals)
            {
                for (var i = 0; i < rowFieldCount - 1; i++)
                {
                    rowSubtotalLevels[i] = rowFields[i].ShowSubtotals;
                }
            }

            var columnSubtotalLevels = new bool[columnFieldCount];
            if (layout.ShowColumnSubtotals)
            {
                for (var i = 0; i < columnFieldCount - 1; i++)
                {
                    columnSubtotalLevels[i] = columnFields[i].ShowSubtotals;
                }
            }

            var needsRowParentTotals = valueFields.Any(field => !field.IsCalculated && field.DisplayMode == PivotValueDisplayMode.PercentOfParentRowTotal);
            var needsColumnParentTotals = valueFields.Any(field => !field.IsCalculated && field.DisplayMode == PivotValueDisplayMode.PercentOfParentColumnTotal);
            if (formulaUsage != null)
            {
                needsRowParentTotals |= formulaUsage.UsesParentRowTotals;
                needsColumnParentTotals |= formulaUsage.UsesParentColumnTotals;
            }

            var needsColumnGrandTotals = layout.ShowColumnGrandTotals ||
                needsColumnParentTotals ||
                valueFields.Any(field =>
                    !field.IsCalculated &&
                    (field.DisplayMode == PivotValueDisplayMode.PercentOfRowTotal ||
                     field.DisplayMode == PivotValueDisplayMode.PercentOfGrandTotal ||
                     field.DisplayMode == PivotValueDisplayMode.Index)) ||
                rowFields.Any(field => field.ValueFilter != null || field.ValueSort != null);
            if (formulaUsage != null)
            {
                needsColumnGrandTotals |= formulaUsage.UsesRowTotals || formulaUsage.UsesGrandTotals;
            }

            var needsRowGrandTotals = layout.ShowRowGrandTotals ||
                needsRowParentTotals ||
                valueFields.Any(field =>
                    !field.IsCalculated &&
                    (field.DisplayMode == PivotValueDisplayMode.PercentOfColumnTotal ||
                     field.DisplayMode == PivotValueDisplayMode.PercentOfGrandTotal ||
                     field.DisplayMode == PivotValueDisplayMode.Index)) ||
                columnFields.Any(field => field.ValueFilter != null || field.ValueSort != null);
            if (formulaUsage != null)
            {
                needsRowGrandTotals |= formulaUsage.UsesColumnTotals || formulaUsage.UsesGrandTotals;
            }

            var rowLevels = new bool[rowFieldCount + 1];
            rowLevels[rowFieldCount] = true;
            if (needsRowGrandTotals)
            {
                rowLevels[0] = true;
            }

            for (var i = 0; i < rowFieldCount - 1; i++)
            {
                if (rowSubtotalLevels[i] || rowFields[i].ValueFilter != null || rowFields[i].ValueSort != null || needsRowParentTotals)
                {
                    rowLevels[i + 1] = true;
                }
            }

            var columnLevels = new bool[columnFieldCount + 1];
            columnLevels[columnFieldCount] = true;
            if (needsColumnGrandTotals)
            {
                columnLevels[0] = true;
            }

            for (var i = 0; i < columnFieldCount - 1; i++)
            {
                if (columnSubtotalLevels[i] || columnFields[i].ValueFilter != null || columnFields[i].ValueSort != null || needsColumnParentTotals)
                {
                    columnLevels[i + 1] = true;
                }
            }

            var rowRoot = new PivotGroupNode(null, -1, null, Array.Empty<object?>(), Array.Empty<string?>());
            var columnRoot = new PivotGroupNode(null, -1, null, Array.Empty<object?>(), Array.Empty<string?>());
            var cellStates = new Dictionary<PivotCellKey, PivotCellState>();

            var rowNodes = new PivotGroupNode[rowFieldCount + 1];
            var columnNodes = new PivotGroupNode[columnFieldCount + 1];
            var rowValues = new object?[rowFieldCount];
            var columnValues = new object?[columnFieldCount];
            var valueValues = new object?[valueFieldCount];
            var valueFieldRequiresAggregation = new bool[valueFieldCount];
            for (var i = 0; i < valueFieldCount; i++)
            {
                var field = valueFields[i];
                valueFieldRequiresAggregation[i] = !field.IsCalculated && field.AggregateType != PivotAggregateType.None;
            }

            foreach (var item in itemsSource)
            {
                if (item == null)
                {
                    continue;
                }

                if (!PassesFilters(item, rowFields, columnFields, filterFields, rowValues, columnValues))
                {
                    continue;
                }

                for (var i = 0; i < valueFieldCount; i++)
                {
                    valueValues[i] = valueFieldRequiresAggregation[i] ? valueFields[i].GetValue(item) : null;
                }

                rowNodes[0] = rowRoot;
                for (var i = 0; i < rowFieldCount; i++)
                {
                    rowNodes[i + 1] = rowNodes[i].GetOrCreateChild(rowFields[i], rowValues[i], _culture, layout.EmptyValueLabel);
                }

                columnNodes[0] = columnRoot;
                for (var i = 0; i < columnFieldCount; i++)
                {
                    columnNodes[i + 1] = columnNodes[i].GetOrCreateChild(columnFields[i], columnValues[i], _culture, layout.EmptyValueLabel);
                }

                for (var rowLevel = 0; rowLevel < rowLevels.Length; rowLevel++)
                {
                    if (!rowLevels[rowLevel])
                    {
                        continue;
                    }

                    for (var columnLevel = 0; columnLevel < columnLevels.Length; columnLevel++)
                    {
                        if (!columnLevels[columnLevel])
                        {
                            continue;
                        }

                        var rowNode = rowNodes[rowLevel];
                        var columnNode = columnNodes[columnLevel];
                        var key = new PivotCellKey(rowNode.PathValues, columnNode.PathValues);
                        if (!cellStates.TryGetValue(key, out var state))
                        {
                            state = new PivotCellState(valueFields, _aggregators);
                            cellStates[key] = state;
                        }

                        for (var valueIndex = 0; valueIndex < valueFieldCount; valueIndex++)
                        {
                            if (valueFieldRequiresAggregation[valueIndex])
                            {
                                state.Add(valueIndex, valueValues[valueIndex]);
                            }
                        }
                    }
                }
            }

            EnsureItemsWithNoData(rowRoot, rowFields, layout.EmptyValueLabel, _culture);
            EnsureItemsWithNoData(columnRoot, columnFields, layout.EmptyValueLabel, _culture);

            ApplyValueFilters(rowRoot, rowFields, valueFields, cellStates, isRowAxis: true, formulaEvaluator: formulaEvaluator);
            ApplyValueFilters(columnRoot, columnFields, valueFields, cellStates, isRowAxis: false, formulaEvaluator: formulaEvaluator);

            SortTree(rowRoot, rowFields, valueFields, cellStates, isRowAxis: true, culture: _culture, formulaEvaluator: formulaEvaluator);
            SortTree(columnRoot, columnFields, valueFields, cellStates, isRowAxis: false, culture: _culture, formulaEvaluator: formulaEvaluator);

            var columns = BuildColumns(
                columnRoot,
                columnFields,
                valueFields,
                columnSubtotalLevels,
                layout,
                valueFieldsInColumns: layout.ValuesPosition == PivotValuesPosition.Columns,
                culture: _culture);

            if (columnFieldCount > 0 && layout.ShowColumnGrandTotals)
            {
                var totalColumns = CreateColumnsForNode(
                    columnRoot,
                    columnFields,
                    valueFields,
                    PivotColumnType.GrandTotal,
                    layout,
                    valueFieldsInColumns: layout.ValuesPosition == PivotValuesPosition.Columns,
                    culture: _culture);

                if (layout.ColumnGrandTotalPosition == PivotTotalPosition.Start)
                {
                    columns.InsertRange(0, totalColumns);
                }
                else
                {
                    columns.AddRange(totalColumns);
                }
            }

            for (var i = 0; i < columns.Count; i++)
            {
                var column = columns[i];
                columns[i] = new PivotColumn(
                    i,
                    column.ColumnType,
                    column.ColumnPathValues,
                    column.ColumnDisplayValues,
                    column.ValueField,
                    column.ValueFieldIndex,
                    column.Header);
            }

            var rows = BuildRows(
                rowRoot,
                rowFields,
                valueFields,
                rowSubtotalLevels,
                layout,
                columns.Count,
                _culture);

            if (rowFieldCount > 0 && layout.ShowRowGrandTotals)
            {
                var totalRows = CreateGrandTotalRows(
                    rowRoot,
                    rowFields,
                    valueFields,
                    layout,
                    columns.Count,
                    _culture);

                if (layout.RowGrandTotalPosition == PivotTotalPosition.Start)
                {
                    rows.InsertRange(0, totalRows);
                }
                else
                {
                    rows.AddRange(totalRows);
                }
            }

            ApplyRowLabelRepeats(rows, layout, rowFields.Count, layout.ValuesPosition == PivotValuesPosition.Rows);
            FillCellValues(rows, columns, cellStates, layout, _culture, valueFieldCount, formulaEvaluator);

            var columnDefinitions = BuildColumnDefinitions(
                rowFields,
                valueFields,
                columns,
                layout,
                _culture);

            return new PivotBuildResult(rows, columns, columnDefinitions);
        }

        private static bool PassesFilters(
            object item,
            IList<PivotAxisField> rowFields,
            IList<PivotAxisField> columnFields,
            IList<PivotAxisField> filterFields,
            object?[] rowValues,
            object?[] columnValues)
        {
            for (var i = 0; i < rowFields.Count; i++)
            {
                var field = rowFields[i];
                var value = field.GetGroupValue(item);
                rowValues[i] = value;

                var filter = field.Filter;
                if (filter != null && !filter.IsMatch(value))
                {
                    return false;
                }
            }

            for (var i = 0; i < columnFields.Count; i++)
            {
                var field = columnFields[i];
                var value = field.GetGroupValue(item);
                columnValues[i] = value;

                var filter = field.Filter;
                if (filter != null && !filter.IsMatch(value))
                {
                    return false;
                }
            }

            for (var i = 0; i < filterFields.Count; i++)
            {
                var field = filterFields[i];
                var filter = field.Filter;
                if (filter == null)
                {
                    continue;
                }

                var value = field.GetGroupValue(item);
                if (!filter.IsMatch(value))
                {
                    return false;
                }
            }

            return true;
        }

        private static void EnsureItemsWithNoData(
            PivotGroupNode root,
            IList<PivotAxisField> fields,
            string? emptyValueLabel,
            CultureInfo culture)
        {
            if (fields.Count == 0)
            {
                return;
            }

            var itemsByField = new List<object?>?[fields.Count];
            for (var i = 0; i < fields.Count; i++)
            {
                var field = fields[i];
                if (!field.ShowItemsWithNoData || field.ItemsSource == null)
                {
                    continue;
                }

                var items = new List<object?>();
                var seen = new HashSet<object?>();
                foreach (var item in field.ItemsSource)
                {
                    var value = item;
                    if (field.ApplyGroupSelectorToItemsSource && field.GroupSelector != null)
                    {
                        value = field.GroupSelector(item);
                    }

                    if (!seen.Add(value))
                    {
                        continue;
                    }

                    if (field.Filter != null && !field.Filter.IsMatch(value))
                    {
                        continue;
                    }

                    items.Add(value);
                }

                if (items.Count > 0)
                {
                    itemsByField[i] = items;
                }
            }

            var nodesByLevel = new List<PivotGroupNode>?[fields.Count];
            var nodeSetsByLevel = new HashSet<PivotGroupNode>?[fields.Count];
            CollectNodesByLevel(root, nodesByLevel, nodeSetsByLevel);

            for (var i = 0; i < fields.Count; i++)
            {
                var items = itemsByField[i];
                if (items == null || items.Count == 0)
                {
                    continue;
                }

                var parents = i == 0 ? null : nodesByLevel[i - 1];
                if (i > 0 && (parents == null || parents.Count == 0))
                {
                    continue;
                }

                if (i == 0)
                {
                    foreach (var value in items)
                    {
                        var child = root.GetOrCreateChild(fields[i], value, culture, emptyValueLabel);
                        AddNodeToLevel(nodesByLevel, nodeSetsByLevel, i, child);
                    }

                    continue;
                }

                foreach (var parent in parents!)
                {
                    foreach (var value in items)
                    {
                        var child = parent.GetOrCreateChild(fields[i], value, culture, emptyValueLabel);
                        AddNodeToLevel(nodesByLevel, nodeSetsByLevel, i, child);
                    }
                }
            }
        }

        private static void CollectNodesByLevel(
            PivotGroupNode root,
            List<PivotGroupNode>?[] nodesByLevel,
            HashSet<PivotGroupNode>?[] nodeSetsByLevel)
        {
            if (root.Children.Count == 0)
            {
                return;
            }

            var stack = new Stack<PivotGroupNode>(root.Children);
            while (stack.Count > 0)
            {
                var node = stack.Pop();
                if (node.Level >= 0 && node.Level < nodesByLevel.Length)
                {
                    AddNodeToLevel(nodesByLevel, nodeSetsByLevel, node.Level, node);
                }

                if (node.Children.Count == 0)
                {
                    continue;
                }

                for (var i = 0; i < node.Children.Count; i++)
                {
                    stack.Push(node.Children[i]);
                }
            }
        }

        private static void AddNodeToLevel(
            List<PivotGroupNode>?[] nodesByLevel,
            HashSet<PivotGroupNode>?[] nodeSetsByLevel,
            int level,
            PivotGroupNode node)
        {
            if (level < 0 || level >= nodesByLevel.Length)
            {
                return;
            }

            var set = nodeSetsByLevel[level];
            if (set == null)
            {
                set = new HashSet<PivotGroupNode>();
                nodeSetsByLevel[level] = set;
            }

            if (!set.Add(node))
            {
                return;
            }

            var list = nodesByLevel[level];
            if (list == null)
            {
                list = new List<PivotGroupNode>();
                nodesByLevel[level] = list;
            }

            list.Add(node);
        }

        private static void SortTree(
            PivotGroupNode root,
            IList<PivotAxisField> fields,
            IList<PivotValueField> valueFields,
            Dictionary<PivotCellKey, PivotCellState> cellStates,
            bool isRowAxis,
            CultureInfo culture,
            PivotFormulaEvaluator? formulaEvaluator)
        {
            if (fields.Count == 0)
            {
                return;
            }

            var comparer = new PivotValueComparer(culture);
            SortNode(root, fields, valueFields, cellStates, isRowAxis, comparer, formulaEvaluator);
        }

        private static void SortNode(
            PivotGroupNode node,
            IList<PivotAxisField> fields,
            IList<PivotValueField> valueFields,
            Dictionary<PivotCellKey, PivotCellState> cellStates,
            bool isRowAxis,
            PivotValueComparer defaultComparer,
            PivotFormulaEvaluator? formulaEvaluator)
        {
            if (node.Children.Count == 0)
            {
                return;
            }

            var fieldIndex = node.Level + 1;
            if (fieldIndex < 0 || fieldIndex >= fields.Count)
            {
                return;
            }

            var field = fields[fieldIndex];
            var comparer = field.Comparer ?? defaultComparer;
            var direction = field.SortDirection ?? ListSortDirection.Ascending;
            var valueSort = field.ValueSort;

            if (valueSort == null)
            {
                node.Children.Sort((left, right) =>
                {
                    var result = comparer.Compare(left.Key, right.Key);
                    return direction == ListSortDirection.Descending ? -result : result;
                });
            }
            else
            {
                var valueIndex = ResolveValueFieldIndex(valueSort.ValueField, valueFields);
                if (valueIndex >= 0)
                {
                    var valueDirection = valueSort.SortDirection;
                    var valueLookup = new Dictionary<PivotGroupNode, object?>(node.Children.Count);
                    foreach (var child in node.Children)
                    {
                        valueLookup[child] = GetAggregateValue(
                            child.PathValues,
                            valueIndex,
                            isRowAxis,
                            valueFields,
                            cellStates,
                            formulaEvaluator);
                    }

                    node.Children.Sort((left, right) =>
                    {
                        valueLookup.TryGetValue(left, out var leftValue);
                        valueLookup.TryGetValue(right, out var rightValue);
                        var leftIsNull = leftValue == null;
                        var rightIsNull = rightValue == null;
                        int result;
                        if (leftIsNull || rightIsNull)
                        {
                            if (leftIsNull && rightIsNull)
                            {
                                result = 0;
                            }
                            else
                            {
                                result = leftIsNull ? 1 : -1;
                            }
                        }
                        else
                        {
                            result = CompareAggregateValues(leftValue, rightValue, comparer);
                            if (valueDirection == ListSortDirection.Descending)
                            {
                                result = -result;
                            }
                        }

                        if (result != 0)
                        {
                            return result;
                        }

                        var tieResult = comparer.Compare(left.Key, right.Key);
                        return direction == ListSortDirection.Descending ? -tieResult : tieResult;
                    });
                }
                else
                {
                    node.Children.Sort((left, right) =>
                    {
                        var result = comparer.Compare(left.Key, right.Key);
                        return direction == ListSortDirection.Descending ? -result : result;
                    });
                }
            }

            foreach (var child in node.Children)
            {
                SortNode(child, fields, valueFields, cellStates, isRowAxis, defaultComparer, formulaEvaluator);
            }
        }

        private static void ApplyValueFilters(
            PivotGroupNode root,
            IList<PivotAxisField> fields,
            IList<PivotValueField> valueFields,
            Dictionary<PivotCellKey, PivotCellState> cellStates,
            bool isRowAxis,
            PivotFormulaEvaluator? formulaEvaluator)
        {
            if (fields.Count == 0 || !HasActiveValueFilters(fields))
            {
                return;
            }

            ApplyValueFiltersRecursive(root, fields, valueFields, cellStates, isRowAxis, formulaEvaluator);
        }

        private static bool HasActiveValueFilters(IList<PivotAxisField> fields)
        {
            for (var i = 0; i < fields.Count; i++)
            {
                var filter = fields[i].ValueFilter;
                if (filter != null && filter.FilterType != PivotValueFilterType.None)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ApplyValueFiltersRecursive(
            PivotGroupNode node,
            IList<PivotAxisField> fields,
            IList<PivotValueField> valueFields,
            Dictionary<PivotCellKey, PivotCellState> cellStates,
            bool isRowAxis,
            PivotFormulaEvaluator? formulaEvaluator)
        {
            if (node.Children.Count == 0)
            {
                return true;
            }

            var nextLevel = node.Level + 1;
            if (nextLevel >= fields.Count)
            {
                return true;
            }

            node.FilterChildren(child =>
                ApplyValueFiltersRecursive(child, fields, valueFields, cellStates, isRowAxis, formulaEvaluator));

            var field = fields[nextLevel];
            var valueFilter = field.ValueFilter;
            if (valueFilter != null && valueFilter.FilterType != PivotValueFilterType.None)
            {
                FilterChildrenByValue(node, valueFilter, valueFields, cellStates, isRowAxis, formulaEvaluator);
            }

            return node.Children.Count > 0 || node.Level < 0;
        }

        private static void FilterChildrenByValue(
            PivotGroupNode node,
            PivotValueFilter filter,
            IList<PivotValueField> valueFields,
            Dictionary<PivotCellKey, PivotCellState> cellStates,
            bool isRowAxis,
            PivotFormulaEvaluator? formulaEvaluator)
        {
            if (node.Children.Count == 0)
            {
                return;
            }

            var valueIndex = ResolveValueFieldIndex(filter.ValueField, valueFields);
            if (valueIndex < 0)
            {
                return;
            }

            switch (filter.FilterType)
            {
                case PivotValueFilterType.Top:
                case PivotValueFilterType.Bottom:
                case PivotValueFilterType.TopPercent:
                case PivotValueFilterType.BottomPercent:
                {
                    var numericValues = new List<(PivotGroupNode Node, double Value)>(node.Children.Count);
                    foreach (var child in node.Children)
                    {
                        var raw = GetAggregateValue(
                            child.PathValues,
                            valueIndex,
                            isRowAxis,
                            valueFields,
                            cellStates,
                            formulaEvaluator);
                        if (PivotNumeric.TryGetDouble(raw, out var number))
                        {
                            numericValues.Add((child, number));
                        }
                    }

                    numericValues.Sort((left, right) => right.Value.CompareTo(left.Value));

                    if (filter.FilterType == PivotValueFilterType.Bottom || filter.FilterType == PivotValueFilterType.BottomPercent)
                    {
                        numericValues.Reverse();
                    }

                    var count = filter.Count ?? 0;
                    if (filter.FilterType == PivotValueFilterType.TopPercent || filter.FilterType == PivotValueFilterType.BottomPercent)
                    {
                        var percent = filter.Percent ?? 10d;
                        if (percent <= 0d)
                        {
                            node.FilterChildren(_ => false);
                            return;
                        }

                        count = (int)Math.Ceiling(numericValues.Count * (percent / 100d));
                    }
                    else if (count <= 0 && filter.Percent.HasValue)
                    {
                        var percent = Math.Max(0d, filter.Percent.Value);
                        count = (int)Math.Ceiling(numericValues.Count * (percent / 100d));
                    }
                    else if (count <= 0)
                    {
                        count = Math.Min(10, numericValues.Count);
                    }

                    var keep = new HashSet<PivotGroupNode>();
                    for (var i = 0; i < Math.Min(count, numericValues.Count); i++)
                    {
                        keep.Add(numericValues[i].Node);
                    }

                    node.FilterChildren(child => keep.Contains(child));
                    return;
                }
                default:
                    node.FilterChildren(child =>
                    {
                        var raw = GetAggregateValue(
                            child.PathValues,
                            valueIndex,
                            isRowAxis,
                            valueFields,
                            cellStates,
                            formulaEvaluator);
                        return PivotNumeric.TryGetDouble(raw, out var number) && MatchesValueFilter(number, filter);
                    });
                    return;
            }
        }

        private static bool MatchesValueFilter(double? value, PivotValueFilter filter)
        {
            if (!value.HasValue)
            {
                return false;
            }

            var candidate = value.Value;
            var threshold = filter.Value ?? 0d;
            var threshold2 = filter.Value2 ?? threshold;

            return filter.FilterType switch
            {
                PivotValueFilterType.GreaterThan => candidate > threshold,
                PivotValueFilterType.GreaterThanOrEqual => candidate >= threshold,
                PivotValueFilterType.LessThan => candidate < threshold,
                PivotValueFilterType.LessThanOrEqual => candidate <= threshold,
                PivotValueFilterType.Equal => Math.Abs(candidate - threshold) < double.Epsilon,
                PivotValueFilterType.NotEqual => Math.Abs(candidate - threshold) >= double.Epsilon,
                PivotValueFilterType.Between => candidate >= Math.Min(threshold, threshold2) && candidate <= Math.Max(threshold, threshold2),
                _ => true
            };
        }

        private static int ResolveValueFieldIndex(PivotValueField? field, IList<PivotValueField> valueFields)
        {
            if (valueFields.Count == 0)
            {
                return -1;
            }

            if (field == null)
            {
                return 0;
            }

            var index = valueFields.IndexOf(field);
            if (index >= 0)
            {
                return index;
            }

            var key = field.Key;
            if (key == null)
            {
                return -1;
            }

            for (var i = 0; i < valueFields.Count; i++)
            {
                if (Equals(valueFields[i].Key, key))
                {
                    return i;
                }
            }

            return -1;
        }

        private static object? GetAggregateValue(
            object?[] pathValues,
            int valueIndex,
            bool isRowAxis,
            IList<PivotValueField> valueFields,
            Dictionary<PivotCellKey, PivotCellState> cellStates,
            PivotFormulaEvaluator? formulaEvaluator)
        {
            var rowKey = isRowAxis ? pathValues : Array.Empty<object?>();
            var columnKey = isRowAxis ? Array.Empty<object?>() : pathValues;

            if (formulaEvaluator != null &&
                valueIndex >= 0 &&
                valueIndex < valueFields.Count &&
                valueFields[valueIndex].IsCalculated)
            {
                return formulaEvaluator.EvaluateAt(
                    valueIndex,
                    cellStates,
                    rowKey,
                    columnKey,
                    null,
                    null);
            }

            if (!TryGetCellState(cellStates, rowKey, columnKey, out var state))
            {
                return null;
            }

            return state.GetResult(valueIndex);
        }

        private static int CompareAggregateValues(object? leftValue, object? rightValue, IComparer<object?> comparer)
        {
            var hasLeft = PivotNumeric.TryGetDouble(leftValue, out var leftNumber);
            var hasRight = PivotNumeric.TryGetDouble(rightValue, out var rightNumber);

            if (hasLeft && hasRight)
            {
                return leftNumber.CompareTo(rightNumber);
            }

            if (leftValue == null && rightValue == null)
            {
                return 0;
            }

            if (leftValue == null)
            {
                return 1;
            }

            if (rightValue == null)
            {
                return -1;
            }

            return comparer.Compare(leftValue, rightValue);
        }

        private static List<PivotColumn> BuildColumns(
            PivotGroupNode root,
            IList<PivotAxisField> columnFields,
            IList<PivotValueField> valueFields,
            bool[] columnSubtotalLevels,
            PivotLayoutOptions layout,
            bool valueFieldsInColumns,
            CultureInfo culture)
        {
            var columns = new List<PivotColumn>();
            var columnFieldCount = columnFields.Count;

            if (columnFieldCount == 0)
            {
                var columnsForRoot = CreateColumnsForNode(
                    root,
                    columnFields,
                    valueFields,
                    PivotColumnType.Detail,
                    layout,
                    valueFieldsInColumns,
                    culture);
                columns.AddRange(columnsForRoot);
                return columns;
            }

            void Visit(PivotGroupNode node)
            {
                if (node.Level == columnFieldCount - 1)
                {
                    columns.AddRange(CreateColumnsForNode(node, columnFields, valueFields, PivotColumnType.Detail, layout, valueFieldsInColumns, culture));
                    return;
                }

                var addSubtotal = node.Level >= 0 && node.Level < columnSubtotalLevels.Length && columnSubtotalLevels[node.Level];
                var subtotalAtStart = addSubtotal && columnFields[node.Level].SubtotalPosition == PivotTotalPosition.Start;

                if (addSubtotal && subtotalAtStart)
                {
                    columns.AddRange(CreateColumnsForNode(node, columnFields, valueFields, PivotColumnType.Subtotal, layout, valueFieldsInColumns, culture));
                }

                foreach (var child in node.Children)
                {
                    Visit(child);
                }

                if (addSubtotal && !subtotalAtStart)
                {
                    columns.AddRange(CreateColumnsForNode(node, columnFields, valueFields, PivotColumnType.Subtotal, layout, valueFieldsInColumns, culture));
                }
            }

            foreach (var child in root.Children)
            {
                Visit(child);
            }

            return columns;
        }

        private static List<PivotColumn> CreateColumnsForNode(
            PivotGroupNode node,
            IList<PivotAxisField> columnFields,
            IList<PivotValueField> valueFields,
            PivotColumnType columnType,
            PivotLayoutOptions layout,
            bool valueFieldsInColumns,
            CultureInfo culture)
        {
            var columns = new List<PivotColumn>();
            var displayValues = BuildColumnDisplayValues(node, columnFields, columnType, layout, culture);

            if (valueFieldsInColumns)
            {
                for (var i = 0; i < valueFields.Count; i++)
                {
                    var valueField = valueFields[i];
                    var header = BuildColumnHeader(displayValues, valueField, valueFieldsInColumns, layout);
                    columns.Add(new PivotColumn(
                        -1,
                        columnType,
                        BuildColumnPathValues(node),
                        displayValues,
                        valueField,
                        i,
                        header));
                }
            }
            else
            {
                var header = BuildColumnHeader(displayValues, null, valueFieldsInColumns, layout);
                columns.Add(new PivotColumn(
                    -1,
                    columnType,
                    BuildColumnPathValues(node),
                    displayValues,
                    null,
                    null,
                    header));
            }

            return columns;
        }

        private static object?[] BuildColumnPathValues(PivotGroupNode node)
        {
            if (node.PathValues.Length == 0)
            {
                return Array.Empty<object?>();
            }

            return CopyPathValues(node.PathValues);
        }

        private static object?[] CopyPathValues(object?[] source)
        {
            if (source.Length == 0)
            {
                return Array.Empty<object?>();
            }

            var values = new object?[source.Length];
            Array.Copy(source, values, values.Length);
            return values;
        }

        private static object?[] GetParentPath(object?[] source)
        {
            if (source.Length == 0)
            {
                return source;
            }

            var parent = new object?[source.Length - 1];
            if (parent.Length > 0)
            {
                Array.Copy(source, parent, parent.Length);
            }

            return parent;
        }

        private static string?[] BuildColumnDisplayValues(
            PivotGroupNode node,
            IList<PivotAxisField> columnFields,
            PivotColumnType columnType,
            PivotLayoutOptions layout,
            CultureInfo culture)
        {
            var columnFieldCount = columnFields.Count;
            var displayValues = new string?[columnFieldCount];
            if (node.PathDisplayValues.Length > 0)
            {
                Array.Copy(node.PathDisplayValues, displayValues, Math.Min(node.PathDisplayValues.Length, displayValues.Length));
            }

            if (columnType == PivotColumnType.Subtotal && node.Level >= 0 && node.Level < displayValues.Length)
            {
                var label = FormatSubtotalLabel(displayValues[node.Level], layout, culture);
                displayValues[node.Level] = label;
                for (var i = node.Level + 1; i < displayValues.Length; i++)
                {
                    displayValues[i] = null;
                }
            }
            else if (columnType == PivotColumnType.GrandTotal)
            {
                if (displayValues.Length > 0)
                {
                    displayValues[0] = layout.GrandTotalLabel;
                    for (var i = 1; i < displayValues.Length; i++)
                    {
                        displayValues[i] = null;
                    }
                }
            }

            return displayValues;
        }

        private static PivotHeader BuildColumnHeader(
            string?[] columnDisplayValues,
            PivotValueField? valueField,
            bool valueFieldsInColumns,
            PivotLayoutOptions layout)
        {
            var segments = new List<string>();
            foreach (var segment in columnDisplayValues)
            {
                if (!string.IsNullOrEmpty(segment))
                {
                    segments.Add(segment);
                }
            }

            if (valueFieldsInColumns)
            {
                var valueLabel = valueField?.Header ?? valueField?.Key?.ToString() ?? "Value";
                segments.Add(valueLabel);
            }

            if (segments.Count == 0)
            {
                segments.Add(layout.GrandTotalLabel);
            }

            return new PivotHeader(segments);
        }

        private static List<PivotRow> BuildRows(
            PivotGroupNode root,
            IList<PivotAxisField> rowFields,
            IList<PivotValueField> valueFields,
            bool[] rowSubtotalLevels,
            PivotLayoutOptions layout,
            int columnCount,
            CultureInfo culture)
        {
            var rows = new List<PivotRow>();
            var rowFieldCount = rowFields.Count;
            var valuesInRows = layout.ValuesPosition == PivotValuesPosition.Rows;

            if (rowFieldCount == 0)
            {
                if (valuesInRows)
                {
                    for (var i = 0; i < valueFields.Count; i++)
                    {
                        rows.Add(CreateRow(root, rowFields, layout, PivotRowType.GrandTotal, columnCount, valueFields[i], i, culture));
                    }
                }
                else
                {
                    rows.Add(CreateRow(root, rowFields, layout, PivotRowType.GrandTotal, columnCount, null, null, culture));
                }

                return rows;
            }

            void Visit(PivotGroupNode node)
            {
                if (node.Level == rowFieldCount - 1)
                {
                    if (valuesInRows)
                    {
                        for (var i = 0; i < valueFields.Count; i++)
                        {
                            rows.Add(CreateRow(node, rowFields, layout, PivotRowType.Detail, columnCount, valueFields[i], i, culture));
                        }
                    }
                    else
                    {
                        rows.Add(CreateRow(node, rowFields, layout, PivotRowType.Detail, columnCount, null, null, culture));
                    }

                    return;
                }

                var addSubtotal = node.Level >= 0 && node.Level < rowSubtotalLevels.Length && rowSubtotalLevels[node.Level];
                var subtotalAtStart = addSubtotal && rowFields[node.Level].SubtotalPosition == PivotTotalPosition.Start;

                if (addSubtotal && subtotalAtStart)
                {
                    if (valuesInRows)
                    {
                        for (var i = 0; i < valueFields.Count; i++)
                        {
                            rows.Add(CreateRow(node, rowFields, layout, PivotRowType.Subtotal, columnCount, valueFields[i], i, culture));
                        }
                    }
                    else
                    {
                        rows.Add(CreateRow(node, rowFields, layout, PivotRowType.Subtotal, columnCount, null, null, culture));
                    }
                }

                foreach (var child in node.Children)
                {
                    Visit(child);
                }

                if (addSubtotal && !subtotalAtStart)
                {
                    if (valuesInRows)
                    {
                        for (var i = 0; i < valueFields.Count; i++)
                        {
                            rows.Add(CreateRow(node, rowFields, layout, PivotRowType.Subtotal, columnCount, valueFields[i], i, culture));
                        }
                    }
                    else
                    {
                        rows.Add(CreateRow(node, rowFields, layout, PivotRowType.Subtotal, columnCount, null, null, culture));
                    }
                }
            }

            foreach (var child in root.Children)
            {
                Visit(child);
            }

            return rows;
        }

        private static List<PivotRow> CreateGrandTotalRows(
            PivotGroupNode root,
            IList<PivotAxisField> rowFields,
            IList<PivotValueField> valueFields,
            PivotLayoutOptions layout,
            int columnCount,
            CultureInfo culture)
        {
            var rows = new List<PivotRow>();
            if (layout.ValuesPosition == PivotValuesPosition.Rows)
            {
                for (var i = 0; i < valueFields.Count; i++)
                {
                    rows.Add(CreateRow(root, rowFields, layout, PivotRowType.GrandTotal, columnCount, valueFields[i], i, culture));
                }
            }
            else
            {
                rows.Add(CreateRow(root, rowFields, layout, PivotRowType.GrandTotal, columnCount, null, null, culture));
            }

            return rows;
        }

        private static PivotRow CreateRow(
            PivotGroupNode node,
            IList<PivotAxisField> rowFields,
            PivotLayoutOptions layout,
            PivotRowType rowType,
            int columnCount,
            PivotValueField? valueField,
            int? valueFieldIndex,
            CultureInfo culture)
        {
            var rowFieldCount = rowFields.Count;
            var valuesInRows = layout.ValuesPosition == PivotValuesPosition.Rows;
            var displayFieldCount = valuesInRows ? rowFieldCount + 1 : rowFieldCount;

            var rowPathValues = node.PathValues.Length == 0
                ? Array.Empty<object?>()
                : CopyPathValues(node.PathValues);

            var rowDisplayValues = new object?[displayFieldCount];
            if (rowType == PivotRowType.Detail)
            {
                if (node.PathDisplayValues.Length > 0)
                {
                    Array.Copy(node.PathDisplayValues, rowDisplayValues, Math.Min(node.PathDisplayValues.Length, rowFieldCount));
                }
            }
            else if (rowType == PivotRowType.Subtotal)
            {
                if (node.Level >= 0 && node.Level < rowFieldCount)
                {
                    for (var i = 0; i < node.Level; i++)
                    {
                        rowDisplayValues[i] = node.PathDisplayValues.Length > i ? node.PathDisplayValues[i] : null;
                    }

                    rowDisplayValues[node.Level] = FormatSubtotalLabel(
                        node.PathDisplayValues.Length > node.Level ? node.PathDisplayValues[node.Level] : null,
                        layout,
                        culture);
                }
            }
            else
            {
                if (rowDisplayValues.Length > 0 && rowFieldCount > 0)
                {
                    rowDisplayValues[0] = layout.GrandTotalLabel;
                }
            }

            if (valuesInRows && rowDisplayValues.Length > 0)
            {
                rowDisplayValues[rowDisplayValues.Length - 1] = valueField?.Header ?? valueField?.Key?.ToString() ?? "Value";
            }

            var level = Math.Max(0, node.Level);
            if (valuesInRows)
            {
                level++;
            }

            string? compactLabel = null;
            double indent = 0d;
            if (layout.RowLayout == PivotRowLayout.Compact)
            {
                indent = level * layout.CompactIndentSize;
                if (rowType == PivotRowType.Detail)
                {
                    compactLabel = node.PathDisplayValues.Length > 0
                        ? node.PathDisplayValues[node.PathDisplayValues.Length - 1]
                        : string.Empty;
                }
                else if (rowType == PivotRowType.Subtotal)
                {
                    compactLabel = FormatSubtotalLabel(
                        node.PathDisplayValues.Length > node.Level ? node.PathDisplayValues[node.Level] : null,
                        layout,
                        culture);
                }
                else
                {
                    compactLabel = layout.GrandTotalLabel;
                }

                if (valuesInRows)
                {
                    var valueLabel = valueField?.Header ?? valueField?.Key?.ToString() ?? "Value";
                    if (!string.IsNullOrEmpty(valueLabel))
                    {
                        compactLabel = string.IsNullOrEmpty(compactLabel)
                            ? valueLabel
                            : string.Concat(compactLabel, " / ", valueLabel);
                    }
                }
            }

            return new PivotRow(
                rowType,
                level,
                rowPathValues,
                rowDisplayValues,
                compactLabel,
                indent,
                columnCount,
                valueField,
                valueFieldIndex);
        }

        private static void ApplyRowLabelRepeats(
            List<PivotRow> rows,
            PivotLayoutOptions layout,
            int rowFieldCount,
            bool valuesInRows)
        {
            if (layout.RowLayout != PivotRowLayout.Tabular || layout.RepeatRowLabels)
            {
                return;
            }

            var lastValues = new object?[rowFieldCount];
            var reset = true;

            foreach (var row in rows)
            {
                if (row.RowType != PivotRowType.Detail)
                {
                    reset = true;
                    continue;
                }

                if (reset)
                {
                    Array.Clear(lastValues, 0, lastValues.Length);
                    reset = false;
                }

                for (var i = 0; i < rowFieldCount; i++)
                {
                    var current = row.RowDisplayValues.Length > i ? row.RowDisplayValues[i] : null;
                    if (current != null && lastValues[i] != null && current.Equals(lastValues[i]))
                    {
                        row.RowDisplayValues[i] = null;
                    }
                    else
                    {
                        lastValues[i] = current;
                    }
                }

            }
        }

        private static void FillCellValues(
            List<PivotRow> rows,
            List<PivotColumn> columns,
            Dictionary<PivotCellKey, PivotCellState> cellStates,
            PivotLayoutOptions layout,
            CultureInfo culture,
            int valueFieldCount,
            PivotFormulaEvaluator? formulaEvaluator)
        {
            var valuesInRows = layout.ValuesPosition == PivotValuesPosition.Rows;
            CollectDisplayModeUsage(
                rows,
                columns,
                out var usesSequenceModes,
                out var usesRowTotals,
                out var usesColumnTotals,
                out var usesGrandTotals,
                out var usesParentRowTotals,
                out var usesParentColumnTotals);

            if (formulaEvaluator != null)
            {
                var usage = formulaEvaluator.Usage;
                usesRowTotals |= usage.UsesRowTotals;
                usesColumnTotals |= usage.UsesColumnTotals;
                usesGrandTotals |= usage.UsesGrandTotals;
                usesParentRowTotals |= usage.UsesParentRowTotals;
                usesParentColumnTotals |= usage.UsesParentColumnTotals;
            }

            var rowParentPaths = usesParentRowTotals
                ? BuildParentPathLookup(rows)
                : null;
            var columnParentPaths = usesParentColumnTotals
                ? BuildParentPathLookup(columns)
                : null;

            var columnGroupKeys = usesSequenceModes
                ? BuildColumnGroupKeys(columns, valuesInRows, columnParentPaths)
                : Array.Empty<PivotGroupKey>();

            var totalsLookup = BuildTotalsLookup(
                rows,
                columns,
                cellStates,
                valueFieldCount,
                valuesInRows,
                usesRowTotals,
                usesColumnTotals,
                usesGrandTotals);

            Dictionary<PivotGroupKey, double>? runningTotals = null;
            Dictionary<PivotGroupKey, double>? previousValues = null;
            if (usesSequenceModes)
            {
                runningTotals = new Dictionary<PivotGroupKey, double>();
                previousValues = new Dictionary<PivotGroupKey, double>();
            }

            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                if (usesSequenceModes)
                {
                    runningTotals!.Clear();
                    previousValues!.Clear();
                }

                for (var columnIndex = 0; columnIndex < columns.Count; columnIndex++)
                {
                    var column = columns[columnIndex];
                    if (!TryGetCellRawValue(
                            row,
                            column,
                            cellStates,
                            valuesInRows,
                            formulaEvaluator,
                            rowParentPaths,
                            columnParentPaths,
                            out var valueField,
                            out var valueIndex,
                            out var rawValue))
                    {
                        row.SetCellValue(columnIndex, null);
                        continue;
                    }

                    var displayMode = GetEffectiveDisplayMode(valueField!);
                    var isSequenceMode = IsSequenceDisplayMode(displayMode);
                    if (isSequenceMode && row.RowType == PivotRowType.Detail && column.ColumnType == PivotColumnType.Detail)
                    {
                        var groupKey = columnGroupKeys.Length > columnIndex ? columnGroupKeys[columnIndex] : default;
                        var value = GetSequenceDisplayValue(
                            displayMode,
                            rawValue,
                            valueField,
                            runningTotals!,
                            previousValues!,
                            groupKey,
                            valuesInRows,
                            culture,
                            layout.EmptyValueLabel);
                        row.SetCellValue(columnIndex, value);
                    }
                    else
                    {
                        var value = GetCellDisplayValue(
                            row,
                            column,
                            cellStates,
                            layout,
                            valuesInRows,
                            culture,
                            valueField,
                            valueIndex,
                            rawValue,
                            rowIndex,
                            columnIndex,
                            totalsLookup,
                            rowParentPaths,
                            columnParentPaths);
                        row.SetCellValue(columnIndex, value);
                    }
                }
            }
        }

        private static void CollectDisplayModeUsage(
            List<PivotRow> rows,
            List<PivotColumn> columns,
            out bool usesSequenceModes,
            out bool usesRowTotals,
            out bool usesColumnTotals,
            out bool usesGrandTotals,
            out bool usesParentRowTotals,
            out bool usesParentColumnTotals)
        {
            var sequenceModes = false;
            var rowTotals = false;
            var columnTotals = false;
            var grandTotals = false;
            var parentRowTotals = false;
            var parentColumnTotals = false;

            void Evaluate(PivotValueDisplayMode displayMode)
            {
                if (IsSequenceDisplayMode(displayMode))
                {
                    sequenceModes = true;
                }

                switch (displayMode)
                {
                    case PivotValueDisplayMode.PercentOfRowTotal:
                        rowTotals = true;
                        break;
                    case PivotValueDisplayMode.PercentOfColumnTotal:
                        columnTotals = true;
                        break;
                    case PivotValueDisplayMode.PercentOfGrandTotal:
                        grandTotals = true;
                        break;
                    case PivotValueDisplayMode.PercentOfParentRowTotal:
                        parentRowTotals = true;
                        break;
                    case PivotValueDisplayMode.PercentOfParentColumnTotal:
                        parentColumnTotals = true;
                        break;
                    case PivotValueDisplayMode.Index:
                        rowTotals = true;
                        columnTotals = true;
                        grandTotals = true;
                        break;
                }
            }

            foreach (var column in columns)
            {
                var valueField = column.ValueField;
                if (valueField != null)
                {
                    Evaluate(GetEffectiveDisplayMode(valueField));
                }
            }

            foreach (var row in rows)
            {
                var valueField = row.ValueField;
                if (valueField != null)
                {
                    Evaluate(GetEffectiveDisplayMode(valueField));
                }
            }

            usesSequenceModes = sequenceModes;
            usesRowTotals = rowTotals;
            usesColumnTotals = columnTotals;
            usesGrandTotals = grandTotals;
            usesParentRowTotals = parentRowTotals;
            usesParentColumnTotals = parentColumnTotals;
        }

        private static PivotTotalsLookup BuildTotalsLookup(
            List<PivotRow> rows,
            List<PivotColumn> columns,
            Dictionary<PivotCellKey, PivotCellState> cellStates,
            int valueFieldCount,
            bool valuesInRows,
            bool usesRowTotals,
            bool usesColumnTotals,
            bool usesGrandTotals)
        {
            object?[]? rowTotals = null;
            object?[][]? rowTotalsByValueIndex = null;
            object?[]? columnTotals = null;
            object?[][]? columnTotalsByValueIndex = null;
            object?[]? grandTotals = null;

            if (usesRowTotals)
            {
                if (valuesInRows)
                {
                    rowTotals = new object?[rows.Count];
                    for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
                    {
                        var row = rows[rowIndex];
                        var valueIndex = row.ValueFieldIndex;
                        rowTotals[rowIndex] = valueIndex.HasValue
                            ? GetTotalValue(cellStates, row.RowPathValues, Array.Empty<object?>(), valueIndex.Value)
                            : null;
                    }
                }
                else
                {
                    rowTotalsByValueIndex = new object?[valueFieldCount][];
                    for (var valueIndex = 0; valueIndex < valueFieldCount; valueIndex++)
                    {
                        var totals = new object?[rows.Count];
                        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
                        {
                            totals[rowIndex] = GetTotalValue(cellStates, rows[rowIndex].RowPathValues, Array.Empty<object?>(), valueIndex);
                        }

                        rowTotalsByValueIndex[valueIndex] = totals;
                    }
                }
            }

            if (usesColumnTotals)
            {
                if (valuesInRows)
                {
                    columnTotalsByValueIndex = new object?[valueFieldCount][];
                    for (var valueIndex = 0; valueIndex < valueFieldCount; valueIndex++)
                    {
                        var totals = new object?[columns.Count];
                        for (var columnIndex = 0; columnIndex < columns.Count; columnIndex++)
                        {
                            totals[columnIndex] = GetTotalValue(cellStates, Array.Empty<object?>(), columns[columnIndex].ColumnPathValues, valueIndex);
                        }

                        columnTotalsByValueIndex[valueIndex] = totals;
                    }
                }
                else
                {
                    columnTotals = new object?[columns.Count];
                    for (var columnIndex = 0; columnIndex < columns.Count; columnIndex++)
                    {
                        var column = columns[columnIndex];
                        var valueIndex = column.ValueFieldIndex;
                        columnTotals[columnIndex] = valueIndex.HasValue
                            ? GetTotalValue(cellStates, Array.Empty<object?>(), column.ColumnPathValues, valueIndex.Value)
                            : null;
                    }
                }
            }

            if (usesGrandTotals)
            {
                grandTotals = new object?[valueFieldCount];
                for (var valueIndex = 0; valueIndex < valueFieldCount; valueIndex++)
                {
                    grandTotals[valueIndex] = GetTotalValue(cellStates, Array.Empty<object?>(), Array.Empty<object?>(), valueIndex);
                }
            }

            return new PivotTotalsLookup(
                valuesInRows,
                rowTotals,
                rowTotalsByValueIndex,
                columnTotals,
                columnTotalsByValueIndex,
                grandTotals);
        }

        private readonly struct PivotTotalsLookup
        {
            private readonly bool _valuesInRows;
            private readonly object?[]? _rowTotals;
            private readonly object?[][]? _rowTotalsByValueIndex;
            private readonly object?[]? _columnTotals;
            private readonly object?[][]? _columnTotalsByValueIndex;
            private readonly object?[]? _grandTotals;

            public PivotTotalsLookup(
                bool valuesInRows,
                object?[]? rowTotals,
                object?[][]? rowTotalsByValueIndex,
                object?[]? columnTotals,
                object?[][]? columnTotalsByValueIndex,
                object?[]? grandTotals)
            {
                _valuesInRows = valuesInRows;
                _rowTotals = rowTotals;
                _rowTotalsByValueIndex = rowTotalsByValueIndex;
                _columnTotals = columnTotals;
                _columnTotalsByValueIndex = columnTotalsByValueIndex;
                _grandTotals = grandTotals;
            }

            public bool HasRowTotals => _rowTotals != null || _rowTotalsByValueIndex != null;

            public bool HasColumnTotals => _columnTotals != null || _columnTotalsByValueIndex != null;

            public bool HasGrandTotals => _grandTotals != null;

            public object? GetRowTotal(int rowIndex, int valueIndex)
            {
                if (_valuesInRows)
                {
                    return _rowTotals != null && (uint)rowIndex < (uint)_rowTotals.Length
                        ? _rowTotals[rowIndex]
                        : null;
                }

                if (_rowTotalsByValueIndex != null && (uint)valueIndex < (uint)_rowTotalsByValueIndex.Length)
                {
                    var totals = _rowTotalsByValueIndex[valueIndex];
                    return totals != null && (uint)rowIndex < (uint)totals.Length
                        ? totals[rowIndex]
                        : null;
                }

                return null;
            }

            public object? GetColumnTotal(int columnIndex, int valueIndex)
            {
                if (_valuesInRows)
                {
                    if (_columnTotalsByValueIndex != null && (uint)valueIndex < (uint)_columnTotalsByValueIndex.Length)
                    {
                        var totals = _columnTotalsByValueIndex[valueIndex];
                        return totals != null && (uint)columnIndex < (uint)totals.Length
                            ? totals[columnIndex]
                            : null;
                    }

                    return null;
                }

                return _columnTotals != null && (uint)columnIndex < (uint)_columnTotals.Length
                    ? _columnTotals[columnIndex]
                    : null;
            }

            public object? GetGrandTotal(int valueIndex)
            {
                return _grandTotals != null && (uint)valueIndex < (uint)_grandTotals.Length
                    ? _grandTotals[valueIndex]
                    : null;
            }
        }

        private static object? GetCellValue(
            PivotRow row,
            PivotColumn column,
            Dictionary<PivotCellKey, PivotCellState> cellStates,
            PivotLayoutOptions layout,
            bool valuesInRows,
            CultureInfo culture,
            PivotFormulaEvaluator? formulaEvaluator,
            Dictionary<object?[], object?[]>? rowParentPaths,
            Dictionary<object?[], object?[]>? columnParentPaths)
        {
            if (!TryGetCellRawValue(
                    row,
                    column,
                    cellStates,
                    valuesInRows,
                    formulaEvaluator,
                    rowParentPaths,
                    columnParentPaths,
                    out var valueField,
                    out var valueIndex,
                    out var rawValue))
            {
                return null;
            }
            return GetCellDisplayValue(
                row,
                column,
                cellStates,
                layout,
                valuesInRows,
                culture,
                valueField!,
                valueIndex,
                rawValue,
                0,
                0,
                default,
                null,
                null);
        }

        private static bool TryGetCellRawValue(
            PivotRow row,
            PivotColumn column,
            Dictionary<PivotCellKey, PivotCellState> cellStates,
            bool valuesInRows,
            PivotFormulaEvaluator? formulaEvaluator,
            Dictionary<object?[], object?[]>? rowParentPaths,
            Dictionary<object?[], object?[]>? columnParentPaths,
            out PivotValueField? valueField,
            out int valueIndex,
            out object? rawValue)
        {
            rawValue = null;
            valueField = valuesInRows ? row.ValueField : column.ValueField;
            valueIndex = -1;

            if (valueField == null)
            {
                return false;
            }

            var index = valuesInRows ? row.ValueFieldIndex : column.ValueFieldIndex;
            if (!index.HasValue)
            {
                return false;
            }

            valueIndex = index.Value;
            if (formulaEvaluator != null && valueField.IsCalculated)
            {
                var context = formulaEvaluator.CreateContext(
                    cellStates,
                    row.RowPathValues,
                    column.ColumnPathValues,
                    rowParentPaths,
                    columnParentPaths);
                rawValue = context.ResolveValue(valueIndex);
                return true;
            }

            if (!TryGetCellState(cellStates, row.RowPathValues, column.ColumnPathValues, out var state))
            {
                return false;
            }

            rawValue = state.GetResult(valueIndex);
            return true;
        }

        private static object? GetCellDisplayValue(
            PivotRow row,
            PivotColumn column,
            Dictionary<PivotCellKey, PivotCellState> cellStates,
            PivotLayoutOptions layout,
            bool valuesInRows,
            CultureInfo culture,
            PivotValueField valueField,
            int valueIndex,
            object? rawValue,
            int rowIndex,
            int columnIndex,
            in PivotTotalsLookup totalsLookup,
            Dictionary<object?[], object?[]>? rowParentPaths,
            Dictionary<object?[], object?[]>? columnParentPaths)
        {
            if (rawValue == null)
            {
                return valuesInRows
                    ? FormatRowValue(valueField, rawValue, culture, layout.EmptyValueLabel)
                    : null;
            }

            var displayMode = GetEffectiveDisplayMode(valueField);
            switch (displayMode)
            {
                case PivotValueDisplayMode.Value:
                    return valuesInRows ? FormatRowValue(valueField, rawValue, culture, layout.EmptyValueLabel) : rawValue;
                case PivotValueDisplayMode.Index:
                    return GetIndexValue(
                        row,
                        column,
                        cellStates,
                        layout,
                        valuesInRows,
                        culture,
                        valueField,
                        valueIndex,
                        rawValue,
                        rowIndex,
                        columnIndex,
                        totalsLookup);
                case PivotValueDisplayMode.PercentOfRowTotal:
                case PivotValueDisplayMode.PercentOfColumnTotal:
                case PivotValueDisplayMode.PercentOfGrandTotal:
                case PivotValueDisplayMode.PercentOfParentRowTotal:
                case PivotValueDisplayMode.PercentOfParentColumnTotal:
                    var denominator = GetDisplayDenominator(
                        row,
                        column,
                        cellStates,
                        valueField,
                        valueIndex,
                        rowIndex,
                        columnIndex,
                        totalsLookup,
                        rowParentPaths,
                        columnParentPaths);
                    if (denominator == null)
                    {
                        return null;
                    }

                    if (!PivotNumeric.TryGetDouble(rawValue, out var numeratorValue))
                    {
                        return null;
                    }

                    if (!PivotNumeric.TryGetDouble(denominator, out var denominatorValue))
                    {
                        return null;
                    }

                    if (Math.Abs(denominatorValue) < double.Epsilon)
                    {
                        return null;
                    }

                    var ratio = numeratorValue / denominatorValue;
                    return valuesInRows ? FormatRowValue(valueField, ratio, culture, layout.EmptyValueLabel) : ratio;
                default:
                    return valuesInRows ? FormatRowValue(valueField, rawValue, culture, layout.EmptyValueLabel) : rawValue;
            }
        }

        private static object? GetIndexValue(
            PivotRow row,
            PivotColumn column,
            Dictionary<PivotCellKey, PivotCellState> cellStates,
            PivotLayoutOptions layout,
            bool valuesInRows,
            CultureInfo culture,
            PivotValueField valueField,
            int valueIndex,
            object? rawValue,
            int rowIndex,
            int columnIndex,
            in PivotTotalsLookup totalsLookup)
        {
            var rowTotal = totalsLookup.HasRowTotals
                ? totalsLookup.GetRowTotal(rowIndex, valueIndex)
                : GetTotalValue(cellStates, row.RowPathValues, Array.Empty<object?>(), valueIndex);
            var columnTotal = totalsLookup.HasColumnTotals
                ? totalsLookup.GetColumnTotal(columnIndex, valueIndex)
                : GetTotalValue(cellStates, Array.Empty<object?>(), column.ColumnPathValues, valueIndex);
            var grandTotal = totalsLookup.HasGrandTotals
                ? totalsLookup.GetGrandTotal(valueIndex)
                : GetTotalValue(cellStates, Array.Empty<object?>(), Array.Empty<object?>(), valueIndex);

            if (!PivotNumeric.TryGetDouble(rawValue, out var cellValue) ||
                !PivotNumeric.TryGetDouble(rowTotal, out var rowValue) ||
                !PivotNumeric.TryGetDouble(columnTotal, out var columnValue) ||
                !PivotNumeric.TryGetDouble(grandTotal, out var grandValue))
            {
                return null;
            }

            if (Math.Abs(rowValue) < double.Epsilon || Math.Abs(columnValue) < double.Epsilon)
            {
                return null;
            }

            var result = (cellValue * grandValue) / (rowValue * columnValue);
            return valuesInRows ? FormatRowValue(valueField, result, culture, layout.EmptyValueLabel) : result;
        }

        private static object? GetSequenceDisplayValue(
            PivotValueDisplayMode displayMode,
            object? rawValue,
            PivotValueField valueField,
            Dictionary<PivotGroupKey, double> runningTotals,
            Dictionary<PivotGroupKey, double> previousValues,
            PivotGroupKey groupKey,
            bool valuesInRows,
            CultureInfo culture,
            string? emptyValueLabel)
        {
            if (!PivotNumeric.TryGetDouble(rawValue, out var currentValue))
            {
                return null;
            }

            double result;
            switch (displayMode)
            {
                case PivotValueDisplayMode.RunningTotal:
                    runningTotals.TryGetValue(groupKey, out var runningTotal);
                    runningTotal += currentValue;
                    runningTotals[groupKey] = runningTotal;
                    result = runningTotal;
                    break;
                case PivotValueDisplayMode.DifferenceFromPrevious:
                    if (!previousValues.TryGetValue(groupKey, out var previousValue))
                    {
                        previousValues[groupKey] = currentValue;
                        return null;
                    }

                    result = currentValue - previousValue;
                    previousValues[groupKey] = currentValue;
                    break;
                case PivotValueDisplayMode.PercentDifferenceFromPrevious:
                    if (!previousValues.TryGetValue(groupKey, out var previousPercentValue))
                    {
                        previousValues[groupKey] = currentValue;
                        return null;
                    }

                    if (Math.Abs(previousPercentValue) < double.Epsilon)
                    {
                        previousValues[groupKey] = currentValue;
                        return null;
                    }

                    result = (currentValue - previousPercentValue) / previousPercentValue;
                    previousValues[groupKey] = currentValue;
                    break;
                default:
                    result = currentValue;
                    break;
            }

            return valuesInRows ? FormatRowValue(valueField, result, culture, emptyValueLabel) : result;
        }

        private static bool UsesSequenceDisplayModes(List<PivotRow> rows, List<PivotColumn> columns)
        {
            foreach (var column in columns)
            {
                var valueField = column.ValueField;
                if (valueField != null && IsSequenceDisplayMode(GetEffectiveDisplayMode(valueField)))
                {
                    return true;
                }
            }

            foreach (var row in rows)
            {
                var valueField = row.ValueField;
                if (valueField != null && IsSequenceDisplayMode(GetEffectiveDisplayMode(valueField)))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSequenceDisplayMode(PivotValueDisplayMode displayMode)
        {
            return displayMode == PivotValueDisplayMode.RunningTotal ||
                   displayMode == PivotValueDisplayMode.DifferenceFromPrevious ||
                   displayMode == PivotValueDisplayMode.PercentDifferenceFromPrevious;
        }

        private static PivotValueDisplayMode GetEffectiveDisplayMode(PivotValueField? field)
        {
            if (field == null || field.IsCalculated)
            {
                return PivotValueDisplayMode.Value;
            }

            return field.DisplayMode;
        }

        private static PivotGroupKey[] BuildColumnGroupKeys(
            List<PivotColumn> columns,
            bool valuesInRows,
            Dictionary<object?[], object?[]>? parentPaths)
        {
            var keys = new PivotGroupKey[columns.Count];
            for (var i = 0; i < columns.Count; i++)
            {
                var column = columns[i];
                if (column.ColumnType != PivotColumnType.Detail)
                {
                    keys[i] = default;
                    continue;
                }

                var parentPath = TryGetParentPath(column.ColumnPathValues, parentPaths);
                var valueFieldIndex = valuesInRows ? null : column.ValueFieldIndex;
                keys[i] = new PivotGroupKey(parentPath, valueFieldIndex);
            }

            return keys;
        }

        private static bool IsPercentDisplayMode(PivotValueDisplayMode displayMode)
        {
            return displayMode == PivotValueDisplayMode.PercentOfRowTotal ||
                   displayMode == PivotValueDisplayMode.PercentOfColumnTotal ||
                   displayMode == PivotValueDisplayMode.PercentOfGrandTotal ||
                   displayMode == PivotValueDisplayMode.PercentOfParentRowTotal ||
                   displayMode == PivotValueDisplayMode.PercentOfParentColumnTotal ||
                   displayMode == PivotValueDisplayMode.PercentDifferenceFromPrevious;
        }

        private static object? GetDisplayDenominator(
            PivotRow row,
            PivotColumn column,
            Dictionary<PivotCellKey, PivotCellState> cellStates,
            PivotValueField valueField,
            int valueIndex,
            int rowIndex,
            int columnIndex,
            in PivotTotalsLookup totalsLookup,
            Dictionary<object?[], object?[]>? rowParentPaths,
            Dictionary<object?[], object?[]>? columnParentPaths)
        {
            var displayMode = GetEffectiveDisplayMode(valueField);
            switch (displayMode)
            {
                case PivotValueDisplayMode.PercentOfRowTotal:
                    return totalsLookup.HasRowTotals
                        ? totalsLookup.GetRowTotal(rowIndex, valueIndex)
                        : GetTotalValue(cellStates, row.RowPathValues, Array.Empty<object?>(), valueIndex);
                case PivotValueDisplayMode.PercentOfColumnTotal:
                    return totalsLookup.HasColumnTotals
                        ? totalsLookup.GetColumnTotal(columnIndex, valueIndex)
                        : GetTotalValue(cellStates, Array.Empty<object?>(), column.ColumnPathValues, valueIndex);
                case PivotValueDisplayMode.PercentOfGrandTotal:
                    return totalsLookup.HasGrandTotals
                        ? totalsLookup.GetGrandTotal(valueIndex)
                        : GetTotalValue(cellStates, Array.Empty<object?>(), Array.Empty<object?>(), valueIndex);
                case PivotValueDisplayMode.PercentOfParentRowTotal:
                    var parentRow = TryGetParentPath(row.RowPathValues, rowParentPaths);
                    if (parentRow.Length == row.RowPathValues.Length)
                    {
                        return null;
                    }

                    return GetTotalValue(cellStates, parentRow, column.ColumnPathValues, valueIndex);
                case PivotValueDisplayMode.PercentOfParentColumnTotal:
                    var parentColumn = TryGetParentPath(column.ColumnPathValues, columnParentPaths);
                    if (parentColumn.Length == column.ColumnPathValues.Length)
                    {
                        return null;
                    }

                    return GetTotalValue(cellStates, row.RowPathValues, parentColumn, valueIndex);
                default:
                    return null;
            }
        }

        private static Dictionary<object?[], object?[]> BuildParentPathLookup<T>(IEnumerable<T> items)
        {
            var lookup = new Dictionary<object?[], object?[]>();
            foreach (var item in items)
            {
                object?[]? pathValues = null;
                switch (item)
                {
                    case PivotRow row:
                        pathValues = row.RowPathValues;
                        break;
                    case PivotColumn column:
                        pathValues = column.ColumnPathValues;
                        break;
                }

                if (pathValues == null)
                {
                    continue;
                }

                lookup[pathValues] = GetParentPath(pathValues);
            }

            return lookup;
        }

        private static object?[] TryGetParentPath(
            object?[] pathValues,
            Dictionary<object?[], object?[]>? lookup)
        {
            if (lookup != null && lookup.TryGetValue(pathValues, out var parent))
            {
                return parent;
            }

            return GetParentPath(pathValues);
        }

        private static bool UsesDisplayMode(List<PivotRow> rows, List<PivotColumn> columns, PivotValueDisplayMode mode)
        {
            foreach (var column in columns)
            {
                var valueField = column.ValueField;
                if (valueField != null && GetEffectiveDisplayMode(valueField) == mode)
                {
                    return true;
                }
            }

            foreach (var row in rows)
            {
                var valueField = row.ValueField;
                if (valueField != null && GetEffectiveDisplayMode(valueField) == mode)
                {
                    return true;
                }
            }

            return false;
        }

        private static object? FormatRowValue(PivotValueField field, object? value, CultureInfo culture, string? emptyValueLabel)
        {
            if (value == null)
            {
                if (!string.IsNullOrEmpty(field.NullLabel))
                {
                    return field.NullLabel;
                }

                return emptyValueLabel ?? null;
            }

            if (field.Converter != null)
            {
                var converterCulture = field.FormatProvider as CultureInfo ?? culture;
                return field.Converter.Convert(value, typeof(string), field.ConverterParameter, converterCulture);
            }

            var format = field.StringFormat;
            if (IsPercentDisplayMode(GetEffectiveDisplayMode(field)) && string.IsNullOrEmpty(format))
            {
                format = "P2";
            }

            if (!string.IsNullOrEmpty(format))
            {
                try
                {
                    if (format.Contains("{0"))
                    {
                        var provider = field.FormatProvider ?? culture;
                        return string.Format(provider, format, value);
                    }

                    if (value is IFormattable formattable)
                    {
                        return formattable.ToString(format, field.FormatProvider ?? culture);
                    }
                }
                catch
                {
                    // ignore formatting errors and fall back
                }
            }

            return value;
        }

        private static object? GetTotalValue(
            Dictionary<PivotCellKey, PivotCellState> cellStates,
            object?[] rowKey,
            object?[] columnKey,
            int valueIndex)
        {
            if (!TryGetCellState(cellStates, rowKey, columnKey, out var state))
            {
                return null;
            }

            return state.GetResult(valueIndex);
        }

        private static bool TryGetCellState(
            Dictionary<PivotCellKey, PivotCellState> cellStates,
            object?[] rowKey,
            object?[] columnKey,
            out PivotCellState state)
        {
            return cellStates.TryGetValue(new PivotCellKey(rowKey, columnKey), out state!);
        }

        private static List<DataGridColumnDefinition> BuildColumnDefinitions(
            IList<PivotAxisField> rowFields,
            IList<PivotValueField> valueFields,
            List<PivotColumn> columns,
            PivotLayoutOptions layout,
            CultureInfo culture)
        {
            var definitions = new List<DataGridColumnDefinition>();
            var valuesInRows = layout.ValuesPosition == PivotValuesPosition.Rows;

            if (layout.RowLayout == PivotRowLayout.Compact)
            {
                var rowHeader = new DataGridTemplateColumnDefinition
                {
                    Header = layout.RowHeaderLabel,
                    CellTemplateKey = "DataGridPivotRowHeaderTemplate",
                    IsReadOnly = true,
                    CanUserSort = false,
                    CanUserReorder = false,
                    CanUserResize = true
                };

                rowHeader.CellStyleClasses = new List<string> { "pivot-row-header" };
                rowHeader.HeaderStyleClasses = new List<string> { "pivot-row-header" };
                rowHeader.ValueAccessor = CreateCompactLabelAccessor();
                rowHeader.Options = CreateSearchOptions(GetCompactLabelText);
                definitions.Add(rowHeader);
            }
            else
            {
                for (var i = 0; i < rowFields.Count; i++)
                {
                    var field = rowFields[i];
                    var binding = CreateArrayBinding(
                        RowDisplayValuesProperty,
                        RowDisplayValuesGetter,
                        i,
                        null,
                        null,
                        null,
                        null,
                        null);
                    var column = new DataGridTextColumnDefinition
                    {
                        Header = field.Header ?? field.Key?.ToString() ?? "Field",
                        Binding = binding,
                        IsReadOnly = true,
                        CanUserSort = false,
                        CanUserReorder = false,
                        CanUserResize = true
                    };

                    column.CellStyleClasses = new List<string> { "pivot-row-header" };
                    column.HeaderStyleClasses = new List<string> { "pivot-row-header" };
                    column.ValueAccessor = CreateRowPathValueAccessor(i);
                    column.Options = CreateSearchOptions(CreateRowDisplayTextProvider(i, culture));
                    definitions.Add(column);
                }
            }

            if (valuesInRows && layout.RowLayout == PivotRowLayout.Tabular)
            {
                var binding = CreateArrayBinding(
                    RowDisplayValuesProperty,
                    RowDisplayValuesGetter,
                    rowFields.Count,
                    null,
                    null,
                    null,
                    null,
                    null);
                var valueFieldColumn = new DataGridTextColumnDefinition
                {
                    Header = layout.ValuesHeaderLabel,
                    Binding = binding,
                    IsReadOnly = true,
                    CanUserSort = false,
                    CanUserReorder = false,
                    CanUserResize = true
                };

                valueFieldColumn.CellStyleClasses = new List<string> { "pivot-values-header" };
                valueFieldColumn.HeaderStyleClasses = new List<string> { "pivot-values-header" };
                valueFieldColumn.ValueAccessor = CreateRowDisplayValueAccessor(rowFields.Count);
                valueFieldColumn.Options = CreateSearchOptions(CreateRowDisplayTextProvider(rowFields.Count, culture));
                definitions.Add(valueFieldColumn);
            }

            var valuesAsColumns = layout.ValuesPosition == PivotValuesPosition.Columns;
            for (var i = 0; i < columns.Count; i++)
            {
                var columnInfo = columns[i];
                var valueField = valuesAsColumns ? columnInfo.ValueField : null;

                var stringFormat = valueField?.StringFormat;
                if (valueField != null && IsPercentDisplayMode(GetEffectiveDisplayMode(valueField)) && string.IsNullOrEmpty(stringFormat))
                {
                    stringFormat = "P2";
                }

                var converterCulture = valueField?.FormatProvider as CultureInfo ?? culture;
                var isNumericColumn = !valuesInRows && IsNumericValueField(valueField);
                var targetNullValue = isNumericColumn ? null : GetTargetNullValue(valueField, layout.EmptyValueLabel);
                var bindingStringFormat = isNumericColumn ? null : stringFormat;
                var binding = CreateArrayBinding(
                    CellValuesProperty,
                    CellValuesGetter,
                    i,
                    valueField?.Converter,
                    valueField?.ConverterParameter,
                    bindingStringFormat,
                    targetNullValue,
                    converterCulture);

                DataGridColumnDefinition columnDefinition;
                if (isNumericColumn)
                {
                    var numericColumn = new DataGridNumericColumnDefinition
                    {
                        Binding = binding,
                        IsReadOnly = true,
                        CanUserSort = false,
                        CanUserReorder = false,
                        CanUserResize = true
                    };

                    if (!string.IsNullOrEmpty(stringFormat))
                    {
                        numericColumn.FormatString = stringFormat;
                    }

                    if (valueField?.FormatProvider is NumberFormatInfo numberFormat)
                    {
                        numericColumn.NumberFormat = numberFormat;
                    }

                    columnDefinition = numericColumn;
                }
                else
                {
                    columnDefinition = new DataGridTextColumnDefinition
                    {
                        Binding = binding,
                        IsReadOnly = true,
                        CanUserSort = false,
                        CanUserReorder = false,
                        CanUserResize = true
                    };
                }

                columnDefinition.Header = columnInfo.Header;
                columnDefinition.HeaderTemplateKey = "DataGridPivotHeaderTemplate";
                columnDefinition.ColumnKey = columnInfo;
                columnDefinition.CellStyleClasses = BuildColumnStyleClasses(columnInfo);
                columnDefinition.HeaderStyleClasses = BuildColumnStyleClasses(columnInfo);
                columnDefinition.ValueAccessor = CreateCellValueAccessor(i);
                columnDefinition.Options = CreateSearchOptions(CreateCellValueTextProvider(
                    i,
                    valueField,
                    layout,
                    culture,
                    valuesInRows,
                    isNumericColumn));
                definitions.Add(columnDefinition);
            }

            return definitions;
        }

        private static List<string> BuildColumnStyleClasses(PivotColumn column)
        {
            var classes = new List<string> { "pivot-value" };
            switch (column.ColumnType)
            {
                case PivotColumnType.Subtotal:
                    classes.Add("pivot-subtotal");
                    break;
                case PivotColumnType.GrandTotal:
                    classes.Add("pivot-grandtotal");
                    break;
            }

            return classes;
        }

        private static object? GetTargetNullValue(PivotValueField? field, string? emptyValueLabel)
        {
            if (field != null && !string.IsNullOrEmpty(field.NullLabel))
            {
                return field.NullLabel;
            }

            if (!string.IsNullOrEmpty(emptyValueLabel))
            {
                return emptyValueLabel;
            }

            return null;
        }

        private static bool IsNumericValueField(PivotValueField? field)
        {
            if (field == null)
            {
                return false;
            }

            if (field.ValueType != null)
            {
                var type = Nullable.GetUnderlyingType(field.ValueType) ?? field.ValueType;
                return type == typeof(byte) ||
                    type == typeof(sbyte) ||
                    type == typeof(short) ||
                    type == typeof(ushort) ||
                    type == typeof(int) ||
                    type == typeof(uint) ||
                    type == typeof(long) ||
                    type == typeof(ulong) ||
                    type == typeof(float) ||
                    type == typeof(double) ||
                    type == typeof(decimal);
            }

            return field.AggregateType != PivotAggregateType.None &&
                   field.AggregateType != PivotAggregateType.First &&
                   field.AggregateType != PivotAggregateType.Last;
        }

        private static string FormatSubtotalLabel(string? value, PivotLayoutOptions layout, CultureInfo culture)
        {
            var text = value ?? string.Empty;
            try
            {
                if (!string.IsNullOrEmpty(layout.SubtotalLabelFormat))
                {
                    return string.Format(culture, layout.SubtotalLabelFormat, text);
                }
            }
            catch
            {
                return string.Concat(text, " Total");
            }

            return string.Concat(text, " Total");
        }

        private sealed class PivotGroupNode
        {
            private static readonly object NullKey = new();
            private readonly Dictionary<object, PivotGroupNode> _childrenLookup;

            public PivotGroupNode(PivotGroupNode? parent, int level, object? key, object?[] pathValues, string?[] pathDisplayValues)
            {
                Parent = parent;
                Level = level;
                Key = key;
                PathValues = pathValues;
                PathDisplayValues = pathDisplayValues;
                Children = new List<PivotGroupNode>();
                _childrenLookup = new Dictionary<object, PivotGroupNode>();
            }

            public PivotGroupNode? Parent { get; }

            public int Level { get; }

            public object? Key { get; }

            public object?[] PathValues { get; }

            public string?[] PathDisplayValues { get; }

            public List<PivotGroupNode> Children { get; }

            public PivotGroupNode GetOrCreateChild(PivotAxisField field, object? value, CultureInfo culture, string? emptyValueLabel)
            {
                var lookupKey = value ?? NullKey;
                if (_childrenLookup.TryGetValue(lookupKey, out var existing))
                {
                    return existing;
                }

                var pathValues = new object?[PathValues.Length + 1];
                var pathDisplayValues = new string?[PathDisplayValues.Length + 1];
                if (PathValues.Length > 0)
                {
                    Array.Copy(PathValues, pathValues, PathValues.Length);
                }

                if (PathDisplayValues.Length > 0)
                {
                    Array.Copy(PathDisplayValues, pathDisplayValues, PathDisplayValues.Length);
                }

                pathValues[pathValues.Length - 1] = value;
                pathDisplayValues[pathDisplayValues.Length - 1] = field.FormatValue(value, culture, emptyValueLabel);

                var node = new PivotGroupNode(this, Level + 1, value, pathValues, pathDisplayValues);
                _childrenLookup[lookupKey] = node;
                Children.Add(node);
                return node;
            }

            public void FilterChildren(Func<PivotGroupNode, bool> predicate)
            {
                if (Children.Count == 0)
                {
                    return;
                }

                var kept = new List<PivotGroupNode>(Children.Count);
                foreach (var child in Children)
                {
                    if (predicate(child))
                    {
                        kept.Add(child);
                    }
                }

                if (kept.Count == Children.Count)
                {
                    return;
                }

                Children.Clear();
                _childrenLookup.Clear();

                foreach (var child in kept)
                {
                    var lookupKey = child.Key ?? NullKey;
                    _childrenLookup[lookupKey] = child;
                    Children.Add(child);
                }
            }
        }

        private readonly struct PivotGroupKey : IEquatable<PivotGroupKey>
        {
            public PivotGroupKey(object?[] pathValues, int? valueFieldIndex)
            {
                PathValues = pathValues;
                ValueFieldIndex = valueFieldIndex;
            }

            public object?[] PathValues { get; }

            public int? ValueFieldIndex { get; }

            public bool Equals(PivotGroupKey other)
            {
                return ValueFieldIndex == other.ValueFieldIndex &&
                       PathMatches(PathValues ?? Array.Empty<object?>(), other.PathValues ?? Array.Empty<object?>());
            }

            public override bool Equals(object? obj)
            {
                return obj is PivotGroupKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = ValueFieldIndex.GetHashCode();
                    var values = PathValues ?? Array.Empty<object?>();
                    for (var i = 0; i < values.Length; i++)
                    {
                        hash = hash * 31 + (values[i]?.GetHashCode() ?? 0);
                    }

                    return hash;
                }
            }

            private static bool PathMatches(object?[] left, object?[] right)
            {
                if (left.Length != right.Length)
                {
                    return false;
                }

                for (var i = 0; i < left.Length; i++)
                {
                    var a = left[i];
                    var b = right[i];
                    if (a == null && b == null)
                    {
                        continue;
                    }

                    if (a == null || b == null || !a.Equals(b))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private readonly struct PivotCellKey : IEquatable<PivotCellKey>
        {
            public PivotCellKey(object?[] rowPathValues, object?[] columnPathValues)
            {
                RowPathValues = rowPathValues;
                ColumnPathValues = columnPathValues;
            }

            public object?[] RowPathValues { get; }

            public object?[] ColumnPathValues { get; }

            public bool Equals(PivotCellKey other)
            {
                return PathMatches(RowPathValues, other.RowPathValues) && PathMatches(ColumnPathValues, other.ColumnPathValues);
            }

            public override bool Equals(object? obj)
            {
                return obj is PivotCellKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = 17;
                    for (var i = 0; i < RowPathValues.Length; i++)
                    {
                        hash = hash * 31 + (RowPathValues[i]?.GetHashCode() ?? 0);
                    }

                    for (var i = 0; i < ColumnPathValues.Length; i++)
                    {
                        hash = hash * 31 + (ColumnPathValues[i]?.GetHashCode() ?? 0);
                    }

                    return hash;
                }
            }

            private static bool PathMatches(object?[] left, object?[] right)
            {
                if (left.Length != right.Length)
                {
                    return false;
                }

                for (var i = 0; i < left.Length; i++)
                {
                    var a = left[i];
                    var b = right[i];
                    if (a == null && b == null)
                    {
                        continue;
                    }

                    if (a == null || b == null || !a.Equals(b))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private sealed class PivotCellState
        {
            private readonly IPivotAggregationState[] _states;

            public PivotCellState(IList<PivotValueField> valueFields, PivotAggregatorRegistry registry)
            {
                _states = new IPivotAggregationState[valueFields.Count];
                for (var i = 0; i < valueFields.Count; i++)
                {
                    var field = valueFields[i];
                    if (field.IsCalculated || field.AggregateType == PivotAggregateType.None)
                    {
                        _states[i] = new NullPivotAggregationState();
                        continue;
                    }
                    var aggregator = field.AggregateType == PivotAggregateType.Custom
                        ? field.CustomAggregator
                        : registry.Get(field.AggregateType);

                    _states[i] = aggregator?.CreateState() ?? new NullPivotAggregationState();
                }
            }

            public void Add(int index, object? value)
            {
                _states[index].Add(value);
            }

            public object? GetResult(int index)
            {
                return _states[index].GetResult();
            }

            private sealed class NullPivotAggregationState : IPivotAggregationState
            {
                public void Add(object? value)
                {
                }

                public void Merge(IPivotAggregationState other)
                {
                }

                public object? GetResult() => null;
            }
        }

        private sealed class PivotValueComparer : IComparer<object?>
        {
            private readonly CompareInfo _compareInfo;

            public PivotValueComparer(CultureInfo culture)
            {
                _compareInfo = (culture ?? CultureInfo.CurrentCulture).CompareInfo;
            }

            public int Compare(object? x, object? y)
            {
                if (ReferenceEquals(x, y))
                {
                    return 0;
                }

                if (x == null)
                {
                    return 1;
                }

                if (y == null)
                {
                    return -1;
                }

                if (PivotNumeric.TryGetDouble(x, out var xNumber) && PivotNumeric.TryGetDouble(y, out var yNumber))
                {
                    return xNumber.CompareTo(yNumber);
                }

                if (x is string xString && y is string yString)
                {
                    return _compareInfo.Compare(xString, yString, CompareOptions.StringSort);
                }

                if (x is IComparable comparable && y is IComparable)
                {
                    try
                    {
                        return comparable.CompareTo(y);
                    }
                    catch
                    {
                        // fall through
                    }
                }

                var xText = x.ToString() ?? string.Empty;
                var yText = y.ToString() ?? string.Empty;
                return _compareInfo.Compare(xText, yText, CompareOptions.StringSort);
            }
        }

        private static IDataGridColumnValueAccessor CreateCompactLabelAccessor()
        {
            return new DataGridColumnValueAccessor<PivotRow, string?>(row => row.CompactLabel);
        }

        private static IDataGridColumnValueAccessor CreateRowDisplayValueAccessor(int index)
        {
            return new DataGridColumnValueAccessor<PivotRow, object?>(row =>
            {
                if (row == null)
                {
                    return null;
                }

                var values = row.RowDisplayValues;
                if (index < 0 || index >= values.Length)
                {
                    return null;
                }

                return values[index];
            });
        }

        private static IDataGridColumnValueAccessor CreateRowPathValueAccessor(int index)
        {
            return new DataGridColumnValueAccessor<PivotRow, object?>(row =>
            {
                if (row == null)
                {
                    return null;
                }

                var values = row.RowPathValues;
                if (index < 0 || index >= values.Length)
                {
                    return null;
                }

                return values[index];
            });
        }

        private static IDataGridColumnValueAccessor CreateCellValueAccessor(int index)
        {
            return new DataGridColumnValueAccessor<PivotRow, object?>(row =>
            {
                if (row == null)
                {
                    return null;
                }

                var values = row.CellValues;
                if (index < 0 || index >= values.Length)
                {
                    return null;
                }

                return values[index];
            });
        }

        private static DataGridColumnDefinitionOptions CreateSearchOptions(Func<object, string> provider)
        {
            return new DataGridColumnDefinitionOptions
            {
                SearchTextProvider = provider
            };
        }

        private static string GetCompactLabelText(object item)
        {
            return item is PivotRow row ? row.CompactLabel ?? string.Empty : string.Empty;
        }

        private static Func<object, string> CreateRowDisplayTextProvider(int index, CultureInfo culture)
        {
            return item => GetRowDisplayText(item, index, culture);
        }

        private static string GetRowDisplayText(object item, int index, CultureInfo culture)
        {
            if (item is not PivotRow row)
            {
                return string.Empty;
            }

            var values = row.RowDisplayValues;
            if (index < 0 || index >= values.Length)
            {
                return string.Empty;
            }

            return Convert.ToString(values[index], culture) ?? string.Empty;
        }

        private static Func<object, string> CreateCellValueTextProvider(
            int index,
            PivotValueField? valueField,
            PivotLayoutOptions layout,
            CultureInfo culture,
            bool valuesInRows,
            bool isNumericColumn)
        {
            return item => GetCellValueText(item, index, valueField, layout, culture, valuesInRows, isNumericColumn);
        }

        private static string GetCellValueText(
            object item,
            int index,
            PivotValueField? valueField,
            PivotLayoutOptions layout,
            CultureInfo culture,
            bool valuesInRows,
            bool isNumericColumn)
        {
            if (item is not PivotRow row)
            {
                return string.Empty;
            }

            var values = row.CellValues;
            if (index < 0 || index >= values.Length)
            {
                return string.Empty;
            }

            return FormatPivotValueText(values[index], valueField, layout, culture, valuesInRows, isNumericColumn);
        }

        private static string FormatPivotValueText(
            object? value,
            PivotValueField? valueField,
            PivotLayoutOptions layout,
            CultureInfo culture,
            bool valuesInRows,
            bool isNumericColumn)
        {
            if (value == null)
            {
                if (!valuesInRows && !isNumericColumn)
                {
                    var label = GetTargetNullValue(valueField, layout.EmptyValueLabel);
                    return label?.ToString() ?? string.Empty;
                }

                if (valuesInRows && !string.IsNullOrEmpty(layout.EmptyValueLabel))
                {
                    return layout.EmptyValueLabel;
                }

                return string.Empty;
            }

            if (valuesInRows || valueField == null)
            {
                var provider = valueField?.FormatProvider ?? culture;
                return Convert.ToString(value, provider) ?? string.Empty;
            }

            if (valueField.Converter != null)
            {
                var converterCulture = valueField.FormatProvider as CultureInfo ?? culture;
                var converted = valueField.Converter.Convert(value, typeof(string), valueField.ConverterParameter, converterCulture);
                var provider = valueField.FormatProvider ?? culture;
                return Convert.ToString(converted, provider) ?? string.Empty;
            }

            var format = valueField.StringFormat;
            if (IsPercentDisplayMode(GetEffectiveDisplayMode(valueField)) && string.IsNullOrEmpty(format))
            {
                format = "P2";
            }

            if (!string.IsNullOrEmpty(format))
            {
                try
                {
                    var provider = valueField.FormatProvider ?? culture;
                    if (format.Contains("{0"))
                    {
                        return string.Format(provider, format, value);
                    }

                    if (value is IFormattable formattable)
                    {
                        return formattable.ToString(format, provider);
                    }
                }
                catch
                {
                    // ignore formatting errors and fall back
                }
            }

            var fallbackProvider = valueField.FormatProvider ?? culture;
            return Convert.ToString(value, fallbackProvider) ?? string.Empty;
        }

        internal sealed class PivotBuildResult
        {
            public static readonly PivotBuildResult Empty = new(
                new List<PivotRow>(),
                new List<PivotColumn>(),
                new List<DataGridColumnDefinition>());

            public PivotBuildResult(
                List<PivotRow> rows,
                List<PivotColumn> columns,
                List<DataGridColumnDefinition> columnDefinitions)
            {
                Rows = rows;
                Columns = columns;
                ColumnDefinitions = columnDefinitions;
            }

            public List<PivotRow> Rows { get; }

            public List<PivotColumn> Columns { get; }

            public List<DataGridColumnDefinition> ColumnDefinitions { get; }
        }

        private static DataGridBindingDefinition CreateArrayBinding(
            IPropertyInfo property,
            Func<PivotRow, object?[]> getter,
            int index,
            IValueConverter? converter,
            object? converterParameter,
            string? stringFormat,
            object? targetNullValue,
            CultureInfo? converterCulture)
        {
            var binding = DataGridBindingDefinition.CreateCached(property, getter);
            binding.Mode = BindingMode.OneWay;
            binding.Converter = new PivotArrayIndexConverter(index, converter, converterParameter);
            binding.ConverterParameter = converterParameter;
            if (!string.IsNullOrEmpty(stringFormat))
            {
                binding.StringFormat = stringFormat;
            }

            if (targetNullValue != null)
            {
                binding.TargetNullValue = targetNullValue;
            }

            if (converterCulture != null)
            {
                binding.ConverterCulture = converterCulture;
            }

            return binding;
        }
    }
}
