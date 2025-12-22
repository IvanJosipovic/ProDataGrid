// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia.Utilities;

namespace Avalonia.Controls
{
    internal interface IDataGridScrollStateManager
    {
        bool PendingRestore { get; }
        bool PreserveOnAttach { get; }

        void Capture(bool preserveOnAttach);
        bool ShouldPreserveScrollState();
        void Clear();
        void ClearPreserveOnAttachFlag();
        bool TryRestore();
    }

    public partial class DataGrid
    {
        private sealed class ScrollStateManager : IDataGridScrollStateManager
        {
            private readonly DataGrid _owner;
            private bool _preserveOnAttach;
            private bool _pendingRestore;
            private IEnumerable _dataSource;
            private int? _dataSourceCount;
            private List<PreservedScrollStateSample> _samples;
            private int _firstScrollingSlot;
            private double _negVerticalOffset;
            private double _verticalOffset;
            private RowHeightEstimatorState _rowHeightEstimatorState;

            public ScrollStateManager(DataGrid owner)
            {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            }

            public bool PendingRestore => _pendingRestore;

            public bool PreserveOnAttach => _preserveOnAttach;

            public void Capture(bool preserveOnAttach)
            {
                if (_owner.DisplayData.FirstScrollingSlot < 0 || _owner.DataConnection?.DataSource == null)
                {
                    if (_pendingRestore)
                    {
                        _preserveOnAttach = preserveOnAttach;
                        return;
                    }

                    Clear();
                    return;
                }

                _preserveOnAttach = preserveOnAttach;
                _dataSource = _owner.DataConnection.DataSource;
                _dataSourceCount = TryGetDataSourceCount(_owner.DataConnection.DataSource);
                _samples = BuildSamples(_dataSourceCount);
                _firstScrollingSlot = _owner.DisplayData.FirstScrollingSlot;
                _negVerticalOffset = _owner.NegVerticalOffset;
                _verticalOffset = _owner._verticalOffset;
                _rowHeightEstimatorState = CaptureEstimatorState();
                _pendingRestore = true;
            }

            public bool ShouldPreserveScrollState()
            {
                return _preserveOnAttach && IsValid();
            }

            public void Clear()
            {
                _preserveOnAttach = false;
                _pendingRestore = false;
                _dataSource = null;
                _dataSourceCount = null;
                _samples = null;
                _firstScrollingSlot = -1;
                _negVerticalOffset = 0;
                _verticalOffset = 0;
                _rowHeightEstimatorState = null;
            }

            public void ClearPreserveOnAttachFlag()
            {
                _preserveOnAttach = false;
            }

            public bool TryRestore()
            {
                if (!_pendingRestore)
                {
                    return false;
                }

                if (!_owner.IsAttachedToVisualTree || !_owner.IsVisible)
                {
                    return false;
                }

                if (!IsValid())
                {
                    Clear();
                    return false;
                }

                if (!TryRestoreEstimatorState())
                {
                    Clear();
                    return false;
                }

                if (_owner.SlotCount == 0 || _owner.ColumnsItemsInternal.Count == 0 || MathUtilities.LessThanOrClose(_owner.CellsEstimatedHeight, 0))
                {
                    return false;
                }

                int targetSlot = Math.Min(_firstScrollingSlot, _owner.SlotCount - 1);
                if (targetSlot < 0)
                {
                    Clear();
                    return false;
                }

                if (_owner._collapsedSlotsTable.Contains(targetSlot))
                {
                    targetSlot = _owner.GetNextVisibleSlot(targetSlot);
                    if (targetSlot == -1)
                    {
                        Clear();
                        return false;
                    }
                }

                if (_owner.DisplayData.FirstScrollingSlot != -1)
                {
                    int previousSlot = _owner.GetPreviousVisibleSlot(_owner.DisplayData.FirstScrollingSlot);
                    int nextSlot = _owner.GetNextVisibleSlot(_owner.DisplayData.LastScrollingSlot);
                    if (targetSlot < previousSlot || targetSlot > nextSlot)
                    {
                        _owner.ResetDisplayedRows();
                    }
                }

                _owner.NegVerticalOffset = Math.Max(0, _negVerticalOffset);
                _owner.UpdateDisplayedRows(targetSlot, _owner.CellsEstimatedHeight);

                double firstHeight = _owner.GetExactSlotElementHeight(_owner.DisplayData.FirstScrollingSlot);
                if (MathUtilities.GreaterThanOrClose(_owner.NegVerticalOffset, firstHeight))
                {
                    _owner.NegVerticalOffset = Math.Max(0, firstHeight - MathUtilities.DoubleEpsilon);
                }

                _owner._verticalOffset = Math.Max(0, _verticalOffset);
                _owner.SetVerticalOffset(_owner._verticalOffset);

                Clear();
                return true;
            }

            private RowHeightEstimatorState CaptureEstimatorState()
            {
                if (_owner.RowHeightEstimator is IDataGridRowHeightEstimatorStateful stateful)
                {
                    return stateful.CaptureState();
                }

                return null;
            }

            private bool TryRestoreEstimatorState()
            {
                if (_rowHeightEstimatorState == null)
                {
                    return true;
                }

                if (_owner.RowHeightEstimator is not IDataGridRowHeightEstimatorStateful stateful)
                {
                    return false;
                }

                return stateful.TryRestoreState(_rowHeightEstimatorState);
            }

            private bool IsValid()
            {
                if (_dataSource == null || !ReferenceEquals(_owner.DataConnection.DataSource, _dataSource))
                {
                    return false;
                }

                if (_rowHeightEstimatorState != null && _owner.RowHeightEstimator is not IDataGridRowHeightEstimatorStateful)
                {
                    return false;
                }

                var currentCount = TryGetDataSourceCount(_owner.DataConnection.DataSource);
                if (_dataSourceCount.HasValue)
                {
                    if (!currentCount.HasValue || currentCount.Value != _dataSourceCount.Value)
                    {
                        return false;
                    }
                }

                if (_samples != null)
                {
                    foreach (var sample in _samples)
                    {
                        if (currentCount.HasValue && sample.Index >= currentCount.Value)
                        {
                            return false;
                        }

                        object currentItem;
                        try
                        {
                            currentItem = _owner.DataConnection.GetDataItem(sample.Index);
                        }
                        catch
                        {
                            return false;
                        }

                        if (!ItemsMatch(sample.Item, currentItem))
                        {
                            return false;
                        }
                    }
                }

                return true;
            }

            private List<PreservedScrollStateSample> BuildSamples(int? count)
            {
                List<PreservedScrollStateSample> samples = null;

                AddSampleFromSlot(ref samples, _owner.DisplayData.FirstScrollingSlot);
                AddSampleFromSlot(ref samples, _owner.DisplayData.LastScrollingSlot);

                if (count.HasValue && count.Value > 0)
                {
                    AddSample(ref samples, 0);

                    if (count.Value > 2)
                    {
                        AddSample(ref samples, count.Value / 2);
                    }

                    if (count.Value > 1)
                    {
                        AddSample(ref samples, count.Value - 1);
                    }
                }

                return samples;
            }

            private void AddSampleFromSlot(ref List<PreservedScrollStateSample> samples, int slot)
            {
                if (slot < 0 || _owner.IsGroupSlot(slot))
                {
                    return;
                }

                AddSample(ref samples, _owner.RowIndexFromSlot(slot));
            }

            private void AddSample(ref List<PreservedScrollStateSample> samples, int index)
            {
                if (index < 0)
                {
                    return;
                }

                if (samples != null)
                {
                    foreach (var sample in samples)
                    {
                        if (sample.Index == index)
                        {
                            return;
                        }
                    }
                }

                var item = _owner.DataConnection.GetDataItem(index);
                samples ??= new List<PreservedScrollStateSample>(4);
                samples.Add(new PreservedScrollStateSample(index, item));
            }

            private static int? TryGetDataSourceCount(IEnumerable dataSource)
            {
                if (dataSource == null)
                {
                    return null;
                }

                if (dataSource is ICollection collection)
                {
                    return collection.Count;
                }

                var interfaces = dataSource.GetType().GetInterfaces();
                foreach (var candidate in interfaces)
                {
                    if (!candidate.IsGenericType)
                    {
                        continue;
                    }

                    var genericDefinition = candidate.GetGenericTypeDefinition();
                    if (genericDefinition != typeof(IReadOnlyCollection<>) &&
                        genericDefinition != typeof(ICollection<>))
                    {
                        continue;
                    }

                    var countProperty = candidate.GetProperty("Count");
                    if (countProperty?.PropertyType == typeof(int))
                    {
                        return (int?)countProperty.GetValue(dataSource);
                    }
                }

                return null;
            }

            private static bool ItemsMatch(object left, object right)
            {
                if (ReferenceEquals(left, right))
                {
                    return true;
                }

                if (left == null || right == null)
                {
                    return false;
                }

                if (left is string || right is string)
                {
                    return string.Equals(left as string, right as string, StringComparison.Ordinal);
                }

                if (left.GetType().IsValueType || right.GetType().IsValueType)
                {
                    return left.Equals(right);
                }

                return false;
            }

            private sealed class PreservedScrollStateSample
            {
                public PreservedScrollStateSample(int index, object item)
                {
                    Index = index;
                    Item = item;
                }

                public int Index { get; }
                public object Item { get; }
            }
        }
    }
}
