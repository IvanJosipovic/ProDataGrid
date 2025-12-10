// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Avalonia.Controls.DataGridHierarchical
{
    /// <summary>
    /// Factory to create a hierarchical adapter without subclassing <see cref="DataGrid"/>.
    /// </summary>
    public interface IDataGridHierarchicalAdapterFactory
    {
        DataGridHierarchicalAdapter Create(DataGrid grid, IHierarchicalModel model);
    }

    /// <summary>
    /// Bridges a hierarchical model to the DataGrid by exposing flattened accessors and gestures.
    /// </summary>
    public class DataGridHierarchicalAdapter : IDisposable
    {
        private readonly IHierarchicalModel _model;
        private readonly Action<FlattenedChangedEventArgs>? _flattenedChangedCallback;
        private IDisposable? _virtualizationGuard;
        private DispatcherTimer? _guardTimer;
        private static readonly TimeSpan GuardDebounce = TimeSpan.FromMilliseconds(50);

        public DataGridHierarchicalAdapter(
            IHierarchicalModel model,
            Action<FlattenedChangedEventArgs>? flattenedChangedCallback = null)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _flattenedChangedCallback = flattenedChangedCallback;

            _model.FlattenedChanged += OnModelFlattenedChanged;
        }

        public IHierarchicalModel Model => _model;

        public int Count => _model.Count;

        public object? ItemAt(int index) => _model.GetItem(index);

        public HierarchicalNode NodeAt(int index) => _model.GetNode(index);

        public int LevelAt(int index) => _model.GetNode(index).Level;

        public bool IsExpandable(int index)
        {
            var node = _model.GetNode(index);
            return !node.IsLeaf;
        }

        public bool IsExpanded(int index) => _model.GetNode(index).IsExpanded;

        public void Toggle(int index)
        {
            BeginDebouncedVirtualizationGuard();
            _model.Toggle(_model.GetNode(index));
        }

        public void Expand(int index)
        {
            BeginDebouncedVirtualizationGuard();
            _model.Expand(_model.GetNode(index));
        }

        public void Collapse(int index)
        {
            BeginDebouncedVirtualizationGuard();
            _model.Collapse(_model.GetNode(index));
        }

        public int IndexOfNode(HierarchicalNode node) => _model.IndexOf(node);

        public int IndexOfItem(object item) => _model.IndexOf(item);

        public int IndexOf(object item)
        {
            return IndexOfItem(item);
        }

        public event EventHandler<FlattenedChangedEventArgs>? FlattenedChanged;

        public void Dispose()
        {
            _model.FlattenedChanged -= OnModelFlattenedChanged;
        }

        public void SetRoot(object root)
        {
            _model.SetRoot(root);
        }

        public void Sort(HierarchicalNode? node = null, IComparer<object>? comparer = null, bool recursive = true)
        {
            _model.Sort(node, comparer, recursive);
        }

        public void ApplySiblingComparer(IComparer<object>? comparer, bool recursive = true)
        {
            _model.ApplySiblingComparer(comparer, recursive);
        }

        public void ExpandAll(HierarchicalNode? node = null, int? maxDepth = null)
        {
            BeginDebouncedVirtualizationGuard();
            _model.ExpandAll(node, maxDepth);
        }

        public void CollapseAll(HierarchicalNode? node = null, int? minDepth = null)
        {
            BeginDebouncedVirtualizationGuard();
            _model.CollapseAll(node, minDepth);
        }

        private void OnModelFlattenedChanged(object? sender, FlattenedChangedEventArgs e)
        {
            FlattenedChanged?.Invoke(this, e);
            _flattenedChangedCallback?.Invoke(e);
        }

        private void BeginDebouncedVirtualizationGuard()
        {
            _virtualizationGuard ??= _model.BeginVirtualizationGuard();

            if (_guardTimer == null)
            {
                _guardTimer = new DispatcherTimer { Interval = GuardDebounce };
                _guardTimer.Tick += (_, __) => EndDebouncedVirtualizationGuard();
            }

            _guardTimer.Stop();
            _guardTimer.Start();
        }

        private void EndDebouncedVirtualizationGuard()
        {
            _guardTimer?.Stop();
            _virtualizationGuard?.Dispose();
            _virtualizationGuard = null;
        }
    }
}
