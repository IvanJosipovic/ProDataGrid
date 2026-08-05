using System;

namespace Avalonia.Controls
{
    internal sealed class DataGridScrollHeightIndex
    {
        private double[] _heights = Array.Empty<double>();
        private double[] _heightSums = Array.Empty<double>();
        private double[] _visibleSums = Array.Empty<double>();
        private int[] _visibleCounts = Array.Empty<int>();
        private sbyte[] _visibilityLazy = Array.Empty<sbyte>();
        private int _treeSize;

        public int Count { get; private set; }

        public bool IsInitialized => _treeSize > 0 || Count == 0;

        public double TotalHeight => Count == 0 ? 0 : _visibleSums[1];

        public void Rebuild(int count, Func<int, double> getHeight, Func<int, bool> isVisible)
        {
            Count = Math.Max(0, count);
            if (Count == 0)
            {
                _heights = Array.Empty<double>();
                _heightSums = Array.Empty<double>();
                _visibleSums = Array.Empty<double>();
                _visibleCounts = Array.Empty<int>();
                _visibilityLazy = Array.Empty<sbyte>();
                _treeSize = 0;
                return;
            }

            _treeSize = 1;
            while (_treeSize < Count)
            {
                _treeSize <<= 1;
            }

            _heights = new double[Count];
            _heightSums = new double[_treeSize * 2];
            _visibleSums = new double[_treeSize * 2];
            _visibleCounts = new int[_treeSize * 2];
            _visibilityLazy = new sbyte[_treeSize * 2];
            Array.Fill(_visibilityLazy, (sbyte)-1);

            for (int slot = 0; slot < Count; slot++)
            {
                _heights[slot] = SanitizeHeight(getHeight(slot));
            }

            Build(1, 0, _treeSize, isVisible);
        }

        public double GetHeight(int slot)
        {
            return slot >= 0 && slot < Count ? _heights[slot] : 0;
        }

        public double GetOffsetToSlot(int slot)
        {
            slot = Math.Max(0, Math.Min(slot, Count));
            return QueryVisibleSum(1, 0, _treeSize, 0, slot);
        }

        public int FindSlotAtOffset(double offset)
        {
            if (Count == 0 || _visibleCounts[1] == 0)
            {
                return -1;
            }

            if (offset <= 0)
            {
                return FindFirstVisible(1, 0, _treeSize, 0);
            }

            if (offset >= TotalHeight)
            {
                return FindLastVisible(1, 0, _treeSize, Count - 1);
            }

            int slot = FindFirstSlotAfterOffset(1, 0, _treeSize, offset);
            if (slot >= 0 && slot < Count)
            {
                return slot;
            }

            return FindLastVisible(1, 0, _treeSize, Count - 1);
        }

        public int FindFirstVisibleAtOrAfter(int slot)
        {
            if (Count == 0)
            {
                return -1;
            }

            return FindFirstVisible(1, 0, _treeSize, Math.Max(0, slot));
        }

        public int FindLastVisibleAtOrBefore(int slot)
        {
            if (Count == 0)
            {
                return -1;
            }

            return FindLastVisible(1, 0, _treeSize, Math.Min(Count - 1, slot));
        }

        public void SetHeight(int slot, double height)
        {
            if (slot < 0 || slot >= Count)
            {
                return;
            }

            _heights[slot] = SanitizeHeight(height);
            SetHeight(1, 0, _treeSize, slot);
        }

        public void SetVisible(int slot, bool visible)
        {
            if (slot < 0 || slot >= Count)
            {
                return;
            }

            SetVisible(1, 0, _treeSize, slot, visible);
        }

        public void SetVisibleRange(int startSlot, int endSlot, bool visible)
        {
            if (Count == 0)
            {
                return;
            }

            startSlot = Math.Max(0, startSlot);
            endSlot = Math.Min(Count - 1, endSlot);
            if (startSlot > endSlot)
            {
                return;
            }

            SetVisibleRange(1, 0, _treeSize, startSlot, endSlot + 1, visible);
        }

        private void Build(int node, int start, int end, Func<int, bool> isVisible)
        {
            if (end - start == 1)
            {
                if (start >= Count)
                {
                    return;
                }

                double height = _heights[start];
                _heightSums[node] = height;
                if (isVisible(start))
                {
                    _visibleSums[node] = height;
                    _visibleCounts[node] = 1;
                }

                return;
            }

            int middle = start + ((end - start) >> 1);
            Build(node * 2, start, middle, isVisible);
            Build(node * 2 + 1, middle, end, isVisible);
            Pull(node);
        }

        private void SetHeight(int node, int start, int end, int slot)
        {
            if (end - start == 1)
            {
                _heightSums[node] = _heights[slot];
                _visibleSums[node] = _visibleCounts[node] == 0 ? 0 : _heights[slot];
                return;
            }

            PushVisibility(node, start, end);
            int middle = start + ((end - start) >> 1);
            if (slot < middle)
            {
                SetHeight(node * 2, start, middle, slot);
            }
            else
            {
                SetHeight(node * 2 + 1, middle, end, slot);
            }

            Pull(node);
        }

        private void SetVisible(int node, int start, int end, int slot, bool visible)
        {
            if (end - start == 1)
            {
                _visibleSums[node] = visible ? _heightSums[node] : 0;
                _visibleCounts[node] = visible ? 1 : 0;
                _visibilityLazy[node] = (sbyte)(visible ? 1 : 0);
                return;
            }

            PushVisibility(node, start, end);
            int middle = start + ((end - start) >> 1);
            if (slot < middle)
            {
                SetVisible(node * 2, start, middle, slot, visible);
            }
            else
            {
                SetVisible(node * 2 + 1, middle, end, slot, visible);
            }

            Pull(node);
        }

        private void SetVisibleRange(int node, int start, int end, int rangeStart, int rangeEnd, bool visible)
        {
            if (rangeStart >= end || rangeEnd <= start)
            {
                return;
            }

            if (rangeStart <= start && end <= rangeEnd)
            {
                ApplyVisibility(node, start, end, visible);
                return;
            }

            PushVisibility(node, start, end);
            int middle = start + ((end - start) >> 1);
            SetVisibleRange(node * 2, start, middle, rangeStart, rangeEnd, visible);
            SetVisibleRange(node * 2 + 1, middle, end, rangeStart, rangeEnd, visible);
            Pull(node);
        }

        private double QueryVisibleSum(int node, int start, int end, int queryStart, int queryEnd)
        {
            if (queryStart >= end || queryEnd <= start)
            {
                return 0;
            }

            if (queryStart <= start && end <= queryEnd)
            {
                return _visibleSums[node];
            }

            PushVisibility(node, start, end);
            int middle = start + ((end - start) >> 1);
            return QueryVisibleSum(node * 2, start, middle, queryStart, queryEnd) +
                QueryVisibleSum(node * 2 + 1, middle, end, queryStart, queryEnd);
        }

        private int FindFirstSlotAfterOffset(int node, int start, int end, double offset)
        {
            if (_visibleSums[node] <= offset)
            {
                return -1;
            }

            if (end - start == 1)
            {
                return start;
            }

            PushVisibility(node, start, end);
            int middle = start + ((end - start) >> 1);
            int left = node * 2;
            if (_visibleSums[left] > offset)
            {
                return FindFirstSlotAfterOffset(left, start, middle, offset);
            }

            return FindFirstSlotAfterOffset(left + 1, middle, end, offset - _visibleSums[left]);
        }

        private int FindFirstVisible(int node, int start, int end, int minimumSlot)
        {
            if (end <= minimumSlot || _visibleCounts[node] == 0)
            {
                return -1;
            }

            if (end - start == 1)
            {
                return start < Count ? start : -1;
            }

            PushVisibility(node, start, end);
            int middle = start + ((end - start) >> 1);
            int left = FindFirstVisible(node * 2, start, middle, minimumSlot);
            return left >= 0 ? left : FindFirstVisible(node * 2 + 1, middle, end, minimumSlot);
        }

        private int FindLastVisible(int node, int start, int end, int maximumSlot)
        {
            if (start > maximumSlot || _visibleCounts[node] == 0)
            {
                return -1;
            }

            if (end - start == 1)
            {
                return start < Count ? start : -1;
            }

            PushVisibility(node, start, end);
            int middle = start + ((end - start) >> 1);
            int right = FindLastVisible(node * 2 + 1, middle, end, maximumSlot);
            return right >= 0 ? right : FindLastVisible(node * 2, start, middle, maximumSlot);
        }

        private void ApplyVisibility(int node, int start, int end, bool visible)
        {
            _visibleSums[node] = visible ? _heightSums[node] : 0;
            _visibleCounts[node] = visible ? Math.Max(0, Math.Min(end, Count) - start) : 0;
            _visibilityLazy[node] = (sbyte)(visible ? 1 : 0);
        }

        private void PushVisibility(int node, int start, int end)
        {
            sbyte lazy = _visibilityLazy[node];
            if (lazy == -1 || end - start == 1)
            {
                return;
            }

            int middle = start + ((end - start) >> 1);
            bool visible = lazy == 1;
            ApplyVisibility(node * 2, start, middle, visible);
            ApplyVisibility(node * 2 + 1, middle, end, visible);
            _visibilityLazy[node] = -1;
        }

        private void Pull(int node)
        {
            int left = node * 2;
            int right = left + 1;
            _heightSums[node] = _heightSums[left] + _heightSums[right];
            _visibleSums[node] = _visibleSums[left] + _visibleSums[right];
            _visibleCounts[node] = _visibleCounts[left] + _visibleCounts[right];
            _visibilityLazy[node] = -1;
        }

        private static double SanitizeHeight(double height)
        {
            return double.IsNaN(height) || double.IsInfinity(height) || height < 0 ? 0 : height;
        }
    }
}