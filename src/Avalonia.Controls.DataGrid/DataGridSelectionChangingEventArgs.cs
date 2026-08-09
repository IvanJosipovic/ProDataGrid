// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Controls.DataGridHierarchical;
using Avalonia.Interactivity;

namespace Avalonia.Controls
{
    /// <summary>
    /// Describes where a <see cref="DataGrid.SelectionChanging"/> proposal sits relative to
    /// the state change that produced it.
    /// </summary>
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    enum DataGridSelectionChangingGuarantee
    {
        /// <summary>
        /// The proposal is raised before the DataGrid commits selection, current-cell, anchor,
        /// currency, focus, or scrolling state. Cancellation commits none of that proposed state.
        /// </summary>
        AtomicPreflight = 0,

        /// <summary>
        /// An external producer has already published its change, so the DataGrid is reconciling
        /// that change with its last committed state. Cancellation prevents an optional partial
        /// DataGrid commit, but cannot erase external notifications, undo an item-source change,
        /// or preserve a selected identity that the external source removed.
        /// </summary>
        PostChangeReconciliation = 1,
    }

    /// <summary>
    /// Describes a row participating in a proposed selection delta.
    /// </summary>
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    sealed class DataGridSelectionRowInfo
    {
        internal DataGridSelectionRowInfo(
            object rowDataContext,
            object item,
            int rowIndex,
            HierarchicalNode hierarchicalNode,
            IReadOnlyList<HierarchicalNode> hierarchyPath)
        {
            RowDataContext = rowDataContext;
            Item = item;
            RowIndex = rowIndex;
            HierarchicalNode = hierarchicalNode;
            HierarchyPath = hierarchyPath ?? Array.Empty<HierarchicalNode>();
        }

        /// <summary>
        /// Gets the data context used by the row. In hierarchical mode this is a
        /// <see cref="HierarchicalNode"/>.
        /// </summary>
        public object RowDataContext { get; }

        /// <summary>
        /// Gets the underlying row item.
        /// </summary>
        public object Item { get; }

        /// <summary>
        /// Gets the row index in the current view.
        /// </summary>
        public int RowIndex { get; }

        /// <summary>
        /// Gets the hierarchy node, or <see langword="null"/> for a flat row.
        /// </summary>
        public HierarchicalNode HierarchicalNode { get; }

        /// <summary>
        /// Gets the root-to-node path, excluding a hidden virtual root.
        /// </summary>
        public IReadOnlyList<HierarchicalNode> HierarchyPath { get; }
    }

    /// <summary>
    /// Describes the proposed selection anchor.
    /// </summary>
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    readonly struct DataGridSelectionAnchorInfo : IEquatable<DataGridSelectionAnchorInfo>
    {
        internal DataGridSelectionAnchorInfo(
            object rowDataContext,
            object item,
            int rowIndex,
            int columnIndex,
            HierarchicalNode hierarchicalNode,
            IReadOnlyList<HierarchicalNode> hierarchyPath,
            bool isValid)
        {
            RowDataContext = rowDataContext;
            Item = item;
            RowIndex = rowIndex;
            ColumnIndex = columnIndex;
            HierarchicalNode = hierarchicalNode;
            HierarchyPath = hierarchyPath ?? Array.Empty<HierarchicalNode>();
            IsValid = isValid;
        }

        /// <summary>
        /// Gets the row data context.
        /// </summary>
        public object RowDataContext { get; }

        /// <summary>
        /// Gets the underlying item.
        /// </summary>
        public object Item { get; }

        /// <summary>
        /// Gets the anchor row index.
        /// </summary>
        public int RowIndex { get; }

        /// <summary>
        /// Gets the anchor column index, or -1 for a row-only anchor.
        /// </summary>
        public int ColumnIndex { get; }

        /// <summary>
        /// Gets the hierarchy node, or <see langword="null"/> for a flat row.
        /// </summary>
        public HierarchicalNode HierarchicalNode { get; }

        /// <summary>
        /// Gets the root-to-node hierarchy path.
        /// </summary>
        public IReadOnlyList<HierarchicalNode> HierarchyPath { get; }

        /// <summary>
        /// Gets a value indicating whether the anchor resolves to a current row.
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Gets an unset anchor.
        /// </summary>
        public static DataGridSelectionAnchorInfo Unset { get; } = default;

        /// <inheritdoc />
        public bool Equals(DataGridSelectionAnchorInfo other) =>
            ReferenceEquals(RowDataContext, other.RowDataContext) &&
            ReferenceEquals(Item, other.Item) &&
            RowIndex == other.RowIndex &&
            ColumnIndex == other.ColumnIndex &&
            ReferenceEquals(HierarchicalNode, other.HierarchicalNode) &&
            IsValid == other.IsValid;

        /// <inheritdoc />
        public override bool Equals(object obj) =>
            obj is DataGridSelectionAnchorInfo other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() =>
            HashCode.Combine(RowDataContext, Item, RowIndex, ColumnIndex, HierarchicalNode, IsValid);

        /// <summary>
        /// Determines whether two anchor values identify the same selection anchor.
        /// </summary>
        public static bool operator ==(DataGridSelectionAnchorInfo left, DataGridSelectionAnchorInfo right) =>
            left.Equals(right);

        /// <summary>
        /// Determines whether two anchor values identify different selection anchors.
        /// </summary>
        public static bool operator !=(DataGridSelectionAnchorInfo left, DataGridSelectionAnchorInfo right) =>
            !left.Equals(right);
    }

    /// <summary>
    /// Provides a complete, cancellable selection proposal. <see cref="Guarantee"/> identifies
    /// whether the proposal is an atomic preflight or reconciliation of a change already
    /// published by a caller-owned producer.
    /// </summary>
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    sealed class DataGridSelectionChangingEventArgs : CancelEventArgs
    {
        internal DataGridSelectionChangingEventArgs(
            IReadOnlyList<object> addedItems,
            IReadOnlyList<object> removedItems,
            IReadOnlyList<DataGridSelectionRowInfo> addedRows,
            IReadOnlyList<DataGridSelectionRowInfo> removedRows,
            IReadOnlyList<DataGridCellInfo> addedCells,
            IReadOnlyList<DataGridCellInfo> removedCells,
            IReadOnlyList<DataGridColumn> addedColumns,
            IReadOnlyList<DataGridColumn> removedColumns,
            object proposedCurrentItem,
            DataGridCellInfo proposedCurrentCell,
            DataGridSelectionAnchorInfo proposedAnchor,
            HierarchicalNode hierarchyNode,
            IReadOnlyList<HierarchicalNode> hierarchyPath,
            DataGridSelectionChangeSource source,
            RoutedEventArgs triggerEvent,
            DataGridSelectionChangingGuarantee guarantee = DataGridSelectionChangingGuarantee.AtomicPreflight)
        {
            AddedItems = addedItems ?? Array.Empty<object>();
            RemovedItems = removedItems ?? Array.Empty<object>();
            AddedRows = addedRows ?? Array.Empty<DataGridSelectionRowInfo>();
            RemovedRows = removedRows ?? Array.Empty<DataGridSelectionRowInfo>();
            AddedCells = addedCells ?? Array.Empty<DataGridCellInfo>();
            RemovedCells = removedCells ?? Array.Empty<DataGridCellInfo>();
            AddedColumns = addedColumns ?? Array.Empty<DataGridColumn>();
            RemovedColumns = removedColumns ?? Array.Empty<DataGridColumn>();
            ProposedCurrentItem = proposedCurrentItem;
            ProposedCurrentCell = proposedCurrentCell;
            ProposedAnchor = proposedAnchor;
            HierarchyNode = hierarchyNode;
            HierarchyPath = hierarchyPath ?? Array.Empty<HierarchicalNode>();
            Source = source;
            TriggerEvent = triggerEvent;
            Guarantee = guarantee;
        }

        /// <summary>Gets the underlying items proposed for addition.</summary>
        public IReadOnlyList<object> AddedItems { get; }

        /// <summary>Gets the underlying items proposed for removal.</summary>
        public IReadOnlyList<object> RemovedItems { get; }

        /// <summary>Gets the rows proposed for addition.</summary>
        public IReadOnlyList<DataGridSelectionRowInfo> AddedRows { get; }

        /// <summary>Gets the rows proposed for removal.</summary>
        public IReadOnlyList<DataGridSelectionRowInfo> RemovedRows { get; }

        /// <summary>Gets the cells proposed for addition.</summary>
        public IReadOnlyList<DataGridCellInfo> AddedCells { get; }

        /// <summary>Gets the cells proposed for removal.</summary>
        public IReadOnlyList<DataGridCellInfo> RemovedCells { get; }

        /// <summary>Gets the columns proposed for addition.</summary>
        public IReadOnlyList<DataGridColumn> AddedColumns { get; }

        /// <summary>Gets the columns proposed for removal.</summary>
        public IReadOnlyList<DataGridColumn> RemovedColumns { get; }

        /// <summary>Gets the proposed underlying current item.</summary>
        public object ProposedCurrentItem { get; }

        /// <summary>Gets the proposed current cell.</summary>
        public DataGridCellInfo ProposedCurrentCell { get; }

        /// <summary>Gets the proposed selection anchor.</summary>
        public DataGridSelectionAnchorInfo ProposedAnchor { get; }

        /// <summary>Gets the proposed current hierarchy node, when applicable.</summary>
        public HierarchicalNode HierarchyNode { get; }

        /// <summary>Gets the root-to-node path for the proposed current hierarchy item.</summary>
        public IReadOnlyList<HierarchicalNode> HierarchyPath { get; }

        /// <summary>Gets the origin of the selection proposal.</summary>
        public DataGridSelectionChangeSource Source { get; }

        /// <summary>
        /// Gets whether the proposal is an atomic preflight or post-change reconciliation.
        /// </summary>
        public DataGridSelectionChangingGuarantee Guarantee { get; }

        /// <summary>Gets whether the proposal originated from a user interaction.</summary>
        public bool IsUserInitiated =>
            (Source & (DataGridSelectionChangeSource.Pointer |
                       DataGridSelectionChangeSource.Keyboard |
                       DataGridSelectionChangeSource.Command |
                       DataGridSelectionChangeSource.DragInteraction)) != 0;

        /// <summary>Gets the routed event that triggered the proposal, when available.</summary>
        public RoutedEventArgs TriggerEvent { get; }
    }
}
