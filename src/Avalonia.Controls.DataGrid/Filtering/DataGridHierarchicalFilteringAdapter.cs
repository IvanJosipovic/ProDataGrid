// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Avalonia.Collections;
using Avalonia.Controls.DataGridHierarchical;

namespace Avalonia.Controls.DataGridFiltering
{
    /// <summary>
    /// Controls which materialized hierarchy relatives remain visible when a node matches a filter.
    /// </summary>
    [Flags]
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    enum DataGridHierarchyFilterPolicy
    {
        /// <summary>
        /// Shows only nodes whose underlying item matches. This is equivalent to filtering the
        /// flattened source directly and may display a descendant without its ancestor.
        /// </summary>
        SelfOnly = 0,

        /// <summary>
        /// Keeps the materialized ancestor path of every matching node.
        /// </summary>
        KeepAncestorsOfMatches = 1,

        /// <summary>
        /// Keeps materialized descendants of every matching node. Combine with
        /// <see cref="KeepAncestorsOfMatches"/> to preserve complete paths and subtrees.
        /// </summary>
        KeepDescendantsOfMatches = 2,
    }

    /// <summary>
    /// Applies <see cref="FilteringDescriptor"/> instances to the underlying items of a
    /// <see cref="IHierarchicalModel"/> while preserving an explicitly selected set of
    /// materialized relatives.
    /// </summary>
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    sealed class DataGridHierarchicalFilteringAdapter : DataGridAccessorFilteringAdapter
    {
        private readonly IHierarchicalModel _hierarchicalModel;
        private readonly DataGridHierarchyFilterPolicy _policy;
        private readonly Func<object, bool> _includedPredicate;
        private HashSet<HierarchicalNode> _includedNodes;
        private Func<object, bool> _selfPredicate;
        private bool _disposed;
        private int _refreshQueued;

        /// <summary>
        /// Initializes a hierarchy-aware filtering adapter.
        /// </summary>
        /// <param name="model">Filtering descriptors to apply.</param>
        /// <param name="columnProvider">Provides the current grid columns.</param>
        /// <param name="hierarchicalModel">Hierarchy whose materialized nodes are filtered.</param>
        /// <param name="policy">Relatives that remain visible around matching nodes.</param>
        /// <param name="options">Accessor fast-path behavior.</param>
        /// <param name="beforeViewRefresh">Optional callback before a descriptor-driven refresh.</param>
        /// <param name="afterViewRefresh">Optional callback after a descriptor-driven refresh.</param>
        public DataGridHierarchicalFilteringAdapter(
            IFilteringModel model,
            Func<IEnumerable<DataGridColumn>> columnProvider,
            IHierarchicalModel hierarchicalModel,
            DataGridHierarchyFilterPolicy policy = DataGridHierarchyFilterPolicy.KeepAncestorsOfMatches,
            DataGridFastPathOptions options = null,
            Action beforeViewRefresh = null,
            Action afterViewRefresh = null)
            : base(model, columnProvider, options, beforeViewRefresh, afterViewRefresh)
        {
            const DataGridHierarchyFilterPolicy supported =
                DataGridHierarchyFilterPolicy.KeepAncestorsOfMatches |
                DataGridHierarchyFilterPolicy.KeepDescendantsOfMatches;
            if ((policy & ~supported) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(policy));
            }

            _hierarchicalModel = hierarchicalModel ?? throw new ArgumentNullException(nameof(hierarchicalModel));
            _policy = policy;
            _includedPredicate = IsIncluded;
            _hierarchicalModel.FlattenedChanged += HierarchicalModel_FlattenedChanged;
            _hierarchicalModel.HierarchyChanged += HierarchicalModel_HierarchyChanged;
            _hierarchicalModel.NodeLoaded += HierarchicalModel_NodeLoaded;
        }

        /// <summary>
        /// Releases hierarchy and filtering-model subscriptions.
        /// </summary>
        public override void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Interlocked.Exchange(ref _refreshQueued, 0);
            _hierarchicalModel.FlattenedChanged -= HierarchicalModel_FlattenedChanged;
            _hierarchicalModel.HierarchyChanged -= HierarchicalModel_HierarchyChanged;
            _hierarchicalModel.NodeLoaded -= HierarchicalModel_NodeLoaded;
            base.Dispose();
        }

        protected override bool TryApplyModelToView(
            IReadOnlyList<FilteringDescriptor> descriptors,
            IReadOnlyList<FilteringDescriptor> previousDescriptors,
            out bool changed)
        {
            IDataGridCollectionView view = View;
            if (view == null)
            {
                changed = false;
                return true;
            }

            _selfPredicate = ComposePredicate(descriptors);
            Func<object, bool> viewPredicate = _selfPredicate;
            if (_selfPredicate != null &&
                _policy != DataGridHierarchyFilterPolicy.SelfOnly &&
                _hierarchicalModel.Root != null)
            {
                RebuildIncludedNodes();
                viewPredicate = _includedPredicate;
            }
            else
            {
                _includedNodes = null;
            }

            if (ReferenceEquals(view.Filter, viewPredicate))
            {
                if (viewPredicate == null ||
                    _policy == DataGridHierarchyFilterPolicy.SelfOnly)
                {
                    changed = false;
                    return true;
                }

                // The installed delegate is intentionally stable; its backing included-node
                // set was rebuilt above. Refresh even though the delegate identity did not
                // change so a second non-empty descriptor cannot leave the view's cached rows
                // from the previous filter.
                view.Refresh();

                changed = true;
                return true;
            }

            using (view.DeferRefresh())
            {
                view.Filter = viewPredicate;
            }

            changed = true;
            return true;
        }

        private protected override Func<object, bool> AdaptPredicate(Func<object, bool> predicate)
        {
            if (predicate == null)
            {
                return null;
            }

            return item => predicate(GetUnderlyingItem(item));
        }

        private protected override object GetAccessorItem(
            IDataGridColumnValueAccessor accessor,
            object item)
        {
            if (item is not HierarchicalNode node)
            {
                return item;
            }

            object underlyingItem = node.Item;
            if (underlyingItem != null && accessor.ItemType.IsInstanceOfType(underlyingItem))
            {
                return underlyingItem;
            }

            // Existing hierarchy column definitions commonly use an accessor whose input is
            // HierarchicalNode. Keep that explicit wrapper contract working while predicates
            // and item-typed accessors receive the underlying item by default.
            if (accessor.ItemType.IsInstanceOfType(node))
            {
                return node;
            }

            return underlyingItem;
        }

        private void HierarchicalModel_FlattenedChanged(object sender, FlattenedChangedEventArgs e) =>
            QueueHierarchyRefresh();

        private void HierarchicalModel_HierarchyChanged(object sender, HierarchyChangedEventArgs e) =>
            QueueHierarchyRefresh();

        private void HierarchicalModel_NodeLoaded(object sender, HierarchicalNodeEventArgs e) =>
            QueueHierarchyRefresh();

        private void QueueHierarchyRefresh()
        {
            // Hierarchy implementations commonly raise NodeLoaded/HierarchyChanged followed by
            // FlattenedChanged for one logical materialization. Always cross one queued boundary
            // and coalesce there. In particular, do not inspect Node.IsExpanded or Flattened on a
            // producer thread even when a headless dispatcher reports CheckAccess.
            if (Interlocked.CompareExchange(ref _refreshQueued, 1, 0) != 0)
            {
                return;
            }

            PostToViewThread(() =>
            {
                if (_disposed || Volatile.Read(ref _refreshQueued) == 0)
                {
                    Interlocked.Exchange(ref _refreshQueued, 0);
                    return;
                }

                // Keep the gate closed for one additional view-thread turn. Hierarchy models
                // can publish NodeLoaded before their final FlattenedChanged commit; the second
                // turn lets that final notification join this logical refresh even if a host
                // drains the first posted action from inside the NodeLoaded callback.
                PostToViewThread(() =>
                {
                    if (Interlocked.Exchange(ref _refreshQueued, 0) == 0 || _disposed)
                    {
                        return;
                    }

                    RefreshForHierarchyChangeCore();
                });
            });
        }

        private void RefreshForHierarchyChangeCore()
        {
            IDataGridCollectionView view = View;
            if (_disposed ||
                view == null ||
                _selfPredicate == null ||
                _policy == DataGridHierarchyFilterPolicy.SelfOnly)
            {
                return;
            }

            InvokeBeforeViewRefresh();
            try
            {
                RebuildIncludedNodes();
                view.Refresh();
            }
            finally
            {
                InvokeAfterViewRefresh();
            }
        }

        private void RebuildIncludedNodes()
        {
            HierarchicalNode root = _hierarchicalModel.Root;
            if (root == null || _selfPredicate == null)
            {
                _includedNodes = null;
                return;
            }

            var nodes = new List<HierarchicalNode>();
            var stack = new Stack<HierarchicalNode>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                HierarchicalNode node = stack.Pop();
                nodes.Add(node);
                IReadOnlyList<HierarchicalNode> children = node.Children;
                for (int i = children.Count - 1; i >= 0; i--)
                {
                    stack.Push(children[i]);
                }
            }

            var matches = new HashSet<HierarchicalNode>();
            for (int i = 0; i < nodes.Count; i++)
            {
                HierarchicalNode node = nodes[i];
                if (_hierarchicalModel.IsVirtualRoot && ReferenceEquals(node, root))
                {
                    continue;
                }

                if (_selfPredicate(node))
                {
                    matches.Add(node);
                }
            }

            var included = new HashSet<HierarchicalNode>();
            if ((_policy & DataGridHierarchyFilterPolicy.KeepDescendantsOfMatches) != 0)
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    HierarchicalNode node = nodes[i];
                    if (matches.Contains(node) ||
                        (node.Parent != null && included.Contains(node.Parent)))
                    {
                        included.Add(node);
                    }
                }
            }
            else
            {
                included.UnionWith(matches);
            }

            if ((_policy & DataGridHierarchyFilterPolicy.KeepAncestorsOfMatches) != 0)
            {
                for (int i = nodes.Count - 1; i >= 0; i--)
                {
                    HierarchicalNode node = nodes[i];
                    if (included.Contains(node))
                    {
                        continue;
                    }

                    IReadOnlyList<HierarchicalNode> children = node.Children;
                    for (int childIndex = 0; childIndex < children.Count; childIndex++)
                    {
                        if (included.Contains(children[childIndex]))
                        {
                            included.Add(node);
                            break;
                        }
                    }
                }
            }

            _includedNodes = included;
        }

        private bool IsIncluded(object item)
        {
            return item is HierarchicalNode node &&
                _includedNodes != null &&
                _includedNodes.Contains(node);
        }

        private static object GetUnderlyingItem(object item)
        {
            return item is HierarchicalNode node ? node.Item : item;
        }

    }

    /// <summary>
    /// Creates accessor-only hierarchy-aware filtering adapters for a DataGrid.
    /// </summary>
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    sealed class DataGridHierarchicalFilteringAdapterFactory : IDataGridFilteringAdapterFactory
    {
        /// <summary>
        /// Gets or sets the relative-preservation policy. The default keeps ancestor paths.
        /// </summary>
        public DataGridHierarchyFilterPolicy Policy { get; set; } =
            DataGridHierarchyFilterPolicy.KeepAncestorsOfMatches;

        /// <summary>
        /// Creates an adapter for the grid's current hierarchy model.
        /// </summary>
        /// <param name="grid">Owning grid.</param>
        /// <param name="model">Filtering model.</param>
        /// <returns>A hierarchy-aware filtering adapter.</returns>
        public DataGridFilteringAdapter Create(DataGrid grid, IFilteringModel model)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            return new DataGridHierarchicalFilteringAdapter(
                model,
                () => grid.ColumnsItemsInternal,
                grid.HierarchicalModel,
                Policy,
                grid.FastPathOptions);
        }
    }
}
