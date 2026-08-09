// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Input;
using Avalonia.Controls.DataGridFiltering;
using Avalonia.Controls.DataGridSearching;
using Avalonia.Controls.DataGridSorting;

namespace Avalonia.Controls
{
    /// <summary>Identifies the model owning one generated operation descriptor.</summary>
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    enum DataGridGeneratedOperationDescriptorKind
    {
        /// <summary>A sorting descriptor.</summary>
        Sorting,
        /// <summary>A filtering descriptor.</summary>
        Filtering,
        /// <summary>A searching descriptor.</summary>
        Searching
    }

    /// <summary>Provides immutable presentation metadata for one active operation descriptor.</summary>
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    sealed class DataGridGeneratedOperationDescriptor : IEquatable<DataGridGeneratedOperationDescriptor>
    {
        internal DataGridGeneratedOperationDescriptor(
            DataGridGeneratedOperationDescriptorKind kind,
            object columnId,
            string summary,
            object descriptor)
        {
            Kind = kind;
            ColumnId = columnId;
            Summary = summary ?? string.Empty;
            Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        }

        /// <summary>Gets the owning operation model.</summary>
        public DataGridGeneratedOperationDescriptorKind Kind { get; }

        /// <summary>Gets the stable generated column identifier, when the descriptor targets a column.</summary>
        public object ColumnId { get; }

        /// <summary>Gets an allocation-on-revision display summary suitable for a descriptor chip.</summary>
        public string Summary { get; }

        /// <summary>Gets the underlying immutable runtime descriptor.</summary>
        public object Descriptor { get; }

        /// <inheritdoc />
        public bool Equals(DataGridGeneratedOperationDescriptor other) =>
            other != null && Kind == other.Kind && Equals(Descriptor, other.Descriptor);

        /// <inheritdoc />
        public override bool Equals(object obj) => Equals(obj as DataGridGeneratedOperationDescriptor);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine((int)Kind, Descriptor);
    }

    /// <summary>Exposes reusable framework-neutral commands for a generated operation controller.</summary>
    /// <typeparam name="TItem">The row item type.</typeparam>
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    sealed class DataGridGeneratedOperationCommandSet<TItem>
    {
        internal DataGridGeneratedOperationCommandSet(DataGridGeneratedOperationController<TItem> controller)
        {
            ArgumentNullException.ThrowIfNull(controller);
            ClearAll = new GeneratedCommand(static (owner, _) => owner.ClearOperations(), controller);
            ClearSorting = new GeneratedCommand(static (owner, _) => owner.SetSorting(Array.Empty<SortingDescriptor>()), controller,
                static owner => owner.IsFeatureEnabled(DataGridGeneratedFeatures.Sorting));
            ClearFiltering = new GeneratedCommand(static (owner, _) => owner.SetFiltering(Array.Empty<FilteringDescriptor>()), controller,
                static owner => owner.IsFeatureEnabled(DataGridGeneratedFeatures.Filtering));
            ClearSearching = new GeneratedCommand(static (owner, _) => owner.SetSearching(Array.Empty<SearchDescriptor>()), controller,
                static owner => owner.IsFeatureEnabled(DataGridGeneratedFeatures.Searching));
            ApplyPreset = new GeneratedCommand(
                static (owner, parameter) => owner.ApplyPreset((DataGridGeneratedOperationPreset)parameter),
                controller,
                static (owner, parameter) => parameter is DataGridGeneratedOperationPreset && owner.HasAnyOperationFeature);
            RemoveDescriptor = new GeneratedCommand(
                static (owner, parameter) => owner.RemoveDescriptor((DataGridGeneratedOperationDescriptor)parameter),
                controller,
                static (owner, parameter) => parameter is DataGridGeneratedOperationDescriptor descriptor && owner.CanRemoveDescriptor(descriptor));
            SearchNext = new GeneratedCommand(static (owner, _) => owner.SearchModel.MoveNext(), controller,
                static owner => owner.IsFeatureEnabled(DataGridGeneratedFeatures.Searching));
            SearchPrevious = new GeneratedCommand(static (owner, _) => owner.SearchModel.MovePrevious(), controller,
                static owner => owner.IsFeatureEnabled(DataGridGeneratedFeatures.Searching));
        }

        /// <summary>Gets the command that clears every enabled operation.</summary>
        public ICommand ClearAll { get; }
        /// <summary>Gets the command that clears sorting.</summary>
        public ICommand ClearSorting { get; }
        /// <summary>Gets the command that clears filtering.</summary>
        public ICommand ClearFiltering { get; }
        /// <summary>Gets the command that clears searching.</summary>
        public ICommand ClearSearching { get; }
        /// <summary>Gets the command that applies a <see cref="DataGridGeneratedOperationPreset"/> parameter.</summary>
        public ICommand ApplyPreset { get; }
        /// <summary>Gets the command that removes a <see cref="DataGridGeneratedOperationDescriptor"/> parameter.</summary>
        public ICommand RemoveDescriptor { get; }
        /// <summary>Gets the command that navigates to the next search result.</summary>
        public ICommand SearchNext { get; }
        /// <summary>Gets the command that navigates to the previous search result.</summary>
        public ICommand SearchPrevious { get; }

        private sealed class GeneratedCommand : ICommand
        {
            private readonly DataGridGeneratedOperationController<TItem> _owner;
            private readonly Action<DataGridGeneratedOperationController<TItem>, object> _execute;
            private readonly Func<DataGridGeneratedOperationController<TItem>, object, bool> _canExecute;

            public GeneratedCommand(
                Action<DataGridGeneratedOperationController<TItem>, object> execute,
                DataGridGeneratedOperationController<TItem> owner,
                Func<DataGridGeneratedOperationController<TItem>, bool> canExecute = null)
                : this(execute, owner, canExecute == null ? null : (candidate, _) => canExecute(candidate))
            {
            }

            public GeneratedCommand(
                Action<DataGridGeneratedOperationController<TItem>, object> execute,
                DataGridGeneratedOperationController<TItem> owner,
                Func<DataGridGeneratedOperationController<TItem>, object, bool> canExecute)
            {
                _execute = execute;
                _owner = owner;
                _canExecute = canExecute;
            }

            public event EventHandler CanExecuteChanged
            {
                add { }
                remove { }
            }

            public bool CanExecute(object parameter) => _canExecute?.Invoke(_owner, parameter) ?? true;

            public void Execute(object parameter)
            {
                if (CanExecute(parameter))
                {
                    _execute(_owner, parameter);
                }
            }
        }
    }

    internal static class DataGridGeneratedOperationProjection
    {
        public static IReadOnlyList<DataGridGeneratedOperationDescriptor> Create(
            IReadOnlyList<SortingDescriptor> sorting,
            IReadOnlyList<FilteringDescriptor> filtering,
            IReadOnlyList<SearchDescriptor> searching)
        {
            int count = sorting.Count + filtering.Count + searching.Count;
            if (count == 0)
            {
                return Array.Empty<DataGridGeneratedOperationDescriptor>();
            }

            var result = new DataGridGeneratedOperationDescriptor[count];
            int index = 0;
            for (int descriptorIndex = 0; descriptorIndex < sorting.Count; descriptorIndex++)
            {
                SortingDescriptor descriptor = sorting[descriptorIndex];
                string direction = descriptor.Direction == ListSortDirection.Ascending ? "ascending" : "descending";
                result[index++] = new DataGridGeneratedOperationDescriptor(
                    DataGridGeneratedOperationDescriptorKind.Sorting,
                    descriptor.ColumnId,
                    Convert.ToString(descriptor.ColumnId, CultureInfo.InvariantCulture) + " " + direction,
                    descriptor);
            }

            for (int descriptorIndex = 0; descriptorIndex < filtering.Count; descriptorIndex++)
            {
                FilteringDescriptor descriptor = filtering[descriptorIndex];
                string summary = Convert.ToString(descriptor.ColumnId, CultureInfo.InvariantCulture) + " " + descriptor.Operator;
                if (descriptor.Value != null)
                {
                    summary += " " + Convert.ToString(descriptor.Value, descriptor.Culture ?? CultureInfo.InvariantCulture);
                }
                result[index++] = new DataGridGeneratedOperationDescriptor(
                    DataGridGeneratedOperationDescriptorKind.Filtering,
                    descriptor.ColumnId,
                    summary,
                    descriptor);
            }

            for (int descriptorIndex = 0; descriptorIndex < searching.Count; descriptorIndex++)
            {
                SearchDescriptor descriptor = searching[descriptorIndex];
                result[index++] = new DataGridGeneratedOperationDescriptor(
                    DataGridGeneratedOperationDescriptorKind.Searching,
                    null,
                    "Search " + descriptor.Query,
                    descriptor);
            }

            return result;
        }
    }
}
