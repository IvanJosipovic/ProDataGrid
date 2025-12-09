// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.DataGridSorting;
using Avalonia.Input;
using Avalonia.Threading;

namespace Avalonia.Controls.DataGridSorting
{
    /// <summary>
    /// Bridges sorting gestures/model to the view's SortDescriptions.
    /// </summary>
    public class DataGridSortingAdapter : IDisposable
    {
        private readonly ISortingModel _model;
        private readonly Func<IEnumerable<DataGridColumn>> _columnProvider;
        private IDataGridCollectionView _view;
        private bool _suppressViewSync;
        private bool _suppressModelSync;
        private Action _beforeViewRefresh;
        private Action _afterViewRefresh;

        public DataGridSortingAdapter(
            ISortingModel model,
            Func<IEnumerable<DataGridColumn>> columnProvider,
            Action beforeViewRefresh = null,
            Action afterViewRefresh = null)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _columnProvider = columnProvider ?? throw new ArgumentNullException(nameof(columnProvider));
            _beforeViewRefresh = beforeViewRefresh;
            _afterViewRefresh = afterViewRefresh;

            _model.SortingChanged += OnModelSortingChanged;
        }

        internal void AttachLifecycle(Action beforeViewRefresh, Action afterViewRefresh)
        {
            _beforeViewRefresh = beforeViewRefresh;
            _afterViewRefresh = afterViewRefresh;
        }

        public IDataGridCollectionView View => _view;

        public void AttachView(IDataGridCollectionView view)
        {
            if (ReferenceEquals(_view, view))
            {
                return;
            }

            DetachView();
            _view = view;

            if (_view != null)
            {
                _view.SortDescriptions.CollectionChanged += OnViewSortDescriptionsChanged;

                if (_model.OwnsViewSorts)
                {
                    ApplyModelToView(_model.Descriptors);
                }
                else
                {
                    SyncModelFromView();
                }
            }
        }

        public void HandleHeaderClick(DataGridColumn column, KeyModifiers modifiers, ListSortDirection? forcedDirection = null)
        {
            if (column == null)
            {
                throw new ArgumentNullException(nameof(column));
            }

            var descriptor = CreateDescriptor(column, forcedDirection ?? ListSortDirection.Ascending);
            if (descriptor == null)
            {
                return;
            }

            var sortingModifiers = SortingModifiers.None;
            if (modifiers.HasFlag(KeyModifiers.Shift))
            {
                sortingModifiers |= SortingModifiers.Multi;
            }
            if (modifiers.HasFlag(KeyModifiers.Control) || modifiers.HasFlag(KeyModifiers.Meta))
            {
                sortingModifiers |= SortingModifiers.Clear;
            }

            _model.Toggle(descriptor, sortingModifiers);
        }

        public void Dispose()
        {
            DetachView();
            _model.SortingChanged -= OnModelSortingChanged;
        }

        private void DetachView()
        {
            if (_view != null)
            {
                _view.SortDescriptions.CollectionChanged -= OnViewSortDescriptionsChanged;
                _view = null;
            }
        }

        public void RefreshOwnership()
        {
            if (_view == null)
            {
                return;
            }

            if (_model.OwnsViewSorts)
            {
                ApplyModelToView(_model.Descriptors);
            }
            else
            {
                SyncModelFromView();
            }
        }

        private void OnModelSortingChanged(object sender, SortingChangedEventArgs e)
        {
            if (_suppressModelSync)
            {
                return;
            }

            ApplyModelToView(e.NewDescriptors, e.OldDescriptors);
        }

        private void OnViewSortDescriptionsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_suppressViewSync || _view == null)
            {
                return;
            }

            SyncModelFromView();
            if (_afterViewRefresh != null)
            {
                Dispatcher.UIThread.Post(() => _afterViewRefresh());
            }
        }

        private bool ApplyModelToView(IReadOnlyList<SortingDescriptor> descriptors, IReadOnlyList<SortingDescriptor> previousDescriptors = null)
        {
            if (_view == null)
            {
                return false;
            }

            var beforeInvoked = false;
            void EnsureBeforeViewRefresh()
            {
                if (!beforeInvoked)
                {
                    _beforeViewRefresh?.Invoke();
                    beforeInvoked = true;
                }
            }

            EnsureBeforeViewRefresh();

            if (TryApplyModelToView(descriptors, previousDescriptors, out var handledChanged))
            {
                if (handledChanged)
                {
                    _afterViewRefresh?.Invoke();
                }

                return handledChanged;
            }

            _suppressViewSync = true;
            bool changed = false;
            try
            {
                var targetSorts = (IReadOnlyList<DataGridSortDescription>)(BuildSortDescriptions(descriptors) ?? new List<DataGridSortDescription>());
                if (SortsEqual(_view.SortDescriptions, targetSorts))
                {
                    return false;
                }

                EnsureBeforeViewRefresh();

                var rollback = BuildSortDescriptions(previousDescriptors) ?? _view.SortDescriptions.ToList();

                try
                {
                    using (_view.DeferRefresh())
                    {
                        _view.SortDescriptions.Clear();

                        foreach (var sortDescription in targetSorts)
                        {
                            _view.SortDescriptions.Add(sortDescription);
                        }

                        changed = true;
                    }
                }
                catch (Exception ex)
                {
                    LogInvalidSort($"Applying sort descriptors failed; reverting. {ex}");
                    RestoreSortDescriptions(rollback);

                    if (previousDescriptors != null)
                    {
                        _suppressModelSync = true;
                        try
                        {
                            _model.Apply(previousDescriptors);
                        }
                        finally
                        {
                            _suppressModelSync = false;
                        }
                    }
                }
            }
            finally
            {
                _suppressViewSync = false;
                if (changed)
                {
                    _afterViewRefresh?.Invoke();
                }
            }

            return changed;
        }

        /// <summary>
        /// Override to short-circuit default sort application (e.g., push descriptors upstream).
        /// Return true to indicate the call was handled; set <paramref name="changed"/> to true
        /// if the view was refreshed.
        /// </summary>
        /// <remarks>
        /// This is intended for advanced scenarios (DynamicData/server-side sorting) where
        /// the adapter should not rewrite <see cref="IDataGridCollectionView.SortDescriptions"/>.
        /// </remarks>
        protected virtual bool TryApplyModelToView(
            IReadOnlyList<SortingDescriptor> descriptors,
            IReadOnlyList<SortingDescriptor> previousDescriptors,
            out bool changed)
        {
            changed = false;
            return false;
        }

        private void SyncModelFromView()
        {
            if (_view == null)
            {
                return;
            }

            var descriptors = new List<SortingDescriptor>();
            var seen = new HashSet<object>();
            foreach (var sort in _view.SortDescriptions)
            {
                var descriptor = ToSortingDescriptor(sort);
                if (descriptor != null)
                {
                    if (seen.Add(descriptor.ColumnId))
                    {
                        descriptors.Add(descriptor);
                    }
                    else
                    {
                        LogInvalidSort("Ignoring duplicate sort descriptor for the same column.");
                    }
                }
            }

            _suppressModelSync = true;
            try
            {
                _model.Apply(descriptors);
            }
            finally
            {
                _suppressModelSync = false;
            }
        }

        private SortingDescriptor CreateDescriptor(DataGridColumn column, ListSortDirection direction)
        {
            if (column.CustomSortComparer != null)
            {
                return new SortingDescriptor(column, direction, comparer: column.CustomSortComparer, culture: _view?.Culture ?? CultureInfo.InvariantCulture);
            }

            var propertyPath = column.GetSortPropertyName();
            if (string.IsNullOrEmpty(propertyPath))
            {
                LogInvalidSort($"Cannot sort column '{column.Header}' because no sort path was found.");
                return null;
            }

            return new SortingDescriptor(column, direction, propertyPath, culture: _view?.Culture ?? CultureInfo.InvariantCulture);
        }

        private DataGridSortDescription ToSortDescription(SortingDescriptor descriptor)
        {
            if (descriptor == null)
            {
                return null;
            }

            if (descriptor.HasComparer)
            {
                return DataGridSortDescription.FromComparer(descriptor.Comparer, descriptor.Direction);
            }

            if (descriptor.HasPropertyPath)
            {
                return DataGridSortDescription.FromPath(descriptor.PropertyPath, descriptor.Direction, descriptor.Culture);
            }

            return null;
        }

        private SortingDescriptor ToSortingDescriptor(DataGridSortDescription sort)
        {
            if (sort == null)
            {
                return null;
            }

            var column = FindColumnForSort(sort);
            if (sort is DataGridComparerSortDescription comparerSort)
            {
                var id = (object)column ?? (object)comparerSort.SourceComparer ?? (object)sort;
                return new SortingDescriptor(id, comparerSort.Direction, comparer: comparerSort.SourceComparer, culture: _view?.Culture);
            }

            var propertyPath = sort.PropertyPath;
            var columnId = (object)column ?? (!string.IsNullOrEmpty(propertyPath) ? (object)propertyPath : sort);
            return new SortingDescriptor(columnId, sort.Direction, propertyPath, culture: _view?.Culture);
        }

        private DataGridColumn FindColumnForSort(DataGridSortDescription sort)
        {
            foreach (var column in EnumerateColumns())
            {
                if (column == null)
                {
                    continue;
                }

                if (sort is DataGridComparerSortDescription comparerSort && column.CustomSortComparer != null && Equals(column.CustomSortComparer, comparerSort.SourceComparer))
                {
                    return column;
                }

                var propertyPath = column.GetSortPropertyName();
                if (!string.IsNullOrEmpty(propertyPath) && string.Equals(propertyPath, sort.PropertyPath, StringComparison.Ordinal))
                {
                    return column;
                }
            }

            return null;
        }

        private IEnumerable<DataGridColumn> EnumerateColumns()
        {
            return _columnProvider()?.Where(c => c != null) ?? Array.Empty<DataGridColumn>();
        }

        private List<DataGridSortDescription> BuildSortDescriptions(IReadOnlyList<SortingDescriptor> descriptors)
        {
            if (descriptors == null)
            {
                return null;
            }

            var list = new List<DataGridSortDescription>();
            foreach (var descriptor in descriptors)
            {
                var sort = ToSortDescription(descriptor);
                if (sort != null)
                {
                    list.Add(sort);
                }
                else
                {
                    LogInvalidSort("Skipping invalid sort descriptor (missing path/comparer).");
                }
            }
            return list;
        }

        private static bool SortsEqual(IReadOnlyList<DataGridSortDescription> existing, IReadOnlyList<DataGridSortDescription> target)
        {
            if (existing == null || target == null || existing.Count != target.Count)
            {
                return false;
            }

            for (int i = 0; i < existing.Count; i++)
            {
                var left = existing[i];
                var right = target[i];

                if (left.Direction != right.Direction)
                {
                    return false;
                }

                if (left is DataGridComparerSortDescription leftComparer &&
                    right is DataGridComparerSortDescription rightComparer)
                {
                    if (!Equals(leftComparer.SourceComparer, rightComparer.SourceComparer))
                    {
                        return false;
                    }
                }
                else if (left is DataGridComparerSortDescription || right is DataGridComparerSortDescription)
                {
                    return false;
                }
                else if (!string.Equals(left.PropertyPath, right.PropertyPath, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private void RestoreSortDescriptions(IReadOnlyList<DataGridSortDescription> descriptions)
        {
            if (_view == null || descriptions == null)
            {
                return;
            }

            using (_view.DeferRefresh())
            {
                _view.SortDescriptions.Clear();
                foreach (var sort in descriptions)
                {
                    _view.SortDescriptions.Add(sort);
                }
            }
        }

        private void LogInvalidSort(string message)
        {
            Debug.WriteLine($"[DataGridSortingAdapter] {message}");
        }
    }
}
