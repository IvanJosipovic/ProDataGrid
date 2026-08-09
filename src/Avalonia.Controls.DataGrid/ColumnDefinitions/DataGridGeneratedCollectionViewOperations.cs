// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Collections;
using Avalonia.Controls.DataGridSorting;

namespace Avalonia.Controls
{
    /// <summary>Applies generated typed operations to a <see cref="DataGridCollectionView"/>.</summary>
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    static class DataGridGeneratedCollectionViewOperations
    {
        /// <summary>
        /// Replaces collection-view sorting with one comparer compiled from generated field accessors.
        /// </summary>
        /// <typeparam name="TItem">The row item type.</typeparam>
        public static void ApplySorting<TItem>(
            DataGridCollectionView view,
            IDataGridSortingCompiler<TItem> compiler,
            IReadOnlyList<SortingDescriptor> descriptors)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }
            if (compiler == null)
            {
                throw new ArgumentNullException(nameof(compiler));
            }
            if (descriptors == null)
            {
                throw new ArgumentNullException(nameof(descriptors));
            }

            view.SortDescriptions.Clear();
            if (descriptors.Count == 0)
            {
                return;
            }

            IComparer<TItem> comparer = compiler.CreateSortComparer(descriptors);
            view.SortDescriptions.Add(DataGridSortDescription.FromComparer(
                new TypedComparer<TItem>(comparer),
                ListSortDirection.Ascending));
        }

        private sealed class TypedComparer<TItem> : IComparer
        {
            private readonly IComparer<TItem> _comparer;

            public TypedComparer(IComparer<TItem> comparer) =>
                _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));

            public int Compare(object x, object y) => _comparer.Compare((TItem)x, (TItem)y);
        }
    }
}
