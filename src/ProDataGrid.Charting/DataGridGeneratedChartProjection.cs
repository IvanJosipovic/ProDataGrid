// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Controls;
using ProCharts;

namespace ProDataGrid.Charting
{
    /// <summary>
    /// Owns a reflection-free chart projection for an inclusive spreadsheet cell range.
    /// Generated column keys select series and the chart request windows the source rows.
    /// </summary>
    public sealed class DataGridGeneratedChartRangeProjection : IDisposable
    {
        private readonly IReadOnlyList<IDataGridGeneratedAnalyticsField> _fields;
        private readonly IReadOnlyList<DataGridColumnDefinition> _columns;
        private bool _configured;
        private bool _disposed;

        internal DataGridGeneratedChartRangeProjection(
            IEnumerable items,
            IReadOnlyList<IDataGridGeneratedAnalyticsField> fields,
            IReadOnlyList<DataGridColumnDefinition> columns,
            DataGridCellRange range,
            int maximumRows)
        {
            ArgumentNullException.ThrowIfNull(items);
            _fields = fields ?? throw new ArgumentNullException(nameof(fields));
            _columns = columns ?? throw new ArgumentNullException(nameof(columns));
            if (maximumRows <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumRows));
            }

            MaximumRows = maximumRows;
            DataSource = new DataGridChartModel { ItemsSource = items };
            ValidateRowRange(range);
            DataGridGeneratedChartAdapter.ConfigureRange(DataSource, _fields, _columns, range);
            _configured = true;
            Range = range;
            Model = new ChartModel { AutoRefresh = false };
            ApplyRequest(range);
            Model.DataSource = DataSource;
            Model.AutoRefresh = true;
        }

        /// <summary>Gets the typed-selector data source owned by the projection.</summary>
        public DataGridChartModel DataSource { get; }

        /// <summary>Gets the render-ready chart model owned by the projection.</summary>
        public ChartModel Model { get; }

        /// <summary>Gets the current inclusive source range.</summary>
        public DataGridCellRange Range { get; private set; }

        /// <summary>Gets the maximum number of rows accepted by one projection.</summary>
        public int MaximumRows { get; }

        /// <summary>Reconfigures row windowing and selected generated series.</summary>
        public void UpdateRange(DataGridCellRange range)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ValidateRowRange(range);

            if (!_configured || range.StartColumn != Range.StartColumn || range.EndColumn != Range.EndColumn)
            {
                DataGridGeneratedChartAdapter.ConfigureRange(DataSource, _fields, _columns, range);
                _configured = true;
            }
            Range = range;
            ApplyRequest(range);
        }

        private void ApplyRequest(DataGridCellRange range)
        {
            using (Model.DeferRefresh())
            {
                Model.Request.WindowStart = range.StartRow;
                Model.Request.WindowCount = range.RowCount;
                Model.Request.MaxPoints = null;
                Model.Request.DownsampleMode = ChartDownsampleMode.None;
            }
        }

        private void ValidateRowRange(DataGridCellRange range)
        {
            if (range.RowCount <= 0 || range.RowCount > MaximumRows)
            {
                throw new ArgumentOutOfRangeException(nameof(range), $"A chart range must contain between 1 and {MaximumRows} rows.");
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Model.Dispose();
            DataSource.Dispose();
        }
    }

    /// <summary>Maps rendered chart categories to stable generated item keys.</summary>
    public interface IDataGridGeneratedChartKeyMap<TKey>
    {
        /// <summary>Resolves a source item key at a source index.</summary>
        bool TryGetKey(int sourceIndex, out TKey key);

        /// <summary>Resolves the current source index for a stable item key.</summary>
        bool TryGetIndex(TKey key, out int sourceIndex);
    }

    /// <summary>
    /// Maintains an incremental reflection-free key map over an observable list.
    /// Add, remove, replace, and single-item move notifications update the generated index directly.
    /// </summary>
    public sealed class DataGridGeneratedListChartKeyMap<TItem, TKey> : IDataGridGeneratedChartKeyMap<TKey>, IDisposable
    {
        private readonly IList _source;
        private readonly DataGridGeneratedItemIndex<TItem, TKey> _index;
        private readonly INotifyCollectionChanged? _observableSource;
        private bool _disposed;

        /// <summary>Initializes the key map and captures the current source ordering.</summary>
        public DataGridGeneratedListChartKeyMap(
            IList source,
            IDataGridItemKey<TItem, TKey> keyAccessor,
            IEqualityComparer<TKey>? comparer = null)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _index = new DataGridGeneratedItemIndex<TItem, TKey>(
                keyAccessor ?? throw new ArgumentNullException(nameof(keyAccessor)),
                comparer,
                source.Count);
            Reset();
            _observableSource = source as INotifyCollectionChanged;
            if (_observableSource != null)
            {
                _observableSource.CollectionChanged += OnCollectionChanged;
            }
        }

        /// <inheritdoc />
        public bool TryGetKey(int sourceIndex, out TKey key)
        {
            if ((uint)sourceIndex < (uint)_index.Count)
            {
                key = _index.GetKeyAt(sourceIndex);
                return true;
            }

            key = default!;
            return false;
        }

        /// <inheritdoc />
        public bool TryGetIndex(TKey key, out int sourceIndex) => _index.TryGetIndex(key, out sourceIndex);

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_observableSource != null)
            {
                _observableSource.CollectionChanged -= OnCollectionChanged;
            }
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add when e.NewStartingIndex >= 0 && e.NewItems != null:
                        for (int itemIndex = 0; itemIndex < e.NewItems.Count; itemIndex++)
                        {
                            _index.Insert(e.NewStartingIndex + itemIndex, GetItem(e.NewItems[itemIndex]));
                        }
                        return;

                    case NotifyCollectionChangedAction.Remove when e.OldStartingIndex >= 0 && e.OldItems != null:
                        for (int itemIndex = 0; itemIndex < e.OldItems.Count; itemIndex++)
                        {
                            _index.RemoveAt(e.OldStartingIndex);
                        }
                        return;

                    case NotifyCollectionChangedAction.Replace
                        when e.NewStartingIndex >= 0 && e.NewItems != null && e.OldItems?.Count == e.NewItems.Count:
                        for (int itemIndex = 0; itemIndex < e.NewItems.Count; itemIndex++)
                        {
                            _index.Replace(e.NewStartingIndex + itemIndex, GetItem(e.NewItems[itemIndex]));
                        }
                        return;

                    case NotifyCollectionChangedAction.Move
                        when e.OldStartingIndex >= 0 && e.NewStartingIndex >= 0 && e.OldItems?.Count == 1:
                        _index.Move(e.OldStartingIndex, e.NewStartingIndex);
                        return;
                }
            }
            catch (InvalidOperationException)
            {
                // A malformed or coalesced collection notification falls back to an atomic snapshot.
            }
            catch (ArgumentOutOfRangeException)
            {
                // A producer-specific index convention falls back to an atomic snapshot.
            }

            Reset();
        }

        private void Reset()
        {
            var items = new List<TItem>(_source.Count);
            for (int index = 0; index < _source.Count; index++)
            {
                items.Add(GetItem(_source[index]));
            }
            _index.Reset(items);
        }

        private static TItem GetItem(object? item) => item is TItem typed
            ? typed
            : throw new InvalidOperationException($"Chart key-map sources must contain only {typeof(TItem).FullName} items.");
    }

    /// <summary>
    /// Synchronizes stable generated grid selection with a chart category interaction.
    /// Index projection delegates keep grouped, windowed, or user-defined chart sources first-class.
    /// </summary>
    public sealed class DataGridGeneratedChartSelectionSynchronizer<TItem, TKey> : IDisposable
    {
        private readonly IDataGridGeneratedChartKeyMap<TKey> _keyMap;
        private readonly DataGridGeneratedSelectionController<TItem, TKey> _selection;
        private readonly ChartInteractionState _interaction;
        private readonly Func<int, int> _categoryToSourceIndex;
        private readonly Func<int, int> _sourceToCategoryIndex;
        private readonly Func<int, string?>? _categoryLabel;
        private bool _synchronizing;
        private bool _disposed;

        /// <summary>Initializes bidirectional keyed selection synchronization.</summary>
        public DataGridGeneratedChartSelectionSynchronizer(
            IDataGridGeneratedChartKeyMap<TKey> keyMap,
            DataGridGeneratedSelectionController<TItem, TKey> selection,
            ChartInteractionState interaction,
            Func<int, int>? categoryToSourceIndex = null,
            Func<int, int>? sourceToCategoryIndex = null,
            Func<int, string?>? categoryLabel = null)
        {
            _keyMap = keyMap ?? throw new ArgumentNullException(nameof(keyMap));
            _selection = selection ?? throw new ArgumentNullException(nameof(selection));
            _interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
            _categoryToSourceIndex = categoryToSourceIndex ?? Identity;
            _sourceToCategoryIndex = sourceToCategoryIndex ?? Identity;
            _categoryLabel = categoryLabel;
            _selection.SelectionChanged += OnSelectionChanged;
            _interaction.PropertyChanged += OnInteractionPropertyChanged;
            UpdateChartFromSelection();
        }

        /// <summary>Selects the stable grid row represented by a chart category.</summary>
        public bool SelectCategory(int categoryIndex)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            int sourceIndex = _categoryToSourceIndex(categoryIndex);
            if (!_keyMap.TryGetKey(sourceIndex, out TKey key))
            {
                return false;
            }

            _synchronizing = true;
            try
            {
                return _selection.SelectOnlyKey(key, DataGridGeneratedSelectionOrigin.Chart);
            }
            finally
            {
                _synchronizing = false;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _selection.SelectionChanged -= OnSelectionChanged;
            _interaction.PropertyChanged -= OnInteractionPropertyChanged;
        }

        private void OnSelectionChanged(object? sender, DataGridGeneratedSelectionChangedEventArgs e)
        {
            if (!_synchronizing && e.Origin != DataGridGeneratedSelectionOrigin.Chart)
            {
                UpdateChartFromSelection();
            }
        }

        private void OnInteractionPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (!_synchronizing && e.PropertyName == nameof(ChartInteractionState.CrosshairCategoryIndex) &&
                _interaction.CrosshairCategoryIndex is int categoryIndex)
            {
                SelectCategory(categoryIndex);
            }
        }

        private void UpdateChartFromSelection()
        {
            IReadOnlyList<TKey> selectedKeys = _selection.SelectedItemKeys;
            if (selectedKeys.Count == 0 || !_keyMap.TryGetIndex(selectedKeys[selectedKeys.Count - 1], out int sourceIndex))
            {
                _interaction.ClearCrosshair();
                return;
            }

            int categoryIndex = _sourceToCategoryIndex(sourceIndex);
            if (categoryIndex < 0)
            {
                _interaction.ClearCrosshair();
                return;
            }

            _synchronizing = true;
            try
            {
                _interaction.SetCrosshair(
                    categoryIndex,
                    _categoryLabel?.Invoke(categoryIndex),
                    null,
                    0.5d,
                    0.5d);
            }
            finally
            {
                _synchronizing = false;
            }
        }

        private static int Identity(int index) => index;
    }
}
