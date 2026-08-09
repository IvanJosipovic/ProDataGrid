// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Avalonia.Controls
{
    /// <summary>
    /// Identifies DataGrid routed events that a generated view can forward to a ViewModel command.
    /// </summary>
    [Flags]
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    enum DataGridGeneratedViewEventKinds
    {
        /// <summary>No routed events are forwarded.</summary>
        None = 0,

        /// <summary>Forward selection changes.</summary>
        SelectionChanged = 1 << 0,

        /// <summary>Forward current-cell changes.</summary>
        CurrentCellChanged = 1 << 1,

        /// <summary>Forward column sorting requests.</summary>
        Sorting = 1 << 2,

        /// <summary>Forward cancellable beginning-edit events.</summary>
        BeginningEdit = 1 << 3,

        /// <summary>Forward cancellable cell-edit-ending events.</summary>
        CellEditEnding = 1 << 4,

        /// <summary>Forward cell-edit-ended events.</summary>
        CellEditEnded = 1 << 5,

        /// <summary>Forward cancellable row-edit-ending events.</summary>
        RowEditEnding = 1 << 6,

        /// <summary>Forward row-edit-ended events.</summary>
        RowEditEnded = 1 << 7,

        /// <summary>Forward cancellable transactional selection proposals.</summary>
        SelectionChanging = 1 << 8,

        /// <summary>Forward realized cell preparation notifications.</summary>
        CellPrepared = 1 << 9,

        /// <summary>Forward realized cell clearing notifications.</summary>
        CellClearing = 1 << 10,

        /// <summary>Forward committed grid-editor value changes.</summary>
        CellValueChanged = 1 << 11,

        /// <summary>Forward all editing lifecycle events.</summary>
        Editing = BeginningEdit | CellEditEnding | CellEditEnded | RowEditEnding | RowEditEnded,

        /// <summary>Forward both cell realization lifecycle events.</summary>
        CellLifecycle = CellPrepared | CellClearing,

        /// <summary>Forward every supported generated view event.</summary>
        All = SelectionChanged | CurrentCellChanged | Sorting | Editing |
              SelectionChanging | CellLifecycle | CellValueChanged
    }

    /// <summary>
    /// Provides a zero-copy typed view over the item lists supplied by a selection event.
    /// </summary>
    /// <typeparam name="TItem">The generated grid item type.</typeparam>
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    readonly struct DataGridGeneratedItemList<TItem> : IReadOnlyList<TItem>, IEquatable<DataGridGeneratedItemList<TItem>>
    {
        private readonly IList? _items;
        private readonly IReadOnlyList<object>? _readOnlyItems;

        /// <summary>
        /// Initializes a typed list view over an existing non-generic item list.
        /// </summary>
        public DataGridGeneratedItemList(IList items)
        {
            _items = items;
            _readOnlyItems = null;
        }

        /// <summary>
        /// Initializes a typed list view over an existing read-only object list.
        /// </summary>
        public DataGridGeneratedItemList(IReadOnlyList<object> items)
        {
            _items = null;
            _readOnlyItems = items;
        }

        /// <inheritdoc />
        public int Count => _items?.Count ?? _readOnlyItems?.Count ?? 0;

        /// <inheritdoc />
        public TItem this[int index] => (TItem)(_items != null ? _items[index]! : _readOnlyItems![index]!);

        /// <summary>
        /// Returns an allocation-free enumerator when used directly in a <c>foreach</c> statement.
        /// </summary>
        public Enumerator GetEnumerator() => new(_items, _readOnlyItems);

        IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        public bool Equals(DataGridGeneratedItemList<TItem> other) =>
            ReferenceEquals(_items, other._items) && ReferenceEquals(_readOnlyItems, other._readOnlyItems);

        /// <inheritdoc />
        public override bool Equals(object? obj) =>
            obj is DataGridGeneratedItemList<TItem> other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => _items?.GetHashCode() ?? _readOnlyItems?.GetHashCode() ?? 0;

        /// <summary>Compares whether two projections wrap the same source list.</summary>
        public static bool operator ==(
            DataGridGeneratedItemList<TItem> left,
            DataGridGeneratedItemList<TItem> right) => left.Equals(right);

        /// <summary>Compares whether two projections wrap different source lists.</summary>
        public static bool operator !=(
            DataGridGeneratedItemList<TItem> left,
            DataGridGeneratedItemList<TItem> right) => !left.Equals(right);

        /// <summary>
        /// Enumerates a generated item-list projection.
        /// </summary>
        public struct Enumerator : IEnumerator<TItem>
        {
            private readonly IList? _items;
            private readonly IReadOnlyList<object>? _readOnlyItems;
            private int _index;

            internal Enumerator(IList? items, IReadOnlyList<object>? readOnlyItems)
            {
                _items = items;
                _readOnlyItems = readOnlyItems;
                _index = -1;
            }

            /// <inheritdoc />
            public TItem Current => (TItem)(_items != null ? _items[_index]! : _readOnlyItems![_index]!);

            object IEnumerator.Current => Current!;

            /// <inheritdoc />
            public bool MoveNext()
            {
                int next = _index + 1;
                int count = _items?.Count ?? _readOnlyItems?.Count ?? 0;
                if (next >= count)
                {
                    return false;
                }

                _index = next;
                return true;
            }

            /// <inheritdoc />
            public void Reset() => _index = -1;

            /// <inheritdoc />
            public void Dispose()
            {
            }
        }
    }

    /// <summary>
    /// Carries a typed, reflection-free snapshot from a generated DataGrid routed-event bridge.
    /// </summary>
    /// <typeparam name="TItem">The generated grid item type.</typeparam>
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    sealed class DataGridGeneratedViewEvent<TItem>
    {
        private DataGridGeneratedViewEvent(DataGridGeneratedViewEventKinds kind)
        {
            Kind = kind;
            ColumnKey = string.Empty;
            OldColumnKey = string.Empty;
            NewColumnKey = string.Empty;
            RowIndex = -1;
            SelectionSource = DataGridSelectionChangeSource.Unknown;
            HierarchyPath = Array.Empty<global::Avalonia.Controls.DataGridHierarchical.HierarchicalNode>();
            AddedRows = Array.Empty<DataGridSelectionRowInfo>();
            RemovedRows = Array.Empty<DataGridSelectionRowInfo>();
            AddedCells = Array.Empty<DataGridCellInfo>();
            RemovedCells = Array.Empty<DataGridCellInfo>();
            AddedColumns = Array.Empty<DataGridColumn>();
            RemovedColumns = Array.Empty<DataGridColumn>();
        }

        /// <summary>Gets the single routed-event kind represented by this snapshot.</summary>
        public DataGridGeneratedViewEventKinds Kind { get; }

        /// <summary>Gets the row item associated with an edit event.</summary>
        public TItem? Item { get; private set; }

        /// <summary>Gets the previous current-cell item.</summary>
        public TItem? OldItem { get; private set; }

        /// <summary>Gets the new current-cell item.</summary>
        public TItem? NewItem { get; private set; }

        /// <summary>Gets the items added by a selection change without copying the source list.</summary>
        public DataGridGeneratedItemList<TItem> AddedItems { get; private set; }

        /// <summary>Gets the items removed by a selection change without copying the source list.</summary>
        public DataGridGeneratedItemList<TItem> RemovedItems { get; private set; }

        /// <summary>Gets rows participating in a transactional selection proposal.</summary>
        public IReadOnlyList<DataGridSelectionRowInfo> AddedRows { get; private set; }

        /// <summary>Gets rows removed by a transactional selection proposal.</summary>
        public IReadOnlyList<DataGridSelectionRowInfo> RemovedRows { get; private set; }

        /// <summary>Gets cells participating in a transactional selection proposal.</summary>
        public IReadOnlyList<DataGridCellInfo> AddedCells { get; private set; }

        /// <summary>Gets cells removed by a transactional selection proposal.</summary>
        public IReadOnlyList<DataGridCellInfo> RemovedCells { get; private set; }

        /// <summary>Gets columns participating in a transactional selection proposal.</summary>
        public IReadOnlyList<DataGridColumn> AddedColumns { get; private set; }

        /// <summary>Gets columns removed by a transactional selection proposal.</summary>
        public IReadOnlyList<DataGridColumn> RemovedColumns { get; private set; }

        /// <summary>Gets the proposed current item.</summary>
        public TItem? ProposedCurrentItem { get; private set; }

        /// <summary>Gets the proposed current cell.</summary>
        public DataGridCellInfo ProposedCurrentCell { get; private set; }

        /// <summary>Gets the proposed selection anchor.</summary>
        public DataGridSelectionAnchorInfo ProposedAnchor { get; private set; }

        /// <summary>Gets the guarantee supplied for a transactional selection proposal.</summary>
        public DataGridSelectionChangingGuarantee? SelectionGuarantee { get; private set; }

        /// <summary>Gets the stable column key associated with the event.</summary>
        public string ColumnKey { get; private set; }

        /// <summary>Gets the previous current-cell column key.</summary>
        public string OldColumnKey { get; private set; }

        /// <summary>Gets the new current-cell column key.</summary>
        public string NewColumnKey { get; private set; }

        /// <summary>Gets the realized row index for an edit event, or -1 when unavailable.</summary>
        public int RowIndex { get; private set; }

        /// <summary>Gets the realized cell associated with a cell event.</summary>
        public DataGridCell? Cell { get; private set; }

        /// <summary>Gets the realized row associated with a cell event.</summary>
        public DataGridRow? Row { get; private set; }

        /// <summary>Gets the row data context associated with a cell event.</summary>
        public object? RowDataContext { get; private set; }

        /// <summary>Gets the hierarchy node associated with the event.</summary>
        public global::Avalonia.Controls.DataGridHierarchical.HierarchicalNode? HierarchicalNode { get; private set; }

        /// <summary>Gets the root-to-node hierarchy path associated with the event.</summary>
        public IReadOnlyList<global::Avalonia.Controls.DataGridHierarchical.HierarchicalNode> HierarchyPath { get; private set; }

        /// <summary>Gets the value before a committed cell edit.</summary>
        public object? OldValue { get; private set; }

        /// <summary>Gets the value after a committed cell edit.</summary>
        public object? NewValue { get; private set; }

        /// <summary>Gets the origin of a committed cell value change.</summary>
        public DataGridCellValueChangeOrigin? CellValueChangeOrigin { get; private set; }

        /// <summary>Gets the routed input event that triggered a selection proposal, when present.</summary>
        public global::Avalonia.Interactivity.RoutedEventArgs? TriggerEvent { get; private set; }

        /// <summary>Gets the edit action when the event belongs to the editing lifecycle.</summary>
        public DataGridEditAction? EditAction { get; private set; }

        /// <summary>Gets the origin of a selection change.</summary>
        public DataGridSelectionChangeSource SelectionSource { get; private set; }

        /// <summary>Gets a value indicating whether a selection change originated from user input.</summary>
        public bool IsUserInitiated { get; private set; }

        /// <summary>
        /// Gets or sets whether a cancellable edit event should be canceled. Generated views copy this
        /// value back after the command executes.
        /// </summary>
        public bool Cancel { get; set; }

        /// <summary>
        /// Gets or sets whether the originating routed event should be marked handled.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>Creates a selection-change snapshot.</summary>
        public static DataGridGeneratedViewEvent<TItem> CreateSelectionChanged(
            IList addedItems,
            IList removedItems,
            DataGridSelectionChangeSource source,
            bool isUserInitiated)
        {
            return new DataGridGeneratedViewEvent<TItem>(DataGridGeneratedViewEventKinds.SelectionChanged)
            {
                AddedItems = new DataGridGeneratedItemList<TItem>(addedItems),
                RemovedItems = new DataGridGeneratedItemList<TItem>(removedItems),
                SelectionSource = source,
                IsUserInitiated = isUserInitiated
            };
        }

        /// <summary>Creates a complete transactional selection-proposal snapshot.</summary>
        public static DataGridGeneratedViewEvent<TItem> CreateSelectionChanging(DataGridSelectionChangingEventArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            return new DataGridGeneratedViewEvent<TItem>(DataGridGeneratedViewEventKinds.SelectionChanging)
            {
                AddedItems = new DataGridGeneratedItemList<TItem>(args.AddedItems),
                RemovedItems = new DataGridGeneratedItemList<TItem>(args.RemovedItems),
                AddedRows = args.AddedRows,
                RemovedRows = args.RemovedRows,
                AddedCells = args.AddedCells,
                RemovedCells = args.RemovedCells,
                AddedColumns = args.AddedColumns,
                RemovedColumns = args.RemovedColumns,
                ProposedCurrentItem = args.ProposedCurrentItem is TItem item ? item : default,
                ProposedCurrentCell = args.ProposedCurrentCell,
                ProposedAnchor = args.ProposedAnchor,
                HierarchicalNode = args.HierarchyNode,
                HierarchyPath = args.HierarchyPath,
                SelectionSource = args.Source,
                SelectionGuarantee = args.Guarantee,
                IsUserInitiated = args.IsUserInitiated,
                TriggerEvent = args.TriggerEvent,
                Cancel = args.Cancel,
                Handled = args.TriggerEvent?.Handled ?? false
            };
        }

        /// <summary>Creates a cell realization lifecycle snapshot.</summary>
        public static DataGridGeneratedViewEvent<TItem> CreateCellLifecycle(
            DataGridGeneratedViewEventKinds kind,
            DataGridCellLifecycleEventArgs args,
            string columnKey)
        {
            if (kind != DataGridGeneratedViewEventKinds.CellPrepared &&
                kind != DataGridGeneratedViewEventKinds.CellClearing)
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            return new DataGridGeneratedViewEvent<TItem>(kind)
            {
                Item = args.Item is TItem item ? item : default,
                Cell = args.Cell,
                Row = args.Row,
                RowDataContext = args.RowDataContext,
                RowIndex = args.Row.Index,
                ColumnKey = columnKey ?? string.Empty,
                HierarchicalNode = args.HierarchicalNode,
                HierarchyPath = args.HierarchyPath
            };
        }

        /// <summary>Creates a committed cell-value snapshot.</summary>
        public static DataGridGeneratedViewEvent<TItem> CreateCellValueChanged(
            DataGridCellValueChangedEventArgs args,
            string columnKey)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            return new DataGridGeneratedViewEvent<TItem>(DataGridGeneratedViewEventKinds.CellValueChanged)
            {
                Item = args.Item is TItem item ? item : default,
                Cell = args.Cell,
                Row = args.Row,
                RowDataContext = args.RowDataContext,
                RowIndex = args.Row.Index,
                ColumnKey = columnKey ?? string.Empty,
                HierarchicalNode = args.HierarchicalNode,
                HierarchyPath = args.HierarchyPath,
                OldValue = args.OldValue,
                NewValue = args.NewValue,
                CellValueChangeOrigin = args.Origin
            };
        }

        /// <summary>Creates a current-cell-change snapshot.</summary>
        public static DataGridGeneratedViewEvent<TItem> CreateCurrentCellChanged(
            TItem oldItem,
            string oldColumnKey,
            TItem newItem,
            string newColumnKey)
        {
            return new DataGridGeneratedViewEvent<TItem>(DataGridGeneratedViewEventKinds.CurrentCellChanged)
            {
                OldItem = oldItem,
                OldColumnKey = oldColumnKey ?? string.Empty,
                NewItem = newItem,
                NewColumnKey = newColumnKey ?? string.Empty
            };
        }

        /// <summary>Creates a sorting-request snapshot.</summary>
        public static DataGridGeneratedViewEvent<TItem> CreateSorting(string columnKey)
        {
            return new DataGridGeneratedViewEvent<TItem>(DataGridGeneratedViewEventKinds.Sorting)
            {
                ColumnKey = columnKey ?? string.Empty
            };
        }

        /// <summary>Creates an editing-lifecycle snapshot.</summary>
        public static DataGridGeneratedViewEvent<TItem> CreateEdit(
            DataGridGeneratedViewEventKinds kind,
            TItem item,
            int rowIndex,
            string columnKey,
            DataGridEditAction? editAction,
            bool cancel)
        {
            const DataGridGeneratedViewEventKinds editKinds = DataGridGeneratedViewEventKinds.Editing;
            int kindValue = (int)kind;
            if (kind == DataGridGeneratedViewEventKinds.None ||
                (kind & ~editKinds) != 0 ||
                (kindValue & (kindValue - 1)) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            return new DataGridGeneratedViewEvent<TItem>(kind)
            {
                Item = item,
                RowIndex = rowIndex,
                ColumnKey = columnKey ?? string.Empty,
                EditAction = editAction,
                Cancel = cancel
            };
        }
    }
}
