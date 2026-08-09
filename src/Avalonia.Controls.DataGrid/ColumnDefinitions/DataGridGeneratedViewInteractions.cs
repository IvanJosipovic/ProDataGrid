// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Avalonia.Controls
{
    /// <summary>
    /// Supplies a generated view interaction handler with its typed input and activation context.
    /// </summary>
    /// <typeparam name="TInput">The interaction input type.</typeparam>
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    readonly struct DataGridGeneratedViewInteractionContext<TInput> : IEquatable<DataGridGeneratedViewInteractionContext<TInput>>
    {
        /// <summary>Initializes a generated view interaction context.</summary>
        public DataGridGeneratedViewInteractionContext(
            Control view,
            DataGrid dataGrid,
            TInput input,
            CancellationToken cancellationToken)
        {
            View = view ?? throw new System.ArgumentNullException(nameof(view));
            DataGrid = dataGrid ?? throw new System.ArgumentNullException(nameof(dataGrid));
            Input = input;
            CancellationToken = cancellationToken;
        }

        /// <summary>Gets the generated view that registered the interaction.</summary>
        public Control View { get; }

        /// <summary>Gets the DataGrid owned by the generated view.</summary>
        public DataGrid DataGrid { get; }

        /// <summary>Gets the input supplied by the ViewModel interaction.</summary>
        public TInput Input { get; }

        /// <summary>Gets the token canceled when the generated view is deactivated.</summary>
        public CancellationToken CancellationToken { get; }

        /// <inheritdoc />
        public bool Equals(DataGridGeneratedViewInteractionContext<TInput> other) =>
            ReferenceEquals(View, other.View) &&
            ReferenceEquals(DataGrid, other.DataGrid) &&
            EqualityComparer<TInput>.Default.Equals(Input, other.Input) &&
            CancellationToken.Equals(other.CancellationToken);

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is DataGridGeneratedViewInteractionContext<TInput> other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(View, DataGrid, Input, CancellationToken);

        /// <summary>Tests two interaction contexts for equality.</summary>
        public static bool operator ==(DataGridGeneratedViewInteractionContext<TInput> left, DataGridGeneratedViewInteractionContext<TInput> right) => left.Equals(right);

        /// <summary>Tests two interaction contexts for inequality.</summary>
        public static bool operator !=(DataGridGeneratedViewInteractionContext<TInput> left, DataGridGeneratedViewInteractionContext<TInput> right) => !left.Equals(right);
    }

    /// <summary>
    /// Implements a typed, reflection-free response adapter for a generated ReactiveUI view interaction.
    /// </summary>
    /// <typeparam name="TInput">The interaction input type.</typeparam>
    /// <typeparam name="TOutput">The interaction output type.</typeparam>
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    interface IDataGridGeneratedViewInteractionHandler<TInput, TOutput>
    {
        /// <summary>Handles an interaction while the generated view is active.</summary>
        ValueTask<TOutput> HandleAsync(DataGridGeneratedViewInteractionContext<TInput> context);
    }
}
