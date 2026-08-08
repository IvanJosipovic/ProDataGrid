// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Controls.DataGridFiltering;
using Avalonia.Controls.DataGridSearching;
using Avalonia.Controls.DataGridSorting;
using Xunit;

namespace Avalonia.Controls.DataGridTests.ColumnDefinitions;

public sealed class DataGridRuntimeSchemaAdapterTests
{
    [Fact]
    public void Provider_shape_is_materialized_once_and_columns_are_fresh()
    {
        var provider = new DictionarySchemaProvider();
        var schema = new DataGridRuntimeSchemaAdapter<Dictionary<string, object?>>(provider);

        DataGridColumnDefinitionList first = schema.CreateColumnDefinitions();
        DataGridColumnDefinitionList second = schema.CreateColumnDefinitions();

        Assert.Equal(1, provider.CreateFieldsCalls);
        Assert.Equal(2, first.Count);
        Assert.NotSame(first[0], second[0]);
        Assert.Equal("name", first[0].ColumnKey);
        Assert.Equal("Name", first[0].SortMemberPath);
        Assert.Same(schema.RuntimeFields[0].Accessor, first[0].ValueAccessor);
        Assert.Equal(typeof(string), first[0].ValueType);
    }

    [Fact]
    public void Explicit_runtime_accessors_drive_sort_filter_and_search()
    {
        var schema = new DataGridRuntimeSchemaAdapter<Dictionary<string, object?>>(new DictionarySchemaProvider());
        var rows = new List<Dictionary<string, object?>>
        {
            Row("Beta", 2),
            Row("Alpha", 1),
            Row("Alpha", 3)
        };

        IComparer<Dictionary<string, object?>> comparer = schema.CreateSortComparer(
            new[]
            {
                new SortingDescriptor("name", ListSortDirection.Ascending, "Name"),
                new SortingDescriptor("score", ListSortDirection.Descending, "Score")
            });
        rows.Sort(comparer);
        Func<Dictionary<string, object?>, bool> filter = schema.CreateFilterPredicate(
            new[] { new FilteringDescriptor("score", FilteringOperator.GreaterThan, "Score", 1) });
        Func<Dictionary<string, object?>, bool> search = schema.CreateSearchPredicate(
            new[] { new SearchDescriptor("alp", SearchMatchMode.Contains) });

        Assert.Equal(new[] { 3, 1, 2 }, rows.ConvertAll(static row => (int)row["Score"]!));
        Assert.True(filter(rows[0]));
        Assert.False(filter(rows[1]));
        Assert.True(search(rows[0]));
        Assert.False(search(rows[2]));
    }

    [Fact]
    public void Runtime_manifest_is_stable_and_path_is_explicitly_marked()
    {
        var first = new DataGridRuntimeSchemaAdapter<Dictionary<string, object?>>(new DictionarySchemaProvider());
        var second = new DataGridRuntimeSchemaAdapter<Dictionary<string, object?>>(new DictionarySchemaProvider());

        Assert.IsAssignableFrom<IDataGridRuntimeDefinedSchema>(first);
        Assert.Equal("tests/dictionary/v1", first.Manifest.SchemaId);
        Assert.Equal(first.Manifest.SchemaHash, second.Manifest.SchemaHash);
        Assert.Equal(2, first.Manifest.Fields.Count);
        Assert.Equal("name", first.Manifest.Fields[0].ColumnKey);
        Assert.True(first.CreateFastPathOptions().UseAccessorsOnly);
        Assert.True(first.CreateFastPathOptions().ThrowOnMissingAccessor);
    }

    [Fact]
    public void Adapter_rejects_invalid_runtime_shapes_early()
    {
        Assert.Throws<ArgumentException>(() =>
            new DataGridRuntimeSchemaAdapter<Dictionary<string, object?>>(new DuplicateSchemaProvider()));
        Assert.Throws<ArgumentException>(() =>
            new DataGridRuntimeSchemaField<Dictionary<string, object?>>(
                "invalid",
                "Invalid",
                new DataGridColumnValueAccessor<object, string>(static _ => string.Empty),
                static () => new DataGridTextColumnDefinition()));

        var field = new DataGridRuntimeSchemaField<Dictionary<string, object?>>(
            "null-factory",
            "NullFactory",
            new DataGridColumnValueAccessor<Dictionary<string, object?>, string>(static _ => string.Empty),
            static () => null!);
        Assert.Throws<InvalidOperationException>(() => field.CreateColumnDefinition());
    }

    private static Dictionary<string, object?> Row(string name, int score) =>
        new(StringComparer.Ordinal)
        {
            ["Name"] = name,
            ["Score"] = score
        };

    private sealed class DictionarySchemaProvider : IDataGridRuntimeSchemaProvider<Dictionary<string, object?>>
    {
        public int CreateFieldsCalls { get; private set; }

        public string SchemaId => "tests/dictionary/v1";

        public IReadOnlyList<DataGridRuntimeSchemaField<Dictionary<string, object?>>> CreateFields()
        {
            CreateFieldsCalls++;
            return
            [
                new DataGridRuntimeSchemaField<Dictionary<string, object?>>(
                    "name",
                    "Name",
                    new DataGridColumnValueAccessor<Dictionary<string, object?>, string>(
                        static row => (string)row["Name"]!,
                        static (row, value) => row["Name"] = value),
                    static () => new DataGridTextColumnDefinition { Header = "Name" }),
                new DataGridRuntimeSchemaField<Dictionary<string, object?>>(
                    "score",
                    "Score",
                    new DataGridColumnValueAccessor<Dictionary<string, object?>, int>(
                        static row => (int)row["Score"]!,
                        static (row, value) => row["Score"] = value),
                    static () => new DataGridNumericColumnDefinition { Header = "Score" })
            ];
        }

        public DataGridFastPathOptions CreateFastPathOptions() =>
            new()
            {
                UseAccessorsOnly = true,
                ThrowOnMissingAccessor = true,
                EnableHighPerformanceSearching = true
            };
    }

    private sealed class DuplicateSchemaProvider : IDataGridRuntimeSchemaProvider<Dictionary<string, object?>>
    {
        public string SchemaId => "tests/duplicate/v1";

        public IReadOnlyList<DataGridRuntimeSchemaField<Dictionary<string, object?>>> CreateFields()
        {
            DataGridRuntimeSchemaField<Dictionary<string, object?>> Field() =>
                new(
                    "duplicate",
                    "Value",
                    new DataGridColumnValueAccessor<Dictionary<string, object?>, object?>(static _ => null),
                    static () => new DataGridTextColumnDefinition());
            return [Field(), Field()];
        }

        public DataGridFastPathOptions CreateFastPathOptions() => new();
    }
}
