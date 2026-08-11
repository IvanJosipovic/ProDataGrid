// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Avalonia.Controls
{
    /// <summary>
    /// Applies range-aware collection mutations to an application-owned data source.
    /// </summary>
    /// <typeparam name="TItem">The row item type.</typeparam>
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    interface IDataGridGeneratedCollectionMutationHandler<TItem>
    {
        /// <summary>Inserts a contiguous range at <paramref name="index"/>.</summary>
        ValueTask AddAsync(int index, ReadOnlyMemory<TItem> items, CancellationToken cancellationToken);

        /// <summary>Removes the supplied contiguous range at <paramref name="index"/>.</summary>
        ValueTask RemoveAsync(int index, ReadOnlyMemory<TItem> items, CancellationToken cancellationToken);

        /// <summary>Replaces one contiguous range with another at <paramref name="index"/>.</summary>
        ValueTask ReplaceAsync(
            int index,
            ReadOnlyMemory<TItem> oldItems,
            ReadOnlyMemory<TItem> newItems,
            CancellationToken cancellationToken);

        /// <summary>Moves a contiguous range without expanding it into per-item operations.</summary>
        ValueTask MoveAsync(int oldIndex, int newIndex, int count, CancellationToken cancellationToken);

        /// <summary>Replaces the complete domain source with the supplied snapshot.</summary>
        ValueTask ResetAsync(ReadOnlyMemory<TItem> items, CancellationToken cancellationToken);
    }

    /// <summary>Creates new rows through an application-owned policy.</summary>
    /// <typeparam name="TItem">The row item type.</typeparam>
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    interface IDataGridGeneratedNewRowFactory<TItem>
    {
        /// <summary>Creates one row without requiring reflection or a public parameterless item constructor.</summary>
        ValueTask<TItem> CreateAsync(CancellationToken cancellationToken);
    }

    /// <summary>
    /// Validates and forwards bounded range mutations to an application-owned handler.
    /// The service does not own or inspect the destination collection.
    /// </summary>
    /// <typeparam name="TItem">The row item type.</typeparam>
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    sealed class DataGridGeneratedCollectionMutationService<TItem>
    {
        private readonly IDataGridGeneratedCollectionMutationHandler<TItem> _handler;

        /// <summary>Initializes a bounded mutation service.</summary>
        public DataGridGeneratedCollectionMutationService(
            IDataGridGeneratedCollectionMutationHandler<TItem> handler,
            int maximumItemsPerMutation = 65536)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            if (maximumItemsPerMutation <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumItemsPerMutation));
            }

            MaximumItemsPerMutation = maximumItemsPerMutation;
        }

        /// <summary>Gets the maximum number of items accepted by one mutation.</summary>
        public int MaximumItemsPerMutation { get; }

        /// <summary>Forwards one bounded add range.</summary>
        public ValueTask AddAsync(
            int index,
            ReadOnlyMemory<TItem> items,
            CancellationToken cancellationToken = default)
        {
            ValidateIndex(index, nameof(index));
            ValidateRange(items, nameof(items));
            cancellationToken.ThrowIfCancellationRequested();
            return _handler.AddAsync(index, items, cancellationToken);
        }

        /// <summary>Forwards one bounded remove range.</summary>
        public ValueTask RemoveAsync(
            int index,
            ReadOnlyMemory<TItem> items,
            CancellationToken cancellationToken = default)
        {
            ValidateIndex(index, nameof(index));
            ValidateRange(items, nameof(items));
            cancellationToken.ThrowIfCancellationRequested();
            return _handler.RemoveAsync(index, items, cancellationToken);
        }

        /// <summary>Forwards one bounded replace range.</summary>
        public ValueTask ReplaceAsync(
            int index,
            ReadOnlyMemory<TItem> oldItems,
            ReadOnlyMemory<TItem> newItems,
            CancellationToken cancellationToken = default)
        {
            ValidateIndex(index, nameof(index));
            ValidateRange(oldItems, nameof(oldItems));
            ValidateRange(newItems, nameof(newItems));
            cancellationToken.ThrowIfCancellationRequested();
            return _handler.ReplaceAsync(index, oldItems, newItems, cancellationToken);
        }

        /// <summary>Forwards one bounded move range.</summary>
        public ValueTask MoveAsync(
            int oldIndex,
            int newIndex,
            int count,
            CancellationToken cancellationToken = default)
        {
            ValidateIndex(oldIndex, nameof(oldIndex));
            ValidateIndex(newIndex, nameof(newIndex));
            ValidateCount(count, nameof(count));
            cancellationToken.ThrowIfCancellationRequested();
            return _handler.MoveAsync(oldIndex, newIndex, count, cancellationToken);
        }

        /// <summary>Forwards one bounded complete-source reset.</summary>
        public ValueTask ResetAsync(
            ReadOnlyMemory<TItem> items,
            CancellationToken cancellationToken = default)
        {
            ValidateRange(items, nameof(items), allowEmpty: true);
            cancellationToken.ThrowIfCancellationRequested();
            return _handler.ResetAsync(items, cancellationToken);
        }

        private static void ValidateIndex(int index, string parameterName)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }

        private void ValidateRange(ReadOnlyMemory<TItem> items, string parameterName, bool allowEmpty = false)
        {
            if (!allowEmpty && items.IsEmpty)
            {
                throw new ArgumentException("A collection mutation range cannot be empty.", parameterName);
            }

            ValidateCount(items.Length, parameterName, allowEmpty);
        }

        private void ValidateCount(int count, string parameterName, bool allowEmpty = false)
        {
            if ((!allowEmpty && count <= 0) || count < 0 || count > MaximumItemsPerMutation)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }
    }

    /// <summary>Executes an application-owned new-row policy.</summary>
    /// <typeparam name="TItem">The row item type.</typeparam>
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    sealed class DataGridGeneratedNewRowService<TItem>
    {
        private readonly IDataGridGeneratedNewRowFactory<TItem> _factory;

        /// <summary>Initializes the service.</summary>
        public DataGridGeneratedNewRowService(IDataGridGeneratedNewRowFactory<TItem> factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <summary>Creates one row through the configured application policy.</summary>
        public ValueTask<TItem> CreateAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return _factory.CreateAsync(cancellationToken);
        }
    }
}
