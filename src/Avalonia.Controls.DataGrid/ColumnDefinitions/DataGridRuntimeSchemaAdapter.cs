// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable disable

using System;
using System.Collections.Generic;
using Avalonia.Controls.DataGridFiltering;
using Avalonia.Controls.DataGridSearching;
using Avalonia.Controls.DataGridSorting;

namespace Avalonia.Controls
{
    /// <summary>
    /// Describes a runtime-defined field backed by an explicit value accessor.
    /// </summary>
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    interface IDataGridRuntimeSchemaField
    {
        /// <summary>Gets the stable field and column key.</summary>
        string ColumnKey { get; }

        /// <summary>Gets the compatibility property-path alias.</summary>
        string PropertyName { get; }

        /// <summary>Gets the explicit runtime accessor.</summary>
        IDataGridColumnValueAccessor Accessor { get; }

        /// <summary>Gets whether global search includes this field.</summary>
        bool IsSearchable { get; }

        /// <summary>Gets export, editor, localization, and accessibility metadata.</summary>
        DataGridGeneratedFieldMetadata Metadata { get; }

        /// <summary>Creates one mutable column definition for a grid instance.</summary>
        DataGridColumnDefinition CreateColumnDefinition();
    }

    /// <summary>
    /// Supplies a bounded runtime schema without assembly scanning or property discovery.
    /// Implementations own any dynamic-shape inspection and must return explicit accessors.
    /// </summary>
    /// <typeparam name="TItem">The runtime row item type.</typeparam>
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    interface IDataGridRuntimeSchemaProvider<TItem>
    {
        /// <summary>Gets the stable application-defined schema identifier.</summary>
        string SchemaId { get; }

        /// <summary>
        /// Creates the runtime field collection. The adapter calls this exactly once.
        /// </summary>
        IReadOnlyList<DataGridRuntimeSchemaField<TItem>> CreateFields();

        /// <summary>Creates fast-path options for the explicit-accessor schema.</summary>
        DataGridFastPathOptions CreateFastPathOptions();
    }

    /// <summary>
    /// Marks a schema whose field shape is supplied explicitly at runtime rather than inferred by a source generator.
    /// </summary>
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    interface IDataGridRuntimeDefinedSchema
    {
        /// <summary>Gets the immutable runtime fields materialized for this schema instance.</summary>
        IReadOnlyList<IDataGridRuntimeSchemaField> RuntimeFields { get; }
    }

    /// <summary>
    /// Defines one runtime field while preserving an explicit accessor and a fresh-column factory.
    /// </summary>
    /// <typeparam name="TItem">The runtime row item type.</typeparam>
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    sealed class DataGridRuntimeSchemaField<TItem> : IDataGridRuntimeSchemaField
    {
        private readonly Func<DataGridColumnDefinition> _columnFactory;

        /// <summary>Initializes a runtime schema field.</summary>
        public DataGridRuntimeSchemaField(
            string columnKey,
            string propertyName,
            IDataGridColumnValueAccessor accessor,
            Func<DataGridColumnDefinition> columnFactory,
            bool isSearchable = true,
            DataGridGeneratedFieldMetadata metadata = null)
        {
            if (string.IsNullOrWhiteSpace(columnKey))
            {
                throw new ArgumentException("A runtime field requires a non-empty column key.", nameof(columnKey));
            }

            Accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
            if (accessor.ItemType != typeof(TItem))
            {
                throw new ArgumentException(
                    $"Accessor item type '{accessor.ItemType}' must exactly match runtime schema item type '{typeof(TItem)}'.",
                    nameof(accessor));
            }

            _columnFactory = columnFactory ?? throw new ArgumentNullException(nameof(columnFactory));
            ColumnKey = columnKey;
            PropertyName = string.IsNullOrWhiteSpace(propertyName) ? columnKey : propertyName;
            IsSearchable = isSearchable;
            Metadata = metadata ?? new DataGridGeneratedFieldMetadata(automationId: columnKey);
        }

        /// <inheritdoc />
        public string ColumnKey { get; }

        /// <inheritdoc />
        public string PropertyName { get; }

        /// <inheritdoc />
        public IDataGridColumnValueAccessor Accessor { get; }

        /// <inheritdoc />
        public bool IsSearchable { get; }

        /// <inheritdoc />
        public DataGridGeneratedFieldMetadata Metadata { get; }

        /// <inheritdoc />
        public DataGridColumnDefinition CreateColumnDefinition()
        {
            DataGridColumnDefinition column = _columnFactory() ??
                throw new InvalidOperationException($"Runtime column factory for '{ColumnKey}' returned null.");
            column.ColumnKey ??= ColumnKey;
            column.SortMemberPath ??= PropertyName;
            column.ValueAccessor ??= Accessor;
            column.ValueType ??= Accessor.ValueType;
            return column;
        }
    }

    /// <summary>
    /// Adapts an explicit runtime field provider to the same column, operation, manifest, and fast-path
    /// contracts used by generated schemas.
    /// </summary>
    /// <typeparam name="TItem">The runtime row item type.</typeparam>
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    class DataGridRuntimeSchemaAdapter<TItem> :
        IDataGridGeneratedSchema<TItem>,
        IDataGridGeneratedSchemaManifestProvider,
        IDataGridRuntimeDefinedSchema
    {
        private readonly IDataGridRuntimeSchemaProvider<TItem> _provider;
        private readonly DataGridRuntimeSchemaField<TItem>[] _fields;
        private readonly IReadOnlyList<IDataGridRuntimeSchemaField> _runtimeFields;
        private readonly DataGridGeneratedDataOperations<TItem> _operations;
        private readonly DataGridGeneratedSchemaManifest _manifest;

        /// <summary>Initializes an adapter and materializes the provider shape exactly once.</summary>
        public DataGridRuntimeSchemaAdapter(IDataGridRuntimeSchemaProvider<TItem> provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            if (string.IsNullOrWhiteSpace(provider.SchemaId))
            {
                throw new ArgumentException("A runtime schema provider requires a non-empty schema identifier.", nameof(provider));
            }

            IReadOnlyList<DataGridRuntimeSchemaField<TItem>> suppliedFields = provider.CreateFields() ??
                throw new ArgumentException("A runtime schema provider returned a null field collection.", nameof(provider));
            _fields = new DataGridRuntimeSchemaField<TItem>[suppliedFields.Count];
            var runtimeFields = new IDataGridRuntimeSchemaField[suppliedFields.Count];
            var registrations = new DataGridColumnAccessorRegistration[suppliedFields.Count];
            var manifestFields = new DataGridGeneratedField[suppliedFields.Count];
            var keys = new HashSet<string>(StringComparer.Ordinal);

            for (int index = 0; index < suppliedFields.Count; index++)
            {
                DataGridRuntimeSchemaField<TItem> field = suppliedFields[index] ??
                    throw new ArgumentException("Runtime schema fields cannot contain null entries.", nameof(provider));
                if (!keys.Add(field.ColumnKey))
                {
                    throw new ArgumentException($"Runtime schema field key '{field.ColumnKey}' is duplicated.", nameof(provider));
                }

                _fields[index] = field;
                runtimeFields[index] = field;
                registrations[index] = new DataGridColumnAccessorRegistration(
                    field.ColumnKey,
                    field.PropertyName,
                    field.Accessor,
                    field.IsSearchable);
                manifestFields[index] = new DataGridGeneratedField(
                    index,
                    field.ColumnKey,
                    field.PropertyName,
                    field.Accessor.ValueType,
                    field.Accessor,
                    field.IsSearchable,
                    field.Metadata);
            }

            _operations = new DataGridGeneratedDataOperations<TItem>(registrations);
            _runtimeFields = Array.AsReadOnly(runtimeFields);
            _manifest = new DataGridGeneratedSchemaManifest(
                1,
                provider.SchemaId,
                CreateShapeHash(provider.SchemaId, _fields),
                typeof(TItem),
                manifestFields);
        }

        /// <inheritdoc />
        public DataGridGeneratedSchemaManifest Manifest => _manifest;

        /// <inheritdoc />
        public IReadOnlyList<IDataGridRuntimeSchemaField> RuntimeFields => _runtimeFields;

        /// <inheritdoc />
        public DataGridColumnDefinitionList CreateColumnDefinitions()
        {
            var columns = new DataGridColumnDefinitionList();
            for (int index = 0; index < _fields.Length; index++)
            {
                columns.Add(_fields[index].CreateColumnDefinition());
            }

            return columns;
        }

        /// <inheritdoc />
        public IComparer<TItem> CreateSortComparer(IReadOnlyList<SortingDescriptor> descriptors) =>
            _operations.CreateSortComparer(descriptors);

        /// <inheritdoc />
        public Func<TItem, bool> CreateFilterPredicate(IReadOnlyList<FilteringDescriptor> descriptors) =>
            _operations.CreateFilterPredicate(descriptors);

        /// <inheritdoc />
        public Func<TItem, bool> CreateSearchPredicate(IReadOnlyList<SearchDescriptor> descriptors) =>
            _operations.CreateSearchPredicate(descriptors);

        /// <inheritdoc />
        public DataGridFastPathOptions CreateFastPathOptions() =>
            _provider.CreateFastPathOptions() ??
            throw new InvalidOperationException("Runtime schema provider returned null fast-path options.");

        private static string CreateShapeHash(
            string schemaId,
            IReadOnlyList<DataGridRuntimeSchemaField<TItem>> fields)
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            ulong hash = offset;
            Add(schemaId);
            Add(typeof(TItem).FullName);
            for (int index = 0; index < fields.Count; index++)
            {
                DataGridRuntimeSchemaField<TItem> field = fields[index];
                Add(field.ColumnKey);
                Add(field.PropertyName);
                Add(field.Accessor.ValueType.FullName);
                Add(field.IsSearchable ? "1" : "0");
            }

            return hash.ToString("x16", System.Globalization.CultureInfo.InvariantCulture);

            void Add(string value)
            {
                if (value != null)
                {
                    for (int index = 0; index < value.Length; index++)
                    {
                        char character = value[index];
                        hash ^= (byte)character;
                        hash *= prime;
                        hash ^= (byte)(character >> 8);
                        hash *= prime;
                    }
                }

                hash ^= 255;
                hash *= prime;
            }
        }
    }
}
